using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatQueryTests
{
    [Fact] public void ExactShotBandCohesionEscapeRepairAndOfficerForecastArePure()
    {
        var g=CombatFixtures.Convoy();var owner=g.State.Empires[0];var f=owner.Fleets!.Fleets[0];var officer=owner.Fleets.Officers[0];officer.Status=OfficerStatus.Commanding;officer.PostedColony=null;officer.Flagship=f.Flagship;f.Officer=officer.Id;CombatFixtures.Valid(g);string before=CombatFixtures.Bytes(g.State);
        var action=CombatCommandTests.Attack(g.State,true);var p=CombatQueries.Preview(g.Content,EmpireViewProjector.Project(g.Content,g.State,owner.Id),action);Assert.True(p.Allowed);Assert.Equal(3,p.Exchange!.Shots.Length);Assert.Equal(72,p.Exchange.Ships[0].After);Assert.Equal(CombatDamageBand.Operational,p.Forecasts[0].After);Assert.Equal(100,p.Forecasts[0].CohesionAfter);Assert.Equal(1,p.Cargo);
        var json=JsonSerializer.SerializeToElement(p);Assert.True(json.TryGetProperty("OfficerRisk",out var risk));Assert.Equal(50,risk.GetProperty("RecoveryPercent").GetInt32());Assert.Equal(2,risk.GetProperty("Delay").GetInt32());Assert.True(json.TryGetProperty("RepairForecasts",out var repairs));Assert.Equal(2,repairs[0].GetProperty("Intervals").GetInt32());Assert.Equal(before,CombatFixtures.Bytes(g.State));
        var queued=CombatWithdrawalTests.Queued(CombatWithdrawalTests.Battle(new[]{100,25},navigator:1),StragglerPolicy.DetachCrippled);var e=queued.State.Combat!.Encounters[0];var withdrawal=CombatQueries.Preview(queued.Content,EmpireViewProjector.Project(queued.Content,queued.State,queued.State.Empires[0].Id),new CombatAction.WithdrawCombat(e.Id,e.Revision));Assert.True(withdrawal.Allowed);Assert.Single(withdrawal.Exposed);Assert.Equal(queued.State.Vessels[1].Id,withdrawal.Exposed[0]);Assert.Equal(11,withdrawal.Exchange!.Ships[1].After);Assert.Equal(100,withdrawal.Forecasts[0].CohesionAfter);var repair=JsonSerializer.SerializeToElement(withdrawal).GetProperty("RepairForecasts");Assert.Equal(4,repair[1].GetProperty("Intervals").GetInt32());
    }
    [Fact] public void OwnShipStatusTotalsAndRepairIntervalsAreMaterialAndImmutable()
    {
        var g=CombatFixtures.Game(enabled:false);var v=g.State.Vessels[0];v.RoleId="escort";CombatInitializer.InitializeVessel(g.Content,9,v);v.Combat!.Hull=46;v.Combat.Cohesion=30;v.Combat.HullLost=54;v.Combat.ReturnColony=g.State.Colonies[0].Id;v.Combat.OnArrival=CombatArrivalOrder.Repair;v.Combat.Repairing=true;g.State.Empires[0].Combat!.LifetimeHullLost=54;CombatFixtures.Valid(g);string before=CombatFixtures.Bytes(g.State);
        var view=EmpireViewProjector.Project(g.Content,g.State,v.EmpireId);var json=JsonSerializer.SerializeToElement(view.Combat);Assert.True(json.TryGetProperty("Totals",out var totals));Assert.Equal(54,totals.GetProperty("HullLost").GetInt64());var ship=json.GetProperty("Ships")[0];Assert.Equal(100,ship.GetProperty("MaxHull").GetInt32());Assert.Equal(225,ship.GetProperty("EffectiveSpeed").GetInt32());Assert.Equal(4,ship.GetProperty("RepairIntervals").GetInt32());Assert.NotEmpty(json.GetProperty("Notices").EnumerateArray());Assert.Equal(before,CombatFixtures.Bytes(g.State));
        var repairPreview=CombatQueries.Preview(g.Content,view,new CombatAction.SetRepair(ImmutableArray.Create(v.Id),true));Assert.True(repairPreview.Allowed);Assert.Single(repairPreview.RepairForecasts);Assert.Equal(4,repairPreview.RepairForecasts[0].Intervals);
        var next=CombatSaveTests.Next(g);Assert.Equal(46,view.Combat!.Ships[0].Hull);Assert.Equal(71,next.State.Vessels[0].Combat!.Hull);Assert.Equal(25,next.State.Empires[0].Combat!.LifetimeHullRepaired);
    }
    [Fact] public void RoutineValidBattleNeedsNoRepeatedModalOrMutation()
    {
        var g=CombatSaveTests.Next(CombatTurnTests.Pending(doctrine:CombatDoctrine.Hold));string before=CombatFixtures.Bytes(g.State);var owner=g.State.Empires[0].Id;
        for(int i=0;i<3;i++){Assert.Empty(CombatReadyQueries.ValidateResponse(g.Content,g.State,owner));var view=EmpireViewProjector.Project(g.Content,g.State,owner);Assert.Empty(view.Combat!.Responses);var material=JsonSerializer.SerializeToElement(view.Combat);Assert.True(material.TryGetProperty("Notices",out var notices));Assert.DoesNotContain(notices.EnumerateArray(),n=>n.GetProperty("Level").GetInt32()==2);}
        Assert.Equal(before,CombatFixtures.Bytes(g.State));
    }
}
