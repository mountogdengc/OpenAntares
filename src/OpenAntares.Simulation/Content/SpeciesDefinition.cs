using System.Collections.Immutable;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Content;

public sealed record SpeciesDefinition(string Id, string Name, string Description, string CultureName,
    string PopulationUnitLabel, int StartingPopulation, int BaseCapacity, int? SealedBaseCapacity,
    Workforce StartingWorkforce, long SupportPerPopulation, long SupportPerGrowthPoint,
    int ProductionBasisPoints, int ResearchBasisPoints, int BaseGrowth, int MaxSurplusGrowth,
    ImmutableArray<string> NaturalEnvironmentIds, ImmutableArray<string> AllowedVesselIds, bool AllowsSealedHabitats)
{
    public Fleets.CommandEmbodiment? CommandEmbodiment { get; init; }
    public string? CommandCultureId { get; init; }
}
public sealed record EnvironmentDefinition(string Id, string Name, string Description);
