using System.IO;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class ColonyCompatibilityTests
{
    [Fact]
    public void GenuineActiveAiRules4ContinuesExactly()
    {
        string path = CompatibilityFixtures.PathFor("legacy-rules4-v5-save.json");
        Assert.True(File.Exists(path), "Genuine rules4 fixture absent; capture with unchanged engine first.");
        using var source = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(5, source.RootElement.GetProperty("schema_version").GetInt32());
        var game = CompatibilityFixtures.LoadRules4();
        Assert.Equal(4, game.State.RulesVersion);
        Assert.Equal(14, game.State.Turn);
        Assert.Equal(5, game.State.Colonies.Count);
        Assert.Single(game.State.Empires, e => e.Controller == ControllerKind.Ai);
        Assert.Equal("expansion_v1", game.State.Empires.Single(e => e.Controller == ControllerKind.Ai).AiPolicyId);
        var trace = CompatibilityFixtures.Rules4Trace();
        Assert.Equal(4, trace.Length);
        foreach (string expected in trace)
        {
            var human = game.State.Empires.Single(e => e.Controller == ControllerKind.Human);
            var next = CommandProcessor.Execute(game.Content, game.State, new SetReadyCommand(human.Id, game.State.Turn, true));
            Assert.True(next.Accepted, next.Rejection?.Message);
            Assert.Equal(expected, CompatibilityFixtures.CanonicalWithoutSchema(game.Content, next.State));
            var reload = SaveGame.Deserialize(SaveGame.Serialize(game.Content, next.State));
            Assert.True(reload.Succeeded, string.Join("; ", reload.Errors));
            Assert.Equal(expected, CompatibilityFixtures.CanonicalWithoutSchema(reload.Game!.Content, reload.Game.State));
            game = reload.Game;
        }
    }
}
