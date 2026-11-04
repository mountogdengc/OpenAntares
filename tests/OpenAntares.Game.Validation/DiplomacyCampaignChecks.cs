using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Colonization;
using OpenAntares.Simulation.Saves;
namespace OpenAntares.Game.Validation;
public partial class DiplomacyGraphicalValidation
{
    public sealed record CampaignStep(Command Command,bool Accepted,string Hash,string? Stage);
    public sealed record EarnedCampaign(ulong Seed,string Species,GameSession Final,List<CampaignStep> Steps,long PaidSetup,long TradePaid,int Founded,long BothSetupPaid,long EarnedProduction,long FinalBank,long HullPayments);
    private static string Digest(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    public static EarnedCampaign BuildCampaign(ContentSet content,string species,ulong seed)
    {
        var generated=GalaxyGenerator.CreateNewGame(content,new("small",seed,Rules.CurrentVersion,species,1,"wenth",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));if(!generated.Succeeded)throw new InvalidOperationException(generated.Error);
        var s=new GameSession(content,generated.State!);var owner=s.State.Empires[0].Id;var other=s.State.Empires[1].Id;var home=s.View(owner).Colonies.Single().Id;var steps=new List<CampaignStep>();long paid=0,income=0,bothPaid=0,earnedProduction=0;int founded=0;StarId? shared=null;bool secondOrdered=false,blocked=false;var stages=new HashSet<string>();
        void Send(Command command,string? stage=null,bool accepted=true)
        {
            var result=s.Execute(command);if(result.Accepted!=accepted)throw new InvalidOperationException(command.GetType().Name+": "+result.Rejection?.Message);
            if(stage is null&&command is SetReadyCommand&&s.State.Diplomacy!.Pairs.Any(p=>p.Pending is not null)&&!stages.Contains("pending-offer"))stage="pending-offer";
            if(stage is not null&&!stages.Add(stage))stage=null;steps.Add(new(command,accepted,Digest(SaveGame.Serialize(content,s.State)),stage));
            if(result.Report is {} report){earnedProduction+=report.Economy.Colonies.Where(c=>c.Empire==owner).Sum(c=>c.NetProduction.Total);if(report.DiplomacyEvents is {} events){paid+=events.Where(e=>e.Viewer==owner).Sum(e=>e.Paid);bothPaid+=events.Sum(e=>e.Paid);}income+=report.Economy.Colonies.Where(c=>c.Empire==owner).SelectMany(c=>c.NetProduction.Lines).Where(c=>c.Kind=="diplomacy_trade").Sum(c=>c.Amount);}
        }
        void Dip(DiplomacyAction action,string? stage=null){var c=s.View(owner).Diplomacy!.Contacts.Single();Send(new DiplomacyCommand(owner,s.State.Turn,other,c.PublicRevision,c.PrivateRevision,action),stage);}
        void Ready(string? stage=null)=>Send(new SetReadyCommand(owner,s.State.Turn,true),stage);
        for(int n=0;n<160;n++)
        {
            if(n%25==0)Console.WriteLine($"CURATE {species} seed{seed} turn{s.State.Turn} founded{founded} shared{shared} paid{paid} income{income}");var view=s.View(owner);var context=RivalPlanningQueries.Create(content,s.State,owner);var contact=view.Diplomacy!.Contacts.FirstOrDefault();var accord=contact?.Agreements.SingleOrDefault(a=>a.Kind==AgreementKind.Settlement);var trade=contact?.Agreements.SingleOrDefault(a=>a.Kind==AgreementKind.Trade);
            foreach(var colony in view.Colonies)
            {
                // Hypothetical economy derives solely from this player's current view.
                var target=colony.ProjectId is null?null:new ProductionTarget(colony.ProjectKind,colony.ProjectId);Workforce? best=null;
                for(int support=target is {Kind:ProductionKinds.Vessel} ship?content.Vessels[ship.ContentId].EmbarkPopulation:0;support<=colony.Population;support++){var w=new Workforce(support,colony.Population-support,0);var forecast=context.Forecast(colony.Id,w,target);if(forecast is not null&&forecast.SupportProduced.Total>=forecast.SupportRequired.Total){best=w;break;}}
                var work=best??colony.Workforce;if(best is not null&&work.Support<colony.Population&&colony.Population<colony.Economy.Growth.Capacity)work=work with{Support=work.Support+1,Production=work.Production-1};
                if(best is not null&&work!=colony.Workforce)Send(new SetWorkforceCommand(owner,s.State.Turn,colony.Id,work));
            }
            view=s.View(owner);context=RivalPlanningQueries.Create(content,s.State,owner);
            var role=view.AvailableVesselRoleIds.Select(id=>content.Vessels[id]).Where(r=>r.CanFoundColony).OrderBy(r=>r.FoundsSealedHabitat?0:1).ThenBy(r=>r.Cost).ThenBy(r=>r.Id,StringComparer.Ordinal).First();
            var founder=view.Vessels.FirstOrDefault(v=>content.Vessels[v.RoleId].CanFoundColony);
            if(founder is null&&founded==0||founder is not null&&!secondOrdered&&founded==0||founder is null&&accord is not null&&!secondOrdered)
            {var c=view.Colonies.Single(c=>c.Id==home);if(c.ProjectId is null){Send(new SetProductionTargetCommand(owner,s.State.Turn,home,new(ProductionKinds.Vessel,role.Id)));if(founder is not null||founded>0)secondOrdered=true;}}
            // Find a currently observed shared-system opportunity; no unobserved rival home lookup.
            var opportunities=view.Stars.Where(st=>st.Current&&st.Planets is {} ps&&ps.Any(p=>p.OwnerId==other)).Select(st=>new{Star=st,Targets=st.Planets!.Value.Where(p=>p.OwnerId is null&&context.PreviewSettlement(p.Id,role.Id) is {RejectionCode:null}).ToArray()}).Where(x=>x.Targets.Length>=2).OrderBy(x=>x.Star.Chart.Id).ToArray();
            if(founded==0&&opportunities.FirstOrDefault() is {} opportunity)shared=opportunity.Star.Chart.Id;
            var scout=view.Vessels.Single(v=>v.RoleId=="scout");
            if(shared is null&&scout.Transit is null&&scout.PendingMove is null){var next=view.Stars.Where(st=>st.Chart.Id!=scout.StationaryStarId).OrderBy(st=>st.LastObservedTurn??-1).ThenBy(st=>st.Chart.Id).First();Send(new SetMoveOrderCommand(owner,s.State.Turn,scout.Id,next.Chart.Id));}
            if(founder is not null&&shared is {} destination&&founder.Transit is null&&founder.PendingMove is null)
            {
                if(founder.StationaryStarId!=destination)Send(new SetMoveOrderCommand(owner,s.State.Turn,founder.Id,destination));
                else
                {
                    var star=view.Stars.Single(st=>st.Chart.Id==destination);var target=star.Planets!.Value.Where(p=>p.OwnerId is null&&p.Colonizable).OrderBy(p=>p.Id).FirstOrDefault(p=>FoundingRules.Preview(content,s.State,owner,founder.Id,p.Id).RejectionCode is null||accord is not null);
                    if(target is not null&&founder.PendingFounding is null)
                    {
                        if(accord is not null&&!blocked){Send(new SetFoundingOrderCommand(owner,s.State.Turn,founder.Id,target.Id),"blocked-founder",false);blocked=true;}
                        else if(accord is null&&(founded==0||stages.Contains("accord-notice"))){Send(new SetFoundingOrderCommand(owner,s.State.Turn,founder.Id,target.Id));}
                    }
                }
            }
            view=s.View(owner);contact=view.Diplomacy!.Contacts.FirstOrDefault();accord=contact?.Agreements.SingleOrDefault(a=>a.Kind==AgreementKind.Settlement);trade=contact?.Agreements.SingleOrDefault(a=>a.Kind==AgreementKind.Trade);
            if(contact?.Pending is {} pending&&pending.Recipient==owner&&!pending.OwnAnswered)
            {
                if(pending.Kind==AgreementKind.Settlement&&!stages.Contains("accord-notice"))Dip(new DiplomacyAction.Answer(pending.Id,true,null),"queued-answer");
                else if(pending.Kind==AgreementKind.Trade&&!stages.Contains("trade-notice")&&view.Diplomacy.Hubs.FirstOrDefault(h=>h.NewCommitment.Allowed) is {} hub)Dip(new DiplomacyAction.Answer(pending.Id,true,hub.Id),"trade-answer");
            }
            else if(contact is not null&&contact.Pending is null)
            {
                if(accord is null&&founded==1&&!stages.Contains("accord-notice")&&shared is {} st)Dip(new DiplomacyAction.OfferSettlement(st),"pending-accord");
                else if(accord is not null&&!stages.Contains("trade-notice")&&contact.OwnAttitude.Score>=10&&trade is null&&view.Diplomacy.Hubs.SingleOrDefault(h=>h.Id==home&&h.NewCommitment.Allowed) is {} hub)Dip(new DiplomacyAction.OfferTrade(hub.Id),"trade-earmark");
            }
            view=s.View(owner);contact=view.Diplomacy!.Contacts.FirstOrDefault();accord=contact?.Agreements.SingleOrDefault(a=>a.Kind==AgreementKind.Settlement);trade=contact?.Agreements.SingleOrDefault(a=>a.Kind==AgreementKind.Trade);
            if(trade is not null&&income>0&&view.Turn>=trade.FirstTerminationTurn&&!trade.OwnNotice)Dip(new DiplomacyAction.Terminate(AgreementKind.Trade),"trade-notice");
            if(accord is not null&&income>0&&blocked&&view.Turn>=accord.FirstTerminationTurn&&!accord.OwnNotice)Dip(new DiplomacyAction.Terminate(AgreementKind.Settlement),"accord-notice");
            var before=view.Colonies.Length;Ready(trade is not null&&trade.WarmupRemaining>0?"warmup":null);founded+=s.View(owner).Colonies.Length-before;
            if(founded>=2&&paid>=500&&income>0&&stages.Contains("accord-notice")&&stages.Contains("trade-notice")){long bank=s.View(owner).Colonies.Sum(c=>c.ProductionReserve);long hullPayments=2*role.Cost;if(earnedProduction!=bank+paid+hullPayments||bothPaid!=1000)throw new InvalidOperationException("Earned production ledger does not balance");return new(seed,species,s,steps,paid,income,founded,bothPaid,earnedProduction,bank,hullPayments);}
        }
        throw new InvalidOperationException($"Bound160 reached; founded={founded}, shared={shared}, paid={paid}, income={income}, stages={string.Join(',',stages)}");
    }
}







