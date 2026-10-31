using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static class DiplomacyStateValidator
{
    public static IReadOnlyList<string> Validate(ContentSet content,GameState state)
    {
        var errors=new List<string>();void Error(string message)=>errors.Add("Diplomacy: "+message);
        if(state.RulesVersion!=11){if(state.Diplomacy is not null)Error("Old rules cannot contain diplomacy state.");return errors;}
        if(content.Diplomacy is not {} d||state.Diplomacy is not {} diplomacy){Error("Explicit content and state required.");return errors;}
        if(diplomacy.Pairs is null){Error("Pairs are required.");return errors;}
        var seen=new HashSet<DiplomacyPair>();var earmarked=new HashSet<ColonyId>();DiplomacyPair? previous=null;
        foreach(var p in diplomacy.Pairs)
        {
            if(p is null){Error("Null pair.");continue;}
            var key=p.Pair;
            if(key.Low.Value<=0||key.Low.CompareTo(key.High)>=0||state.FindEmpire(key.Low) is null||state.FindEmpire(key.High) is null||!seen.Add(key)||previous is {} prev&&prev.CompareTo(key)>=0)Error("Pair must contain sorted unique existing empires.");previous=key;
            if(p.NextOfferId<1||p.PublicRevision<1)Error("Counters/revisions must be positive.");
            if(p.Parties is null||p.Parties.Count!=2||p.Parties[0].Owner!=key.Low||p.Parties[1].Owner!=key.High){Error("Exactly two ordered signatories required.");continue;}
            foreach(var party in p.Parties)
            {
                if(party.PrivateRevision<1||party.NextAiOfferTurn<0||party.Attitude is null){Error("Invalid party state.");continue;}
                if(party.Attitude.Streak<0||party.Attitude.Streak>d.StreakCap||party.Attitude.LastCounterpartNoticeTurn is {} t&&(t<0||t>state.Turn))Error("Invalid attitude history.");
                var own=state.FindEmpire(party.Owner);var other=state.FindEmpire(key.Other(party.Owner));
                if(own is null||other is null||!own.Contacts.Any(c=>c.OtherEmpireId==other.Id))Error("Reciprocal identity contacts required.");
            }
            if(p.Agreements is null){Error("Agreements are required.");continue;}
            if(p.Pending is {} o)
            {
                if(o.Id.Value<1||o.Id.Value>=p.NextOfferId||!key.Contains(o.Proposer)||o.Recipient!=key.Other(o.Proposer)||!Enum.IsDefined(o.Kind)||o.CreatedTurn<0||o.CreatedTurn>state.Turn||(long)o.ExpiryTurn!= (long)o.CreatedTurn+d.OfferLifetime||o.ExpiryTurn<=state.Turn)Error("Invalid pending offer identity/dates.");
                if(p.Agreements.Any(a=>a.Kind==o.Kind))Error("Cannot offer a live agreement kind.");
                if(o.Kind==AgreementKind.Settlement)
                {if(o.Star is not {} star||state.Stars.All(s=>s.Id!=star)||o.ProposerHub is not null||o.Answer?.Hub is not null)Error("Invalid settlement terms.");}
                else
                {
                    if(o.Star is not null)Error("Trade cannot disclose a system.");
                    Hub(o.ProposerHub,o.Proposer);
                    if(o.Answer?.Accept==true)Hub(o.Answer.Hub,o.Recipient);
                    else if(o.Answer?.Hub is not null)Error("Decline cannot reserve a hub.");
                }
            }
            if(p.Agreements is null||p.Agreements.Count>2){Error("At most two agreements.");continue;}
            var kinds=new HashSet<AgreementKind>();
            foreach(var a in p.Agreements)
            {
                if(a is null){Error("Null agreement.");continue;}
                if(!Enum.IsDefined(a.Kind)||!kinds.Add(a.Kind)||a.ActivatedTurn<0||a.ActivatedTurn>state.Turn)Error("Invalid agreement kind/activation.");
                if(a.Kind==AgreementKind.Settlement)
                {if(a.Star is not {} star||state.Stars.All(s=>s.Id!=star)||a.LowHub is not null||a.HighHub is not null)Error("Invalid accord.");}
                else{Origin(a.LowHub,key.Low,a.ActivatedTurn);Origin(a.HighHub,key.High,a.ActivatedTurn);if(a.Star is not null)Error("Trade system must be absent.");}
                var dates=new[]{a.LowNoticeTurn,a.HighNoticeTurn}.Where(t=>t is not null).Select(t=>t!.Value).ToArray();
                foreach(int t in dates)if(t<a.ActivatedTurn+d.Minimum||t>=state.Turn)Error("Invalid notice history.");
                if(dates.Length==0?a.EndTurn is not null:a.EndTurn!=(long)dates.Min()+d.Notice||a.EndTurn<=state.Turn)Error("Invalid notice end.");
            }
            if(p.LastResults is null||p.LastResults.Count>4)Error("Last results must be bounded to one per viewer/kind.");
            else foreach(var e in p.LastResults)if(!key.Contains(e.Viewer)||e.Other!=key.Other(e.Viewer)||e.Turn<0||e.Turn>state.Turn||e.Reason is null||e.Reason.Length>256)Error("Invalid bounded result.");
        }
        foreach(var empire in state.Empires)
        foreach(var contact in empire.Contacts)
        {var key=DiplomacyPair.For(empire.Id,contact.OtherEmpireId);if(!seen.Contains(key))Error("Every mutual contact requires a pair.");}
        return errors;
        void Hub(HubCommitment? hub,EmpireId owner)
        {
            if(hub is null){Error("Authorized hub required.");return;}
            var colony=state.FindColony(hub.Hub);
            if(colony is null||colony.EmpireId!=owner||hub.Reserved!=d.SetupCost||colony.ProductionReserve<hub.Reserved||!earmarked.Add(hub.Hub))Error("Invalid/duplicate hub earmark.");
        }
        void Origin(HubOrigin? origin,EmpireId owner,int turn)
        {if(origin is null||origin.Hub.Value<=0||origin.Hub.Value>=state.NextEntityId||origin.Owner!=owner||origin.ActivationTurn!=turn)Error("Invalid historical hub origin.");}
    }
}
