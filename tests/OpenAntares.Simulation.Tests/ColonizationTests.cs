using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;

public class ColonizationTests
{
    private static GameState Settler()
    {
        var s = ExpansionFixtures.AtHome(); s.Vessels[0].RoleId = "settler"; s.Vessels[0].CarriedPopulation = 2; s.Vessels[0].StationaryStarId = new(6); ObservationUpdater.Refresh(s, 0); return s;
    }
    [Fact]
    public void FoundingConsumesOneCargoAndStartsNextSnapshot()
    {
        var session = new GameSession(ExpansionFixtures.Content(), Settler()); var o = session.Execute(new SetFoundingOrderCommand(Fixtures.Empire, 0, new(5), new(7))); Assert.True(o.Accepted, o.Rejection?.Message); Assert.Single(session.State.Colonies);
        o = session.Execute(new SetReadyCommand(Fixtures.Empire, 0, true)); Assert.True(o.Accepted, o.Rejection?.Message); Assert.Empty(session.State.Vessels); Assert.Equal(2, session.State.Colonies.Count);
        var colony = session.State.Colonies[1]; Assert.Equal(8, colony.Id.Value); Assert.Equal(2, colony.Population); Assert.Equal(new Workforce(2, 0, 0), colony.Workforce); Assert.Equal(0, colony.GrowthProgress); Assert.Equal(0, colony.ProductionReserve); Assert.Null(colony.ProjectId); Assert.Empty(colony.CompletedBuildingIds); Assert.Single(o.Report!.Economy.Colonies);
        var f = session.Forecast().Colony(colony.Id); Assert.Equal(400, f.SupportProduced.Total); Assert.Equal(200, f.SupportRequired.Total); Assert.Equal(30, f.GrowthEarned.Total); Assert.Equal(10, f.Growth.Capacity);
    }
    [Fact]
    public void UnknownPlanetFailuresAreIndistinguishableAndOrdersReplaceAtomically()
    {
        var c = ExpansionFixtures.Content(); var s = ExpansionFixtures.AtHome(); s.Vessels[0].RoleId = "settler"; s.Vessels[0].CarriedPopulation = 2;
        var a = CommandProcessor.Execute(c, s, new SetFoundingOrderCommand(Fixtures.Empire, 0, new(5), new(7))); var b = CommandProcessor.Execute(c, s, new SetFoundingOrderCommand(Fixtures.Empire, 0, new(5), new(999)));
        Assert.Equal("target_not_known", a.Rejection!.Code); Assert.Equal(a.Rejection, b.Rejection); Assert.Same(s, a.State);
        s = Settler(); var founding = CommandProcessor.Execute(c, s, new SetFoundingOrderCommand(Fixtures.Empire, 0, new(5), new(7))).State; founding.Empires[0].Ready = true;
        var invalid = CommandProcessor.Execute(c, founding, new SetMoveOrderCommand(Fixtures.Empire, 0, new(5), new(999))); Assert.Same(founding, invalid.State); Assert.NotNull(founding.Vessels[0].PendingFounding); Assert.True(founding.Empires[0].Ready);
        var clearedMove = CommandProcessor.Execute(c, founding, new SetMoveOrderCommand(Fixtures.Empire, 0, new(5), null)); Assert.False(clearedMove.Changed); Assert.NotNull(clearedMove.State.Vessels[0].PendingFounding);
        var move = CommandProcessor.Execute(c, founding, new SetMoveOrderCommand(Fixtures.Empire, 0, new(5), Fixtures.HomeStar)); Assert.True(move.Accepted); Assert.Null(move.State.Vessels[0].PendingFounding);
        var restored = CommandProcessor.Execute(c, move.State, new SetFoundingOrderCommand(Fixtures.Empire, 0, new(5), new(7))); Assert.True(restored.Accepted); Assert.Null(restored.State.Vessels[0].PendingMove);
    }
    [Fact]
    public void SameEmpireReservationsRejectAndDifferentEmpiresResolveByEmpireThenUnit()
    {
        var c = ExpansionFixtures.Content(); var s = Settler(); s.Vessels.Add(new VesselState { Id = new(8), EmpireId = Fixtures.Empire, RoleId = "settler", CarriedPopulation = 2, StationaryStarId = new(6) }); s.NextEntityId = 9;
        var session = new GameSession(c, s); Assert.True(session.Execute(new SetFoundingOrderCommand(Fixtures.Empire, 0, new(5), new(7))).Accepted); Assert.Equal("target_reserved", session.Execute(new SetFoundingOrderCommand(Fixtures.Empire, 0, new(8), new(7))).Rejection!.Code);
        s = Settler(); s.Empires.Add(new EmpireState { Id = new(8), Controller = ControllerKind.Ai }); s.Vessels[0].EmpireId = new(8); s.Vessels.Add(new VesselState { Id = new(9), EmpireId = Fixtures.Empire, RoleId = "settler", CarriedPopulation = 2, StationaryStarId = new(6) }); s.NextEntityId = 10; ObservationUpdater.Refresh(s, 0);
        session = new(c, s); Assert.True(session.Execute(new SetFoundingOrderCommand(new(8), 0, new(5), new(7))).Accepted); Assert.True(session.Execute(new SetFoundingOrderCommand(Fixtures.Empire, 0, new(9), new(7))).Accepted);
        var resolved = session.Execute(new SetReadyCommand(Fixtures.Empire, 0, true)); Assert.True(resolved.Accepted, resolved.Rejection?.Message); Assert.Equal(Fixtures.Empire, session.State.Colonies[1].EmpireId); var loser = Assert.Single(session.State.Vessels); Assert.Equal(new VesselId(5), loser.Id); Assert.Equal(2, loser.CarriedPopulation); Assert.Null(loser.PendingFounding); Assert.Contains(resolved.Report!.ExpansionEvents, e => e.Kind == "founding_failed" && e.Owner == new EmpireId(8));
    }
    [Fact]
    public void PendingFoundingSaveAndAllocationOverflowPreserveIntent()
    {
        var c = ExpansionFixtures.Content(); var session = new GameSession(c, Settler()); session.Execute(new SetFoundingOrderCommand(Fixtures.Empire, 0, new(5), new(7))); var loaded = SaveGame.Deserialize(SaveGame.Serialize(c, session.State)); Assert.True(loaded.Succeeded); var other = new GameSession(loaded.Game!.Content, loaded.Game.State);
        Assert.True(session.Execute(new SetReadyCommand(Fixtures.Empire, 0, true)).Accepted); Assert.True(other.Execute(new SetReadyCommand(Fixtures.Empire, 0, true)).Accepted); Assert.Equal(Fixtures.Snapshot(session.State), Fixtures.Snapshot(other.State));
        var s = Settler(); s.NextEntityId = int.MaxValue; session = new(c, s); session.Execute(new SetFoundingOrderCommand(Fixtures.Empire, 0, new(5), new(7))); var before = session.State; Assert.False(session.Execute(new SetReadyCommand(Fixtures.Empire, 0, true)).Accepted); Assert.Same(before, session.State); Assert.False(before.Empires[0].Ready); Assert.NotNull(before.Vessels[0].PendingFounding);
    }
    [Fact]
    public void ArrivalRequiresAnotherPlanningOrderBeforeFounding()
    {
        var s = Settler(); s.Vessels[0].StationaryStarId = Fixtures.HomeStar; var session = new GameSession(ExpansionFixtures.Content(), s); Assert.True(session.Execute(new SetMoveOrderCommand(Fixtures.Empire, 0, new(5), new(6))).Accepted); Assert.False(session.Execute(new SetFoundingOrderCommand(Fixtures.Empire, 0, new(5), new(7))).Accepted);
        for (int i = 0; i < 2; i++) Assert.True(session.Execute(new SetReadyCommand(Fixtures.Empire, session.State.Turn, true)).Accepted); Assert.Single(session.State.Colonies); Assert.True(session.Execute(new SetFoundingOrderCommand(Fixtures.Empire, 2, new(5), new(7))).Accepted);
    }
    [Fact]
    public void TwoWorldsInOneSystemRemainIndependent()
    {
        var s = Settler(); s.Planets.Add(new PlanetState { Id = new(8), StarId = new(6), Orbit = 2, PlanetTypeId = "crystal_world" }); s.Vessels.Add(new VesselState { Id = new(9), EmpireId = Fixtures.Empire, RoleId = "settler", CarriedPopulation = 2, StationaryStarId = new(6) }); s.NextEntityId = 10; ObservationUpdater.Refresh(s, 0); var session = new GameSession(ExpansionFixtures.Content(), s);
        Assert.True(session.Execute(new SetFoundingOrderCommand(Fixtures.Empire, 0, new(5), new(7))).Accepted); Assert.True(session.Execute(new SetFoundingOrderCommand(Fixtures.Empire, 0, new(9), new(8))).Accepted); Assert.True(session.Execute(new SetReadyCommand(Fixtures.Empire, 0, true)).Accepted); Assert.Equal(new[] { 7, 8 }, session.View(Fixtures.Empire).Colonies.Where(c => c.StarId == new StarId(6)).Select(c => c.PlanetId.Value));
    }
    [Theory]
    [InlineData("scout")]
    [InlineData("foreign")]
    [InlineData("transit")]
    [InlineData("occupied")]
    [InlineData("unsupported")]
    public void InvalidFoundingPreservesCargo(string mode)
    {
        var s = Settler(); var c = ExpansionFixtures.Content();
        if (mode == "scout") { s.Vessels[0].RoleId = "scout"; s.Vessels[0].CarriedPopulation = 0; }
        if (mode == "foreign") { s.Vessels[0].EmpireId = new(99); }
        if (mode == "transit") { s.Vessels[0].StationaryStarId = null; s.Vessels[0].Transit = new(Fixtures.HomeStar, new(6), 2, 1); }
        if (mode == "occupied") { s.Colonies.Add(new ColonyState { Id = new(8), EmpireId = Fixtures.Empire, PlanetId = new(7), Population = 2, Workforce = new(2, 0, 0) }); s.NextEntityId = 9; }
        if (mode == "unsupported") c = c with { PlanetTypes = c.PlanetTypes.SetItem("forge_world", c.PlanetTypes["forge_world"] with { Colonizable = false }) };
        var o = CommandProcessor.Execute(c, s, new SetFoundingOrderCommand(Fixtures.Empire, 0, new(5), new(7))); Assert.False(o.Accepted); Assert.Same(s, o.State);
    }
}
