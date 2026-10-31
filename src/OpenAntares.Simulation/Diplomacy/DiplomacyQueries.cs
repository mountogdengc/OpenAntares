using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static partial class DiplomacyQueries
{
    internal static DiplomacyView Project(ContentSet content,GameState state,EmpireId viewer,EmpireView ownView)
    {
        var d=content.Diplomacy!;
        AttitudeView Attitude(AttitudeState a)=>new(DiplomacyAttitudes.Calculate(d,a.Streak,a.LastCounterpartNoticeTurn,state.Turn),DiplomacyAttitudes.Label(DiplomacyAttitudes.Calculate(d,a.Streak,a.LastCounterpartNoticeTurn,state.Turn)),a.Streak,checked(a.Streak*d.StreakStep),a.LastCounterpartNoticeTurn is {} t&&(long)state.Turn-t<d.MemoryLifetime?d.CancellationPenalty:0,a.LastCounterpartNoticeTurn is {} when?Math.Max(0,checked(d.MemoryLifetime-(state.Turn-when))):0);
        var contacts=ownView.Contacts.OrderBy(c=>c.OtherEmpireId).Select(contact=>
        {
            var p=state.Diplomacy!.Pairs.Single(p=>p.Pair==DiplomacyPair.For(viewer,contact.OtherEmpireId));var own=p.Parties.Single(x=>x.Owner==viewer);var other=p.Parties.Single(x=>x.Owner!=viewer);
            DiplomacyOfferView? offer=p.Pending is {} o?new(o.Id,o.Proposer,o.Recipient,o.Kind,o.CreatedTurn,o.ExpiryTurn,o.Star,o.Recipient==viewer&&o.Answer is not null,o.Recipient==viewer?o.Answer?.Accept:null,o.Proposer==viewer?o.ProposerHub?.Hub:o.Answer?.Hub?.Hub):null;
            var agreements=p.Agreements.Select(a=>
            {
                int? ownNotice=viewer==p.Pair.Low?a.LowNoticeTurn:a.HighNoticeTurn;int? otherNotice=viewer==p.Pair.Low?a.HighNoticeTurn:a.LowNoticeTurn;
                if(otherNotice>=state.Turn)otherNotice=null;
                int? end=ownNotice is {} on?checked(on+d.Notice):null;
                if(otherNotice is {} pn)end=end is {} e?Math.Min(e,checked(pn+d.Notice)):checked(pn+d.Notice);
                return new DiplomacyAgreementView(a.Kind,a.Star,a.ActivatedTurn,checked((long)a.ActivatedTurn+d.Minimum),end,(viewer==p.Pair.Low?a.LowHub:a.HighHub)?.Hub,checked((int)Math.Max(0L,(long)a.ActivatedTurn+d.Warmup-state.Turn)),ownNotice is not null);
            }).ToImmutableArray();
            return new ContactDiplomacyView(contact,p.PublicRevision,DiplomacyTokens.PrivateRevision(content,state,viewer,contact.OtherEmpireId),Attitude(own.Attitude),Attitude(other.Attitude),offer,agreements,own.NextAiOfferTurn,p.LastResults.Where(e=>e.Viewer==viewer&&e.Turn<=state.Turn).ToImmutableArray());
        }).ToImmutableArray();
        var hubs=ownView.Colonies.OrderBy(c=>c.Id).Select(c=>
        {
            var pending=state.Diplomacy!.Pairs.Select(p=>p.Pending).FirstOrDefault(o=>o is not null&&(o.Proposer==viewer&&o.ProposerHub?.Hub==c.Id||o.Recipient==viewer&&o.Answer?.Hub?.Hub==c.Id));
            return new OwnDiplomacyHub(c.Id,DiplomacyHubQueries.Eligibility(content,state,viewer,c.Id,null),pending is null?null:DiplomacyHubQueries.Eligibility(content,state,viewer,c.Id,pending.Id));
        }).ToImmutableArray();
        return new(contacts,hubs);
    }
}
