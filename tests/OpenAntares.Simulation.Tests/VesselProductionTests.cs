using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;

public class VesselProductionTests
{
    private static GameSession Selected(GameState s, ContentSet? content = null)
    {
        var session = new GameSession(content ?? ExpansionFixtures.Content(), s); var o = session.Execute(new SetProductionTargetCommand(Fixtures.Empire, s.Turn, Fixtures.HomeColony, new("vessel", "settler"))); Assert.True(o.Accepted, o.Rejection?.Message); return session;
    }
    [Fact]
    public void SettlerCompletionTransfersExistingSupportWorkers()
    {
        var s = ExpansionFixtures.AtHome(); s.Colonies[0].ProductionReserve = 2000; var session = Selected(s); var before = Fixtures.Snapshot(session.State); var f = session.Forecast().Colonies[0];
        Assert.Equal(before, Fixtures.Snapshot(session.State)); Assert.Equal(0, f.Production.Ending); Assert.Equal(4, f.EmbarkOutcome!.PopulationAfter); Assert.Equal(new Workforce(0, 2, 2), f.EmbarkOutcome.WorkforceAfter); Assert.Equal(0, f.EmbarkOutcome.NextSupportProduced.Total); Assert.Equal(400, f.EmbarkOutcome.NextSupportRequired.Total); Assert.True(f.EmbarkOutcome.ReassignmentNeeded);
        var o = session.Execute(new SetReadyCommand(Fixtures.Empire, 0, true)); Assert.True(o.Accepted, o.Rejection?.Message); var colony = session.State.Colonies[0]; Assert.Equal(4, colony.Population); Assert.Equal(new Workforce(0, 2, 2), colony.Workforce); Assert.Equal(10, colony.GrowthProgress); Assert.Empty(colony.CompletedBuildingIds); Assert.Null(colony.ProjectId); Assert.Equal("building", colony.ProjectKind);
        var settler = Assert.Single(session.State.Vessels, v => v.RoleId == "settler"); Assert.Equal(2, settler.CarriedPopulation); Assert.Equal(Fixtures.HomeStar, settler.StationaryStarId); Assert.Null(settler.Transit); Assert.Equal(6, session.State.Colonies.Sum(c => c.Population) + session.State.Vessels.Sum(v => v.CarriedPopulation));
        Assert.True(session.Execute(new SetProductionTargetCommand(Fixtures.Empire, 1, Fixtures.HomeColony, new("vessel", "settler"))).Accepted);
    }
    [Fact]
    public void BirthCannotUnlockEmbarkThisTurn()
    {
        var s = ExpansionFixtures.AtHome(); var c = s.Colonies[0]; c.Population = 3; c.Workforce = new(2, 1, 0); c.GrowthProgress = 90; c.ProductionReserve = 2400; var session = Selected(s);
        Assert.Equal("awaiting_embarkation", session.Forecast().Colonies[0].Production.BlockedReasonCode);
        Assert.True(session.Execute(new SetReadyCommand(Fixtures.Empire, 0, true)).Accepted); Assert.Single(session.State.Vessels); Assert.Equal(4, session.State.Colonies[0].Population); Assert.Equal(2600, session.State.Colonies[0].ProductionReserve); Assert.Equal("settler", session.State.Colonies[0].ProjectId);
        Assert.True(session.Execute(new SetReadyCommand(Fixtures.Empire, 1, true)).Accepted); Assert.Equal(2, session.State.Vessels.Count); Assert.Equal(400, session.State.Colonies[0].ProductionReserve);
    }
    [Fact]
    public void SupportShortageStallsAndReassignmentCompletesWithoutLostReserve()
    {
        var s = ExpansionFixtures.AtHome(); s.Colonies[0].Population = 4; s.Colonies[0].Workforce = new(1, 3, 0); s.Colonies[0].ProductionReserve = 2400; var session = Selected(s);
        Assert.True(session.Execute(new SetReadyCommand(Fixtures.Empire, 0, true)).Accepted); Assert.Equal(2850, session.State.Colonies[0].ProductionReserve); Assert.Single(session.State.Vessels);
        Assert.True(session.Execute(new SetWorkforceCommand(Fixtures.Empire, 1, Fixtures.HomeColony, new(2, 2, 0))).Accepted);
        Assert.True(session.Execute(new SetReadyCommand(Fixtures.Empire, 1, true)).Accepted); Assert.Equal(850, session.State.Colonies[0].ProductionReserve); Assert.Equal(2, session.State.Colonies[0].Population);
    }
    [Fact]
    public void SwitchingAndClearingConserveReserveAndKindsDoNotCollide()
    {
        var content = ExpansionFixtures.Content(); content = content with { Buildings = content.Buildings.Add("scout", content.Buildings["fabrication_hall"] with { Id = "scout" }) }; var s = ExpansionFixtures.AtHome(); s.Colonies[0].CompletedBuildingIds.Add("scout"); s.Colonies[0].ProductionReserve = 700; var session = new GameSession(content, s);
        Assert.True(session.Execute(new SetProductionTargetCommand(Fixtures.Empire, 0, Fixtures.HomeColony, new("vessel", "scout"))).Accepted);
        Assert.Equal("vessel", session.State.Colonies[0].ProjectKind); Assert.Equal(700, session.State.Colonies[0].ProductionReserve);
        Assert.False(session.Execute(new SetProjectCommand(Fixtures.Empire, 0, Fixtures.HomeColony, "scout")).Accepted);
        Assert.True(session.Execute(new SetProductionTargetCommand(Fixtures.Empire, 0, Fixtures.HomeColony, null)).Accepted); Assert.Equal(700, session.State.Colonies[0].ProductionReserve); Assert.Equal("building", session.State.Colonies[0].ProjectKind);
    }
    [Fact]
    public void CapacityTimingAndIdOverflowAreAtomic()
    {
        var s = ExpansionFixtures.AtHome(); s.Colonies[0].Population = 10; s.Colonies[0].Workforce = new(2, 8, 0); s.Colonies[0].ProductionReserve = 2400; var session = Selected(s);
        Assert.Equal(0, session.Forecast().Colonies[0].GrowthEarned.Total); Assert.True(session.Execute(new SetReadyCommand(Fixtures.Empire, 0, true)).Accepted); Assert.Equal(8, session.State.Colonies[0].Population); Assert.Equal(0, session.State.Colonies[0].GrowthProgress);
        s = ExpansionFixtures.AtHome(); s.Colonies[0].ProductionReserve = 2400; s.NextEntityId = int.MaxValue; session = Selected(s); var old = session.State; var o = session.Execute(new SetReadyCommand(Fixtures.Empire, 0, true)); Assert.False(o.Accepted); Assert.Same(old, session.State); Assert.False(session.State.Empires[0].Ready); Assert.Single(session.State.Vessels);
    }
    [Fact] public void LegacyRejectsVesselSelection() { var session = new GameSession(ExpansionFixtures.Content(), Fixtures.StartingState()); Assert.Equal("expansion_unavailable", session.Execute(new SetProductionTargetCommand(Fixtures.Empire, 0, Fixtures.HomeColony, new("vessel", "scout"))).Rejection!.Code); }
}
