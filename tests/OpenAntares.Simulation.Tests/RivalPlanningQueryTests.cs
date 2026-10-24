using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class RivalPlanningQueryTests
{
    [Theory]
    [InlineData("peldra",5,1,0,1500,1200,600,0,20)]
    [InlineData("harl",3,2,1,900,600,400,200,70)]
    [InlineData("sulan",3,2,1,900,600,400,200,40)]
    [InlineData("wenth",3,1,0,900,400,400,0,50)]
    public void QueriesMatchAuthoritativeOwnCalculators(string species,int support,int production,int research,long produced,long required,long netP,long netR,long growth)
    {
        var content = RivalFixtures.Content(); var state = RivalFixtures.AtHome(species); var ai = state.Empires[1];
        var colony = state.Colonies.Single(c => c.EmpireId == ai.Id); var workforce = new Workforce(support,production,research);
        var context = RivalPlanningQueries.Create(content,state,ai.Id); string before = JsonSerializer.Serialize(state);
        var result = context.Forecast(colony.Id,workforce,new(ProductionKinds.Building,"fabrication_hall")); Assert.NotNull(result);
        Assert.Equal(produced,result.SupportProduced.Total); Assert.Equal(required,result.SupportRequired.Total);
        Assert.Equal(netP,result.NetProduction.Total); Assert.Equal(netR,result.NetResearch.Total); Assert.Equal(growth,result.GrowthEarned.Total);
        var reference = state.Clone(); var own = reference.FindColony(colony.Id)!; own.Workforce = workforce; own.ProjectKind = ProductionKinds.Building; own.ProjectId = "fabrication_hall";
        Assert.Equal(JsonSerializer.Serialize(EconomyCalculator.Calculate(content,reference).Colony(colony.Id)),JsonSerializer.Serialize(result));
        Assert.Equal(before,JsonSerializer.Serialize(state));
        var built = context.Forecast(colony.Id,workforce,null,"fabrication_hall"); Assert.NotNull(built);
        Assert.Equal(netP+100,built.NetProduction.Total); Assert.Empty(colony.CompletedBuildingIds);
    }

    [Fact]
    public void UnknownAndUnseenTargetsReturnSameUnavailableResult()
    {
        var state = RivalFixtures.AtHome("peldra"); var context = RivalPlanningQueries.Create(RivalFixtures.Content(),state,state.Empires[1].Id);
        Assert.Null(context.PreviewSettlement(state.Colonies[0].PlanetId,"peldra_settler"));
        Assert.Null(context.PreviewSettlement(new(99999),"peldra_settler"));
        Assert.Null(context.Forecast(state.Colonies[0].Id,state.Colonies[0].Workforce,null));
        Assert.Null(context.TravelTurns(new(99999),state.Stars[0].Id,"scout"));
        Assert.Equal(0,context.TravelTurns(state.Stars[1].Id,state.Stars[1].Id,"scout"));
        Assert.Equal(1,context.TravelTurns(state.Stars[1].Id,state.Stars[0].Id,"scout"));
    }

    [Fact]
    public void StaleEmptyWorldPreviewIgnoresCurrentHiddenOccupancy()
    {
        var content = RivalFixtures.Content(); var state = RivalFixtures.AtHome("sulan"); var ai = state.Empires[1]; var world = state.Planets[0];
        ai.Surveys.Add(new SystemObservation { StarId = world.StarId, LastObservedTurn = 0, Planets = { new(world.Id,world.Orbit,world.PlanetTypeId,null,null) { EnvironmentId = world.EnvironmentId } } });
        ai.Surveys.Sort((a,b)=>a.StarId.CompareTo(b.StarId)); Assert.Empty(GameStateValidator.Validate(content,state));
        var result = RivalPlanningQueries.Create(content,state,ai.Id).PreviewSettlement(world.Id,"settler"); Assert.NotNull(result);
        Assert.Null(result.RejectionCode); Assert.Equal(2,result.Population); Assert.Equal(12,result.Capacity); Assert.Equal("native",result.HabitatMode);
        Assert.NotNull(result.Economy); Assert.Equal(600,result.Economy.SupportProduced.Total); Assert.Equal(200,result.Economy.SupportRequired.Total);
        Assert.Null(RivalPlanningQueries.Create(content,state,ai.Id).PreviewSettlement(world.Id,"peldra_settler"));
    }

    [Fact]
    public void HiddenStateAndGlobalCountersCannotChangePlanningFacts()
    {
        var content = RivalFixtures.Content(); var state = GalaxyGenerator.CreateNewGame(content,new("small",42,4,"harl",1,"peldra")).State!;
        var ai = state.Empires[1]; var before = RivalPlanningQueries.Create(content,state,ai.Id);
        Assert.Null(before.View.Galaxy.Seed);
        var changed = state.Clone(); changed.Random.State ^= 123UL; changed.NextEntityId += 100; changed.GalaxySeed = 999;
        changed.Empires[0].SpeciesId = "wenth"; changed.Empires[0].ResearchReserve = long.MaxValue; changed.Empires[0].ResearchTargetId = "measurement_methods";
        var hidden = changed.FindPlanet(changed.Colonies[0].PlanetId)!; hidden.EnvironmentId = "dry";
        var after = RivalPlanningQueries.Create(content,changed,ai.Id);
        Assert.Equal(JsonSerializer.Serialize(before.View),JsonSerializer.Serialize(after.View));
        var colony = state.Colonies.Single(c=>c.EmpireId==ai.Id);
        Assert.Equal(JsonSerializer.Serialize(before.Forecast(colony.Id,new(5,1,0),null)),JsonSerializer.Serialize(after.Forecast(colony.Id,new(5,1,0),null)));
    }

    [Fact]
    public void InvalidAndOverflowingHypothesesAreUnavailableWithoutMutation()
    {
        var content = RivalFixtures.Content(); var state = RivalFixtures.AtHome("harl"); var ai = state.Empires[1]; var colony = state.Colonies.Single(c=>c.EmpireId==ai.Id);
        colony.Workforce = new(5,0,1); colony.ProductionReserve = long.MaxValue;
        var context = RivalPlanningQueries.Create(content,state,ai.Id); string before = JsonSerializer.Serialize(state);
        Assert.Null(context.Forecast(colony.Id,new(4,2,0),null));
        Assert.Null(context.Forecast(colony.Id,new(-1,6,1),null));
        Assert.Null(context.Forecast(colony.Id,new(6,6,6),null));
        Assert.Null(context.Forecast(colony.Id,new(5,0,1),new("unsupported","fabrication_hall")));
        Assert.Null(context.Forecast(colony.Id,new(5,0,1),null,"cultivation_hub"));
        Assert.Equal(before,JsonSerializer.Serialize(state));
    }

    [Fact]
    public void EditedCargoCostAndPublicLabelsUseSharedCalculators()
    {
        var content = RivalFixtures.Content(); var role = content.Vessels["harl_settler"] with { Name = "Changed ark", Cost = 1800, EmbarkPopulation = 2, MinimumSourcePopulation = 4 };
        var renamed = content.Species["harl"] with { Id = "renamed_harl", Name = "Renamed species", ProductionBasisPoints = 15000 };
        content = content with { Vessels = content.Vessels.SetItem(role.Id,role), Species = content.Species.Add(renamed.Id,renamed) };
        var state = RivalFixtures.AtHome("harl"); var ai = state.Empires[1]; var colony = state.Colonies.Single(c=>c.EmpireId==ai.Id); colony.ProductionReserve = 1600;
        ai.SpeciesId = renamed.Id;
        var context = RivalPlanningQueries.Create(content,state,ai.Id);
        Assert.Equal("renamed_harl",context.View.SpeciesId);
        var result = context.Forecast(colony.Id,new(3,2,1),new(ProductionKinds.Vessel,role.Id)); Assert.NotNull(result);
        Assert.Equal(400,result.Production.Ending); Assert.Equal(2,result.VesselCompletionPlan!.Cargo); Assert.Equal(4,result.EmbarkOutcome!.PopulationAfter);
        Assert.Equal(6,colony.Population); Assert.Equal(1600,colony.ProductionReserve);
    }
}
