using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static partial class DiplomacyResolver
{
    public static ImmutableArray<DiplomacyEvent> ResolveOffers(ContentSet content,GameState snapshot,GameState candidate)
    {
        var events=ImmutableArray.CreateBuilder<DiplomacyEvent>();
        if(!Rules.HasDiplomacy(snapshot.RulesVersion)||candidate.Diplomacy is null)return events.ToImmutable();
        var d=content.Diplomacy!;int next=checked(snapshot.Turn+1);
        foreach(var pair in candidate.Diplomacy.Pairs.OrderBy(p=>p.Pair))
        {
            if(pair.Pending is not {} offer)continue;
            if(offer.Withdraw){Close("withdrawn",false);continue;}
            if(offer.Answer is {} answer)
            {
                if(!answer.Accept){Close("declined",false);continue;}
                bool allowed;
                if(offer.Kind==AgreementKind.Settlement)
                    allowed=offer.Star is {} star&&DiplomacyCommandProcessor.CanObserveAccord(candidate,offer.Proposer,offer.Recipient,star)&&DiplomacyCommandProcessor.CanObserveAccord(candidate,offer.Recipient,offer.Proposer,star);
                else allowed=offer.ProposerHub is {} ph&&answer.Hub is {} rh&&DiplomacyHubQueries.Eligibility(content,candidate,offer.Proposer,ph.Hub,offer.Id).Allowed&&DiplomacyHubQueries.Eligibility(content,candidate,offer.Recipient,rh.Hub,offer.Id).Allowed;
                if(!allowed){Close("failed",false);continue;}
                var agreement=new DiplomacyAgreement{Kind=offer.Kind,Star=offer.Star,ActivatedTurn=next};
                if(offer.Kind==AgreementKind.Trade)
                {
                    var proposer=candidate.FindColony(offer.ProposerHub!.Hub)!;var recipient=candidate.FindColony(answer.Hub!.Hub)!;
                    proposer.ProductionReserve=checked(proposer.ProductionReserve-d.SetupCost);recipient.ProductionReserve=checked(recipient.ProductionReserve-d.SetupCost);
                    var first=new HubOrigin(proposer.Id,offer.Proposer,next);var second=new HubOrigin(recipient.Id,offer.Recipient,next);
                    agreement.LowHub=offer.Proposer==pair.Pair.Low?first:second;agreement.HighHub=offer.Proposer==pair.Pair.High?first:second;
                }
                pair.Agreements.Add(agreement);pair.Agreements.Sort((a,b)=>a.Kind.CompareTo(b.Kind));Close("activated",true);continue;
            }
            if(offer.ExpiryTurn<=next)Close("expired",false);
            void Close(string result,bool activated)
            {
                pair.Pending=null;pair.PublicRevision=checked(pair.PublicRevision+1);
                if(!activated)pair.Parties.Single(p=>p.Owner==offer.Proposer).NextAiOfferTurn=checked(next+d.Cooldown);
                foreach(var party in pair.Parties)
                {
                    party.PrivateRevision=checked(party.PrivateRevision+1);
                    string prefix=offer.Kind==AgreementKind.Trade?"trade_":"settlement_";
                    var hub=party.Owner==offer.Proposer?offer.ProposerHub:offer.Answer?.Hub;
                    var e=new DiplomacyEvent(party.Owner,pair.Pair.Other(party.Owner),next,prefix+result,offer.Star,hub?.Hub,activated?"Agreement activates next planning turn.":result=="failed"?"Agreement could not activate.":offer.Answer?.PolicyReason??("Offer "+result+".")){Paid=activated&&offer.Kind==AgreementKind.Trade?d.SetupCost:0};
                    pair.LastResults.RemoveAll(x=>x.Viewer==party.Owner&&x.Kind.StartsWith(prefix,StringComparison.Ordinal));pair.LastResults.Add(e);events.Add(e);
                }
            }
        }
        return events.ToImmutable();
    }
}
