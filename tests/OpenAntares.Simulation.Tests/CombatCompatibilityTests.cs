using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Saves;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CombatCompatibilityTests
{
    [Fact]
    public void FrozenRules8ManifestExistsAndMatchesSource()
    {
        string root = CompatibilityFixtures.PathFor("rules8-combat-baselines");
        string manifestPath = Path.Combine(root, "manifest.json");
        Assert.True(File.Exists(manifestPath), "Freeze genuine unchanged rules8 sources and futures before combat product changes.");
        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))!;
        Assert.Equal(8, manifest["rulesVersion"]!.GetValue<int>());
        Assert.Equal(9, manifest["writerVersion"]!.GetValue<int>());
        Assert.Equal(48, manifest["generatedSources"]!.GetValue<int>());
        Assert.Equal(192, manifest["generatedCheckpoints"]!.GetValue<int>());
        Assert.NotEmpty(manifest["productTree"]!.GetValue<string>());
        foreach (var file in manifest["files"]!.AsObject())
            Assert.Equal(file.Value!.GetValue<string>(), Hash(Path.Combine(root, file.Key)));
        foreach (var entry in manifest["cases"]!.AsArray())
        {
            var loaded = SaveGame.ReadFile(Path.Combine(root, entry!["save"]!.GetValue<string>()));
            Assert.True(loaded.Succeeded, string.Join("; ", loaded.Errors));
            Assert.Equal(8, loaded.Game!.State.RulesVersion);
            var session = new GameSession(loaded.Game.Content, loaded.Game.State);
            var commands = JsonNode.Parse(File.ReadAllText(Path.Combine(root, entry["commands"]!.GetValue<string>())))!.AsArray();
            var expected = JsonNode.Parse(File.ReadAllText(Path.Combine(root, entry["checkpoints"]!.GetValue<string>())))!.AsObject();
            Assert.Equal(10, commands.Count);
            for (int boundary = 0; boundary < commands.Count; boundary++)
            {
                int before = session.State.Turn;
                foreach (var step in commands[boundary]!.AsArray())
                {
                    var outcome = session.Execute(Decode(step!));
                    Assert.True(outcome.Accepted, outcome.Rejection?.Message);
                }
                Assert.Equal(before + 1, session.State.Turn);
                if (boundary is 2 or 9)
                {
                    string canonical = CompatibilityFixtures.CanonicalWithoutSchema(session.Content, session.State);
                    Assert.Equal(expected[(boundary + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)]!.GetValue<string>(), canonical);
                    var roundTrip = SaveGame.Deserialize(SaveGame.Serialize(session.Content, session.State));
                    Assert.True(roundTrip.Succeeded, string.Join("; ", roundTrip.Errors));
                    Assert.Equal(canonical, CompatibilityFixtures.CanonicalWithoutSchema(roundTrip.Game!.Content, roundTrip.Game.State));
                    session = new(roundTrip.Game.Content, roundTrip.Game.State);
                }
            }
        }
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    private static Command Decode(JsonNode step)
    {
        string json = step["json"]!.GetValue<string>();
        return step["kind"]!.GetValue<string>() switch
        {
            nameof(SetReadyCommand) => JsonSerializer.Deserialize<SetReadyCommand>(json)!,
            nameof(AcknowledgeCrisisCommand) => JsonSerializer.Deserialize<AcknowledgeCrisisCommand>(json)!,
            nameof(AcknowledgeAmebaCommand) => JsonSerializer.Deserialize<AcknowledgeAmebaCommand>(json)!,
            nameof(SetAmebaRiskOrderCommand) => JsonSerializer.Deserialize<SetAmebaRiskOrderCommand>(json)!,
            nameof(SetScoutRetreatOrderCommand) => JsonSerializer.Deserialize<SetScoutRetreatOrderCommand>(json)!,
            nameof(SetScoutPostureCommand) => JsonSerializer.Deserialize<SetScoutPostureCommand>(json)!,
            nameof(SetMoveOrderCommand) => JsonSerializer.Deserialize<SetMoveOrderCommand>(json)!,
            nameof(SetFoundingOrderCommand) => JsonSerializer.Deserialize<SetFoundingOrderCommand>(json)!,
            nameof(SetProductionTargetCommand) => JsonSerializer.Deserialize<SetProductionTargetCommand>(json)!,
            nameof(SetResearchTargetCommand) => JsonSerializer.Deserialize<SetResearchTargetCommand>(json)!,
            nameof(FleetCommand) => DecodeFleet(step),
            _ => throw new InvalidOperationException("Unknown frozen baseline command; never silently drop it.")
        };
    }
    private static FleetCommand DecodeFleet(JsonNode step)
    {
        var data = JsonNode.Parse(step["json"]!.GetValue<string>())!;
        Assert.Equal(nameof(FleetAction.MoveSelection), step["actionKind"]!.GetValue<string>());
        return new(JsonSerializer.Deserialize<EmpireId>(data["Issuer"]!.ToJsonString()),
            data["ExpectedTurn"]!.GetValue<int>(), JsonSerializer.Deserialize<FleetAction.MoveSelection>(step["actionJson"]!.GetValue<string>())!)
            { Fingerprint = data["Fingerprint"]?.GetValue<string>() };
    }
}
