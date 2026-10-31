using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static class DiplomacyAttitudes
{
    public static int Calculate(DiplomacyDefinition d,int streak,int? lastNotice,int turn)=>checked(d.StreakStep*Math.Min(d.StreakCap,streak)-(lastNotice is {} t&&(long)turn-t<d.MemoryLifetime?d.CancellationPenalty:0));
    public static string Label(int score)=>score<0?"Strained":score<10?"Neutral":score<20?"Receptive":"Cooperative";
}
public static partial class DiplomacyResolver
{
    public static DiplomacyFrame PrepareEffective(ContentSet content,GameState snapshot,GameState candidate,DiplomacyFrame frame)
    {
        if(candidate.Diplomacy is null)return frame;int next=checked(snapshot.Turn+1);
        foreach(var p in candidate.Diplomacy.Pairs)
        foreach(var a in p.Agreements.Where(a=>a.Kind==AgreementKind.Trade&&a.ActivatedTurn<=snapshot.Turn).ToArray())
        {
            bool Missing(HubOrigin? h,EmpireId owner)=>h is null||candidate.FindColony(h.Hub) is not {} c||c.EmpireId!=owner;
            bool lostLow=Missing(a.LowHub,p.Pair.Low),lostHigh=Missing(a.HighHub,p.Pair.High);
            if(!lostLow&&!lostHigh)continue;p.Agreements.Remove(a);p.PublicRevision=checked(p.PublicRevision+1);
            foreach(var party in p.Parties)
            {
                bool low=party.Owner==p.Pair.Low;var own=low?a.LowHub:a.HighHub;bool lost=low?lostLow:lostHigh;
                var e=new DiplomacyEvent(party.Owner,p.Pair.Other(party.Owner),next,"trade_ended",null,own?.Hub,lost?"Your exchange hub was lost; trade ended without a refund or penalty.":"Trade ended: a partner's exchange link was lost.");
                Record(p,e);
            }
        }
        return frame with{Agreements=frame.Agreements.Where(a=>candidate.Diplomacy.Pairs.Any(p=>p.Pair==a.Pair&&p.Agreements.Any(x=>x.Kind==a.Kind))).ToImmutableArray()};
    }
    public static ImmutableArray<DiplomacyEvent> Finalize(ContentSet content,GameState snapshot,GameState candidate,DiplomacyFrame frame)
    {
        var events=ImmutableArray.CreateBuilder<DiplomacyEvent>();if(candidate.Diplomacy is null)return events.ToImmutable();var d=content.Diplomacy!;int next=checked(snapshot.Turn+1);
        if(candidate.Turn!=next)throw new InvalidOperationException("Diplomacy finalization requires the single advanced turn.");
        foreach(var p in candidate.Diplomacy.Pairs)
        {
            bool changed=false;bool cooperative=frame.Agreements.Any(a=>a.Pair==p.Pair);
            foreach(var party in p.Parties)
            {int after=cooperative?Math.Min(d.StreakCap,checked(party.Attitude.Streak+1)):0;if(after!=party.Attitude.Streak){party.Attitude.Streak=after;changed=true;}}
            foreach(var a in p.Agreements.ToArray())
            {
                foreach(var issuer in p.Parties.Where(x=>(x.Owner==p.Pair.Low?a.LowNoticeTurn:a.HighNoticeTurn)==snapshot.Turn))
                {
                    p.Parties.Single(x=>x.Owner!=issuer.Owner).Attitude.LastCounterpartNoticeTurn=next;changed=true;
                    foreach(var viewer in p.Parties)
                    {var e=Event(viewer.Owner,"notice","Termination notice resolved; agreement ends at turn "+a.EndTurn+".");Record(p,e);events.Add(e);}
                }
                if(a.EndTurn is {} end&&end<=next)
                {
                    p.Agreements.Remove(a);changed=true;
                    foreach(var viewer in p.Parties){var e=Event(viewer.Owner,"ended","Agreement ended after its notice period.");Record(p,e);events.Add(e);}
                }
                DiplomacyEvent Event(EmpireId viewer,string kind,string reason)=>new(viewer,p.Pair.Other(viewer),next,(a.Kind==AgreementKind.Trade?"trade_":"settlement_")+kind,a.Star,(viewer==p.Pair.Low?a.LowHub:a.HighHub)?.Hub,reason);
            }
            if(changed)p.PublicRevision=checked(p.PublicRevision+1);
        }
        DiplomacyReconciliation.AfterCommand(content,candidate);
        return events.ToImmutable();
    }
    private static void Record(DiplomacyPairState p,DiplomacyEvent e)
    {string prefix=e.Kind.StartsWith("trade_",StringComparison.Ordinal)?"trade_":"settlement_";p.LastResults.RemoveAll(x=>x.Viewer==e.Viewer&&x.Kind.StartsWith(prefix,StringComparison.Ordinal));p.LastResults.Add(e);}
}
