using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;

namespace OpenAntares.Simulation.Commands;

/// <summary>
/// The result of processing a command. <see cref="State"/> is the state to publish: the previous
/// state, unchanged, when the command was rejected or had no effect.
/// </summary>
public sealed record CommandOutcome(GameState State, CommandRejection? Rejection, bool Changed, TurnReport? Report)
{
    public bool Accepted => Rejection is null;

    internal static CommandOutcome Reject(GameState previous, string code, string message) =>
        new(previous, new CommandRejection(code, message), Changed: false, Report: null);

    internal static CommandOutcome NoChange(GameState previous) =>
        new(previous, Rejection: null, Changed: false, Report: null);
}

/// <summary>Why a command was rejected. <see cref="Code"/> is one of <see cref="RejectionCodes"/>.</summary>
public sealed record CommandRejection(string Code, string Message);

/// <summary>Stable rejection codes. Presentation maps these to localized text.</summary>
public static class RejectionCodes
{
    public const string UnknownEmpire = "unknown_empire";
    public const string StaleTurn = "stale_turn";
    public const string UnknownColony = "unknown_colony";
    public const string NotOwner = "not_owner";
    public const string NegativeWorkforce = "negative_workforce";
    public const string WorkforceSumMismatch = "workforce_sum_mismatch";
    public const string UnknownBuilding = "unknown_building";
    public const string UnknownTechnology = "unknown_technology";
    public const string PrerequisiteNotMet = "prerequisite_not_met";
    public const string AlreadyCompleted = "already_completed";
    public const string AlreadyKnown = "already_known";
    public const string ResolutionFailed = "resolution_failed";
}
