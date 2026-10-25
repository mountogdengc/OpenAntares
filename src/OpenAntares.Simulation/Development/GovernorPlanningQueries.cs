using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Random;
using OpenAntares.Simulation.Species;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Development;
public static class GovernorPlanningQueries
{
    public static IGovernorPlanningContext Create(ContentSet content,EmpireView view)
    {
        if(!Rules.HasDevelopment(view.RulesVersion))throw new ArgumentException("Governor planning requires development rules.");
        return new Context(content,view with{Galaxy=view.Galaxy with{Seed=null}});
    }
    public static GovernorColonyOrder? Preview(ContentSet content,EmpireView view,ColonyId colony,GovernorTemplateSnapshot? template,bool workforceEnabled,bool constructionEnabled)
    {
        if(!Rules.HasDevelopment(view.RulesVersion)||!workforceEnabled&&!constructionEnabled||workforceEnabled&&template is null)return null;
        if(template is not null&&DevelopmentStateValidator.DraftErrors(content,template.Definition).Any())return null;
        var owned=view.Colonies.FirstOrDefault(c=>c.Id==colony&&c.EmpireId==view.Viewer);
        if(owned?.Development is not {} d)return null;
        var proposed=owned with{Development=d with{Template=template,WorkforceEnabled=workforceEnabled,ConstructionEnabled=constructionEnabled}};
        var preview=view with{Colonies=System.Collections.Immutable.ImmutableArray.Create(proposed)};
        var result=GovernorPlanner.Plan(Create(content,preview));return result.Succeeded?result.Orders.SingleOrDefault():null;
    }
    private sealed record Context(ContentSet Content,EmpireView View):IGovernorPlanningContext
    {
        // A calculator input built solely from immutable permitted own facts. No world reference.
        private readonly GameState _frame=OwnFrame(View);
        public ColonyEconomy? Forecast(ColonyId colony,Workforce workforce,ProductionTarget? target)
        {
            var source=_frame.FindColony(colony);
            if(source is null||workforce.HasNegativeCount||(long)workforce.Support+workforce.Production+workforce.Research!=source.Population)return null;
            if(target is {} t) {
                if(t.Kind==ProductionKinds.Building) {if(!Content.Buildings.TryGetValue(t.ContentId,out var b)||source.CompletedBuildingIds.Contains(t.ContentId)||!Rules.PrerequisitesKnown(_frame.Empires[0],b.PrerequisiteTechnologyIds))return null;}
                else if(t.Kind!=ProductionKinds.Vessel||!SpeciesRules.CanBuildVessel(Content,_frame,View.Viewer,t.ContentId))return null;
            }
            var frame=_frame.Clone();var proposed=frame.FindColony(colony)!;proposed.Workforce=workforce;proposed.ProjectId=target?.ContentId;proposed.ProjectKind=target?.Kind??ProductionKinds.Building;
            try{return EconomyCalculator.CalculateColony(Content,frame,proposed);}catch(OverflowException){return null;}
        }
        private static GameState OwnFrame(EmpireView view)
        {
            var owner=new EmpireState{Id=view.Viewer,SpeciesId=view.SpeciesId,Controller=ControllerKind.Human,KnownTechnologyIds=view.Research.KnownTechnologyIds.ToList()};
            return new GameState {
                RulesVersion=view.RulesVersion,Turn=view.Turn,Random=new Pcg32{State=0,Increment=1},Empires=new(){owner},
                Planets=view.Stars.Where(s=>s.Planets.HasValue).SelectMany(s=>s.Planets!.Value.Where(p=>view.Colonies.Any(c=>c.PlanetId==p.Id)).Select(p=>new PlanetState{Id=p.Id,StarId=s.Chart.Id,Orbit=p.Orbit,PlanetTypeId=p.PlanetTypeId,EnvironmentId=p.EnvironmentId})).ToList(),
                Colonies=view.Colonies.Select(c=>new ColonyState{Id=c.Id,EmpireId=view.Viewer,PlanetId=c.PlanetId,Population=c.Population,Workforce=c.Workforce,GrowthProgress=c.GrowthProgress,ProductionReserve=c.ProductionReserve,ProjectId=c.ProjectId,ProjectKind=c.ProjectKind,HabitatMode=c.HabitatMode,CompletedBuildingIds=c.CompletedBuildingIds.ToList()}).ToList()
            };
        }
    }
}
