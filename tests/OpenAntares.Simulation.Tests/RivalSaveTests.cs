using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class RivalSaveTests
{
    [Theory]
    [InlineData("move")]
    [InlineData("transit")]
    [InlineData("found")]
    public void RivalCargoAndOrdersRoundTripWithoutAdvancingOrPlanning(string mode)
    {
        var content = RivalFixtures.Content(); var state = RivalFixtures.AtHome("sulan");
        var ai = state.Empires.Single(e => e.Controller == ControllerKind.Ai);
        var ship = state.Vessels.Single(v => v.EmpireId == ai.Id);
        ship.RoleId = "harbor_settler"; ship.CarriedPopulation = 2;
        var target = new PlanetState { Id = new(state.AllocateId()), StarId = ship.StationaryStarId!.Value,
            Orbit = 2, PlanetTypeId = "verdant_world", EnvironmentId = "dry" };
        state.Planets.Add(target);
        if (mode == "move") ship.PendingMove = state.Stars[0].Id;
        if (mode == "transit") { ship.Transit = new(ship.StationaryStarId.Value, state.Stars[0].Id, 2, 1); ship.StationaryStarId = null; }
        if (mode == "found") ship.PendingFounding = target.Id;
        ObservationUpdater.Refresh(state, state.Turn);
        string before = SaveGame.Serialize(content, state);
        var result = SaveGame.Deserialize(before);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        Assert.Equal(before, SaveGame.Serialize(result.Game!.Content, result.Game.State));
        Assert.Equal(0, result.Game.State.Turn); Assert.Equal(2, result.Game.State.FindVessel(ship.Id)!.CarriedPopulation);
    }
    public static TheoryData<int, int, bool> VersionPairs
    {
        get
        {
            var data = new TheoryData<int, int, bool>();
            for (int schema = 0; schema <= 6; schema++) for (int rules = 0; rules <= 5; rules++)
                data.Add(schema, rules, (schema, rules) is (1,1) or (2,1) or (3,1) or (3,2) or (4,1) or (4,2) or (4,3) or (5,1) or (5,2) or (5,3) or (5,4));
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(VersionPairs))]
    public void AcceptsExactlyElevenSchemaRulesPairs(int schema, int rules, bool expected)
    {
        string original = rules == 4 ? SaveGame.Serialize(RivalFixtures.Content(), RivalFixtures.AtHome("harl"))
            : File.ReadAllText(CompatibilityFixtures.PathFor(rules == 3 ? "legacy-rules3-v4-save.json" : rules == 2 ? "legacy-rules2-v3-save.json" : schema == 1 ? "legacy-v1-save.json" : "legacy-v2-save.json"));
        var root = JsonNode.Parse(original)!; root["schema_version"] = schema; root["rules_version"] = rules;
        Assert.Equal(expected, SaveGame.Deserialize(root.ToJsonString()).Succeeded);
    }

    [Theory]
    [InlineData("peldra")]
    [InlineData("harl")]
    [InlineData("sulan")]
    [InlineData("wenth")]
    public void PolicyContactsAndAllSpeciesStateRoundTrip(string species)
    {
        var content = RivalFixtures.Content() with { RivalAi = new("expansion_v1", 0, 9) };
        var state = RivalFixtures.AtHome(species); state.Turn = 2;
        var ai = state.Empires.Single(e => e.Controller == ControllerKind.Ai); var human = state.Empires[0];
        state.Empires.Add(new EmpireState { Id = new(state.AllocateId()), Controller = ControllerKind.Human, SpeciesId = "harl" });
        human.Ready = true; ai.ResearchReserve = 321; ai.ResearchTargetId = "cultivation_methods";
        ai.Contacts.Add(new(human.Id, human.SpeciesId!, 1)); human.Contacts.Add(new(ai.Id, ai.SpeciesId!, 0));
        state.Vessels.Single(v => v.EmpireId == ai.Id).PendingMove = state.Stars[0].Id;
        ObservationUpdater.Refresh(state, state.Turn);
        string original = SaveGame.Serialize(content, state);
        var loaded = SaveGame.Deserialize(original);
        Assert.True(loaded.Succeeded, string.Join("; ", loaded.Errors));
        Assert.Equal(original, SaveGame.Serialize(loaded.Game!.Content, loaded.Game.State));
        Assert.Equal(5, JsonNode.Parse(original)!["schema_version"]!.GetValue<int>());
        Assert.Equal(new("expansion_v1", 0, 9), loaded.Game.Content.RivalAi);
        Assert.True(loaded.Game.State.FindEmpire(human.Id)!.Ready);
        Assert.Equal(321, loaded.Game.State.FindEmpire(ai.Id)!.ResearchReserve);
        Assert.Equal(state.Random.State, loaded.Game.State.Random.State);
        Assert.Equal(state.NextEntityId, loaded.Game.State.NextEntityId);
    }

    public static TheoryData<int, int, string> OldPairs
    {
        get
        {
            var data = new TheoryData<int, int, string>();
            foreach (var (schema, rules) in new[] { (1,1),(2,1),(3,1),(3,2),(4,1),(4,2),(4,3),(5,1),(5,2),(5,3) })
                foreach (string field in new[] { "ai_policy_id", "contacts", "contacts_null", "content" }) data.Add(schema, rules, field);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(OldPairs))]
    public void OldFormatsRejectInjectedRivalFields(int schema, int rules, string field)
    {
        string name = rules == 3 ? "legacy-rules3-v4-save.json" : rules == 2 ? "legacy-rules2-v3-save.json"
            : schema == 1 ? "legacy-v1-save.json" : "legacy-v2-save.json";
        var root = JsonNode.Parse(File.ReadAllText(CompatibilityFixtures.PathFor(name)))!;
        root["schema_version"] = schema;
        Assert.True(SaveGame.Deserialize(root.ToJsonString()).Succeeded);
        Assert.DoesNotContain("rival_ai", root.ToJsonString());
        Assert.DoesNotContain("ai_policy_id", root.ToJsonString());
        Assert.DoesNotContain("contacts", root.ToJsonString());
        if (field == "content") root["content"]!["rival_ai"] = JsonNode.Parse("""{"algorithm_id":"expansion_v1","growth_support_workers":1,"resurvey_min_age":3}""");
        else root["state"]!["empires"]![0]![field == "contacts_null" ? "contacts" : field] = field == "contacts" ? new JsonArray() : null;
        Assert.False(SaveGame.Deserialize(root.ToJsonString()).Succeeded);
    }

    [Theory]
    [InlineData("missing_policy")]
    [InlineData("missing_contacts")]
    [InlineData("bad_policy")]
    [InlineData("missing_content")]
    [InlineData("self")]
    [InlineData("foreign")]
    [InlineData("future")]
    [InlineData("identity")]
    [InlineData("duplicate")]
    public void InvalidRivalDataCannotReplaceTheSession(string mutation)
    {
        var content = RivalFixtures.Content(); var state = RivalFixtures.AtHome("peldra");
        var session = new GameSession(content, state);
        var root = JsonNode.Parse(SaveGame.Serialize(content, state))!;
        var ai = root["state"]!["empires"]![1]!.AsObject();
        if (mutation == "missing_policy") ai.Remove("ai_policy_id");
        else if (mutation == "missing_contacts") ai.Remove("contacts");
        else if (mutation == "bad_policy") ai["ai_policy_id"] = "unsupported";
        else if (mutation == "missing_content") root["content"]!.AsObject().Remove("rival_ai");
        else
        {
            var contact = new JsonObject { ["other_empire"] = state.Empires[0].Id.Value, ["species_id"] = "harl", ["first_observed_turn"] = 0 };
            if (mutation == "self") { contact["other_empire"] = state.Empires[1].Id.Value; contact["species_id"] = "peldra"; }
            if (mutation == "foreign") contact["other_empire"] = 9999;
            if (mutation == "future") contact["first_observed_turn"] = 1;
            if (mutation == "identity") contact["species_id"] = "wenth";
            var contacts = new JsonArray(contact); if (mutation == "duplicate") contacts.Add(contact.DeepClone()); ai["contacts"] = contacts;
        }
        string path = Path.Combine(Path.GetTempPath(), "OpenAntares-rival-load-" + Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path, root.ToJsonString());
            Assert.False(session.Load(path).Succeeded);
            Assert.Same(state, session.State); Assert.Same(content, session.Content);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void FailedAtomicWritePreservesPriorDestinationAndCleansTemporaryFile()
    {
        var content = RivalFixtures.Content(); var state = RivalFixtures.AtHome("harl");
        string directory = Path.Combine(Path.GetTempPath(), "OpenAntares-rival-write-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "save.json"); File.WriteAllText(path, "prior contents");
        try
        {
            Exception? failure;
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                failure = Record.Exception(() => SaveGame.WriteFile(path, content, state));
            Assert.True(failure is IOException or UnauthorizedAccessException);
            Assert.Equal("prior contents", File.ReadAllText(path));
            Assert.Single(Directory.GetFiles(directory));
        }
        finally { File.Delete(path); Directory.Delete(directory); }
    }
}
