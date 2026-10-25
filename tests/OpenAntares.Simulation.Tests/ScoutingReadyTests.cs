using System;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ScoutingReadyTests
{
    internal static LoadedGame MultiWarning()
    {
        var g=ScoutingFixtures.TwoHumansAndAi();var s=g.State;var v=s.Vessels[0];v.StationaryStarId=new(16);v.AmebaEncounter=new(){Number=1,HabitatId=1};s.Empires[0].Scouting!.NextEncounterNumber=2;s.Empires[0].Scouting!.KnownHabitats.Add(new(1,new(16),0));OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(s,0);return ScoutingFixtures.Valid(g);
    }
    internal static LoadedGame Acknowledge(LoadedGame g)
    {
        var owner=g.State.Empires[0].Id;var r=CommandProcessor.Execute(g.Content,g.State,new AcknowledgeAmebaCommand(owner,g.State.Turn,ScoutQueries.Project(g.Content,g.State,owner).Attention));Assert.True(r.Accepted);return g with{State=r.State};
    }
    [Fact] public void ReadyAndDirectResolveCannotBypassCurrentConsent()
    {
        var g=ScoutingFixtures.AtAmeba();var s=g.State;string before=ScoutingFixtures.Bytes(s);var denied=CommandProcessor.Execute(g.Content,s,new SetReadyCommand(s.Empires[0].Id,s.Turn,true));
        Assert.False(denied.Accepted);Assert.Equal("ameba_acknowledgement_required",denied.Rejection!.Code);Assert.Same(s,denied.State);Assert.Equal(before,ScoutingFixtures.Bytes(s));var direct=TurnResolver.Resolve(g.Content,s);Assert.False(direct.Succeeded);Assert.Same(s,direct.State);
    }
    [Fact] public void NonFinalReadyMaterializesOnlyThePreviewedRouteAndKeepsExactReceipt()
    {
        var g=Acknowledge(MultiWarning());var owner=g.State.Empires[0].Id;var token=g.State.Empires[0].Scouting!.Receipts[0].Token;var before=ScoutingFixtures.Bytes(g.State);
        var ready=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(owner,0,true));Assert.True(ready.Accepted,ready.Rejection?.Message);Assert.Equal(0,ready.State.Turn);Assert.True(ready.State.Empires[0].Ready);Assert.Equal(token.Destination,ready.State.Vessels[0].PendingMove);Assert.Equal(token,ScoutQueries.Project(g.Content,ready.State,owner).Attention[0]);Assert.Equal(before,ScoutingFixtures.Bytes(g.State));
        var same=CommandProcessor.Execute(g.Content,ready.State,new SetReadyCommand(owner,0,true));Assert.Same(ready.State,same.State);Assert.False(same.Changed);
        var withdraw=CommandProcessor.Execute(g.Content,ready.State,new SetReadyCommand(owner,0,false));Assert.True(withdraw.Accepted);Assert.False(withdraw.State.Empires[0].Ready);Assert.Equal(token,withdraw.State.Empires[0].Scouting!.Receipts[0].Token);
    }
    [Fact] public void AcknowledgementAloneDoesNotSatisfyCargoResponse()
    {
        var g=MultiWarning();var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.Scout=null;g=Acknowledge(ScoutingFixtures.Valid(g));var before=g.State;
        var r=CommandProcessor.Execute(g.Content,before,new SetReadyCommand(before.Empires[0].Id,0,true));Assert.False(r.Accepted);Assert.Same(before,r.State);Assert.Equal("ameba_response_required",r.Rejection!.Code);
    }
    [Fact] public void PreparationIsIdempotentAndRejectsInjectedCommandsAtomically()
    {
        var g=Acknowledge(MultiWarning());var owner=g.State.Empires[0].Id;var p=ScoutTurnCoordinator.Prepare(g.Content,g.State,owner);Assert.True(p.Succeeded);Assert.Equal(Fixtures.HomeStar,p.State.Vessels[0].PendingMove);var again=ScoutTurnCoordinator.Prepare(g.Content,p.State,owner);Assert.True(again.Succeeded);Assert.Same(p.State,again.State);
        var bad=ScoutTurnCoordinator.Prepare(g.Content,g.State,owner,(ctx,ai)=>new(ImmutableArray.Create<Command>(new SetMoveOrderCommand(owner,0,g.State.Vessels[0].Id,new(14))),ImmutableArray<ScoutOrderEvent>.Empty));Assert.False(bad.Succeeded);Assert.Same(g.State,bad.State);
        var throws=ScoutTurnCoordinator.Prepare(g.Content,g.State,owner,(_,_)=>throw new OverflowException());Assert.False(throws.Succeeded);Assert.Same(g.State,throws.State);
    }
    [Fact] public void FinalHumanFailurePreservesPreviouslyAcceptedHumanPlanning()
    {
        var g=Acknowledge(MultiWarning());var owner=g.State.Empires[0].Id;var first=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(owner,0,true));Assert.True(first.Accepted);var state=first.State;state.Colonies.Single(c=>c.EmpireId==state.Empires[1].Id).ProductionReserve=long.MaxValue;string before=ScoutingFixtures.Bytes(state);
        var failed=CommandProcessor.Execute(g.Content,state,new SetReadyCommand(state.Empires[1].Id,0,true));Assert.False(failed.Accepted);Assert.Same(state,failed.State);Assert.Equal(before,ScoutingFixtures.Bytes(failed.State));Assert.True(failed.State.Empires[0].Ready);
    }
    [Fact] public void ManualUnknownRouteWinsAndRiskSuppressesOnlyThisBoundaryRetreat()
    {
        var g=MultiWarning();var owner=g.State.Empires[0].Id;var v=g.State.Vessels[0];var move=CommandProcessor.Execute(g.Content,g.State,new SetMoveOrderCommand(owner,0,v.Id,new(14)));Assert.True(move.Accepted);g=Acknowledge(g with{State=move.State});
        var p=ScoutTurnCoordinator.Prepare(g.Content,g.State,owner);Assert.True(p.Succeeded);Assert.Equal(new StarId(14),p.State.Vessels[0].PendingMove);Assert.Empty(p.Plan.Commands);
        g=MultiWarning();var risk=CommandProcessor.Execute(g.Content,g.State,new SetAmebaRiskOrderCommand(owner,0,v.Id,1,1,AmebaRiskAction.Study));Assert.True(risk.Accepted);g=Acknowledge(g with{State=risk.State});p=ScoutTurnCoordinator.Prepare(g.Content,g.State,owner);Assert.True(p.Succeeded);Assert.Null(p.State.Vessels[0].PendingMove);Assert.Empty(p.Plan.Commands);
    }
    [Fact] public void ClosedCargoPortBlocksActualDepartureWithoutSpendingWarning()
    {
        var g=MultiWarning();var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.Scout=null;v.PendingMove=Fixtures.HomeStar;g.State.Colonies[0].Crisis!.Quarantined=true;g=Acknowledge(ScoutingFixtures.Valid(g));
        string before=ScoutingFixtures.Bytes(g.State);var r=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,0,true));Assert.False(r.Accepted);Assert.Same(g.State,r.State);Assert.Equal(before,ScoutingFixtures.Bytes(r.State));Assert.Equal(AmebaStage.Warning,r.State.Vessels[0].AmebaEncounter!.Stage);Assert.Equal(0,r.State.Empires[0].Scouting!.RiskRollCount);
    }
    [Fact] public void ScoutAcknowledgementDoesNotAcknowledgePlague()
    {
        var g=ScoutingFixtures.AtAmeba();var warning=CrisisFixtures.Warning();g.State.Turn=20;g.State.Vessels[0].AmebaEncounter!.ArrivalTurn=20;g.State.Empires[0].Scouting!.KnownHabitats[0]=new(1,new(8),20);g.State.Empires[0].Crisis=warning.State.Empires[0].Crisis!.Clone();g.State.Colonies[0].Crisis=warning.State.Colonies[0].Crisis!.Clone();OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(g.State,20);g=Acknowledge(ScoutingFixtures.Valid(g));
        var r=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,20,true));Assert.False(r.Accepted);Assert.Same(g.State,r.State);Assert.Equal("crisis_acknowledgement_required",r.Rejection!.Code);
    }
}
