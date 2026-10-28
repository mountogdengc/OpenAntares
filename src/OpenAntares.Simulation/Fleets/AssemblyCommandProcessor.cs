using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Fleets;
public static class AssemblyCommandProcessor
{
    public static CommandOutcome Execute(ContentSet content,GameState state,AssemblyCommand command)
    {
        CommandOutcome Reject(string reason)=>CommandOutcome.Reject(state,"request_illegal",reason);
        if(!Rules.HasFleets(state.RulesVersion)||state.FindEmpire(command.Issuer) is not {Controller:ControllerKind.Human,Fleets:not null})return Reject("Finite requests require an owned human fleet campaign.");
        if(command.ExpectedTurn!=state.Turn)return Reject("Request command is stale.");
        try
        {
            var view=EmpireViewProjector.Project(content,state,command.Issuer);var context=FleetPlanningQueries.Create(content,view);
            var candidate=state.Clone();var e=candidate.FindEmpire(command.Issuer)!;var f=e.Fleets!;
            string Facts(GameState s)=>JsonSerializer.Serialize(new{Fleets=s.FindEmpire(command.Issuer)!.Fleets,Colonies=s.Colonies.Where(c=>c.EmpireId==command.Issuer)});
            string before=Facts(state);AssemblyRequest? request=null;long originalRevision=0;
            void Require(bool condition,string reason){if(!condition)throw new InvalidOperationException(reason);}
            void Check(AssemblyPreview preview,string? fingerprint=null)
            {Require(preview.Allowed,string.Join("; ",preview.Rejections));if(fingerprint is not null)Require(fingerprint==preview.Fingerprint,"Preview inputs changed; review the current finite request.");}
            void Coordinator(AssemblyRequest r,OfficerId? id)
            {
                if(r.Coordinator==id)return;
                if(r.Coordinator is {} old)
                {var officer=f.Officers.Single(o=>o.Id==old);officer.Request=null;officer.Status=OfficerStatus.Posted;if(r.HasAllocated)r.CoordinatorXpForfeited=true;}
                r.Coordinator=id;
                if(id is {} chosen){var officer=f.Officers.Single(o=>o.Id==chosen);officer.Status=OfficerStatus.Coordinating;officer.Request=r.Id;}
                if(!r.HasAllocated)r.InitialXpRecipient=id;
            }
            void Install(AssemblyRequest r,AssemblyDraft draft,AssemblyPreview preview)
            {
                var previous=r.Slots.ToDictionary(s=>s.Ordinal);var tokens=new Dictionary<int,(ColonyId Colony,int Token)>();
                foreach(var colPreview in preview.Colonies)
                {
                    var controls=candidate.FindColony(colPreview.Colony)!.Development!;var queue=new List<ProductionQueueEntry>();
                    foreach(var item in colPreview.ProposedQueue)
                    {
                        int token;if(item.Token is {} retained)token=retained;
                        else {token=controls.NextQueueToken;controls.NextQueueToken=checked(token+1);}
                        queue.Add(new(token,item.Target));
                        if(item.SlotOrdinal is {} ordinal)tokens.Add(ordinal,(colPreview.Colony,token));
                    }
                    controls.Queue=queue;
                }
                var slots=new List<AssemblySlot>();
                foreach(var plan in preview.Slots)
                {
                    if(plan.RetainedSlotOrdinal is {} ordinal){slots.Add(previous[ordinal].Clone());continue;}
                    var slot=new AssemblySlot{Ordinal=plan.Ordinal,RoleId=plan.RoleId,Vessel=plan.Vessel,Status=plan.Vessel is null?SlotStatus.Queued:SlotStatus.Built};
                    if(tokens.TryGetValue(plan.Ordinal,out var binding)){slot.Colony=binding.Colony;slot.QueueToken=binding.Token;}
                    if(r.Status==RequestStatus.SafetyPaused){slot.ResumeStatus=slot.Status;slot.Status=SlotStatus.BlockedSafety;slot.BlockedReason=r.Attention;}
                    slots.Add(slot);if(slot.Vessel is {} ship)candidate.FindVessel(ship)!.NewlyCompleted=false;
                }
                r.Slots=slots;r.NextSlotOrdinal=Math.Max(r.NextSlotOrdinal,checked(slots.Max(s=>s.Ordinal)+1));
                Coordinator(r,draft.Coordinator);r.HasAllocated=true;r.Name=draft.Name;r.Rally=draft.Rally;r.RallyFleet=draft.RallyFleet;r.ReservedFleetSlot=draft.RallyFleet is null;r.Priority=draft.Priority;r.Colonies=draft.Colonies.OrderBy(id=>id).ToList();r.PausedContributions.RemoveAll(id=>!r.Colonies.Contains(id));r.AuthorizeCargo=draft.AuthorizeCargo;
            }
            if(command.Action is AssemblyAction.Edit edit)
            {
                request=f.Requests.SingleOrDefault(r=>r.Id==edit.Request);Require(request is not null&&FleetQueries.IsActive(request.Status),"Request is unavailable or final.");Require(request!.Revision==edit.Revision,"Request revision changed; review the current request.");originalRevision=request.Revision;
            }
            switch(command.Action)
            {
                case AssemblyAction.Accept a:
                    var accepted=AssemblyPlanner.Preview(context,a.Draft);Check(accepted,a.Fingerprint);
                    var id=new RequestId(f.NextRequestId);f.NextRequestId=checked(f.NextRequestId+1);request=new(){Id=id,Status=RequestStatus.Active};f.Requests.Add(request);Install(request,a.Draft,accepted);break;
                case AssemblyAction.Pause:
                    if(request!.Status==RequestStatus.Active)request.Status=RequestStatus.Paused;break;
                case AssemblyAction.SetContributionPaused a:
                    Require(request!.Colonies.Contains(a.Colony)&&candidate.FindColony(a.Colony)?.EmpireId==e.Id,"Choose a currently participating own shipyard.");
                    if(a.Paused){if(!request.PausedContributions.Contains(a.Colony)){request.PausedContributions.Add(a.Colony);request.PausedContributions.Sort();}}else request.PausedContributions.Remove(a.Colony);
                    break;
                case AssemblyAction.Resume a:
                    var resumed=AssemblyPlanner.PreviewRetained(context,request!.Id);Check(resumed,a.Fingerprint);
                    request.Status=RequestStatus.Active;request.PausedContributions.Clear();request.Attention=request.Slots.Any(s=>s.Status==SlotStatus.Missing)?"Missing ships require an explicit finite amendment.":null;
                    foreach(var slot in request.Slots.Where(s=>s.Status==SlotStatus.BlockedSafety)){slot.Status=slot.ResumeStatus!.Value;slot.ResumeStatus=null;slot.BlockedReason=null;}break;
                case AssemblyAction.SetPriority a:
                    Require(Enum.IsDefined(a.Priority),"Choose Normal or Urgent priority.");
                    if(a.Fingerprint is not null)Check(AssemblyPlanner.PreviewRetained(context,request!.Id,priority:a.Priority),a.Fingerprint);
                    if(request!.Priority==a.Priority)break;
                    foreach(var colony in request.Colonies)
                    {
                        var controls=candidate.FindColony(colony)!.Development!;var bound=request.Slots.Where(s=>s.Colony==colony&&s.QueueToken is not null).Select(s=>s.QueueToken!.Value).ToHashSet();
                        var active=controls.Queue.Where(q=>q.Token==controls.ActiveQueueToken);var contribution=controls.Queue.Where(q=>q.Token!=controls.ActiveQueueToken&&bound.Contains(q.Token));var other=controls.Queue.Where(q=>q.Token!=controls.ActiveQueueToken&&!bound.Contains(q.Token));
                        controls.Queue=(a.Priority==RequestPriority.Urgent?active.Concat(contribution).Concat(other):active.Concat(other).Concat(contribution)).ToList();
                    }
                    request.Priority=a.Priority;break;
                case AssemblyAction.ChangeRally a:
                    var rallied=AssemblyPlanner.PreviewRetained(context,request!.Id,a.Rally,a.RallyFleet);Check(rallied,a.Fingerprint);request.Rally=a.Rally;request.RallyFleet=a.RallyFleet;request.ReservedFleetSlot=a.RallyFleet is null;
                    foreach(var slot in request.Slots.Where(s=>s.Vessel is not null))
                    {
                        var ship=candidate.FindVessel(slot.Vessel!.Value)!;
                        var progress=ship.Transit is not null?SlotStatus.Travelling:ship.StationaryStarId==a.Rally&&ship.Fleet==a.RallyFleet&&a.RallyFleet is not null?SlotStatus.Arrived:SlotStatus.Built;
                        if(slot.Status==SlotStatus.BlockedSafety)slot.ResumeStatus=progress;else slot.Status=progress;
                    }
                    break;
                case AssemblyAction.Amend a:
                    var amended=AssemblyPlanner.Preview(context,a.Draft,request!.Id);Check(amended,a.Fingerprint);Install(request,a.Draft,amended);break;
                case AssemblyAction.DetachSlot a:
                    var detached=request!.Slots.SingleOrDefault(s=>s.Ordinal==a.Ordinal);Require(detached?.Vessel is not null,"Only a living reserved ship can be detached; amend unstarted quota explicitly.");var vessel=candidate.FindVessel(detached!.Vessel!.Value)!;Require(vessel.Transit is null,"Travel is committed until arrival.");
                    if(vessel.Fleet is {} fleetId)
                    {
                        var fleet=f.Fleets.Single(x=>x.Id==fleetId);Require(fleet.Flagship!=vessel.Id||fleet.Officer is null,"Transfer or remove the admiral before detaching the flagship.");fleet.Members.Remove(vessel.Id);vessel.Fleet=null;
                        if(fleet.Members.Count==0){f.Fleets.Remove(fleet);RequestQueueAuthority.RallyDisbanded(candidate,e.Id,fleetId);}else if(fleet.Flagship==vessel.Id)fleet.Flagship=fleet.Members.Min();
                    }
                    detached.FormerVessel=detached.Vessel;detached.Vessel=null;detached.Status=SlotStatus.Missing;detached.ResumeStatus=null;detached.QueueToken=null;detached.BlockedReason="Manually detached; a finite amendment is required for replacement.";if(request.Status!=RequestStatus.SafetyPaused)request.Status=RequestStatus.Paused;break;
                case AssemblyAction.Cancel a:
                    Require(Enum.IsDefined(a.Choice),"Choose how to retain finite queued work.");
                    if(a.Choice==RequestCancellation.RemoveUnstarted)
                        foreach(var slot in request!.Slots.Where(s=>s.Colony is not null&&s.QueueToken is not null))
                        {var controls=candidate.FindColony(slot.Colony!.Value)!.Development!;if(controls.ActiveQueueToken!=slot.QueueToken)controls.Queue.RemoveAll(q=>q.Token==slot.QueueToken);}
                    foreach(var slot in request!.Slots.Where(s=>s.Status==SlotStatus.BlockedSafety)){slot.Status=slot.ResumeStatus!.Value;slot.ResumeStatus=null;slot.BlockedReason=null;}
                    Coordinator(request!,null);request!.Status=RequestStatus.Cancelled;request.ReservedFleetSlot=false;request.CoordinatorXpForfeited=true;break;
                default: throw new InvalidOperationException("Unknown finite request action.");
            }
            if(before==Facts(candidate))return CommandOutcome.NoChange(state);
            if(originalRevision!=0)request!.Revision=checked(originalRevision+1);
            string kind=command.Action.GetType().Name;
            f.PendingEvents.RemoveAll(x=>x.Turn==state.Turn&&x.Kind=="request_"+kind&&x.Request==request!.Id);
            long eventId=f.NextEventId;f.NextEventId=checked(eventId+1);f.PendingEvents.Add(new(e.Id,eventId,state.Turn,"request_"+kind,$"{request!.Name}: {kind}.",Request:request.Id));
            e.Ready=false;return new(candidate,null,true,null);
        }
        catch(Exception error)when(error is OverflowException or ArgumentException or InvalidOperationException){return Reject(error.Message);}
    }
}
