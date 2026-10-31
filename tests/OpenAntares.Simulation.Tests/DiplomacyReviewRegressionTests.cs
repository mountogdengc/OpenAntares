using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacyReviewRegressionTests
{
    [Theory][InlineData(false)][InlineData(true)] public void MultipleContactsBindSequentialOwnReservations(bool thirdAi)
    {
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create());var state=s.State;var b=state.Empires[1].Id;
        var third=state.Empires[0].Clone();third.Id=new(state.AllocateId());third.Ready=false;third.Contacts.Clear();third.Surveys.Clear();third.Controller=thirdAi?ControllerKind.Ai:ControllerKind.Human;third.AiPolicyId=thirdAi?"expansion_v1":null;
        third.Crisis=new(){Random=CrisisInitializer.CreateRandom(state.GalaxySeed!.Value,third.Id)};third.Fleets!.RecoveryRandom=FleetInitializer.RecoveryRandom(state.GalaxySeed.Value,third.Id);third.Fleets.SurveyCredits.Clear();
        foreach(var o in third.Fleets.Officers){o.Status=OfficerStatus.Retired;o.PostedColony=null;o.Flagship=null;o.Request=null;o.RecoveryColony=null;o.AvailableAtTurn=null;o.FinalSummary="Retired before this labelled review supplement.";}
        state.Empires.Add(third);state.Empires[1].Contacts.Add(new(third.Id,third.SpeciesId!,0));third.Contacts.Add(new(b,state.Empires[1].SpeciesId!,0));var hub=state.Colonies.Single(c=>c.EmpireId==b);hub.ProductionReserve=500;
        state.Diplomacy!.Pairs.Add(new(){Pair=DiplomacyPair.For(b,third.Id),NextOfferId=2,Parties=new(){new(){Owner=b},new(){Owner=third.Id}},Pending=new(){Id=new(1),Kind=AgreementKind.Settlement,Proposer=third.Id,Recipient=b,CreatedTurn=0,ExpiryTurn=3,Star=state.FindPlanet(hub.PlanetId)!.StarId}});foreach(var party in state.Diplomacy.Pairs[0].Parties)party.Attitude.Streak=5;
        Assert.Empty(GameStateValidator.Validate(s.Content,state));var before=SaveGame.Serialize(s.Content,state);Assert.True(DiplomacyTurnCoordinator.Prepare(s.Content,state).Succeeded);Assert.Equal(before,SaveGame.Serialize(s.Content,state));
        var actual=TurnResolver.Resolve(s.Content,state);Assert.True(actual.Succeeded);Assert.Equal(1,actual.State.Turn);Assert.Empty(GameStateValidator.Validate(s.Content,actual.State));
    }
    [Fact]public void OneBoundaryAiOfferProvidesAFullPlanningOpportunity()
    {
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create());var state=s.State;foreach(var party in state.Diplomacy!.Pairs[0].Parties)party.Attitude.Streak=5;foreach(var funded in state.Colonies)funded.ProductionReserve=500;
        s=new(s.Content with{Diplomacy=s.Content.Diplomacy! with{OfferLifetime=1}},state);var outcome=s.Execute(new SetReadyCommand(state.Empires[0].Id,0,true));Assert.True(outcome.Accepted);var offer=Assert.IsType<DiplomacyOffer>(s.State.Diplomacy!.Pairs[0].Pending);Assert.Equal(1,offer.CreatedTurn);Assert.Equal(2,offer.ExpiryTurn);Assert.True(SaveGame.Deserialize(SaveGame.Serialize(s.Content,s.State)).Succeeded);
        Assert.True(DiplomacyCommandTests.Act(s,state.Empires[0].Id,new DiplomacyAction.Answer(offer.Id,true,s.View(state.Empires[0].Id).Colonies.Single().Id)).Accepted);Assert.True(s.Execute(new SetReadyCommand(state.Empires[0].Id,1,true)).Accepted);Assert.Contains(s.State.Diplomacy!.Pairs[0].Agreements,a=>a.Kind==AgreementKind.Trade);
    }
    [Theory][InlineData(false)][InlineData(true)] public void SupportedNearLimitSavedDatesProjectSafely(bool trade)
    {
        var s=trade?DiplomacyTradeTests.Activated():DiplomacyCommandTests.Funded();s.State.Turn=int.MaxValue-1;var p=s.State.Diplomacy!.Pairs[0];if(!trade)p.Agreements.Add(new(){Kind=AgreementKind.Settlement,Star=s.State.Stars[0].Id});var a=p.Agreements.Single();a.ActivatedTurn=s.State.Turn;if(trade){a.LowHub=a.LowHub! with{ActivationTurn=s.State.Turn};a.HighHub=a.HighHub! with{ActivationTurn=s.State.Turn};}ObservationUpdater.Refresh(s.State,s.State.Turn);Assert.Empty(GameStateValidator.Validate(s.Content,s.State));var loaded=SaveGame.Deserialize(SaveGame.Serialize(s.Content,s.State));Assert.True(loaded.Succeeded);s=new(loaded.Game!.Content,loaded.Game.State);var view=s.View(s.State.Empires[0].Id).Diplomacy!.Contacts.Single().Agreements.Single();Assert.Equal((long)s.State.Turn+s.Content.Diplomacy!.Minimum,view.FirstTerminationTurn);if(trade)Assert.Equal(3,view.WarmupRemaining);
    }
    [Fact]public void LiveTradeSurvivesOwnHypotheticalFrames()
    {
        var s=DiplomacyTradeTests.Activated();s.State.Turn=4;ObservationUpdater.Refresh(s.State,4);var owner=s.State.Empires[0].Id;var c=s.State.Colonies.Single(c=>c.EmpireId==owner);c.Workforce=new(c.Population,0,0);var view=s.View(owner);var own=view.Colonies.Single();Assert.Equal(100,own.Economy.NetProduction.Total);
        var governor=GovernorPlanningQueries.Create(s.Content,view).Forecast(c.Id,c.Workforce,null)!;var rival=RivalPlanningQueries.Create(s.Content,s.State,owner).Forecast(c.Id,c.Workforce,null)!;Assert.Equal(100,governor.NetProduction.Total);Assert.Equal(100,rival.NetProduction.Total);Assert.Equal(own.Economy.Production.Ending,governor.Production.Ending);
    }
    [Fact]public void ReleaseAlternativeIsConditionalAndIgnoresHiddenAnswers()
    {
        var s=DiplomacyOfferResolutionTests.Offered(false,2);var hub=s.State.Colonies[0];var a=hub.EmpireId;var b=s.State.Empires[1].Id;hub.ProjectId="fabrication_hall";hub.ProjectKind=ProductionKinds.Building;hub.ProductionReserve=1200-s.Forecast().Colony(hub.Id).NetProduction.Total;Assert.Empty(GameStateValidator.Validate(s.Content,s.State));
        var property=typeof(OwnedColonyView).GetProperty("ProductionIfSetupReleased");Assert.NotNull(property);var before=s.View(a).Colonies.Single();var release=Assert.IsType<ReserveOutcome>(property!.GetValue(before));Assert.Null(before.Economy.Production.CompletedId);Assert.Equal("fabrication_hall",release.CompletedId);var token=s.View(a).Diplomacy!.Contacts.Single().PrivateRevision;
        var offer=s.State.Diplomacy!.Pairs[0].Pending!;Assert.True(DiplomacyCommandTests.Act(s,b,new DiplomacyAction.Answer(offer.Id,false,null)).Accepted);var after=s.View(a).Colonies.Single();Assert.Equal(System.Text.Json.JsonSerializer.Serialize(release),System.Text.Json.JsonSerializer.Serialize(property.GetValue(after)));Assert.Equal(token,s.View(a).Diplomacy!.Contacts.Single().PrivateRevision);var actual=TurnResolver.Resolve(s.Content,s.State);Assert.True(actual.Succeeded);Assert.Equal(release.CompletedId,actual.Report!.Economy.Colony(hub.Id).Production.CompletedId);
    }
    [Fact]public void IncomingOfferIsReportedOnceAndWithdrawalRemainsReviewable()
    {
        var s=DiplomacyOfferResolutionTests.Offered(false);var p=s.State.Diplomacy!.Pairs[0];var first=TurnResolver.Resolve(s.Content,s.State);Assert.True(first.Succeeded);Assert.Contains(first.Report!.DiplomacyEvents!.Value,e=>e.Viewer==p.Pending!.Recipient&&e.Kind=="trade_offered");s=new(s.Content,first.State);var next=TurnResolver.Resolve(s.Content,s.State);Assert.True(next.Succeeded);Assert.DoesNotContain(next.Report!.DiplomacyEvents!.Value,e=>e.Kind=="trade_offered");
        s=new(s.Content,next.State);var id=s.State.Diplomacy!.Pairs[0].Pending!.Id;Assert.True(DiplomacyCommandTests.Act(s,p.Pair.Low,new DiplomacyAction.Withdraw(id)).Accepted);Assert.All(s.State.Diplomacy.Pairs[0].Parties,party=>Assert.Contains(s.View(party.Owner).Diplomacy!.Contacts.Single().Results,e=>e.Kind=="trade_withdrawn"));var ended=TurnResolver.Resolve(s.Content,s.State);Assert.True(ended.Succeeded);Assert.Equal(2,ended.Report!.DiplomacyEvents!.Value.Count(e=>e.Kind=="trade_withdrawn"));
    }
    [Fact]public void FailedActivationKeepsOwnerReasonPrivate()
    {
        var s=DiplomacyOfferResolutionTests.Offered(true);var hub=s.State.Colonies[1];hub.Workforce=new(hub.Population,0,0);var outcome=TurnResolver.Resolve(s.Content,s.State);Assert.True(outcome.Succeeded);var events=outcome.Report!.DiplomacyEvents!.Value.Where(e=>e.Kind=="trade_failed").ToArray();Assert.Equal(2,events.Length);Assert.Contains("production",events.Single(e=>e.Viewer==hub.EmpireId).Reason,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("production",events.Single(e=>e.Viewer!=hub.EmpireId).Reason,StringComparison.OrdinalIgnoreCase);Assert.All(events,e=>Assert.Equal(0,e.Paid));
    }
    [Fact]public void ConditionalPreviewAndPublishedReportUseExactDates()
    {
        var s=DiplomacyOfferResolutionTests.Offered(true);var p=s.State.Diplomacy!.Pairs[0];var preview=DiplomacyQueries.Preview(s.Content,s.View(p.Pair.High),p.Pair.Low,new DiplomacyAction.Answer(p.Pending!.Id,true,p.Pending.Answer!.Hub!.Hub));Assert.Contains("effective at turn 1",preview.Terms,StringComparison.OrdinalIgnoreCase);Assert.Contains("turn 5",preview.Terms);var turn=TurnResolver.Resolve(s.Content,s.State);Assert.True(turn.Succeeded);Assert.All(turn.Report!.DiplomacyEvents!.Value.Where(e=>e.Kind=="trade_activated"),e=>{Assert.Contains("turn 1",e.Reason);Assert.Contains("1 to 2",e.Reason);Assert.Contains("turn 5",e.Reason);Assert.DoesNotContain("next planning",e.Reason);});
    }
}

