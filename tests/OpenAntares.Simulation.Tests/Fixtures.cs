using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Random;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Tests;

/// <summary>The prototype content and starting state from docs/FIRST_PLAYABLE_ECONOMY.md.</summary>
internal static class Fixtures
{
    public static readonly EmpireId Empire = new(1);
    public static readonly PlanetId HomePlanet = new(2);
    public static readonly ColonyId HomeColony = new(3);

    public static ContentSet PrototypeContent() => new(
        new RulesParameters(SupportPerPopulation: 100, BaseGrowth: 10, MaxSurplusGrowth: 40, BaseColonyCapacity: 10),
        PlanetTypes: Sorted(
            new PlanetTypeDefinition("verdant_world", "Verdant World", 300, 200, 200),
            new PlanetTypeDefinition("forge_world", "Forge World", 200, 300, 200),
            new PlanetTypeDefinition("crystal_world", "Crystal World", 200, 200, 300),
            new PlanetTypeDefinition("barren_rock", "Barren Rock", 100, 200, 100)),
        Buildings: Sorted(
            Building("fabrication_hall", "Fabrication Hall", 1200, null, EffectKinds.Production, 100),
            Building("cultivation_hub", "Cultivation Hub", 1000, "cultivation_methods", EffectKinds.Support, 200),
            Building("analysis_lab", "Analysis Lab", 1200, "measurement_methods", EffectKinds.Research, 100),
            Building("habitat_extension", "Habitat Extension", 1600, "habitat_methods", EffectKinds.Capacity, 2)),
        Technologies: Sorted(
            Technology("cultivation_methods", "Cultivation Methods", 1200),
            Technology("measurement_methods", "Measurement Methods", 1200),
            Technology("habitat_methods", "Habitat Methods", 1600)));

    /// <summary>Turn 0: one human empire with one colony of population 6 on a verdant world, assigned 2/2/2.</summary>
    public static GameState StartingState() => new()
    {
        Turn = 0,
        NextEntityId = 4,
        Random = Pcg32.FromSeed(12345, 1),
        Empires = { new EmpireState { Id = Empire, Controller = ControllerKind.Human } },
        Planets = { new PlanetState { Id = HomePlanet, PlanetTypeId = "verdant_world" } },
        Colonies =
        {
            new ColonyState
            {
                Id = HomeColony,
                PlanetId = HomePlanet,
                EmpireId = Empire,
                Population = 6,
                Workforce = new Workforce(2, 2, 2),
            },
        },
    };

    /// <summary>Adds an empire with its own colony and returns its ID.</summary>
    public static EmpireId AddEmpire(GameState state, ControllerKind controller)
    {
        var empire = new EmpireId(state.AllocateId());
        var planet = new PlanetId(state.AllocateId());
        var colony = new ColonyId(state.AllocateId());
        state.Empires.Add(new EmpireState { Id = empire, Controller = controller });
        state.Planets.Add(new PlanetState { Id = planet, PlanetTypeId = "forge_world" });
        state.Colonies.Add(new ColonyState
        {
            Id = colony,
            PlanetId = planet,
            EmpireId = empire,
            Population = 3,
            Workforce = new Workforce(1, 1, 1),
        });
        return empire;
    }

    /// <summary>A canonical text form of a state, for exact comparisons.</summary>
    public static string Snapshot(GameState state) => JsonSerializer.Serialize(state);

    private static ImmutableSortedDictionary<string, T> Sorted<T>(params T[] items)
        where T : notnull =>
        items.ToImmutableSortedDictionary(item => (string)item.GetType().GetProperty("Id")!.GetValue(item)!, item => item, System.StringComparer.Ordinal);

    private static BuildingDefinition Building(string id, string name, long cost, string? prerequisite, string effectKind, long amount) =>
        new(id, name, cost,
            prerequisite is null ? ImmutableArray<string>.Empty : ImmutableArray.Create(prerequisite),
            ImmutableArray.Create(new BuildingEffect(effectKind, amount)));

    private static TechnologyDefinition Technology(string id, string name, long cost, params string[] prerequisites) =>
        new(id, name, cost, prerequisites.ToImmutableArray());
}
