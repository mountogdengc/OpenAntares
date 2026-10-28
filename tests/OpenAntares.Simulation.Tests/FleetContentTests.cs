using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class FleetContentTests
{
    [Fact]
    public void LoadsApprovedValuesAndRejectsInvalidReferences()
    {
        var content = Core();
        var d = content.FleetCommand;
        Assert.NotNull(d);
        Assert.Equal(new[] { 10, 30 }, d.RankThresholds);
        Assert.Equal(new[] { 5, 10 }, d.NavigatorBonuses);
        Assert.Equal(new[] { 100, 200 }, d.SurveyResearchBonuses);
        Assert.Equal(new[] { 5, 10 }, d.OrganizerDiscounts);
        Assert.Equal((32, 64, 32, 32), (d.MemberLimit, d.FleetLimit, d.RequestLimit, d.ParticipatingColonyLimit));
        Assert.Equal((4, 64, 2, 30, 32, 10), (d.ActiveOfficerLimit, d.LifetimeCommissionLimit, d.CommissionTurns, d.XpCap, d.HighlightLimit, d.DetailTurnLimit));
        Assert.Equal((2, 1, 8), (d.FirstSurveyXp, d.AssemblyHullXp, d.StudyXp));
        Assert.Equal((50, 70, 2, 2, 4, 1), (d.MortalRecoveryPercent, d.ResearchedRecoveryPercent, d.MortalRecoveryTurns, d.LinkRecoveryTurns, d.LastWatchLinkTurns, d.ResearchedLinkReduction));
        Assert.All(d.Cultures, c => Assert.Equal(OfficerDoctrine.RecoverCommand, c.DefaultDoctrine));
        Assert.Equal(12, d.Profiles.Length);
        var technology = content.Technologies[d.RecoveryTechnologyId];
        Assert.Equal(2400, technology.Cost);
        Assert.Equal(new[] { "industrial_coordination" }, technology.PrerequisiteTechnologyIds);

        foreach (var mutate in new Action<JsonObject>[]
        {
            f => f["mortal_recovery_percent"] = 101,
            f => f["mortal_recovery_turns"] = 0,
            f => f["rank_thresholds"] = new JsonArray(30, 10),
            f => f["member_limit"] = 33,
            f => f["recovery_technology_id"] = "missing",
            f => f["profiles"]![0]!["culture_id"] = "missing",
            f => f["cultures"]![0]!["profile_ids"]![0] = "missing",
            f => f["cultures"]![0]!["default_doctrine"] = "last_watch",
            f => f["cultures"]![0]!["profile_ids"]!.AsArray().Add(f["cultures"]![0]!["profile_ids"]![0]!.DeepClone()),
            f => f["profiles"]![1]!["id"] = f["profiles"]![0]!["id"]!.GetValue<string>(),
            f => f["profiles"]![0]!["specialty"] = "wizard",
            f => f["profiles"]![0]!.AsObject().Remove("specialty"),
            f => f.AsObject().Remove("mortal_recovery_percent"),
            f => f["unknown_permission"] = true
        })
        {
            var root = JoinedCore(); mutate(root["fleet_command"]!.AsObject());
            var result = Load(root);
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Field?.Contains("fleet_command", StringComparison.Ordinal) == true);
        }
    }

    [Fact]
    public void AlternativeSpeciesAndCulturesUseTheirDeclaredCommandIdentity()
    {
        Assert.NotNull(Core().FleetCommand);
        var root = JoinedCore();
        var species = root["species"]!.AsArray().Single(n => n!["id"]!.GetValue<string>() == "wenth")!;
        species["id"] = "chorus"; species["name"] = "Chorus"; species["culture_name"] = "New Chorus"; species["command_culture_id"] = "chorus_culture";
        var culture = root["fleet_command"]!["cultures"]!.AsArray().Single(n => n!["id"]!.GetValue<string>() == "kinship")!;
        culture["id"] = "chorus_culture"; culture["name"] = "New Chorus";
        foreach (var profile in root["fleet_command"]!["profiles"]!.AsArray().Where(n => n!["culture_id"]!.GetValue<string>() == "kinship")) profile!["culture_id"] = "chorus_culture";
        var result = Load(root);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        Assert.False(result.Content!.Species.ContainsKey("wenth"));
        Assert.Equal(CommandEmbodiment.ColonyBody, result.Content.Species["chorus"].CommandEmbodiment);
        Assert.Equal("chorus_culture", result.Content.Species["chorus"].CommandCultureId);
        // The embedding round-trip must retain declared identity without inferring it from an ID.
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) ContentWriter.Write(writer, result.Content);
        var roundTrip = Load(JsonNode.Parse(stream.ToArray())!.AsObject());
        Assert.True(roundTrip.Succeeded, string.Join("; ", roundTrip.Errors));
        species["command_embodiment"] = "unknown";
        Assert.False(Load(root).Succeeded);
    }

    [Fact]
    public void RecoveryResearchRequiresIndustrialCoordinationAndNeverAppearsInOldCampaignSaves()
    {
        var c = Core(); Assert.NotNull(c.FleetCommand);
        var created = GalaxyGenerator.CreateNewGame(c, new("small", 42, 7, "harl", AmebasEnabled: false));
        Assert.True(created.Succeeded, created.Error);
        var state = created.State!; var owner = state.Empires[0].Id;
        var result = CommandProcessor.Execute(c, state, new SetResearchTargetCommand(owner, 0, "command_recovery"));
        Assert.False(result.Accepted);
        Assert.Same(state, result.State);
        // Labelled unlocked-tech setup: even satisfying prerequisites cannot add rules8
        // equipment to an existing rules7 campaign using the full current core dataset.
        state.Empires[0].KnownTechnologyIds.AddRange(new[] { "industrial_coordination", "measurement_methods" });
        result = CommandProcessor.Execute(c, state, new SetResearchTargetCommand(owner, 0, "command_recovery"));
        Assert.False(result.Accepted);
        Assert.Equal(RejectionCodes.UnknownTechnology, result.Rejection!.Code);
        string save = Saves.SaveGame.Serialize(c, state);
        Assert.DoesNotContain("command_recovery", save);
        Assert.DoesNotContain("fleet_command", save);
        Assert.DoesNotContain("command_embodiment", save);
        Assert.True(Saves.SaveGame.Deserialize(save).Succeeded);
        var injected = JsonNode.Parse(save)!;
        injected["content"]!["fleet_command"] = JoinedCore()["fleet_command"]!.DeepClone();
        Assert.False(Saves.SaveGame.Deserialize(injected.ToJsonString()).Succeeded);

        var next = GalaxyGenerator.CreateNewGame(c, new("small", 42, 8, "harl", AmebasEnabled: false));
        Assert.True(next.Succeeded, next.Error);
        var nextState = next.State!; var nextOwner = nextState.Empires[0].Id;
        result = CommandProcessor.Execute(c, nextState, new SetResearchTargetCommand(nextOwner, 0, "command_recovery"));
        Assert.False(result.Accepted);
        Assert.Equal(RejectionCodes.PrerequisiteNotMet, result.Rejection!.Code);
        nextState.Empires[0].KnownTechnologyIds.AddRange(new[] { "industrial_coordination", "measurement_methods" });
        result = CommandProcessor.Execute(c, nextState, new SetResearchTargetCommand(nextOwner, 0, "command_recovery"));
        Assert.True(result.Accepted, result.Rejection?.Message);
        Assert.Equal("command_recovery", result.State.Empires[0].ResearchTargetId);
    }

    [Fact]
    public void IndependentRecoveryVectorsMatch()
    {
        foreach (var row in new[]
        {
            (0UL, 1, 1783446105968468059UL, 11856436985063973889UL, new uint[] { 50353123, 3417059036, 1585724331, 4047853392 }),
            (42UL, 1, 10822750475600051645UL, 11856436985063973889UL, new uint[] { 1038021008, 2897589185, 1792309901, 4126569821 }),
            (42UL, 2, 12114079671261706449UL, 11856436985063973895UL, new uint[] { 4047523869, 1149529208, 2343501704, 871191071 }),
            (ulong.MaxValue, 1, 8606524431442022100UL, 11856436985063973889UL, new uint[] { 3245324364, 1208278847, 3848589321, 447352628 })
        })
        {
            var random = FleetInitializer.RecoveryRandom(row.Item1, new(row.Item2));
            Assert.Equal(row.Item3, random.State); Assert.Equal(row.Item4, random.Increment);
            Assert.Equal(row.Item5, Enumerable.Range(0, 4).Select(_ => random.NextUInt32()));
        }
    }

    internal static ContentSet Core()
    {
        var result = Load(JoinedCore());
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        return result.Content!;
    }
    private static ContentLoadResult Load(JsonObject root) => ContentLoader.Load(new[] { new ContentSource("fleet-content-test.json", root.ToJsonString()) }, requireFleets: true);
    internal static JsonObject JoinedCore()
    {
        var root = new JsonObject();
        foreach (string path in Directory.GetFiles(ContentLoaderTests.CoreContentDirectory(), "*.json").OrderBy(x => x, StringComparer.Ordinal))
            foreach (var item in JsonNode.Parse(File.ReadAllText(path))!.AsObject()) root[item.Key] = item.Value!.DeepClone();
        return root;
    }
}
