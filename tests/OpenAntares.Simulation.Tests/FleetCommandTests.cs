using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetCommandTests
{
    [Fact]
    public void SplitMergeFlagshipAndLateBulkFailureAreAtomic()
    {
        var g=FleetFixtures.Game();var s=g.State;var first=s.Vessels[0];var second=first.Clone();second.Id=new(s.NextEntityId++);s.Vessels.Add(second);
        var create=Run(g,new FleetAction.Create("Both",ImmutableArray.Create(second.Id,first.Id,first.Id)));
        Assert.True(create.Accepted,create.Rejection?.Message);g=new(g.Content,create.State);
        var group=Assert.Single(g.State.Empires[0].Fleets!.Fleets);Assert.Equal(first.Id,group.Flagship);
        Assert.False(CommandProcessor.Execute(g.Content,g.State,new SetMoveOrderCommand(g.State.Empires[0].Id,0,second.Id,g.State.Stars.First(x=>x.Id!=first.StationaryStarId).Id)).Accepted);
        var flag=Run(g,new FleetAction.SetFlagship(group.Id,second.Id));Assert.True(flag.Accepted);g=new(g.Content,flag.State);
        var split=Run(g,new FleetAction.Split(group.Id,"Separate",ImmutableArray.Create(first.Id)));Assert.True(split.Accepted,split.Rejection?.Message);g=new(g.Content,split.State);
        var merge=Run(g,new FleetAction.Merge(group.Id,ImmutableArray.Create(new FleetId(2))));Assert.True(merge.Accepted,merge.Rejection?.Message);g=new(g.Content,merge.State);Assert.Single(g.State.Empires[0].Fleets!.Fleets);
        var detached=Run(g,new FleetAction.Disband(group.Id));Assert.True(detached.Accepted);g=new(g.Content,detached.State);
        var destination=g.State.Stars.First(x=>x.Id!=first.StationaryStarId).Id;g.State.FindVessel(second.Id)!.StationaryStarId=destination;
        string before=FleetFixtures.Bytes(g.State);
        var reject=Run(g,new FleetAction.MoveSelection(ImmutableArray.Create(first.Id,second.Id),ImmutableArray<FleetId>.Empty,destination));Assert.False(reject.Accepted);Assert.Same(g.State,reject.State);Assert.Equal(before,FleetFixtures.Bytes(g.State));
    }
    internal static CommandOutcome Run(Saves.LoadedGame g, FleetAction action, int? turn = null) => CommandProcessor.Execute(g.Content, g.State, new FleetCommand(g.State.Empires[0].Id, turn ?? g.State.Turn, action));
    [Fact]
    public void BulkRejectsAllOnStaleForeignOrIllegalMember()
    {
        var g = FleetFixtures.Game(rivals: 1); var own = g.State.Empires[0]; var ship = g.State.Vessels.Single(v => v.EmpireId == own.Id);
        string before = FleetFixtures.Bytes(g.State);
        foreach (var ids in new[] { ImmutableArray.Create(ship.Id, new VesselId(99999)), g.State.Vessels.Select(v => v.Id).ToImmutableArray() })
        { var rejected = Run(g, new FleetAction.Create("Test", ids)); Assert.False(rejected.Accepted); Assert.Same(g.State, rejected.State); }
        Assert.False(Run(g, new FleetAction.Create("Test", ImmutableArray.Create(ship.Id)), -1).Accepted);
        Assert.Equal(before, FleetFixtures.Bytes(g.State));
        var accepted = Run(g, new FleetAction.Create("Test", ImmutableArray.Create(ship.Id)));
        Assert.True(accepted.Accepted, accepted.Rejection?.Message); Assert.Single(accepted.State.Empires[0].Fleets!.Fleets);
        Assert.Equal(before, FleetFixtures.Bytes(g.State)); Assert.Empty(GameStateValidator.Validate(g.Content, accepted.State));
    }
    [Fact]
    public void GroupEditsAndNoOpsPreserveAssignmentsAndReadiness()
    {
        var g = FleetFixtures.Valid(FleetFixtures.Nested()); var f = g.State.Empires[0].Fleets!.Fleets[0];
        g.State.Empires[0].Ready = true;
        var same = Run(g, new FleetAction.Rename(f.Id, f.Name)); Assert.True(same.Accepted); Assert.Same(g.State, same.State); Assert.True(same.State.Empires[0].Ready);
        var changed = Run(g, new FleetAction.Rename(f.Id, "Renamed")); Assert.True(changed.Accepted); Assert.False(changed.State.Empires[0].Ready);
        Assert.False(Run(g, new FleetAction.Rename(f.Id, new string('x',81))).Accepted);
        var disband = Run(g, new FleetAction.Disband(f.Id)); Assert.True(disband.Accepted); Assert.Null(disband.State.FindVessel(f.Flagship)!.Fleet);
    }
    [Fact]
    public void NamedFleetCapDoesNotBlockIndependentConstructionOrSafety()
    {
        var g = FleetFixtures.Game(); var s = g.State; var e = s.Empires[0]; var f = e.Fleets!; var original = s.Vessels[0];
        for (int i = 1; i <= 64; i++)
        {
            var v = i == 1 ? original : original.Clone();
            if(i != 1) { v.Id = new(s.NextEntityId++); s.Vessels.Add(v); }
            v.Fleet = new(i); f.Fleets.Add(new() { Id=new(i),Name=$"Fleet {i}",Members=new(){v.Id},Flagship=v.Id });
        }
        f.NextFleetId=65; var independent=original.Clone(); independent.Id=new(s.NextEntityId++); independent.Fleet=null; s.Vessels.Add(independent);
        FleetFixtures.Valid(g);
        Assert.False(Run(g,new FleetAction.Create("Too many",ImmutableArray.Create(independent.Id))).Accepted);
        var destination=s.Stars.First(x=>x.Id!=original.StationaryStarId).Id;
        var moved=Run(g,new FleetAction.MoveSelection(ImmutableArray.Create(independent.Id),ImmutableArray<FleetId>.Empty,destination));
        Assert.True(moved.Accepted,moved.Rejection?.Message); Assert.Equal(destination,moved.State.FindVessel(independent.Id)!.PendingMove);
        Assert.Equal(64,moved.State.Empires[0].Fleets!.Fleets.Count);
        s.Colonies[0].ProductionReserve=800;
        var session=new GameSession(g.Content,s);
        Assert.True(session.Execute(new SetProductionTargetCommand(e.Id,0,s.Colonies[0].Id,new(ProductionKinds.Vessel,"scout"))).Accepted);
        var completed=session.Execute(new SetReadyCommand(e.Id,0,true));Assert.True(completed.Accepted,completed.Rejection?.Message);
        Assert.Equal(66,session.State.Vessels.Count);Assert.Equal(64,session.State.Empires[0].Fleets!.Fleets.Count);
        Assert.Single(session.State.Vessels,v=>v.NewlyCompleted&&v.Fleet is null);
    }
}
