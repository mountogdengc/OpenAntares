using System;
using System.Collections.Generic;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Turns;

/// <summary>The outcome of resolving a turn. On failure, <see cref="State"/> is the unchanged input.</summary>
public sealed record TurnResolution(GameState State, TurnReport? Report, IReadOnlyList<string> Errors)
{
    public bool Succeeded => Errors.Count == 0;
}

/// <summary>What happened during one turn resolution: the turn transition and the economy that was applied.</summary>
public sealed record TurnReport(int FromTurn, int ToTurn, TurnEconomy Economy);

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
        TurnEconomy economy;
        try
        {
            // Phases 1-2: from the snapshot, calculate support, outputs, and growth for every colony in
            // ID order, and research for every empire in ID order. Effects completed this turn apply
            // only from the next snapshot.
            economy = EconomyCalculator.Calculate(content, snapshot);

            // Phase 3: growth, with new workers assigned to support.
            // Phase 4: production and research reserves, at most one completion per colony and empire.
            foreach (ColonyEconomy result in economy.Colonies)
            {
                ColonyState colony = candidate.FindColony(result.Colony)!;
                colony.Population = result.Growth.PopulationAfter;
                colony.Workforce = result.Growth.WorkforceAfter;
                colony.GrowthProgress = result.Growth.ProgressAfter;
                colony.ProductionReserve = result.Production.Ending;
                if (result.Production.CompletedId is { } building)
                {
                    InsertSorted(colony.CompletedBuildingIds, building);
                    colony.ProjectId = null;
                }
            }

            foreach (EmpireEconomy result in economy.Empires)
            {
                EmpireState empire = candidate.FindEmpire(result.Empire)!;
                empire.ResearchReserve = result.Research.Ending;
                if (result.Research.CompletedId is { } technology)
                {
                    InsertSorted(empire.KnownTechnologyIds, technology);
                    empire.ResearchTargetId = null;
                }
            }

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

        return new TurnResolution(candidate, new TurnReport(snapshot.Turn, candidate.Turn, economy), Array.Empty<string>());
    }

    private static void InsertSorted(List<string> ids, string id)
    {
        int index = ids.BinarySearch(id, StringComparer.Ordinal);
        ids.Insert(index < 0 ? ~index : index, id);
    }

    private static TurnResolution Failed(GameState snapshot, string error) =>
        new(snapshot, Report: null, new[] { error });
}
