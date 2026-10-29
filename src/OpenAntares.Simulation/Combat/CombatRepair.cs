using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public static class CombatRepair
{
    public static ImmutableArray<VesselId> Capture(ContentSet content,GameState snapshot)=>!Rules.HasCombat(snapshot.RulesVersion)?ImmutableArray<VesselId>.Empty:snapshot.Vessels.Where(v=>v.Combat?.Repairing==true&&Eligible(snapshot,v)).OrderBy(v=>v.Id).Select(v=>v.Id).ToImmutableArray();
    private static bool Conflict(GameState state,VesselState vessel)
    {
        var command=state.FindEmpire(vessel.EmpireId)?.Fleets;var group=command?.Fleets.SingleOrDefault(f=>f.Id==vessel.Fleet);
        return vessel.PendingMove is not null||vessel.PendingFounding is not null||vessel.AmebaEncounter is not null||state.Combat!.Encounters.Any(e=>e.Members.Contains(vessel.Id))||group is not null&&group.Order.Kind!=FleetOrderKind.Hold||command?.Requests.Any(r=>FleetQueries.IsActive(r.Status)&&(group is not null&&r.RallyFleet==group.Id||r.Slots.Any(s=>s.Vessel==vessel.Id&&s.Status!=SlotStatus.Missing)))==true;
    }
    internal static bool Eligible(GameState state,VesselState vessel)
    {
        if(vessel.Combat is not {} c||c.ReturnColony is not {} destination||c.OnArrival!=CombatArrivalOrder.Repair||vessel.Transit is not null||Conflict(state,vessel))return false;
        var port=state.FindColony(destination);return port?.EmpireId==vessel.EmpireId&&state.FindPlanet(port.PlanetId)?.StarId==vessel.StationaryStarId&&!(vessel.CarriedPopulation>0&&(Crises.CrisisQueries.IsHeld(state,vessel.Id)||Crises.CrisisQueries.PortClosed(state,vessel.EmpireId,vessel.StationaryStarId!.Value)));
    }
    private static void Clear(ShipCombatState state){state.Repairing=false;state.ReturnColony=null;state.OnArrival=null;}
    private static void Record(CombatDefinition definition,GameState state,VesselState vessel,string kind,string reason)
    {
        var c=state.FindEmpire(vessel.EmpireId)!.Combat!;long id=c.NextEventId;c.NextEventId=checked(id+1);c.PendingEvents.Add(new(id,state.Turn,kind,reason,Vessel:vessel.Id));while(c.PendingEvents.Count>definition.HistoryLimit)c.PendingEvents.RemoveAt(0);
    }
    public static void Apply(ContentSet content,GameState snapshot,GameState candidate,CombatResolutionContext context)
    {
        if(context.RepairsApplied||context.FromTurn!=snapshot.Turn)throw new InvalidOperationException("Repair phase already applied or snapshot changed.");context.RepairsApplied=true;
        if(!Rules.HasCombat(snapshot.RulesVersion))return;
        var definition=content.Combat!;
        foreach(var vessel in candidate.Vessels)
        {
            if(vessel.Combat is not {} ship)continue;
            if(ship.ReturnColony is {} destination&&candidate.FindColony(destination)?.EmpireId!=vessel.EmpireId)
            {Clear(ship);Record(definition,candidate,vessel,"repair_unavailable","The selected return colony is unavailable; finite repair authority ended without retargeting.");}
            else if(ship.Repairing&&Conflict(candidate,vessel)){Clear(ship);Record(definition,candidate,vessel,"repair_stopped","An accepted movement, founding, combat or finite request order took priority over Repair.");}
        }
        foreach(var vessel in candidate.Vessels)
        {
            var ship=vessel.Combat!;
            bool waiting=ship.OnArrival==CombatArrivalOrder.Repair&&ship.ReturnColony is {} destination&&candidate.FindColony(destination) is {} port&&candidate.FindPlanet(port.PlanetId)?.StarId==vessel.StationaryStarId&&vessel.Transit is null;
            if(!waiting||Eligible(candidate,vessel)||ship.LastRepairBlockedTurn==context.FromTurn)continue;
            ship.LastRepairBlockedTurn=context.FromTurn;Record(definition,candidate,vessel,"repair_blocked","Repair paused at the selected owned port because current cargo/port conditions prevent an eligible stationary interval. No hull or cohesion was restored.");
        }
        foreach(var id in context.RepairCapture)
        {
            var vessel=candidate.FindVessel(id);if(vessel?.Combat is not {} ship||!ship.Repairing||ship.LastRepairTurn==context.FromTurn||!Eligible(candidate,vessel)||context.Departures.ContainsKey(id)||context.Rounds.Any(r=>r.Participants.Contains(id)))continue;
            var profile=definition.RoleProfiles[vessel.RoleId];int before=ship.Hull;
            Int128 gain=((Int128)profile.MaxHull+definition.RepairHullDivisor-1)/definition.RepairHullDivisor;
            ship.Hull=checked((int)Int128.Min(profile.MaxHull,(Int128)ship.Hull+gain));ship.Cohesion=Math.Min(100,checked(ship.Cohesion+definition.RepairCohesion));ship.LastRepairTurn=context.FromTurn;
            int actual=ship.Hull-before;ship.HullRepaired=checked(ship.HullRepaired+actual);var owner=candidate.FindEmpire(vessel.EmpireId)!.Combat!;owner.LifetimeHullRepaired=checked(owner.LifetimeHullRepaired+actual);
            Record(definition,candidate,vessel,"repair","A full stationary interval restored hull and command cohesion without spending production.");
            if(ship.Hull==profile.MaxHull&&ship.Cohesion==100){Clear(ship);Record(definition,candidate,vessel,"repair_complete","Hull and command cohesion are full; finite Repair ended in Hold.");}
        }
    }
    public static void ObserveArrivals(GameState candidate,CombatResolutionContext context)
    {
        if(!Rules.HasCombat(candidate.RulesVersion))return;var definition=context.Definition!;
        foreach(var vessel in candidate.Vessels)
        {
            if(vessel.Combat is not {ReturnColony:{} destination,OnArrival:{} arrival} ship||vessel.Transit is not null||vessel.PendingMove is not null||vessel.PendingFounding is not null)continue;
            var port=candidate.FindColony(destination);if(port?.EmpireId!=vessel.EmpireId||candidate.FindPlanet(port.PlanetId)?.StarId!=vessel.StationaryStarId)continue;
            if(arrival==CombatArrivalOrder.Hold){Clear(ship);Record(definition,candidate,vessel,"return_hold","The authorized return completed in Hold without arrival healing.");continue;}
            if(!Eligible(candidate,vessel))continue;
            if(ship.Hull==definition.RoleProfiles[vessel.RoleId].MaxHull&&ship.Cohesion==100){Clear(ship);continue;}
            ship.Repairing=true; // Eligibility capture already happened; no arrival tick can be granted.
        }
    }
    internal static CommandOutcome Override(ContentSet content,GameState before,CommandOutcome result,Command command)
    {
        if(!result.Accepted||!Rules.HasCombat(before.RulesVersion))return result;
        var targets=new HashSet<VesselId>();var owner=before.FindEmpire(command.Issuer)!;
        void Group(FleetId id){var fleet=owner.Fleets!.Fleets.SingleOrDefault(f=>f.Id==id);if(fleet is not null)targets.UnionWith(fleet.Members);}
        void Request(RequestId id){var r=result.State.FindEmpire(command.Issuer)!.Fleets!.Requests.SingleOrDefault(r=>r.Id==id);if(r is null)return;if(r.RallyFleet is {} fleet)Group(fleet);targets.UnionWith(r.Slots.Where(s=>s.Vessel is not null).Select(s=>s.Vessel!.Value));}
        switch(command)
        {
            case SetMoveOrderCommand move:targets.Add(move.Vessel);break;
            case SetFoundingOrderCommand found:targets.Add(found.Vessel);break;
            case SetAmebaRiskOrderCommand {Action:not null} risk:targets.Add(risk.Vessel);break;
            case FleetCommand {Action:FleetAction.SetOrder order}:foreach(var id in order.Fleets)Group(id);break;
            case FleetCommand {Action:FleetAction.MoveSelection move}:targets.UnionWith(move.Vessels);foreach(var id in move.Fleets)Group(id);break;
            case AssemblyCommand {Action:AssemblyAction.Accept accept}:targets.UnionWith(accept.Draft.AcceptedIdleVessels);if(accept.Draft.RallyFleet is {} rally)Group(rally);break;
            case AssemblyCommand {Action:AssemblyAction.Edit edit}:if(edit is AssemblyAction.Resume or AssemblyAction.ChangeRally or AssemblyAction.Amend)Request(edit.Request);break;
        }
        var affected=targets.Where(id=>result.State.FindVessel(id)?.Combat is {} ship&&(ship.Repairing||ship.ReturnColony is not null||ship.OnArrival is not null)).ToArray();if(affected.Length==0)return result;
        var candidate=result.Changed?result.State:result.State.Clone();foreach(var id in affected){var ship=candidate.FindVessel(id)!;Clear(ship.Combat!);Record(content.Combat!,candidate,ship,"repair_stopped","The explicitly accepted ordinary order replaced finite return/repair authority.");}candidate.FindEmpire(command.Issuer)!.Ready=false;return new(candidate,null,true,result.Report);
    }
}
