using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class RivalSpeciesPolicyTests
{
    [Theory]
    [InlineData("peldra")]
    [InlineData("harl")]
    [InlineData("sulan")]
    [InlineData("wenth")]
    public void GeneratedRivalBuildsSecondColonyThroughRealTurns(string species)
    {
        var content=RivalFixtures.Content();var state=OpenAntares.Simulation.Galaxy.GalaxyGenerator.CreateNewGame(content,new("small",42,4,"harl",1,species)).State!;
        var ai=state.Empires[1].Id;
        for(int turn=0;turn<80&&state.Colonies.Count(c=>c.EmpireId==ai)<2;turn++)
        {
            var planned=RivalPolicyTests.Plan(content,state);var next=TurnResolver.Resolve(content,planned.State);
            Assert.True(next.Succeeded,string.Join("; ",next.Errors));state=next.State;
        }
        Assert.True(state.Colonies.Count(c=>c.EmpireId==ai)>=2,$"{species}: no second colony by turn {state.Turn}.");
        Assert.True(state.FindEmpire(ai)!.Surveys.Count>1);
    }
    [Fact]
    public void PeldraSupportBridgeUsesOrdinaryShortage()
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("peldra");var ai=state.Empires[1];ai.KnownTechnologyIds.Add("cultivation_methods");
        var world=RivalPolicyTests.KnownWorld(state);var child=new ColonyState{Id=new(state.AllocateId()),EmpireId=ai.Id,PlanetId=world.Id,Population=2,Workforce=new(2,0,0),HabitatMode="native"};state.Colonies.Add(child);ObservationUpdater.Refresh(state,0);
        var planned=RivalPolicyTests.Plan(content,state);var daughter=planned.State.FindColony(child.Id)!;
        Assert.Equal("cultivation_hub",daughter.ProjectId);Assert.Equal(new Workforce(1,1,0),daughter.Workforce);
        var forecast=EconomyCalculator.Calculate(content,planned.State).Colony(child.Id);
        Assert.Equal(100,forecast.SupportDeficit);Assert.Equal(450,forecast.NetProduction.Total);Assert.Equal(0,forecast.GrowthEarned.Total);
    }

    [Fact]
    public void HarlOneUnitRecoversInTwoSnapshots()
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("harl");var ai=state.Empires[1];var world=RivalPolicyTests.KnownWorld(state);
        var child=new ColonyState{Id=new(state.AllocateId()),EmpireId=ai.Id,PlanetId=world.Id,Population=1,Workforce=new(1,0,0),HabitatMode="native"};state.Colonies.Add(child);ObservationUpdater.Refresh(state,0);
        for(int turn=0;turn<2;turn++)
        {
            var planned=RivalPolicyTests.Plan(content,state);Assert.Equal(50,EconomyCalculator.Calculate(content,planned.State).Colony(child.Id).GrowthEarned.Total);
            var next=TurnResolver.Resolve(content,planned.State);Assert.True(next.Succeeded,string.Join("; ",next.Errors));state=next.State;
        }
        Assert.Equal(2,state.FindColony(child.Id)!.Population);
    }

    [Theory]
    [InlineData("dry","shelf_sea","harbor_settler",8)]
    [InlineData("shelf_sea","dry","settler",12)]
    public void SulanChoosesNearHarborOrSuitableNativeWithoutIdBranches(string nearEnvironment,string farEnvironment,string expected,int capacity)
    {
        var content=RivalFixtures.Content();var species=content.Species["sulan"] with{Id="renamed_sulan",Name="Renamed sea people"};content=content with{Species=content.Species.Add(species.Id,species)};
        var state=RivalFixtures.AtHome("sulan");var ai=state.Empires[1];ai.SpeciesId=species.Id;var near=RivalPolicyTests.KnownWorld(state,environment:nearEnvironment);
        var star=new StarState{Id=new(state.AllocateId()),Name="Far",X=900,Y=0};state.Stars.Add(star);
        var far=new PlanetState{Id=new(state.AllocateId()),StarId=star.Id,Orbit=1,PlanetTypeId="verdant_world",EnvironmentId=farEnvironment};state.Planets.Add(far);
        ai.Surveys.Add(new SystemObservation{StarId=star.Id,LastObservedTurn=0,Planets={new(far.Id,1,far.PlanetTypeId,null,null){EnvironmentId=far.EnvironmentId}}});
        var planned=RivalPolicyTests.Plan(content,state);Assert.Equal(expected,planned.State.Colonies.Single(c=>c.EmpireId==ai.Id).ProjectId);
        Assert.Equal(capacity,RivalPlanningQueries.Create(content,state,ai.Id).PreviewSettlement(near.Id,expected)!.Capacity);
    }

    [Fact]
    public void WenthBanksThenEmbarksAtCurrentMaturity()
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("wenth");var ai=state.Empires[1];RivalPolicyTests.KnownWorld(state);
        var parent=state.Colonies.Single(c=>c.EmpireId==ai.Id);parent.Population=5;parent.Workforce=new(3,1,1);parent.GrowthProgress=50;parent.ProductionReserve=2400;
        var planned=RivalPolicyTests.Plan(content,state);var snapshot=EconomyCalculator.Calculate(content,planned.State).Colony(parent.Id);
        Assert.Null(snapshot.VesselCompletionPlan);Assert.Equal(6,snapshot.Growth.PopulationAfter);
        var immature=TurnResolver.Resolve(content,planned.State);Assert.True(immature.Succeeded,string.Join("; ",immature.Errors));Assert.DoesNotContain(immature.State.Vessels,v=>v.RoleId=="daughter_seed");
        var mature=RivalFixtures.AtHome("wenth");RivalPolicyTests.KnownWorld(mature);parent=mature.Colonies.Single(c=>c.EmpireId==ai.Id);parent.Population=6;parent.Workforce=new(3,2,1);parent.ProductionReserve=2400;
        planned=RivalPolicyTests.Plan(content,mature);snapshot=EconomyCalculator.Calculate(content,planned.State).Colony(parent.Id);
        Assert.Equal(800,snapshot.NetProduction.Total);Assert.Equal(800,snapshot.Production.Ending);Assert.Equal(3,snapshot.VesselCompletionPlan!.Cargo);
        var completed=TurnResolver.Resolve(content,planned.State);Assert.True(completed.Succeeded,string.Join("; ",completed.Errors));
        var after=completed.State.FindColony(parent.Id)!;Assert.Equal(3,after.Population);Assert.Equal(new Workforce(0,2,1),after.Workforce);
        var repaired=RivalPolicyTests.Plan(content,completed.State);var forecast=EconomyCalculator.Calculate(content,repaired.State).Colony(parent.Id);
        Assert.True(forecast.SupportProduced.Total>=forecast.SupportRequired.Total);
    }

    [Fact]
    public void ExtendedWenthNeedsEightAndStillPlantsThreeIntoSix()
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("wenth");var ai=state.Empires[1];var world=RivalPolicyTests.KnownWorld(state);
        ai.KnownTechnologyIds.Add("habitat_methods");var parent=state.Colonies.Single(c=>c.EmpireId==ai.Id);parent.CompletedBuildingIds.Add("habitat_extension");
        parent.Population=6;parent.Workforce=new(4,1,1);parent.ProductionReserve=2400;
        var planned=RivalPolicyTests.Plan(content,state);var forecast=EconomyCalculator.Calculate(content,planned.State).Colony(parent.Id);
        Assert.Null(forecast.VesselCompletionPlan);Assert.Contains(forecast.Production.BlockedRequirements,r=>r.Code=="full_colony"&&r.Required==8);
        parent.Population=8;parent.Workforce=new(6,1,1);planned=RivalPolicyTests.Plan(content,state);var next=TurnResolver.Resolve(content,planned.State);Assert.True(next.Succeeded,string.Join("; ",next.Errors));
        var seed=Assert.Single(next.State.Vessels,v=>v.RoleId=="daughter_seed");Assert.Equal(3,seed.CarriedPopulation);Assert.Equal(5,next.State.FindColony(parent.Id)!.Population);
        Assert.Equal(6,RivalPlanningQueries.Create(content,next.State,ai.Id).PreviewSettlement(world.Id,seed.RoleId)!.Capacity);
    }

    [Theory]
    [InlineData(0,4,1,1,5)]
    [InlineData(1,5,1,0,20)]
    public void EmbeddedGrowthPreferenceChangesLegalWorkersWithoutBonuses(int preference,int support,int production,int research,int growth)
    {
        var content=RivalFixtures.Content() with{RivalAi=new("expansion_v1",preference,3)};var state=RivalFixtures.AtHome("peldra");var ai=state.Empires[1];var planned=RivalPolicyTests.Plan(content,state);
        var colony=planned.State.Colonies.Single(c=>c.EmpireId==ai.Id);Assert.Equal(new Workforce(support,production,research),colony.Workforce);
        Assert.Equal(growth,EconomyCalculator.Calculate(content,planned.State).Colony(colony.Id).GrowthEarned.Total);
    }
    [Theory]
    [InlineData("peldra",5,1,0,1500,1200,600,0,20)]
    [InlineData("harl",3,2,1,900,600,400,200,70)]
    [InlineData("sulan",3,2,1,900,600,400,200,40)]
    [InlineData("wenth",3,1,0,900,400,400,0,50)]
    public void FirstSnapshotUsesExactSpeciesEconomy(string species,int support,int production,int research,long produced,long required,long netP,long netR,long growth)
    {
        var content=RivalFixtures.Content();var input=RivalFixtures.AtHome(species);var ai=input.Empires[1];
        var result=RivalPolicyTests.Plan(content,input);var colony=result.State.Colonies.Single(c=>c.EmpireId==ai.Id);
        Assert.Equal(new Workforce(support,production,research),colony.Workforce);Assert.Equal("fabrication_hall",colony.ProjectId);
        Assert.Equal("cultivation_methods",result.State.FindEmpire(ai.Id)!.ResearchTargetId);
        var economy=EconomyCalculator.Calculate(content,result.State).Colony(colony.Id);
        Assert.Equal(produced,economy.SupportProduced.Total);Assert.Equal(required,economy.SupportRequired.Total);
        Assert.Equal(netP,economy.NetProduction.Total);Assert.Equal(netR,economy.NetResearch.Total);Assert.Equal(growth,economy.GrowthEarned.Total);
    }
}
