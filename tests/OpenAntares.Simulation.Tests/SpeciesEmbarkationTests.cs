using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class SpeciesEmbarkationTests
{
    private static GameSession Selected(string species,string role,Action<GameState>? setup=null)
    {
        var state=SpeciesFixtures.AtHome(species);setup?.Invoke(state);var session=new GameSession(SpeciesFixtures.Content(),state);
        var outcome=session.Execute(new SetProductionTargetCommand(Fixtures.Empire,state.Turn,Fixtures.HomeColony,new(ProductionKinds.Vessel,role)));Assert.True(outcome.Accepted,outcome.Rejection?.Message);return session;
    }
    private static void Resolve(GameSession session)
    {
        var outcome=session.Execute(new SetReadyCommand(Fixtures.Empire,session.State.Turn,true));Assert.True(outcome.Accepted,outcome.Rejection?.Message);
    }
    [Theory][InlineData("peldra")][InlineData("harl")][InlineData("sulan")][InlineData("wenth")]
    public void OnlyOwnedSpeciesRolesCanBeSelectedWithoutSpendingOrLosingReadiness(string species)
    {
        string[] allowed=species switch{"peldra"=>new[]{"scout","peldra_settler"},"harl"=>new[]{"scout","harl_settler"},"sulan"=>new[]{"scout","settler","harbor_settler"},_=>new[]{"scout","daughter_seed"}};
        foreach(string role in new[]{"scout","peldra_settler","harl_settler","settler","harbor_settler","daughter_seed"})
        {
            var s=SpeciesFixtures.AtHome(species);s.Empires.Add(new EmpireState{Id=new(8),SpeciesId="harl",Controller=ControllerKind.Human});s.NextEntityId=9;s.Empires[0].Ready=true;s.Colonies[0].ProductionReserve=900;
            var before=SpeciesFixtures.Snapshot(s);var outcome=CommandProcessor.Execute(SpeciesFixtures.Content(),s,new SetProductionTargetCommand(Fixtures.Empire,0,Fixtures.HomeColony,new(ProductionKinds.Vessel,role)));
            Assert.Equal(allowed.Contains(role),outcome.Accepted);Assert.Equal(before,SpeciesFixtures.Snapshot(s));Assert.Equal(900,outcome.State.Colonies[0].ProductionReserve);
            if(!outcome.Accepted){Assert.Equal("project_not_available_for_species",outcome.Rejection!.Code);Assert.Same(s,outcome.State);Assert.True(outcome.State.Empires[0].Ready);}
            else Assert.False(outcome.State.Empires[0].Ready);
        }
    }
    [Theory][InlineData("peldra","peldra_settler",3200,2,4)][InlineData("harl","harl_settler",1600,1,3)]
    [InlineData("sulan","settler",2400,2,4)][InlineData("sulan","harbor_settler",3600,2,4)][InlineData("wenth","daughter_seed",2400,3,6)]
    public void ApprovedRolesUseExactCostCargoMinimumAndSpeed(string species,string id,long cost,int cargo,int minimum)
    {
        var role=SpeciesFixtures.Content().Vessels[id];Assert.Equal(cost,role.Cost);Assert.Equal(cargo,role.EmbarkPopulation);Assert.Equal(minimum,role.MinimumSourcePopulation);Assert.Equal(200,role.Speed);
        var session=Selected(species,id);Assert.Equal(0,session.State.Colonies[0].ProductionReserve);Assert.Single(session.State.Vessels);
    }
    [Fact]
    public void PeldraConservesCargoProgressAndShowsTheNextSupportShortage()
    {
        var session=Selected("peldra","peldra_settler",s=>s.Colonies[0].ProductionReserve=2600);Resolve(session);var c=session.State.Colonies[0];
        Assert.Equal(4,c.Population);Assert.Equal(new Workforce(2,1,1),c.Workforce);Assert.Equal(5,c.GrowthProgress);Assert.Equal(0,c.ProductionReserve);Assert.Null(c.ProjectId);
        var ship=Assert.Single(session.State.Vessels,v=>v.RoleId=="peldra_settler");Assert.Equal(2,ship.CarriedPopulation);Assert.Null(ship.PendingMove);Assert.Null(ship.PendingFounding);Assert.Null(ship.Transit);
        var next=session.Forecast().Colonies[0];Assert.Equal(600,next.SupportProduced.Total);Assert.Equal(800,next.SupportRequired.Total);Assert.Equal(450,next.NetProduction.Total);Assert.Equal(450,next.NetResearch.Total);
    }
    [Theory][InlineData(6,3)][InlineData(8,5)]
    public void MatureWenthTransfersExactlyThreeAndRetainsCurrentCapacity(int population,int remaining)
    {
        var session=Selected("wenth","daughter_seed",s=>{var c=s.Colonies[0];c.Population=population;c.Workforce=new(population-2,1,1);c.ProductionReserve=2000;if(population==8)c.CompletedBuildingIds.Add("habitat_extension");});
        Resolve(session);var c=session.State.Colonies[0];Assert.Equal(remaining,c.Population);Assert.Equal(new Workforce(remaining-2,1,1),c.Workforce);Assert.Equal(0,c.ProductionReserve);
        var seed=Assert.Single(session.State.Vessels,v=>v.RoleId=="daughter_seed");Assert.Equal(3,seed.CarriedPopulation);Assert.Equal(population,c.Population+seed.CarriedPopulation);
        var next=session.Forecast().Colonies[0];Assert.Equal(population,next.Growth.Capacity);
        if(population==6){Assert.Equal(300,next.SupportProduced.Total);Assert.Equal(300,next.SupportRequired.Total);Assert.Equal(400,next.NetProduction.Total);Assert.Equal(400,next.NetResearch.Total);Assert.Equal(10,next.GrowthEarned.Total);}
    }
    [Fact]
    public void EveryUnmetConditionIsExplainedAndBankedSeedCanBeCancelled()
    {
        var session=Selected("wenth","daughter_seed",s=>s.Colonies[0].ProductionReserve=2400);var forecast=session.Forecast().Colonies[0];
        Assert.Equal("awaiting_embarkation",forecast.Production.BlockedReasonCode);Assert.True(forecast.Production.Estimate!.Stalled);
        var requirements=JsonNode.Parse(JsonSerializer.Serialize(forecast.Production))!["BlockedRequirements"]!.AsArray();Assert.Equal(3,requirements.Count);
        Assert.Contains(requirements,n=>n!["Code"]!.GetValue<string>()=="minimum_population");Assert.Contains(requirements,n=>n!["Code"]!.GetValue<string>()=="support_workers");Assert.Contains(requirements,n=>n!["Code"]!.GetValue<string>()=="full_colony");
        int nextId=session.State.NextEntityId;Resolve(session);Assert.Equal(nextId,session.State.NextEntityId);Assert.Single(session.State.Vessels);Assert.Equal(2800,session.State.Colonies[0].ProductionReserve);Assert.Equal("daughter_seed",session.State.Colonies[0].ProjectId);
        Assert.True(session.Execute(new SetProductionTargetCommand(Fixtures.Empire,1,Fixtures.HomeColony,new(ProductionKinds.Vessel,"scout"))).Accepted);
        Assert.Equal(2800,session.State.Colonies[0].ProductionReserve);Assert.True(session.Execute(new SetProductionTargetCommand(Fixtures.Empire,1,Fixtures.HomeColony,null)).Accepted);Assert.Equal(2800,session.State.Colonies[0].ProductionReserve);Assert.Single(session.State.Vessels);
    }
    [Fact]
    public void BirthCannotUnlockSeedUntilTheFollowingResolution()
    {
        var session=Selected("wenth","daughter_seed",s=>{var c=s.Colonies[0];c.Population=5;c.Workforce=new(3,1,1);c.GrowthProgress=90;c.ProductionReserve=2400;});int id=session.State.NextEntityId;
        Resolve(session);Assert.Equal(6,session.State.Colonies[0].Population);Assert.Single(session.State.Vessels);Assert.Equal(id,session.State.NextEntityId);Assert.Equal(2800,session.State.Colonies[0].ProductionReserve);
        Resolve(session);Assert.Equal(3,session.State.Colonies[0].Population);Assert.Equal(2,session.State.Vessels.Count);Assert.Equal(800,session.State.Colonies[0].ProductionReserve);
    }
    [Fact]
    public void ExtendedBodyAtSixRemainsBlockedUntilEight()
    {
        var session=Selected("wenth","daughter_seed",s=>{var c=s.Colonies[0];c.Population=6;c.Workforce=new(4,1,1);c.CompletedBuildingIds.Add("habitat_extension");c.ProductionReserve=2400;});
        Assert.Equal("awaiting_embarkation",session.Forecast().Colonies[0].Production.BlockedReasonCode);Resolve(session);Assert.Single(session.State.Vessels);Assert.Equal("daughter_seed",session.State.Colonies[0].ProjectId);
    }
    [Theory][InlineData("settler",6)][InlineData("harbor_settler",9)]
    public void SulanOpeningInvestsSixOrNineResolutions(string role,int turns)
    {
        var session=Selected("sulan",role);for(int t=1;t<=turns;t++){Resolve(session);Assert.Equal(t==turns?2:1,session.State.Vessels.Count);}Assert.Equal(0,session.State.Colonies[0].ProductionReserve);Assert.Equal(2,session.State.Vessels.Single(v=>v.RoleId==role).CarriedPopulation);
    }
    [Fact]
    public void VesselIdOverflowPreservesTheWholePlanningState()
    {
        var session=Selected("wenth","daughter_seed",s=>{s.Colonies[0].Population=6;s.Colonies[0].Workforce=new(4,1,1);s.Colonies[0].ProductionReserve=2400;s.NextEntityId=int.MaxValue;});
        var before=session.State;var text=SpeciesFixtures.Snapshot(before);var outcome=session.Execute(new SetReadyCommand(Fixtures.Empire,0,true));Assert.False(outcome.Accepted);Assert.Same(before,session.State);Assert.Equal(text,SpeciesFixtures.Snapshot(session.State));
    }
}
