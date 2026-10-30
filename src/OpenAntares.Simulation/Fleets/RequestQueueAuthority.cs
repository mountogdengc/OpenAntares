using System;
using System.Linq;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Simulation.Fleets;
public static class RequestQueueAuthority
{
    // Helpers mutate only the enclosing transaction's private candidate.
    public static void SafetyPause(GameState candidate,EmpireId owner,RequestId request,string reason)
    {
        var r=candidate.FindEmpire(owner)!.Fleets!.Requests.Single(r=>r.Id==request);
        if(!FleetQueries.IsActive(r.Status)||r.Status==RequestStatus.SafetyPaused&&r.Attention==reason)return;
        r.Status=RequestStatus.SafetyPaused;r.Attention=reason;r.Revision=checked(r.Revision+1);
        foreach(var slot in r.Slots.Where(s=>s.Status!=SlotStatus.Missing))
        {if(slot.Status!=SlotStatus.BlockedSafety)slot.ResumeStatus=slot.Status;slot.Status=SlotStatus.BlockedSafety;slot.BlockedReason=reason;}
    }
    public static int LastAuthorizedQueueIndex(EmpireView view,ColonyId colony)
    {
        if(!Rules.HasFleets(view.RulesVersion)||view.Fleets is not {} f)return -1;
        var queue=view.Colonies.Single(c=>c.Id==colony).Development!.Queue;int last=-1;
        foreach(var r in f.Requests.Where(r=>r.Status==RequestStatus.Active&&r.Colonies.Contains(colony)&&!r.PausedContributions.Contains(colony)))
            foreach(var slot in r.Slots.Where(s=>s.Colony==colony&&s.Status==SlotStatus.Queued&&s.QueueToken is not null))
                for(int i=0;i<queue.Length;i++)if(queue[i].Token==slot.QueueToken)last=Math.Max(last,i);
        return last;
    }
    public static bool MaySelect(EmpireView view,ColonyId colony,int token)
    {
        if(!Rules.HasFleets(view.RulesVersion)||view.Fleets is not {} f)return true;
        var r=f.Requests.FirstOrDefault(r=>FleetQueries.IsActive(r.Status)&&r.Slots.Any(s=>s.Colony==colony&&s.QueueToken==token));
        return r is null||r.Status==RequestStatus.Active&&!r.PausedContributions.Contains(colony);
    }
    internal static bool MayExecute(Content.ContentSet content,GameState state,ColonyId colony,int? token)
    {
        if(token is null||!Rules.HasFleets(state.RulesVersion))return false;
        var col=state.FindColony(colony)!;var view=EmpireViewProjector.ProjectBase(content,state,col.EmpireId);int last=LastAuthorizedQueueIndex(view,colony);
        int position=col.Development!.Queue.FindIndex(q=>q.Token==token);
        return position>=0&&position<=last&&MaySelect(view,colony,token.Value);
    }
    internal static void ManualTakeover(GameState candidate,ColonyId colony)
    {
        var col=candidate.FindColony(colony)!;
        if(!Rules.HasFleets(candidate.RulesVersion)||candidate.FindEmpire(col.EmpireId)!.Fleets is not {} f)return;
        foreach(var r in f.Requests.Where(r=>FleetQueries.IsActive(r.Status)&&r.Colonies.Contains(colony)&&!r.PausedContributions.Contains(colony)))
        {r.PausedContributions.Add(colony);r.PausedContributions.Sort();r.Revision=checked(r.Revision+1);}
    }
    internal static void ReconcileManualQueues(GameState before,GameState candidate,EmpireId owner)
    {
        if(!Rules.HasFleets(candidate.RulesVersion))return;
        foreach(var col in candidate.Colonies.Where(c=>c.EmpireId==owner))
        {
            var original=before.FindColony(col.Id)!;
            if(original.ProjectId!=col.ProjectId||original.ProjectKind!=col.ProjectKind)ManualTakeover(candidate,col.Id);
            foreach(var r in candidate.FindEmpire(owner)!.Fleets!.Requests.Where(r=>FleetQueries.IsActive(r.Status)))
            {
                bool changed=false;
                foreach(var slot in r.Slots.Where(s=>s.Colony==col.Id&&s.QueueToken is not null))
                    if(!col.Development!.Queue.Any(q=>q.Token==slot.QueueToken&&q.Target==AssemblyDesigns.Target(slot.RoleId,slot.Design)))
                    {slot.QueueToken=null;slot.Status=SlotStatus.Missing;slot.ResumeStatus=null;slot.BlockedReason="Queue entry changed manually; finite amendment required.";changed=true;}
                if(changed){if(!r.PausedContributions.Contains(col.Id))r.PausedContributions.Add(col.Id);r.Revision=checked(r.Revision+1);}
            }
        }
    }
    internal static void RallyDisbanded(GameState candidate,EmpireId owner,FleetId fleet)
    {
        foreach(var r in candidate.FindEmpire(owner)!.Fleets!.Requests.Where(r=>FleetQueries.IsActive(r.Status)&&r.RallyFleet==fleet))
        {r.RallyFleet=null;r.ReservedFleetSlot=true;if(r.Status!=RequestStatus.SafetyPaused)r.Status=RequestStatus.Paused;r.Attention="Rally fleet disbanded; explicitly review and resume the replacement rally.";r.Revision=checked(r.Revision+1);}
    }
}
