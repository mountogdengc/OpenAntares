using System;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisCommandTests
{
    [Fact]
    public void PurgeUsesExactApprovedCosts()
    {
        var g=PurgeFixture(5);var c=g.State.Colonies[0];var before=c.Clone();var preview=CrisisQueries.PreviewPurge(g.Content,g.State,c.EmpireId,c.Id);
        var result=CommandProcessor.Execute(g.Content,g.State,new PurgeColonyCommand(c.EmpireId,g.State.Turn,preview));
        Assert.True(result.Accepted,result.Rejection?.Message);var after=result.State.FindColony(c.Id)!;
        Assert.Equal(2,after.Population);Assert.Equal(new Workforce(2,0,0),after.Workforce);Assert.Equal(450,after.ProductionReserve);Assert.Equal(0,after.GrowthProgress);
        Assert.Equal(before.CompletedBuildingIds,after.CompletedBuildingIds);Assert.Equal(before.ProjectId,after.ProjectId);Assert.Equal(before.Development!.Queue,after.Development!.Queue);Assert.True(after.Development.WorkforceEnabled);Assert.True(after.Development.ConstructionEnabled);
        Assert.Null(after.Crisis!.Case);Assert.Equal(34,after.Crisis.ImmuneUntilTurn);Assert.Equal(1,result.State.Empires[0].Crisis!.CivicLevel);Assert.Equal(27,result.State.Empires[0].Crisis!.CivicExpiresAt);
        Assert.Contains(result.State.Empires[0].Crisis!.PendingDigests.SelectMany(x=>x.Entries),e=>e.Disposition=="direct_order"&&e.PopulationLost==3);
        Assert.Equal(5,c.Population);Assert.Empty(CrisisQueries.PendingAttention(result.State,c.EmpireId));AssertValidAndRoundTrips(g.Content,result.State);
    }
    [Theory][InlineData(1,0)][InlineData(2,1)]
    public void PurgeCanLoseLastColonyWithoutFreeRescue(int population,int remaining)
    {
        var g=PurgeFixture(population);var c=g.State.Colonies[0];int vessels=g.State.Vessels.Count;
        var result=CommandProcessor.Execute(g.Content,g.State,new PurgeColonyCommand(c.EmpireId,g.State.Turn,CrisisQueries.PreviewPurge(g.Content,g.State,c.EmpireId,c.Id)));
        Assert.True(result.Accepted,result.Rejection?.Message);Assert.Equal(remaining,result.State.FindColony(c.Id)?.Population??0);Assert.Equal(vessels,result.State.Vessels.Count);Assert.Equal(g.State.Planets.Count,result.State.Planets.Count);
        AssertValidAndRoundTrips(g.Content,result.State);
    }
    [Fact]
    public void StaleCrossColonyCivicPreviewRejectsAtomically()
    {
        var g=TwoOwnCases();var c=g.State.Colonies[0];var second=g.State.Colonies[1];
        var stale=CrisisQueries.PreviewPurge(g.Content,g.State,c.EmpireId,c.Id);
        var other=CommandProcessor.Execute(g.Content,g.State,new PurgeColonyCommand(c.EmpireId,g.State.Turn,CrisisQueries.PreviewPurge(g.Content,g.State,c.EmpireId,second.Id)));
        Assert.True(other.Accepted,other.Rejection?.Message);string before=SaveGame.Serialize(g.Content,other.State);
        var result=CommandProcessor.Execute(g.Content,other.State,new PurgeColonyCommand(c.EmpireId,g.State.Turn,stale));
        Assert.False(result.Accepted);Assert.Same(other.State,result.State);Assert.Equal(before,SaveGame.Serialize(g.Content,result.State));
    }
    [Fact]
    public void StaleTransportOrAssetPreviewRejectsAtomically()
    {
        foreach(string mutation in new[]{"order","queue","civic","carrier"})
        {
            var g=PurgeFixture(5);var c=g.State.Colonies[0];var preview=CrisisQueries.PreviewPurge(g.Content,g.State,c.EmpireId,c.Id);
            switch(mutation){case "order":g.State.Vessels[0].PendingMove=new(6);break;case "queue":c.Development!.Queue.Clear();break;case "civic":g.State.Empires[0].Crisis!.CivicLevel=1;g.State.Empires[0].Crisis!.CivicExpiresAt=25;break;case "carrier":var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.Crisis!.CarrierEpisode=1;break;}
            AssertRejectedUnchanged(g,new PurgeColonyCommand(c.EmpireId,g.State.Turn,preview));
        }
    }
    [Fact]
    public void StaleSourcePortAndReleaseConsequencesRejectPurge()
    {
        var g=PurgeFixture(5);var c=g.State.Colonies[0];var preview=CrisisQueries.PreviewPurge(g.Content,g.State,c.EmpireId,c.Id);
        var q=CommandProcessor.Execute(g.Content,g.State,new SetQuarantineCommand(c.EmpireId,g.State.Turn,CrisisQueries.PreviewQuarantine(g.Content,g.State,c.EmpireId,ImmutableArray.Create(c.Id),true,true)));
        Assert.True(q.Accepted,q.Rejection?.Message);
        AssertRejectedUnchanged(new(g.Content,q.State),new PurgeColonyCommand(c.EmpireId,g.State.Turn,preview));
    }
    [Fact]
    public void GroupedForeignAcknowledgementRejectsAll()
    {
        var g=CrisisFixtures.Warning();var c=g.State.Colonies[0];var own=Assert.Single(CrisisQueries.PendingAttention(g.State,c.EmpireId));
        AssertRejectedUnchanged(g,new AcknowledgeCrisisCommand(c.EmpireId,g.State.Turn,ImmutableArray.Create(own,own with{Key=own.Key with{Owner=new(999)}})));
        var result=CommandProcessor.Execute(g.Content,g.State,new AcknowledgeCrisisCommand(c.EmpireId,g.State.Turn,ImmutableArray.Create(own)));
        Assert.True(result.Accepted,result.Rejection?.Message);Assert.Empty(CrisisQueries.PendingAttention(result.State,c.EmpireId));
        Assert.Contains(result.State.Empires[0].Crisis!.PendingDigests.SelectMany(d=>d.Entries),e=>e.Disposition=="handled");
    }
    [Fact]
    public void QuarantineCancelsOnlyDisplayedOwnOrders()
    {
        var g=TwoOwnCases();var s=g.State;var c=s.Colonies[0];var owner=c.EmpireId;
        s.Vessels.Add(new(){Id=new(s.AllocateId()),EmpireId=owner,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=new(6),PendingMove=Fixtures.HomeStar,Crisis=new()});
        s.Vessels[0].PendingMove=new(6);ObservationUpdater.Refresh(s,s.Turn);
        var expected=CrisisQueries.PreviewQuarantine(g.Content,s,owner,ImmutableArray.Create(c.Id),true,true);Assert.Single(expected.CancelledOrders);
        var result=CommandProcessor.Execute(g.Content,s,new SetQuarantineCommand(owner,s.Turn,expected));
        Assert.True(result.Accepted,result.Rejection?.Message);Assert.Null(result.State.Vessels[1].PendingMove);Assert.Equal(new StarId(6),result.State.Vessels[0].PendingMove);
        Assert.True(result.State.FindColony(c.Id)!.Crisis!.Quarantined);Assert.Equal(1,result.State.FindColony(c.Id)!.Crisis!.ReleaseEpisode);Assert.True(result.State.FindColony(c.Id)!.Crisis!.PolicyPaused);
        Assert.True(result.State.FindColony(c.Id)!.Development!.WorkforceEnabled);AssertValidAndRoundTrips(g.Content,result.State);
    }
    [Fact]
    public void ManualResponsePausesOnlyItsCrisisChannelAndResumeIsExplicit()
    {
        var g=TwoOwnCases();var c=g.State.Colonies[0];var owner=c.EmpireId;
        var q=CommandProcessor.Execute(g.Content,g.State,new SetQuarantineCommand(owner,g.State.Turn,CrisisQueries.PreviewQuarantine(g.Content,g.State,owner,ImmutableArray.Create(c.Id),true,true)));
        Assert.True(q.Accepted,q.Rejection?.Message);Assert.False(q.State.Colonies[1].Crisis!.PolicyPaused);Assert.False(q.State.Empires[0].Crisis!.ResearchPolicyPaused);
        var share=CommandProcessor.Execute(g.Content,q.State,new SetCureShareCommand(owner,g.State.Turn,1,25));Assert.True(share.Accepted);Assert.True(share.State.Empires[0].Crisis!.ResearchPolicyPaused);Assert.True(share.State.Colonies[0].Development!.ConstructionEnabled);
        var resume=CommandProcessor.Execute(g.Content,share.State,new ResumeCrisisPolicyCommand(owner,g.State.Turn,ImmutableArray.Create(c.Id),true));Assert.True(resume.Accepted);Assert.False(resume.State.Colonies[0].Crisis!.PolicyPaused);Assert.False(resume.State.Empires[0].Crisis!.ResearchPolicyPaused);Assert.False(resume.State.Empires[0].Crisis!.Episode!.ResearchOverride);
    }
    [Fact]
    public void AutomaticResponseUsesSameCostsWithoutPausingManualChannels()
    {
        var g=CrisisFixtures.Warning();g.State.Empires[0].Crisis!.Authority=CrisisAuthority.Contain;var c=g.State.Colonies[0];var expected=CrisisQueries.PreviewQuarantine(g.Content,g.State,c.EmpireId,ImmutableArray.Create(c.Id),true,true);
        var result=CommandProcessor.ExecuteCrisisAutomatic(g.Content,g.State,new SetQuarantineCommand(c.EmpireId,g.State.Turn,expected));
        Assert.True(result.Accepted,result.Rejection?.Message);Assert.True(result.State.Colonies[0].Crisis!.Quarantined);Assert.False(result.State.Colonies[0].Crisis!.PolicyPaused);
        AssertRejectedUnchanged(g,new PurgeColonyCommand(c.EmpireId,g.State.Turn,CrisisQueries.PreviewPurge(g.Content,g.State,c.EmpireId,c.Id)),automatic:true);
    }
    [Fact]
    public void AutomaticCannotBypassManualOrPausedAuthority()
    {
        var g=CrisisFixtures.Warning();var c=g.State.Colonies[0];
        var q=new SetQuarantineCommand(c.EmpireId,g.State.Turn,CrisisQueries.PreviewQuarantine(g.Content,g.State,c.EmpireId,ImmutableArray.Create(c.Id),true,true));
        AssertRejectedUnchanged(g,q,automatic:true);
        g.State.Empires[0].Crisis!.Authority=CrisisAuthority.Contain;c.Crisis!.PolicyPaused=true;
        q=new(c.EmpireId,g.State.Turn,CrisisQueries.PreviewQuarantine(g.Content,g.State,c.EmpireId,ImmutableArray.Create(c.Id),true,true));AssertRejectedUnchanged(g,q,automatic:true);
        g.State.Empires[0].Crisis!.Authority=CrisisAuthority.ContainAndResearch;g.State.Empires[0].Crisis!.ResearchPolicyPaused=true;
        AssertRejectedUnchanged(g,new SetCureShareCommand(c.EmpireId,g.State.Turn,1,50),automatic:true);
    }
    [Fact]
    public void ForeignQuarantineDoesNotBlockOwnCargoMovement()
    {
        var g=CrisisFixtures.TwoHumansAndAi();var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;g.State.Colonies[1].Crisis!.Quarantined=true;
        Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        var result=CommandProcessor.Execute(g.Content,g.State,new SetMoveOrderCommand(v.EmpireId,0,v.Id,new(6)));Assert.True(result.Accepted,result.Rejection?.Message);
    }
    [Fact]
    public void QuarantineAndInspectionBlockFoundingButReleaseDoesNotRestoreOrders()
    {
        var g=CrisisFixtures.Active(quarantine:true);var s=g.State;var v=s.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;
        var planet=new PlanetState{Id=new(s.AllocateId()),StarId=Fixtures.HomeStar,Orbit=2,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea",Appearance=new(1,1,8)};s.Planets.Add(planet);ObservationUpdater.Refresh(s,s.Turn);
        AssertRejectedUnchanged(g,new SetFoundingOrderCommand(v.EmpireId,s.Turn,v.Id,planet.Id));
        var release=CommandProcessor.Execute(g.Content,s,new SetQuarantineCommand(v.EmpireId,s.Turn,CrisisQueries.PreviewQuarantine(g.Content,s,v.EmpireId,ImmutableArray.Create(s.Colonies[0].Id),false,false)));
        Assert.True(release.Accepted,release.Rejection?.Message);Assert.Null(release.State.Vessels[0].PendingFounding);
        var found=CommandProcessor.Execute(g.Content,release.State,new SetFoundingOrderCommand(v.EmpireId,s.Turn,v.Id,planet.Id));Assert.True(found.Accepted,found.Rejection?.Message);
        // Separate labelled held state before issuing any command: cure need not leave an infection flag.
        release.State.Vessels[0].Crisis!.InspectionDueTurn=s.Turn+1;
        AssertRejectedUnchanged(new(g.Content,release.State),new SetFoundingOrderCommand(v.EmpireId,s.Turn,v.Id,planet.Id));
    }
    [Fact]
    public void CivicRefreshUsesCurrentPlanningTurnAndCap()
    {
        var g=PurgeFixture(5);g.State.Turn=24;var e=g.State.Empires[0].Crisis!;e.CivicLevel=3;e.CivicExpiresAt=27;ObservationUpdater.Refresh(g.State,24);
        var c=g.State.Colonies[0];var result=CommandProcessor.Execute(g.Content,g.State,new PurgeColonyCommand(c.EmpireId,24,CrisisQueries.PreviewPurge(g.Content,g.State,c.EmpireId,c.Id)));
        Assert.True(result.Accepted,result.Rejection?.Message);Assert.Equal(3,result.State.Empires[0].Crisis!.CivicLevel);Assert.Equal(29,result.State.Empires[0].Crisis!.CivicExpiresAt);
    }
    [Fact]
    public void WaiverIsSavedAsWaivedAndReenableRenewsCurrentFacts()
    {
        var g=CrisisFixtures.Warning();var c=g.State.Colonies[0];var owner=c.EmpireId;
        var waive=CommandProcessor.Execute(g.Content,g.State,new SetCrisisAcknowledgementCommand(owner,g.State.Turn,false));Assert.True(waive.Accepted,waive.Rejection?.Message);
        var illness=waive.State.Colonies[0].Crisis!.Case!;Assert.Equal(0,illness.HandledRevision);Assert.Equal(1,illness.WaivedRevision);Assert.Empty(CrisisQueries.PendingAttention(waive.State,owner));
        Assert.Contains(waive.State.Empires[0].Crisis!.PendingDigests.SelectMany(d=>d.Entries),e=>e.Disposition=="waived");AssertValidAndRoundTrips(g.Content,waive.State);
        var require=CommandProcessor.Execute(g.Content,waive.State,new SetCrisisAcknowledgementCommand(owner,g.State.Turn,true));Assert.True(require.Accepted);Assert.Single(CrisisQueries.PendingAttention(require.State,owner));
    }
    [Fact]
    public void DecontaminationCancelsOnlyConfirmedOrderAndHoldsFullInterval()
    {
        var g=CrisisFixtures.Active();var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.Crisis!.CarrierEpisode=1;v.PendingMove=new(6);
        var expected=ImmutableArray.Create(CrisisQueries.OrderFact(v));
        var result=CommandProcessor.Execute(g.Content,g.State,new DecontaminateVesselCommand(v.EmpireId,g.State.Turn,v.Id,expected));
        Assert.True(result.Accepted,result.Rejection?.Message);Assert.Null(result.State.Vessels[0].PendingMove);Assert.Equal(g.State.Turn+1,result.State.Vessels[0].Crisis!.InspectionDueTurn);Assert.Equal(1,result.State.Vessels[0].Crisis!.CarrierEpisode);AssertValidAndRoundTrips(g.Content,result.State);
        AssertRejectedUnchanged(g,new DecontaminateVesselCommand(v.EmpireId,g.State.Turn,v.Id,ImmutableArray<VesselOrderFact>.Empty));
    }
    [Fact]
    public void ClosedPortsBlockCargoOrdersButNeverScoutOrCancellation()
    {
        var g=CrisisFixtures.Active(quarantine:true);var s=g.State;var v=s.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;
        AssertRejectedUnchanged(g,new SetMoveOrderCommand(v.EmpireId,s.Turn,v.Id,new(6)));
        v.PendingMove=new(6);var cancel=CommandProcessor.Execute(g.Content,s,new SetMoveOrderCommand(v.EmpireId,s.Turn,v.Id,null));Assert.True(cancel.Accepted);Assert.Null(cancel.State.Vessels[0].PendingMove);
        v.PendingMove=null;v.RoleId="scout";v.CarriedPopulation=0;var scout=CommandProcessor.Execute(g.Content,s,new SetMoveOrderCommand(v.EmpireId,s.Turn,v.Id,new(6)));Assert.True(scout.Accepted);
    }
    [Fact]
    public void PurgedHistoryAndStaleForeignSurveyRoundTripBeforeTurnIntegration()
    {
        var g=PurgeFixture(1);var c=g.State.Colonies[0];
        g.State.Empires[0].Development!.CompletedReports.Add(new(g.State.Turn-1,ImmutableArray.Create(new GovernorReportEntry(c.Id,null,null,null,null,ImmutableArray<GovernorIssue>.Empty,false,false)),0));
        var second=new EmpireState{Id=new(g.State.AllocateId()),Controller=ControllerKind.Human,SpeciesId="harl",Development=new(),Crisis=new(){Random=CrisisInitializer.CreateRandom(42,new(g.State.NextEntityId-1))}};
        second.Surveys.Add(ObservationUpdater.Observe(g.State,Fixtures.HomeStar,g.State.Turn));g.State.Empires.Add(second);
        Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        var result=CommandProcessor.Execute(g.Content,g.State,new PurgeColonyCommand(c.EmpireId,g.State.Turn,CrisisQueries.PreviewPurge(g.Content,g.State,c.EmpireId,c.Id)));
        Assert.True(result.Accepted,result.Rejection?.Message);Assert.Single(result.State.Empires[0].Development!.CompletedReports);Assert.Single(result.State.Empires[0].Crisis!.LostColonyLocations);AssertValidAndRoundTrips(g.Content,result.State);
    }
    [Theory][InlineData("stale_turn")][InlineData("wrong_owner")][InlineData("bad_share")][InlineData("missing_episode")][InlineData("wrong_revision")][InlineData("revision_overflow")][InlineData("expiry_overflow")]
    public void InvalidRequestsAreAtomic(string kind)
    {
        var g=CrisisFixtures.Warning();var c=g.State.Colonies[0];var owner=c.EmpireId;
        CrisisCommand command=kind switch
        {
            "stale_turn"=>new SetCureShareCommand(owner,g.State.Turn-1,1,50),
            "wrong_owner"=>new PurgeColonyCommand(new(999),g.State.Turn,CrisisQueries.PreviewPurge(g.Content,g.State,owner,c.Id)),
            "bad_share"=>new SetCureShareCommand(owner,g.State.Turn,1,30),
            "missing_episode"=>new SetCureShareCommand(owner,g.State.Turn,9,50),
            "wrong_revision"=>new AcknowledgeCrisisCommand(owner,g.State.Turn,ImmutableArray.Create(CrisisQueries.PendingAttention(g.State,owner)[0] with{Revision=99})),
            _=>new PurgeColonyCommand(owner,g.State.Turn,CrisisQueries.PreviewPurge(g.Content,g.State,owner,c.Id))
        };
        if(kind=="revision_overflow"){c.Crisis!.Case!.AttentionRevision=long.MaxValue-1;command=new PurgeColonyCommand(owner,g.State.Turn,CrisisQueries.PreviewPurge(g.Content,g.State,owner,c.Id));}
        if(kind=="expiry_overflow"){g.State.Turn=int.MaxValue-2;ObservationUpdater.Refresh(g.State,g.State.Turn);command=new PurgeColonyCommand(owner,g.State.Turn,CrisisQueries.PreviewPurge(g.Content,g.State,owner,c.Id));}
        AssertRejectedUnchanged(g,command);
    }
    [Fact]
    public void NoOpsKeepReferenceAndReadyAndRealChangesClearOnlyIssuer()
    {
        var g=CrisisFixtures.TwoHumansAndAi();var owner=g.State.Empires[0];owner.Ready=true;
        var noOp=CommandProcessor.Execute(g.Content,g.State,new SetCrisisAuthorityCommand(owner.Id,0,CrisisAuthority.Manual,50));Assert.True(noOp.Accepted);Assert.False(noOp.Changed);Assert.Same(g.State,noOp.State);Assert.True(noOp.State.Empires[0].Ready);
        var changed=CommandProcessor.Execute(g.Content,g.State,new SetCrisisAuthorityCommand(owner.Id,0,CrisisAuthority.Contain,50));Assert.True(changed.Accepted);Assert.False(changed.State.Empires[0].Ready);Assert.False(changed.State.Empires[1].Ready);
        g.State.Empires[0].Ready=false;g.State.Empires[1].Ready=true;
        changed=CommandProcessor.Execute(g.Content,g.State,new SetCrisisAuthorityCommand(owner.Id,0,CrisisAuthority.Contain,50));Assert.True(changed.Accepted);Assert.True(changed.State.Empires[1].Ready);
    }
    [Fact]
    public void AiAndCopiedForeignPreviewCannotPurge()
    {
        var g=CrisisFixtures.TwoHumansAndAi();var ai=g.State.Empires[2];var c=g.State.Colonies[2];var location=CrisisFixtures.Location(g.State,c);
        ai.Crisis!.NextEpisodeNumber=2;ai.Crisis.Episode=new(){Id=1,FamilyId="ash_bloom",Source=location,SourcePopulation=6,CureCost=1200};c.Crisis!.Case=new(){EpisodeId=1,Stage=CrisisStage.Warning,GraceRemaining=2,AttentionRevision=1,Origin=location};
        AssertRejectedUnchanged(g,new PurgeColonyCommand(ai.Id,0,CrisisQueries.PreviewPurge(g.Content,g.State,ai.Id,c.Id)));
        var own=CrisisFixtures.Active();var ownC=own.State.Colonies[0];var preview=CrisisQueries.PreviewPurge(own.Content,own.State,ownC.EmpireId,ownC.Id);
        AssertRejectedUnchanged(g,new PurgeColonyCommand(g.State.Empires[1].Id,0,preview with{Owner=g.State.Empires[1].Id,Turn=0}));
    }
    [Fact]
    public void LocalPurgeDoesNotClearOtherCasesCarriersOrCureProgress()
    {
        var g=TwoOwnCases();var c=g.State.Colonies[0];var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.StationaryStarId=null;v.Transit=new(Fixtures.HomeStar,new(6),2,1);v.Crisis!.CarrierEpisode=1;g.State.Empires[0].Crisis!.Episode!.CureProgress=333;
        var result=CommandProcessor.Execute(g.Content,g.State,new PurgeColonyCommand(c.EmpireId,g.State.Turn,CrisisQueries.PreviewPurge(g.Content,g.State,c.EmpireId,c.Id)));
        Assert.True(result.Accepted,result.Rejection?.Message);Assert.NotNull(result.State.Colonies[1].Crisis!.Case);Assert.Equal(1,result.State.Vessels[0].Crisis!.CarrierEpisode);Assert.Equal(v.Transit,result.State.Vessels[0].Transit);Assert.Equal(333,result.State.Empires[0].Crisis!.Episode!.CureProgress);Assert.Equal(1100,result.State.Empires[0].Crisis!.Episode!.CureCost);
        AssertValidAndRoundTrips(g.Content,result.State);
    }
    [Theory][InlineData("purge")][InlineData("quarantine")][InlineData("ack")][InlineData("resume")][InlineData("orders")][InlineData("episode")]
    public void MissingConfirmationOrImpossibleIdentifiersNeverMutate(string kind)
    {
        var g=CrisisFixtures.Warning();var c=g.State.Colonies[0];var owner=c.EmpireId;
        CrisisCommand command=kind switch{"purge"=>new PurgeColonyCommand(owner,g.State.Turn,null!),"quarantine"=>new SetQuarantineCommand(owner,g.State.Turn,null!),"ack"=>new AcknowledgeCrisisCommand(owner,g.State.Turn,default),"resume"=>new ResumeCrisisPolicyCommand(owner,g.State.Turn,default,true),"orders"=>new DecontaminateVesselCommand(owner,g.State.Turn,g.State.Vessels[0].Id,default),_=>new SetCureShareCommand(owner,g.State.Turn,long.MaxValue,50)};
        AssertRejectedUnchanged(g,command);
    }
    private static LoadedGame PurgeFixture(int population)
    {
        var g=CrisisFixtures.Active(population:population);var c=g.State.Colonies[0];c.Workforce=population==5?new(2,2,1):new(population,0,0);c.GrowthProgress=55;c.ProductionReserve=901;c.CompletedBuildingIds=new(){"analysis_lab","fabrication_hall"};g.State.Empires[0].KnownTechnologyIds=g.Content.Technologies.Keys.ToList();
        c.ProjectId="production_complex";c.Development!.Origin=ProductionOrigin.Manual;c.Development.Template=ColonyFixtures.Snapshot(g.Content,"industrial");c.Development.WorkforceEnabled=true;c.Development.ConstructionEnabled=true;c.Development.Queue.Add(new(1,new("building","discovery_center")));c.Development.NextQueueToken=2;
        Assert.Empty(GameStateValidator.Validate(g.Content,g.State));return g;
    }
    private static LoadedGame TwoOwnCases()
    {
        var g=PurgeFixture(5);var c=g.State.Colonies[0];var second=new ColonyState{Id=new(g.State.AllocateId()),EmpireId=c.EmpireId,PlanetId=new(7),Population=3,Workforce=new(3,0,0),HabitatMode="native",Development=new(),Crisis=new(){Case=c.Crisis!.Case!.Clone()}};g.State.Colonies.Add(second);ObservationUpdater.Refresh(g.State,g.State.Turn);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));return g;
    }
    private static void AssertRejectedUnchanged(LoadedGame g,Command command,bool automatic=false)
    {
        string before=SaveGame.Serialize(g.Content,g.State);var result=automatic?CommandProcessor.ExecuteCrisisAutomatic(g.Content,g.State,(CrisisCommand)command):CommandProcessor.Execute(g.Content,g.State,command);
        Assert.False(result.Accepted);Assert.False(result.Changed);Assert.Same(g.State,result.State);Assert.Equal(before,SaveGame.Serialize(g.Content,result.State));
    }
    private static void AssertValidAndRoundTrips(OpenAntares.Simulation.Content.ContentSet content,GameState state)
    {
        Assert.Empty(GameStateValidator.Validate(content,state));string text=SaveGame.Serialize(content,state);var loaded=SaveGame.Deserialize(text);Assert.True(loaded.Succeeded,string.Join("; ",loaded.Errors));Assert.Equal(text,SaveGame.Serialize(loaded.Game!.Content,loaded.Game.State));
    }
}
