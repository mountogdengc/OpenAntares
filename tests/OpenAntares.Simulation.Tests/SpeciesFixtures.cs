using System.IO;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Content;

namespace OpenAntares.Simulation.Tests;
internal static class SpeciesFixtures
{
    internal static string ContentJson() => File.ReadAllText(CompatibilityFixtures.PathFor("species-test-content.json"));
    internal static ContentSet Content()
    {
        var loaded = ContentLoader.Load(new[] { new ContentSource("species-test.json", ContentJson()) });
        return loaded.Content ?? throw new System.InvalidOperationException(string.Join("; ", loaded.Errors));
    }
    internal static GameState AtHome(string id)
    {
        var content = Content(); var sp = content.Species[id]; var state = ExpansionFixtures.AtHome();
        state.RulesVersion = 3; state.Empires[0].SpeciesId = id;
        foreach(var p in state.Planets) p.EnvironmentId = "shelf_sea";
        var c = state.Colonies[0]; c.HabitatMode = "native"; c.Population = sp.StartingPopulation; c.Workforce = sp.StartingWorkforce;
        var observation = state.Empires[0].Surveys[0];
        observation.Planets = observation.Planets.Select(p => p with { EnvironmentId = "shelf_sea" }).ToList();
        return state;
    }
    internal static string Snapshot(GameState state) => JsonSerializer.Serialize(state);
}
