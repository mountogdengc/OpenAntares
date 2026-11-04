using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.Combat;
namespace OpenAntares.Simulation.Conflict;
public sealed record ConflictReportedRound(ConflictEncounterId Encounter,EmpireId Other,StarId Star,ConflictRoundReceipt Receipt);
public sealed record ConflictReportView(EmpireId Viewer,ImmutableArray<ConflictEvent> Events,ImmutableArray<OwnConflictEncounterView> Encounters,ImmutableArray<ConflictHistoryView> History,ImmutableArray<OwnBlockadeView> Blockades,ImmutableArray<OwnBlockadePressure> OwnPressure,ImmutableArray<ConflictReportedRound> Rounds);
public static class ConflictReports
{
    public static string DescribeRound(ConflictRoundReceipt receipt)
    {
        var lines=receipt.Shots.Select(shot=>
        {
            var calculation=receipt.Calculations.SingleOrDefault(c=>c.Source.Value==shot.Source.Id&&c.Target.Value==shot.Target.Id);
            string math=calculation is null?"damage after damaged attack and armor":$"max(1, attack {calculation.BaseAttack} × {calculation.FirePercent}% rounded down − armor {calculation.TargetArmor})";
            return $"Ship {shot.Source.Id} → armed ship {shot.Target.Id}: {shot.Damage} damage; {math}.";
        }).ToList();
        lines.AddRange(receipt.Own.Select(h=>$"Your ship {h.Id.Value}: {h.Before} → {h.After}; actual hull loss {h.ActualLoss}."));
        lines.AddRange(receipt.Opposing.Select(h=>$"Observed opposing ship {h.Id.Value}: {h.Before} → {h.After}; actual hull loss {h.ActualLoss}."));
        lines.Add("Frozen targets; simultaneous shots still fire from destroyed actors. Loss is capped at the target's hull; excess damage has no spill or retargeting.");
        return string.Join("\n",lines);
    }
    internal static ImmutableArray<ConflictReportView> Finalize(ContentSet content,GameState snapshot,GameState candidate,ConflictResolutionContext context)
    {
        var conflict=candidate.Conflict!;var priorBlockades=ConflictBlockades.Capture(content,snapshot);var currentBlockades=ConflictBlockades.Capture(content,candidate);
        foreach(var owner in conflict.Owners)
        {
            long first=snapshot.Conflict!.Owners.Single(o=>o.Owner==owner.Owner).NextEventId;
            // Early resolution records occur before the clock advances. Their publication date is this boundary.
            for(int i=0;i<owner.Events.Count;i++)if(owner.Events[i].Id>=first)owner.Events[i]=owner.Events[i] with{Turn=candidate.Turn};
            foreach(var encounter in conflict.Encounters.Where(e=>e.Pair.Contains(owner.Owner)))
            {
                var side=encounter.LowSide.Owner==owner.Owner?encounter.LowSide:encounter.HighSide;string key="battle-response/"+encounter.Id.Value;
                owner.Notices.RemoveAll(n=>n.Key==key);
                if(encounter.Phase!=ConflictPhase.Completed&&side.NeedsResponse)owner.Notices.Add(new(key,ConflictNoticeLevel.Emergency,"Your revised force needs an explicit response before Ready.",encounter.Pair.Other(owner.Owner),encounter.Star,side.Fleet,encounter.Id,side.ConsentRevision));
                bool fresh=snapshot.Conflict.Encounters.All(e=>e.Id!=encounter.Id);
                if(fresh)Notice("battle-danger/"+encounter.Id.Value,"A new armed encounter threatens your participating force.",encounter);
                if(encounter.Phase!=ConflictPhase.Completed||owner.History.Any(h=>h.Encounter==encounter.Id))continue;
                var departure=context.Departures.FirstOrDefault(d=>d.Encounter==encounter.Id&&d.Owner==owner.Owner);
                string outcome=side.Members.Count==0?"Your armed force was lost":context.AcceptedPeace.Contains(encounter.Pair)?"Peace ended the battle":departure is {Destination:null}?"Local Break contact":departure is not null?"Your force withdrew":"The encounter ended";
                owner.History.Add(new(){Encounter=encounter.Id,Other=encounter.Pair.Other(owner.Owner),Star=encounter.Star,StartedTurn=encounter.CreatedTurn,EndedTurn=encounter.CompletedTurn??candidate.Turn,Outcome=outcome,LostMembers=side.DestroyedMembers.ToList(),Rounds=encounter.Rounds.Select(r=>owner.Owner==encounter.Pair.Low?r:r with{Own=r.Opposing,Opposing=r.Own}).ToList()});
            }
            foreach(var loss in context.Destructions.Where(d=>d.Owner==owner.Owner))
            {var encounter=conflict.Encounters.Single(e=>e.Id==loss.Encounter);Notice("battle-loss/"+loss.Encounter.Value+"/"+loss.Vessel.Value,"An armed ship was destroyed. Review survivors, finite repair and officer service records.",encounter);}
            var previous=priorBlockades.Orders.Where(b=>b.Owner==owner.Owner).ToArray();var current=currentBlockades.Orders.Where(b=>b.Owner==owner.Owner).ToArray();
            foreach(var row in previous.Concat(current).DistinctBy(b=>b.Fleet))
            {
                var before=previous.SingleOrDefault(b=>b.Fleet==row.Fleet);var after=current.SingleOrDefault(b=>b.Fleet==row.Fleet);
                if(before?.Status==after?.Status)continue;
                ConflictRelations.RecordEvent(candidate,owner.Owner,row.Other,"blockade_changed",after is null?"Your blockade ended.":"Your blockade status is now "+after.Status+"; eligible from turn "+after.EffectiveTurn+".",row.Star,attention:true);
            }
            var oldPressure=priorBlockades.Pressure.Where(p=>p.Victim==owner.Owner).ToArray();var newPressure=currentBlockades.Pressure.Where(p=>p.Victim==owner.Owner).ToArray();
            foreach(var p in oldPressure.Concat(newPressure).DistinctBy(p=>p.Star))if(oldPressure.Any(x=>x.Star==p.Star)!=newPressure.Any(x=>x.Star==p.Star))ConflictRelations.RecordEvent(candidate,owner.Owner,p.Blockader,"pressure_changed",newPressure.Any(x=>x.Star==p.Star)?"Blockade pressure now reduces ordinary net production at your colony system before trade.":"Blockade pressure at your colony system ended.",p.Star,attention:true);
            foreach(var p in oldPressure.Where(p=>context.AcceptedPeace.Contains(p.Pair)))ConflictRelations.RecordEvent(candidate,owner.Owner,p.Blockader,"peace_no_charge","Accepted peace stopped this interval's blockade charge. No final volley or healing was applied.",p.Star);
            owner.History=owner.History.OrderBy(h=>h.EndedTurn).ThenBy(h=>h.Encounter).TakeLast(content.Conflict!.HistoryLimit).ToList();
            if(owner.Notices.Count>256)owner.Notices=owner.Notices.OrderBy(n=>n.Level==ConflictNoticeLevel.Emergency?1:0).TakeLast(256).ToList();
            void Notice(string key,string reason,ConflictEncounterState e){if(owner.Notices.Any(n=>n.Key==key))return;var side=e.LowSide.Owner==owner.Owner?e.LowSide:e.HighSide;owner.Notices.Add(new(key,ConflictNoticeLevel.Attention,reason,e.Pair.Other(owner.Owner),e.Star,side.Fleet,e.Id));}
        }
        var retain=conflict.Encounters.Where(e=>e.Phase==ConflictPhase.Completed).OrderBy(e=>e.CompletedTurn).ThenBy(e=>e.Id).TakeLast(content.Conflict!.HistoryLimit).Select(e=>e.Id).ToHashSet();
        conflict.Encounters.RemoveAll(e=>e.Phase==ConflictPhase.Completed&&!retain.Contains(e.Id));
        return conflict.Owners.OrderBy(o=>o.Owner).Select(owner=>
        {
            var view=ConflictQueries.Project(content,candidate,owner.Owner);
            var rounds=context.Rounds.Where(r=>r.Frame.Pair.Contains(owner.Owner)).Select(r=>new ConflictReportedRound(r.Frame.Encounter,r.Frame.Pair.Other(owner.Owner),r.Frame.Star,ConflictExchange.Receipt(r.Frame,r.Result,r.Interception) with{Own=owner.Owner==r.Frame.Pair.Low?r.Result.Low:r.Result.High,Opposing=owner.Owner==r.Frame.Pair.Low?r.Result.High:r.Result.Low})).ToImmutableArray();
            return new ConflictReportView(owner.Owner,owner.Events.Where(e=>e.Turn==candidate.Turn).ToImmutableArray(),view.Encounters,view.History,view.Blockades,view.OwnPressure,rounds);
        }).ToImmutableArray();
    }
    internal static void RetainFinalObservations(ContentSet content,GameState candidate,ConflictResolutionContext context)
    {
        foreach(var round in context.Rounds)
        foreach(var viewer in new[]{round.Frame.Pair.Low,round.Frame.Pair.High})
        {
            var owner=candidate.Conflict!.Owners.Single(o=>o.Owner==viewer);
            var rows=viewer==round.Frame.Pair.Low?round.Result.High:round.Result.Low;
            foreach(var damage in rows)
            {
                int index=owner.Observations.FindIndex(o=>o.Vessel==damage.Id);
                if(damage.After==0){if(index>=0)owner.Observations.RemoveAt(index);continue;}
                if(index<0||owner.Observations[index].Current)continue; // Fresh visible facts may include newly published research.
                var before=context.Snapshot.FindVessel(damage.Id);if(before is null)continue;
                var load=VesselLoadouts.Resolve(content,context.Snapshot,before);var profile=load.Profile!;
                owner.Observations[index]=owner.Observations[index] with{Turn=candidate.Turn,Current=false,Hull=damage.After,Speed=CombatMath.Stats(profile,damage.After,load.BaseSpeed,0,content.Combat).Speed};
            }
        }
    }
}
