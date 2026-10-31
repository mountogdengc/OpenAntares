using System;
using System.Linq;
using System.Collections.Generic;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static class DiplomacyPlanner
{
    public static void Plan(ContentSet content,EmpireView view,Action<DiplomacyCommand> emit)
    {
        if(view.Diplomacy is not {} diplomacy||content.Diplomacy is not {} d)return;var used=new HashSet<ColonyId>();
        foreach(var c in diplomacy.Contacts.OrderBy(c=>c.Identity.OtherEmpireId))
        {
            var other=c.Identity.OtherEmpireId;
            ColonyId? Hub()=>diplomacy.Hubs.Where(h=>h.NewCommitment.Allowed&&!used.Contains(h.Id)).OrderBy(h=>h.Id).Select(h=>(ColonyId?)h.Id).FirstOrDefault();
            void Emit(DiplomacyAction action)=>emit(new(view.Viewer,view.Turn,other,c.PublicRevision,c.PrivateRevision,action));
            if(c.Pending is {} o)
            {
                if(o.Recipient==view.Viewer&&!o.OwnAnswered)
                {
                    bool accept;ColonyId? hub=null;
                    if(o.Kind==AgreementKind.Trade){hub=Hub();accept=c.OwnAttitude.Score>=d.TradeThreshold&&hub is not null;}
                    else accept=c.OwnAttitude.Score>=d.AccordThreshold&&o.Star is {} star&&view.Colonies.Any(col=>col.StarId==star);
                    if(accept&&hub is {} selected)used.Add(selected);Emit(new DiplomacyAction.Answer(o.Id,accept,accept?hub:null));
                }
                continue;
            }
            if(view.Turn<c.NextAiOfferTurn)continue;
            if(!c.Agreements.Any(a=>a.Kind==AgreementKind.Trade)&&c.OwnAttitude.Score>=d.TradeThreshold&&Hub() is {} tradeHub)
            {used.Add(tradeHub);Emit(new DiplomacyAction.OfferTrade(tradeHub));continue;}
            if(c.Agreements.Any(a=>a.Kind==AgreementKind.Settlement)||c.OwnAttitude.Score<d.AccordThreshold)continue;
            var candidate=view.Stars.Where(s=>s.Current&&view.Colonies.Any(col=>col.StarId==s.Chart.Id)&&s.Planets is {} planets&&planets.Any(p=>p.OwnerId==other)).OrderBy(s=>s.Chart.Id).Select(s=>(StarId?)s.Chart.Id).FirstOrDefault();
            if(candidate is {} target)Emit(new DiplomacyAction.OfferSettlement(target));
        }
    }
}
