using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public static class ConflictAftermath
{
    public static void InterruptRefits(ContentSet content,GameState snapshot,GameState candidate,ConflictResolutionContext context)
    {
        if(context.RefitsInterrupted||!context.MatchingResolved||context.ExchangesResolved||context.WithdrawalsResolved||snapshot.Turn!=context.FromTurn||candidate.Turn!=snapshot.Turn)throw new InvalidOperationException("Refit interruption requires freshly matched conflict.");
        context.RefitsInterrupted=true;if(!Rules.HasConflict(snapshot.RulesVersion))return;
        foreach(var frame in context.Battles.Where(f=>f.Phase==ConflictPhase.Pending&&f.Round==0))
        foreach(var actor in frame.Low.Concat(frame.High))
        {
            var ship=candidate.FindVessel(actor.VesselId)!;var data=candidate.FindEmpire(ship.EmpireId)!.ShipDesigns!;var job=data.Refits.SingleOrDefault(j=>j.Vessel==ship.Id);if(job is null)continue;
            var original=context.Snapshot.FindEmpire(ship.EmpireId)!.ShipDesigns!.Refits.SingleOrDefault(j=>j.Id==job.Id);if(original is null||!SameRefit(original,job))throw new InvalidOperationException("Matched refit identity changed.");
            var port=candidate.FindColony(job.Port);
            if(port is not null)
            {
                if(port.EmpireId!=ship.EmpireId||ShipDesignTargets.Job(candidate,port)?.Id!=job.Id)throw new InvalidOperationException("Matched refit slot changed.");
                port.ProjectId=null;port.ProjectKind=ProductionKinds.Building;port.Development!.ConstructionEnabled=false;port.Development.ActiveQueueToken=null;port.Development.Origin=ProductionOrigin.None;
            }
            data.Refits.Remove(job);context.InterruptedRefits.Add(new(ship.EmpireId,job.Clone()));
        }
    }
    internal static bool SameRefit(RefitJob a,RefitJob b)=>a.Id==b.Id&&a.Vessel==b.Vessel&&a.Port==b.Port&&a.SourceDesign==b.SourceDesign&&a.TargetDesign==b.TargetDesign&&a.Price==b.Price&&a.Revision==b.Revision&&a.Status==b.Status&&a.AcceptedTurn==b.AcceptedTurn&&StringComparer.Ordinal.Equals(a.Reason,b.Reason);
    internal static bool ProvesInterruption(GameState snapshot,GameState candidate,ConflictResolutionContext context,EmpireId owner,RefitJob job)
    {
        if(!Rules.HasConflict(snapshot.RulesVersion)||!context.RefitsInterrupted||context.FromTurn!=snapshot.Turn||candidate.Turn!=snapshot.Turn)return false;
        var fact=context.InterruptedRefits.SingleOrDefault(f=>f.Owner==owner&&f.Job.Id==job.Id);
        var original=context.Snapshot.FindEmpire(owner)?.ShipDesigns?.Refits.SingleOrDefault(j=>j.Id==job.Id);
        if(fact is null||original is null||!SameRefit(fact.Job,job)||!SameRefit(original,job)||candidate.FindEmpire(owner)!.ShipDesigns!.Refits.Any(j=>j.Id==job.Id))return false;
        var port=candidate.FindColony(job.Port);return port is null||port.EmpireId==owner&&port.ProjectId is null&&port.ProjectKind==ProductionKinds.Building&&port.Development is{ConstructionEnabled:false,ActiveQueueToken:null};
    }
    public static void Apply(ContentSet content,GameState snapshot,GameState candidate,FleetResolutionContext fleet,ConflictResolutionContext context)
    {
        if(context.AftermathApplied||!context.MatchingResolved||!context.ExchangesResolved||!ReferenceEquals(fleet,context.FleetContext)||snapshot.Turn!=context.FromTurn||candidate.Turn!=snapshot.Turn)throw new InvalidOperationException("Conflict aftermath requires its shared snapshot officer context once.");
        context.AftermathApplied=true;if(!Rules.HasConflict(snapshot.RulesVersion))return;
        // Freeze detachment custody before ordinary loss normalization chooses a replacement flagship.
        foreach(var departure in context.Departures.Where(d=>!d.Main))
        foreach(var id in departure.Members)
        {
            var ship=candidate.FindVessel(id);if(ship is null)continue;
            if(ship.Fleet is {} group)
            {var f=candidate.FindEmpire(ship.EmpireId)!.Fleets!.Fleets.Single(x=>x.Id==group);if(f.Flagship==id)throw new InvalidOperationException("A designated flagship cannot detach as a straggler.");f.Members.Remove(id);ship.Fleet=null;}
        }
        CombatLossIntegration.ApplyConflict(content,snapshot,candidate,fleet,context);
        foreach(var encounter in candidate.Conflict!.Encounters.Where(e=>e.Phase!=ConflictPhase.Completed||e.CompletedTurn==(long)candidate.Turn+1))
        foreach(var side in new[]{encounter.LowSide,encounter.HighSide})
        {
            var group=candidate.FindEmpire(side.Owner)!.Fleets!.Fleets.SingleOrDefault(f=>f.Id==side.Fleet);
            if(candidate.FindVessel(side.Flagship) is null&&group is not null)side.Flagship=group.Flagship;
            foreach(var id in side.Members)
            {var ship=candidate.FindVessel(id);if(context.UsedSet.Contains(id)&&ship?.Combat is {} combat){combat.Repairing=false;combat.ReturnColony=null;combat.OnArrival=null;}}
        }
        foreach(var departure in context.Departures)
        foreach(var id in departure.Members)
        {
            if(candidate.FindVessel(id) is not {} ship)continue;
            ship.Combat!.Repairing=false;ship.Combat.ReturnColony=departure.ReturnColony;ship.Combat.OnArrival=departure.Destination is null?null:departure.OnArrival;
            if(departure.Destination is null)ship.PendingMove=null;
        }
        NormalizeProtectedMembership(candidate);
    }
    internal static void NormalizeProtectedMembership(GameState candidate)
    {
        if(candidate.Conflict is null)return;
        foreach(var encounter in candidate.Conflict.Encounters.Where(e=>e.Phase!=ConflictPhase.Completed||e.CompletedTurn==(long)candidate.Turn+1))
        foreach(var side in new[]{encounter.LowSide,encounter.HighSide})
        {
            side.ProtectedCivilians.RemoveAll(id=>candidate.FindVessel(id) is null);
            var fleet=candidate.FindEmpire(side.Owner)!.Fleets!.Fleets.SingleOrDefault(f=>f.Id==side.Fleet);
            if(candidate.FindVessel(side.Flagship) is null&&fleet is not null)side.Flagship=fleet.Flagship;
        }
        foreach(var owner in candidate.Conflict.Owners)
        {
            owner.Preferences.RemoveAll(p=>candidate.FindVessel(p.Anchor) is not {} ship||ship.EmpireId!=owner.Owner||ship.Fleet!=p.Fleet);
            owner.Blockades.RemoveAll(b=>candidate.FindEmpire(owner.Owner)!.Fleets!.Fleets.All(f=>f.Id!=b.Fleet));
        }
    }
}
