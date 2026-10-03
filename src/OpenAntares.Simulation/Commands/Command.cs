using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Commands;

/// <summary>
/// A serializable description of intent, issued by an empire for a specific turn. Every change to
/// game state goes through <see cref="CommandProcessor"/>, whoever issues it.
/// </summary>
public abstract record Command(EmpireId Issuer, int ExpectedTurn);

/// <summary>Assign an owned colony's population. Counts must be nonnegative and sum to population.</summary>
public sealed record SetWorkforceCommand(EmpireId Issuer, int ExpectedTurn, ColonyId Colony, Workforce Workforce)
    : Command(Issuer, ExpectedTurn);

/// <summary>Select a building project for an owned colony, or clear it with <c>null</c>.</summary>
public sealed record SetProjectCommand(EmpireId Issuer, int ExpectedTurn, ColonyId Colony, string? BuildingId)
    : Command(Issuer, ExpectedTurn);

/// <summary>Select the issuing empire's research target, or clear it with <c>null</c>.</summary>
public sealed record SetResearchTargetCommand(EmpireId Issuer, int ExpectedTurn, string? TechnologyId)
    : Command(Issuer, ExpectedTurn);

/// <summary>Mark the issuing empire ready for turn resolution, or withdraw readiness.</summary>
public sealed record SetReadyCommand(EmpireId Issuer, int ExpectedTurn, bool Ready)
    : Command(Issuer, ExpectedTurn);
