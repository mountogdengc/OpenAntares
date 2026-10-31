using System.Linq;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacyTradeTests
{
    internal static GameSession Activated()
    {
        var s=DiplomacyOfferResolutionTests.Offered(true);var candidate=s.State.Clone();DiplomacyResolver.ResolveOffers(s.Content,s.State,candidate);candidate.Turn=1;ObservationUpdater.Refresh(candidate,1);return new(s.Content,candidate);
    }
    [Fact] public void SetupPaymentCannotDoubleCharge()
    {
        var s=DiplomacyOfferResolutionTests.Offered(true);var candidate=s.State.Clone();var events=DiplomacyResolver.ResolveOffers(s.Content,s.State,candidate);
        Assert.All(events,e=>Assert.Equal(500,e.Paid));Assert.All(candidate.Colonies,c=>Assert.Equal(0,c.ProductionReserve));
        Assert.Empty(DiplomacyResolver.ResolveOffers(s.Content,s.State,candidate));Assert.All(candidate.Colonies,c=>Assert.Equal(0,c.ProductionReserve));
        var economy=EconomyCalculator.Calculate(s.Content,candidate);var adjusted=DiplomacyEconomy.IncludeSetupPayments(economy,events);
        foreach(var c in adjusted.Colonies){Assert.Equal(500,c.Production.Starting);Assert.Equal(c.Production.Starting+c.Production.Change.Total,c.Production.Ending);}
    }
    [Fact] public void FirstOutputIsFourthEffectiveBoundaryAndKeepsGrossUnchanged()
    {
        var s=Activated();var hub=s.State.Colonies[0].Id;
        foreach(int turn in new[]{1,2,3}){s.State.Turn=turn;Assert.Equal(0,DiplomacyEconomy.TradeOutput(s.Content,DiplomacyFrame.Capture(s.Content,s.State),hub));}
        s.State.Turn=4;Assert.Equal(100,DiplomacyEconomy.TradeOutput(s.Content,DiplomacyFrame.Capture(s.Content,s.State),hub));
        var trade=s.Forecast().Colony(hub);var ordinary=s.State.Clone();ordinary.Diplomacy!.Pairs[0].Agreements.Clear();var plain=EconomyCalculator.Calculate(s.Content,ordinary).Colony(hub);
        Assert.Equal(plain.GrossProduction.Total,trade.GrossProduction.Total);Assert.Equal(plain.GrossProduction.Lines.ToArray(),trade.GrossProduction.Lines.ToArray());Assert.Equal(plain.NetProduction.Total+100,trade.NetProduction.Total);Assert.Equal(plain.NetResearch.Total,trade.NetResearch.Total);
    }
    [Fact] public void LaterAllSupportWorkersKeepTradeWithoutOpeningNewOrdinaryHub()
    {
        var s=Activated();s.State.Turn=4;var hub=s.State.Colonies[0];hub.Workforce=new(hub.Population,0,0);
        Assert.Equal(100,s.Forecast().Colony(hub.Id).NetProduction.Total);
        Assert.False(DiplomacyHubQueries.Eligibility(s.Content,s.State,hub.EmpireId,hub.Id,null).Allowed);
    }
}
