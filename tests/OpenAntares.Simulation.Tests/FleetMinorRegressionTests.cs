using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetMinorRegressionTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public void ReturnChecksLostColonyAfterCommittedArrivalAndKeepsAttention(bool lost)
    {
        var g=FleetFixtures.Game();g.State.Crisis!.Enabled=true;g.State.Empires[0].Crisis!.NextEligibleTurn=1000;var colony=FleetOfficerTests.AddColony(g);var target=g.State.FindPlanet(colony.PlanetId)!.StarId;var ship=g.State.Vessels[0];
        var created=FleetCommandTests.Run(g,new FleetAction.Create("Homeward",ImmutableArray.Create(ship.Id)));Assert.True(created.Accepted);g=new(g.Content,created.State);
        var ordered=FleetCommandTests.Run(g,new FleetAction.SetOrder(ImmutableArray.Create(new FleetId(1)),new(FleetOrderKind.Return,target)));Assert.True(ordered.Accepted,ordered.Rejection?.Message);
        var session=new GameSession(g.Content,ordered.State);var owner=session.State.Empires[0].Id;
        Assert.True(session.Execute(new SetReadyCommand(owner,session.State.Turn,true)).Accepted);
        // Labelled lost-colony supplement after ordinary departure, before mounting a new session.
        var committed=session.State.Clone();Assert.NotNull(committed.FindVessel(ship.Id)!.Transit);if(lost)Crises.CrisisLoss.Remove(committed,colony.Id,Crises.CrisisQueries.Location(committed,committed.FindColony(colony.Id)!));ObservationUpdater.Refresh(committed,committed.Turn);FleetFixtures.Valid(new(g.Content,committed));
        session=new GameSession(g.Content,committed);
        for(int i=0;i<100&&session.State.FindVessel(ship.Id)!.Transit is not null;i++)
        {
            var fleet=session.State.Empires[0].Fleets!.Fleets[0];Assert.Equal(FleetOrderKind.Return,fleet.Order.Kind);Assert.Null(fleet.Attention);
            Assert.True(session.Execute(new SetReadyCommand(owner,session.State.Turn,true)).Accepted);
        }
        Assert.Equal(target,session.State.FindVessel(ship.Id)!.StationaryStarId);
        var result=FleetTurnCoordinator.PrepareOrders(session.Content,session.State,owner);Assert.True(result.Succeeded);var final=result.State.Empires[0].Fleets!.Fleets[0];
        Assert.Equal(lost?FleetOrderKind.Return:FleetOrderKind.Hold,final.Order.Kind);
        if(lost){Assert.Contains("no longer an owned colony",final.Attention);Assert.Equal(FleetImportance.Attention,Assert.Single(result.State.Empires[0].Fleets!.PendingEvents,e=>e.Kind=="fleet_order"&&e.Reason.Contains("no longer an owned colony")).Importance);}else Assert.Null(final.Attention);
        Assert.Null(result.State.FindVessel(ship.Id)!.PendingMove);
        string bytes=SaveGame.Serialize(g.Content,result.State);var loaded=SaveGame.Deserialize(bytes);Assert.True(loaded.Succeeded);Assert.Equal(bytes,SaveGame.Serialize(loaded.Game!.Content,loaded.Game.State));
        var retry=FleetTurnCoordinator.PrepareOrders(g.Content,loaded.Game.State,owner);Assert.True(retry.Succeeded);Assert.Equal(bytes,SaveGame.Serialize(g.Content,retry.State));
        if(lost)
        {
            var held=FleetCommandTests.Run(new(g.Content,retry.State),new FleetAction.SetOrder(ImmutableArray.Create(new FleetId(1)),new(FleetOrderKind.Hold)));Assert.True(held.Accepted);Assert.Null(held.State.Empires[0].Fleets!.Fleets[0].Attention);
            var moved=FleetCommandTests.Run(new(g.Content,retry.State),new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(new FleetId(1)),g.State.Vessels[0].StationaryStarId));Assert.True(moved.Accepted,moved.Rejection?.Message);Assert.Null(moved.State.Empires[0].Fleets!.Fleets[0].Attention);Assert.Equal(FleetOrderKind.Hold,moved.State.Empires[0].Fleets!.Fleets[0].Order.Kind);
        }
    }
}
