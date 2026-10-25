using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ScoutingReportTests
{
    internal static LoadedGame NeutralSharedHome()
    {
        var g=ScoutingFixtures.TwoHumansAndAi();var s=g.State;s.Vessels[0].Scout!.Posture=ScoutPosture.AvoidEncounters;s.Vessels.Add(new(){Id=new(s.AllocateId()),EmpireId=s.Empires[1].Id,RoleId="scout",StationaryStarId=Fixtures.HomeStar,Crisis=new(),Scout=new()});ObservationUpdater.Refresh(s,0);ContactUpdater.Refresh(s.Clone(),s);return ScoutingFixtures.Valid(g);
    }
    [Fact] public void ActualStudyAndLossRemainReadableWithoutLiveVesselAndArePrivate()
    {
        var g=ScoutingResolutionTests.Risk(ScoutingFixtures.WithNextDraw(ScoutingFixtures.AtAmeba(),74),AmebaRiskAction.Study);var r=ScoutingResolutionTests.Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);g=ScoutingFixtures.WithNextDraw(g with{State=r.State},95);g=ScoutingReadyTests.Acknowledge(g);r=ScoutingResolutionTests.Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);Assert.Empty(r.State.Vessels);
        var view=ScoutQueries.Project(g.Content,r.State,r.State.Empires[0].Id);var history=view.History.SelectMany(d=>d.Entries).ToArray();Assert.Equal(600,Assert.Single(history,e=>e.Kind=="study").ResearchAward);Assert.Single(history,e=>e.Kind=="loss");Assert.Single(r.State.Empires[0].Scouting!.RetiredVessels);
        Assert.Single(EmpireViewProjector.ProjectReport(g.Content,r.State,r.Report!,view.Owner).ScoutEvents.Where(e=>e.Kind=="loss"));
        var multi=NeutralSharedHome();var own=ScoutTurnCoordinator.Prepare(multi.Content,multi.State,multi.State.Empires[0].Id);Assert.True(own.Succeeded);Assert.NotEmpty(own.State.Empires[0].Scouting!.PendingDigest!.Entries);Assert.DoesNotContain(ScoutQueries.Project(multi.Content,own.State,multi.State.Empires[1].Id).History.SelectMany(d=>d.Entries),e=>e.Owner==multi.State.Empires[0].Id);
        for(int i=0;i<12;i++){var before=r.State;var next=before.Clone();next.Turn++;ObservationUpdater.Refresh(next,next.Turn);CrisisReports.Finalize(before,next,CrisisResolutionContext.Capture(g.Content,before));ScoutReports.Finalize(before,next,AmebaResolutionContext.Capture(g.Content,before));r=r with{State=next};}
        Assert.True(r.State.Empires[0].Scouting!.CompletedDigests.Count<=10);Assert.Empty(r.State.Empires[0].Scouting!.RetiredVessels);Assert.Empty(GameStateValidator.Validate(g.Content,r.State));
    }
    [Fact] public void NeutralWaitIsPersistentButRecordedOnceAndDoesNotBlockReady()
    {
        var g=NeutralSharedHome();var owner=g.State.Empires[0].Id;var preview=ScoutQueries.Project(g.Content,g.State,owner);Assert.Contains("No known retreat",preview.Vessels[0].Blocker);Assert.Empty(ScoutQueries.PreviewPreparation(g.Content,g.State,owner).Blockers);
        int waits=0;
        for(int i=0;i<15;i++)
        {
            var p=ScoutTurnCoordinator.Prepare(g.Content,g.State,owner);Assert.True(p.Succeeded);var pending=p.State.Empires[0].Scouting!.PendingDigest;waits+=pending?.Entries.Count(e=>e.Kind=="wait")??0;
            var next=p.State.Clone();next.Turn++;ObservationUpdater.Refresh(next,next.Turn);ScoutReports.Finalize(p.State,next,AmebaResolutionContext.Capture(g.Content,p.State));g=g with{State=next};Assert.Contains("No known retreat",ScoutQueries.Project(g.Content,g.State,owner).Vessels[0].Blocker);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        }
        Assert.Equal(1,waits);Assert.NotNull(g.State.Vessels[0].Scout!.LastWaitSignature);Assert.True(g.State.Empires[0].Scouting!.CompletedDigests.Count<=10);
    }
    [Fact] public void NeutralAvoidPreparesKnownRetreatButDoesNotAutomaticallyExploreUnknownStars()
    {
        var g=NeutralSharedHome();var owner=g.State.Empires[0];owner.Surveys.Add(ObservationUpdater.Observe(g.State,new(14),0));owner.Surveys.Sort((a,b)=>a.StarId.CompareTo(b.StarId));var p=ScoutTurnCoordinator.Prepare(g.Content,g.State,owner.Id);Assert.True(p.Succeeded);Assert.Equal(new StarId(14),p.State.Vessels[0].PendingMove);Assert.Null(g.State.Vessels[0].PendingMove);
    }
    [Fact] public void ThirtyWarningsRemainOneGroupWithThirtyExactSelectableResponses()
    {
        var g=ScoutingFixtures.AtAmeba();var s=g.State;var owner=s.Empires[0];for(int i=1;i<30;i++)s.Vessels.Add(new(){Id=new(s.AllocateId()),EmpireId=owner.Id,RoleId="scout",StationaryStarId=new(8),Scout=new(),Crisis=new(),AmebaEncounter=new(){Number=owner.Scouting!.NextEncounterNumber++,HabitatId=1,ArrivalTurn=10}});g=ScoutingFixtures.Valid(g);
        var view=ScoutQueries.Project(g.Content,s,owner.Id);Assert.Equal(30,view.Attention.Length);Assert.All(view.Vessels,v=>Assert.Contains(v.Actions,c=>c is SetAmebaRiskOrderCommand {Action:AmebaRiskAction.Remain}));
        var ack=CommandProcessor.Execute(g.Content,s,new AcknowledgeAmebaCommand(owner.Id,10,view.Attention));Assert.True(ack.Accepted);Assert.Equal(30,ack.State.Empires[0].Scouting!.Receipts.Count);
    }
    [Fact] public void RepeatedPlanningTogglesCoalesceInsteadOfKeepingAClickLog()
    {
        var g=ScoutingFixtures.Game();var owner=g.State.Empires[0].Id;for(int i=0;i<100;i++){var r=CommandProcessor.Execute(g.Content,g.State,new SetScoutPostureCommand(owner,0,g.State.Vessels[0].Id,i%2==0?ScoutPosture.AvoidEncounters:ScoutPosture.Observe));Assert.True(r.Accepted);g=g with{State=r.State};}
        Assert.Single(g.State.Empires[0].Scouting!.PendingDigest!.Entries);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
    }
    [Fact] public void ReentryUsesNewEncounterNumberAndCannotReusePriorAcknowledgement()
    {
        var g=ScoutingReadyTests.Acknowledge(ScoutingFixtures.AtAmeba());var owner=g.State.Empires[0].Id;var old=ScoutQueries.Project(g.Content,g.State,owner).Attention;
        for(int i=0;i<2;i++){var r=ScoutingResolutionTests.Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);g=g with{State=r.State};}
        var approach=CommandProcessor.Execute(g.Content,g.State,new SetAmebaApproachOrderCommand(owner,g.State.Turn,g.State.Vessels[0].Id,new(8),1));Assert.True(approach.Accepted);g=g with{State=approach.State};
        for(int i=0;i<2;i++){var r=ScoutingResolutionTests.Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);g=g with{State=r.State};}
        Assert.Equal(2,g.State.Vessels[0].AmebaEncounter!.Number);Assert.Empty(g.State.Empires[0].Scouting!.Receipts);var stale=CommandProcessor.Execute(g.Content,g.State,new AcknowledgeAmebaCommand(owner,g.State.Turn,old));Assert.False(stale.Accepted);Assert.Same(g.State,stale.State);
    }
    [Fact] public void VanishedForeignShipEndsCautionWithoutCreatingABlacklistOrLossReport()
    {
        var g=NeutralSharedHome();var owner=g.State.Empires[0].Id;var p=ScoutTurnCoordinator.Prepare(g.Content,g.State,owner);Assert.True(p.Succeeded);g=g with{State=p.State};g.State.Vessels.RemoveAt(1);ObservationUpdater.Refresh(g.State,g.State.Turn);
        Assert.Null(ScoutQueries.Project(g.Content,g.State,owner).Vessels[0].Blocker);p=ScoutTurnCoordinator.Prepare(g.Content,g.State,owner);Assert.True(p.Succeeded);Assert.Null(p.State.Vessels[0].Scout!.LastWaitSignature);Assert.DoesNotContain(ScoutQueries.Project(g.Content,p.State,owner).History.SelectMany(d=>d.Entries),e=>e.Kind=="loss");
    }
}
