using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Tests;

internal static class CompatibilityFixtures
{
    internal static string PathFor(string name) => Path.Combine(ContentLoaderTests.CoreContentDirectory(), "..", "..", "tests", "OpenAntares.Simulation.Tests", "Fixtures", name);
    internal static LoadedGame LoadRules2() => SaveGame.ReadFile(PathFor("legacy-rules2-v3-save.json")).Game ?? throw new InvalidOperationException("Missing or invalid genuine rules-2 fixture.");
    internal static ContentSet Rules2Content() => LoadRules2().Content;
    internal static LoadedGame LoadRules3() => SaveGame.ReadFile(PathFor("legacy-rules3-v4-save.json")).Game ?? throw new InvalidOperationException("Missing or invalid genuine rules-3 fixture.");
    internal static LoadedGame LoadPassiveAiRules3() => SaveGame.ReadFile(PathFor("passive-ai-rules3-v4-save.json")).Game ?? throw new InvalidOperationException("Missing or invalid pre-rival passive-Ai fixture.");
    internal static ImmutableArray<string> Rules3Trace(bool passiveAi) => JsonSerializer.Deserialize<ImmutableArray<string>>(File.ReadAllText(PathFor(passiveAi ? "passive-ai-rules3-v4-continuation.json" : "legacy-rules3-v4-continuation.json")));
    internal static string CanonicalWithoutSchema(ContentSet content, GameState state)
    {
        JsonObject root = JsonNode.Parse(SaveGame.Serialize(content, state))!.AsObject();
        root.Remove("schema_version");
        return root.ToJsonString();
    }
    internal static ImmutableArray<string> ExpectedTrace() => JsonSerializer.Deserialize<ImmutableArray<string>>(File.ReadAllText(PathFor("legacy-rules2-v3-continuation.json")));
}
