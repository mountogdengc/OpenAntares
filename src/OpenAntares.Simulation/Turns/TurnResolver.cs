using System;
using System.Collections.Generic;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Turns;

/// <summary>The outcome of resolving a turn. On failure, <see cref="State"/> is the unchanged input.</summary>
public sealed record TurnResolution(GameState State, TurnReport? Report, IReadOnlyList<string> Errors)
{
    public bool Succeeded => Errors.Count == 0;
}

/// <summary>What happened during one turn resolution.</summary>
public sealed record TurnReport(int FromTurn, int ToTurn);

/// <summary>
/// Resolves a turn in explicit phases from an immutable snapshot (see the economy specification's
/// resolution contract). The snapshot is never modified; all changes go to a candidate copy that is
/// published only if every phase and the final validation succeed.
/// </summary>
public static class TurnResolver
{
    public static TurnResolution Resolve(ContentSet content, GameState snapshot)
    {
        GameState candidate = snapshot.Clone();
        try
        {
            // Phases 1-4 (snapshot capture, colony economy, growth, production and research) are
            // added with the economy implementation.

            // Phase 5: reset readiness, validate, and advance the turn once.
            foreach (EmpireState empire in candidate.Empires)
            {
                if (empire.Controller == ControllerKind.Human)
                {
                    empire.Ready = false;
                }
            }

            candidate.Turn = checked(candidate.Turn + 1);
        }
        catch (OverflowException exception)
        {
            return Failed(snapshot, "Arithmetic overflow during resolution: " + exception.Message);
        }

        IReadOnlyList<string> errors = GameStateValidator.Validate(content, candidate);
        if (errors.Count > 0)
        {
            return new TurnResolution(snapshot, Report: null, errors);
        }

        return new TurnResolution(candidate, new TurnReport(snapshot.Turn, candidate.Turn), Array.Empty<string>());
    }

    private static TurnResolution Failed(GameState snapshot, string error) =>
        new(snapshot, Report: null, new[] { error });
}
