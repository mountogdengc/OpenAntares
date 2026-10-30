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
    public static GovernorColonyOrder? Preview(ContentSet content,EmpireView view,ColonyId colony,GovernorTemplateSnapshot? template,bool workforceEnabled,bool constructionEnabled,bool replaceActive=false)
    {
        if(!Rules.HasDevelopment(view.RulesVersion)||!workforceEnabled&&!constructionEnabled||workforceEnabled&&template is null)return null;
        if(template is not null&&DevelopmentStateValidator.DraftErrors(content,template.Definition).Any())return null;
        var owned=view.Colonies.FirstOrDefault(c=>c.Id==colony&&c.EmpireId==view.Viewer);
        if(owned?.Development is not {} d)return null;
        var proposed=owned with{Development=d with{Template=template,WorkforceEnabled=workforceEnabled,ConstructionEnabled=constructionEnabled}};
        if(replaceActive&&owned.ProjectId is not null)
        {
            proposed=proposed with
            {
                ProjectId=null,
                ProjectKind=ProductionKinds.Building,
                Development=proposed.Development! with{ActiveQueueToken=null,Origin=ProductionOrigin.None}
            };
        }
        var preview=view with{Colonies=System.Collections.Immutable.ImmutableArray.Create(proposed)};
        var result=GovernorPlanner.Plan(Create(content,preview));return result.Succeeded?result.Orders.SingleOrDefault():null;
    }
    private sealed record Context(ContentSet Content,EmpireView View):IGovernorPlanningContext
    {
        // A calculator input built solely from immutable permitted own facts. No world reference.
        private readonly GameState _frame=OwnFrame(View);
        public ColonyEconomy? Forecast(ColonyId colony,Workforce workforce,ProductionTarget? target)
        {
            if(target is not null)
            {
                try{target=ShipDesigns.ShipDesignTargets.Normalize(Content,_frame,View.Viewer,target);}
                catch(ArgumentException){return null;}
            }
            var source=_frame.FindColony(colony);
            if(source is null||workforce.HasNegativeCount||(long)workforce.Support+workforce.Production+workforce.Research!=source.Population)return null;
            if(target is {} t) {
                if(t.Kind==ProductionKinds.Building) {if(!Content.Buildings.TryGetValue(t.ContentId,out var b)||source.CompletedBuildingIds.Contains(t.ContentId)||!Rules.PrerequisitesKnown(_frame.Empires[0],b.PrerequisiteTechnologyIds))return null;}
                else if(t.Kind==ProductionKinds.Refit) {if(ShipDesigns.ShipDesignTargets.Job(_frame,source) is null||source.ProjectKind!=t.Kind||source.ProjectId!=t.ContentId)return null;}
                else if(t.Kind==ProductionKinds.ShipDesign) {if(ShipDesigns.ShipDesignTargets.Bound(Content,_frame,View.Viewer,t) is null)return null;}
                else if(t.Kind!=ProductionKinds.Vessel||!SpeciesRules.CanBuildVessel(Content,_frame,View.Viewer,t.ContentId))return null;
            }
            var frame=_frame.Clone();var proposed=frame.FindColony(colony)!;proposed.Workforce=workforce;proposed.ProjectId=target?.ContentId;proposed.ProjectKind=target?.Kind??ProductionKinds.Building;
            try{return EconomyCalculator.CalculateColony(Content,frame,proposed);}catch(OverflowException){return null;}
        }
        private static GameState OwnFrame(EmpireView view)
        {
            var owner=new EmpireState{Id=view.Viewer,SpeciesId=view.SpeciesId,Controller=ControllerKind.Human,KnownTechnologyIds=view.Research.KnownTechnologyIds.ToList()};
            if(Rules.HasShipDesigns(view.RulesVersion)&&view.ShipDesigns is {} designs)
                owner.ShipDesigns=new(){CatalogueRevision=designs.CatalogueRevision,NextDesignId=checked(designs.Designs.Select(d=>d.Id.Value).DefaultIfEmpty().Max()+1),Designs=designs.Designs.Select(d=>new ShipDesigns.ShipDesign(d.Id,d.Name,d.Recipe,d.Archived)).ToList(),NextRefitId=designs.NextRefitId,Refits=designs.Refits.Select(j=>new ShipDesigns.RefitJob{Id=j.Id,Vessel=j.Vessel,Port=j.Port,SourceDesign=j.SourceDesign,TargetDesign=j.TargetDesign,AcceptedTurn=j.AcceptedTurn,Revision=j.Revision,Price=j.Price,Status=j.Status,Reason=j.Reason}).ToList()};
            var frame=new GameState {
                RulesVersion=view.RulesVersion,Turn=view.Turn,Random=new Pcg32{State=0,Increment=1},Empires=new(){owner},
                Planets=view.Stars.Where(s=>s.Planets.HasValue).SelectMany(s=>s.Planets!.Value.Where(p=>view.Colonies.Any(c=>c.PlanetId==p.Id)).Select(p=>new PlanetState{Id=p.Id,StarId=s.Chart.Id,Orbit=p.Orbit,PlanetTypeId=p.PlanetTypeId,EnvironmentId=p.EnvironmentId})).ToList(),
                Colonies=view.Colonies.Select(OwnColony).ToList()
            };
            // Calculator-only own metadata: retain exact existing bindings, without
            // allocating hypothetical IDs/tokens or holding a world reference.
            if(Rules.HasFleets(view.RulesVersion)&&view.Fleets is {} fleets)
                owner.Fleets=new()
                {
                    Fleets=fleets.Fleets.Select(f=>new Fleets.FleetRecord{Id=f.Id,Name=f.Name,Members=f.Members.ToList(),Flagship=f.Flagship,Officer=f.Officer,Order=f.Order,Cohort=f.Cohort}).ToList(),
                    Officers=fleets.Officers.Select(o=>new Fleets.OfficerRecord{Id=o.Id,Flagship=o.Flagship,Specialty=o.Specialty,Xp=o.Xp,Rank=o.Rank,Status=o.Status,PostedColony=o.PostedColony,Request=o.Request}).ToList(),
                    Requests=fleets.Requests.Select(r=>new Fleets.AssemblyRequest{Id=r.Id,RallyFleet=r.RallyFleet,Status=r.Status,Colonies=r.Colonies.ToList(),PausedContributions=r.PausedContributions.ToList(),Coordinator=r.Coordinator,Slots=r.Slots.Select(s=>new Fleets.AssemblySlot{Ordinal=s.Ordinal,RoleId=s.RoleId,Design=s.Design,Vessel=s.Vessel,Colony=s.Colony,QueueToken=s.QueueToken,Status=s.Status,ResumeStatus=s.ResumeStatus}).ToList()}).ToList()
                };
            if(Rules.HasShipDesigns(view.RulesVersion))
            {
                frame.Vessels=view.Vessels.Select(v=>
                {
                    var combat=view.Combat!.Ships.Single(c=>c.Id==v.Id);var scout=view.Scouting?.Vessels.SingleOrDefault(s=>s.Id==v.Id);
                    return new VesselState{Id=v.Id,EmpireId=v.EmpireId,RoleId=v.RoleId,Design=v.Loadout?.Design,StationaryStarId=v.StationaryStarId,Transit=v.Transit,PendingMove=v.PendingMove,PendingFounding=v.PendingFounding,CarriedPopulation=v.CarriedPopulation,Fleet=view.Fleets!.Ships.Single(s=>s.Id==v.Id).Fleet,Combat=new(){Hull=combat.Hull,Cohesion=combat.Cohesion,Repairing=combat.Repairing,ReturnColony=combat.ReturnColony,OnArrival=combat.OnArrival},AmebaEncounter=scout?.Stage is {} stage?new(){Stage=stage}:null};
                }).ToList();
                frame.Combat=new(){Encounters=view.Combat!.Encounters.Select(e=>new Combat.EncounterState{Id=e.Id,Owner=view.Viewer,Fleet=e.Fleet,Members=e.Members.ToList(),Phase=e.Phase}).ToList()};
            }
            Crises.CrisisForecastFrame.Apply(frame,view);
            return frame;
        }
        private static ColonyState OwnColony(OwnedColonyView c)=>new()
        {
            Id=c.Id,EmpireId=c.EmpireId,PlanetId=c.PlanetId,Population=c.Population,Workforce=c.Workforce,GrowthProgress=c.GrowthProgress,ProductionReserve=c.ProductionReserve,ProjectId=c.ProjectId,ProjectKind=c.ProjectKind,HabitatMode=c.HabitatMode,CompletedBuildingIds=c.CompletedBuildingIds.ToList(),
            Development=c.Development is {} d?new(){Template=d.Template,WorkforceEnabled=d.WorkforceEnabled,ConstructionEnabled=d.ConstructionEnabled,Queue=d.Queue.ToList(),NextQueueToken=d.NextQueueToken??checked(d.Queue.Select(q=>q.Token).DefaultIfEmpty().Max()+1),ActiveQueueToken=d.ActiveQueueToken,Origin=d.Origin}:null
        };
    }
}
