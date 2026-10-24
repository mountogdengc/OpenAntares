using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Rivals;

namespace OpenAntares.Simulation.Turns;

/// <summary>The outcome of resolving a turn. On failure, <see cref="State"/> is the unchanged input.</summary>
public sealed record TurnResolution(GameState State, TurnReport? Report, IReadOnlyList<string> Errors)
{
    public bool Succeeded => Errors.Count == 0;
}

/// <summary>What happened during one turn resolution: the turn transition and the economy that was applied.</summary>
public sealed record TurnReport(int FromTurn, int ToTurn, TurnEconomy Economy)
{
    public ImmutableArray<ExpansionEvent> ExpansionEvents { get; init; } = ImmutableArray<ExpansionEvent>.Empty;
    public ImmutableArray<EncounterEvent> EncounterEvents { get; init; } = ImmutableArray<EncounterEvent>.Empty;
}

/// <summary>
/// Resolves a turn in explicit phases from an immutable snapshot (see the economy specification's
/// resolution contract). The snapshot is never modified; all changes go to a candidate copy that is
/// published only if every phase and the final validation succeed.
/// </summary>
public static class TurnResolver
{
    public static TurnResolution Resolve(ContentSet content, GameState snapshot) => Resolve(content,snapshot,RivalTurnPlanner.Prepare);

    internal static TurnResolution Resolve(ContentSet content, GameState snapshot, Func<ContentSet,GameState,RivalPlanningOutcome> prepare)
    {
        GameState original = snapshot;
        var inputErrors = GameStateValidator.Validate(content, snapshot, allowAllHumansReady: true);
        if (inputErrors.Count > 0) return new TurnResolution(snapshot, null, inputErrors);
        var events = new List<ExpansionEvent>();
        var encounters = ImmutableArray<EncounterEvent>.Empty;
        GameState candidate;
        TurnEconomy economy;
        try
        {
            if (Rules.HasRivals(snapshot.RulesVersion))
            {
                var planned = prepare(content,snapshot);
                if (!planned.Succeeded) return Failed(original,RivalTurnPlanner.FailureMessage);
                snapshot = planned.State;
            }
            candidate = snapshot.Clone();
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
                if (result.VesselCompletionPlan is { } vesselPlan)
                {
                    var vessel = new VesselState { Id = new(candidate.AllocateId()), EmpireId = colony.EmpireId, RoleId = vesselPlan.RoleId, CarriedPopulation = vesselPlan.Cargo, StationaryStarId = vesselPlan.Star };
                    candidate.Vessels.Add(vessel);
                    if (result.EmbarkOutcome is { } embark) { colony.Population = embark.PopulationAfter; colony.Workforce = embark.WorkforceAfter; }
                    colony.ProjectId = null; colony.ProjectKind = ProductionKinds.Building;
                    events.Add(new("vessel_completed", colony.EmpireId, vessel.Id, vesselPlan.Star, colony.PlanetId, colony.Id));
                }
                else if (result.Production.CompletedId is { } building)
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

            if (Rules.HasExpansion(snapshot.RulesVersion))
            {
                MovementResolver.Apply(content, snapshot, candidate, events);
                Colonization.ColonizationResolver.Apply(content, snapshot, candidate, events);
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
            ObservationUpdater.Refresh(candidate, candidate.Turn);
            encounters = ContactUpdater.Refresh(snapshot, candidate);
        }
        catch (OverflowException exception)
        {
            return Failed(original, Rules.HasRivals(original.RulesVersion) ? RivalTurnPlanner.FailureMessage : "Arithmetic overflow during resolution: " + exception.Message);
        }
        catch (Exception error) when (Rules.HasRivals(original.RulesVersion) && error is (ArgumentException or InvalidOperationException or KeyNotFoundException))
        { return Failed(original,RivalTurnPlanner.FailureMessage); }

        IReadOnlyList<string> errors = GameStateValidator.Validate(content, candidate);
        if (errors.Count > 0)
        {
            return Rules.HasRivals(original.RulesVersion) ? Failed(original,RivalTurnPlanner.FailureMessage) : new TurnResolution(original, Report: null, errors);
        }

        return new TurnResolution(candidate, new TurnReport(snapshot.Turn, candidate.Turn, economy) { ExpansionEvents = events.ToImmutableArray(), EncounterEvents = encounters }, Array.Empty<string>());
    }

    private static void InsertSorted(List<string> ids, string id)
    {
        int index = ids.BinarySearch(id, StringComparer.Ordinal);
        ids.Insert(index < 0 ? ~index : index, id);
    }

    private static TurnResolution Failed(GameState snapshot, string error) =>
        new(snapshot, Report: null, new[] { error });
}
