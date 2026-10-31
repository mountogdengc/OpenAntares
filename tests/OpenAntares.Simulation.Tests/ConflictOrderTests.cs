using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictOrderTests
{
    internal static GameSession LocalForces(bool mixed=false)
    {
        var g=mixed?ConflictFixtures.WithMixedDefense():ConflictFixtures.WithEncounter();var e=g.State.Conflict!.Encounters[0];var battle=g.State.FindPlanet(g.State.Colonies.First(c=>c.EmpireId==e.HighSide.Owner).PlanetId)!.StarId;
        foreach(var id in e.LowSide.Members.Concat(e.HighSide.Members).Concat(e.HighSide.ProtectedCivilians))g.State.FindVessel(id)!.StationaryStarId=battle;
        g.State.Conflict.Encounters.Clear();ObservationUpdater.Refresh(g.State,g.State.Turn);ConflictObservations.Refresh(g.Content,g.State);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));return g;
    }
    internal static ConflictAction.Attack Attack(GameSession g)
    {
        var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;return new(g.State.FindEmpire(a)!.Fleets!.Fleets[0].Id,g.State.Vessels.First(v=>v.EmpireId==b&&v.Design is not null).Id,ConflictOrders.Default with{ReturnColony=g.State.Colonies.First(c=>c.EmpireId==a).Id});
    }
    [Fact] public void ObservedAnchorCanBeAttackedButProtectedCargoCannot()
    {
        var g=LocalForces(true);var a=g.State.Empires[0].Id;var action=Attack(g);var civilian=g.State.FindEmpire(g.State.Empires[1].Id)!.Fleets!.Fleets[0].Flagship;
        Assert.True(ConflictQueries.Preview(g.Content,g.View(a),action).Allowed);Assert.False(ConflictQueries.Preview(g.Content,g.View(a),action with{Target=civilian}).Allowed);
        string bytes=ConflictFixtures.Canonical(g.State);var illegal=action with{Target=civilian};var result=g.Execute(new ConflictCommand(a,g.State.Turn,illegal,ConflictTokens.For(g.Content,g.View(a),illegal)));Assert.False(result.Accepted);Assert.Equal(bytes,ConflictFixtures.Canonical(g.State));
        var own=g.State.Conflict!.Owners.Single(o=>o.Owner==a);ConflictDeclarationTests.Issue(g,a,action);Assert.Single(g.State.Conflict!.Owners.Single(o=>o.Owner==a).Attacks);Assert.Empty(g.State.Conflict.Encounters);Assert.Empty(own.Attacks);
    }
    [Theory][InlineData("new")][InlineData("repair")][InlineData("move")][InlineData("civilian")][InlineData("stale")][InlineData("return")]
    public void IneligibleOwnFleetOrObservationRejectsWithoutMutation(string reason)
    {
        var g=LocalForces();var a=g.State.Empires[0].Id;var action=Attack(g);var ship=g.State.FindVessel(g.State.FindEmpire(a)!.Fleets!.Fleets[0].Members[0])!;
        switch(reason){case "new":ship.NewlyCompleted=true;break;case "repair":ship.Combat!.Repairing=true;break;case "move":ship.PendingMove=g.State.FindPlanet(g.State.Colonies.First(c=>c.EmpireId==a).PlanetId)!.StarId;break;case "civilian":ship.CarriedPopulation=1;break;case "stale":g.State.Conflict!.Owners.Single(o=>o.Owner==a).Observations.Clear();break;case "return":action=action with{Orders=ConflictOrders.Default};break;}
        string bytes=ConflictFixtures.Canonical(g.State);Assert.False(ConflictQueries.Preview(g.Content,g.View(a),action).Allowed);var result=g.Execute(new ConflictCommand(a,g.State.Turn,action,ConflictTokens.For(g.Content,g.View(a),action)));Assert.False(result.Accepted);Assert.Equal(bytes,ConflictFixtures.Canonical(g.State));
    }
    [Fact] public void UngroupedDefenderCanConfigureLocalBreakWithoutRevealingIt()
    {
        var g=LocalForces();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var ship=g.State.Vessels.First(v=>v.EmpireId==b&&v.Design is not null);var action=Attack(g);string token=ConflictTokens.For(g.Content,g.View(a),action);
        Assert.Null(g.View(b).Conflict!.Ships.Single(s=>s.Id==ship.Id).Defense.ReturnColony);ConflictDeclarationTests.Issue(g,b,new ConflictAction.ConfigureDefense(ship.Id,ConflictOrders.Default with{Doctrine=CombatDoctrine.Press}));Assert.Equal(token,ConflictTokens.For(g.Content,g.View(a),action));Assert.Equal(CombatDoctrine.Press,g.View(b).Conflict!.Ships.Single(s=>s.Id==ship.Id).Defense.Doctrine);
    }
    [Fact] public void PrivateEncounterOrdersChangeOnlyOwnRevisionAndReadiness()
    {
        var g=ConflictFixtures.WithEncounter();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var e=g.View(a).Conflict!.Encounters[0];var foreign=new ConflictAction.Withdraw(e.Id,g.View(b).Conflict!.Encounters[0].Revision);string token=ConflictTokens.For(g.Content,g.View(b),foreign);
        ConflictDeclarationTests.Issue(g,a,new ConflictAction.ChangeOrders(e.Id,e.Revision,ConflictOrders.Default with{Doctrine=CombatDoctrine.Hold}));Assert.Equal(token,ConflictTokens.For(g.Content,g.View(b),foreign));var updated=g.View(a).Conflict!.Encounters[0];Assert.Equal(CombatDoctrine.Hold,updated.OwnOrders.Doctrine);ConflictDeclarationTests.Issue(g,a,new ConflictAction.Withdraw(updated.Id,updated.Revision));Assert.True(g.View(a).Conflict!.Encounters[0].OwnWithdrawQueued);
    }
    [Fact] public void AcceptedAttackAndActiveEncounterRejectMovementGroupingAndRepair()
    {
        foreach(bool active in new[]{false,true})
        {
            var g=active?ConflictFixtures.WithEncounter():LocalForces();var a=g.State.Empires[0].Id;var fleet=g.State.FindEmpire(a)!.Fleets!.Fleets[0];if(!active)ConflictDeclarationTests.Issue(g,a,Attack(g));
            var move=new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(fleet.Id),g.State.FindPlanet(g.State.Colonies.First(c=>c.EmpireId==a).PlanetId)!.StarId);
            string before=ConflictFixtures.Canonical(g.State);Assert.False(FleetQueries.PreviewAction(g.Content,g.View(a),move).Allowed);var result=g.Execute(new FleetCommand(a,g.State.Turn,move));Assert.False(result.Accepted);Assert.Equal(before,ConflictFixtures.Canonical(g.State));
            Assert.False(CombatQueries.Preview(g.Content,g.View(a),new CombatAction.SetRepair(fleet.Members.ToImmutableArray(),true)).Allowed);
            Assert.False(FleetQueries.PreviewAction(g.Content,g.View(a),new FleetAction.Disband(fleet.Id)).Allowed);
        }
    }
    [Fact] public void BlockadeIsFutureDatedAndMovementExplicitlyLiftsIt()
    {
        var g=LocalForces();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var fleet=g.State.FindEmpire(a)!.Fleets!.Fleets[0];ConflictDeclarationTests.Issue(g,a,new ConflictAction.Blockade(fleet.Id,b));
        Assert.Equal((long)g.State.Turn+1,g.View(a).Conflict!.Blockades[0].EffectiveTurn);Assert.Equal("Pending",g.View(a).Conflict!.Blockades[0].Status);
        var move=new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(fleet.Id),g.State.FindPlanet(g.State.Colonies.First(c=>c.EmpireId==a).PlanetId)!.StarId);var result=g.Execute(new FleetCommand(a,g.State.Turn,move));Assert.True(result.Accepted,result.Rejection?.Message);Assert.Empty(g.State.Conflict!.Owners.Single(o=>o.Owner==a).Blockades);Assert.Contains(g.State.Conflict.Owners.Single(o=>o.Owner==a).Events,e=>e.Kind=="blockade_lifted");
    }
    [Fact] public void OwnPendingAttackCanBeCancelledWithoutAllocatingOrWarningTarget()
    {
        var g=LocalForces();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var action=Attack(g);int id=g.State.NextEntityId;long revision=g.State.Conflict!.Owners.Single(o=>o.Owner==b).Revision;
        ConflictDeclarationTests.Issue(g,a,action);Assert.False(ConflictDeclarationTests.Issue(g,a,action).Changed);ConflictDeclarationTests.Issue(g,a,new ConflictAction.CancelAttack(action.Fleet));Assert.Empty(g.State.Conflict!.Owners.Single(o=>o.Owner==a).Attacks);Assert.Equal(id,g.State.NextEntityId);Assert.Equal(revision,g.State.Conflict.Owners.Single(o=>o.Owner==b).Revision);
    }
    [Fact] public void ArmedRoleCanHaveAnAlternateContentId()
    {
        var original=LocalForces();var old=original.Content.ShipDesigns!.WarshipRoleId;const string replacement="test_guardian";
        var role=original.Content.Vessels[old] with{Id=replacement};var content=original.Content with{Vessels=original.Content.Vessels.Remove(old).Add(replacement,role),ShipDesigns=original.Content.ShipDesigns with{WarshipRoleId=replacement},Combat=original.Content.Combat! with{RoleProfiles=original.Content.Combat.RoleProfiles.Remove(old).Add(replacement,original.Content.Combat.RoleProfiles[old])}};
        foreach(var vessel in original.State.Vessels.Where(v=>v.RoleId==old))vessel.RoleId=replacement;var g=new GameSession(content,original.State);ConflictObservations.Refresh(content,g.State);Assert.True(ConflictQueries.Preview(content,g.View(g.State.Empires[0].Id),Attack(g)).Allowed);
    }
    [Fact] public void DecliningOwnRevisedForceKeepsWithdrawalAsAnOffRamp()
    {
        var g=ConflictFixtures.WithEncounter();var a=g.State.Empires[0].Id;g.State.Conflict!.Encounters[0].LowSide.NeedsResponse=true;var e=g.View(a).Conflict!.Encounters[0];ConflictDeclarationTests.Issue(g,a,new ConflictAction.Withdraw(e.Id,e.Revision));Assert.False(g.View(a).Conflict!.Encounters[0].OwnNeedsResponse);Assert.True(g.View(a).Conflict!.Encounters[0].OwnWithdrawQueued);
    }
}
