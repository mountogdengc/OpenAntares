using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Rivals;

namespace OpenAntares.Simulation.Turns;

internal enum CrisisResolutionPhase { Economy,Health,Transport,Finalize,Validation }

/// <summary>The outcome of resolving a turn. On failure, <see cref="State"/> is the unchanged input.</summary>
public sealed record TurnResolution(GameState State, TurnReport? Report, IReadOnlyList<string> Errors)
{
    public bool Succeeded => Errors.Count == 0;
}

/// <summary>What happened during one turn resolution: the turn transition and the economy that was applied.</summary>
public sealed record TurnReport(int FromTurn, int ToTurn, TurnEconomy Economy)
{
    public ImmutableArray<Crises.CrisisEvent> CrisisEvents { get; init; } = ImmutableArray<Crises.CrisisEvent>.Empty;
    public ImmutableDictionary<ColonyId,Crises.CrisisLocation> ColonyLocations { get; init; } = ImmutableDictionary<ColonyId,Crises.CrisisLocation>.Empty;
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
    public static TurnResolution Resolve(ContentSet content, GameState snapshot) => ResolveCore(content,snapshot,RivalTurnPlanner.Prepare,null);

    internal static TurnResolution Resolve(ContentSet content,GameState snapshot,Action<CrisisResolutionPhase> hook) => ResolveCore(content,snapshot,RivalTurnPlanner.Prepare,hook);

    internal static TurnResolution Resolve(ContentSet content, GameState snapshot, Func<ContentSet,GameState,RivalPlanningOutcome> prepare)
        => ResolveCore(content,snapshot,prepare,null);

