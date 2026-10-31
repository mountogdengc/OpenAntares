using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictTreatyTests
{
    [Fact] public void PendingSetupIsReleasedBeforeEconomyButCurrentAccordSurvivesUntilPublication()
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));var p=g.State.Diplomacy!.Pairs[0];var a=p.Pair.Low;var b=p.Pair.High;
        var low=g.State.Colonies.First(c=>c.EmpireId==a);var high=g.State.Colonies.First(c=>c.EmpireId==b);low.ProductionReserve=high.ProductionReserve=500;
        var star=g.State.FindPlanet(low.PlanetId)!.StarId;
        p.Agreements.Add(new(){Kind=AgreementKind.Settlement,Star=star,ActivatedTurn=0});
        p.Pending=new(){Id=new(1),Kind=AgreementKind.Trade,Proposer=a,Recipient=b,CreatedTurn=0,ExpiryTurn=g.Content.Diplomacy!.OfferLifetime,ProposerHub=new(low.Id,g.Content.Diplomacy.SetupCost),Answer=new(){Accept=true,Hub=new(high.Id,g.Content.Diplomacy.SetupCost)}};p.NextOfferId=2;
        Assert.Empty(GameStateValidator.Validate(g.Content,g.State));Assert.True(DiplomacyReservations.Reserved(g.State,low.Id)>0);
        ConflictDeclarationTests.Issue(g,a,new ConflictAction.DeclareWar(b));var snapshot=g.State;var next=snapshot.Clone();var frame=DiplomacyFrame.Capture(g.Content,snapshot);var context=ConflictResolutionContext.Capture(g.Content,snapshot);
        ConflictRelations.Prepare(g.Content,snapshot,next,context);Assert.Null(next.Diplomacy!.Pairs[0].Pending);Assert.Equal(0,DiplomacyReservations.Reserved(next,low.Id));Assert.Equal(500,next.FindColony(low.Id)!.ProductionReserve);Assert.True(DiplomacyRules.BlocksFounding(frame,a,star));Assert.Single(next.Diplomacy.Pairs[0].Agreements);
        Assert.Empty(DiplomacyResolver.ResolveOffers(g.Content,snapshot,next));next.Turn=1;DiplomacyResolver.Finalize(g.Content,snapshot,next,frame);ConflictRelations.Finalize(g.Content,snapshot,next,context);
        ObservationUpdater.Refresh(next,1);ConflictObservations.Refresh(g.Content,next);
        Assert.Empty(next.Diplomacy.Pairs[0].Agreements);Assert.Equal(500,next.FindColony(low.Id)!.ProductionReserve);Assert.Equal(500,next.FindColony(high.Id)!.ProductionReserve);Assert.All(next.Diplomacy.Pairs[0].LastResults,e=>Assert.Equal(0,e.Paid));Assert.Empty(GameStateValidator.Validate(g.Content,next));
    }
    [Fact] public void BreachAtTwentyPreservesOldCancellationMemoryAndNeverAddsDuplicatePenalty()
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));g.State.Turn=20;ObservationUpdater.Refresh(g.State,20);var p=g.State.Diplomacy!.Pairs[0];var a=p.Pair.Low;var b=p.Pair.High;var low=g.State.Colonies.First(c=>c.EmpireId==a);var high=g.State.Colonies.First(c=>c.EmpireId==b);
        p.Agreements.Add(new(){Kind=AgreementKind.Trade,ActivatedTurn=0,LowHub=new(low.Id,a,0),HighHub=new(high.Id,b,0),LowNoticeTurn=20,EndTurn=20+g.Content.Diplomacy!.Notice});
        foreach(var party in p.Parties)party.Attitude.Streak=10;p.Parties[1].Attitude.LastCounterpartNoticeTurn=19;
        Assert.Empty(GameStateValidator.Validate(g.Content,g.State));ConflictDeclarationTests.Issue(g,a,new ConflictAction.DeclareWar(b));var before=g.State;var next=before.Clone();var frame=DiplomacyFrame.Capture(g.Content,before);var context=ConflictResolutionContext.Capture(g.Content,before);
        ConflictRelations.Prepare(g.Content,before,next,context);Assert.Single(frame.Agreements);Assert.Null(next.Diplomacy!.Pairs[0].Agreements[0].LowNoticeTurn);next.Turn=21;DiplomacyResolver.Finalize(g.Content,before,next,frame);ConflictRelations.Finalize(g.Content,before,next,context);ObservationUpdater.Refresh(next,21);ConflictObservations.Refresh(g.Content,next);g=new(g.Content,next);
        Assert.Equal(19,next.Diplomacy.Pairs[0].Parties[1].Attitude.LastCounterpartNoticeTurn);Assert.All(next.Diplomacy.Pairs[0].Parties,x=>Assert.Equal(0,x.Attitude.Streak));Assert.Equal(21,next.Conflict!.Pairs[0].WarTurn);
        var own=g.View(b).Diplomacy!.Contacts[0].OwnAttitude;Assert.Equal(-40,own.Score);Assert.Equal(30,own.DeclarationGrievance);Assert.Equal("War",own.Label);Assert.Equal(0,g.View(a).Diplomacy!.Contacts[0].OwnAttitude.Score);Assert.Empty(GameStateValidator.Validate(g.Content,next));
    }
    [Fact] public void OrdinaryDiplomacyIsUnavailableDuringWarWithoutMutatingAnything()
    {
        var g=ConflictPeaceTests.War();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var hub=g.State.Colonies.First(c=>c.EmpireId==a).Id;
        var preview=DiplomacyQueries.Preview(g.Content,g.View(a),b,new DiplomacyAction.OfferTrade(hub));Assert.False(preview.Allowed);var c=g.View(a).Diplomacy!.Contacts[0];string before=ConflictFixtures.Canonical(g.State);
        var result=g.Execute(new DiplomacyCommand(a,g.State.Turn,b,c.PublicRevision,c.PrivateRevision,new DiplomacyAction.OfferTrade(hub)));Assert.False(result.Accepted);Assert.Equal(before,ConflictFixtures.Canonical(g.State));
    }
    [Fact] public void IndependentThirdPartyAgreementSurvivesWarAndHasNoGrievance()
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var star=g.State.FindPlanet(g.State.Colonies[0].PlanetId)!.StarId;var c=ConflictFixtures.AddNeutralObserver(g,star);
        var key=DiplomacyPair.For(a,c);g.State.Diplomacy!.Pairs.Single(p=>p.Pair==key).Agreements.Add(new(){Kind=AgreementKind.Settlement,Star=star,ActivatedTurn=0});Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        ConflictDeclarationTests.Issue(g,a,new ConflictAction.DeclareWar(b));g=ConflictDeclarationTests.Publish(g);Assert.Single(g.State.Diplomacy!.Pairs.Single(p=>p.Pair==key).Agreements);Assert.All(g.State.Conflict!.Pairs.Single(p=>p.Pair==key).Parties,p=>Assert.False(p.DeclarationGrievance));Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
    }
}
