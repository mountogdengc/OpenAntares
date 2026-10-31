using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static class DiplomacyReconciliation
{
    public static void AfterCommand(ContentSet content,GameState candidate)
    {
        if(!Rules.HasDiplomacy(candidate.RulesVersion)||candidate.Diplomacy is null||content.Diplomacy is not {} d)return;
        foreach(var p in candidate.Diplomacy.Pairs)
        {
            if(p.Pending is not {Kind:AgreementKind.Trade} o)continue;
            bool Missing(HubCommitment? h,EmpireId owner)=>h is null||candidate.FindColony(h.Hub) is not {} c||c.EmpireId!=owner||c.ProductionReserve<h.Reserved;
            bool proposerLost=Missing(o.ProposerHub,o.Proposer),recipientLost=o.Answer?.Accept==true&&Missing(o.Answer.Hub,o.Recipient);
            if(!proposerLost&&!recipientLost)continue;
            p.Pending=null;p.PublicRevision=checked(p.PublicRevision+1);
            p.Parties.Single(x=>x.Owner==o.Proposer).NextAiOfferTurn=checked(candidate.Turn+d.Cooldown);
            foreach(var party in p.Parties)
            {
                party.PrivateRevision=checked(party.PrivateRevision+1);
                p.LastResults.RemoveAll(e=>e.Viewer==party.Owner&&e.Kind.StartsWith("trade_",System.StringComparison.Ordinal));
                bool ownLost=party.Owner==o.Proposer?proposerLost:recipientLost;
                var ownHub=party.Owner==o.Proposer?o.ProposerHub?.Hub:o.Answer?.Hub?.Hub;
                p.LastResults.Add(new(party.Owner,p.Pair.Other(party.Owner),candidate.Turn,"trade_failed",null,ownLost?ownHub:null,ownLost?"Your trade hub commitment could no longer be maintained.":"Trade could not activate."));
            }
        }
    }
}
