using System;
using System.Linq;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatTurnTests
{
    internal static LoadedGame Pending(int hull=100,CombatDoctrine doctrine=CombatDoctrine.Cautious)
    {
        var g=CombatFixtures.Duel(hull);var attack=CombatCommandTests.Attack(g.State);return new(g.Content,CombatCommandTests.Commit(g,attack with {Orders=attack.Orders with {Doctrine=doctrine}}));
    }
    [Fact] public void IntegratedDeathCannotReachMovementLookup()
    {
        var g=Pending(1);var before=CombatFixtures.Bytes(g.State);var outcome=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,0,true));Assert.True(outcome.Accepted,outcome.Rejection?.Message);Assert.Null(outcome.State.FindVessel(g.State.Vessels[0].Id));Assert.Empty(outcome.State.Combat!.Encounters);Assert.Equal(1,outcome.State.Turn);Assert.Equal(76,outcome.State.Combat.Pockets[0].Sentries[0].Hull);Assert.Equal(before,CombatFixtures.Bytes(g.State));
    }
    [Fact] public void DirectResolveAndReadyCommitExactlyOneVolley()
    {
        var g=Pending();var direct=TurnResolver.Resolve(g.Content,g.State);var ready=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,0,true));Assert.True(direct.Succeeded,string.Join(";",direct.Errors));Assert.True(ready.Accepted,ready.Rejection?.Message);Assert.Equal(72,ready.State.Vessels[0].Combat!.Hull);Assert.Equal(1,ready.State.Combat!.Encounters[0].Round);Assert.Equal(1,ready.State.Turn);Assert.Equal(CombatFixtures.Bytes(direct.State),CombatFixtures.Bytes(ready.State));
    }
    [Theory] [InlineData((int)CombatResolutionPhase.Preparation)] [InlineData((int)CombatResolutionPhase.Snapshot)] [InlineData((int)CombatResolutionPhase.Health)] [InlineData((int)CombatResolutionPhase.Exchange)] [InlineData((int)CombatResolutionPhase.LossNormalization)] [InlineData((int)CombatResolutionPhase.Withdrawal)] [InlineData((int)CombatResolutionPhase.Movement)] [InlineData((int)CombatResolutionPhase.Founding)] [InlineData((int)CombatResolutionPhase.Repair)] [InlineData((int)CombatResolutionPhase.Observation)] [InlineData((int)CombatResolutionPhase.OfficerRecovery)] [InlineData((int)CombatResolutionPhase.Reports)] [InlineData((int)CombatResolutionPhase.Validation)]
    public void EveryCombatPhaseFailureRollsBackCompleteOriginal(int rawPhase)
    {
        var phase=(CombatResolutionPhase)rawPhase;
        var g=Pending(1);var officer=g.State.Empires[0].Fleets!.Officers[0];officer.Status=OpenAntares.Simulation.Fleets.OfficerStatus.Commanding;officer.PostedColony=null;officer.Flagship=g.State.Vessels[0].Id;g.State.Empires[0].Fleets!.Fleets[0].Officer=officer.Id;CombatFixtures.Valid(g);string before=CombatFixtures.Bytes(g.State);var failed=TurnResolver.ResolveCombat(g.Content,g.State,p=>{if(p==phase)throw new InvalidOperationException("Injected combat phase failure");});Assert.False(failed.Succeeded);Assert.Same(g.State,failed.State);Assert.Null(failed.Report);Assert.Equal(before,CombatFixtures.Bytes(failed.State));
    }
    [Fact] public void PublishedPocketFactsSupportTheNextExactPreview()
    {
        var g=Pending(doctrine:CombatDoctrine.Hold);var next=TurnResolver.Resolve(g.Content,g.State);Assert.True(next.Succeeded,string.Join(";",next.Errors));var known=Assert.Single(next.State.Empires[0].Combat!.Observations);Assert.Equal(1,known.Turn);Assert.Equal(58,known.Sentries[0].Hull);var e=next.State.Combat!.Encounters[0];var preview=CombatQueries.Preview(g.Content,OpenAntares.Simulation.Knowledge.EmpireViewProjector.Project(g.Content,next.State,next.State.Empires[0].Id),new CombatAction.ChangeCombatOrders(e.Id,e.Revision,e.Orders with {Doctrine=CombatDoctrine.Press},false));Assert.True(preview.Allowed,preview.RejectionCode);
    }
    [Fact] public void PostInterceptionLaunchOverridesAnIndependentlyCapturedStaleFleetLaunch()
    {
        CombatDefinition Configure(CombatDefinition d)=>d with {SentryProfile=new(1000,80,2),SentryPursuitSpeed=300};
        var old=CombatWithdrawalTests.Battle(new[]{75,100},configure:Configure);var prior=old.State.Combat!.Encounters[0];var cancelled=CombatCommandTests.Commit(old,new CombatAction.WithdrawCombat(prior.Id,prior.Revision));var stale=OpenAntares.Simulation.Fleets.FleetResolutionContext.Capture(old.Content,cancelled);
        var current=CombatWithdrawalTests.Queued(CombatWithdrawalTests.Battle(new[]{75,100},configure:Configure),StragglerPolicy.KeepTogether);var (candidate,context)=CombatWithdrawalTests.Depart(current);var departure=Assert.Single(context.Departures).Value;Assert.Equal(150,departure.Speed);Assert.True(departure.TotalTurns>1,"Fixture requires a multi-turn return.");Assert.Equal(300,stale.Launches[departure.Members[0]].EffectiveSpeed);
        OpenAntares.Simulation.Movement.MovementResolver.Apply(current.Content,current.State,candidate,new(),fleetContext:stale,combat:context);var ship=Assert.Single(candidate.Vessels);Assert.Equal(departure.TotalTurns,ship.Transit!.TotalTurns);Assert.Equal(departure.TotalTurns-1,ship.Transit.RemainingTurns);Assert.Equal(150,candidate.Empires[0].Fleets!.Fleets[0].Cohort!.EffectiveSpeed);
    }
    [Fact] public void HitAndRunWaitsOneBoundaryThenUsesCombatLaunchWithoutSnapshotMove()
    {
        var g=Pending(doctrine:CombatDoctrine.HitAndRun);var first=TurnResolver.Resolve(g.Content,g.State);Assert.True(first.Succeeded,string.Join(";",first.Errors));var ship=first.State.Vessels[0];Assert.Null(ship.PendingMove);Assert.Null(ship.Transit);Assert.Equal(CombatPhase.WithdrawalQueued,first.State.Combat!.Encounters[0].Phase);
        var second=TurnResolver.Resolve(g.Content,first.State);Assert.True(second.Succeeded,string.Join(";",second.Errors));Assert.Empty(second.State.Combat!.Encounters);var returned=Assert.Single(second.State.Vessels);var home=second.State.FindPlanet(second.State.Colonies[0].PlanetId)!.StarId;Assert.True(returned.Transit?.Destination==home||returned.StationaryStarId==home);Assert.Equal(72,returned.Combat!.Hull);
    }
}
