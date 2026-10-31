using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static class DiplomacyCommandProcessor
{
    public static CommandOutcome Execute(ContentSet content,GameState state,DiplomacyCommand command)
    {
        CommandOutcome Reject(string code="diplomacy_unavailable")=>CommandOutcome.Reject(state,code,"Diplomacy action is unavailable from the current view.");
        if(!Rules.HasDiplomacy(state.RulesVersion)||content.Diplomacy is not {} d||state.Diplomacy is null||command.Action is null||command.ExpectedTurn!=state.Turn||command.Issuer==command.Other)return Reject();
        var key=DiplomacyPair.For(command.Issuer,command.Other);var original=state.Diplomacy.Pairs.SingleOrDefault(p=>p.Pair==key);
        if(original is null||state.FindEmpire(command.Issuer) is not {} issuer||!issuer.Contacts.Any(c=>c.OtherEmpireId==command.Other))return Reject();
        if(state.Conflict?.Pairs.Any(p=>p.Pair==key&&p.Relation==Conflict.RelationKind.War)==true)return Reject("diplomacy_at_war");
        var own=original.Parties.Single(p=>p.Owner==command.Issuer);
        if(command.PublicRevision!=original.PublicRevision||command.PrivateRevision!=DiplomacyTokens.PrivateRevision(content,state,command.Issuer,command.Other))return Reject("diplomacy_stale");
        try
        {
            var candidate=state.Clone();var p=candidate.Diplomacy!.Pairs.Single(p=>p.Pair==key);var party=p.Parties.Single(x=>x.Owner==command.Issuer);bool publicChange=true;
            bool HubAllowed(ColonyId hub)=>DiplomacyHubQueries.Eligibility(content,state,command.Issuer,hub,null).Allowed;
            switch(command.Action)
            {
                case DiplomacyAction.OfferTrade trade:
                    if(p.Pending is {} old){if(old.Proposer==command.Issuer&&old.Kind==AgreementKind.Trade&&old.ProposerHub?.Hub==trade.Hub&&!old.Withdraw)return CommandOutcome.NoChange(state);return Reject();}
                    if(p.Agreements.Any(a=>a.Kind==AgreementKind.Trade)||!HubAllowed(trade.Hub))return Reject();
                    p.Pending=new(){Id=new(p.NextOfferId),Proposer=command.Issuer,Recipient=command.Other,Kind=AgreementKind.Trade,CreatedTurn=state.Turn,ExpiryTurn=checked(state.Turn+d.OfferLifetime),ProposerHub=new(trade.Hub,d.SetupCost)};p.NextOfferId=checked(p.NextOfferId+1);break;
                case DiplomacyAction.OfferSettlement accord:
                    if(p.Pending is {} oldAccord){if(oldAccord.Proposer==command.Issuer&&oldAccord.Kind==AgreementKind.Settlement&&oldAccord.Star==accord.Star&&!oldAccord.Withdraw)return CommandOutcome.NoChange(state);return Reject();}
                    if(p.Agreements.Any(a=>a.Kind==AgreementKind.Settlement)||!CanObserveAccord(state,command.Issuer,command.Other,accord.Star))return Reject();
                    p.Pending=new(){Id=new(p.NextOfferId),Proposer=command.Issuer,Recipient=command.Other,Kind=AgreementKind.Settlement,Star=accord.Star,CreatedTurn=state.Turn,ExpiryTurn=checked(state.Turn+d.OfferLifetime)};p.NextOfferId=checked(p.NextOfferId+1);break;
                case DiplomacyAction.Answer answer:
                    if(p.Pending is not {} pending||pending.Id!=answer.Id||pending.Recipient!=command.Issuer||pending.Withdraw)return Reject();
                    if(pending.Answer is {} existing){if(existing.Accept==answer.Accept&&existing.Hub?.Hub==answer.Hub)return CommandOutcome.NoChange(state);return Reject();}
                    if(!answer.Accept&&answer.Hub is not null)return Reject();
                    if(answer.Accept)
                    {
                        if(pending.Kind==AgreementKind.Trade&&(answer.Hub is not {} hub||!HubAllowed(hub)))return Reject();
                        if(pending.Kind==AgreementKind.Settlement&&(answer.Hub is not null||pending.Star is not {} star||!CanObserveAccord(state,command.Issuer,command.Other,star)))return Reject();
                    }
                    pending.Answer=new(){Accept=answer.Accept,Hub=answer.Accept&&pending.Kind==AgreementKind.Trade?new(answer.Hub!.Value,d.SetupCost):null};
                    if(issuer.Controller==ControllerKind.Ai)
                        pending.Answer.PolicyReason=answer.Accept?(pending.Kind==AgreementKind.Trade?"A record of cooperation makes trade worthwhile.":"This protects our settlement."):pending.Kind==AgreementKind.Settlement&&pending.Star is {} offered&&!state.Colonies.Any(c=>c.EmpireId==issuer.Id&&state.FindPlanet(c.PlanetId)?.StarId==offered)?"We have no settlement to protect in that system.":"We are not ready for this agreement.";
                    publicChange=false;break;
                case DiplomacyAction.RevokeAnswer revoke:
                    if(p.Pending is not {} rev||rev.Id!=revoke.Id||rev.Recipient!=command.Issuer||rev.Withdraw)return Reject();
                    if(rev.Answer is null)return CommandOutcome.NoChange(state);rev.Answer=null;publicChange=false;break;
                case DiplomacyAction.Withdraw withdraw:
                    if(p.Pending is not {} w||w.Id!=withdraw.Id||w.Proposer!=command.Issuer)return Reject();
                    p.Pending=null;party.NextAiOfferTurn=checked(state.Turn+d.Cooldown);
                    foreach(var signatory in p.Parties){var ownHub=signatory.Owner==w.Proposer?w.ProposerHub?.Hub:w.Answer?.Hub?.Hub;DiplomacyResolver.Record(p,new(signatory.Owner,p.Pair.Other(signatory.Owner),state.Turn,(w.Kind==AgreementKind.Trade?"trade_":"settlement_")+"withdrawn",w.Star,ownHub,"Offer withdrawn; any setup reservations released."));}
                    break;
                case DiplomacyAction.Terminate terminate:
                    var active=p.Agreements.SingleOrDefault(a=>a.Kind==terminate.Kind);if(active is null||state.Turn-active.ActivatedTurn<d.Minimum)return Reject();
                    bool low=command.Issuer==key.Low;if((low?active.LowNoticeTurn:active.HighNoticeTurn) is not null)return CommandOutcome.NoChange(state);
                    if(low)active.LowNoticeTurn=state.Turn;else active.HighNoticeTurn=state.Turn;
                    active.EndTurn=active.EndTurn is {} end?Math.Min(end,checked(state.Turn+d.Notice)):checked(state.Turn+d.Notice);publicChange=false;break;
                default:return Reject();
            }
            party.PrivateRevision=checked(party.PrivateRevision+1);if(publicChange)p.PublicRevision=checked(p.PublicRevision+1);
            candidate.FindEmpire(command.Issuer)!.Ready=false;
            if(GameStateValidator.Validate(content,candidate,allowAllHumansReady:issuer.Controller==ControllerKind.Ai).Count!=0)return Reject("diplomacy_invalid");
            return new(candidate,null,true,null);
        }
        catch(Exception e)when(e is OverflowException or InvalidOperationException or ArgumentException){return Reject("diplomacy_invalid");}
    }
    internal static bool CanObserveAccord(GameState state,EmpireId viewer,EmpireId other,StarId star)
    {
        var owner=state.FindEmpire(viewer);var survey=owner?.Surveys.SingleOrDefault(s=>s.StarId==star);
        return survey is not null&&ObservationUpdater.IsCurrent(state,viewer,star)&&survey.Planets.Any(p=>p.OwnerId==viewer||p.OwnerId==other);
    }
}
