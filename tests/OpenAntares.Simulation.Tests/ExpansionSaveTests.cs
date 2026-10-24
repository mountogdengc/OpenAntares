using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;

public class ExpansionSaveTests
{
    private static string Fixture(int schema) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../..", "Fixtures", $"legacy-v{schema}-save.json"));
    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(2, 1, true)]
    [InlineData(3, 1, true)]
    [InlineData(3, 2, true)]
    [InlineData(1, 2, false)]
    [InlineData(2, 2, false)]
    [InlineData(3, 0, false)]
    [InlineData(3, 3, false)]
    [InlineData(4, 1, true)]
    [InlineData(5, 1, false)]
    public void ReadsOnlySupportedSchemaRulesPairs(int schema, int rules, bool expected)
    {
        string json = schema <= 2 ? Fixture(schema) : SaveGame.Serialize(rules == 2 ? ExpansionFixtures.Content() : Fixtures.PrototypeContent(), rules == 2 ? ExpansionFixtures.AtHome() : Fixtures.StartingState());
        var root = JsonNode.Parse(json)!; root["schema_version"] = schema; root["rules_version"] = rules;
        var loaded = SaveGame.Deserialize(root.ToJsonString()); Assert.Equal(expected, loaded.Succeeded);
    }
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ActualLegacyFixturesContinueAfterResave(int schema)
    {
        var loaded = SaveGame.Deserialize(Fixture(schema)); Assert.True(loaded.Succeeded, string.Join("\n", loaded.Errors));
        var original = loaded.Game!; var text = SaveGame.Serialize(original.Content, original.State);
        Assert.Equal(4, JsonNode.Parse(text)!["schema_version"]!.GetValue<int>());
        var reread = SaveGame.Deserialize(text); Assert.True(reread.Succeeded, string.Join("\n", reread.Errors));
        var copy = reread.Game!; Assert.Equal(Fixtures.Snapshot(original.State), Fixtures.Snapshot(copy.State));
        Assert.Equal(1, copy.State.RulesVersion); Assert.Empty(copy.State.Vessels); Assert.Null(copy.Content.Expansion); Assert.Empty(copy.Content.Vessels);
        var a = original.State; var b = copy.State;
        for (int i = 0; i < 4; i++) { var x = TurnResolver.Resolve(original.Content, a); var y = TurnResolver.Resolve(copy.Content, b); Assert.True(x.Succeeded); Assert.True(y.Succeeded); a = x.State; b = y.State; Assert.Equal(Fixtures.Snapshot(a), Fixtures.Snapshot(b)); }
    }
    [Theory]
    [InlineData("move")]
    [InlineData("transit")]
    [InlineData("found")]
    public void OrdersTransitAndSurveysRoundTrip(string mode)
    {
        var s = ExpansionFixtures.AtHome(); var v = s.Vessels[0];
        if (mode == "move") v.PendingMove = new(6);
        if (mode == "transit") { v.StationaryStarId = null; v.Transit = new(Fixtures.HomeStar, new(6), 2, 1); }
        if (mode == "found") { v.RoleId = "settler"; v.CarriedPopulation = 2; v.StationaryStarId = new(6); v.PendingFounding = new(7); s.Empires[0].Surveys.Add(new SystemObservation { StarId = new(6), Planets = { new PlanetObservation(new(7), 1, "forge_world", null, null) } }); }
        var result = SaveGame.Deserialize(SaveGame.Serialize(ExpansionFixtures.Content(), s)); Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(Fixtures.Snapshot(s), Fixtures.Snapshot(result.Game!.State)); Assert.Equal("settler", result.Game.Content.Vessels["settler"].Id);
    }
    [Theory]
    [InlineData("vessels")]
    [InlineData("surveys")]
    [InlineData("project_kind")]
    public void RequiredExpansionFieldsCannotDisappear(string field)
    {
        var j = JsonNode.Parse(SaveGame.Serialize(ExpansionFixtures.Content(), ExpansionFixtures.AtHome()))!;
        if (field == "vessels") j["state"]!.AsObject().Remove(field);
        else if (field == "surveys") j["state"]!["empires"]![0]!.AsObject().Remove(field);
        else j["state"]!["colonies"]![0]!.AsObject().Remove(field);
        Assert.False(SaveGame.Deserialize(j.ToJsonString()).Succeeded);
    }
    [Fact]
    public void MalformedExtensionCannotReplaceSession()
    {
        var c = ExpansionFixtures.Content(); var s = ExpansionFixtures.AtHome(); var j = JsonNode.Parse(SaveGame.Serialize(c, s))!;
        j["state"]!["vessels"]![0]!["empire"] = 999;
        Assert.False(SaveGame.Deserialize(j.ToJsonString()).Succeeded);
        j["state"]!["vessels"] = "bad"; Assert.False(SaveGame.Deserialize(j.ToJsonString()).Succeeded);
    }
    [Theory]
    [InlineData("missing-colony")]
    [InlineData("wrong-planet")]
    public void HistoricalObservationRejectsImpossibleColonyReference(string change)
    {
        var state = HistoricalColonyObservation();
        var j = JsonNode.Parse(SaveGame.Serialize(ExpansionFixtures.Content(), state))!;
        j["state"]!["empires"]![0]!["surveys"]![1]!["planets"]![0]!["colony"] = change == "missing-colony" ? 2147483646 : Fixtures.HomeColony.Value;
        var loaded = SaveGame.Deserialize(j.ToJsonString());
        Assert.False(loaded.Succeeded);
        Assert.Null(loaded.Game);
    }
    [Fact]
    public void HistoricalObservationPreservesStaleOwnershipForExistingColony()
    {
        var state = HistoricalColonyObservation();
        // Historical ownership may differ from truth; colony identity and planet must still exist.
        state.Empires[0].Surveys[1].Planets[0] = state.Empires[0].Surveys[1].Planets[0] with { OwnerId = Fixtures.Empire };
        var loaded = SaveGame.Deserialize(SaveGame.Serialize(ExpansionFixtures.Content(), state));
        Assert.True(loaded.Succeeded, string.Join("\n", loaded.Errors));
        Assert.Equal(Fixtures.Empire, loaded.Game!.State.Empires[0].Surveys[1].Planets[0].OwnerId);
        Assert.Equal(new EmpireId(8), loaded.Game.State.FindColony(new(9))!.EmpireId);
    }
    private static GameState HistoricalColonyObservation()
    {
        var state = ExpansionFixtures.AtHome();
        state.Empires.Add(new EmpireState { Id = new(8), Controller = ControllerKind.Ai });
        state.Colonies.Add(new ColonyState { Id = new(9), EmpireId = new(8), PlanetId = new(7), Population = 2, Workforce = new(2, 0, 0) });
        state.NextEntityId = 10;
        state.Vessels[0].StationaryStarId = new(6);
        OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(state, 0);
        state.Vessels[0].StationaryStarId = Fixtures.HomeStar;
        state.Turn = 1;
        OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(state, 1);
        return state;
    }
    [Theory]
    [InlineData("empty")]
    [InlineData("stale")]
    [InlineData("wrong-owner")]
    public void CurrentPresenceRequiresCompleteCurrentObservation(string change)
    {
        var state = ExpansionFixtures.AtHome(); state.Turn = 1; OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(state, 1);
        var j = JsonNode.Parse(SaveGame.Serialize(ExpansionFixtures.Content(), state))!; var surveys = j["state"]!["empires"]![0]!["surveys"]!;
        if (change == "empty") surveys.AsArray().Clear();
        if (change == "stale") surveys[0]!["last_observed_turn"] = 0;
        if (change == "wrong-owner") { surveys[0]!["planets"]![0]!["owner"] = null; surveys[0]!["planets"]![0]!["colony"] = null; }
        Assert.False(SaveGame.Deserialize(j.ToJsonString()).Succeeded);
    }
}
