using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacyCommandTests
{
    internal static CommandOutcome Act(GameSession s,EmpireId issuer,DiplomacyAction action)
    {
        var p=s.State.Diplomacy!.Pairs.Single();return s.Execute(new DiplomacyCommand(issuer,s.State.Turn,p.Pair.Other(issuer),p.PublicRevision,DiplomacyTokens.PrivateRevision(s.Content,s.State,issuer,p.Pair.Other(issuer)),action));
    }
    internal static GameSession Funded()
    {
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create(humans:2));
        foreach(var c in s.State.Colonies)c.ProductionReserve=500;return s;
    }
    [Fact] public void RevokeAtExpiryDoesNotExtendOffer()
    {
        var s=Funded();var a=s.State.Empires[0].Id;var b=s.State.Empires[1].Id;
        Assert.True(Act(s,a,new DiplomacyAction.OfferTrade(s.State.Colonies.Single(c=>c.EmpireId==a).Id)).Accepted);
        var original=s.State.Diplomacy!.Pairs.Single().Pending!;int expiry=original.ExpiryTurn;
        s.State.Turn=2;OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(s.State,2);Assert.Empty(GameStateValidator.Validate(s.Content,s.State));var id=original.Id;
        Assert.True(Act(s,b,new DiplomacyAction.Answer(id,true,s.State.Colonies.Single(c=>c.EmpireId==b).Id)).Accepted);
        Assert.True(Act(s,b,new DiplomacyAction.RevokeAnswer(id)).Accepted);
        var o=s.State.Diplomacy!.Pairs.Single().Pending!;Assert.Null(o.Answer);Assert.Equal(expiry,o.ExpiryTurn);Assert.Equal(500,o.ProposerHub!.Reserved);
        Assert.True(Act(s,b,new DiplomacyAction.Answer(id,false,null)).Accepted);Assert.True(Act(s,b,new DiplomacyAction.RevokeAnswer(id)).Accepted);
    }
    [Fact] public void WithdrawalBeatsQueuedAnswerAndFreesBothHubs()
    {
        var s=Funded();var a=s.State.Empires[0].Id;var b=s.State.Empires[1].Id;
        Assert.True(Act(s,a,new DiplomacyAction.OfferTrade(s.State.Colonies.Single(c=>c.EmpireId==a).Id)).Accepted);
        var id=s.State.Diplomacy!.Pairs.Single().Pending!.Id;
        Assert.True(Act(s,b,new DiplomacyAction.Answer(id,true,s.State.Colonies.Single(c=>c.EmpireId==b).Id)).Accepted);
        Assert.True(Act(s,a,new DiplomacyAction.Withdraw(id)).Accepted);Assert.Null(s.State.Diplomacy!.Pairs.Single().Pending);
        Assert.False(Act(s,b,new DiplomacyAction.Answer(id,true,s.State.Colonies.Single(c=>c.EmpireId==b).Id)).Accepted);
        Assert.All(s.State.Colonies,c=>Assert.Equal(500,c.ProductionReserve));
    }
    [Fact] public void NoOpPreservesReadinessAndCountersAndStaleRejects()
    {
        var s=Funded();var a=s.State.Empires[0].Id;var p=s.State.Diplomacy!.Pairs.Single();var hub=s.State.Colonies.Single(c=>c.EmpireId==a).Id;
        var old=new DiplomacyCommand(a,0,p.Pair.Other(a),p.PublicRevision,DiplomacyTokens.PrivateRevision(s.Content,s.State,a,p.Pair.Other(a)),new DiplomacyAction.OfferTrade(hub));
        Assert.True(s.Execute(old).Accepted);Assert.False(s.Execute(old).Accepted);
        s.State.Empires[0].Ready=true;long next=s.State.Diplomacy!.Pairs.Single().NextOfferId;
        var noop=Act(s,a,new DiplomacyAction.OfferTrade(hub));Assert.True(noop.Accepted);Assert.False(noop.Changed);Assert.True(s.State.Empires[0].Ready);Assert.Equal(next,s.State.Diplomacy!.Pairs.Single().NextOfferId);
        Assert.False(Act(s,p.Pair.Other(a),new DiplomacyAction.OfferTrade(s.State.Colonies.Single(c=>c.EmpireId!=a).Id)).Accepted);
    }
    [Fact] public void UncontactedForgedAndForeignHubRejectWithoutMutation()
    {
        var s=DiplomacyFixtures.Create();var before=s.State;
        var bad=new DiplomacyCommand(s.State.Empires[0].Id,0,s.State.Empires[1].Id,1,1,new DiplomacyAction.OfferTrade(s.State.Colonies[1].Id));
        Assert.False(s.Execute(bad).Accepted);Assert.Same(before,s.State);
        s=Funded();before=s.State;Assert.False(Act(s,s.State.Empires[0].Id,new DiplomacyAction.OfferTrade(s.State.Colonies[1].Id)).Accepted);Assert.Same(before,s.State);
    }
    [Fact] public void TerminationQueuesOnlyAfterMinimumWithoutChangingAttitudeYet()
    {
        var s=Funded();s.State.Turn=10;OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(s.State,10);
        var p=s.State.Diplomacy!.Pairs.Single();var a=p.Pair.Low;
        p.Agreements.Add(new(){Kind=AgreementKind.Settlement,ActivatedTurn=1,Star=s.State.Stars[0].Id});
        Assert.False(Act(s,a,new DiplomacyAction.Terminate(AgreementKind.Settlement)).Accepted);
        s.State.Turn=11;OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(s.State,11);
        Assert.True(Act(s,a,new DiplomacyAction.Terminate(AgreementKind.Settlement)).Accepted);
        p=s.State.Diplomacy!.Pairs.Single();Assert.Equal(13,p.Agreements[0].EndTurn);Assert.Null(p.Parties[1].Attitude.LastCounterpartNoticeTurn);
    }}
