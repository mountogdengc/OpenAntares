using OpenAntares.Simulation.Commands;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public class ExpansionCommandTests
{
    [Fact] public void PendingMovesReplaceCancelAndNoOpsPreserveReadiness()
    {
        var c=ExpansionFixtures.Content();var s=ExpansionFixtures.AtHome();s.Empires[0].Ready=true;
        var cancel=CommandProcessor.Execute(c,s,new SetMoveOrderCommand(Fixtures.Empire,0,new(5),null));Assert.False(cancel.Changed);Assert.True(cancel.State.Empires[0].Ready);
        var ordered=CommandProcessor.Execute(c,s,new SetMoveOrderCommand(Fixtures.Empire,0,new(5),new(6)));Assert.True(ordered.Accepted);Assert.False(ordered.State.Empires[0].Ready);s=ordered.State;s.Empires[0].Ready=true;
        Assert.False(CommandProcessor.Execute(c,s,new SetMoveOrderCommand(Fixtures.Empire,0,new(5),new(6))).Changed);
        var invalid=CommandProcessor.Execute(c,s,new SetMoveOrderCommand(Fixtures.Empire,0,new(5),new(999)));Assert.False(invalid.Accepted);Assert.Same(s,invalid.State);Assert.True(s.Empires[0].Ready);
        Assert.Null(CommandProcessor.Execute(c,s,new SetMoveOrderCommand(Fixtures.Empire,0,new(5),null)).State.Vessels[0].PendingMove);
    }
    [Fact] public void OwnershipTurnLegacyAndTransitRejectWithoutMutation()
    {
        var c=ExpansionFixtures.Content();var s=ExpansionFixtures.AtHome();
        Assert.Equal("not_owner",CommandProcessor.Execute(c,s,new SetMoveOrderCommand(Fixtures.Empire,0,new(999),new(6))).Rejection!.Code);
        Assert.Equal("stale_turn",CommandProcessor.Execute(c,s,new SetMoveOrderCommand(Fixtures.Empire,99,new(5),new(6))).Rejection!.Code);
        Assert.Equal("already_there",CommandProcessor.Execute(c,s,new SetMoveOrderCommand(Fixtures.Empire,0,new(5),Fixtures.HomeStar)).Rejection!.Code);
        s.RulesVersion=1;Assert.Equal("expansion_unavailable",CommandProcessor.Execute(c,s,new SetMoveOrderCommand(Fixtures.Empire,0,new(5),new(6))).Rejection!.Code);s.RulesVersion=2;
        s.Vessels[0].StationaryStarId=null;s.Vessels[0].Transit=new(Fixtures.HomeStar,new(6),2,1);
        foreach(StarId? target in new StarId?[]{new(6),Fixtures.HomeStar,null}) {var o=CommandProcessor.Execute(c,s,new SetMoveOrderCommand(Fixtures.Empire,0,new(5),target));Assert.Equal("vessel_in_transit",o.Rejection!.Code);Assert.Same(s,o.State);}
    }
}
