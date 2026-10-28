using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
namespace OpenAntares.Simulation.Fleets;
internal static class FleetResolver
{
    internal static void BindCompletion(ContentSet content,GameState snapshot,GameState candidate,FleetResolutionContext context,ColonyId colony,VesselState ship)
    {
        context.Completed.Add(ship.Id);var before=snapshot.FindColony(colony)!;
        if(before.Development?.ActiveQueueToken is not {} token)return;
        var f=candidate.FindEmpire(ship.EmpireId)!.Fleets!;
        var binding=f.Requests.Where(r=>FleetQueries.IsActive(r.Status)).SelectMany(r=>r.Slots.Select(slot=>(Request:r,Slot:slot))).SingleOrDefault(x=>x.Slot.Colony==colony&&x.Slot.QueueToken==token&&x.Slot.RoleId==ship.RoleId&&x.Slot.Vessel is null&&(x.Slot.Status==SlotStatus.Queued||x.Slot.Status==SlotStatus.BlockedSafety&&x.Slot.ResumeStatus==SlotStatus.Queued));
        if(binding.Request is null)return;
        var slot=binding.Slot;ship.BuildOrigin=new(binding.Request.Id,slot.Ordinal,colony,token);slot.Vessel=ship.Id;slot.NewHull=true;slot.QueueToken=null;
        if(slot.Status==SlotStatus.BlockedSafety)slot.ResumeStatus=SlotStatus.Built;else slot.Status=SlotStatus.Built;
        binding.Request.Revision=checked(binding.Request.Revision+1);
        context.Event(candidate,ship.EmpireId,"request_hull_built","Original queue reservation completed; dispatch begins at the next preparation.",request:binding.Request.Id,vessel:ship.Id);
    }
    internal static void NormalizeAfterLosses(ContentSet content,GameState snapshot,GameState candidate,FleetResolutionContext context)
    {
        foreach(var e in candidate.Empires.OrderBy(e=>e.Id))
        {
            var f=e.Fleets!;
            foreach(var fleet in f.Fleets.OrderBy(f=>f.Id).ToArray())
            {
                var lost=fleet.Members.Where(id=>candidate.FindVessel(id) is null).ToArray();
                foreach(var id in lost){fleet.Members.Remove(id);context.Event(candidate,e.Id,"fleet_ship_lost","A fleet ship was destroyed.",fleet:fleet.Id,vessel:id);}
                if(lost.Contains(fleet.Flagship))
                {
                    if(fleet.Officer is {} officer)
                    {
                        var before=snapshot.FindVessel(fleet.Flagship)!;var appointment=context.Officers.Single(o=>o.Owner==e.Id&&o.Officer.Id==officer);
                        context.Losses.Add(new(appointment,before.Id,before.StationaryStarId??before.Transit!.Origin));fleet.Officer=null;
                    }
                    if(fleet.Members.Count>0)fleet.Flagship=fleet.Members.Min();
                }
                if(fleet.Members.Count==0){f.Fleets.Remove(fleet);RequestQueueAuthority.RallyDisbanded(candidate,e.Id,fleet.Id);}
            }
            foreach(var r in f.Requests.Where(r=>FleetQueries.IsActive(r.Status)))
            {
                bool changed=false;
                foreach(var slot in r.Slots.Where(s=>s.Vessel is {} id&&candidate.FindVessel(id) is null))
                {slot.FormerVessel=slot.Vessel;slot.Vessel=null;slot.Status=SlotStatus.Missing;slot.ResumeStatus=null;slot.QueueToken=null;slot.BlockedReason="Reserved hull lost; explicit finite amendment required.";changed=true;}
                if(changed){r.Revision=checked(r.Revision+1);context.Event(candidate,e.Id,"request_missing","A reserved hull was lost; no automatic replacement authorized.",request:r.Id,importance:FleetImportance.Attention);}
            }
            NormalizeLostPostings(candidate,e.Id,context);
        }
    }
    // Colony removal already exists in the plague rules. End an unavailable appointment without
    // asserting the body's survival/death or relocating a person to a different colony.
    private static void NormalizeLostPostings(GameState candidate,EmpireId owner,FleetResolutionContext context)
    {
        var f=candidate.FindEmpire(owner)!.Fleets!;
        foreach(var o in f.Officers.Where(o=>FleetQueries.IsActive(o.Status)&&(o.SourceBody is {} body&&candidate.FindColony(body) is null||o.PostedColony is {} posting&&candidate.FindColony(posting) is null)))
        {
            foreach(var fleet in f.Fleets.Where(fleet=>fleet.Officer==o.Id))fleet.Officer=null;
            foreach(var r in f.Requests.Where(r=>r.Coordinator==o.Id)){r.Coordinator=null;r.CoordinatorXpForfeited=true;}
            o.Status=OfficerStatus.Retired;o.PostedColony=null;o.Flagship=null;o.Request=null;o.AvailableAtTurn=null;o.RecoveryColony=null;o.FinalSummary="Appointment ended when its colony became unavailable; no survival or death outcome is asserted.";
            context.Event(candidate,owner,"appointment_unavailable",o.FinalSummary,officer:o.Id,importance:FleetImportance.Attention);
        }
        foreach(var r in f.Requests.Where(r=>FleetQueries.IsActive(r.Status)))
        {
            var lost=r.Colonies.Where(id=>candidate.FindColony(id) is null).ToArray();if(lost.Length==0)continue;
            r.Colonies.RemoveAll(lost.Contains);r.PausedContributions.RemoveAll(lost.Contains);
            foreach(var slot in r.Slots.Where(s=>s.Colony is {} col&&lost.Contains(col)&&s.Vessel is null)){slot.Status=SlotStatus.Missing;slot.ResumeStatus=null;slot.QueueToken=null;slot.BlockedReason="Shipyard lost; explicit finite amendment required.";}
            RequestQueueAuthority.SafetyPause(candidate,owner,r.Id,"A participating colony was lost; review remaining finite work.");
        }
    }
    internal static void AfterFounding(GameState candidate,FleetResolutionContext context,IReadOnlyList<ExpansionEvent> events)
    {
        foreach(var founded in events.Where(e=>e.Kind=="colony_founded"))
        {
            var f=candidate.FindEmpire(founded.Owner)!.Fleets!;
            foreach(var fleet in f.Fleets.Where(f=>f.Members.Contains(founded.Vessel!.Value)).ToArray())
            {
                var vessel=founded.Vessel!.Value;
                if(fleet.Flagship==vessel&&fleet.Officer is {} officer)
                {var o=f.Officers.Single(o=>o.Id==officer);o.Status=OfficerStatus.Posted;o.Flagship=null;o.PostedColony=founded.Colony;fleet.Officer=null;context.Event(candidate,founded.Owner,"officer_posted_after_founding","Colony founded; command posted at the new colony without a loss or recovery roll.",officer:o.Id,vessel:vessel);}
                fleet.Members.Remove(vessel);fleet.Order=new(FleetOrderKind.Hold);fleet.Cohort=null;
                if(fleet.Members.Count==0){f.Fleets.Remove(fleet);RequestQueueAuthority.RallyDisbanded(candidate,founded.Owner,fleet.Id);}else if(fleet.Flagship==vessel)fleet.Flagship=fleet.Members.Min();
            }
        }
    }
    internal static void ObserveAssembly(ContentSet content,GameState candidate,FleetResolutionContext context)
    {
        foreach(var e in candidate.Empires.OrderBy(e=>e.Id))
        {
            var f=e.Fleets!;
            foreach(var r in f.Requests.Where(r=>FleetQueries.IsActive(r.Status)).OrderBy(r=>r.Id))
            {
                foreach(var slot in r.Slots.Where(s=>s.Vessel is not null))
                {
                    var ship=candidate.FindVessel(slot.Vessel!.Value)!;var progress=ship.Transit is not null?SlotStatus.Travelling:ship.Fleet==r.RallyFleet&&r.RallyFleet is not null&&ship.StationaryStarId==r.Rally?SlotStatus.Arrived:SlotStatus.Built;
                    if(slot.Status==SlotStatus.BlockedSafety)slot.ResumeStatus=progress;else slot.Status=progress;
                }
                if(FleetQueries.RallyUnsafe(content,candidate,e.Id,r)){RequestQueueAuthority.SafetyPause(candidate,e.Id,r.Id,"Rally is unsafe under current own knowledge or population port restrictions.");continue;}
                if(r.Status!=RequestStatus.Active)continue;
                foreach(var slot in r.Slots.Where(s=>s.Status==SlotStatus.Built&&s.Vessel is {} id&&!context.Completed.Contains(id)).OrderBy(s=>s.Vessel))
                {
                    var ship=candidate.FindVessel(slot.Vessel!.Value)!;if(ship.StationaryStarId!=r.Rally||ship.PendingMove is not null||ship.PendingFounding is not null||ship.AmebaEncounter is not null)continue;
                    var fleet=f.Fleets.SingleOrDefault(fleet=>fleet.Id==r.RallyFleet);
                    if(fleet is null)
                    {
                        if(!r.ReservedFleetSlot)throw new InvalidOperationException("Rally capacity is not reserved.");
                        var id=new FleetId(f.NextFleetId);f.NextFleetId=checked(f.NextFleetId+1);fleet=new(){Id=id,Name=r.Name,Flagship=ship.Id};f.Fleets.Add(fleet);r.RallyFleet=id;r.ReservedFleetSlot=false;
                    }
                    if(fleet.Members.Count>=content.FleetCommand!.MemberLimit||fleet.Members.Any(id=>candidate.FindVessel(id) is not{Transit:null,PendingMove:null} v||v.StationaryStarId!=r.Rally))continue;
                    if(ship.Fleet is not null&&ship.Fleet!=fleet.Id)continue;
                    if(!fleet.Members.Contains(ship.Id))fleet.Members.Add(ship.Id);fleet.Members.Sort();ship.Fleet=fleet.Id;slot.Status=SlotStatus.Arrived;r.Revision=checked(r.Revision+1);
                }
                if(r.Slots.Any(s=>s.Status!=SlotStatus.Arrived))continue;
                r.Status=RequestStatus.Complete;r.CompletedTurn=candidate.Turn;r.Attention=null;r.Revision=checked(r.Revision+1);
                f.Fleets.Single(fleet=>fleet.Id==r.RallyFleet).Order=new(FleetOrderKind.Hold);
                if(r.Coordinator is {} coordinator){var o=f.Officers.Single(o=>o.Id==coordinator);o.Status=OfficerStatus.Posted;o.Request=null;r.Coordinator=null;}
                context.Fulfilled.Add((e.Id,r.Id));context.Event(candidate,e.Id,"request_complete","The entire finite quota arrived; fleet is Holding.",fleet:r.RallyFleet,request:r.Id);
            }
        }
    }
}
