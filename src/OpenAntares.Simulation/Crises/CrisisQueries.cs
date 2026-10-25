using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Crises;

/// <summary>Pure owner-scoped facts shared by commands, policies and presentation.</summary>
public static class CrisisQueries
{
    public static bool PortClosed(GameState state,EmpireId owner,StarId star) => Rules.HasCrises(state.RulesVersion)&&state.Crisis is{Enabled:true}&&state.Colonies.Any(c=>c.EmpireId==owner&&c.Crisis?.Quarantined==true&&state.FindPlanet(c.PlanetId)?.StarId==star);
    public static bool IsHeld(GameState state,VesselId vessel) => Rules.HasCrises(state.RulesVersion)&&state.Crisis is{Enabled:true}&&state.FindVessel(vessel)?.Crisis?.InspectionDueTurn is {} due&&state.Turn<due;
    public static CrisisLocation Location(GameState state,ColonyState c)
    {
        var p=state.FindPlanet(c.PlanetId)!;var star=state.FindStar(p.StarId)!;return new(c.Id,p.Id,star.Id,$"{star.Name} {p.Orbit}");
    }
    public static ImmutableArray<CrisisReceipt> PendingAttention(GameState state,EmpireId owner)
    {
        if(!Rules.HasCrises(state.RulesVersion)||state.Crisis is not{Enabled:true}||state.FindEmpire(owner)?.Crisis is not {} e)return ImmutableArray<CrisisReceipt>.Empty;
        var receipts=new List<CrisisReceipt>(e.PendingReceipts);
        foreach(var colony in state.Colonies.Where(c=>c.EmpireId==owner))
        {
            if(colony.Crisis?.Case is not {} illness)continue;
            if(illness.AttentionRevision<=Math.Max(illness.HandledRevision,illness.WaivedRevision))continue;
            var key=new CrisisKey(owner,illness.EpisodeId,colony.Id);
            if(!receipts.Any(r=>r.Key==key&&r.Revision>=illness.AttentionRevision))receipts.Add(new(key,illness.AttentionRevision,state.Turn,Location(state,colony),illness.Stage==CrisisStage.Warning?"New plague exposure":"Plague escalation"));
        }
        return receipts.GroupBy(r=>r.Key).Select(g=>g.OrderByDescending(r=>r.Revision).First()).OrderBy(r=>r.Key.Episode).ThenBy(r=>r.Key.Colony).ToImmutableArray();
    }
    public static CrisisView Project(ContentSet content,GameState state,EmpireId viewer)
    {
        var owner=state.FindEmpire(viewer)??throw new ArgumentException("Empire not found.",nameof(viewer));
        if(!Rules.HasCrises(state.RulesVersion)||owner.Crisis is not {} e)throw new ArgumentException("Crisis rules are unavailable.",nameof(state));
        // Remove other empires before calculating anything. Foreign economic overflow cannot affect this view.
        var frame=state.Clone();frame.Empires.RemoveAll(x=>x.Id!=viewer);frame.Colonies.RemoveAll(x=>x.EmpireId!=viewer);frame.Vessels.RemoveAll(x=>x.EmpireId!=viewer);
        var economy=EconomyCalculator.Calculate(content,frame);var definition=content.Plagues.Values.Single();
        var cure=economy.Empire(viewer).Cure??new(0,0,0,0,0,false);
        var conditions=frame.Colonies.OrderBy(c=>c.Id).Select(c=>
        {
            var health=c.Crisis!;var illness=health.Case;var output=economy.Colony(c.Id);
            int population=output.EmbarkOutcome?.PopulationAfter??output.Growth.PopulationAfter;
            bool death=illness is{Stage:CrisisStage.Active}&&illness.Severity>=definition.MortalityThreshold&&illness.ActiveAge+1<definition.RecoveryIntervals&&!cure.Completes;
            return new CrisisCaseView(illness is null?null:new(viewer,illness.EpisodeId,c.Id),Location(frame,c),illness?.Stage,illness?.GraceRemaining??0,illness?.ActiveAge??0,illness?.Severity??0,illness?.AttentionRevision??0,illness?.HandledRevision??0,illness?.WaivedRevision??0,health.Quarantined,health.ReleaseEpisode,health.PolicyPaused,health.ImmunityFamilyId,health.ImmuneUntilTurn,c.Population,illness?.Stage==CrisisStage.Active?definition.RecoveryIntervals-illness.ActiveAge:definition.RecoveryIntervals,death,death&&population<=definition.MortalityUnits,output);
        }).ToImmutableArray();
        var vessels=frame.Vessels.OrderBy(v=>v.Id).Select(v=>
        {
            bool port=v.CarriedPopulation>0&&v.StationaryStarId is {} station&&PortClosed(frame,viewer,station);
            var risks=v.StationaryStarId is {} star&&v.CarriedPopulation>0&&!port?frame.Colonies.Where(c=>c.Crisis?.Case?.Stage==CrisisStage.Active&&!c.Crisis.Quarantined&&frame.FindPlanet(c.PlanetId)?.StarId==star).OrderBy(c=>c.Id).Select(c=>Location(frame,c)).ToImmutableArray():ImmutableArray<CrisisLocation>.Empty;
            bool decontaminate=v.CarriedPopulation>0&&v.Crisis?.CarrierEpisode is not null&&v.StationaryStarId is {} at&&frame.Colonies.Any(c=>frame.FindPlanet(c.PlanetId)?.StarId==at)&&!IsHeld(frame,v.Id);
            return new CrisisVesselView(v.Id,v.RoleId,v.CarriedPopulation,v.Crisis?.CarrierEpisode,v.Crisis?.InspectionDueTurn,v.StationaryStarId,v.Transit,v.PendingMove,v.PendingFounding,port,IsHeld(frame,v.Id),decontaminate,risks);
        }).ToImmutableArray();
        var episode=e.Episode is {} ep?new CrisisEpisodeView(ep.Id,ep.FamilyId,definition.Name,ep.Source,ep.SourcePopulation,ep.CureCost,ep.CureProgress,ep.ResearchShare,ep.ResearchOverride,cure):null;
        return new(state.Turn,viewer,state.Crisis!.Enabled,e.Authority,e.AuthorityResearchShare,e.ResearchPolicyPaused,e.RequireAcknowledgement,e.CivicLevel,e.CivicExpiresAt,episode,conditions.Where(c=>c.Key is not null).ToImmutableArray(),vessels.Where(v=>v.CarrierEpisode is not null||v.InspectionDueTurn is not null).ToImmutableArray(),PendingAttention(frame,viewer),e.CompletedDigests.ToImmutableArray(),e.LostColonyLocations.ToImmutableArray()){ColonyConditions=conditions,Vessels=vessels,PendingDigests=e.PendingDigests.ToImmutableArray()};
    }
    public static PurgePreview PreviewPurge(ContentSet content,GameState state,EmpireId owner,ColonyId colony)
    {
        PurgePreview Reject(string reason)=>new(){Turn=state.Turn,Owner=owner,RejectionReason=reason};
        if(!Rules.HasCrises(state.RulesVersion)||state.Crisis is not{Enabled:true})return Reject("crises_unavailable");
        var c=state.FindColony(colony);var empire=state.FindEmpire(owner);
        if(c is null||c.EmpireId!=owner||c.Crisis?.Case is not {} illness||empire?.Crisis is not {} e)return Reject("no_owned_case");
        if(empire.Controller!=ControllerKind.Human)return Reject("destructive_authority_required");
        var d=content.Plagues.Values.Single();
        try
        {
            int lost=checked((int)(((Int128)c.Population*d.PurgeNumerator+d.PurgeDenominator-1)/d.PurgeDenominator));int after=checked(c.Population-lost);
            int civic=state.Turn<e.CivicExpiresAt?e.CivicLevel:0;
            var vessels=state.Vessels.Where(v=>v.EmpireId==owner).OrderBy(v=>v.Id).ToArray();
            return new(){Allowed=true,Turn=state.Turn,Owner=owner,Key=new(owner,illness.EpisodeId,c.Id),Location=Location(state,c),Case=CaseFact(state,c),SourcePortClosedBefore=PortClosed(state,owner,state.FindPlanet(c.PlanetId)!.StarId),SourcePortClosedAfter=state.Colonies.Any(x=>x.EmpireId==owner&&state.FindPlanet(x.PlanetId)!.StarId==state.FindPlanet(c.PlanetId)!.StarId&&x.Crisis?.Quarantined==true&&(x.Id!=colony||after>0&&x.Crisis.ReleaseEpisode!=illness.EpisodeId)),Revision=illness.AttentionRevision,PopulationBefore=c.Population,PopulationLost=lost,PopulationAfter=after,WorkforceBefore=c.Workforce,WorkforceAfter=AfterCasualties(c.Workforce,lost),ReserveBefore=c.ProductionReserve,ReserveAfter=c.ProductionReserve/2,GrowthAfter=0,ColonyLost=after==0,LastColony=state.Colonies.Count(x=>x.EmpireId==owner)==1,OwnerColonyCount=state.Colonies.Count(x=>x.EmpireId==owner),SurvivingFounders=vessels.Count(v=>v.CarriedPopulation>0&&content.Vessels[v.RoleId].CanFoundColony),CivicLevelBefore=e.CivicLevel,CivicExpiresAtBefore=e.CivicExpiresAt,NextCivicLevel=Math.Min(checked(civic+1),d.CivicMaxLevel),NextCivicExpiresAt=checked(state.Turn+d.CivicTurns),Assets=new(c.CompletedBuildingIds.ToImmutableArray(),c.ProjectId is {} project?new(c.ProjectKind,project):null,c.Development!.Queue.ToImmutableArray(),c.Development.Template,c.Development.WorkforceEnabled,c.Development.ConstructionEnabled,c.GrowthProgress),OtherCases=state.Colonies.Where(x=>x.EmpireId==owner&&x.Id!=colony&&x.Crisis?.Case is not null).OrderBy(x=>x.Id).Select(x=>CaseFact(state,x)).ToImmutableArray(),Carriers=vessels.Where(v=>v.Crisis?.CarrierEpisode is not null||v.Crisis?.InspectionDueTurn is not null).Select(v=>new CrisisCarrierFact(v.Id,v.RoleId,v.CarriedPopulation,v.Crisis!.CarrierEpisode,v.Crisis.InspectionDueTurn,v.StationaryStarId,v.Transit,v.PendingMove,v.PendingFounding)).ToImmutableArray(),Orders=vessels.Select(OrderFact).ToImmutableArray()};
        }
        catch(OverflowException){return Reject("crisis_limit");}
    }
    public static QuarantinePreview PreviewQuarantine(ContentSet content,GameState state,EmpireId owner,ImmutableArray<ColonyId> colonies,bool enable,bool releaseOnRecovery)
    {
        QuarantinePreview Reject(string reason)=>new(){Turn=state.Turn,Owner=owner,Enable=enable,ReleaseOnRecovery=releaseOnRecovery,RejectionReason=reason};
        if(!Rules.HasCrises(state.RulesVersion)||state.Crisis is not{Enabled:true})return Reject("crises_unavailable");
        if(colonies.IsDefaultOrEmpty||colonies.Distinct().Count()!=colonies.Length)return Reject("invalid_group");
        var facts=ImmutableArray.CreateBuilder<QuarantineColonyFact>();
        foreach(var id in colonies.OrderBy(x=>x))
        {
            var c=state.FindColony(id);
            if(c is null||c.EmpireId!=owner||c.Crisis is not {} health)return Reject("no_owned_colony");
            if(releaseOnRecovery&&(!enable||health.Case is null))return Reject("release_needs_case");
            facts.Add(new(c.Id,Location(state,c),health.Case?.EpisodeId,health.Case?.AttentionRevision??0,health.Quarantined,enable,health.ReleaseEpisode,enable&&releaseOnRecovery?health.Case?.EpisodeId:null,health.PolicyPaused,c.Population));
        }
        var before=ClosedPorts(state,owner);var frame=state.Clone();
        foreach(var fact in facts){var c=frame.FindColony(fact.Colony)!.Crisis!;c.Quarantined=enable;c.ReleaseEpisode=fact.ReleaseAfter;}
        var after=ClosedPorts(frame,owner);
        var cancelled=enable?state.Vessels.Where(v=>v.EmpireId==owner&&v.CarriedPopulation>0&&v.Transit is null&&(v.PendingMove is not null||v.PendingFounding is not null)&&((v.StationaryStarId is {} star&&after.Contains(star))||(v.PendingMove is {} move&&after.Contains(move))||(v.PendingFounding is {} target&&state.FindPlanet(target) is {} planet&&after.Contains(planet.StarId)))).OrderBy(v=>v.Id).Select(OrderFact).ToImmutableArray():ImmutableArray<VesselOrderFact>.Empty;
        var ownBefore=state.Clone();ownBefore.Empires.RemoveAll(e=>e.Id!=owner);ownBefore.Colonies.RemoveAll(c=>c.EmpireId!=owner);ownBefore.Vessels.RemoveAll(v=>v.EmpireId!=owner);
        frame.Empires.RemoveAll(e=>e.Id!=owner);frame.Colonies.RemoveAll(c=>c.EmpireId!=owner);frame.Vessels.RemoveAll(v=>v.EmpireId!=owner);
        var beforeEconomy=EconomyCalculator.Calculate(content,ownBefore);var afterEconomy=EconomyCalculator.Calculate(content,frame);
        var economic=ownBefore.Colonies.Where(c=>colonies.Contains(c.Id)||before.Contains(state.FindPlanet(c.PlanetId)!.StarId)||after.Contains(state.FindPlanet(c.PlanetId)!.StarId)).OrderBy(c=>c.Id).Select(c=>
        {
            var old=beforeEconomy.Colony(c.Id);var next=afterEconomy.Colony(c.Id);
            return new QuarantineEconomyFact(c.Id,c.Workforce,c.Population,c.GrowthProgress,c.ProductionReserve,c.ProjectId is {} project?new(c.ProjectKind,project):null,old.NetProduction.Total,next.NetProduction.Total,old.NetResearch.Total,next.NetResearch.Total,old.GrowthEarned.Total,next.GrowthEarned.Total,next.Production.BlockedReasonCode=="quarantine");
        }).ToImmutableArray();
        return new(){Allowed=true,Turn=state.Turn,Owner=owner,Enable=enable,ReleaseOnRecovery=releaseOnRecovery,Colonies=facts.ToImmutable(),ClosedPortsBefore=before,ClosedPortsAfter=after,CancelledOrders=cancelled,Economy=economic};
    }
    public static VesselOrderFact OrderFact(VesselState v)=>new(v.Id,v.StationaryStarId,v.Transit,v.PendingMove,v.PendingFounding,v.Crisis?.InspectionDueTurn);
    public static Workforce AfterCasualties(Workforce workforce,int lost)
    {
        if(lost<0||lost>workforce.Total)throw new ArgumentOutOfRangeException(nameof(lost));
        int research=Math.Min(workforce.Research,lost);lost-=research;int production=Math.Min(workforce.Production,lost);lost-=production;
        return new(workforce.Support-lost,workforce.Production-production,workforce.Research-research);
    }
    private static ImmutableArray<StarId> ClosedPorts(GameState state,EmpireId owner)=>state.Colonies.Where(c=>c.EmpireId==owner&&c.Crisis?.Quarantined==true).Select(c=>state.FindPlanet(c.PlanetId)!.StarId).Distinct().OrderBy(x=>x).ToImmutableArray();
    private static CrisisCaseFact CaseFact(GameState state,ColonyState c){var x=c.Crisis!;var illness=x.Case!;return new(new(c.EmpireId,illness.EpisodeId,c.Id),Location(state,c),c.Population,illness.Stage,illness.GraceRemaining,illness.ActiveAge,illness.Severity,illness.AttentionRevision,x.Quarantined,x.ReleaseEpisode,x.PolicyPaused);}
}
