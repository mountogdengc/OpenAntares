using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public sealed record ConflictPeaceDecision(DiplomacyPair Pair,string Kind,EmpireId Proposer);
public static class ConflictPeace
{
    internal static EmpireId? Other(ConflictAction action)=>action switch
    {ConflictAction.ProposePeace a=>a.Other,ConflictAction.AnswerPeace a=>a.Other,ConflictAction.WithdrawPeace a=>a.Other,ConflictAction.RevokePeaceAnswer a=>a.Other,_=>null};
    internal static bool Allowed(ConflictRelationView p,EmpireId owner,ConflictAction action,int turn)
    {
        if(p.Relation!=RelationKind.War||turn==int.MaxValue)return false;
        return action switch
        {
            ConflictAction.ProposePeace=>p.Peace is {} o?o.Proposer==owner||o.OwnAnswer is null or true:true,
            ConflictAction.AnswerPeace a=>p.Peace is {} o&&o.Id==a.Offer&&o.Proposer!=owner&&(o.OwnAnswer is null||o.OwnAnswer==a.Accept),
            ConflictAction.RevokePeaceAnswer a=>p.Peace is {} o&&o.Id==a.Offer&&o.Proposer!=owner,
            ConflictAction.WithdrawPeace a=>a.Offer.Value==0?p.Peace is null&&p.OwnPeaceProposed:p.Peace is {} o&&o.Id==a.Offer&&o.Proposer==owner,
            _=>false
        };
    }
    // A false result means an accepted no-op, never a rejection. Preview is authoritative.
    internal static bool Edit(ContentSet content,GameState state,ConflictPairState pair,EmpireId owner,ConflictAction action)
    {
        switch(action)
        {
            case ConflictAction.ProposePeace:
                if(pair.PendingPeace is {} offer){if(offer.Proposer==owner||offer.Answer==true)return false;offer.Answer=true;return true;}
                if(pair.PeaceIntents.Any(i=>i.Owner==owner&&i.Proposed&&!i.Withdrawn&&!i.Revoked))return false;
                SetIntent(new(owner,state.Turn,true));return true;
            case ConflictAction.AnswerPeace a:
                if(pair.PendingPeace!.Answer==a.Accept)return false;pair.PendingPeace.Answer=a.Accept;return true;
            case ConflictAction.RevokePeaceAnswer:
                if(pair.PendingPeace!.Answer is null)return false;pair.PendingPeace.Answer=null;return true;
            case ConflictAction.WithdrawPeace a:
                if(a.Offer.Value==0)SetIntent(new(owner,state.Turn,true,true));
                else {pair.PendingPeace=null;pair.Revision=checked(pair.Revision+1);pair.Parties.Single(p=>p.Owner!=owner).PrivateRevision=checked(pair.Parties.Single(p=>p.Owner!=owner).PrivateRevision+1);pair.Parties.Single(p=>p.Owner==owner).NextPeaceOfferTurn=state.FindEmpire(owner)!.Controller==ControllerKind.Ai?(long)state.Turn+content.Conflict!.PeaceCooldown:0;}
                return true;
            default:throw new ArgumentException("Unsupported peace edit.");
        }
        void SetIntent(PeaceIntent intent){pair.PeaceIntents.RemoveAll(i=>i.Owner==owner);pair.PeaceIntents.Add(intent);pair.PeaceIntents.Sort((a,b)=>a.Owner.CompareTo(b.Owner));}
    }
    internal static void Prepare(ContentSet content,GameState snapshot,GameState candidate,ConflictResolutionContext context)
    {
        var decisions=ImmutableArray.CreateBuilder<ConflictPeaceDecision>();long next=(long)snapshot.Turn+1;
        foreach(var pair in candidate.Conflict!.Pairs.Where(p=>p.Relation==RelationKind.War).OrderBy(p=>p.Pair))
        {
            if(pair.PendingPeace is {} offer)
            {
                if(offer.Answer is {} answer)decisions.Add(new(pair.Pair,answer?"accepted":"declined",offer.Proposer));
                else if(next>=offer.ExpiryTurn)decisions.Add(new(pair.Pair,"expired",offer.Proposer));
                continue;
            }
            var proposed=pair.PeaceIntents.Where(i=>i.Proposed).OrderBy(i=>i.Owner).ToArray();
            if(proposed.Length==0)continue;
            var first=proposed[0];bool active=!first.Withdrawn&&!first.Revoked;
            if(proposed.Length==2&&active&&!proposed[1].Withdrawn&&!proposed[1].Revoked)decisions.Add(new(pair.Pair,"accepted",first.Owner));
            else if(active)decisions.Add(new(pair.Pair,"offered",first.Owner));
            else decisions.Add(new(pair.Pair,"withdrawn",first.Owner));
        }
        context.PeaceDecisions=decisions.ToImmutable();context.AcceptedPeace=context.PeaceDecisions.Where(d=>d.Kind=="accepted").Select(d=>d.Pair).ToImmutableHashSet();
        foreach(var key in context.AcceptedPeace)
        {
            candidate.Conflict.Pairs.Single(p=>p.Pair==key).Relation=RelationKind.Peace;
            foreach(var owner in candidate.Conflict.Owners.Where(o=>key.Contains(o.Owner)))
            {
                owner.Blockades.RemoveAll(b=>b.Other==key.Other(owner.Owner));
                owner.Attacks.RemoveAll(a=>candidate.FindVessel(a.Target)?.EmpireId==key.Other(owner.Owner));
            }
            foreach(var encounter in candidate.Conflict.Encounters.Where(e=>e.Pair==key&&e.Phase!=ConflictPhase.Completed))
            {
                encounter.Phase=ConflictPhase.Completed;encounter.CompletedTurn=checked(snapshot.Turn+1);encounter.Revision=checked(encounter.Revision+1);
                foreach(var side in new[]{encounter.LowSide,encounter.HighSide})
                {side.WithdrawQueued=false;side.NeedsResponse=false;if(side.Fleet is {} id&&candidate.FindEmpire(side.Owner)!.Fleets!.Fleets.SingleOrDefault(f=>f.Id==id) is {} fleet)fleet.Order=new(FleetOrderKind.Hold);}
            }
        }
    }
    internal static void Finalize(ContentSet content,GameState candidate,ConflictResolutionContext context)
    {
        foreach(var decision in context.PeaceDecisions)
        {
            var pair=candidate.Conflict!.Pairs.Single(p=>p.Pair==decision.Pair);if(decision.Kind!="withdrawn")pair.Revision=checked(pair.Revision+1);
            if(decision.Kind=="offered")
            {pair.PendingPeace=new(){Id=new(pair.NextPeaceOfferId),Proposer=decision.Proposer,CreatedTurn=candidate.Turn,ExpiryTurn=(long)candidate.Turn+content.Conflict!.OfferLifetime};pair.NextPeaceOfferId=checked(pair.NextPeaceOfferId+1);}
            else
            {
                pair.PendingPeace=null;
                if(decision.Kind=="accepted"){pair.Relation=RelationKind.Peace;pair.LastPeaceTurn=candidate.Turn;pair.TruceEndTurn=(long)candidate.Turn+content.Conflict!.Truce;}
                else pair.Parties.Single(p=>p.Owner==decision.Proposer).NextPeaceOfferTurn=candidate.FindEmpire(decision.Proposer)!.Controller==ControllerKind.Ai?(long)candidate.Turn+content.Conflict!.PeaceCooldown:0;
            }
            var viewers=decision.Kind=="withdrawn"?pair.PeaceIntents.Where(i=>i.Proposed).Select(i=>i.Owner):new[]{pair.Pair.Low,pair.Pair.High};
            foreach(var viewer in viewers)ConflictRelations.RecordEvent(candidate,viewer,pair.Pair.Other(viewer),"peace_"+(decision.Kind=="withdrawn"?"not_agreed":decision.Kind),decision.Kind=="accepted"?FormattableString.Invariant($"Peace is effective. No final exchange occurred. Combat and blockades have stopped; the bilateral truce ends at turn {pair.TruceEndTurn}."):decision.Kind=="offered"?"A peace offer is available. No answer is required to Ready.":"Peace was not agreed.");
        }
        foreach(var pair in candidate.Conflict!.Pairs)
        {
            foreach(var intent in pair.PeaceIntents){var party=pair.Parties.Single(p=>p.Owner==intent.Owner);party.PrivateRevision=checked(party.PrivateRevision+1);}
            pair.PeaceIntents.Clear();
        }
    }
}
