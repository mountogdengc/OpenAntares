using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public static class ConflictTreatyEffects
{
    internal static void Prepare(ContentSet content,GameState snapshot,GameState candidate,ConflictResolutionContext context)
    {
        foreach(var war in context.NewWars)
        {
            var pair=candidate.Diplomacy!.Pairs.Single(p=>p.Pair==war.Pair);
            if(pair.Pending is {} offer)
            {
                pair.Pending=null;pair.PublicRevision=checked(pair.PublicRevision+1);
                foreach(var party in pair.Parties)
                {
                    party.PrivateRevision=checked(party.PrivateRevision+1);
                    if(party.Owner==offer.Proposer)party.NextAiOfferTurn=(int)Math.Min(int.MaxValue,(long)snapshot.Turn+1+content.Diplomacy!.Cooldown);
                    DiplomacyResolver.Record(pair,new(party.Owner,pair.Pair.Other(party.Owner),checked(snapshot.Turn+1),offer.Kind==AgreementKind.Trade?"trade_failed":"settlement_failed",offer.Star,party.Owner==offer.Proposer?offer.ProposerHub?.Hub:offer.Answer?.Hub?.Hub,"War superseded the pending agreement; setup reservations were released without payment."));
                }
            }
            // Supersede only notices from this unpublished planning boundary. Prior memory stays intact.
            foreach(var agreement in pair.Agreements)
            {
                if(agreement.LowNoticeTurn==snapshot.Turn)agreement.LowNoticeTurn=null;
                if(agreement.HighNoticeTurn==snapshot.Turn)agreement.HighNoticeTurn=null;
                var notices=new[]{agreement.LowNoticeTurn,agreement.HighNoticeTurn}.Where(t=>t.HasValue).Select(t=>(long)t!.Value+content.Diplomacy!.Notice).ToArray();
                agreement.EndTurn=notices.Length==0?null:checked((int)notices.Min());
            }
        }
    }
    internal static void Finalize(ContentSet content,GameState candidate,ConflictResolutionContext context)
    {
        foreach(var war in context.NewWars)
        {
            var pair=candidate.Diplomacy!.Pairs.Single(p=>p.Pair==war.Pair);
            foreach(var agreement in pair.Agreements)
            foreach(var party in pair.Parties)DiplomacyResolver.Record(pair,new(party.Owner,pair.Pair.Other(party.Owner),candidate.Turn,agreement.Kind==AgreementKind.Trade?"trade_ended":"settlement_ended",agreement.Star,(party.Owner==pair.Pair.Low?agreement.LowHub:agreement.HighHub)?.Hub,"War ended the agreement at publication; no setup refund or cancellation penalty applies."));
            pair.Agreements.Clear();foreach(var party in pair.Parties){party.Attitude.Streak=0;party.PrivateRevision=checked(party.PrivateRevision+1);}pair.PublicRevision=checked(pair.PublicRevision+1);
        }
        DiplomacyReconciliation.AfterCommand(content,candidate);
    }
}
