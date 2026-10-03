using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Economy;

/// <summary>
/// The economic outcome of resolving the current planning state. The same value serves as the
/// forecast shown during planning and as the record of what resolution applied.
/// </summary>
public sealed record TurnEconomy(ImmutableArray<ColonyEconomy> Colonies, ImmutableArray<EmpireEconomy> Empires)
{
    public ColonyEconomy Colony(ColonyId id) => Colonies.Single(c => c.Colony == id);
    public EmpireEconomy Empire(EmpireId id) => Empires.Single(e => e.Empire == id);
}

/// <summary>One colony's outputs, growth, and production reserve for a turn.</summary>
public sealed record ColonyEconomy(
    ColonyId Colony,
    EmpireId Empire,
    string PlanetTypeId,
    Quantity SupportProduced,
    Quantity SupportRequired,
    long SupportSurplus,
    long SupportDeficit,
    long UnusedSupport,
    Quantity GrossProduction,
    Quantity GrossResearch,
    Quantity NetProduction,
    Quantity NetResearch,
    Quantity GrowthEarned,
    GrowthOutcome Growth,
    ReserveOutcome Production);

/// <summary>An empire's combined research and research reserve for a turn.</summary>
public sealed record EmpireEconomy(EmpireId Empire, Quantity NetResearch, ReserveOutcome Research);

/// <summary>
/// How growth changes population. <see cref="DiscardedGrowth"/> is growth lost because the colony
/// reached capacity; capacity prevents saving births for a later upgrade.
/// </summary>
public sealed record GrowthOutcome(
    int Capacity,
    int PopulationBefore,
    int PopulationAfter,
    int Births,
    int ProgressBefore,
    int ProgressAfter,
    int DiscardedGrowth,
    Workforce WorkforceAfter)
{
    public bool AtCapacity => PopulationAfter >= Capacity;
}

/// <summary>
/// A production or research reserve for one turn: <c>Ending = Starting + Change.Total</c>, where the
/// change is the output earned minus any completed target's cost.
/// </summary>
public sealed record ReserveOutcome(
    long Starting,
    Quantity Change,
    long Ending,
    string? TargetId,
    long? TargetCost,
    string? CompletedId,
    TurnEstimate? Estimate);

/// <summary>Turns until completion assuming current output continues. <c>null</c> turns means stalled.</summary>
public sealed record TurnEstimate(long? Turns)
{
    public bool Stalled => Turns is null;
}
