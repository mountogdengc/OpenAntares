using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetReadyTests
{
    // Explicit labelled multi-controller supplement; never generated-campaign evidence.
    internal static Saves.LoadedGame Multi()
    {
        var old=ScoutingFixtures.TwoHumansAndAi();var c=FleetContentTests.Core();var s=old.State;s.RulesVersion=8;
        FleetInitializer.Initialize(c,s);foreach(var col in s.Colonies){col.Workforce=new(col.Population,0,0);col.ProductionReserve=0;col.ProjectId=null;col.ProjectKind=ProductionKinds.Building;col.Development!.Origin=ProductionOrigin.None;col.Development.ActiveQueueToken=null;}
        ObservationUpdater.Refresh(s,s.Turn);return FleetFixtures.Valid(new(c,s));
    }
    internal static Saves.LoadedGame Explore(Saves.LoadedGame g)
    {
        var ship=g.State.Vessels.First(v=>v.EmpireId==g.State.Empires[0].Id);
        var create=FleetCommandTests.Run(g,new FleetAction.Create("Pathfinders",ImmutableArray.Create(ship.Id)));Assert.True(create.Accepted,create.Rejection?.Message);g=new(g.Content,create.State);
        var ordered=FleetCommandTests.Run(g,new FleetAction.SetOrder(ImmutableArray.Create(new FleetId(1)),new(FleetOrderKind.Explore)));Assert.True(ordered.Accepted,ordered.Rejection?.Message);return new(g.Content,ordered.State);
    }
    [Fact]
    public void PreparesEachHumanOnceAcrossReadyRetryAndDirectResolve()
    {
        var g=Explore(Multi());g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,1));var initial=g.State;var owner=initial.Empires[0].Id;var second=initial.Empires[1].Id;var ship=initial.Vessels.First(v=>v.EmpireId==owner).Id;
        var first=CommandProcessor.Execute(g.Content,initial,new SetReadyCommand(owner,0,true));Assert.True(first.Accepted,first.Rejection?.Message);Assert.Equal(0,first.State.Turn);Assert.NotNull(first.State.FindVessel(ship)!.PendingMove);Assert.Equal(1,first.State.Colonies[0].Development!.ActiveQueueToken);
        string prepared=FleetFixtures.Bytes(first.State);int next=first.State.Colonies[0].Development!.NextQueueToken;
        var same=CommandProcessor.Execute(g.Content,first.State,new SetReadyCommand(owner,0,true));Assert.True(same.Accepted);Assert.Same(first.State,same.State);
        var withdrawn=CommandProcessor.Execute(g.Content,first.State,new SetReadyCommand(owner,0,false));Assert.True(withdrawn.Accepted);var retry=CommandProcessor.Execute(g.Content,withdrawn.State,new SetReadyCommand(owner,0,true));Assert.True(retry.Accepted,retry.Rejection?.Message);Assert.Equal(prepared,FleetFixtures.Bytes(retry.State));Assert.Equal(next,retry.State.Colonies[0].Development!.NextQueueToken);
        var resolved=CommandProcessor.Execute(g.Content,retry.State,new SetReadyCommand(second,0,true));Assert.True(resolved.Accepted,resolved.Rejection?.Message);Assert.Equal(1,resolved.State.Turn);
        var direct=TurnResolver.Resolve(g.Content,initial);Assert.True(direct.Succeeded,string.Join(";",direct.Errors));Assert.Equal(FleetFixtures.Bytes(resolved.State),FleetFixtures.Bytes(direct.State));Assert.Equal(FleetFixtures.Bytes(initial),FleetFixtures.Bytes(g.State));
    }
    [Fact]
    public void ExploreUsesOwnKnowledgeFiveTurnResurveyAndStableRanking()
    {
        var g=Explore(FleetFixtures.Game());var owner=g.State.Empires[0].Id;var fleet=g.State.Empires[0].Fleets!.Fleets[0];var ship=g.State.FindVessel(fleet.Flagship)!;
        var view=EmpireViewProjector.Project(g.Content,g.State,owner);var expected=view.Stars.Where(s=>s.LastObservedTurn is null).OrderBy(s=>FleetTravel.Duration(g.Content,g.State,owner,fleet.Id,s.Chart.Id)).ThenBy(s=>s.Chart.Id).First().Chart.Id;
        var plan=FleetPlanner.Plan(FleetPlanningQueries.Create(g.Content,view));Assert.Equal(expected,Assert.Single(plan.Moves).Destination);
        var candidates=view.Stars.Where(s=>s.Chart.Id!=ship.StationaryStarId).OrderBy(s=>s.Chart.Id).Take(2).ToArray();
        view=view with{Turn=6,Stars=view.Stars.Select(s=>s with{LastObservedTurn=s.Chart.Id==candidates[0].Chart.Id?1:s.Chart.Id==candidates[1].Chart.Id?2:6}).ToImmutableArray()};
        plan=FleetPlanner.Plan(FleetPlanningQueries.Create(g.Content,view));Assert.Equal(candidates[0].Chart.Id,Assert.Single(plan.Moves).Destination);
        var fresh=view with{Stars=view.Stars.Select(s=>s with{LastObservedTurn=2}).ToImmutableArray()};plan=FleetPlanner.Plan(FleetPlanningQueries.Create(g.Content,fresh));Assert.Empty(plan.Moves);Assert.Equal(FleetOrderKind.Hold,Assert.Single(plan.Orders).Order.Kind);
        string before=FleetFixtures.Bytes(g.State);for(int i=0;i<100;i++)FleetPlanner.Plan(FleetPlanningQueries.Create(g.Content,view));Assert.Equal(before,FleetFixtures.Bytes(g.State));
    }
    [Fact]
    public void SafetyOverridesGoalsWithoutConsent()
    {
        var g=Multi();var owner=g.State.Empires[0].Id;var original=g.State.Vessels.First(v=>v.EmpireId==owner);var extra=original.Clone();extra.Id=new(g.State.AllocateId());g.State.Vessels.Add(extra);
        var created=FleetCommandTests.Run(g,new FleetAction.Create("Cohort",ImmutableArray.Create(original.Id,extra.Id)));Assert.True(created.Accepted,created.Rejection?.Message);g=new(g.Content,created.State);
        var officer=g.State.Empires[0].Fleets!.Officers[0];var board=FleetOfficerTests.Run(g,new OfficerAction.AssignFlagship(officer.Id,new(1)));Assert.True(board.Accepted);g=new(g.Content,board.State);var s=g.State;var e=s.Empires[0];var habitat=s.Scouting!.Habitat!;
        int number=0;foreach(var v in s.Vessels.Where(v=>v.EmpireId==owner)){v.StationaryStarId=habitat.Star;v.AmebaEncounter=new(){Number=++number,HabitatId=habitat.Id,ArrivalTurn=0,Stage=AmebaStage.Warning};}e.Scouting!.NextEncounterNumber=number+1;e.Scouting.KnownHabitats.Add(new(habitat.Id,habitat.Star,0));ObservationUpdater.Refresh(s,0);
        var remain=CommandProcessor.Execute(g.Content,s,new SetAmebaRiskOrderCommand(owner,0,extra.Id,s.FindVessel(extra.Id)!.AmebaEncounter!.Number,1,AmebaRiskAction.Remain));Assert.True(remain.Accepted,remain.Rejection?.Message);s=remain.State;
        var unread=CommandProcessor.Execute(g.Content,s,new SetReadyCommand(owner,0,true));Assert.False(unread.Accepted);Assert.Same(s,unread.State);
        var attention=ScoutQueries.Project(g.Content,s,owner).Attention;var ack=CommandProcessor.Execute(g.Content,s,new AcknowledgeAmebaCommand(owner,0,attention));Assert.True(ack.Accepted,ack.Rejection?.Message);string acknowledged=FleetFixtures.Bytes(ack.State);
        var ready=CommandProcessor.Execute(g.Content,ack.State,new SetReadyCommand(owner,0,true));Assert.True(ready.Accepted,ready.Rejection?.Message);var fleet=Assert.Single(ready.State.Empires[0].Fleets!.Fleets);Assert.Single(fleet.Members);Assert.Equal(original.Id,fleet.Flagship);Assert.Null(ready.State.FindVessel(extra.Id)!.Fleet);Assert.NotNull(ready.State.FindVessel(original.Id)!.PendingMove);Assert.Equal(AmebaRiskAction.Remain,ready.State.FindVessel(extra.Id)!.AmebaEncounter!.RiskOrder!.Action);Assert.True(attention.SequenceEqual(ScoutQueries.Project(g.Content,ready.State,owner).Attention));Assert.Equal(0,ready.State.Empires[0].Fleets!.RecoveryRollCount);Assert.Equal(2,ready.State.Empires[0].Fleets!.NextFleetId);Assert.Equal(acknowledged,FleetFixtures.Bytes(ack.State));
        string normalized=FleetFixtures.Bytes(ready.State);var withdraw=CommandProcessor.Execute(g.Content,ready.State,new SetReadyCommand(owner,0,false));Assert.True(withdraw.Accepted);var retry=CommandProcessor.Execute(g.Content,withdraw.State,new SetReadyCommand(owner,0,true));Assert.True(retry.Accepted,retry.Rejection?.Message);Assert.Equal(normalized,FleetFixtures.Bytes(retry.State));
    }
    [Fact]
    public void LateSecondHumanFailurePreservesFirstHumansPreparedOrders()
    {
        var g=Explore(Multi());var second=g.State.Colonies.First(c=>c.EmpireId==g.State.Empires[1].Id);second.Workforce=new(second.Population-1,1,0);second.ProductionReserve=long.MaxValue;
        string before=FleetFixtures.Bytes(g.State);var direct=TurnResolver.Resolve(g.Content,g.State);Assert.False(direct.Succeeded);Assert.Same(g.State,direct.State);Assert.Null(direct.Report);Assert.Equal(before,FleetFixtures.Bytes(g.State));
        var first=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,0,true));Assert.True(first.Accepted,first.Rejection?.Message);string prepared=FleetFixtures.Bytes(first.State);
        var failure=CommandProcessor.Execute(g.Content,first.State,new SetReadyCommand(g.State.Empires[1].Id,0,true));Assert.False(failure.Accepted);Assert.Same(first.State,failure.State);Assert.Equal(prepared,FleetFixtures.Bytes(first.State));Assert.NotNull(first.State.Vessels.First(v=>v.EmpireId==g.State.Empires[0].Id).PendingMove);
    }
    [Fact]
    public void AutomaticContainmentIsVisibleBeforeEncounterConsent()
    {
        var g=Multi();var s=g.State;var e=s.Empires[0];var col=s.Colonies.First(c=>c.EmpireId==e.Id);var location=CrisisFixtures.Location(s,col);var d=g.Content.Plagues.Values.Single();
        e.Crisis!.Authority=Crises.CrisisAuthority.Contain;e.Crisis.NextEpisodeNumber=2;e.Crisis.Episode=new(){Id=1,FamilyId=d.Id,Source=location,SourcePopulation=col.Population,CureCost=d.CureBase+d.CurePerPopulation*col.Population};
        col.Crisis!.Case=new(){EpisodeId=1,Stage=Crises.CrisisStage.Active,Severity=20,AttentionRevision=2,HandledRevision=2,Origin=location};
        var ship=s.Vessels.First(v=>v.EmpireId==e.Id);ship.RoleId="harl_settler";ship.CarriedPopulation=1;ship.Scout=null;ship.StationaryStarId=s.Scouting!.Habitat!.Star;ship.PendingMove=location.Star;ship.AmebaEncounter=new(){Number=1,HabitatId=1,ArrivalTurn=0,Stage=AmebaStage.Warning};e.Scouting!.NextEncounterNumber=2;e.Scouting.KnownHabitats.Add(new(1,ship.StationaryStarId.Value,0));ObservationUpdater.Refresh(s,0);FleetFixtures.Valid(g);
        string before=FleetFixtures.Bytes(s);var row=ScoutQueries.Project(g.Content,s,e.Id).Vessels.Single(v=>v.Id==ship.Id);Assert.Null(row.EffectiveDestination);Assert.NotNull(row.Blocker);Assert.False(col.Crisis.Quarantined);
        for(int i=0;i<25;i++)ScoutQueries.Project(g.Content,s,e.Id);Assert.Equal(before,FleetFixtures.Bytes(s));
        var alternate=s.Stars.First(star=>star.Id!=location.Star&&star.Id!=ship.StationaryStarId).Id;var changed=CommandProcessor.Execute(g.Content,s,new SetMoveOrderCommand(e.Id,0,ship.Id,alternate));Assert.True(changed.Accepted,changed.Rejection?.Message);
        var tokens=ScoutQueries.Project(g.Content,changed.State,e.Id).Attention;var ack=CommandProcessor.Execute(g.Content,changed.State,new AcknowledgeAmebaCommand(e.Id,0,tokens));Assert.True(ack.Accepted,ack.Rejection?.Message);
        var ready=CommandProcessor.Execute(g.Content,ack.State,new SetReadyCommand(e.Id,0,true));Assert.True(ready.Accepted,ready.Rejection?.Message);Assert.True(ready.State.FindColony(col.Id)!.Crisis!.Quarantined);Assert.Equal(alternate,ready.State.FindVessel(ship.Id)!.PendingMove);Assert.True(tokens.SequenceEqual(ScoutQueries.Project(g.Content,ready.State,e.Id).Attention));Assert.Equal(before,FleetFixtures.Bytes(s));
    }
    [Fact]
    public void AutomaticContainmentLimitFailureRollsBack()
    {
        var g=Multi();var e=g.State.Empires[0];e.Crisis!.Authority=Crises.CrisisAuthority.Contain;var col=g.State.Colonies.First(c=>c.EmpireId==e.Id);col.Workforce=new(col.Population-1,1,0);col.ProductionReserve=long.MaxValue;
        string before=FleetFixtures.Bytes(g.State);var result=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(e.Id,0,true));Assert.False(result.Accepted);Assert.Same(g.State,result.State);Assert.Null(result.Report);Assert.Equal(before,FleetFixtures.Bytes(g.State));
    }
    [Fact]
    public void RallyDepartureNeedsExplicitPauseAndManualOrdersWin()
    {
        var g=FleetFixtures.Game();var ship=g.State.Vessels[0];var made=FleetCommandTests.Run(g,new FleetAction.Create("Rally",ImmutableArray.Create(ship.Id)));Assert.True(made.Accepted);g=new(g.Content,made.State);
        g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,1) with{RallyFleet=new(1)});var target=g.State.Stars.First(star=>star.Id!=ship.StationaryStarId).Id;
        var action=new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(new FleetId(1)),target);var denied=FleetCommandTests.Run(g,action);Assert.False(denied.Accepted);Assert.Same(g.State,denied.State);
        var order=new FleetAction.SetOrder(ImmutableArray.Create(new FleetId(1)),new(FleetOrderKind.Explore));Assert.False(FleetCommandTests.Run(g,order).Accepted);
        var r=g.State.Empires[0].Fleets!.Requests[0];var paused=FleetRequestTests.Run(g,new AssemblyAction.Pause(r.Id,r.Revision));Assert.True(paused.Accepted);g=new(g.Content,paused.State);
        var moved=FleetCommandTests.Run(g,action);Assert.True(moved.Accepted,moved.Rejection?.Message);g=new(g.Content,moved.State);var prepared=FleetTurnCoordinator.PrepareOrders(g.Content,g.State,g.State.Empires[0].Id);Assert.True(prepared.Succeeded);Assert.Equal(target,prepared.State.FindVessel(ship.Id)!.PendingMove);Assert.Equal(FleetOrderKind.Hold,prepared.State.Empires[0].Fleets!.Fleets[0].Order.Kind);
        Assert.False(FleetCommandTests.Run(g,new FleetAction.SetOrder(ImmutableArray.Create(new FleetId(1)),new(FleetOrderKind.Return,target))).Accepted);
    }
    [Fact]
    public void SafetyPausedRequestDoesNotCreateAReadyBlock()
    {
        var g=Multi();var e=g.State.Empires[0];var habitat=g.State.Scouting!.Habitat!;g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,1) with{Rally=habitat.Star});g.State.Empires[0].Scouting!.KnownHabitats.Add(new(habitat.Id,habitat.Star,0));
        Assert.True(g.State.Colonies[0].Development!.ConstructionEnabled); // Existing labelled governor retains its ordinary authority.
        var ready=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(e.Id,0,true));Assert.True(ready.Accepted,ready.Rejection?.Message);var r=ready.State.Empires[0].Fleets!.Requests[0];Assert.Equal(RequestStatus.SafetyPaused,r.Status);Assert.Equal(SlotStatus.BlockedSafety,r.Slots[0].Status);Assert.Equal(ProductionKinds.Building,ready.State.Colonies[0].ProjectKind);Assert.Equal(ProductionOrigin.Template,ready.State.Colonies[0].Development!.Origin);Assert.Equal(1,r.Slots[0].QueueToken);Assert.Equal(0,ready.State.Turn);
    }
    [Fact]
    public void AcceptedIdleHullDispatchesOnceWithinItsFiniteBinding()
    {
        var g=Multi();var e=g.State.Empires[0];var ship=g.State.Vessels.First(v=>v.EmpireId==e.Id);var target=g.State.Stars.First(s=>s.Id!=ship.StationaryStarId&&s.Id!=g.State.Scouting!.Habitat!.Star).Id;
        g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,1) with{Rally=target,AcceptedIdleVessels=ImmutableArray.Create(ship.Id)});
        var ready=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(e.Id,0,true));Assert.True(ready.Accepted,ready.Rejection?.Message);Assert.Equal(target,ready.State.FindVessel(ship.Id)!.PendingMove);Assert.Null(ready.State.FindVessel(ship.Id)!.Fleet);Assert.Equal(1,ready.State.Empires[0].Fleets!.NextFleetId);Assert.Equal(ship.Id,ready.State.Empires[0].Fleets!.Requests[0].Slots[0].Vessel);Assert.Empty(ready.State.Colonies[0].Development!.Queue);
        var withdrawn=CommandProcessor.Execute(g.Content,ready.State,new SetReadyCommand(e.Id,0,false));var repeated=CommandProcessor.Execute(g.Content,withdrawn.State,new SetReadyCommand(e.Id,0,true));Assert.True(repeated.Accepted,repeated.Rejection?.Message);Assert.Equal(FleetFixtures.Bytes(ready.State),FleetFixtures.Bytes(repeated.State));
    }
    [Fact]
    public void DirectResolveCannotBypassExistingCrisisAcknowledgement()
    {
        var g=Multi();var e=g.State.Empires[0];var col=g.State.Colonies.First(c=>c.EmpireId==e.Id);var location=CrisisFixtures.Location(g.State,col);var d=g.Content.Plagues.Values.Single();
        e.Crisis!.NextEpisodeNumber=2;e.Crisis.Episode=new(){Id=1,FamilyId=d.Id,Source=location,SourcePopulation=col.Population,CureCost=d.CureBase+d.CurePerPopulation*col.Population};col.Crisis!.Case=new(){EpisodeId=1,Stage=Crises.CrisisStage.Active,Severity=20,AttentionRevision=2,Origin=location};FleetFixtures.Valid(g);
        string before=FleetFixtures.Bytes(g.State);var direct=TurnResolver.Resolve(g.Content,g.State);Assert.False(direct.Succeeded);Assert.Same(g.State,direct.State);Assert.Null(direct.Report);Assert.Equal(before,FleetFixtures.Bytes(g.State));
    }
}
