using System.Linq;
using System;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictAiTests
{
    internal static GameSession WarRival()
    {
        var g=ConflictOrderTests.LocalForces();var b=g.State.Empires[1];b.Controller=ControllerKind.Ai;b.AiPolicyId="expansion_v1";var c=g.State.Colonies.Single(c=>c.EmpireId==b.Id);c.ProjectId=null;c.Development!.ConstructionEnabled=false;return g;
    }
    [Fact] public void MilitarySlotsArePaidFiniteAndDoNotPreemptExpansion()
    {
        var g=WarRival();var b=g.State.Empires[1].Id;var col=g.State.Colonies.Single(c=>c.EmpireId==b);long bank=col.ProductionReserve;int hulls=g.State.Vessels.Count;var result=RivalTurnPlanner.Prepare(g.Content,g.State);Assert.True(result.Succeeded);var planned=result.State.FindColony(col.Id)!;
        Assert.Equal(ProductionKinds.ShipDesign,planned.ProjectKind);Assert.NotNull(planned.ProjectId);Assert.Equal(bank,planned.ProductionReserve);Assert.Equal(hulls,result.State.Vessels.Count);Assert.Equal(4,g.Content.Conflict!.WarHullTarget);
    }
    [Fact] public void NewlyPaidHullsCanJoinAtMostTwoOrdinaryGroups()
    {
        var g=WarRival();var b=g.State.Empires[1].Id;var star=g.State.Vessels.First(v=>v.EmpireId==b&&v.Design is not null).StationaryStarId!.Value;for(int i=0;i<5;i++)ConflictFixtures.AddEscort(g,b,star).NewlyCompleted=true;
        var result=RivalTurnPlanner.Prepare(g.Content,g.State);Assert.True(result.Succeeded);var military=result.State.FindEmpire(b)!.Fleets!.Fleets.Where(f=>f.Members.All(id=>result.State.FindVessel(id)!.Design is not null)).ToArray();Assert.Equal(2,military.Length);Assert.All(military,f=>Assert.InRange(f.Members.Count,1,4));Assert.Equal(6,military.Sum(f=>f.Members.Count));Assert.All(military,f=>Assert.Null(f.Officer));
    }
    [Fact] public void ProductionDoesNotOverrideAnActiveProjectOrQueue()
    {
        var g=WarRival();var view=g.View(g.State.Empires[1].Id);var col=view.Colonies[0];view=view with{Colonies=ImmutableArray.Create(col with{ProjectKind=ProductionKinds.Building,ProjectId="fabrication_hall"})};Assert.Empty(ConflictPlanner.Plan(g.Content,view,ConflictPlanningStage.Production));
    }
    [Theory][InlineData("harl")][InlineData("wenth")][InlineData("sulan")][InlineData("peldra")]
    public void PlanningReplaysWithoutRandomDrawsOrForeignBankKnowledge(string species)
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create(species));string before=JsonSerializer.Serialize(g.State.Random);var a=RivalTurnPlanner.Prepare(g.Content,g.State);var b=RivalTurnPlanner.Prepare(g.Content,g.State.Clone());Assert.True(a.Succeeded);Assert.True(b.Succeeded);Assert.Equal(JsonSerializer.Serialize(a.State),JsonSerializer.Serialize(b.State));Assert.Equal(before,JsonSerializer.Serialize(a.State.Random));
        var ai=g.State.Empires.Single(e=>e.Controller==ControllerKind.Ai).Id;var original=ConflictPlanner.Plan(g.Content,g.View(ai),ConflictPlanningStage.Production);g.State.Colonies.First(c=>c.EmpireId!=ai).ProductionReserve=987654321;Assert.Equal(JsonSerializer.Serialize(original),JsonSerializer.Serialize(ConflictPlanner.Plan(g.Content,g.View(ai),ConflictPlanningStage.Production)));
    }
    [Fact] public void LowHullReturnsToRepairBeforeOffensiveOrders()
    {
        var g=WarRival();var b=g.State.Empires[1].Id;var ship=g.State.Vessels.First(v=>v.EmpireId==b&&v.Design is not null);ship.Combat!.Hull=74;var commands=ConflictPlanner.Plan(g.Content,g.View(b),ConflictPlanningStage.Orders);Assert.Contains(commands,c=>c is CombatActionCommand {Action:CombatAction.SetRepair});Assert.DoesNotContain(commands,c=>c is ConflictCommand {Action:ConflictAction.Attack or ConflictAction.Blockade});
    }
    [Fact] public void PeaceOffersAreAlwaysAcceptedAndDeadlineProducesOwnOffer()
    {
        var g=WarRival();g.State.Turn=15;var b=g.State.Empires[1].Id;var a=g.State.Empires[0].Id;var pair=g.State.Conflict!.Pairs.Single();var commands=ConflictPlanner.Plan(g.Content,g.View(b),ConflictPlanningStage.Responses);Assert.Contains(commands,c=>c is ConflictCommand {Action:ConflictAction.ProposePeace});pair.PendingPeace=new(){Id=new(1),Proposer=a,CreatedTurn=15,ExpiryTurn=18};pair.NextPeaceOfferId=2;
        Assert.Contains(ConflictPlanner.Plan(g.Content,g.View(b),ConflictPlanningStage.Responses),c=>c is ConflictCommand {Action:ConflictAction.AnswerPeace {Accept:true}});
    }
    [Fact] public void DeclarationNeedsTwoHealthyHullsAndCurrentFiveBoundarySharedClaim()
    {
        var g=WarRival();var b=g.State.Empires[1].Id;var view=g.View(b);var relation=view.Conflict!.Relations[0] with{Relation=RelationKind.Peace,WarTurn=null,OwnClaimObservedSince=0};var other=relation.Other;
        view=view with{Turn=5,Contacts=view.Contacts.Select(c=>c with{FirstObservedTurn=0}).ToImmutableArray(),Conflict=view.Conflict with{Turn=5,Relations=ImmutableArray.Create(relation)},Diplomacy=view.Diplomacy! with{Contacts=view.Diplomacy!.Contacts.Select(c=>c with{OwnAttitude=c.OwnAttitude with{Score=0}}).ToImmutableArray()}};
        Assert.Empty(ConflictPlanner.Plan(g.Content,view,ConflictPlanningStage.Declarations));
    }
    [Fact] public void EffectiveOwnBlockadeTriggersPeaceAfterFiveWarBoundaries()
    {
        var g=WarRival();var b=g.State.Empires[1].Id;g.State.Turn=5;var view=g.View(b);view=view with{Conflict=view.Conflict! with{OwnPressure=ImmutableArray.Create(new OwnBlockadePressure(view.Colonies[0].StarId,g.State.Empires[0].Id,50))}};
        Assert.Contains(ConflictPlanner.Plan(g.Content,view,ConflictPlanningStage.Responses),c=>c is ConflictCommand {Action:ConflictAction.ProposePeace});
    }
    [Fact] public void EqualObservedStrengthQualifiesAndLessOwnHullDoesNot()
    {
        var g=WarRival();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var star=g.State.FindPlanet(g.State.Colonies.First(c=>c.EmpireId==a).PlanetId)!.StarId;
        foreach(var v in g.State.Vessels.Where(v=>v.Design is not null))v.StationaryStarId=star;var opposing=ConflictFixtures.AddEscort(g,a,star);var opposingFleet=g.State.FindEmpire(a)!.Fleets!.Fleets.First(f=>f.Members.Any(id=>g.State.FindVessel(id)!.Design is not null));opposingFleet.Members.Add(opposing.Id);opposing.Fleet=opposingFleet.Id;var extra=ConflictFixtures.AddEscort(g,b,star);var members=g.State.Vessels.Where(v=>v.EmpireId==b&&v.Design is not null).Select(v=>v.Id).ToImmutableArray();
        ObservationUpdater.Refresh(g.State,g.State.Turn);ConflictObservations.Refresh(g.Content,g.State);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));var grouped=CommandProcessor.ExecuteConflictPreparation(g.Content,g.State,new FleetCommand(b,g.State.Turn,new FleetAction.Create("Equality",members)));Assert.True(grouped.Accepted,grouped.Rejection?.Message);g=new(g.Content,grouped.State);ConflictObservations.Refresh(g.Content,g.State);
        var commands=ConflictPlanner.Plan(g.Content,g.View(b),ConflictPlanningStage.Orders);Assert.Single(commands.OfType<ConflictCommand>(),c=>c.Action is ConflictAction.Attack);g.State.FindVessel(extra.Id)!.Combat!.Hull=99;
        Assert.DoesNotContain(ConflictPlanner.Plan(g.Content,g.View(b),ConflictPlanningStage.Orders),c=>c is ConflictCommand {Action:ConflictAction.Attack});
    }
    [Fact] public void DeclarationBecomesEligibleOnlyAfterBothPoliticalClocks()
    {
        var g=WarRival();var b=g.State.Empires[1].Id;var ship=g.State.Vessels.First(v=>v.EmpireId==b&&v.Design is not null);var extra=ConflictFixtures.AddEscort(g,b,ship.StationaryStarId!.Value);var grouped=CommandProcessor.ExecuteConflictPreparation(g.Content,g.State,new FleetCommand(b,g.State.Turn,new FleetAction.Create("Eligible",ImmutableArray.Create(ship.Id,extra.Id))));Assert.True(grouped.Accepted);g=new(g.Content,grouped.State);var view=g.View(b);
        view=view with{Turn=5,Conflict=view.Conflict! with{Turn=5,Relations=view.Conflict!.Relations.Select(r=>r with{Relation=RelationKind.Peace,WarTurn=null,OwnClaimObservedSince=0}).ToImmutableArray()},Contacts=view.Contacts.Select(c=>c with{FirstObservedTurn=0}).ToImmutableArray(),Diplomacy=view.Diplomacy! with{Contacts=view.Diplomacy!.Contacts.Select(c=>c with{OwnAttitude=c.OwnAttitude with{Score=0}}).ToImmutableArray()}};
        Assert.Single(ConflictPlanner.Plan(g.Content,view,ConflictPlanningStage.Declarations));Assert.Empty(ConflictPlanner.Plan(g.Content,view with{Turn=4},ConflictPlanningStage.Declarations));Assert.Empty(ConflictPlanner.Plan(g.Content,view with{Conflict=view.Conflict! with{Relations=view.Conflict!.Relations.Select(r=>r with{OwnClaimObservedSince=null}).ToImmutableArray()}},ConflictPlanningStage.Declarations));
    }
    [Fact] public void SoleHealthyHomeDefenseDoesNotLeaveForRemoteBlockade()
    {
        var g=WarRival();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var ship=g.State.Vessels.First(v=>v.EmpireId==b&&v.Design is not null);var grouped=CommandProcessor.ExecuteConflictPreparation(g.Content,g.State,new FleetCommand(b,g.State.Turn,new FleetAction.Create("Home defense",ImmutableArray.Create(ship.Id))));Assert.True(grouped.Accepted);g=new(g.Content,grouped.State);var view=g.View(b);var home=g.View(a).Stars.Single(s=>s.Planets?.Any(p=>p.OwnerId==a)==true);view=view with{Stars=view.Stars.Select(s=>s.Chart.Id==home.Chart.Id?home with{Current=false}:s).ToImmutableArray(),Conflict=view.Conflict! with{Military=ImmutableArray<MilitaryObservation>.Empty}};
        Assert.DoesNotContain(ConflictPlanner.Plan(g.Content,view,ConflictPlanningStage.Orders),c=>c is FleetCommand {Action:FleetAction.MoveSelection});
    }
    [Fact] public void FourHealthyHullFormationCanSplitAnOrdinarySpareDefenseForce()
    {
        var g=WarRival();var b=g.State.Empires[1].Id;var ship=g.State.Vessels.First(v=>v.EmpireId==b&&v.Design is not null);for(int i=0;i<3;i++)ConflictFixtures.AddEscort(g,b,ship.StationaryStarId!.Value);var members=g.State.Vessels.Where(v=>v.EmpireId==b&&v.Design is not null).Select(v=>v.Id).ToImmutableArray();var grouped=CommandProcessor.ExecuteConflictPreparation(g.Content,g.State,new FleetCommand(b,g.State.Turn,new FleetAction.Create("Four",members)));Assert.True(grouped.Accepted);g=new(g.Content,grouped.State);
        Assert.Contains(ConflictPlanner.Plan(g.Content,g.View(b),ConflictPlanningStage.Grouping),c=>c is FleetCommand {Action:FleetAction.Split});
    }
    [Fact] public void AsymmetricContactsAndFullFleetSlotsRemainLocalLegalSkips()
    {
        var g=WarRival();var b=g.State.Empires[1].Id;var third=ConflictFixtures.AddNeutralObserver(g,g.State.Stars[0].Id);var neutral=g.State.FindEmpire(third)!;neutral.Controller=ControllerKind.Ai;neutral.AiPolicyId="limited_conflict_v1";
        var content=g.Content with{FleetCommand=g.Content.FleetCommand! with{FleetLimit=1}};Assert.Empty(GameStateValidator.Validate(content,g.State));int ships=g.State.Vessels.Count;var result=RivalTurnPlanner.Prepare(content,g.State);Assert.True(result.Succeeded);Assert.Equal(ships,result.State.Vessels.Count);Assert.Single(result.State.FindEmpire(b)!.Fleets!.Fleets);Assert.DoesNotContain(result.State.Colonies,c=>c.EmpireId==third);Assert.Equal(ProductionKinds.ShipDesign,result.State.Colonies.Single(c=>c.EmpireId==b).ProjectKind);
    }
}
