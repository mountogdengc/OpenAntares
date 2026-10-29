using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Species;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Saves;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CombatContentTests
{
    [Fact]
    public void OldExpansionCannotEmbedCombatOnlyUnitOrProject()
    {
        var content = ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        var result = GalaxyGenerator.CreateNewGame(content, new("small", 42, 2));
        Assert.True(result.Succeeded, result.Error);
        var unit = result.State!.Clone(); unit.Vessels[0].RoleId = "escort";
        Assert.Throws<ArgumentException>(() => SaveGame.Serialize(content, unit));
        var project = result.State.Clone(); project.Colonies[0].ProjectId = "escort"; project.Colonies[0].ProjectKind = ProductionKinds.Vessel;
        Assert.Throws<ArgumentException>(() => SaveGame.Serialize(content, project));
    }

    [Fact]
    public void CoreProfilesAndLegacyRoleGate()
    {
        var result = ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory());
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        var content = result.Content!;
        Assert.True(content.Vessels.ContainsKey("escort"), "Paid combat hull is not defined.");
        Assert.Equal((2400L, 300, 0, false), (content.Vessels["escort"].Cost, content.Vessels["escort"].Speed, content.Vessels["escort"].EmbarkPopulation, content.Vessels["escort"].CanFoundColony));
        var full = Embedded(content, null);
        Assert.NotNull(full["combat"]);
        var profiles = full["combat"]!["role_profiles"]!;
        Assert.Equal((100, 24, 4), (profiles["escort"]!["max_hull"]!.GetValue<int>(), profiles["escort"]!["attack"]!.GetValue<int>(), profiles["escort"]!["armor"]!.GetValue<int>()));
        Assert.Equal(60, profiles["scout"]!["max_hull"]!.GetValue<int>());
        Assert.Equal((80, 18, 2, 250), (full["combat"]!["sentry_profile"]!["max_hull"]!.GetValue<int>(), full["combat"]!["sentry_profile"]!["attack"]!.GetValue<int>(), full["combat"]!["sentry_profile"]!["armor"]!.GetValue<int>(), full["combat"]!["sentry_pursuit_speed"]!.GetValue<int>()));
        foreach (string species in content.Species.Keys)
        {
            var start = GalaxyGenerator.CreateNewGame(content, new("small", 42, 8, species, AmebasEnabled: false));
            Assert.True(start.Succeeded, start.Error);
            var old = start.State!; var owner = old.Empires[0].Id;
            Assert.False(SpeciesRules.CanBuildVessel(content, old, owner, "escort"));
            var request = CommandProcessor.Execute(content, old, new SetProductionTargetCommand(owner, old.Turn, old.Colonies[0].Id, new(ProductionKinds.Vessel, "escort")));
            Assert.False(request.Accepted); Assert.Same(old, request.State);
            var future = old.Clone(); future.RulesVersion = 9;
            Assert.True(SpeciesRules.CanBuildVessel(content, future, owner, "escort"));
        }
        var legacy = Embedded(content, 8);
        Assert.Null(legacy["combat"]);
        Assert.DoesNotContain(legacy["vessels"]!.AsArray(), v => v!["id"]!.GetValue<string>() == "escort");
        Assert.DoesNotContain("combat_only", legacy.ToJsonString(), StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidProfilesAndBoundsHaveLocatedErrors()
    {
        var original = CoreNode();
        Assert.NotNull(original["combat"]);
        foreach (var change in new Action<JsonObject>[]
        {
            root => root["combat"]!["role_profiles"]!.AsObject().Remove("scout"),
            root => root["combat"]!["role_profiles"]!["escort"]!["max_hull"] = 0,
            root => root["combat"]!["role_profiles"]!["escort"]!["attack"] = -1,
            root => root["combat"]!["sentry_pursuit_speed"] = 0,
            root => root["combat"]!["repair_hull_divisor"] = 0,
            root => root["combat"]!["history_limit"] = 21,
            root => root["combat"]!["attack_percent"] = new JsonArray(100, 50),
            root => root["combat"]!["hull_thresholds"] = new JsonArray(25, 50),
            root => root["combat"]!.AsObject().Remove("repair_cohesion")
        })
        {
            var node = (JsonObject)original.DeepClone(); change(node);
            var loaded = ContentLoader.Load(new[] { new ContentSource("invalid-combat.json", node.ToJsonString()) });
            Assert.False(loaded.Succeeded);
            Assert.Contains(loaded.Errors, error => error.File == "invalid-combat.json" && error.Field is not null && error.Field.Contains("combat", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void DuplicateCombatFieldsAreRejected()
    {
        var node = CoreNode(); Assert.NotNull(node["combat"]);
        string text = node.ToJsonString();
        Assert.Contains("\"max_hull\":100", text, StringComparison.Ordinal);
        text = text.Replace("\"max_hull\":100", "\"max_hull\":100,\"max_hull\":100", StringComparison.Ordinal);
        var loaded = ContentLoader.Load(new[] { new ContentSource("duplicate-combat.json", text) });
        Assert.False(loaded.Succeeded);
        Assert.Contains(loaded.Errors, e => e.File == "duplicate-combat.json" && e.Field!.Contains("combat", StringComparison.Ordinal));
    }

    internal static JsonObject CoreNode()
    {
        var root = new JsonObject();
        foreach (string file in Directory.GetFiles(ContentLoaderTests.CoreContentDirectory(), "*.json").OrderBy(p => p, StringComparer.Ordinal))
            foreach (var item in JsonNode.Parse(File.ReadAllText(file))!.AsObject()) root.Add(item.Key, item.Value!.DeepClone());
        return root;
    }
    internal static JsonObject Embedded(ContentSet content, int? rules)
    {
        using var memory = new MemoryStream();
        using (var writer = new Utf8JsonWriter(memory)) { if (rules is {} version) ContentWriter.Write(writer, content, version); else ContentWriter.Write(writer, content); }
        return JsonNode.Parse(Encoding.UTF8.GetString(memory.ToArray()))!.AsObject();
    }
}
