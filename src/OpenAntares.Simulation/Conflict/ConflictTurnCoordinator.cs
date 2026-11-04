using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
internal enum ConflictResolutionPhase { Planning,Snapshot,Relations,Payments,Economy,Health,Matching,Departure,Exchange,Aftermath,Movement,Repair,Publication,Observation,Reports,Validation }
internal static class ConflictTurnCoordinator
{
    internal static ImmutableArray<string> ValidateResponse(GameState state,EmpireId owner)=>!Rules.HasConflict(state.RulesVersion)?ImmutableArray<string>.Empty:state.Conflict!.Encounters.Where(e=>e.Phase!=ConflictPhase.Completed&&(e.LowSide.Owner==owner&&e.LowSide.NeedsResponse||e.HighSide.Owner==owner&&e.HighSide.NeedsResponse)).Select(_=>"conflict_response_required").Distinct().ToImmutableArray();
    internal static void ResolveBattles(ContentSet content,GameState snapshot,GameState candidate,FleetResolutionContext fleet,CombatResolutionContext combat,ConflictResolutionContext conflict,Action<ConflictResolutionPhase>? hook)
    {
        ConflictMatching.Resolve(content,snapshot,candidate,conflict);combat.ReserveEmpireParticipants(conflict);hook?.Invoke(ConflictResolutionPhase.Matching);
        ConflictAftermath.InterruptRefits(content,snapshot,candidate,conflict);
        ConflictWithdrawal.Resolve(content,snapshot,candidate,conflict);hook?.Invoke(ConflictResolutionPhase.Departure);
        ConflictExchange.Apply(content,snapshot,candidate,conflict);hook?.Invoke(ConflictResolutionPhase.Exchange);
        ConflictAftermath.Apply(content,snapshot,candidate,fleet,conflict);hook?.Invoke(ConflictResolutionPhase.Aftermath);
    }
}
