using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class GovernorTransactionTests
{
    [Fact] public void NonfinalReadyPublishesOnlyItsOwnOrders() {
        var g=ColonyFixtures.TwoHumansAndAi();Assert.Empty(GameStateValidator.Validate(g.Content,g.State));var e=g.State.Empires[0];string original=JsonSerializer.Serialize(g.State);
        var r=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(e.Id,0,true));Assert.True(r.Accepted,r.Rejection?.Message);Assert.Null(r.Report);Assert.Equal(0,r.State.Turn);Assert.True(r.State.FindEmpire(e.Id)!.Ready);
        Assert.Equal(new Workforce(2,3,1),r.State.Colonies[0].Workforce);Assert.Equal("production_complex",r.State.Colonies[0].ProjectId);Assert.Equal(g.State.Colonies[1].Workforce,r.State.Colonies[1].Workforce);Assert.Null(r.State.Colonies[2].ProjectId);Assert.Equal(original,JsonSerializer.Serialize(g.State));Assert.Single(r.State.Empires[0].Development!.PendingReport!.Entries);
    }
    [Fact] public void LastHumanFailureRestoresExactPreReadyState() {
        var g=ColonyFixtures.TwoHumansAndAi();var first=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,0,true));Assert.True(first.Accepted);var before=first.State;before.Colonies[2].ProductionReserve=long.MaxValue;string bytes=JsonSerializer.Serialize(before);
        var r=CommandProcessor.Execute(g.Content,before,new SetReadyCommand(before.Empires[1].Id,0,true));Assert.False(r.Accepted);Assert.Same(before,r.State);Assert.Equal(bytes,JsonSerializer.Serialize(r.State));Assert.True(r.State.Empires[0].Ready);Assert.False(r.State.Empires[1].Ready);Assert.NotNull(r.State.Empires[0].Development!.PendingReport);Assert.Empty(r.State.Empires[1].Development!.CompletedReports);
    }
    [Fact] public void WithdrawAndResubmitDoesNotDuplicateActions() {
        var g=ColonyFixtures.TwoHumansAndAi();var e=g.State.Empires[0].Id;
        var prepared=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(e,0,true));var report=prepared.State.Empires[0].Development!.PendingReport;Assert.NotNull(report);
        var noOp=CommandProcessor.Execute(g.Content,prepared.State,new SetReadyCommand(e,0,true));Assert.Same(prepared.State,noOp.State);
        var withdrawn=CommandProcessor.Execute(g.Content,prepared.State,new SetReadyCommand(e,0,false));var resumed=CommandProcessor.Execute(g.Content,withdrawn.State,new SetReadyCommand(e,0,true));Assert.True(resumed.Accepted);Assert.Equal(JsonSerializer.Serialize(report),JsonSerializer.Serialize(resumed.State.Empires[0].Development!.PendingReport));
    }
    [Fact] public void ManualInterventionWinsAfterPreparation() {
        var g=ColonyFixtures.TwoHumansAndAi();var e=g.State.Empires[0].Id;var c=g.State.Colonies[0].Id;
        var ready=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(e,0,true));var manual=CommandProcessor.Execute(g.Content,ready.State,new SetWorkforceCommand(e,0,c,new(3,1,2)));var submitted=CommandProcessor.Execute(g.Content,manual.State,new SetReadyCommand(e,0,true));Assert.True(submitted.Accepted);Assert.Equal(new Workforce(3,1,2),submitted.State.Colonies[0].Workforce);var row=Assert.Single(submitted.State.Empires[0].Development!.PendingReport!.Entries);Assert.True(row.WorkforceOverridden);Assert.Equal(new Workforce(2,3,1),row.AfterWorkforce);
    }
    [Fact] public void OneCompletionConsumesOnlyBoundQueueEntry() {
        var g=ColonyFixtures.Game();var c=g.State.Colonies[0];string role=g.Content.Expansion!.StartingVesselId;c.ProductionReserve=10000;c.Development!.ConstructionEnabled=true;c.Development.Queue.Add(new(1,new("vessel",role)));c.Development.Queue.Add(new(2,new("vessel",role)));c.Development.NextQueueToken=3;
        var r=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(c.EmpireId,0,true));Assert.True(r.Accepted,r.Rejection?.Message);Assert.Equal(2,r.State.Vessels.Count);Assert.Equal(2,Assert.Single(r.State.Colonies[0].Development!.Queue).Token);Assert.Null(r.State.Colonies[0].ProjectId);Assert.Null(r.State.Colonies[0].Development!.ActiveQueueToken);Assert.Single(r.State.Empires[0].Development!.CompletedReports);
    }
    [Fact] public void NewColonyInheritanceWaitsUntilNextPlanning() {
        var g=ColonyFixtures.Game();var e=g.State.Empires[0];e.Development!.NewColonyPlan=new(ColonyFixtures.Snapshot(g.Content,"industrial"),true,true);string role=g.Content.Species["harl"].AllowedVesselIds.Single(id=>g.Content.Vessels[id].CanFoundColony);
        g.State.Vessels.Add(new(){Id=new(8),EmpireId=e.Id,RoleId=role,StationaryStarId=new(6),CarriedPopulation=g.Content.Vessels[role].EmbarkPopulation});g.State.NextEntityId=9;ObservationUpdater.Refresh(g.State,0);
        var order=CommandProcessor.Execute(g.Content,g.State,new SetFoundingOrderCommand(e.Id,0,new(8),new(7)));Assert.True(order.Accepted,order.Rejection?.Message);
        var r=CommandProcessor.Execute(g.Content,order.State,new SetReadyCommand(e.Id,0,true));Assert.True(r.Accepted,r.Rejection?.Message);var founded=r.State.Colonies.Single(x=>x.PlanetId.Value==7);Assert.NotNull(founded.Development);Assert.True(founded.Development!.ConstructionEnabled);Assert.Null(founded.ProjectId);Assert.Equal(new Workforce(founded.Population,0,0),founded.Workforce);
        var next=CommandProcessor.Execute(g.Content,r.State,new SetReadyCommand(e.Id,1,true));Assert.True(next.Accepted,next.Rejection?.Message);Assert.True(next.State.FindColony(founded.Id)!.ProjectId is not null||next.State.FindColony(founded.Id)!.CompletedBuildingIds.Count>0);
    }
    [Fact] public void BadAndOverBudgetPreparationPublishNothing() {
        var g=ColonyFixtures.TwoHumansAndAi();var e=g.State.Empires[0].Id;string before=JsonSerializer.Serialize(g.State);
        var r=GovernorTurnCoordinator.Prepare(g.Content,g.State,e,ctx=>new(false,ImmutableArray<GovernorColonyOrder>.Empty,"private"));Assert.False(r.Succeeded);Assert.Same(g.State,r.State);
        r=GovernorTurnCoordinator.Prepare(g.Content,g.State,e,ctx=>{var plan=GovernorPlanner.Plan(ctx);return plan with{Orders=plan.Orders.Add(plan.Orders[0])};});Assert.False(r.Succeeded);Assert.Same(g.State,r.State);Assert.Equal(before,JsonSerializer.Serialize(g.State));
    }
    [Fact] public void LateValidationFailureRetainsAcceptedPlanningAndExactRandomState() {
        var g=ColonyFixtures.TwoHumansAndAi();var ready=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,0,true));var snapshot=ready.State;string before=JsonSerializer.Serialize(snapshot);
        var r=OpenAntares.Simulation.Turns.TurnResolver.Resolve(g.Content,snapshot,(content,state)=>{var corrupt=state.Clone();corrupt.NextEntityId=0;return new OpenAntares.Simulation.Rivals.RivalPlanningOutcome(corrupt,true);});
        Assert.False(r.Succeeded);Assert.Same(snapshot,r.State);Assert.Equal(before,JsonSerializer.Serialize(snapshot));Assert.NotNull(snapshot.Empires[0].Development!.PendingReport);
    }
    [Fact] public void ManualBuildingCompletionRemovesSatisfiedQueueWithoutStartingAnother() {
        var g=ColonyFixtures.Game();var c=g.State.Colonies[0];c.ProjectId="fabrication_hall";c.Development!.Origin=ProductionOrigin.Manual;c.Development.Queue.Add(new(1,new("building","fabrication_hall")));c.Development.Queue.Add(new(2,new("building","cultivation_hub")));c.Development.NextQueueToken=3;c.ProductionReserve=10000;
        var r=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(c.EmpireId,0,true));Assert.True(r.Accepted,r.Rejection?.Message);var after=r.State.Colonies[0];Assert.Equal("fabrication_hall",Assert.Single(after.CompletedBuildingIds));Assert.Equal(2,Assert.Single(after.Development!.Queue).Token);Assert.Null(after.ProjectId);Assert.Equal(ProductionOrigin.None,after.Development.Origin);
    }
    [Fact] public void CompletedReportsArePrivateAndRetainLastTen() {
        var g=ColonyFixtures.TwoHumansAndAi();
        for(int i=0;i<12;i++)foreach(var id in g.State.Empires.Where(e=>e.Controller==ControllerKind.Human).Select(e=>e.Id).ToArray()) {
            var r=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(id,g.State.Turn,true));Assert.True(r.Accepted,r.Rejection?.Message);g=g with{State=r.State};
        }
        foreach(var owner in g.State.Empires.Where(e=>e.Controller==ControllerKind.Human)){Assert.Equal(10,owner.Development!.CompletedReports.Count);Assert.Equal(2,owner.Development.CompletedReports[0].Turn);foreach(var entry in owner.Development.CompletedReports.SelectMany(r=>r.Entries))Assert.Equal(owner.Id,g.State.FindColony(entry.Colony)!.EmpireId);}
        Assert.Empty(g.State.Empires.Single(e=>e.Controller==ControllerKind.Ai).Development!.CompletedReports);
    }
    [Fact] public void IdOverflowAndOwnOutputOverflowPublishNothing() {
        foreach(bool ids in new[]{false,true}) {
            var g=ColonyFixtures.Game();var c=g.State.Colonies[0];if(ids){g.State.NextEntityId=int.MaxValue;c.ProjectKind="vessel";c.ProjectId=g.Content.Expansion!.StartingVesselId;c.Development!.Origin=ProductionOrigin.Manual;c.ProductionReserve=10000;}else c.ProductionReserve=long.MaxValue;
            if(!ids)c.Workforce=new(2,3,1);string before=JsonSerializer.Serialize(g.State);var r=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(c.EmpireId,0,true));Assert.False(r.Accepted);Assert.Same(g.State,r.State);Assert.Equal(before,JsonSerializer.Serialize(g.State));
        }
    }
}
