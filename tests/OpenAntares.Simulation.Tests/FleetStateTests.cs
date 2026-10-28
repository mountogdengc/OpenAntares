using System;
using System.Linq;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class FleetStateTests
{
    [Fact]
    public void AlternativeBodySpeciesInitializesFromDeclaredCapability()
    {
        var root = FleetContentTests.JoinedCore();
        var species = root["species"]!.AsArray().Single(n => n!["id"]!.GetValue<string>() == "wenth")!;
        species["id"] = "chorus";
        var loaded = OpenAntares.Simulation.Content.ContentLoader.Load(new[] { new OpenAntares.Simulation.Content.ContentSource("alternative-body.json", root.ToJsonString()) }, requireFleets: true);
        Assert.True(loaded.Succeeded, string.Join("; ", loaded.Errors));
        var generated = GalaxyGenerator.CreateNewGame(loaded.Content!, new("small", 42, 8, "chorus", CrisesEnabled: false, AmebasEnabled: false));
        Assert.True(generated.Succeeded, generated.Error);
        var state = generated.State!;
        var command = Assert.Single(state.Empires).Fleets!;
        var officer = Assert.Single(command.Officers);
        Assert.Equal(CommandEmbodiment.ColonyBody, officer.Embodiment);
        Assert.Equal(Assert.Single(state.Colonies).Id, officer.SourceBody);
        Assert.Empty(command.CandidateProfiles);
        Assert.Empty(GameStateValidator.Validate(loaded.Content!, state));
    }

    [Fact]
    public void OrdinaryCompletionCreatesMarkedIndependentHullWithoutAllocatingAFleet()
    {
        var g = FleetFixtures.Game(); var s = g.State; var col = Assert.Single(s.Colonies);
        col.ProductionReserve = 800; // Labelled funded pre-session supplement.
        var session = new GameSession(g.Content, s); var owner = s.Empires[0].Id;
        Assert.True(session.Execute(new SetProductionTargetCommand(owner, s.Turn, col.Id, new(ProductionKinds.Vessel, "scout"))).Accepted);
        var result = session.Execute(new SetReadyCommand(owner, s.Turn, true));
        Assert.True(result.Accepted, result.Rejection?.Message);
        var added = Assert.Single(session.State.Vessels, v => s.Vessels.All(old => old.Id != v.Id));
        Assert.True(added.NewlyCompleted); Assert.Null(added.Fleet);
        Assert.Empty(session.State.Empires[0].Fleets!.Fleets); Assert.Equal(1, session.State.Empires[0].Fleets!.NextFleetId);
    }

    [Fact]
    public void CloneIsDeepAndLegacyMetadataRejected()
    {
        var g = FleetFixtures.Valid(FleetFixtures.Nested()); var s = g.State;
        string before = FleetFixtures.Bytes(s);
        var clone = s.Clone(); var f = clone.Empires[0].Fleets!;
        f.Fleets[0].Members.Clear(); f.Requests[0].Colonies.Clear(); f.Requests[0].Slots[0].RoleId = "changed";
        f.Officers[0].Highlights.Add(new(1, 0, "test", "Clone only"));
        f.CandidateProfiles.Clear(); f.ProfileCommissions[f.Officers[0].ProfileId]++;
        f.SurveyCredits.Clear(); f.RecoveryRandom!.NextInt(100);
        f.PendingEvents.Add(new(clone.Empires[0].Id, 1, 0, "test", "Clone only"));
        Assert.Equal(before, FleetFixtures.Bytes(s));

        var old = GalaxyGenerator.CreateNewGame(g.Content, new("small", 42, 7, "harl", AmebasEnabled: false)).State!;
        Assert.Empty(GameStateValidator.Validate(g.Content, old));
        old.Empires[0].Fleets = s.Empires[0].Fleets!.Clone();
        Assert.NotEmpty(GameStateValidator.Validate(g.Content, old));
    }

    [Fact]
    public void RejectsBrokenMembershipAssignmentsDeadlinesAndCounters()
    {
        var g = FleetFixtures.Valid(FleetFixtures.Nested());
        foreach (var mutate in new Action<GameState>[]
        {
            s => s.Empires[0].Fleets!.Fleets[0].Members.Add(s.Empires[0].Fleets!.Fleets[0].Flagship),
            s => s.Empires[0].Fleets!.Fleets[0].Members.Add(s.Vessels.First(v => v.EmpireId != s.Empires[0].Id).Id),
            s => s.Empires[0].Fleets!.Fleets[0].Flagship = new(999999),
            s => s.Empires[0].Fleets!.NextFleetId = 1,
            s => s.Empires[0].Fleets!.Officers[0].Xp = 31,
            s => s.Empires[0].Fleets!.Officers[0].Rank = 2,
            s => { var o = s.Empires[0].Fleets!.Officers[0]; o.Status = OfficerStatus.Recovering; o.PostedColony = null; o.RecoveryColony = s.Colonies.First(c => c.EmpireId == s.Empires[0].Id).Id; o.AvailableAtTurn = s.Turn; },
            s => s.Empires[0].Fleets!.RecoveryRandom!.Increment = 2,
            s => s.Empires[0].Fleets!.RecoveryRollCount = -1,
            s => s.Empires[0].Fleets!.Requests[0].Slots[0].QueueToken = 999,
            s => s.Empires[0].Fleets!.Requests[0].Slots[0].Status = SlotStatus.BlockedSafety,
            s => s.Empires[0].Fleets!.Officers[0].SourceBody = s.Colonies.First(c => c.EmpireId == s.Empires[0].Id).Id
        })
        {
            var copy = g.State.Clone(); mutate(copy);
            Assert.NotEmpty(GameStateValidator.Validate(g.Content, copy));
        }
    }

    [Fact]
    public void StartsOnePostedNavigatorWithoutGlobalIdsOrOldRngDraws()
    {
        foreach (string species in new[] { "peldra", "harl", "sulan", "wenth" })
        {
            var g = FleetFixtures.Game(species, amebas: true); var s = g.State;
            var old = GalaxyGenerator.CreateNewGame(g.Content, new("small", 42, 7, species, AmebasEnabled: true)).State!;
            var e = Assert.Single(s.Empires); var f = e.Fleets; Assert.NotNull(f);
            var officer = Assert.Single(f.Officers);
            Assert.Equal(OfficerStatus.Posted, officer.Status); Assert.Equal(OfficerSpecialty.Navigator, officer.Specialty);
            Assert.Equal(0, officer.Xp); Assert.Equal(1, f.LifetimeCommissions);
            Assert.Equal(Assert.Single(s.Colonies).Id, officer.PostedColony);
            Assert.Equal(old.NextEntityId, s.NextEntityId); Assert.Equal(old.Random.State, s.Random.State); Assert.Equal(old.Random.Increment, s.Random.Increment);
            Assert.Equal(old.Scouting!.PlacementRandom!.State, s.Scouting!.PlacementRandom!.State);
            Assert.Equal(old.Empires[0].Scouting!.RiskRandom!.State, e.Scouting!.RiskRandom!.State);
            Assert.Equal(0, f.RecoveryRollCount); Assert.NotNull(f.RecoveryRandom);
            Assert.Empty(f.Fleets); Assert.Empty(f.Requests);
            Assert.All(s.Vessels, v => { Assert.Null(v.Fleet); Assert.False(v.NewlyCompleted); });
            if (species == "wenth") { Assert.Equal(Assert.Single(s.Colonies).Id, officer.SourceBody); Assert.Empty(f.CandidateProfiles); Assert.Contains("Command appointment", officer.Name); }
            else { Assert.Null(officer.SourceBody); Assert.Equal(2, f.CandidateProfiles.Count); }
        }
    }
}
