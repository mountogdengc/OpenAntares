using System.Collections.Immutable;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Galaxy;

namespace OpenAntares.Simulation.Content;

/// <summary>
/// The effective content definitions and rule parameters for a game. Immutable, and keyed by content
/// ID with ordinal ordering, so enumeration order is deterministic.
/// </summary>
public sealed record ContentSet(
    RulesParameters Rules,
    StartingColonyDefinition StartingColony,
    GalaxyRules Galaxy,
    ImmutableSortedDictionary<string, GalaxySizeDefinition> GalaxySizes,
    ImmutableSortedDictionary<string, PlanetTypeDefinition> PlanetTypes,
    ImmutableSortedDictionary<string, BuildingDefinition> Buildings,
    ImmutableSortedDictionary<string, TechnologyDefinition> Technologies)
{
    public ImmutableSortedDictionary<string, VesselDefinition> Vessels { get; init; } = ImmutableSortedDictionary<string, VesselDefinition>.Empty.WithComparers(System.StringComparer.Ordinal);
    public ExpansionDefinition? Expansion { get; init; }
    public ImmutableSortedDictionary<string, GovernorTemplateDefinition> GovernorTemplates { get; init; } = ImmutableSortedDictionary<string, GovernorTemplateDefinition>.Empty.WithComparers(System.StringComparer.Ordinal);
    public RivalAiDefinition? RivalAi { get; init; }
    public ImmutableSortedDictionary<string, SpeciesDefinition> Species { get; init; } = ImmutableSortedDictionary<string, SpeciesDefinition>.Empty.WithComparers(System.StringComparer.Ordinal);
    public ImmutableSortedDictionary<string, EnvironmentDefinition> Environments { get; init; } = ImmutableSortedDictionary<string, EnvironmentDefinition>.Empty.WithComparers(System.StringComparer.Ordinal);
    public ImmutableSortedDictionary<string, GalaxyRegionDefinition> RegionDefinitions => Galaxy.RegionDefinitions;
}

/// <summary>Numeric rule parameters. Economic quantities are stored hundredths.</summary>
public sealed record RulesParameters(
    long SupportPerPopulation,
    int BaseGrowth,
    int MaxSurplusGrowth,
    int BaseColonyCapacity);

/// <summary>The colony each empire starts the game with.</summary>
public sealed record StartingColonyDefinition(
    string PlanetTypeId,
    int Population,
    Workforce Workforce);

/// <summary>Parameters for generating a new galaxy. Coordinates are whole map units.</summary>
public sealed record GalaxyRules(
    int MinStarDistance,
    int MinPlanetsPerStar,
    int MaxPlanetsPerStar,
    ImmutableArray<string> StarNames)
{
    public ImmutableSortedDictionary<string, VesselDefinition> Vessels { get; init; } = ImmutableSortedDictionary<string, VesselDefinition>.Empty.WithComparers(System.StringComparer.Ordinal);
    public ExpansionDefinition? Expansion { get; init; }
    public ImmutableSortedDictionary<string, GalaxyRegionDefinition> RegionDefinitions { get; init; } = GalaxyRegions.DefaultDefinitions;
}

/// <summary>A galaxy size the new-game screen offers.</summary>
public sealed record GalaxySizeDefinition(
    string Id,
    string Name,
    int StarCount,
    int Width,
    int Height);

/// <summary>
/// A planet type. Per-worker rates are stored hundredths. <see cref="GenerationWeight"/> is the
/// relative chance of the type appearing in a generated galaxy; zero means it is never generated.
/// </summary>
public sealed record PlanetTypeDefinition(
    string Id,
    string Name,
    long SupportPerWorker,
    long ProductionPerWorker,
    long ResearchPerWorker,
    int GenerationWeight)
{
    public ImmutableSortedDictionary<string, int> RegionGenerationWeights { get; init; } =
        ImmutableSortedDictionary<string, int>.Empty.WithComparers(System.StringComparer.Ordinal);

    public bool Colonizable { get; init; }
    public ImmutableSortedDictionary<string, int> EnvironmentGenerationWeights { get; init; } = ImmutableSortedDictionary<string, int>.Empty.WithComparers(System.StringComparer.Ordinal);

    public int WeightForRegion(string regionId) =>
        RegionGenerationWeights.TryGetValue(regionId, out int weight) ? weight : GenerationWeight;
}

/// <summary>A building that a colony can construct once.</summary>
public sealed record BuildingDefinition(
    string Id,
    string Name,
    long Cost,
    ImmutableArray<string> PrerequisiteTechnologyIds,
    ImmutableArray<BuildingEffect> Effects);

/// <summary>A flat modifier granted by a completed building. <see cref="Kind"/> is one of <see cref="EffectKinds"/>.</summary>
public sealed record BuildingEffect(string Kind, long Amount);

/// <summary>The modifier kinds this rules version supports.</summary>
public static class EffectKinds
{
    public const string Support = "support";
    public const string Production = "production";
    public const string Research = "research";
    public const string Capacity = "capacity";
}

/// <summary>A technology an empire can research once.</summary>
public sealed record TechnologyDefinition(
    string Id,
    string Name,
    long Cost,
    ImmutableArray<string> PrerequisiteTechnologyIds);

public sealed record VesselDefinition(string Id, string Name, long Cost, int Speed, int EmbarkPopulation, int MinimumSourcePopulation, bool CanFoundColony)
{
    public bool RequiresFullColony { get; init; }
    public bool FoundsSealedHabitat { get; init; }
}
public sealed record ExpansionDefinition(string StartingVesselId);
