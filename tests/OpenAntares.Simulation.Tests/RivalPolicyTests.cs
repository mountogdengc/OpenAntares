using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Colonization;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class RivalPolicyTests
{
    [Fact]
    public void ExtremePopulationAndEmptyCandidatesStayBounded()
    {
        var state=RivalFixtures.AtHome("harl");var real=RivalPlanningQueries.Create(RivalFixtures.Content(),state,state.Empires[1].Id);
        var context=new HugeContext(real);var commands=new List<Command>();
        var result=ExpansionRivalPlanner.Plan(()=>context,c=>{commands.Add(c);return true;});
        Assert.True(result.Succeeded);Assert.InRange(context.ForecastCalls,1,100);
        var workforce=Assert.Single(commands.OfType<SetWorkforceCommand>()).Workforce;
        Assert.Equal(new Workforce(0,1073741824,1073741823),workforce);
        Assert.InRange(commands.Count,1,4);Assert.DoesNotContain(commands,c=>c is SetProductionTargetCommand);
    }

    private sealed class HugeContext : IRivalPlanningContext
    {
        private readonly IRivalPlanningContext _inner;
        public HugeContext(IRivalPlanningContext inner)
        {
            _inner=inner;Content=inner.Content with{Buildings=ImmutableSortedDictionary<string,BuildingDefinition>.Empty,Technologies=ImmutableSortedDictionary<string,TechnologyDefinition>.Empty};
            var old=inner.View.Colonies[0];var huge=old with{Population=int.MaxValue,Workforce=new(int.MaxValue,0,0),Economy=old.Economy with{Growth=old.Economy.Growth with{Capacity=int.MaxValue}}};
            View=inner.View with{Colonies=ImmutableArray.Create(huge)};
        }
        public int ForecastCalls { get; private set; }
        public EmpireView View { get; }
        public ContentSet Content { get; }
        public RivalAiDefinition Policy=>_inner.Policy;
        public ColonyEconomy? Forecast(ColonyId colony,Workforce workers,ProductionTarget? target,string? completedBuildingId=null)
        {
            if(++ForecastCalls>100)throw new InvalidOperationException("Population scan exceeded bounded query budget.");
            return View.Colonies[0].Economy with{SupportProduced=Quantity.Zero,SupportRequired=Quantity.Zero,GrowthEarned=Quantity.Zero,
                NetProduction=Quantity.From(new BreakdownLine("test","",null,workers.Production)),NetResearch=Quantity.From(new BreakdownLine("test","",null,workers.Research))};
        }
        public FoundingPreview? PreviewSettlement(PlanetId planet,string role)=>null;
        public int? TravelTurns(StarId from,StarId to,string role)=>_inner.TravelTurns(from,to,role);
    }

    [Fact]
    public void UnexpectedRejectedCommandStopsImmediately()
    {
        var state=RivalFixtures.AtHome("harl");var context=RivalPlanningQueries.Create(RivalFixtures.Content(),state,state.Empires[1].Id);int submitted=0;
        var result=ExpansionRivalPlanner.Plan(()=>context,c=>{submitted++;return false;});
        Assert.False(result.Succeeded);Assert.Equal(1,submitted);Assert.Equal(1,result.CommandCount);
    }

    [Theory]
    [InlineData(false,0,1)]
    [InlineData(true,1,0)]
    public void SoleEconomicWorkerResearchesOrBanksWhenNoProjectExists(bool allKnown,int production,int research)
    {
        var content=RivalFixtures.Content();content=content with{Buildings=ImmutableSortedDictionary<string,BuildingDefinition>.Empty};
        var state=RivalFixtures.AtHome("harl");var ai=state.Empires[1];var colony=state.Colonies.Single(c=>c.EmpireId==ai.Id);colony.Population=2;colony.Workforce=new(1,1,0);
        if(allKnown)ai.KnownTechnologyIds=content.Technologies.Keys.ToList();
        var result=Plan(content,state);Assert.Equal(new Workforce(1,production,research),result.State.FindColony(colony.Id)!.Workforce);Assert.Null(result.State.FindColony(colony.Id)!.ProjectId);
    }

    [Fact]
    public void ScoutsAvoidOwnClaimedDestinationsAndRespectEmbeddedSurveyAge()
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("harl");var ai=state.Empires[1];
        var third=new StarState{Id=new(state.AllocateId()),Name="Third",X=600,Y=0};state.Stars.Add(third);
        var planet=new PlanetState{Id=new(state.AllocateId()),StarId=third.Id,Orbit=1,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea"};state.Planets.Add(planet);
        var secondScout=new VesselState{Id=new(state.AllocateId()),EmpireId=ai.Id,RoleId="scout",StationaryStarId=state.Stars[1].Id};state.Vessels.Add(secondScout);
        var result=Plan(content,state);var destinations=result.State.Vessels.Where(v=>v.EmpireId==ai.Id).Select(v=>v.PendingMove).ToArray();
        Assert.Equal(2,destinations.Distinct().Count());Assert.Contains(state.Stars[0].Id,destinations);Assert.Contains(third.Id,destinations);
        state.Vessels.Remove(secondScout);state.Turn=5;
        var human=state.Empires[0];var humanPlanet=state.Planets[0];
        ai.Surveys.Add(new SystemObservation{StarId=state.Stars[0].Id,LastObservedTurn=1,Planets={new(humanPlanet.Id,1,humanPlanet.PlanetTypeId,state.Colonies[0].Id,human.Id){EnvironmentId=humanPlanet.EnvironmentId}}});
        ai.Surveys.Add(new SystemObservation{StarId=third.Id,LastObservedTurn=2,Planets={new(planet.Id,1,planet.PlanetTypeId,null,null){EnvironmentId=planet.EnvironmentId}}});
        ai.Contacts.Add(new(human.Id,human.SpeciesId!,1));human.Contacts.Add(new(ai.Id,ai.SpeciesId!,1));ObservationUpdater.Refresh(state,5);
        Assert.Equal(state.Stars[0].Id,Plan(content,state).State.Vessels.Single(v=>v.EmpireId==ai.Id).PendingMove);
        Assert.Null(Plan(content with{RivalAi=new("expansion_v1",1,5)},state).State.Vessels.Single(v=>v.EmpireId==ai.Id).PendingMove);
    }
    internal static (GameState State, List<Command> Commands, RivalPlanningResult Result) Plan(ContentSet content, GameState input)
    {
        var state = input; var ai = state.Empires.Single(e=>e.Controller==ControllerKind.Ai); var commands = new List<Command>();
        var result = ExpansionRivalPlanner.Plan(() => RivalPlanningQueries.Create(content,state,ai.Id), command => {
            commands.Add(command); var outcome = CommandProcessor.Execute(content,state,command);
            Assert.True(outcome.Accepted,outcome.Rejection?.Message); Assert.True(outcome.Changed,"Planner must not emit no-op commands."); state=outcome.State; return true;
        });
        Assert.True(result.Succeeded); Assert.Equal(commands.Count,result.CommandCount);
        Assert.True(commands.Count <= 2L*input.Colonies.Count(c=>c.EmpireId==ai.Id)+input.Vessels.Count(v=>v.EmpireId==ai.Id)+1);
        Assert.All(commands,c=>{Assert.Equal(ai.Id,c.Issuer);Assert.Equal(input.Turn,c.ExpectedTurn);Assert.IsNotType<SetReadyCommand>(c);});
        return(state,commands,result);
    }

    internal static PlanetState KnownWorld(GameState state, string type="verdant_world", string environment="shelf_sea")
    {
        var ai=state.Empires.Single(e=>e.Controller==ControllerKind.Ai); var home=state.FindPlanet(state.Colonies.Single(c=>c.EmpireId==ai.Id).PlanetId)!;
        var world=new PlanetState{Id=new(state.AllocateId()),StarId=home.StarId,Orbit=state.Planets.Where(p=>p.StarId==home.StarId).Max(p=>p.Orbit)+1,PlanetTypeId=type,EnvironmentId=environment};
        state.Planets.Add(world); ObservationUpdater.Refresh(state,state.Turn); return world;
    }

    internal static string CommandText(IEnumerable<Command> commands) => string.Join("\n",commands.Select(c=>JsonSerializer.Serialize(c,c.GetType())));

    [Fact]
    public void GoalAndWorkforceTieOrderIsStable()
    {
        var content=RivalFixtures.Content(); var alpha=content.Buildings["fabrication_hall"] with{Id="alpha_fab",Name="Alpha fabrication"};
        content=content with{Buildings=content.Buildings.Add(alpha.Id,alpha),Technologies=content.Technologies.Add("aaa_methods",content.Technologies["cultivation_methods"] with{Id="aaa_methods"})};
        var state=RivalFixtures.AtHome("harl"); var ai=state.Empires[1];
        var first=Plan(content,state); Assert.Equal("alpha_fab",first.State.Colonies.Single(c=>c.EmpireId==ai.Id).ProjectId);
        Assert.Equal("aaa_methods",first.State.FindEmpire(ai.Id)!.ResearchTargetId);
        Assert.Equal(CommandText(first.Commands),CommandText(Plan(content,state).Commands));
        state.Colonies.Single(c=>c.EmpireId==ai.Id).ProjectId="fabrication_hall";
        Assert.Equal("fabrication_hall",Plan(content,state).State.Colonies.Single(c=>c.EmpireId==ai.Id).ProjectId);
    }

    [Fact]
    public void ScoutAndFounderPipelineStayBounded()
    {
        var content=RivalFixtures.Content(); var state=RivalFixtures.AtHome("harl"); var world=KnownWorld(state); var ai=state.Empires[1];
        var first=Plan(content,state); var colony=first.State.Colonies.Single(c=>c.EmpireId==ai.Id);
        Assert.Equal(ProductionKinds.Vessel,colony.ProjectKind); Assert.Equal("harl_settler",colony.ProjectId);
        var second=Plan(content,first.State); Assert.Equal("harl_settler",second.State.FindColony(colony.Id)!.ProjectId);
        var existing=first.State.Clone(); existing.FindColony(colony.Id)!.ProductionReserve=500;
        var founder=new VesselState{Id=new(existing.AllocateId()),EmpireId=ai.Id,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=world.StarId};existing.Vessels.Add(founder);
        var live=Plan(content,existing);Assert.NotEqual("harl_settler",live.State.FindColony(colony.Id)!.ProjectId);Assert.Equal(500,live.State.FindColony(colony.Id)!.ProductionReserve);
        Assert.Equal(world.Id,live.State.FindVessel(founder.Id)!.PendingFounding);
        state.Vessels.RemoveAll(v=>v.EmpireId==ai.Id);
        var replacement=Plan(content,state);Assert.Equal("scout",replacement.State.Colonies.Single(c=>c.EmpireId==ai.Id).ProjectId);
        Assert.Equal("scout",Plan(content,replacement.State).State.Colonies.Single(c=>c.EmpireId==ai.Id).ProjectId);
    }

    [Fact]
    public void ShipTargetsUseOnlyObservedWorlds()
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("peldra");var ai=state.Empires[1];var poor=KnownWorld(state,"barren_rock","dry");
        var preview=RivalPlanningQueries.Create(content,state,ai.Id).PreviewSettlement(poor.Id,"peldra_settler");Assert.NotNull(preview);
        Assert.Equal(200,preview.Economy!.SupportProduced.Total);Assert.Equal(400,preview.Economy.SupportRequired.Total);
        var founder=new VesselState{Id=new(state.AllocateId()),EmpireId=ai.Id,RoleId="peldra_settler",CarriedPopulation=2,StationaryStarId=state.Stars[1].Id};state.Vessels.Add(founder);
        var result=Plan(content,state); Assert.Equal(ProductionKinds.Building,result.State.Colonies.Single(c=>c.EmpireId==ai.Id).ProjectKind);
        Assert.Null(result.State.FindVessel(founder.Id)!.PendingMove);Assert.Null(result.State.FindVessel(founder.Id)!.PendingFounding);Assert.Equal(2,result.State.FindVessel(founder.Id)!.CarriedPopulation);
    }

    [Fact]
    public void PolicyIsInvariantUnderHiddenMutations()
    {
        var content=RivalFixtures.Content();var state=GalaxyGenerator.CreateNewGame(content,new("small",42,4,"harl",1,"peldra")).State!;
        var changed=state.Clone();changed.Empires[0].SpeciesId="wenth";changed.Empires[0].ResearchReserve=long.MaxValue;changed.Random.State^=123;changed.NextEntityId+=10;
        changed.FindPlanet(changed.Colonies[0].PlanetId)!.EnvironmentId="dry";
        var original=Plan(content,state);Assert.NotEmpty(original.Commands);
        Assert.Equal(CommandText(original.Commands),CommandText(Plan(content,changed).Commands));
    }
}
