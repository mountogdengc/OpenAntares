using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictAftermathTests
{
    internal static (GameState Candidate,ConflictResolutionContext Context,CombatResolutionContext Combat) Boundary(GameSession g)
    {
        var snapshot=g.State;var candidate=snapshot.Clone();var combat=CombatResolutionContext.Capture(g.Content,snapshot);var context=ConflictResolutionContext.Capture(g.Content,snapshot,combat.FleetContext);
        ConflictRelations.Prepare(g.Content,snapshot,candidate,context);ConflictMatching.Resolve(g.Content,snapshot,candidate,context);ConflictAftermath.InterruptRefits(g.Content,snapshot,candidate,context);ConflictWithdrawal.Resolve(g.Content,snapshot,candidate,context);ConflictExchange.Apply(g.Content,snapshot,candidate,context);ConflictAftermath.Apply(g.Content,snapshot,candidate,combat.FleetContext,context);return(candidate,context,combat);
    }
    internal static GameSession CompletingRefit()
    {
        var g=ConflictOrderTests.LocalForces();var attack=ConflictOrderTests.Attack(g);var target=g.State.FindVessel(attack.Target)!;var b=target.EmpireId;ConflictWithdrawalTests.Upgrade(g,b);target.Design=new(1);var port=g.State.Colonies.First(c=>c.EmpireId==b);port.ProductionReserve=5000;
        port.Development!.Queue.Add(new(1,new(ProductionKinds.Building,g.Content.Buildings.Values.First(x=>x.PrerequisiteTechnologyIds.IsEmpty).Id)));port.Development.NextQueueToken=2;
        var action=new RefitAction.Start(target.Id,port.Id,new(2));var preview=RefitQueries.Preview(g.Content,g.View(b),action);Assert.True(preview.Allowed,preview.RejectionCode);var result=g.Execute(new RefitCommand(b,g.State.Turn,action,preview.Token));Assert.True(result.Accepted,result.Rejection?.Message);ConflictObservations.Refresh(g.Content,g.State);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));return g;
    }
    [Fact] public void AttackedCompletingRefitKeepsReserveAndOldEquipment()
    {
        var g=CompletingRefit();var a=g.State.Empires[0].Id;var attack=ConflictOrderTests.Attack(g);ConflictDeclarationTests.Issue(g,a,attack);var snapshot=g.State;var target=snapshot.FindVessel(attack.Target)!;var port=snapshot.Colonies.First(c=>c.EmpireId==target.EmpireId);long bank=port.ProductionReserve;
        var economy=EconomyCalculator.Calculate(g.Content,snapshot);Assert.True(economy.Colony(port.Id).RefitPlan!.Covered);var refits=RefitResolutionContext.Capture(g.Content,snapshot);var fleet=FleetResolutionContext.Capture(g.Content,snapshot);var context=ConflictResolutionContext.Capture(g.Content,snapshot,fleet);var candidate=snapshot.Clone();foreach(var row in economy.Colonies)candidate.FindColony(row.Colony)!.ProductionReserve=row.Production.Ending;
        ConflictRelations.Prepare(g.Content,snapshot,candidate,context);ConflictMatching.Resolve(g.Content,snapshot,candidate,context);ConflictAftermath.InterruptRefits(g.Content,snapshot,candidate,context);Assert.Empty(candidate.FindEmpire(target.EmpireId)!.ShipDesigns!.Refits);Assert.Equal(new DesignId(1),candidate.FindVessel(target.Id)!.Design);Assert.Null(candidate.FindColony(port.Id)!.ProjectId);Assert.Single(candidate.FindColony(port.Id)!.Development!.Queue);
        ConflictWithdrawal.Resolve(g.Content,snapshot,candidate,context);ConflictExchange.Apply(g.Content,snapshot,candidate,context);ConflictAftermath.Apply(g.Content,snapshot,candidate,fleet,context);var applied=RefitResolver.Apply(g.Content,snapshot,candidate,refits,economy,context);
        Assert.Equal(bank+economy.Colony(port.Id).NetProduction.Total,candidate.FindColony(port.Id)!.ProductionReserve);Assert.Equal(0,Assert.Single(applied.Events).Paid);Assert.Equal(new DesignId(1),candidate.FindVessel(target.Id)!.Design);Assert.False(applied.Economy.Colony(port.Id).RefitPlan!.Covered);Assert.Null(applied.Economy.Colony(port.Id).Production.CompletedId);Assert.Equal(80,candidate.FindVessel(target.Id)!.Combat!.Hull);Assert.False(candidate.FindColony(port.Id)!.Development!.ConstructionEnabled);
    }
    [Fact] public void SurvivingReturnCohortLaunchesOnceAtPostDamageSpeedWithoutArrivalHealing()
    {
        var g=ConflictFixtures.WithEncounter();ConflictWithdrawalTests.AtNeutralStar(g);var e=g.State.Conflict!.Encounters[0];e.LowSide.WithdrawQueued=true;e.LowSide.Orders=ConflictOrders.Default with{ReturnColony=g.State.Colonies.First(c=>c.EmpireId==e.LowSide.Owner).Id};var ship=e.LowSide.Members[0];g.State.FindVessel(ship)!.Combat!.Hull=60;
        var result=Boundary(g);var route=Assert.Single(result.Context.Departures);var events=new System.Collections.Generic.List<Turns.ExpansionEvent>();MovementResolver.Apply(g.Content,g.State,result.Candidate,events,fleetContext:result.Combat.FleetContext,combat:result.Combat,conflict:result.Context);
        var after=result.Candidate.FindVessel(ship)!;Assert.Equal(40,after.Combat!.Hull);Assert.True(after.Transit is not null||after.StationaryStarId==route.Destination);if(after.Transit is {} transit){Assert.Equal(route.TotalTurns,transit.TotalTurns);Assert.Equal(route.TotalTurns-1,transit.RemainingTurns);}CombatRepair.Apply(g.Content,g.State,result.Candidate,result.Combat,result.Context);CombatRepair.ObserveArrivals(result.Candidate,result.Combat);Assert.Equal(40,after.Combat.Hull);
        Assert.Throws<InvalidOperationException>(()=>MovementResolver.Apply(g.Content,g.State,result.Candidate,events,combat:result.Combat,conflict:result.Context));
    }
    [Fact] public void LocalBreakNeverCreatesTransitOrARepairTick()
    {
        var g=ConflictFixtures.WithEncounter();g.State.Conflict!.Encounters[0].LowSide.WithdrawQueued=true;var id=g.State.Conflict.Encounters[0].LowSide.Members[0];int allocator=g.State.NextEntityId;var result=Boundary(g);var events=new System.Collections.Generic.List<Turns.ExpansionEvent>();MovementResolver.Apply(g.Content,g.State,result.Candidate,events,combat:result.Combat,conflict:result.Context);CombatRepair.Apply(g.Content,g.State,result.Candidate,result.Combat,result.Context);
        Assert.Null(result.Candidate.FindVessel(id)!.Transit);Assert.Equal(g.State.FindVessel(id)!.StationaryStarId,result.Candidate.FindVessel(id)!.StationaryStarId);Assert.Equal(80,result.Candidate.FindVessel(id)!.Combat!.Hull);Assert.Equal(allocator,result.Candidate.NextEntityId);Assert.Empty(events);
    }
    [Fact] public void MatchedRepairingDefenderGetsNoHealAlongsideItsVolley()
    {
        var g=ConflictOrderTests.LocalForces();var a=g.State.Empires[0].Id;var attack=ConflictOrderTests.Attack(g);var target=g.State.FindVessel(attack.Target)!;target.Combat!.Hull=60;target.Combat.Cohesion=80;target.Combat.Repairing=true;target.Combat.ReturnColony=g.State.Colonies.First(c=>c.EmpireId==target.EmpireId).Id;target.Combat.OnArrival=CombatArrivalOrder.Repair;ConflictObservations.Refresh(g.Content,g.State);ConflictDeclarationTests.Issue(g,a,attack);
        var result=Boundary(g);CombatRepair.Apply(g.Content,g.State,result.Candidate,result.Combat,result.Context);Assert.Equal(40,result.Candidate.FindVessel(target.Id)!.Combat!.Hull);Assert.False(result.Candidate.FindVessel(target.Id)!.Combat!.Repairing);
    }
    [Fact] public void MissingUnrelatedJobStillThrowsWithoutContextProof()
    {
        var g=CompletingRefit();var snapshot=g.State;var economy=EconomyCalculator.Calculate(g.Content,snapshot);var context=RefitResolutionContext.Capture(g.Content,snapshot);var candidate=snapshot.Clone();candidate.Empires[1].ShipDesigns!.Refits.Clear();var conflict=ConflictResolutionContext.Capture(g.Content,snapshot);
        Assert.Throws<InvalidOperationException>(()=>RefitResolver.Apply(g.Content,snapshot,candidate,context,economy,conflict));Assert.Single(g.State.Empires[1].ShipDesigns!.Refits);
    }
    [Theory][InlineData("harl",false,1)][InlineData("harl",true,0)][InlineData("wenth",false,0)][InlineData("wenth",true,0)]
    public void DesignatedFlagshipLossUsesOnlyTheExistingRecoveryStream(string species,bool lastWatch,long draws)
    {
        var g=ConflictFixtures.WithEncounter(species);var encounter=g.State.Conflict!.Encounters[0];g.State.Conflict.Encounters.Clear();var a=encounter.LowSide.Owner;var data=g.State.FindEmpire(a)!.Fleets!;var officer=data.Officers[0];if(lastWatch)officer.Doctrine=OfficerDoctrine.LastWatch;
        var assign=g.Execute(new OfficerCommand(a,g.State.Turn,new OfficerAction.AssignFlagship(officer.Id,encounter.LowSide.Fleet!.Value)));Assert.True(assign.Accepted,assign.Rejection?.Message);g.State.Conflict!.Encounters.Add(encounter);g.State.FindVessel(encounter.LowSide.Flagship)!.Combat!.Hull=1;ConflictObservations.Refresh(g.Content,g.State);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        var before=g.State.FindEmpire(a)!.Fleets!.Officers[0].Clone();long rolls=g.State.FindEmpire(a)!.Fleets!.RecoveryRollCount;var result=Boundary(g);Assert.Single(result.Context.Destructions);Assert.Single(result.Combat.FleetContext.Losses);Assert.Throws<InvalidOperationException>(()=>ConflictAftermath.Apply(g.Content,g.State,result.Candidate,result.Combat.FleetContext,result.Context));
        result.Candidate.Turn++;OfficerProgression.Apply(g.Content,g.State,result.Candidate,result.Combat.FleetContext,null);OfficerRecovery.Apply(g.Content,g.State,result.Candidate,result.Combat.FleetContext);OfficerRecovery.Apply(g.Content,g.State,result.Candidate,result.Combat.FleetContext);var after=result.Candidate.FindEmpire(a)!.Fleets!.Officers[0];Assert.Equal(rolls+draws,result.Candidate.FindEmpire(a)!.Fleets!.RecoveryRollCount);Assert.Equal(before.Id,after.Id);Assert.Equal(before.Name,after.Name);Assert.Equal(before.Xp,after.Xp);Assert.Equal(1,after.Career.FlagshipsLost);Assert.Equal(1,after.Career.ShipsLost);
        if(species=="wenth"){Assert.Equal(before.SourceBody,after.SourceBody);Assert.Equal(OfficerStatus.Recovering,after.Status);}else if(lastWatch)Assert.Equal(OfficerStatus.Dead,after.Status);
        ObservationUpdater.Refresh(result.Candidate,result.Candidate.Turn);ConflictObservations.Refresh(g.Content,result.Candidate);FleetReports.Finalize(g.Content,result.Candidate,result.Combat.FleetContext);Assert.Empty(GameStateValidator.Validate(g.Content,result.Candidate));
    }
    [Fact] public void RefitOnUnrelatedShipSurvivesIfTheSelectedAnchorVanishes()
    {
        var g=CompletingRefit();var a=g.State.Empires[0].Id;var attack=ConflictOrderTests.Attack(g);var target=g.State.FindVessel(attack.Target)!;var extra=ConflictFixtures.AddEscort(g,target.EmpireId,target.StationaryStarId!.Value);var job=g.State.FindEmpire(target.EmpireId)!.ShipDesigns!.Refits[0];job.Vessel=extra.Id;ConflictObservations.Refresh(g.Content,g.State);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));ConflictDeclarationTests.Issue(g,a,attack);
        var candidate=g.State.Clone();candidate.Vessels.Remove(candidate.FindVessel(target.Id)!);var context=ConflictResolutionContext.Capture(g.Content,g.State);ConflictRelations.Prepare(g.Content,g.State,candidate,context);ConflictMatching.Resolve(g.Content,g.State,candidate,context);ConflictAftermath.InterruptRefits(g.Content,g.State,candidate,context);Assert.Empty(context.Battles);Assert.Single(candidate.FindEmpire(extra.EmpireId)!.ShipDesigns!.Refits);Assert.Empty(context.InterruptedRefits);
    }
    [Fact] public void InterruptedRefitStillRejectsAFrozenRecipeChangedByAFault()
    {
        var g=CompletingRefit();ConflictDeclarationTests.Issue(g,g.State.Empires[0].Id,ConflictOrderTests.Attack(g));var snapshot=g.State;var economy=EconomyCalculator.Calculate(g.Content,snapshot);var refits=RefitResolutionContext.Capture(g.Content,snapshot);var result=Boundary(g);foreach(var row in economy.Colonies)result.Candidate.FindColony(row.Colony)!.ProductionReserve=row.Production.Ending;
        var data=result.Candidate.Empires[1].ShipDesigns!;data.Designs[1]=data.Designs[1] with{Recipe=data.Designs[1].Recipe with{ArmorId=null}};Assert.Throws<InvalidOperationException>(()=>RefitResolver.Apply(g.Content,snapshot,result.Candidate,refits,economy,result.Context));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void ProtectedCivilianFoundingRemainsAnOrdinaryFiniteAction(bool returning)
    {
        var g=ConflictFixtures.WithMixedDefense();var e=g.State.Conflict!.Encounters[0];var b=e.HighSide.Owner;var civilian=e.HighSide.ProtectedCivilians[0];var template=g.State.Colonies.First(c=>c.EmpireId==b);var home=g.State.FindPlanet(template.PlanetId)!;var planet=g.State.Planets.First(p=>p.StarId==e.Star&&g.State.Colonies.All(c=>c.PlanetId!=p.Id));planet.PlanetTypeId=home.PlanetTypeId;planet.EnvironmentId=home.EnvironmentId;
        if(returning)
        {
            var from=g.State.FindStar(e.Star)!;var far=g.State.Stars.OrderByDescending(s=>(long)(s.X-from.X)*(s.X-from.X)+(long)(s.Y-from.Y)*(s.Y-from.Y)).First();var p=g.State.Planets.First(p=>p.StarId==far.Id&&g.State.Colonies.All(c=>c.PlanetId!=p.Id));p.PlanetTypeId=home.PlanetTypeId;p.EnvironmentId=home.EnvironmentId;var port=template.Clone();port.Id=new(g.State.AllocateId());port.PlanetId=p.Id;g.State.Colonies.Add(port);e.HighSide.WithdrawQueued=true;e.HighSide.Orders=ConflictOrders.Default with{ReturnColony=port.Id};
        }
        ObservationUpdater.Refresh(g.State,g.State.Turn);ConflictObservations.Refresh(g.Content,g.State);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));var found=g.Execute(new SetFoundingOrderCommand(b,g.State.Turn,civilian,planet.Id));Assert.True(found.Accepted,found.Rejection?.Message);
        var result=Boundary(g);var events=new System.Collections.Generic.List<Turns.ExpansionEvent>();MovementResolver.Apply(g.Content,g.State,result.Candidate,events,combat:result.Combat,conflict:result.Context);Colonization.ColonizationResolver.Apply(g.Content,g.State,result.Candidate,events);FleetResolver.AfterFounding(result.Candidate,result.Combat.FleetContext,events,result.Context);MovementResolver.CompleteConflictFounding(g.Content,g.State,result.Candidate,events,result.Context);
        Assert.Null(result.Candidate.FindVessel(civilian));Assert.Contains(result.Candidate.Colonies,c=>c.EmpireId==b&&c.PlanetId==planet.Id);Assert.DoesNotContain(civilian,result.Context.Destructions.Select(d=>d.Vessel));result.Candidate.Turn++;ObservationUpdater.Refresh(result.Candidate,result.Candidate.Turn);ConflictObservations.Refresh(g.Content,result.Candidate);Assert.Empty(GameStateValidator.Validate(g.Content,result.Candidate));
    }
    [Fact] public void FrozenClosedReturnPortCannotReopenEarlyInsideTheBoundary()
    {
        var g=ConflictFixtures.WithEncounter();ConflictWithdrawalTests.AtNeutralStar(g);var e=g.State.Conflict!.Encounters[0];e.LowSide.WithdrawQueued=true;var port=g.State.Colonies.First(c=>c.EmpireId==e.LowSide.Owner);e.LowSide.Orders=ConflictOrders.Default with{ReturnColony=port.Id};g.State.Crisis!.Enabled=true;port.Crisis!.Quarantined=true;
        var candidate=g.State.Clone();candidate.FindColony(port.Id)!.Crisis!.Quarantined=false;var context=ConflictResolutionContext.Capture(g.Content,g.State);ConflictRelations.Prepare(g.Content,g.State,candidate,context);ConflictMatching.Resolve(g.Content,g.State,candidate,context);ConflictAftermath.InterruptRefits(g.Content,g.State,candidate,context);ConflictWithdrawal.Resolve(g.Content,g.State,candidate,context);Assert.Null(Assert.Single(context.Departures).Destination);
    }
    [Fact] public void OneIntervalArrivalDoesNotHealButNextStationaryIntervalDoes()
    {
        var original=ConflictFixtures.WithEncounter();var definition=original.Content.ShipDesigns!;var engine=definition.Components[definition.StartingRecipe.EngineId];var content=original.Content with{ShipDesigns=definition with{Components=definition.Components.SetItem(engine.Id,engine with{Value=100000})}};var g=new GameSession(content,original.State);ConflictWithdrawalTests.AtNeutralStar(g);var e=g.State.Conflict!.Encounters[0];e.LowSide.WithdrawQueued=true;e.LowSide.Orders=ConflictOrders.Default with{ReturnColony=g.State.Colonies.First(c=>c.EmpireId==e.LowSide.Owner).Id};var id=e.LowSide.Members[0];g.State.FindVessel(id)!.Combat!.Hull=60;
        var result=Boundary(g);Assert.Equal(1,Assert.Single(result.Context.Departures).TotalTurns);var events=new System.Collections.Generic.List<Turns.ExpansionEvent>();MovementResolver.Apply(content,g.State,result.Candidate,events,combat:result.Combat,conflict:result.Context);CombatRepair.Apply(content,g.State,result.Candidate,result.Combat,result.Context);CombatRepair.ObserveArrivals(result.Candidate,result.Combat);Assert.Null(result.Candidate.FindVessel(id)!.Transit);Assert.Equal(40,result.Candidate.FindVessel(id)!.Combat!.Hull);Assert.True(result.Candidate.FindVessel(id)!.Combat!.Repairing);
        result.Candidate.Turn++;ObservationUpdater.Refresh(result.Candidate,result.Candidate.Turn);ConflictObservations.Refresh(content,result.Candidate);g=new(content,result.Candidate);Assert.Empty(GameStateValidator.Validate(content,g.State));var next=Boundary(g);CombatRepair.Apply(content,g.State,next.Candidate,next.Combat,next.Context);Assert.Equal(65,next.Candidate.FindVessel(id)!.Combat!.Hull);
    }
    [Fact] public void ProtectedCivilianFlagshipAndOfficerSurviveLossOfTheirLastEscort()
    {
        var g=ConflictFixtures.WithMixedDefense();var e=g.State.Conflict!.Encounters[0];var data=g.State.FindEmpire(e.HighSide.Owner)!.Fleets!;foreach(var f in data.Fleets)f.Officer=null;var officer=data.Officers[0];officer.Status=OfficerStatus.Commanding;officer.Flagship=e.HighSide.Flagship;officer.PostedColony=null;officer.Request=null;data.Fleets.Single(f=>f.Id==e.HighSide.Fleet).Officer=officer.Id;g.State.FindVessel(e.HighSide.Members[0])!.Combat!.Hull=1;ConflictObservations.Refresh(g.Content,g.State);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        int cargo=g.State.FindVessel(e.HighSide.Flagship)!.CarriedPopulation;var result=Boundary(g);Assert.Empty(result.Combat.FleetContext.Losses);result.Candidate.Turn++;OfficerProgression.Apply(g.Content,g.State,result.Candidate,result.Combat.FleetContext,null);OfficerRecovery.Apply(g.Content,g.State,result.Candidate,result.Combat.FleetContext);Assert.Equal(OfficerStatus.Commanding,result.Candidate.FindEmpire(e.HighSide.Owner)!.Fleets!.Officers[0].Status);Assert.Equal(0,result.Candidate.FindEmpire(e.HighSide.Owner)!.Fleets!.RecoveryRollCount);Assert.Equal(cargo,result.Candidate.FindVessel(e.HighSide.Flagship)!.CarriedPopulation);
    }
}
