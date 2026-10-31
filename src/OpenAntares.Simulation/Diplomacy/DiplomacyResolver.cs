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
            events.AddRange(pair.LastResults.Where(e=>e.Turn==snapshot.Turn&&e.Kind.EndsWith("_withdrawn",StringComparison.Ordinal)));
            if(pair.Pending is not {} offer)continue;
            string prefix=offer.Kind==AgreementKind.Trade?"trade_":"settlement_";string? proposerFailure=null,recipientFailure=null;
            if(offer.Withdraw){Close("withdrawn",false);continue;}
            if(offer.Answer is {} answer)
            {
                if(!answer.Accept){Close("declined",false);continue;}
                if(offer.Kind==AgreementKind.Settlement)
                {
                    if(offer.Star is not {} star||!DiplomacyCommandProcessor.CanObserveAccord(candidate,offer.Proposer,offer.Recipient,star))proposerFailure="Your current observations no longer support this accord.";
                    if(offer.Star is not {} target||!DiplomacyCommandProcessor.CanObserveAccord(candidate,offer.Recipient,offer.Proposer,target))recipientFailure="Your current observations no longer support this accord.";
                }
                else
                {
                    proposerFailure=Failure(offer.Proposer,offer.ProposerHub);recipientFailure=Failure(offer.Recipient,answer.Hub);
                }
                if(proposerFailure is not null||recipientFailure is not null){Close("failed",false);continue;}
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
            if(offer.ExpiryTurn<=next){Close("expired",false);continue;}
            if((offer.CreatedTurn==snapshot.Turn||offer.CreatedTurn==next)&&!pair.LastResults.Any(e=>e.Viewer==offer.Recipient&&e.Kind==prefix+"offered"&&e.Turn>=offer.CreatedTurn))
            {
                var incoming=new DiplomacyEvent(offer.Recipient,offer.Proposer,next,prefix+"offered",offer.Star,null,FormattableString.Invariant($"Incoming offer; answer by the resolution to turn {offer.ExpiryTurn}. No answer is required to Ready."));Record(pair,incoming);events.Add(incoming);
            }
            string? Failure(EmpireId owner,HubCommitment? hub)
            {
                var eligibility=hub is null?null:DiplomacyHubQueries.Eligibility(content,candidate,owner,hub.Hub,offer.Id);
                if(eligibility?.Allowed==true)return null;
                return eligibility?.Reason switch{"hub_not_productive"=>"Your hub has no ordinary positive net production.","hub_setup_unfunded"=>"Your hub no longer has the required setup funds.","hub_refitting"=>"Your hub is refitting.","hub_assembly"=>"Your hub is committed to active assembly.","hub_already_committed"=>"Your hub is reserved for another offer.",_=>"Your authorized hub is no longer available."};
            }
            void Close(string result,bool activated)
            {
                pair.Pending=null;pair.PublicRevision=checked(pair.PublicRevision+1);
                if(!activated)pair.Parties.Single(p=>p.Owner==offer.Proposer).NextAiOfferTurn=checked(next+d.Cooldown);
                foreach(var party in pair.Parties)
                {
                    party.PrivateRevision=checked(party.PrivateRevision+1);var hub=party.Owner==offer.Proposer?offer.ProposerHub:offer.Answer?.Hub;
                    string reason=activated?FormattableString.Invariant($"Effective at turn {next}; effects first apply during turn {next} to {(long)next+1}.")+(offer.Kind==AgreementKind.Trade?FormattableString.Invariant($" First payout resolves at turn {(long)next+d.Warmup+1}."):""):result=="failed"?(party.Owner==offer.Proposer?proposerFailure:recipientFailure)??"Agreement could not activate; the partner was not ready.":offer.Answer?.PolicyReason??("Offer "+result+".");
                    var e=new DiplomacyEvent(party.Owner,pair.Pair.Other(party.Owner),next,prefix+result,offer.Star,hub?.Hub,reason){Paid=activated&&offer.Kind==AgreementKind.Trade?d.SetupCost:0};Record(pair,e);events.Add(e);
                }
            }
        }
        return events.ToImmutable();
    }
}
