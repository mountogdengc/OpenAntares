using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class RivalContactTests
{
    [Fact]
    public void KnownContactsRetainOnlyDatedFactsAndMissingSavedContactRejects()
    {
        var content = RivalFixtures.Content(); var state = RivalFixtures.AtHome("peldra");
        var human = state.Empires[0]; var ai = state.Empires[1];
        state.Vessels.Single(v => v.EmpireId == ai.Id).PendingMove = state.Stars[0].Id;
        var next = TurnResolver.Resolve(content, state); Assert.True(next.Succeeded, string.Join("; ", next.Errors));
        var contact = Assert.Single(EmpireViewProjector.Project(content, next.State, human.Id).Contacts);
        Assert.Equal(1, contact.FirstObservedTurn); Assert.Equal("peldra", contact.SpeciesId);
        Assert.Equal(content.Species["peldra"].CultureName, contact.CultureName);
        var root = JsonNode.Parse(SaveGame.Serialize(content, next.State))!;
        root["state"]!["empires"]![0]!["contacts"] = new JsonArray();
        Assert.False(SaveGame.Deserialize(root.ToJsonString()).Succeeded);
        var candidate = state.Clone(); candidate.Vessels.Single(v => v.EmpireId == ai.Id).PendingMove = null;
        candidate.Vessels.Single(v => v.EmpireId == ai.Id).StationaryStarId = state.Stars[0].Id;
        ObservationUpdater.Refresh(candidate, 0);
        int allocator = candidate.NextEntityId; ulong rng = candidate.Random.State;
        Assert.Equal(2, ContactUpdater.Refresh(state, candidate).Count(e => e.Kind == "first_contact"));
        Assert.Equal(allocator, candidate.NextEntityId); Assert.Equal(rng, candidate.Random.State);
        var legacy = SpeciesFixtures.AtHome("harl");
        Assert.Empty(ContactUpdater.Refresh(legacy, legacy.Clone())); Assert.Empty(legacy.Empires[0].Contacts);
    }
    private static ImmutableArray<EncounterEvent> Encounters(TurnReport report) => report.EncounterEvents;
    private static ImmutableArray<EncounterEvent> Encounters(EmpireTurnReportView report) => report.EncounterEvents;
    private static string ReportSnapshot(EmpireTurnReportView report) => JsonSerializer.Serialize(new {
        report.FromTurn, report.ToTurn, report.Colonies, report.Research, report.Events, report.EncounterEvents,
        Stars = report.StarNames.OrderBy(p => p.Key).Select(p => new { p.Key, p.Value }).ToArray() });

    [Fact]
    public void OnlyCurrentPresenceCreatesViewerContact()
    {
        var content = RivalFixtures.Content(); var state = RivalFixtures.AtHome("peldra");
        var ai = state.Empires[1]; var human = state.Empires[0]; var ship = state.Vessels.Single(v => v.EmpireId == ai.Id);
        ship.Transit = new(ship.StationaryStarId!.Value, state.Stars[0].Id, 2, 2); ship.StationaryStarId = null;
        var travelling = TurnResolver.Resolve(content, state); Assert.True(travelling.Succeeded, string.Join("; ", travelling.Errors));
        Assert.All(travelling.State.Empires, e => Assert.Empty(e.Contacts));
        var arrived = TurnResolver.Resolve(content, travelling.State); Assert.True(arrived.Succeeded, string.Join("; ", arrived.Errors));
        Assert.All(arrived.State.Empires, e => Assert.Single(e.Contacts));
        Assert.All(arrived.State.Empires.SelectMany(e => e.Contacts), c => Assert.Equal(2, c.FirstObservedTurn));
        Assert.Equal(2, Encounters(arrived.Report!).Count(e => e.Kind == "first_contact"));
        var view = EmpireViewProjector.Project(content, arrived.State, human.Id);
        Assert.Equal("peldra", Assert.Single(view.Contacts).SpeciesId);
        var rivalStar = state.FindPlanet(state.Colonies.Single(c => c.EmpireId == ai.Id).PlanetId)!.StarId;
        Assert.Null(view.Stars.Single(s => s.Chart.Id == rivalStar).Planets); Assert.DoesNotContain(rivalStar, view.Galaxy.HomeMarkers);
        var again = TurnResolver.Resolve(content, arrived.State); Assert.True(again.Succeeded, string.Join("; ", again.Errors));
        Assert.Empty(Encounters(again.Report!));
        var move = CommandProcessor.Execute(content, again.State, new SetMoveOrderCommand(ai.Id, again.State.Turn, ship.Id, rivalStar)); Assert.True(move.Accepted);
        // Isolate departure/contact retention: the active policy can choose a different scouting order.
        var left = TurnResolver.Resolve(content, move.State, (_, snapshot) => new(snapshot, true)); Assert.True(left.Succeeded, string.Join("; ", left.Errors));
        Assert.Empty(EmpireViewProjector.Project(content, left.State, human.Id).ForeignVessels);
        Assert.Single(left.State.FindEmpire(human.Id)!.Contacts);
        var saved = SaveGame.Deserialize(SaveGame.Serialize(content, left.State)); Assert.True(saved.Succeeded);
        Assert.Equal(2, saved.Game!.State.FindEmpire(human.Id)!.Contacts[0].FirstObservedTurn);
    }

    [Fact]
    public void StaleColonyDiscoveryIsObservedNotNewlyFounded()
    {
        var content = RivalFixtures.Content(); var state = RivalFixtures.AtHome("peldra"); var human = state.Empires[0]; var ai = state.Empires[1];
        var foreignPlanet = state.FindPlanet(state.Colonies.Single(c => c.EmpireId == ai.Id).PlanetId)!;
        human.Surveys.Add(new SystemObservation { StarId = foreignPlanet.StarId, LastObservedTurn = 0,
            Planets = { new(foreignPlanet.Id, foreignPlanet.Orbit, foreignPlanet.PlanetTypeId, null, null) { EnvironmentId = foreignPlanet.EnvironmentId } } });
        var ship = state.Vessels.Single(v => v.EmpireId == human.Id); ship.PendingMove = foreignPlanet.StarId;
        var result = TurnResolver.Resolve(content, state); Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        var report = EmpireViewProjector.ProjectReport(content, result.State, result.Report!, human.Id);
        var observed = Assert.Single(Encounters(report), e => e.Kind == "observed_colony");
        Assert.Equal(1, observed.ObservedTurn);
        Assert.All(Encounters(report), e => Assert.Equal(human.Id, e.Viewer));
        var departure = CommandProcessor.Execute(content, result.State, new SetMoveOrderCommand(human.Id, 1, ship.Id, state.Stars[0].Id)); Assert.True(departure.Accepted);
        var left = TurnResolver.Resolve(content, departure.State); Assert.True(left.Succeeded, string.Join("; ", left.Errors));
        var stale = EmpireViewProjector.Project(content, left.State, human.Id).Stars.Single(s => s.Chart.Id == foreignPlanet.StarId);
        Assert.False(stale.Current); Assert.Equal(1, stale.LastObservedTurn); Assert.Equal(ai.Id, stale.Planets!.Value[0].OwnerId);
    }

    [Fact]
    public void UnseenForeignChangesDoNotChangeOwnViewOrReport()
    {
        var content = RivalFixtures.Content(); var state = RivalFixtures.AtHome("peldra"); var human = state.Empires[0]; var ai = state.Empires[1];
        var ship = state.Vessels.Single(v => v.EmpireId == ai.Id); ship.PendingMove = state.Stars[0].Id;
        var next = TurnResolver.Resolve(content, state); Assert.True(next.Succeeded, string.Join("; ", next.Errors));
        var beforeView = JsonSerializer.Serialize(EmpireViewProjector.Project(content, next.State, human.Id));
        var beforeReport = ReportSnapshot(EmpireViewProjector.ProjectReport(content, next.State, next.Report!, human.Id));
        var changed = next.State.Clone(); var changedAi = changed.FindEmpire(ai.Id)!;
        changedAi.SpeciesId = "wenth"; changedAi.ResearchReserve = long.MaxValue; changedAi.ResearchTargetId = "measurement_methods";
        var hidden = changed.FindPlanet(changed.Colonies.Single(c => c.EmpireId == ai.Id).PlanetId)!; hidden.EnvironmentId = "dry";
        changed.FindVessel(ship.Id)!.PendingMove = hidden.StarId;
        Assert.Equal(beforeView, JsonSerializer.Serialize(EmpireViewProjector.Project(content, changed, human.Id)));
        Assert.Equal(beforeReport, ReportSnapshot(EmpireViewProjector.ProjectReport(content, changed, next.Report!, human.Id)));
    }
}
