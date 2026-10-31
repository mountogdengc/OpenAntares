using System.Linq;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Knowledge;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacyDurationTests
{
    [Fact] public void MinimumAndTwoBoundaryNoticeHaveExactDates()
    {
        var s=DiplomacyTradeTests.Activated();s.State.Turn=11;ObservationUpdater.Refresh(s.State,11);
        var pair=s.State.Diplomacy!.Pairs.Single();var low=pair.Pair.Low;var high=pair.Pair.High;
        Assert.True(DiplomacyCommandTests.Act(s,low,new DiplomacyAction.Terminate(AgreementKind.Trade)).Accepted);
        var c=s.State.Clone();var frame=DiplomacyResolver.PrepareEffective(s.Content,s.State,c,DiplomacyFrame.Capture(s.Content,s.State));Assert.Single(frame.Agreements);
        c.Turn=12;DiplomacyResolver.Finalize(s.Content,s.State,c,frame);Assert.Single(c.Diplomacy!.Pairs.Single().Agreements);Assert.Equal(12,c.Diplomacy.Pairs.Single().Parties.Single(p=>p.Owner==high).Attitude.LastCounterpartNoticeTurn);
        var next=c.Clone();frame=DiplomacyResolver.PrepareEffective(s.Content,c,next,DiplomacyFrame.Capture(s.Content,c));Assert.Single(frame.Agreements);next.Turn=13;DiplomacyResolver.Finalize(s.Content,c,next,frame);Assert.Empty(next.Diplomacy!.Pairs.Single().Agreements);
    }
    [Fact] public void DirectedMemoryExpiresWithoutStackingAndLabelsMatch()
    {
        var d=DiplomacyFixtures.Create().Content.Diplomacy!;
        Assert.Equal(20,DiplomacyAttitudes.Calculate(d,10,null,22));Assert.Equal(10,DiplomacyAttitudes.Calculate(d,10,12,21));Assert.Equal(20,DiplomacyAttitudes.Calculate(d,10,12,22));
        Assert.Equal(-10,DiplomacyAttitudes.Calculate(d,0,12,12));Assert.Equal("Strained",DiplomacyAttitudes.Label(-1));Assert.Equal("Neutral",DiplomacyAttitudes.Label(9));Assert.Equal("Receptive",DiplomacyAttitudes.Label(19));Assert.Equal("Cooperative",DiplomacyAttitudes.Label(20));
    }
    [Fact] public void LiveHubLossStopsBothSidesBeforeIncomeWithoutPenalty()
    {
        var s=DiplomacyTradeTests.Activated();s.State.Turn=4;var hub=s.State.Colonies[0].Id;s.State.Colonies.RemoveAll(c=>c.Id==hub);
        var c=s.State.Clone();var frame=DiplomacyResolver.PrepareEffective(s.Content,s.State,c,DiplomacyFrame.Capture(s.Content,s.State));Assert.Empty(frame.Agreements);Assert.Empty(c.Diplomacy!.Pairs.Single().Agreements);
        Assert.All(c.Diplomacy.Pairs.Single().Parties,p=>Assert.Null(p.Attitude.LastCounterpartNoticeTurn));Assert.All(c.Diplomacy.Pairs.Single().LastResults,e=>Assert.Equal(0,e.Paid));
    }
    [Fact] public void OnlyCompletedEffectiveBoundariesBuildOneCooperationStreak()
    {
        var s=DiplomacyTradeTests.Activated();var p=s.State.Diplomacy!.Pairs.Single();p.Agreements.Add(new(){Kind=AgreementKind.Settlement,Star=s.State.Stars[0].Id,ActivatedTurn=1});
        var c=s.State.Clone();c.Turn=2;DiplomacyResolver.Finalize(s.Content,s.State,c,DiplomacyFrame.Capture(s.Content,s.State));Assert.All(c.Diplomacy!.Pairs.Single().Parties,p=>Assert.Equal(1,p.Attitude.Streak));
        var empty=c.Clone();empty.Diplomacy!.Pairs.Single().Agreements.Clear();var next=empty.Clone();next.Turn=3;DiplomacyResolver.Finalize(s.Content,empty,next,DiplomacyFrame.Capture(s.Content,empty));Assert.All(next.Diplomacy!.Pairs.Single().Parties,p=>Assert.Equal(0,p.Attitude.Streak));
    }
}
