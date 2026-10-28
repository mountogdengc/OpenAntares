using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Saves;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class FleetCompatibilityTests
{
    [Fact]
    public void GenuineRules7SourcesAndFuturesAreFrozen()
    {
        string root = CompatibilityFixtures.PathFor("rules7-fleet-baselines");
        string path = Path.Combine(root, "manifest.json");
        Assert.True(File.Exists(path), "Capture genuine rules7 sources and four old-engine futures before any product change.");
        var manifest = JsonNode.Parse(File.ReadAllText(path))!;
        Assert.Equal(7, manifest["rulesVersion"]!.GetValue<int>());
        Assert.Equal(4, manifest["turns"]!.GetValue<int>());
        Assert.True(manifest["generatedSources"]!.GetValue<int>() >= 48);
        Assert.NotEmpty(manifest["cases"]!.AsArray());
        foreach (var entry in manifest["cases"]!.AsArray())
        {
            string save = Path.Combine(root, entry!["save"]!.GetValue<string>());
            Assert.Equal(entry["saveSha256"]!.GetValue<string>(), Hash(save));
            var loaded = SaveGame.ReadFile(save);
            Assert.True(loaded.Succeeded, string.Join("; ", loaded.Errors));
            Assert.Equal(7, loaded.Game!.State.RulesVersion);
            var session = new GameSession(loaded.Game.Content, loaded.Game.State);
            var reloadedSession = new GameSession(loaded.Game.Content, loaded.Game.State.Clone());
            var futures = JsonNode.Parse(File.ReadAllText(Path.Combine(root, entry["futures"]!.GetValue<string>())))!.AsArray();
            var commands = JsonNode.Parse(File.ReadAllText(Path.Combine(root, entry["commands"]!.GetValue<string>())))!.AsArray();
            Assert.Equal(4, futures.Count);
            Assert.Equal(4, commands.Count);
            for (int turn = 0; turn < 4; turn++)
            {
                foreach (var step in commands[turn]!.AsArray())
                {
                    Command command = Decode(step!);
                    foreach (var target in new[] { session, reloadedSession })
                    {
                        var result = target.Execute(command);
                        Assert.True(result.Accepted, result.Rejection?.Message);
                    }
                }
                string expected = futures[turn]!.GetValue<string>();
                Assert.Equal(expected, CompatibilityFixtures.CanonicalWithoutSchema(session.Content, session.State));
                Assert.Equal(expected, CompatibilityFixtures.CanonicalWithoutSchema(reloadedSession.Content, reloadedSession.State));
                var roundTrip = SaveGame.Deserialize(SaveGame.Serialize(reloadedSession.Content, reloadedSession.State));
                Assert.True(roundTrip.Succeeded, string.Join("; ", roundTrip.Errors));
                reloadedSession = new(roundTrip.Game!.Content, roundTrip.Game.State);
            }
        }
        foreach (var source in manifest["uiSources"]!.AsArray())
            Assert.Equal(source!["sha256"]!.GetValue<string>(), Hash(Path.Combine(root, source["copy"]!.GetValue<string>())));
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static Command Decode(JsonNode step)
    {
        string data = step["json"]!.GetValue<string>();
        return step["kind"]!.GetValue<string>() switch
        {
            nameof(SetReadyCommand) => JsonSerializer.Deserialize<SetReadyCommand>(data)!,
            nameof(AcknowledgeCrisisCommand) => JsonSerializer.Deserialize<AcknowledgeCrisisCommand>(data)!,
            nameof(AcknowledgeAmebaCommand) => JsonSerializer.Deserialize<AcknowledgeAmebaCommand>(data)!,
            nameof(SetAmebaRiskOrderCommand) => JsonSerializer.Deserialize<SetAmebaRiskOrderCommand>(data)!,
            nameof(SetScoutRetreatOrderCommand) => JsonSerializer.Deserialize<SetScoutRetreatOrderCommand>(data)!,
            nameof(SetMoveOrderCommand) => JsonSerializer.Deserialize<SetMoveOrderCommand>(data)!,
            _ => throw new InvalidOperationException("Unknown recorded genuine-future command.")
        };
    }
}
