using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictPeaceTests
{
    internal static GameSession War(int turn=0)
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));
        foreach(var e in g.State.Empires)ConflictDeclarationTests.Issue(g,e.Id,new ConflictAction.DeclareWar(g.State.Empires.Single(x=>x.Id!=e.Id).Id));
        g=ConflictDeclarationTests.Publish(g);g.State.Turn=Math.Max(turn,1);ObservationUpdater.Refresh(g.State,g.State.Turn);ConflictObservations.Refresh(g.Content,g.State);return g;
    }
    [Theory][InlineData(false,false)][InlineData(true,false)][InlineData(false,true)][InlineData(true,true)]
    public void CrossedPrivatePeaceConsentIsCanonicalAndWithdrawWins(bool reverse,bool withdraw)
    {
        var g=War(30);var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;
        foreach(var actor in reverse?new[]{b,a}:new[]{a,b})ConflictDeclarationTests.Issue(g,actor,new ConflictAction.ProposePeace(actor==a?b:a));
        Assert.Null(g.View(a).Conflict!.Relations[0].Peace);Assert.Null(g.View(b).Conflict!.Relations[0].Peace);
        if(withdraw)ConflictDeclarationTests.Issue(g,a,new ConflictAction.WithdrawPeace(b,new PeaceOfferId(0)));
        g=ConflictDeclarationTests.Publish(g);var p=g.State.Conflict!.Pairs[0];
        Assert.Equal(withdraw?RelationKind.War:RelationKind.Peace,p.Relation);Assert.Null(p.PendingPeace);
        if(!withdraw){Assert.Equal(31,p.LastPeaceTurn);Assert.Equal(41,p.TruceEndTurn);}
        Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
    }
    [Fact] public void PublishedOfferHasARealAnswerWindowAndRevocableConsent()
    {
        var g=War();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;
        ConflictDeclarationTests.Issue(g,a,new ConflictAction.ProposePeace(b));g=ConflictDeclarationTests.Publish(g);
        var offer=g.State.Conflict!.Pairs[0].PendingPeace!;Assert.NotNull(offer);Assert.Equal(g.State.Turn,offer.CreatedTurn);Assert.Equal((long)g.State.Turn+g.Content.Conflict!.OfferLifetime,offer.ExpiryTurn);
        foreach(bool accept in new[]{true,false}){ConflictDeclarationTests.Issue(g,b,new ConflictAction.AnswerPeace(a,offer.Id,accept));Assert.Null(g.View(a).Conflict!.Relations[0].Peace!.OwnAnswer);ConflictDeclarationTests.Issue(g,b,new ConflictAction.RevokePeaceAnswer(a,offer.Id));Assert.Null(g.State.Conflict.Pairs[0].PendingPeace!.Answer);}
        ConflictDeclarationTests.Issue(g,b,new ConflictAction.AnswerPeace(a,offer.Id,true));ConflictDeclarationTests.Issue(g,a,new ConflictAction.WithdrawPeace(b,offer.Id));g=ConflictDeclarationTests.Publish(g);Assert.Equal(RelationKind.War,g.State.Conflict!.Pairs[0].Relation);Assert.Null(g.State.Conflict.Pairs[0].PendingPeace);
    }
    [Fact] public void DeclarationMemoryCountsOnlyCompletePeaceIntervals()
    {
        var g=War(30);var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;
        foreach(var actor in new[]{a,b})ConflictDeclarationTests.Issue(g,actor,new ConflictAction.ProposePeace(actor==a?b:a));g=ConflictDeclarationTests.Publish(g);
        Assert.Equal(30,g.View(a).Conflict!.Relations[0].OwnGrievance);
        while(g.State.Turn<50)g=ConflictDeclarationTests.Publish(g);
        Assert.Equal(30,g.View(a).Conflict!.Relations[0].OwnGrievance);g=ConflictDeclarationTests.Publish(g);Assert.Equal(0,g.View(a).Conflict!.Relations[0].OwnGrievance);
    }
    [Fact] public void LowerProposalSurvivesRespondentWithdrawalWithoutConsent()
    {
        var g=War();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;
        foreach(var actor in new[]{b,a})ConflictDeclarationTests.Issue(g,actor,new ConflictAction.ProposePeace(actor==a?b:a));
        ConflictDeclarationTests.Issue(g,b,new ConflictAction.WithdrawPeace(a,new(0)));g=ConflictDeclarationTests.Publish(g);
        var p=g.State.Conflict!.Pairs[0];Assert.Equal(RelationKind.War,p.Relation);Assert.Equal(a,p.PendingPeace!.Proposer);Assert.Null(p.PendingPeace.Answer);
    }
    [Fact] public void LifetimeOneStartsAtPublicationAndWithdrawAtExpiryBeatsAccept()
    {
        var original=War();var g=new GameSession(original.Content with{Conflict=original.Content.Conflict! with{OfferLifetime=1}},original.State);
        var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;ConflictDeclarationTests.Issue(g,a,new ConflictAction.ProposePeace(b));g=ConflictDeclarationTests.Publish(g);var offer=g.State.Conflict!.Pairs[0].PendingPeace!;
        Assert.Equal((long)g.State.Turn+1,offer.ExpiryTurn);ConflictDeclarationTests.Issue(g,b,new ConflictAction.AnswerPeace(a,offer.Id,true));ConflictDeclarationTests.Issue(g,a,new ConflictAction.WithdrawPeace(b,offer.Id));g=ConflictDeclarationTests.Publish(g);Assert.Equal(RelationKind.War,g.State.Conflict!.Pairs[0].Relation);
    }
    [Fact] public void AcceptedPeaceSuppressesMilitaryBeforePublicationAndPreservesHull()
    {
        var g=ConflictFixtures.WithEncounter();g.State.Turn=30;ObservationUpdater.Refresh(g.State,30);ConflictObservations.Refresh(g.Content,g.State);var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var encounter=g.State.Conflict!.Encounters[0];
        int hull=g.State.FindVessel(encounter.LowSide.Members[0])!.Combat!.Hull;foreach(var actor in new[]{a,b})ConflictDeclarationTests.Issue(g,actor,new ConflictAction.ProposePeace(actor==a?b:a));
        var next=g.State.Clone();var context=ConflictResolutionContext.Capture(g.Content,g.State);ConflictRelations.Prepare(g.Content,g.State,next,context);
        Assert.Contains(encounter.Pair,context.AcceptedPeace);Assert.All(next.Conflict!.Encounters,e=>Assert.Equal(ConflictPhase.Completed,e.Phase));Assert.Empty(next.Conflict.Owners.SelectMany(o=>o.Blockades));Assert.Equal(hull,next.FindVessel(encounter.LowSide.Members[0])!.Combat!.Hull);
        next.Turn=31;ConflictRelations.Finalize(g.Content,g.State,next,context);ObservationUpdater.Refresh(next,31);ConflictObservations.Refresh(g.Content,next);Assert.Empty(GameStateValidator.Validate(g.Content,next));
    }
    [Fact] public void PartnerPrivateProposalAndAnswerCannotInvalidateOwnToken()
    {
        var g=War();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var action=new ConflictAction.ProposePeace(b);string token=ConflictTokens.For(g.Content,g.View(a),action);
        ConflictDeclarationTests.Issue(g,b,new ConflictAction.ProposePeace(a));Assert.Equal(token,ConflictTokens.For(g.Content,g.View(a),action));ConflictDeclarationTests.Issue(g,a,action);g=ConflictDeclarationTests.Publish(g);
        // New war fixture for a real published outgoing offer.
        g=War();ConflictDeclarationTests.Issue(g,a,action);g=ConflictDeclarationTests.Publish(g);var offer=g.State.Conflict!.Pairs[0].PendingPeace!;var withdraw=new ConflictAction.WithdrawPeace(b,offer.Id);token=ConflictTokens.For(g.Content,g.View(a),withdraw);
        ConflictDeclarationTests.Issue(g,b,new ConflictAction.AnswerPeace(a,offer.Id,true));Assert.Equal(token,ConflictTokens.For(g.Content,g.View(a),withdraw));
    }
    [Fact] public void TruceAndOfferDatesUseWideArithmeticNearTurnLimit()
    {
        var g=War(int.MaxValue-1);var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;
        foreach(var actor in new[]{a,b})ConflictDeclarationTests.Issue(g,actor,new ConflictAction.ProposePeace(actor==a?b:a));g=ConflictDeclarationTests.Publish(g);Assert.Equal((long)int.MaxValue+10,g.State.Conflict!.Pairs[0].TruceEndTurn);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
    }
    [Fact] public void WithdrawnUnpublishedProposalDoesNotDiscloseItToCounterpart()
    {
        var g=War();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;int events=g.State.Conflict!.Owners.Single(o=>o.Owner==b).Events.Count;
        ConflictDeclarationTests.Issue(g,a,new ConflictAction.ProposePeace(b));ConflictDeclarationTests.Issue(g,a,new ConflictAction.WithdrawPeace(b,new(0)));g=ConflictDeclarationTests.Publish(g);Assert.Equal(events,g.State.Conflict!.Owners.Single(o=>o.Owner==b).Events.Count);
    }
    [Fact] public void WithdrawalCooldownUsesEmbeddedContent()
    {
        var original=War();var g=new GameSession(original.Content with{Conflict=original.Content.Conflict! with{PeaceCooldown=7}},original.State);var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;
        ConflictDeclarationTests.Issue(g,a,new ConflictAction.ProposePeace(b));g=ConflictDeclarationTests.Publish(g);var offer=g.State.Conflict!.Pairs[0].PendingPeace!;ConflictDeclarationTests.Issue(g,a,new ConflictAction.WithdrawPeace(b,offer.Id));Assert.Equal((long)g.State.Turn+7,g.State.Conflict.Pairs[0].Parties[0].NextPeaceOfferTurn);
    }
}
