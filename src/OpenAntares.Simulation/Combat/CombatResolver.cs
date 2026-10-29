using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
internal static class CombatResolver
{
    internal static void ResolveExchanges(ContentSet content,GameState snapshot,GameState candidate,CombatResolutionContext context)
    {
        if(context.ExchangesResolved||context.FromTurn!=snapshot.Turn)throw new InvalidOperationException("Combat exchange phase already applied or snapshot changed.");
        context.ExchangesResolved=true;
        if(!Rules.HasCombat(snapshot.RulesVersion))return;
        foreach(var id in context.Encounters)
        {
            var e=candidate.Combat!.Encounters.SingleOrDefault(e=>e.Id==id);var before=snapshot.Combat!.Encounters.Single(e=>e.Id==id);
            if(e is null||e.LastResolvedTurn==context.FromTurn)continue;
            var missing=before.Members.Where(id=>candidate.FindVessel(id) is null).OrderBy(id=>id).ToArray();
            if(missing.Length>0)
            {
                e.Members.RemoveAll(missing.Contains);foreach(var lost in missing)if(!e.DestroyedMembers.Contains(lost))e.DestroyedMembers.Add(lost);e.DestroyedMembers.Sort();
                FleetResolver.NormalizeAfterLosses(content,context.Snapshot,candidate,context.FleetContext);
                e.Revision=checked(e.Revision+1);e.ConsentRevision=null;
                if(before.Phase==CombatPhase.Pending||e.Members.Count==0){Finish(content,candidate,e,e.Members.Count==0?"Force lost before exchange":"Cancelled: force revised");continue;}
                e.Phase=CombatPhase.BlockedResponse;e.LastResolvedTurn=context.FromTurn;e.LastResolvedPhase=CombatPhase.BlockedResponse;
                var owner=candidate.FindEmpire(e.Owner)!.Combat!;owner.Responses.RemoveAll(r=>r.Encounter==e.Id);owner.Responses.Add(new(e.Id,e.Revision,"Health or survival loss revised the participating force.",CombatResponseNeed.RevisedForce));owner.Responses=owner.Responses.OrderBy(r=>r.Encounter).ToList();
                Event(content,candidate,e,"emergency","The participating force changed before combat. This boundary has no volley; review the revised force or withdraw.");continue;
            }
            if(e.Phase is CombatPhase.BlockedResponse or CombatPhase.WithdrawalQueued||e.ConsentRevision!=e.Revision)continue;
            var frame=CombatQueries.Frame(content,candidate,e.Id);var result=CombatExchange.Calculate(frame,false,ImmutableArray<VesselId>.Empty);ApplyExchange(content,candidate,e,frame,result,context);
        }
    }
    internal static void ApplyExchange(ContentSet content,GameState candidate,EncounterState encounter,CombatFrame frame,CombatExchangeResult result,CombatResolutionContext context,bool interception=false,ImmutableArray<VesselId> participants=default)
    {
        if(encounter.LastResolvedTurn==context.FromTurn)throw new InvalidOperationException("Encounter already resolved at this boundary.");
        var definition=content.Combat!;var own=candidate.FindEmpire(encounter.Owner)!.Combat!;var pocket=candidate.Combat!.Pockets.Single(p=>p.Id==encounter.Pocket);
        var forecasts=participants.IsDefault?CombatQueries.Forecast(frame,result):CombatQueries.Forecast(frame,result,participants);
        long round=checked(encounter.Round+1);if(round>(long)context.FromTurn+1)throw new OverflowException("Combat round exceeds its boundary count.");
        foreach(var damage in result.Ships.OrderBy(d=>d.Id))
        {
            var ship=candidate.FindVessel(damage.Id)??throw new InvalidOperationException("Combat participant already consumed.");var actor=frame.Ships.Single(s=>s.VesselId==damage.Id);
            encounter.HullLost=checked(encounter.HullLost+damage.ActualLoss);own.LifetimeHullLost=checked(own.LifetimeHullLost+damage.ActualLoss);
            if(damage.After==0)
            {
                if(!context.ConsumedSet.Add(ship.Id))throw new InvalidOperationException("Combat destruction applied twice.");
                context.DestructionFacts.Add(new(encounter.Id,ship.Id,ship.EmpireId,ship.RoleId,ship.CarriedPopulation,actor.Flagship,pocket.Star));
                own.LifetimeShipsLost=checked(own.LifetimeShipsLost+1);own.LifetimeCargoLost=checked(own.LifetimeCargoLost+ship.CarriedPopulation);encounter.CargoLost=checked(encounter.CargoLost+ship.CarriedPopulation);
                encounter.DestroyedMembers.Add(ship.Id);encounter.Members.Remove(ship.Id);candidate.Vessels.Remove(ship);
            }
            else {ship.Combat!.Hull=damage.After;ship.Combat.Cohesion=forecasts.Single(f=>f.Id==ship.Id).CohesionAfter;}
        }
        foreach(var damage in result.Sentries)pocket.Sentries.Single(s=>s.Id==damage.Id).Hull=damage.After;
        pocket.Cleared=pocket.Sentries.All(s=>s.Hull==0);encounter.DestroyedMembers.Sort();encounter.Round=round;encounter.LastResolvedTurn=context.FromTurn;encounter.LastResolvedPhase=interception?CombatPhase.WithdrawalQueued:CombatPhase.Active;
        encounter.Revision=checked(encounter.Revision+1);encounter.ConsentRevision=encounter.Revision;encounter.ContinueRiskTurn=null;
        var receipt=new CombatRoundReceipt(encounter.Id,context.FromTurn,round,interception,frame,result,forecasts){Participants=participants.IsDefault?frame.Ships.Select(s=>s.VesselId).ToImmutableArray():participants};encounter.Rounds.Add(receipt);while(encounter.Rounds.Count>definition.RoundDetailLimit)encounter.Rounds.RemoveAt(0);context.RoundFacts.Add(receipt);
        Event(content,candidate,encounter,interception?"interception":"exchange","A frozen simultaneous volley resolved; actual hull loss and destruction are retained in the encounter receipt.");
        FleetResolver.NormalizeAfterLosses(content,context.Snapshot,candidate,context.FleetContext);
        if(pocket.Cleared||encounter.Members.Count==0)
        {Finish(content,candidate,encounter,pocket.Cleared?encounter.Members.Count==0?"Mutual destruction":"Victory":"Force destroyed");return;}
        var survivors=encounter.Members.Select(id=>candidate.FindVessel(id)!).ToArray();bool armed=survivors.Any(v=>definition.RoleProfiles[v.RoleId].Attack>0);int cohesion=survivors.Min(v=>v.Combat!.Cohesion);
        bool crippled=survivors.Any(v=>CombatMath.Band(definition.RoleProfiles[v.RoleId],v.Combat!.Hull,definition)==CombatDamageBand.Crippled);
        bool withdraw=!armed||encounter.Orders.Doctrine switch {CombatDoctrine.Cautious=>crippled||result.DestroyedVessels.Length>0||cohesion<=definition.RetreatCohesionThreshold,CombatDoctrine.Press=>cohesion<=definition.RetreatCohesionThreshold,CombatDoctrine.HitAndRun=>true,_=>false};
        encounter.Phase=withdraw?CombatPhase.WithdrawalQueued:CombatPhase.Active;
        if(!armed)Event(content,candidate,encounter,"emergency","No armed ship survives. Civilians require withdrawal at the next boundary and remain exposed if their authorized route is blocked.");
    }
    internal static void Finish(ContentSet content,GameState candidate,EncounterState encounter,string outcome)
    {
        var owner=candidate.FindEmpire(encounter.Owner)!;var c=owner.Combat!;candidate.Combat!.Encounters.Remove(encounter);c.Responses.RemoveAll(r=>r.Encounter==encounter.Id);
        c.History.Add(new(encounter.Id,encounter.Pocket,candidate.Turn,outcome,encounter.DestroyedMembers.OrderBy(v=>v).ToImmutableArray(),encounter.Round){Details=encounter.Rounds.ToImmutableArray(),HullLost=encounter.HullLost,CargoLost=encounter.CargoLost});while(c.History.Count>content.Combat!.HistoryLimit)c.History.RemoveAt(0);
        var fleet=owner.Fleets!.Fleets.SingleOrDefault(f=>f.Id==encounter.Fleet);if(fleet is not null)fleet.Order=new(FleetOrderKind.Hold);
        Event(content,candidate,encounter,"encounter_closed",outcome);
    }
    internal static void Event(ContentSet content,GameState candidate,EncounterState encounter,string kind,string reason)
    {
        var c=candidate.FindEmpire(encounter.Owner)!.Combat!;long id=c.NextEventId;c.NextEventId=checked(id+1);c.PendingEvents.Add(new(id,candidate.Turn,kind,reason,encounter.Id));while(c.PendingEvents.Count>content.Combat!.HistoryLimit)c.PendingEvents.RemoveAt(0);
    }
}
