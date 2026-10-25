using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Crises;
public static class CrisisReports
{
    public static void CloseFinishedEpisodes(ContentSet content,GameState candidate,CrisisResolutionContext context)
    {
        if(!Rules.HasCrises(candidate.RulesVersion)||candidate.Crisis is not{Enabled:true})return;
        foreach(var owner in candidate.Empires.OrderBy(e=>e.Id))
        {
            if(owner.Crisis!.Episode is not {} episode)continue;
            if(candidate.Colonies.Any(c=>c.EmpireId==owner.Id&&c.Crisis?.Case?.EpisodeId==episode.Id)||candidate.Vessels.Any(v=>v.EmpireId==owner.Id&&v.Crisis?.CarrierEpisode==episode.Id))continue;
            CloseEpisode(content,candidate,owner.Id,context,cured:false,0);
        }
    }
    internal static void CloseEpisode(ContentSet content,GameState candidate,EmpireId owner,CrisisResolutionContext context,bool cured,long diverted)
    {
        var e=candidate.FindEmpire(owner)!.Crisis!;var episode=e.Episode!;var d=content.Plagues.Values.Single();
        e.NextEligibleTurn=checked(context.ToTurn+d.CooldownTurns);e.Episode=null;
        context.Events.Add(new(new(owner,episode.Id,episode.Source.Colony),context.ToTurn,episode.Source,cured?"cure_completed":"episode_closed",0,cured?"Cure completed and cleared this empire's cases and carrier flags.":"Episode ended; "+Points(episode.CureProgress)+" research of partial cure investment discarded without refund.",diverted)
        {CureCost=episode.CureCost,CureProgress=episode.CureProgress,DiscardedProgress=cured?0:episode.CureProgress});
    }
    public static void Finalize(GameState snapshot,GameState candidate,CrisisResolutionContext context)
    {
        if(!Rules.HasCrises(candidate.RulesVersion)||candidate.Crisis is not{Enabled:true})return;
        var finalized=new List<CrisisEvent>();
        foreach(var owner in candidate.Empires.OrderBy(e=>e.Id))
        {
            var e=owner.Crisis!;
            bool human=owner.Controller==ControllerKind.Human;
            // Planning actions/current facts from the previous planning period become history.
            e.CompletedDigests=e.CompletedDigests.Concat(e.PendingDigests).GroupBy(d=>d.Turn).Select(g=>g.Last()).OrderBy(d=>d.Turn).TakeLast(10).ToList();
            var pending=CrisisQueries.PendingAttention(snapshot,owner.Id).ToList();
            var rows=new List<CrisisEvent>();
            foreach(var group in context.Events.Where(x=>x.Key.Owner==owner.Id).GroupBy(x=>x.Key).OrderBy(g=>g.Key.Episode).ThenBy(g=>g.Key.Colony))
            {
                var key=group.Key;var old=snapshot.FindColony(key.Colony)?.Crisis?.Case;
                if(old?.EpisodeId!=key.Episode)old=null;
                var illness=candidate.FindColony(key.Colony)?.Crisis?.Case;
                if(illness?.EpisodeId!=key.Episode)illness=null;
                bool material=group.Any(x=>x.Material);
                long prior=old?.AttentionRevision??pending.Where(r=>r.Key==key).Select(r=>r.Revision).DefaultIfEmpty(0).Max();
                long revision=illness is not null&&old is null?illness.AttentionRevision:material?checked(prior+1):Math.Max(1,prior);
                if(revision>=long.MaxValue)throw new OverflowException("Attention identifiers exhausted.");
                if(illness is not null)
                {
                    illness.AttentionRevision=revision;
                    if(material&&human&&!e.RequireAcknowledgement)illness.WaivedRevision=revision;
                }
                foreach(var entry in group.OrderBy(x=>x.Kind,StringComparer.Ordinal))
                    rows.Add(entry with{Revision=revision,Disposition=entry.Material&&human?(e.RequireAcknowledgement?"pending":"waived"):"recorded"});
                if(material)
                {
                    var earlier=pending.FirstOrDefault(r=>r.Key==key);
                    pending.RemoveAll(r=>r.Key==key);
                    if(human&&e.RequireAcknowledgement)
                    {
                        var details=group.Where(x=>x.Material).OrderBy(x=>x.Kind,StringComparer.Ordinal).Select(x=>x.Reason).Distinct(StringComparer.Ordinal);
                        string reason=earlier?.Reason??string.Empty;
                        foreach(string detail in details)if(!reason.Contains(detail,StringComparison.Ordinal))reason=reason.Length==0?detail:reason+" "+detail;
                        pending.Add(new(key,revision,context.ToTurn,group.First().Location,reason));
                    }
                }
            }
            if(!human)pending.Clear();
            else if(!e.RequireAcknowledgement)
            {
                foreach(var receipt in pending)
                {
                    var illness=candidate.FindColony(receipt.Key.Colony)?.Crisis?.Case;
                    if(illness?.EpisodeId==receipt.Key.Episode)illness.WaivedRevision=Math.Max(illness.WaivedRevision,receipt.Revision);
                    rows.Add(new(receipt.Key,context.ToTurn,receipt.Location,"acknowledgement",receipt.Revision,receipt.Reason){Disposition="waived"});
                }
                pending.Clear();
            }
            e.PendingReceipts=pending.GroupBy(r=>r.Key).Select(g=>g.OrderByDescending(r=>r.Revision).First()).OrderBy(r=>r.Key.Episode).ThenBy(r=>r.Key.Colony).ToList();
            rows=rows.OrderBy(x=>x.Key.Episode).ThenBy(x=>x.Key.Colony).ThenBy(x=>x.Kind,StringComparer.Ordinal).ThenBy(x=>x.Revision).ToList();
            e.PendingDigests.Clear();e.PendingDigests.Add(new(context.ToTurn,rows.Select(x=>new CrisisDigestEntry(x.Key,x.Location,x.Kind,x.Revision,x.Disposition,x.ResearchDiverted,x.PopulationLost,x.Reason)).ToImmutableArray()));
            finalized.AddRange(rows);
        }
        context.Events.Clear();context.Events.AddRange(finalized);
    }
    private static string Points(long amount)
    {
        string whole=(amount/100).ToString(CultureInfo.InvariantCulture);long fraction=amount%100;
        return fraction==0?whole:whole+"."+fraction.ToString("00",CultureInfo.InvariantCulture).TrimEnd('0');
    }
}
