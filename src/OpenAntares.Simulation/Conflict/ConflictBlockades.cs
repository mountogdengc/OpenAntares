using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public sealed record ConflictBlockadePressure(DiplomacyPair Pair,EmpireId Victim,EmpireId Blockader,StarId Star,int RetainedPercent);
public sealed record ConflictBlockadeStatus(EmpireId Owner,FleetId Fleet,EmpireId Other,StarId Star,long EffectiveTurn,string Status);
public sealed record ConflictBlockadeFrame(int Turn,ImmutableArray<ConflictBlockadePressure> Pressure,ImmutableArray<ConflictBlockadeStatus> Orders)
{
    public ConflictBlockadeFrame Suppress(ImmutableHashSet<DiplomacyPair> pairs)=>this with{Pressure=Pressure.Where(p=>!pairs.Contains(p.Pair)).ToImmutableArray(),Orders=Orders.Select(o=>pairs.Contains(DiplomacyPair.For(o.Owner,o.Other))?o with{Status="Stopped by peace"}:o).ToImmutableArray()};
}
public static class ConflictBlockades
{
    public static ConflictBlockadeFrame Capture(ContentSet content,GameState state)
    {
        if(!Rules.HasConflict(state.RulesVersion))return new(state.Turn,ImmutableArray<ConflictBlockadePressure>.Empty,ImmutableArray<ConflictBlockadeStatus>.Empty);
        if(state.Conflict is not {} conflict||content.Conflict is null)throw new InvalidOperationException("Conflict blockade capture requires explicit state and definitions.");
        var pressure=ImmutableArray.CreateBuilder<ConflictBlockadePressure>();var statuses=ImmutableArray.CreateBuilder<ConflictBlockadeStatus>();
        foreach(var owner in conflict.Owners.OrderBy(o=>o.Owner))
        foreach(var order in owner.Blockades.OrderBy(b=>b.Fleet))
        {
            string status=Status();statuses.Add(new(owner.Owner,order.Fleet,order.Other,order.Star,(long)order.AcceptedTurn+1,status));
            if(status=="Effective")pressure.Add(new(DiplomacyPair.For(owner.Owner,order.Other),order.Other,owner.Owner,order.Star,content.Conflict.BlockadeRetainedPercent));
            string Status()
            {
                if(conflict.Pairs.All(p=>p.Pair!=DiplomacyPair.For(owner.Owner,order.Other)||p.Relation!=RelationKind.War))return "Unavailable";
                if(order.AcceptedTurn>=state.Turn)return "Pending";
                var empire=state.FindEmpire(owner.Owner);var fleet=empire?.Fleets?.Fleets.SingleOrDefault(f=>f.Id==order.Fleet);if(fleet is null||fleet.Members.Count is <1 or >32)return "Unavailable";
                var ships=fleet.Members.Select(state.FindVessel).ToArray();
                if(ships.Any(v=>v is null||v.StationaryStarId!=order.Star||v.Transit is not null||v.PendingMove is not null||v.PendingFounding is not null||!ConflictObservations.IsMilitary(content,state,v)))return "Own force unavailable";
                if(fleet.Cohort is not null||fleet.Order.Kind!=FleetOrderKind.Hold||ships.Any(v=>v!.Combat!.Repairing||empire!.ShipDesigns!.Refits.Any(r=>r.Vessel==v.Id)||v.AmebaEncounter is not null)||empire!.Fleets!.Requests.Any(r=>FleetQueries.IsActive(r.Status)&&(r.RallyFleet==fleet.Id||r.Slots.Any(s=>s.Vessel is {} id&&fleet.Members.Contains(id))))||state.Combat!.Encounters.Any(e=>e.Members.Any(fleet.Members.Contains))||conflict.Encounters.Any(e=>e.Phase!=ConflictPhase.Completed&&(e.LowSide.Members.Any(fleet.Members.Contains)||e.HighSide.Members.Any(fleet.Members.Contains))))return "Own force busy";
                if(!ships.Any(v=>v!.Combat!.Cohesion>content.Combat!.RetreatCohesionThreshold&&CombatMath.Band(VesselLoadouts.CombatProfile(content,state,v),v.Combat.Hull,content.Combat)==CombatDamageBand.Operational))return "Own force needs repair";
                if(!ConflictObservations.OwnPresence(state,owner.Owner,order.Star)||!state.Colonies.Any(c=>c.EmpireId==order.Other&&state.FindPlanet(c.PlanetId)?.StarId==order.Star))return "Target unavailable";
                // Every stationary hostile armed hull contests, including repair, refit and sentry duty.
                if(state.Vessels.Any(v=>v.EmpireId==order.Other&&v.StationaryStarId==order.Star&&v.Transit is null&&ConflictObservations.IsMilitary(content,state,v)))return "Contested";
                return "Effective";
            }
        }
        return new(state.Turn,pressure.DistinctBy(p=>(p.Victim,p.Star)).OrderBy(p=>p.Victim).ThenBy(p=>p.Star).ToImmutableArray(),statuses.ToImmutable());
    }
}
