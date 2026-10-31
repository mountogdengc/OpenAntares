using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacyTurnTests
{
    [Fact] public void FinalReadyDiplomacyFailureRestoresOriginalState()
    {
        foreach(var phase in Enum.GetValues<DiplomacyResolutionPhase>())
        {
            var s=DiplomacyOfferResolutionTests.Offered(true);var a=s.State.Empires[0].Id;var b=s.State.Empires[1].Id;
            Assert.True(s.Execute(new SetReadyCommand(a,0,true)).Accepted);var original=s.State;string before=JsonSerializer.Serialize(original);bool reached=false;
            var failed=CommandProcessor.ExecuteDiplomacy(s.Content,original,new(b,0,true),p=>{if(p==phase){reached=true;throw new InvalidOperationException("Injected diplomacy phase failure.");}});
            Assert.True(reached);Assert.False(failed.Accepted);Assert.Same(original,failed.State);Assert.Equal(before,JsonSerializer.Serialize(failed.State));
        }
    }
    [Fact] public void ActualReadyChargesSetupAndPublishesNextTurnAgreement()
    {
        var s=DiplomacyOfferResolutionTests.Offered(true);Assert.True(s.Execute(new SetReadyCommand(s.State.Empires[0].Id,0,true)).Accepted);
        var result=s.Execute(new SetReadyCommand(s.State.Empires[1].Id,0,true));Assert.True(result.Accepted,result.Rejection?.Message);
        Assert.Equal(1,s.State.Turn);Assert.Null(s.State.Diplomacy!.Pairs.Single().Pending);Assert.Equal(1,s.State.Diplomacy.Pairs.Single().Agreements.Single().ActivatedTurn);
        foreach(var e in result.Report!.Economy.Colonies){Assert.Equal(500,e.Production.Starting);Assert.Equal(e.NetProduction.Total,e.Production.Ending);Assert.Equal(e.Production.Starting+e.Production.Change.Total,e.Production.Ending);}
    }
    [Fact] public void ReciprocalIntroductionHasNoInventedLocationAndDoesNotReplay()
    {
        // Labelled ContactUpdater output-boundary supplement for the asymmetric radio case.
        var s=DiplomacyFixtures.Create(humans:2);var c=s.State.Clone();c.Turn=1;ObservationUpdater.Refresh(c,1);var a=c.Empires[0];var b=c.Empires[1];a.Contacts.Add(new(b.Id,b.SpeciesId!,1));
        var star=c.FindPlanet(c.Colonies[1].PlanetId)!.StarId;
        var observed=ImmutableArray.Create(new EncounterEvent("first_contact",a.Id,b.Id,star,null,1));
        var events=DiplomacyContactUpdater.Refresh(s.State,c,observed);Assert.Equal(2,events.Length);Assert.Null(events.Single(e=>e.Viewer==b.Id).Star);Assert.Equal(star,events.Single(e=>e.Viewer==a.Id).Star);
        Assert.Single(b.Contacts);Assert.Single(c.Diplomacy!.Pairs);Assert.Empty(DiplomacyContactUpdater.Refresh(c,c.Clone(),ImmutableArray<EncounterEvent>.Empty));
    }
    [Fact] public void NormalReadyRunsRivalResponseAndPublishesOnlyOwnReportEvents()
    {
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create());var human=s.State.Empires[0].Id;
        foreach(var party in s.State.Diplomacy!.Pairs[0].Parties)party.Attitude.Streak=5;
        foreach(var c in s.State.Colonies)c.ProductionReserve=500;
        Assert.True(DiplomacyCommandTests.Act(s,human,new DiplomacyAction.OfferTrade(s.State.Colonies.Single(c=>c.EmpireId==human).Id)).Accepted);
        var result=s.Execute(new SetReadyCommand(human,0,true));Assert.True(result.Accepted,result.Rejection?.Message);Assert.Equal(1,s.State.Turn);
        Assert.Single(s.State.Diplomacy!.Pairs[0].Agreements);Assert.Null(s.State.Diplomacy.Pairs[0].Pending);
        var own=EmpireViewProjector.ProjectReport(s.Content,s.State,result.Report!,human);Assert.All(own.DiplomacyEvents!.Value,e=>Assert.Equal(human,e.Viewer));Assert.Single(own.DiplomacyEvents.Value.Where(e=>e.Kind=="trade_activated"));
    }
    [Fact] public void NonfinalAndRepeatedReadyDoNotResolveOffers()
    {
        var s=DiplomacyOfferResolutionTests.Offered(true);var owner=s.State.Empires[0].Id;var before=s.State.Diplomacy!.Pairs[0].NextOfferId;
        Assert.True(s.Execute(new SetReadyCommand(owner,0,true)).Accepted);Assert.Equal(0,s.State.Turn);Assert.NotNull(s.State.Diplomacy!.Pairs[0].Pending);Assert.Empty(s.State.Diplomacy.Pairs[0].Agreements);
        var noop=s.Execute(new SetReadyCommand(owner,0,true));Assert.True(noop.Accepted);Assert.False(noop.Changed);Assert.Equal(before,s.State.Diplomacy.Pairs[0].NextOfferId);Assert.All(s.State.Colonies,c=>Assert.Equal(500,c.ProductionReserve));
    }}
