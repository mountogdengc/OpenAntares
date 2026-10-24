using System.IO;
using System.Linq;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class RivalCompatibilityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenuineRules3AndPassiveAiContinueExactly(bool passiveAi)
    {
        string name = passiveAi ? "passive-ai-rules3-v4-save.json" : "legacy-rules3-v4-save.json";
        Assert.True(File.Exists(CompatibilityFixtures.PathFor(name)), "Pre-rival source fixture missing; capture with the unchanged engine first.");
        var game = passiveAi ? CompatibilityFixtures.LoadPassiveAiRules3() : CompatibilityFixtures.LoadRules3();
        Assert.Equal(3, game.State.RulesVersion);
        Assert.Equal(2, game.State.Colonies.Count);
        Assert.Equal(passiveAi ? 1 : 0, game.State.Empires.Count(e => e.Controller == ControllerKind.Ai));
        if (passiveAi)
        {
            var ai = game.State.Empires.Single(e => e.Controller == ControllerKind.Ai);
            Assert.Single(game.State.Colonies, c => c.EmpireId == ai.Id);
            Assert.Contains(game.State.Vessels, v => v.EmpireId == ai.Id);
        }
        var trace = CompatibilityFixtures.Rules3Trace(passiveAi);
        Assert.Equal(4, trace.Length);
        var state = game.State;
        foreach (string expected in trace)
        {
            var next = TurnResolver.Resolve(game.Content, state);
            Assert.True(next.Succeeded, string.Join("; ", next.Errors));
            Assert.Equal(expected, CompatibilityFixtures.CanonicalWithoutSchema(game.Content, next.State));
            var reloaded = SaveGame.Deserialize(SaveGame.Serialize(game.Content, next.State));
            Assert.True(reloaded.Succeeded, string.Join("; ", reloaded.Errors));
            Assert.Equal(expected, CompatibilityFixtures.CanonicalWithoutSchema(reloaded.Game!.Content, reloaded.Game.State));
            state = reloaded.Game.State;
        }
    }
}
