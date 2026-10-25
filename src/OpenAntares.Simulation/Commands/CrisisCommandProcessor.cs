using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Commands;

public static partial class CommandProcessor
{
    internal static CommandOutcome ExecuteCrisisAutomatic(ContentSet content,GameState state,CrisisCommand command)
    {
        if(command is not(SetQuarantineCommand or SetCureShareCommand or DecontaminateVesselCommand)||state.FindEmpire(command.Issuer) is not {} issuer||command.ExpectedTurn!=state.Turn)
            return CommandOutcome.Reject(state,"invalid_crisis","Automatic crisis order is outside its response scope.");
        return ExecuteCrisis(content,state,issuer,command,automatic:true);
    }
    private static CommandOutcome ExecuteCrisis(ContentSet content,GameState state,EmpireState issuer,CrisisCommand command,bool automatic=false)
    {
        CommandOutcome Reject()=>CommandOutcome.Reject(state,"invalid_crisis","Crisis order is invalid, stale, or outside this empire's authority. Refresh its own crisis overview.");
        if(!Rules.HasCrises(state.RulesVersion)||state.Crisis is not{Enabled:true}||issuer.Crisis is null)return Reject();
        if(issuer.Controller!=ControllerKind.Human&&command is not(SetQuarantineCommand or SetCureShareCommand or DecontaminateVesselCommand))return Reject();
        if(automatic&&issuer.Controller==ControllerKind.Human&&issuer.Crisis.Authority==CrisisAuthority.Manual)return Reject();
        var candidate=state.Clone();var owner=candidate.FindEmpire(issuer.Id)!;var e=owner.Crisis!;var definition=content.Plagues.Values.Single();bool changed=false;
        try
        {
            switch(command)
            {
                case SetQuarantineCommand c:
                {
                    Require(c.Expected is not null&&c.Expected.Owner==issuer.Id,"Own confirmation required.");
                    var expected=c.Expected;
                    Require(!expected.Colonies.IsDefault&&expected.Colonies.All(x=>x is not null),"Missing colony group.");
                    var fresh=CrisisQueries.PreviewQuarantine(content,state,issuer.Id,expected.Colonies.Select(x=>x.Colony).ToImmutableArray(),expected.Enable,expected.ReleaseOnRecovery);
                    Require(fresh.Allowed&&CrisisPreviewEquality.Equal(expected,fresh),"Quarantine consequences changed.");
                    if(automatic)Require(fresh.Enable&&fresh.ReleaseOnRecovery&&fresh.Colonies.All(f=>Owned(f.Colony).Crisis!.Case is not null&&(issuer.Controller==ControllerKind.Ai||!Owned(f.Colony).Crisis!.PolicyPaused)),"Only authorized unpaused cases may be contained automatically.");
                    foreach(var fact in fresh.Colonies)
                    {
                        var colony=Owned(fact.Colony);var health=colony.Crisis!;
                        bool local=health.Quarantined!=fact.After||health.ReleaseEpisode!=fact.ReleaseAfter;
                        if(!local)continue;
                        health.Quarantined=fact.After;health.ReleaseEpisode=fact.ReleaseAfter;
                        if(!automatic&&health.Case is not null)health.PolicyPaused=true;
                        AuditColony(colony,"quarantine",automatic?"automatic":"manual",fact.After?"Population port quarantined; displayed orders cancelled.":"Quarantine released; cancelled orders stay cancelled.");changed=true;
                    }
                    foreach(var order in fresh.CancelledOrders){var v=candidate.FindVessel(order.Id)!;v.PendingMove=null;v.PendingFounding=null;changed=true;}
                    break;
                }
                case SetCureShareCommand c:
                {
                    Require(c.Share is 0 or 25 or 50 or 100,"Invalid cure share.");var episode=e.Episode;Require(episode is not null&&episode.Id==c.Episode,"Missing own episode.");
                    if(automatic)Require(issuer.Controller==ControllerKind.Ai?c.Share==50:e.Authority==CrisisAuthority.ContainAndResearch&&c.Share==e.AuthorityResearchShare&&!e.ResearchPolicyPaused&&!episode.ResearchOverride,"Automatic research authority is paused or absent.");
                    if(episode.ResearchShare==c.Share)break;
                    episode.ResearchShare=c.Share;if(!automatic){episode.ResearchOverride=true;e.ResearchPolicyPaused=true;}
                    AuditSource("research_share",automatic?"automatic":"manual","Cure research share changed to "+c.Share+"% of new research.");changed=true;break;
                }
                case SetCrisisAuthorityCommand c:
                    Require(Enum.IsDefined(c.Authority)&&c.Share is 25 or 50 or 100,"Invalid crisis authority/share.");
                    if(e.Authority!=c.Authority||e.AuthorityResearchShare!=c.Share){e.Authority=c.Authority;e.AuthorityResearchShare=c.Share;AuditSource("authority","manual","Crisis response authority changed explicitly.");changed=true;}break;
                case SetCrisisAcknowledgementCommand c:
                {
                    if(e.RequireAcknowledgement==c.Require)break;
                    var pending=CrisisQueries.PendingAttention(state,issuer.Id);e.RequireAcknowledgement=c.Require;
                    if(!c.Require)
                    {
                        foreach(var receipt in pending)
                        {
                            var illness=candidate.FindColony(receipt.Key.Colony)?.Crisis?.Case;
                            if(illness?.EpisodeId==receipt.Key.Episode)illness.WaivedRevision=Math.Max(illness.WaivedRevision,receipt.Revision);
                            CrisisAudit.Add(candidate,issuer.Id,new(receipt.Key,receipt.Location,"acknowledgement",receipt.Revision,"waived",0,0,receipt.Reason));
                        }
                        e.PendingReceipts.Clear();
                    }
                    else
                    {
                        foreach(var colony in candidate.Colonies.Where(x=>x.EmpireId==issuer.Id&&x.Crisis?.Case is not null).OrderBy(x=>x.Id))
                        {
                            var illness=colony.Crisis!.Case!;illness.AttentionRevision=checked(illness.AttentionRevision+1);illness.WaivedRevision=0;
                            var key=new CrisisKey(issuer.Id,illness.EpisodeId,colony.Id);e.PendingReceipts.RemoveAll(r=>r.Key==key);
                            e.PendingReceipts.Add(new(key,illness.AttentionRevision,state.Turn,CrisisQueries.Location(candidate,colony),"Acknowledgement requirement restored; review this active case."));
                            AuditColony(colony,"acknowledgement_restored","pending","Acknowledgement requirement restored; current case awaits review.");
                        }
                        SortReceipts();
                    }
                    changed=true;break;
                }
                case AcknowledgeCrisisCommand c:
                {
                    Require(!c.Expected.IsDefaultOrEmpty&&c.Expected.All(r=>r is not null)&&c.Expected.Select(r=>r.Key).Distinct().Count()==c.Expected.Length,"Select distinct current revisions.");
                    var pending=CrisisQueries.PendingAttention(state,issuer.Id);
                    foreach(var receipt in c.Expected)Require(receipt is not null&&receipt.Key.Owner==issuer.Id&&pending.Contains(receipt),"Acknowledgement group changed.");
                    foreach(var receipt in c.Expected.OrderBy(r=>r.Key.Episode).ThenBy(r=>r.Key.Colony))
                    {
                        var illness=candidate.FindColony(receipt.Key.Colony)?.Crisis?.Case;
                        if(illness?.EpisodeId==receipt.Key.Episode)illness.HandledRevision=receipt.Revision;
                        e.PendingReceipts.RemoveAll(r=>r.Key==receipt.Key);
                        CrisisAudit.Add(candidate,issuer.Id,new(receipt.Key,receipt.Location,"acknowledgement",receipt.Revision,"handled",0,0,receipt.Reason));
                    }
                    changed=true;break;
                }
                case ResumeCrisisPolicyCommand c:
                {
                    Require(!c.Colonies.IsDefault&&c.Colonies.Distinct().Count()==c.Colonies.Length&&(c.Colonies.Length>0||c.Research),"Select a paused channel.");
                    foreach(var id in c.Colonies)Owned(id);
                    foreach(var id in c.Colonies.OrderBy(x=>x)){var colony=Owned(id);if(colony.Crisis!.PolicyPaused){colony.Crisis.PolicyPaused=false;AuditColony(colony,"resume","manual","Case response policy resumed.");changed=true;}}
                    if(c.Research&&(e.ResearchPolicyPaused||e.Episode?.ResearchOverride==true)){e.ResearchPolicyPaused=false;if(e.Episode is {} episode)episode.ResearchOverride=false;AuditSource("resume_research","manual","Research response policy resumed.");changed=true;}
                    break;
                }
                case DecontaminateVesselCommand c:
                {
                    var v=candidate.FindVessel(c.Vessel);
                    Require(v is not null&&v.EmpireId==issuer.Id&&v.CarriedPopulation>0&&v.StationaryStarId is not null&&v.Crisis?.CarrierEpisode==e.Episode?.Id&&e.Episode is not null,"Select own stationary infectious cargo.");
                    Require(candidate.Colonies.Any(x=>x.EmpireId==issuer.Id&&candidate.FindPlanet(x.PlanetId)?.StarId==v.StationaryStarId),"Own colony port required.");
                    Require(!c.ExpectedOrders.IsDefault&&c.ExpectedOrders.SequenceEqual(new[]{CrisisQueries.OrderFact(v)}),"Inspection order facts changed.");
                    if(CrisisQueries.IsHeld(state,v.Id))break;
                    v.Crisis!.InspectionDueTurn=checked(state.Turn+1);v.PendingMove=null;v.PendingFounding=null;
                    AuditSource("inspection",automatic?"automatic":"manual","Cargo "+v.Id+" held for one full resolution; confirmed orders cancelled.");changed=true;break;
                }
                case PurgeColonyCommand c:
                {
                    Require(!automatic&&issuer.Controller==ControllerKind.Human&&c.Expected is not null&&c.Expected.Owner==issuer.Id&&c.Expected.Key is not null,"Explicit own human confirmation required.");
                    var fresh=CrisisQueries.PreviewPurge(content,state,issuer.Id,c.Expected.Key.Colony);
                    Require(fresh.Allowed&&CrisisPreviewEquality.Equal(c.Expected,fresh),"Destructive consequences changed.");
                    var colony=Owned(fresh.Key!.Colony);var health=colony.Crisis!;var illness=health.Case!;long revision=checked(illness.AttentionRevision+1);
                    Require(revision<long.MaxValue,"Attention counter exhausted.");
                    if(fresh.ColonyLost)CrisisLoss.Remove(candidate,colony.Id,fresh.Location!);
                    else
                    {
                        colony.Population=fresh.PopulationAfter;colony.Workforce=fresh.WorkforceAfter;colony.ProductionReserve=fresh.ReserveAfter;colony.GrowthProgress=0;
                        health.Case=null;health.ImmunityFamilyId=definition.Id;health.ImmuneUntilTurn=checked(state.Turn+definition.ImmunityTurns);
                        if(health.ReleaseEpisode==illness.EpisodeId){health.Quarantined=false;health.ReleaseEpisode=null;}
                    }
                    e.CivicLevel=fresh.NextCivicLevel;e.CivicExpiresAt=fresh.NextCivicExpiresAt;
                    e.PendingReceipts.RemoveAll(r=>r.Key==fresh.Key);
                    CrisisAudit.Add(candidate,issuer.Id,new(fresh.Key,fresh.Location!,fresh.ColonyLost?"colony_lost":"destructive_containment",revision,"direct_order",0,fresh.PopulationLost,"Confirmed destructive containment; other cases and cargo remain unaffected."));
                    changed=true;break;
                }
                default:return Reject();
            }
            if(!changed)return CommandOutcome.NoChange(state);
            owner.Ready=false;
            // Crisis actions can close/cancel the exact departure the owner acknowledged.
            // Reconcile before validating this candidate, including automatic preparation.
            if(Rules.HasScouting(state.RulesVersion))ReconcileScoutConsent(content,state,candidate,issuer.Id);
            Require(GameStateValidator.Validate(content,candidate,allowAllHumansReady:automatic).Count==0,"Candidate validation failed.");
            return new(candidate,null,true,null);
        }
        catch(Exception ex) when(ex is InvalidOperationException or ArgumentException or OverflowException or KeyNotFoundException){return Reject();}

        ColonyState Owned(ColonyId id){var c=candidate.FindColony(id);Require(c is not null&&c.EmpireId==issuer.Id&&c.Crisis is not null,"Own colony required.");return c;}
        void SortReceipts()=>e.PendingReceipts.Sort((a,b)=>{int episode=a.Key.Episode.CompareTo(b.Key.Episode);return episode!=0?episode:a.Key.Colony.CompareTo(b.Key.Colony);});
        void AuditColony(ColonyState c,string kind,string disposition,string reason)
        {
            long? episode=c.Crisis!.Case?.EpisodeId??e.Episode?.Id;if(episode is null)return;
            CrisisAudit.Add(candidate,issuer.Id,new(new(issuer.Id,episode.Value,c.Id),CrisisQueries.Location(candidate,c),kind,c.Crisis.Case?.AttentionRevision??1,disposition,0,0,reason));
        }
        void AuditSource(string kind,string disposition,string reason)
        {
            if(e.Episode is not {} episode)return;
            long revision=candidate.FindColony(episode.Source.Colony)?.Crisis?.Case?.AttentionRevision??1;
            CrisisAudit.Add(candidate,issuer.Id,new(new(issuer.Id,episode.Id,episode.Source.Colony),episode.Source,kind,revision,disposition,0,0,reason));
        }
    }
}
