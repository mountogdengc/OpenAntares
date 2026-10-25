using System.IO;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisCompatibilityTests
{
    [Fact]
    public void GenuineRules5CampaignContinuesExactly()
    {
        string path = CompatibilityFixtures.PathFor("legacy-rules5-v6-save.json");
        Assert.True(File.Exists(path), "Genuine rules5 fixture absent; capture with unchanged engine first.");
        var loaded = SaveGame.ReadFile(path);
        Assert.True(loaded.Succeeded, string.Join("; ", loaded.Errors));
        var game = loaded.Game!;
        Assert.Equal(5, game.State.RulesVersion);
        Assert.Equal(24, game.State.Turn);
        Assert.Equal(6, game.State.Colonies.Count);
        Assert.Equal("expansion_v1", Assert.Single(game.State.Empires, e => e.Controller == ControllerKind.Ai).AiPolicyId);
        var trace = JsonSerializer.Deserialize<string[]>(File.ReadAllText(CompatibilityFixtures.PathFor("legacy-rules5-v6-continuation.json")))!;
        Assert.Equal(4, trace.Length);
        foreach (string expected in trace)
        {
            var session = new GameSession(game.Content, game.State);
            var human = game.State.Empires.Single(e => e.Controller == ControllerKind.Human);
            var next = session.Execute(new SetReadyCommand(human.Id, game.State.Turn, true));
            Assert.True(next.Accepted, next.Rejection?.Message);
            Assert.Equal(expected, CompatibilityFixtures.CanonicalWithoutSchema(session.Content, session.State));
            var reload = SaveGame.Deserialize(SaveGame.Serialize(session.Content, session.State));
            Assert.True(reload.Succeeded, string.Join("; ", reload.Errors));
            Assert.Equal(expected, CompatibilityFixtures.CanonicalWithoutSchema(reload.Game!.Content, reload.Game.State));
            game = reload.Game;
        }
    }
}
