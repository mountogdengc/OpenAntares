using System.Linq;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatReadyTests
{
    internal static LoadedGame Multi()
    {
        var old=FleetReadyTests.Multi();var s=old.State.Clone();s.RulesVersion=9;Assert.Null(CombatInitializer.Initialize(old.Content,s,false));return CombatFixtures.Valid(new(old.Content,s));
    }
    [Fact] public void CoordinatorPreparesPrivatelyAndRetainsFiniteCombatOrders()
    {
        var g=CombatTurnTests.Pending();var before=CombatFixtures.Bytes(g.State);var result=CombatTurnCoordinator.PrepareHuman(g.Content,g.State,g.State.Empires[0].Id);Assert.True(result.Succeeded);Assert.Equal(before,CombatFixtures.Bytes(g.State));Assert.Single(result.State.Combat!.Encounters);Assert.Null(result.State.Vessels[0].PendingMove);Assert.Empty(GameStateValidator.Validate(g.Content,result.State));
    }
    [Fact] public void TwoHumansPrepareOnceAndMatchDirectResolution()
    {
        var g=FleetReadyTests.Explore(Multi());var first=g.State.Empires[0].Id;var second=g.State.Empires[1].Id;var ready1=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(first,0,true));Assert.True(ready1.Accepted,ready1.Rejection?.Message);Assert.True(ready1.State.FindEmpire(first)!.Ready);var priorMove=ready1.State.Vessels.First(v=>v.EmpireId==first).PendingMove;Assert.NotNull(priorMove);string before=CombatFixtures.Bytes(ready1.State);
        var retry=CommandProcessor.Execute(g.Content,ready1.State,new SetReadyCommand(first,0,true));Assert.False(retry.Changed);Assert.Same(ready1.State,retry.State);var completed=CommandProcessor.Execute(g.Content,ready1.State,new SetReadyCommand(second,0,true));var direct=TurnResolver.Resolve(g.Content,g.State);Assert.True(completed.Accepted,completed.Rejection?.Message);Assert.True(direct.Succeeded,string.Join(";",direct.Errors));Assert.Equal(CombatFixtures.Bytes(direct.State),CombatFixtures.Bytes(completed.State));Assert.Equal(before,CombatFixtures.Bytes(ready1.State));
    }
    [Fact] public void OwnAutomaticQuarantineIsVisibleBeforeCombatReadyPreparation()
    {
        var g=CombatWithdrawalTests.Queued(CombatWithdrawalTests.Battle(new[]{100,100},new[]{true,false},crises:true),StragglerPolicy.KeepTogether);var s=g.State;var colony=s.Colonies[0];var definition=g.Content.Plagues.Values.Single();var health=s.Empires[0].Crisis!;health.RequireAcknowledgement=false;health.Authority=OpenAntares.Simulation.Crises.CrisisAuthority.Contain;health.NextEpisodeNumber=2;var location=OpenAntares.Simulation.Crises.CrisisQueries.Location(s,colony);health.Episode=new(){Id=1,FamilyId=definition.Id,Source=location,SourcePopulation=colony.Population,CureCost=checked(definition.CureBase+definition.CurePerPopulation*colony.Population)};colony.Crisis!.Case=new(){EpisodeId=1,Stage=OpenAntares.Simulation.Crises.CrisisStage.Warning,GraceRemaining=definition.GraceTurns,AttentionRevision=1,Origin=location};CombatFixtures.Valid(g);
        Assert.Contains(CombatRejectionCodes.PortClosed,CombatReadyQueries.ValidateResponse(g.Content,s,s.Empires[0].Id));Assert.False(colony.Crisis.Quarantined);var e=s.Combat!.Encounters[0];var continued=CombatCommandTests.Commit(g,new CombatAction.RespondCombat(e.Id,e.Revision,CombatResponse.RemainWithBlockedReturn));var ready=CommandProcessor.Execute(g.Content,continued,new SetReadyCommand(s.Empires[0].Id,s.Turn,true));Assert.True(ready.Accepted,ready.Rejection?.Message);Assert.True(ready.State.Colonies[0].Crisis!.Quarantined);var encounter=Assert.Single(ready.State.Combat!.Encounters);Assert.Equal(CombatPhase.BlockedResponse,encounter.Phase);Assert.Equal(2,encounter.Round);Assert.False(encounter.Rounds.Last().Interception);Assert.Null(ready.State.Vessels[0].Transit);
    }
    [Fact] public void PublishedReadyCannotCarryAnOutstandingCombatResponse()
    {
        var g=CombatTurnTests.Pending();var s=g.State;var e=s.Combat!.Encounters[0];e.Phase=CombatPhase.BlockedResponse;e.Revision=2;e.ConsentRevision=null;s.Empires[0].Combat!.Responses.Add(new(e.Id,2,"Revised force"));s.Empires[0].Ready=true;Assert.NotEmpty(GameStateValidator.Validate(g.Content,s,allowAllHumansReady:true));
    }
    [Fact] public void LatePreparationFailurePreservesEarlierHumanPlanning()
    {
        var g=FleetReadyTests.Explore(Multi());var second=g.State.Colonies.First(c=>c.EmpireId==g.State.Empires[1].Id);second.Workforce=new(second.Population-1,1,0);second.ProductionReserve=long.MaxValue;var first=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,0,true));Assert.True(first.Accepted,first.Rejection?.Message);string before=CombatFixtures.Bytes(first.State);var failed=CommandProcessor.Execute(g.Content,first.State,new SetReadyCommand(g.State.Empires[1].Id,0,true));Assert.False(failed.Accepted);Assert.Same(first.State,failed.State);Assert.Equal(before,CombatFixtures.Bytes(failed.State));Assert.True(failed.State.Empires[0].Ready);
    }
}
