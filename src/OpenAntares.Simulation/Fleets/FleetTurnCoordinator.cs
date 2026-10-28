using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Simulation.Fleets;
public sealed record FleetPreparationResult(GameState State,bool Succeeded);
public static class FleetTurnCoordinator
{
    public static FleetPreparationResult PrepareRequests(ContentSet content,GameState state,EmpireId owner)
    {
        if(!Rules.HasFleets(state.RulesVersion))return new(state,true);
        try
        {
            var candidate=state.Clone();var f=candidate.FindEmpire(owner)!.Fleets!;int visited=0;
            foreach(var r in f.Requests.Where(r=>FleetQueries.IsActive(r.Status)).OrderBy(r=>r.Id))
            {
                foreach(var slot in r.Slots)if(++visited>1024)return new(state,false);
                bool unsafeRally=FleetQueries.RallyUnsafe(content,candidate,owner,r);
                if(unsafeRally)RequestQueueAuthority.SafetyPause(candidate,owner,r.Id,"Rally is unsafe under current own knowledge or population port restrictions.");
            }
            return new(candidate,true);
        }
        catch(Exception error)when(error is OverflowException or ArgumentException or InvalidOperationException){return new(state,false);}
    }
    public static FleetPreparationResult PrepareOrders(ContentSet content,GameState state,EmpireId owner)
    {
        if(!Rules.HasFleets(state.RulesVersion))return new(state,true);
        try
        {
            var view=EmpireViewProjector.Project(content,state,owner);var plan=FleetPlanner.Plan(FleetPlanningQueries.Create(content,view));var candidate=state.Clone();
            if(plan.Moves.Length>view.Vessels.Length||plan.Moves.Select(m=>m.Vessel).Distinct().Count()!=plan.Moves.Length)return new(state,false);
            foreach(var move in plan.Moves)
            {
                var result=CommandProcessor.SetFleetMoveOrder(content,candidate,new(owner,state.Turn,move.Vessel,move.Destination));if(!result.Accepted)return new(state,false);candidate=result.State;
            }
            var f=candidate.FindEmpire(owner)!.Fleets!;
            foreach(var order in plan.Orders)
            {
                var fleet=f.Fleets.Single(fleet=>fleet.Id==order.Fleet);if(fleet.Order==order.Order&&fleet.Attention==order.Attention)continue;fleet.Order=order.Order;fleet.Attention=order.Attention;
                long id=f.NextEventId;f.NextEventId=checked(id+1);f.PendingEvents.RemoveAll(e=>e.Turn==state.Turn&&e.Kind=="fleet_order"&&e.Fleet==fleet.Id);f.PendingEvents.Add(new(owner,id,state.Turn,"fleet_order",order.Attention??"Standing destination reached; Holding.",Fleet:fleet.Id));
            }
            return new(candidate,true);
        }
        catch(Exception error)when(error is OverflowException or ArgumentException or InvalidOperationException){return new(state,false);}
    }
    public static FleetPreparationResult NormalizeSafety(ContentSet content,GameState state,EmpireId owner)
    {
        if(!Rules.HasFleets(state.RulesVersion))return new(state,true);
        try
        {
            var candidate=state.Clone();var f=candidate.FindEmpire(owner)!.Fleets!;
            foreach(var fleet in f.Fleets.OrderBy(f=>f.Id))
            {
                if(candidate.FindVessel(fleet.Flagship)!.Transit is not null)continue;
                var route=candidate.FindVessel(fleet.Flagship)!.PendingMove;
                foreach(var id in fleet.Members.Where(id=>candidate.FindVessel(id)!.PendingMove!=route).OrderBy(id=>id).ToArray())
                {
                    var vessel=candidate.FindVessel(id)!;vessel.Fleet=null;fleet.Members.Remove(id);
                    foreach(var r in f.Requests.Where(r=>FleetQueries.IsActive(r.Status)&&(r.RallyFleet==fleet.Id||r.Slots.Any(s=>s.Vessel==id))))
                    {
                        foreach(var slot in r.Slots.Where(s=>s.Vessel==id)){if(slot.Status==SlotStatus.Arrived)slot.Status=SlotStatus.Built;else if(slot.Status==SlotStatus.BlockedSafety&&slot.ResumeStatus==SlotStatus.Arrived)slot.ResumeStatus=SlotStatus.Built;}
                        RequestQueueAuthority.SafetyPause(candidate,owner,r.Id,"A reserved ship withdrew for safety; living bindings are retained.");
                    }
                    long eventId=f.NextEventId;f.NextEventId=checked(eventId+1);f.PendingEvents.Add(new(owner,eventId,state.Turn,"fleet_safety_detached","Safety departure made this ship independent; no named fleet slot was allocated.",Fleet:fleet.Id,Vessel:id));
                }
            }
            foreach(var r in f.Requests.Where(r=>FleetQueries.IsActive(r.Status)))
                if(r.Slots.Any(slot=>slot.Vessel is {} id&&candidate.FindVessel(id) is {} ship&&ship.AmebaEncounter is not null&&ship.PendingMove is {} target&&target!=r.Rally))RequestQueueAuthority.SafetyPause(candidate,owner,r.Id,"A reserved ship withdrew for safety; living bindings are retained.");
            return new(candidate,true);
        }
        catch(Exception error)when(error is OverflowException or ArgumentException or InvalidOperationException){return new(state,false);}
    }
    internal static FleetPreparationResult PrepareHuman(ContentSet content,GameState state,EmpireId owner)
    {
        try
        {
            if(state.FindEmpire(owner) is not{Controller:ControllerKind.Human} empire||empire.Crisis!.RequireAcknowledgement&&!Crises.CrisisQueries.PendingAttention(state,owner).IsEmpty)return new(state,false);
            var health=Crises.CrisisTurnCoordinator.Prepare(content,state,owner);if(!health.Succeeded)return new(state,false);
            var requests=PrepareRequests(content,health.State,owner);if(!requests.Succeeded)return new(state,false);
            var governor=Development.GovernorTurnCoordinator.Prepare(content,requests.State,owner);if(!governor.Succeeded)return new(state,false);
            var orders=PrepareOrders(content,governor.State,owner);if(!orders.Succeeded)return new(state,false);
            var scout=Scouting.ScoutTurnCoordinator.Prepare(content,orders.State,owner);if(!scout.Succeeded)return new(state,false);
            var safety=NormalizeSafety(content,scout.State,owner);if(!safety.Succeeded)return new(state,false);
            CommandProcessor.ReconcileFleetConsent(content,state,safety.State,owner);
            if(Scouting.ScoutReadyQueries.ValidateResponse(content,safety.State,owner).Length!=0||GameStateValidator.Validate(content,safety.State,true).Count!=0)return new(state,false);
            return safety;
        }
        catch(Exception error)when(error is OverflowException or ArgumentException or InvalidOperationException){return new(state,false);}
    }
}
