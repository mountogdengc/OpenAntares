using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class RivalContentTests
{
    [Fact]
    public void ExplicitRivalLoadingRequiresPolicyAndSpeciesWhileLegacyDoesNot()
    {
        Assert.True(ContentLoader.Load(Sources()).Succeeded);
        Assert.False(ContentLoader.Load(Sources(), requireRivals: true).Succeeded);
        var noSpecies = Sources().Where(s => s.Name is not ("species.json" or "environments.json"));
        Assert.False(ContentLoader.Load(noSpecies.Append(new("policy.json", """{"rival_ai":{"algorithm_id":"expansion_v1","growth_support_workers":0,"resurvey_min_age":7}}""")), requireRivals: true).Succeeded);
    }
    internal static ContentSource[] Sources() => Directory.GetFiles(ContentLoaderTests.CoreContentDirectory(), "*.json")
        .Where(p => Path.GetFileName(p) != "rival_ai.json")
        .Select(p => new ContentSource(Path.GetFileName(p), File.ReadAllText(p))).ToArray();

    [Fact]
    public void PolicyValuesAreExplicitAndEmbeddedReady()
    {
        var result = ContentLoader.Load(Sources().Append(new("policy.json", """{"rival_ai":{"algorithm_id":"expansion_v1","growth_support_workers":1,"resurvey_min_age":3}}""")));
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) ContentWriter.Write(writer, result.Content!);
        using var doc = JsonDocument.Parse(stream.ToArray());
        var policy = doc.RootElement.GetProperty("rival_ai");
        Assert.Equal("expansion_v1", policy.GetProperty("algorithm_id").GetString());
        Assert.Equal(1, policy.GetProperty("growth_support_workers").GetInt32());
        Assert.Equal(3, policy.GetProperty("resurvey_min_age").GetInt32());
    }

    [Theory]
    [InlineData("wrong", 1, "3")]
    [InlineData("expansion_v1", -1, "3")]
    [InlineData("expansion_v1", 2, "3")]
    [InlineData("expansion_v1", 1, "0")]
    [InlineData("expansion_v1", 1, "-1")]
    [InlineData("expansion_v1", 1, "2147483648")]
    public void InvalidPolicyIsRejectedWithLocation(string algorithm, int growth, string age)
    {
        string json = "{\"rival_ai\":{\"algorithm_id\":\"" + algorithm + "\",\"growth_support_workers\":" + growth + ",\"resurvey_min_age\":" + age + "}}";
        var result = ContentLoader.Load(Sources().Append(new("bad-policy.json", json)));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.File == "bad-policy.json");
    }

    [Fact]
    public void DuplicatePolicyIsNotSilentlyOverwritten()
    {
        const string json = """{"rival_ai":{"algorithm_id":"expansion_v1","growth_support_workers":0,"resurvey_min_age":7}}""";
        var result = ContentLoader.Load(Sources().Concat(new[] { new ContentSource("a.json", json), new ContentSource("b.json", json) }));
        Assert.False(result.Succeeded);
    }
}
