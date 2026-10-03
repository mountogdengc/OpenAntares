using System;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using Xunit;
using static OpenAntares.Simulation.Tests.Fixtures;

namespace OpenAntares.Simulation.Tests;

public class GameStateValidatorTests
{
    private readonly ContentSet _content = PrototypeContent();

    [Fact]
    public void StartingStateIsValid()
    {
        Assert.Empty(GameStateValidator.Validate(_content, StartingState()));
    }

    [Fact]
    public void ColonyAtCapacityWithZeroGrowthIsValid()
    {
        GameState state = StartingState();
        state.Colonies[0].Population = 10;
        state.Colonies[0].Workforce = new Workforce(4, 3, 3);

        Assert.Empty(GameStateValidator.Validate(_content, state));
    }

    [Fact]
    public void CapacityBuildingRaisesTheLimit()
    {
        GameState state = StartingState();
        ColonyState colony = state.Colonies[0];
        colony.CompletedBuildingIds.Add("habitat_extension");
        colony.Population = 12;
        colony.Workforce = new Workforce(4, 4, 4);

        Assert.Empty(GameStateValidator.Validate(_content, state));
    }

    public static TheoryData<string, Action<GameState>> InvalidStates => new()
    {
        { "population 0 is below 1", s => { s.Colonies[0].Population = 0; s.Colonies[0].Workforce = new Workforce(0, 0, 0); } },
        { "exceeds capacity", s => { s.Colonies[0].Population = 11; s.Colonies[0].Workforce = new Workforce(5, 3, 3); } },
        { "does not sum to population", s => s.Colonies[0].Workforce = new Workforce(1, 1, 1) },
        { "negative count", s => s.Colonies[0].Workforce = new Workforce(-1, 4, 3) },
        { "outside 0..99", s => s.Colonies[0].GrowthProgress = 100 },
        { "must be 0 at capacity", s => { s.Colonies[0].Population = 10; s.Colonies[0].Workforce = new Workforce(4, 3, 3); s.Colonies[0].GrowthProgress = 5; } },
        { "production reserve -1 is negative", s => s.Colonies[0].ProductionReserve = -1 },
        { "research reserve -1 is negative", s => s.Empires[0].ResearchReserve = -1 },
        { "planet type 'gas_giant' is not defined", s => s.Planets[0].PlanetTypeId = "gas_giant" },
        { "'no_such_building' is not defined", s => s.Colonies[0].CompletedBuildingIds.Add("no_such_building") },
        { "must be unique and sorted", s => s.Colonies[0].CompletedBuildingIds.AddRange(new[] { "fabrication_hall", "fabrication_hall" }) },
        { "project 'fabrication_hall' is already completed", s => { s.Colonies[0].CompletedBuildingIds.Add("fabrication_hall"); s.Colonies[0].ProjectId = "fabrication_hall"; } },
        { "research target 'cultivation_methods' is already known", s => { s.Empires[0].KnownTechnologyIds.Add("cultivation_methods"); s.Empires[0].ResearchTargetId = "cultivation_methods"; } },
        { "research target 'warp_drive' is not defined", s => s.Empires[0].ResearchTargetId = "warp_drive" },
        { "at least one human-controlled empire", s => s.Empires[0].Controller = ControllerKind.Ai },
        { "Every human-controlled empire is ready", s => s.Empires[0].Ready = true },
        { "planet planet:50 does not exist", s => s.Colonies[0].PlanetId = new PlanetId(50) },
        { "owning empire empire:50 does not exist", s => s.Colonies[0].EmpireId = new EmpireId(50) },
        { "outside the allocated range", s => s.NextEntityId = 3 },
        { "used by more than one entity", s => s.Planets[0].Id = new PlanetId(1) },
        { "Rules version 0 is not supported", s => s.RulesVersion = 0 },
        { "planet:2: star star:60 does not exist", s => s.Planets[0].StarId = new StarId(60) },
        { "planet:2: orbit 0 is below 1", s => s.Planets[0].Orbit = 0 },
        { "star:4: name is empty", s => s.Stars[0].Name = "" },
        { "star:4 has more than one planet in orbit 1", s => { s.Planets.Add(new PlanetState { Id = new PlanetId(s.AllocateId()), StarId = HomeStar, Orbit = 1, PlanetTypeId = "forge_world" }); } },
        { "increment must be odd", s => s.Random.Increment = 2 },
    };

    [Theory]
    [MemberData(nameof(InvalidStates))]
    public void InvalidStateIsReported(string expectedFragment, Action<GameState> corrupt)
    {
        GameState state = StartingState();
        corrupt(state);

        var errors = GameStateValidator.Validate(_content, state);

        Assert.Contains(errors, error => error.Contains(expectedFragment, StringComparison.Ordinal));
    }
}
