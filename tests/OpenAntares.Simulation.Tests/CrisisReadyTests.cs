using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Saves;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisReadyTests
{
    [Fact]
    public void AcknowledgementBlocksBeforeAnyPreparation()
    {
        var g=CrisisFixtures.Warning();var c=g.State.Colonies[0];c.Development!.Template=ColonyFixtures.Snapshot(g.Content,"industrial");c.Development.WorkforceEnabled=true;c.Development.ConstructionEnabled=true;g.State.Empires[0].Crisis!.Authority=CrisisAuthority.ContainAndResearch;
        string before=SaveGame.Serialize(g.Content,g.State);var result=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(c.EmpireId,g.State.Turn,true));
        Assert.False(result.Accepted);Assert.Same(g.State,result.State);Assert.Equal(before,SaveGame.Serialize(g.Content,result.State));Assert.False(result.State.Empires[0].Ready);Assert.False(result.State.Colonies[0].Crisis!.Quarantined);Assert.Null(result.State.Empires[0].Development!.PendingReport);
    }
    [Fact]
    public void AnotherReadyHumanIsUnchanged()
    {
        var g=CrisisPlanningTests.MultiWithCase(warning:true);var other=g.State.Empires[1];var readyOther=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(other.Id,g.State.Turn,true));Assert.True(readyOther.Accepted,readyOther.Rejection?.Message);
        string before=SaveGame.Serialize(g.Content,readyOther.State);var owner=readyOther.State.Empires[0];var denied=CommandProcessor.Execute(g.Content,readyOther.State,new SetReadyCommand(owner.Id,readyOther.State.Turn,true));
        Assert.False(denied.Accepted);Assert.Same(readyOther.State,denied.State);Assert.Equal(before,SaveGame.Serialize(g.Content,denied.State));Assert.True(denied.State.Empires[1].Ready);
    }
    [Fact]
    public void AcknowledgementThenPolicyAndWithdrawPreserveAcceptedPlanning()
    {
        var g=CrisisPlanningTests.MultiWithCase(warning:true);var owner=g.State.Empires[0].Id;
        var ack=CommandProcessor.Execute(g.Content,g.State,new AcknowledgeCrisisCommand(owner,g.State.Turn,CrisisQueries.PendingAttention(g.State,owner)));Assert.True(ack.Accepted);
        var ready=CommandProcessor.Execute(g.Content,ack.State,new SetReadyCommand(owner,g.State.Turn,true));Assert.True(ready.Accepted,ready.Rejection?.Message);Assert.True(ready.State.Colonies[0].Crisis!.Quarantined);Assert.Equal(50,ready.State.Empires[0].Crisis!.Episode!.ResearchShare);Assert.True(ready.State.Empires[0].Ready);
        var again=CommandProcessor.Execute(g.Content,ready.State,new SetReadyCommand(owner,g.State.Turn,true));Assert.Same(ready.State,again.State);Assert.False(again.Changed);
        var withdraw=CommandProcessor.Execute(g.Content,again.State,new SetReadyCommand(owner,g.State.Turn,false));Assert.True(withdraw.Accepted);Assert.False(withdraw.State.Empires[0].Ready);Assert.True(withdraw.State.Colonies[0].Crisis!.Quarantined);Assert.Equal(50,withdraw.State.Empires[0].Crisis!.Episode!.ResearchShare);Assert.NotNull(withdraw.State.Empires[0].Development!.PendingReport);
    }
    [Fact]
    public void ExplicitWaiverReloadAndReenableDoNotFabricateRead()
    {
        var g=CrisisPlanningTests.MultiWithCase(warning:true);var owner=g.State.Empires[0].Id;
        var waive=CommandProcessor.Execute(g.Content,g.State,new SetCrisisAcknowledgementCommand(owner,g.State.Turn,false));Assert.True(waive.Accepted);var reload=SaveGame.Deserialize(SaveGame.Serialize(g.Content,waive.State));Assert.True(reload.Succeeded);
        var ready=CommandProcessor.Execute(reload.Game!.Content,reload.Game.State,new SetReadyCommand(owner,g.State.Turn,true));Assert.True(ready.Accepted,ready.Rejection?.Message);Assert.Equal(0,ready.State.Colonies[0].Crisis!.Case!.HandledRevision);Assert.Equal(1,ready.State.Colonies[0].Crisis!.Case!.WaivedRevision);
        var withdrawn=CommandProcessor.Execute(g.Content,ready.State,new SetReadyCommand(owner,g.State.Turn,false));var require=CommandProcessor.Execute(g.Content,withdrawn.State,new SetCrisisAcknowledgementCommand(owner,g.State.Turn,true));Assert.True(require.Accepted);var denied=CommandProcessor.Execute(g.Content,require.State,new SetReadyCommand(owner,g.State.Turn,true));Assert.False(denied.Accepted);Assert.Same(require.State,denied.State);
    }
    [Fact]
    public void LoadedReadyCannotBypassSavedAcknowledgementRequirement()
    {
        var g=CrisisPlanningTests.MultiWithCase(warning:true);g.State.Empires[0].Ready=true;
        Assert.NotEmpty(OpenAntares.Simulation.State.GameStateValidator.Validate(g.Content,g.State));
    }
}
