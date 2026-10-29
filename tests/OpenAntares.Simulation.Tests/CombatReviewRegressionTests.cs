using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CombatReviewRegressionTests
{
    public static IEnumerable<object[]> CivilianCases()
    {
        for (int kind = 0; kind < 3; kind++)
        for (int phase = 0; phase < 3; phase++) yield return new object[] { kind, phase };
    }

    [Theory, MemberData(nameof(CivilianCases))]
    public void MissingCivilianAuthorityRejectsSaveReadyAndDirectResolveAtomically(int kind, int phase)
    {
        // Pre-session supplements cover unarmed passengers, an empty unarmed hull, and armed cargo.
        var g = CombatFixtures.Convoy();
        var civilian = g.State.Vessels.Single(v => v.CarriedPopulation > 0);
        if (kind == 1)
        {
            civilian.RoleId = "scout"; civilian.CarriedPopulation = 0;
            OpenAntares.Simulation.Scouting.ScoutInitializer.InitializeVessel(g.Content, 9, civilian);
            CombatInitializer.InitializeVessel(g.Content, 9, civilian);
            ObservationUpdater.Refresh(g.State, g.State.Turn);
        }
        if (kind == 2)
        {
            var d = g.Content.Combat!;
            g = new(g.Content with { Combat = d with { RoleProfiles = d.RoleProfiles.SetItem(civilian.RoleId, d.RoleProfiles[civilian.RoleId] with { Attack = 1 }) } }, g.State);
        }
        CombatFixtures.Valid(g);
        g = new(g.Content, CombatCommandTests.Commit(g, CombatCommandTests.Attack(g.State, true)));
        if (phase > 0) g = CombatSaveTests.Next(g);
        var e = g.State.Combat!.Encounters.Single();
        if (phase == 2) g = new(g.Content, CombatCommandTests.Commit(g, new CombatAction.WithdrawCombat(e.Id, e.Revision)));
        string valid = SaveGame.Serialize(g.Content, g.State);
        var forged = JsonNode.Parse(valid)!;
        forged["state"]!["combat"]!["encounters"]![0]!["civilian_risk_authorized"] = false;
        Assert.False(SaveGame.Deserialize(forged.ToJsonString()).Succeeded);
        Assert.Equal(valid, SaveGame.Serialize(g.Content, g.State));

        g.State.Combat!.Encounters[0].CivilianRiskAuthorized = false;
        string before = CombatFixtures.Bytes(g.State);
        Assert.Contains(GameStateValidator.Validate(g.Content, g.State), error => error.Contains("civilian", StringComparison.OrdinalIgnoreCase));
        Assert.Throws<ArgumentException>(() => SaveGame.Serialize(g.Content, g.State));
        var ready = CommandProcessor.Execute(g.Content, g.State, new SetReadyCommand(g.State.Empires[0].Id, g.State.Turn, true));
        Assert.False(ready.Accepted); Assert.Same(g.State, ready.State);
        var resolved = TurnResolver.Resolve(g.Content, g.State);
        Assert.False(resolved.Succeeded); Assert.Same(g.State, resolved.State);
        Assert.Equal(before, CombatFixtures.Bytes(g.State));
    }

    public static IEnumerable<object[]> QueuedEdits()
    {
        foreach (var doctrine in Enum.GetValues<CombatDoctrine>())
        foreach (int cohesion in new[] { 40, 100 })
        for (int edit = 0; edit < 4; edit++) yield return new object[] { doctrine, cohesion, edit };
    }

    private static ColonyId AddReturnPort(LoadedGame g)
    {
        var home = g.State.Colonies[0]; var planet = g.State.FindPlanet(home.PlanetId)!;
        var target = g.State.Planets.First(p => p.StarId != planet.StarId && p.StarId != g.State.Combat!.Pockets[0].Star);
        target.PlanetTypeId = planet.PlanetTypeId; target.EnvironmentId = planet.EnvironmentId;
        var colony = home.Clone(); colony.Id = new(g.State.AllocateId()); colony.PlanetId = target.Id;
        g.State.Colonies.Add(colony); ObservationUpdater.Refresh(g.State, g.State.Turn); return colony.Id;
    }

    [Theory, MemberData(nameof(QueuedEdits))]
    public void QueuedOrderPreviewMatchesActualNextBoundary(CombatDoctrine doctrine, int cohesion, int edit)
    {
        var g = CombatWithdrawalTests.Battle(new[] { 100, 25 }, two: true);
        var alternative = AddReturnPort(g);
        foreach (var ship in g.State.Vessels) ship.Combat!.Cohesion = cohesion;
        g = CombatSaveTests.Next(g);
        var e = g.State.Combat!.Encounters.Single();
        g = new(g.Content, CombatCommandTests.Commit(g, new CombatAction.ChangeCombatOrders(e.Id, e.Revision, e.Orders with { Doctrine = doctrine }, false)));
        e = g.State.Combat!.Encounters.Single();
        g = new(g.Content, CombatCommandTests.Commit(g, new CombatAction.WithdrawCombat(e.Id, e.Revision)));
        e = g.State.Combat!.Encounters.Single();
        var orders = edit switch
        {
            1 => e.Orders with { OnArrival = CombatArrivalOrder.Hold },
            2 => e.Orders with { Stragglers = StragglerPolicy.DetachCrippled },
            3 => e.Orders with { ReturnColony = alternative },
            _ => e.Orders
        };
        var action = new CombatAction.ChangeCombatOrders(e.Id, e.Revision, orders, false);
        string before = CombatFixtures.Bytes(g.State);
        var preview = CombatQueries.Preview(g.Content, EmpireViewProjector.Project(g.Content, g.State, e.Owner), action);
        Assert.True(preview.Allowed); Assert.Equal(before, CombatFixtures.Bytes(g.State));
        var accepted = CombatCommandTests.Commit(g, action);
        bool withdrawal = edit == 0 || doctrine is CombatDoctrine.Cautious or CombatDoctrine.HitAndRun || doctrine == CombatDoctrine.Press && cohesion <= 40;
        Assert.Equal(withdrawal ? CombatPhase.WithdrawalQueued : CombatPhase.Active, accepted.Combat!.Encounters.Single().Phase);
        var next = TurnResolver.Resolve(g.Content, accepted);
        Assert.True(next.Succeeded, string.Join(";", next.Errors));
        var actual = Assert.Single(next.Report!.CombatRounds);
        Assert.Equal(withdrawal, actual.Interception);
        Assert.Equal(actual.Result.Shots.ToArray(), preview.Exchange!.Shots.ToArray());
        Assert.Equal(actual.Result.Ships.ToArray(), preview.Exchange.Ships.ToArray());
        Assert.Equal(actual.Result.Sentries.ToArray(), preview.Exchange.Sentries.ToArray());
        Assert.Equal(actual.Forecasts.ToArray(), preview.Forecasts.ToArray());
        Assert.Equal(withdrawal, next.Report.CombatDepartures.Length > 0);
        if (withdrawal)
        {
            Assert.All(preview.Exchange.Shots, shot => Assert.True(shot.Source.Sentry));
            foreach (var departure in next.Report.CombatDepartures)
            {
                var planned = preview.Departures.Single(p => p.Members.Contains(departure.Departure.Members[0]));
                Assert.Equal(planned.Destination, departure.Departure.Destination);
                Assert.Equal(planned.OnArrival, departure.Departure.OnArrival);
                Assert.Equal(planned.Speed, departure.Departure.Speed);
                Assert.Equal(planned.TotalTurns, departure.Departure.TotalTurns);
            }
        }
    }

    [Theory]
    [InlineData(false, CombatDoctrine.Hold)]
    [InlineData(true, CombatDoctrine.Hold)]
    [InlineData(true, CombatDoctrine.Press)]
    public void DangerousDoctrineAndRealWithdrawalOverrideAreExplained(bool queued, CombatDoctrine doctrine)
    {
        var g = CombatFixtures.Duel();
        CombatAction action = CombatCommandTests.Attack(g.State) with { Orders = CombatCommandTests.Attack(g.State).Orders with { Doctrine = doctrine } };
        if (queued)
        {
            g = CombatWithdrawalTests.Queued(CombatWithdrawalTests.Battle(new[] { 100, 25 }, two: true), StragglerPolicy.KeepTogether);
            var e = g.State.Combat!.Encounters.Single();
            action = new CombatAction.ChangeCombatOrders(e.Id, e.Revision, e.Orders with { Doctrine = doctrine, OnArrival = CombatArrivalOrder.Hold }, false);
        }
        var p = CombatQueries.Preview(g.Content, EmpireViewProjector.Project(g.Content, g.State, g.State.Empires[0].Id), action);
        Assert.True(p.Allowed);
        if (doctrine == CombatDoctrine.Hold) Assert.Contains(p.Explanations, text => text.Contains("Hold despite losses", StringComparison.Ordinal) && text.Contains("ignores losses and cohesion", StringComparison.Ordinal));
        if (queued) Assert.Contains(p.Explanations, text => text.Contains("overrides the queued withdrawal", StringComparison.Ordinal));
    }

    [Fact]
    public void UnarmedDefensiveForceChoosingLegalReturnMustQueueTheForecastWithdrawal()
    {
        var g = CombatWithdrawalTests.Battle(new[] { 1, 100 }, new[] { true, false }, two: true, crises: true);
        var alternative = AddReturnPort(g);
        g = CombatSaveTests.Next(g); g.State.Colonies[0].Crisis!.Quarantined = true;
        var e = g.State.Combat!.Encounters.Single();
        g = new(g.Content, CombatCommandTests.Commit(g, new CombatAction.RespondCombat(e.Id, e.Revision, CombatResponse.RemainWithBlockedReturn)));
        e = g.State.Combat!.Encounters.Single();
        g = new(g.Content, CombatCommandTests.Commit(g, new CombatAction.RespondCombat(e.Id, e.Revision, CombatResponse.AcceptRevisedForce)));
        e = g.State.Combat!.Encounters.Single(); Assert.Equal(CombatPhase.Active, e.Phase);
        var action = new CombatAction.ChangeCombatOrders(e.Id, e.Revision, e.Orders with { ReturnColony = alternative }, true);
        var p = CombatQueries.Preview(g.Content, EmpireViewProjector.Project(g.Content, g.State, e.Owner), action);
        Assert.True(p.Allowed); Assert.All(p.Exchange!.Shots, shot => Assert.True(shot.Source.Sentry));
        var changed = CommandProcessor.Execute(g.Content, g.State, CombatCommandTests.Command(g, action));
        Assert.True(changed.Accepted, changed.Rejection?.Message);
        Assert.Equal(CombatPhase.WithdrawalQueued, changed.State.Combat!.Encounters.Single().Phase);
        var next = TurnResolver.Resolve(g.Content, changed.State); Assert.True(next.Succeeded, string.Join(";", next.Errors));
        Assert.Equal(Assert.Single(next.Report!.CombatRounds).Result.Shots.ToArray(), p.Exchange.Shots.ToArray());
        Assert.Equal(alternative, Assert.Single(next.State.Vessels).Combat!.ReturnColony);
    }
}
