using System.IO;
using OpenAntares.Simulation.Turns;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class SpeciesCompatibilityTests
{
    [Fact]
    public void GenuineRules2CampaignMatchesFourPreSpeciesContinuations()
    {
        Assert.True(File.Exists(CompatibilityFixtures.PathFor("legacy-rules2-v3-save.json")), "Genuine pre-species fixture is missing.");
        var game = CompatibilityFixtures.LoadRules2();
        Assert.Equal(2, game.State.RulesVersion);
        Assert.Equal(9, game.State.Turn);
        Assert.Equal(2, game.State.Colonies.Count);
        string original = File.ReadAllText(CompatibilityFixtures.PathFor("legacy-rules2-v3-save.json"));
        Assert.DoesNotContain("species_id", original);
        Assert.DoesNotContain("environment_id", original);
        var state = game.State;
        var trace = CompatibilityFixtures.ExpectedTrace();
        Assert.Equal(4, trace.Length);
        foreach (string expected in trace)
        {
            var next = TurnResolver.Resolve(game.Content, state);
            Assert.True(next.Succeeded, string.Join("; ", next.Errors));
            state = next.State;
            Assert.Equal(expected, CompatibilityFixtures.CanonicalWithoutSchema(game.Content, state));
        }
    }
}
