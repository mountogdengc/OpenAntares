using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacyReservationTests
{
    private static (GameSession Session,ColonyId Hub) ReservedSession()
    {
        var s=DiplomacyCommandTests.Funded();var a=s.State.Empires[0].Id;var hub=s.State.Colonies.Single(c=>c.EmpireId==a).Id;
        Assert.True(DiplomacyCommandTests.Act(s,a,new DiplomacyAction.OfferTrade(hub)).Accepted);return(s,hub);
    }
    [Fact] public void ExactPriceEarmarkActivates()
    {
        var (s,hub)=ReservedSession();var o=s.State.Diplomacy!.Pairs.Single().Pending!;
        Assert.Equal(500,DiplomacyReservations.Reserved(s.State,hub));Assert.Equal(0,DiplomacyReservations.Spendable(s.State,hub));
        Assert.True(DiplomacyHubQueries.Eligibility(s.Content,s.State,o.Proposer,hub,o.Id).Allowed);
        Assert.False(DiplomacyHubQueries.Eligibility(s.Content,s.State,o.Proposer,hub,null).Allowed);
    }
    [Fact] public void OrdinaryCompletionCannotSpendReservedSetup()
    {
        var (s,hub)=ReservedSession();var c=s.State.FindColony(hub)!;
        c.ProjectId="fabrication_hall";c.ProjectKind=ProductionKinds.Building;
        long output=s.Forecast().Colony(hub).NetProduction.Total;c.ProductionReserve=1200-output;
        Assert.True(c.ProductionReserve>=500);
        var forecast=s.Forecast().Colony(hub);Assert.Null(forecast.Production.CompletedId);Assert.Equal(1200,forecast.Production.Ending);
        var before=s.State.Diplomacy!.Pairs.Single().Pending!;Assert.True(DiplomacyCommandTests.Act(s,before.Proposer,new DiplomacyAction.Withdraw(before.Id)).Accepted);
        Assert.Equal("fabrication_hall",s.Forecast().Colony(hub).Production.CompletedId);
    }
    [Fact] public void ReservationDoesNotConsumeOrMultiplyIncome()
    {
        var (s,hub)=ReservedSession();var e=s.Forecast().Colony(hub);Assert.Equal(500+e.NetProduction.Total,e.Production.Ending);
        Assert.Equal(e.Production.Starting+e.Production.Change.Total,e.Production.Ending);
    }

    [Fact] public void LostPendingHubReleasesBothWithoutBlockingOtherCommands()
    {
        var (s,hub)=ReservedSession();s.State.FindColony(hub)!.ProductionReserve=250;
        DiplomacyReconciliation.AfterCommand(s.Content,s.State);
        Assert.Null(s.State.Diplomacy!.Pairs.Single().Pending);Assert.Equal(0,DiplomacyReservations.Reserved(s.State,hub));
        Assert.Empty(DiplomacyStateValidator.Validate(s.Content,s.State));
    }
    [Theory] [InlineData("vessel","scout")][InlineData("ship_design","1")]
    public void VesselAndDesignCompletionCannotSpendSetup(string kind,string target)
    {
        var (s,hub)=ReservedSession();var c=s.State.FindColony(hub)!;c.ProjectId=target;c.ProjectKind=kind;
        long cost=kind=="vessel"?s.Content.Vessels[target].Cost:OpenAntares.Simulation.ShipDesigns.ShipDesignMath.Calculate(s.Content.ShipDesigns!,s.Content.ShipDesigns!.StartingRecipe).BuildCost;
        long output=s.Forecast().Colony(hub).NetProduction.Total;c.ProductionReserve=cost-output;
        var f=s.Forecast().Colony(hub);Assert.Null(f.Production.CompletedId);Assert.Equal(cost,f.Production.Ending);
        var o=s.State.Diplomacy!.Pairs.Single().Pending!;Assert.True(DiplomacyCommandTests.Act(s,o.Proposer,new DiplomacyAction.Withdraw(o.Id)).Accepted);
        Assert.Equal(target,s.Forecast().Colony(hub).Production.CompletedId);
    }
}
