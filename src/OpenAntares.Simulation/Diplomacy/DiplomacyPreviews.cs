using System;
using System.Linq;
using System.Collections.Immutable;
using System.Globalization;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public sealed record DiplomacyPreview(bool Allowed,string? RejectionCode,long PublicRevision,long PrivateRevision,string Terms,HubEligibility? Hub,ImmutableArray<string> Consequences,string PolicyReason,long? BreakEvenBoundaries,bool Conditional);
public static partial class DiplomacyQueries
{
    public static DiplomacyPreview Preview(ContentSet content,EmpireView view,EmpireId other,DiplomacyAction action)
    {
        var c=view.Diplomacy?.Contacts.SingleOrDefault(c=>c.Identity.OtherEmpireId==other);var d=content.Diplomacy;
        if(c is null||d is null)return new(false,"diplomacy_unavailable",0,0,"",null,ImmutableArray<string>.Empty,"",null,false);
        var consequences=ImmutableArray.CreateBuilder<string>();HubEligibility? hub=null;bool trade=action is DiplomacyAction.OfferTrade||(action is DiplomacyAction.Answer or DiplomacyAction.RevokeAnswer or DiplomacyAction.Withdraw)&&c.Pending?.Kind==AgreementKind.Trade||action is DiplomacyAction.Terminate{Kind:AgreementKind.Trade};
        string terms="Minimum "+d.Minimum.ToString(CultureInfo.InvariantCulture)+" effective turns; "+d.Notice.ToString(CultureInfo.InvariantCulture)+" turns notice after the minimum.";
        StarId? named=action is DiplomacyAction.OfferSettlement settlement?settlement.Star:action is DiplomacyAction.Terminate{Kind:AgreementKind.Settlement}?c.Agreements.FirstOrDefault(a=>a.Kind==AgreementKind.Settlement)?.Star:!trade?c.Pending?.Star:null;
        if(named is {} id&&view.Stars.FirstOrDefault(s=>s.Chart.Id==id) is {} system)terms="System: "+system.Chart.Name+". "+terms;
        if(action is DiplomacyAction.OfferTrade or DiplomacyAction.OfferSettlement or DiplomacyAction.Answer{Accept:true})
        {terms+=FormattableString.Invariant($" If accepted now: effective at turn {(long)view.Turn+1}; effects first apply during turn {(long)view.Turn+1} to {(long)view.Turn+2}.");if(trade)terms+=FormattableString.Invariant($" First payout resolves at turn {(long)view.Turn+d.Warmup+2}.");}
        long? breakEven=null;
        if(trade){terms+=" Setup "+Points(d.SetupCost)+" production each; +"+Points(d.TradeOutput)+" production per turn after "+d.Warmup.ToString(CultureInfo.InvariantCulture)+" warmup turns.";breakEven=checked(d.Warmup+(d.SetupCost-1)/d.TradeOutput+1);consequences.Add("Your hub stays private. Setup funds are charged only if both sides can activate. No refund after activation.");if(action is DiplomacyAction.OfferTrade or DiplomacyAction.Answer{Accept:true})consequences.Add("Submitting this order reserves your setup funds immediately.");}
        else consequences.Add(action is DiplomacyAction.OfferSettlement?"Sending this accord reveals that you know the named system. Both sides freeze further founding there.":"An active settlement accord freezes further founding by both sides in its named system; existing colonies and travel remain available.");
        if(action is DiplomacyAction.Withdraw or DiplomacyAction.RevokeAnswer)consequences.Add("This releases the affected setup reservation. Revoking an answer does not extend the offer deadline.");
        if(action is DiplomacyAction.Terminate)consequences.Add("Giving notice creates cancellation memory for the other party. Effects continue through the notice period.");
        string policy=trade?"AI trade threshold: "+d.TradeThreshold.ToString(CultureInfo.InvariantCulture)+"; partner readiness is checked at resolution.":"AI settlement policy requires an own settlement to protect and the stated attitude threshold.";
        DiplomacyPreview Result(bool allowed,string? reason)=>new(allowed,allowed?null:reason,c.PublicRevision,c.PrivateRevision,terms,hub,consequences.ToImmutable(),policy,breakEven,trade);
        bool StarKnown(StarId star)=>view.Stars.Any(s=>s.Chart.Id==star&&s.Current&&s.Planets is {} planets&&planets.Any(p=>p.OwnerId==view.Viewer||p.OwnerId==other));
        switch(action)
        {
            case DiplomacyAction.OfferSettlement s:
                if(c.Pending is {} existing)return Result(existing.Proposer==view.Viewer&&existing.Kind==AgreementKind.Settlement&&existing.Star==s.Star,"offer_pending");
                return Result(!c.Agreements.Any(a=>a.Kind==AgreementKind.Settlement)&&StarKnown(s.Star),"system_not_eligible");
            case DiplomacyAction.OfferTrade t:
                var selected=view.Diplomacy!.Hubs.SingleOrDefault(h=>h.Id==t.Hub);hub=selected?.NewCommitment;
                if(c.Pending is {} pending){hub=selected?.ExistingCommitment??hub;return Result(pending.Proposer==view.Viewer&&pending.Kind==AgreementKind.Trade&&pending.OwnHub==t.Hub,"offer_pending");}
                return Result(!c.Agreements.Any(a=>a.Kind==AgreementKind.Trade)&&hub?.Allowed==true,hub?.Reason??"own_hub_required");
            case DiplomacyAction.Answer a:
                if(c.Pending is not {} o||o.Id!=a.Id||o.Recipient!=view.Viewer)return Result(false,"offer_unavailable");
                if(o.OwnAnswered)return Result(o.OwnAccept==a.Accept&&o.OwnHub==a.Hub,"revoke_answer_first");
                if(!a.Accept)return Result(a.Hub is null,"decline_has_no_hub");
                if(o.Kind==AgreementKind.Settlement)return Result(a.Hub is null&&o.Star is {} star&&StarKnown(star),"system_not_eligible");
                hub=view.Diplomacy!.Hubs.SingleOrDefault(h=>h.Id==a.Hub)?.NewCommitment;return Result(hub?.Allowed==true,hub?.Reason??"own_hub_required");
            case DiplomacyAction.RevokeAnswer r:return Result(c.Pending is {} ro&&ro.Id==r.Id&&ro.Recipient==view.Viewer,"offer_unavailable");
            case DiplomacyAction.Withdraw w:return Result(c.Pending is {} wo&&wo.Id==w.Id&&wo.Proposer==view.Viewer,"offer_unavailable");
            case DiplomacyAction.Terminate t:
                var agreement=c.Agreements.SingleOrDefault(a=>a.Kind==t.Kind);return Result(agreement is not null&&view.Turn>=agreement.FirstTerminationTurn,"minimum_term_active");
            default:return Result(false,"unsupported_diplomacy_action");
        }
    }
    private static string Points(long value)=>value%100==0?(value/100).ToString(CultureInfo.InvariantCulture):(value/100).ToString(CultureInfo.InvariantCulture)+"."+(value%100).ToString("00",CultureInfo.InvariantCulture).TrimEnd('0');
}
