using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Content;
using Xunit;
namespace OpenAntares.Simulation.Tests;

public class ExpansionContentTests
{
    private static string Serialize(ContentSet c) { using var stream = new MemoryStream(); using (var w = new System.Text.Json.Utf8JsonWriter(stream)) ContentWriter.Write(w, c); return System.Text.Encoding.UTF8.GetString(stream.ToArray()); }
    internal static string CorePath => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../content/core"));
    internal static ContentLoadResult Load(JsonObject root) => ContentLoader.Load(new[] { new ContentSource("case.json", root.ToJsonString()) });
    internal static JsonObject Json(ContentSet c) => JsonNode.Parse(Serialize(c))!.AsObject();
    [Fact]
    public void CoreDefinitionsMatchApprovedFixtures()
    {
        var r = ContentLoader.LoadDirectory(CorePath); Assert.True(r.Succeeded, string.Join("\n", r.Errors));
        var c = r.Content!; var scout = c.Vessels["scout"]; var settler = c.Vessels["settler"];
        Assert.Equal((800L, 300, 0), (scout.Cost, scout.Speed, scout.EmbarkPopulation));
        Assert.Equal((2400L, 200, 2, 4), (settler.Cost, settler.Speed, settler.EmbarkPopulation, settler.MinimumSourcePopulation));
        Assert.False(scout.CanFoundColony); Assert.True(settler.CanFoundColony);
        Assert.All(c.PlanetTypes.Values, t => Assert.True(t.Colonizable));
        Assert.True(Load(Json(c)).Succeeded);
    }
    [Fact] public void OldContentNeedsNoExpansionSections() => Assert.True(Load(Json(Fixtures.PrototypeContent())).Succeeded);
    [Theory]
    [InlineData("vessels")]
    [InlineData("expansion")]
    public void PartialExpansionRejects(string missing) { var j = Json(ExpansionFixtures.Content()); j.Remove(missing); Assert.False(Load(j).Succeeded); }
    [Theory]
    [InlineData("speed", 0)]
    [InlineData("speed", -1)]
    [InlineData("cost", 0)]
    [InlineData("cost", -1)]
    [InlineData("embark_population", -1)]
    [InlineData("minimum_source_population", 1)]
    public void InvalidRoleHasLocatedErrors(string field, int value)
    {
        var j = Json(ExpansionFixtures.Content()); j["vessels"]![1]![field] = value;
        var r = Load(j); Assert.False(r.Succeeded); Assert.Contains(r.Errors, e => e.File == "case.json");
    }
    [Fact] public void UnknownStarterRejects() { var j = Json(ExpansionFixtures.Content()); j["expansion"]!["starting_vessel"] = "missing"; Assert.False(Load(j).Succeeded); }
    [Fact] public void DuplicateRolesReject() { var j = Json(ExpansionFixtures.Content()); j["vessels"]!.AsArray().Add(j["vessels"]![0]!.DeepClone()); Assert.False(Load(j).Succeeded); }
    [Fact] public void ColonizableZeroSupportRejects() { var j = Json(ExpansionFixtures.Content()); j["planet_types"]![0]!["support_per_worker"] = 0; Assert.False(Load(j).Succeeded); }
    [Fact]
    public void ExpansionBoundsCountAllPotentialColonies()
    {
        var legacy = Json(Fixtures.PrototypeContent()); legacy["planet_types"]![0]!["research_per_worker"] = 10000000000000000L;
        Assert.True(Load(legacy).Succeeded);
        var expanded = Json(ExpansionFixtures.Content()); expanded["planet_types"]![0]!["research_per_worker"] = 10000000000000000L;
        Assert.False(Load(expanded).Succeeded);
    }
}