    private static TurnResolution ResolveCore(ContentSet content,GameState snapshot,Func<ContentSet,GameState,RivalPlanningOutcome> prepare,Action<CrisisResolutionPhase>? hook)
    {
        GameState original = snapshot;
        var inputErrors = GameStateValidator.Validate(content, snapshot, allowAllHumansReady: true);
        if (inputErrors.Count > 0) return Rules.HasCrises(snapshot.RulesVersion)?Failed(snapshot,RivalTurnPlanner.FailureMessage):new TurnResolution(snapshot, null, inputErrors);
        var events = new List<ExpansionEvent>();
        var encounters = ImmutableArray<EncounterEvent>.Empty;
        GameState candidate;
        TurnEconomy economy;
        TurnReport? crisisReport=null;
        IReadOnlyList<string>? crisisValidation=null;
        try
        {
            if(Rules.HasScouting(snapshot.RulesVersion))
            {
                foreach(var human in snapshot.Empires.Where(e=>e.Controller==ControllerKind.Human).OrderBy(e=>e.Id).ToArray())
                {
                    if(!Scouting.ScoutReadyQueries.HasConsent(content,snapshot,human.Id))return Failed(original,RivalTurnPlanner.FailureMessage);
                    if(human.Ready)
                    {
                        if(Scouting.ScoutReadyQueries.ValidateResponse(content,snapshot,human.Id).Length!=0)return Failed(original,RivalTurnPlanner.FailureMessage);
                        continue;
                    }
                    var health=CrisisTurnCoordinator.Prepare(content,snapshot,human.Id);if(!health.Succeeded)return Failed(original,RivalTurnPlanner.FailureMessage);
                    var governor=Development.GovernorTurnCoordinator.Prepare(content,health.State,human.Id);if(!governor.Succeeded)return Failed(original,RivalTurnPlanner.FailureMessage);
                    var scouting=Scouting.ScoutTurnCoordinator.Prepare(content,governor.State,human.Id);if(!scouting.Succeeded)return Failed(original,RivalTurnPlanner.FailureMessage);
                    snapshot=scouting.State;
                }
            }
            if(Rules.HasCrises(snapshot.RulesVersion))
            {
                var crisisPreparation=CrisisTurnCoordinator.PrepareAi(content,snapshot);
                if(!crisisPreparation.Succeeded)return Failed(original,RivalTurnPlanner.FailureMessage);
                snapshot=crisisPreparation.State;
            }
            if (Rules.HasRivals(snapshot.RulesVersion))
            {
                var planned = prepare(content,snapshot);
                if (!planned.Succeeded) return Failed(original,RivalTurnPlanner.FailureMessage);
                snapshot = planned.State;
            }
            candidate = snapshot.Clone();
            var crisis=Rules.HasCrises(snapshot.RulesVersion)?CrisisResolutionContext.Capture(content,snapshot):null;
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
                    Scouting.ScoutInitializer.InitializeVessel(content,candidate.RulesVersion,vessel);
                    if(crisis is not null)CrisisTransport.TagCompletion(content,snapshot,candidate,crisis,colony.Id,vessel);
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

            if(crisis is not null)
            {
                hook?.Invoke(CrisisResolutionPhase.Economy);
                CrisisProgression.Apply(content,snapshot,candidate,economy,crisis);
                hook?.Invoke(CrisisResolutionPhase.Health);
                CrisisTransport.PrepareDepartures(content,snapshot,candidate,crisis);
            }
            if (Rules.HasExpansion(snapshot.RulesVersion))
            {
                MovementResolver.Apply(content, snapshot, candidate, events,crisis);
                if(crisis is not null)CrisisTransport.HandleArrivals(content,snapshot,candidate,crisis);
                Colonization.ColonizationResolver.Apply(content, snapshot, candidate, events,crisis);
            }
            if(crisis is not null)
            {
                CrisisTransport.ApplyExposures(content,snapshot,candidate,crisis);
                hook?.Invoke(CrisisResolutionPhase.Transport);
                CrisisOnset.Apply(content,candidate,crisis);
                CrisisReports.CloseFinishedEpisodes(content,candidate,crisis);
                CrisisReports.Finalize(snapshot,candidate,crisis);
            }

            // Phase 5: reset readiness, validate, and advance the turn once.
            foreach (EmpireState empire in candidate.Empires)
            {
                if (empire.Controller == ControllerKind.Human)
                {
                    empire.Ready = false;
                }
            }

            Development.DevelopmentCompletion.Apply(content, candidate, new TurnReport(snapshot.Turn, checked(snapshot.Turn + 1), economy));
            candidate.Turn = checked(candidate.Turn + 1);
            ObservationUpdater.Refresh(candidate, candidate.Turn);
            encounters = ContactUpdater.Refresh(snapshot, candidate);
            if(crisis is not null)
            {
                CrisisReports.TrimRetiredLocations(candidate);
                hook?.Invoke(CrisisResolutionPhase.Finalize);
                crisisValidation=GameStateValidator.Validate(content,candidate);
                if(crisisValidation.Count>0)return Failed(original,RivalTurnPlanner.FailureMessage);
                hook?.Invoke(CrisisResolutionPhase.Validation);
                var locations=crisis.Locations.SetItems(candidate.Colonies.Where(c=>!crisis.Locations.ContainsKey(c.Id)).Select(c=>new KeyValuePair<ColonyId,CrisisLocation>(c.Id,CrisisQueries.Location(candidate,c))));
                crisisReport=new(snapshot.Turn,candidate.Turn,economy){ExpansionEvents=events.ToImmutableArray(),EncounterEvents=encounters,CrisisEvents=crisis.Events.ToImmutableArray(),ColonyLocations=locations};
            }
        }
        catch (OverflowException exception)
        {
            return Failed(original, Rules.HasRivals(original.RulesVersion) ? RivalTurnPlanner.FailureMessage : "Arithmetic overflow during resolution: " + exception.Message);
        }
        catch (Exception error) when (Rules.HasRivals(original.RulesVersion) && error is (ArgumentException or InvalidOperationException or KeyNotFoundException))
        { return Failed(original,RivalTurnPlanner.FailureMessage); }

        IReadOnlyList<string> errors = crisisValidation??GameStateValidator.Validate(content, candidate);
        if (errors.Count > 0)
        {
            return Rules.HasRivals(original.RulesVersion) ? Failed(original,RivalTurnPlanner.FailureMessage) : new TurnResolution(original, Report: null, errors);
        }

        return new TurnResolution(candidate, crisisReport??new TurnReport(snapshot.Turn, candidate.Turn, economy) { ExpansionEvents = events.ToImmutableArray(), EncounterEvents = encounters }, Array.Empty<string>());
    }

    private static void InsertSorted(List<string> ids, string id)
    {
        int index = ids.BinarySearch(id, StringComparer.Ordinal);
        ids.Insert(index < 0 ? ~index : index, id);
    }

    private static TurnResolution Failed(GameState snapshot, string error) =>
        new(snapshot, Report: null, new[] { error });
}
