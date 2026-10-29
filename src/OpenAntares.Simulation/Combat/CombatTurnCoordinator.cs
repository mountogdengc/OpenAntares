using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public sealed record CombatPreparationResult(GameState State,bool Succeeded);
internal enum CombatResolutionPhase { Preparation,Snapshot,Health,Exchange,LossNormalization,Withdrawal,Movement,Founding,Repair,Observation,OfficerRecovery,Reports,Validation }
public static class CombatTurnCoordinator
{
    public static CombatPreparationResult PrepareHuman(ContentSet content,GameState state,EmpireId owner)
    {
        if(!Rules.HasCombat(state.RulesVersion))return new(state,true);
        try
        {
            if(state.FindEmpire(owner) is not {Controller:ControllerKind.Human}||!CombatReadyQueries.ValidateResponse(content,state,owner).IsEmpty)return new(state,false);
            var prepared=FleetTurnCoordinator.PrepareHuman(content,state,owner);
            if(!prepared.Succeeded||!CombatReadyQueries.ValidateResponse(content,prepared.State,owner).IsEmpty)return new(state,false);
            return new(prepared.State,true);
        }
        catch(Exception error)when(error is OverflowException or ArgumentException or InvalidOperationException){return new(state,false);}
    }
    // Complete discovery/privacy/founding integration belongs to CombatObservations in Task11.
    internal static void RefreshKnownObservations(ContentSet content,GameState candidate,CombatResolutionContext context)
    {
        foreach(var owner in candidate.Empires)
        {
            var known=owner.Combat!.Observations;
            for(int i=0;i<known.Count;i++)
            {
                var observation=known[i];var pocket=candidate.Combat!.Pockets.Single(p=>p.Id==observation.Pocket);
                bool ownReceipt=context.Rounds.Any(r=>r.Before.Owner==owner.Id&&context.Snapshot.Combat!.Encounters.Any(e=>e.Id==r.Encounter&&e.Pocket==pocket.Id));
                if(!ownReceipt&&!ObservationUpdater.IsCurrent(candidate,owner.Id,pocket.Star))continue;
                known[i]=new(pocket.Id,pocket.Star,candidate.Turn,pocket.Cleared,pocket.Sentries.Select(s=>new ObservedSentry(s.Id,s.Profile,s.Hull,content.Combat!.SentryPursuitSpeed)).ToImmutableArray());
            }
        }
    }
}
