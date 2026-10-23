using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.Saves;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public class MovementTests
{
    [Theory] [InlineData(300,0,300,1)] [InlineData(301,0,300,2)] [InlineData(300,300,200,3)] [InlineData(0,900,200,5)]
    public void TravelDuration(int x,int y,int speed,int expected)=>Assert.Equal(expected,TravelCalculator.Duration(0,0,x,y,speed));
    [Fact] public void InvalidSpeedAndUnrepresentableDurationReject() {Assert.Throws<ArgumentOutOfRangeException>(()=>TravelCalculator.Duration(0,0,1,1,0));Assert.Throws<OverflowException>(()=>TravelCalculator.Duration(int.MinValue,0,int.MaxValue,0,1));}
    [Theory] [InlineData(300,1)] [InlineData(301,2)] public void RouteArrivesAtExactPublishedTurn(int distance,int turns)
    {
        var c=ExpansionFixtures.Content();var s=ExpansionFixtures.AtHome();s.Stars[1].X=distance;ulong rng=s.Random.State;
        var order=CommandProcessor.Execute(c,s,new SetMoveOrderCommand(Fixtures.Empire,0,new(5),new(6)));Assert.True(order.Accepted);Assert.NotNull(order.State.Vessels[0].PendingMove);Assert.Null(s.Vessels[0].PendingMove);
        var session=new GameSession(c,order.State);
        for(int t=1;t<=turns;t++) {var o=session.Execute(new SetReadyCommand(Fixtures.Empire,session.State.Turn,true));Assert.True(o.Accepted,o.Rejection?.Message);Assert.Equal(t,session.State.Turn);Assert.False(session.State.Empires[0].Ready);if(t<turns)Assert.NotNull(session.State.Vessels[0].Transit);}
        Assert.Equal(new StarId(6),session.State.Vessels[0].StationaryStarId);Assert.Equal(rng,session.State.Random.State);Assert.Equal(2,session.State.Empires[0].Surveys.Count);Assert.Contains(session.View(Fixtures.Empire).Stars,x=>x.Chart.Id==new StarId(6)&&x.Current);
    }
    [Fact] public void SaveDuringTransitMatchesUninterruptedArrival()
    {
        var c=ExpansionFixtures.Content();var s=ExpansionFixtures.AtHome();s.Stars[1].X=900;var a=new GameSession(c,s);a.Execute(new SetMoveOrderCommand(Fixtures.Empire,0,new(5),new(6)));Assert.True(a.Execute(new SetReadyCommand(Fixtures.Empire,0,true)).Accepted);
        var loaded=SaveGame.Deserialize(SaveGame.Serialize(c,a.State));Assert.True(loaded.Succeeded);var b=new GameSession(loaded.Game!.Content,loaded.Game.State);
        for(int i=0;i<2;i++){Assert.True(a.Execute(new SetReadyCommand(Fixtures.Empire,a.State.Turn,true)).Accepted);Assert.True(b.Execute(new SetReadyCommand(Fixtures.Empire,b.State.Turn,true)).Accepted);Assert.Equal(Fixtures.Snapshot(a.State),Fixtures.Snapshot(b.State));}
    }
    [Fact] public void ReadyCommandAllowsOnlyUnpublishedAllReadyInput()
    {
        var s=ExpansionFixtures.AtHome();var c=ExpansionFixtures.Content();var o=CommandProcessor.Execute(c,s,new SetReadyCommand(Fixtures.Empire,0,true));Assert.True(o.Accepted,o.Rejection?.Message);Assert.Equal(1,o.State.Turn);
        s.Vessels[0].CarriedPopulation=1;var bad=CommandProcessor.Execute(c,s,new SetReadyCommand(Fixtures.Empire,0,true));Assert.False(bad.Accepted);Assert.Same(s,bad.State);Assert.False(s.Empires[0].Ready);
    }
}
