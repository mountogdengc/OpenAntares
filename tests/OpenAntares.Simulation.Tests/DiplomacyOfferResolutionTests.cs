using System.Linq;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Knowledge;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacyOfferResolutionTests
{
    internal static GameSession Offered(bool answer,int turn=0)
    {
        var s=DiplomacyCommandTests.Funded();var a=s.State.Empires[0].Id;var b=s.State.Empires[1].Id;
        Assert.True(DiplomacyCommandTests.Act(s,a,new DiplomacyAction.OfferTrade(s.State.Colonies.Single(c=>c.EmpireId==a).Id)).Accepted);
        s.State.Turn=turn;ObservationUpdater.Refresh(s.State,turn);
        if(answer)Assert.True(DiplomacyCommandTests.Act(s,b,new DiplomacyAction.Answer(s.State.Diplomacy!.Pairs.Single().Pending!.Id,true,s.State.Colonies.Single(c=>c.EmpireId==b).Id)).Accepted);
        return s;
    }
    [Fact] public void ThirdBoundaryAnswerWinsExpiryAndWithdrawalWinsAnswer()
    {
        var s=Offered(true,2);var candidate=s.State.Clone();var events=DiplomacyResolver.ResolveOffers(s.Content,s.State,candidate);
        var p=candidate.Diplomacy!.Pairs.Single();Assert.Null(p.Pending);Assert.Single(p.Agreements);Assert.Equal(3,p.Agreements[0].ActivatedTurn);
        Assert.Empty(DiplomacyFrame.Capture(s.Content,candidate).Agreements);Assert.All(candidate.Colonies,c=>Assert.Equal(0,c.ProductionReserve));Assert.Equal(2,events.Length);
        var w=Offered(true);var pair=w.State.Diplomacy!.Pairs.Single();Assert.True(DiplomacyCommandTests.Act(w,pair.Pending!.Proposer,new DiplomacyAction.Withdraw(pair.Pending.Id)).Accepted);
        candidate=w.State.Clone();Assert.Empty(DiplomacyResolver.ResolveOffers(w.Content,w.State,candidate));Assert.Empty(candidate.Diplomacy!.Pairs.Single().Agreements);Assert.All(candidate.Colonies,c=>Assert.Equal(500,c.ProductionReserve));
    }
    [Fact] public void ExpiryAndFailedActivationReleaseBeforeOrdinaryCompletion()
    {
        var s=Offered(false,2);var candidate=s.State.Clone();DiplomacyResolver.ResolveOffers(s.Content,s.State,candidate);Assert.Null(candidate.Diplomacy!.Pairs.Single().Pending);Assert.All(candidate.Colonies,c=>Assert.Equal(500,c.ProductionReserve));
        s=Offered(true);var high=s.State.Empires[1].Id;var hub=s.State.Colonies.Single(c=>c.EmpireId==high);hub.Workforce=new(hub.Population,0,0);
        candidate=s.State.Clone();DiplomacyResolver.ResolveOffers(s.Content,s.State,candidate);Assert.Null(candidate.Diplomacy!.Pairs.Single().Pending);Assert.Empty(candidate.Diplomacy.Pairs.Single().Agreements);Assert.All(candidate.Colonies,c=>Assert.Equal(500,c.ProductionReserve));
    }
    [Fact] public void ExpiryReleaseCanCompleteConstructionOnSameBoundary()
    {
        var s=Offered(false,2);var hub=s.State.Colonies[0];hub.ProjectId="fabrication_hall";hub.ProjectKind=OpenAntares.Simulation.State.ProductionKinds.Building;
        long output=s.Forecast().Colony(hub.Id).NetProduction.Total;hub.ProductionReserve=1200-output;
        Assert.Null(s.Forecast().Colony(hub.Id).Production.CompletedId);
        var candidate=s.State.Clone();DiplomacyResolver.ResolveOffers(s.Content,s.State,candidate);
        Assert.Equal("fabrication_hall",OpenAntares.Simulation.Economy.EconomyCalculator.Calculate(s.Content,candidate).Colony(hub.Id).Production.CompletedId);
        Assert.Equal(8,candidate.Diplomacy!.Pairs.Single().Parties[0].NextAiOfferTurn);
    }}
