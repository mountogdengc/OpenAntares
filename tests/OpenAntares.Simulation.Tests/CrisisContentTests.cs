using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Content;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisContentTests
{
    internal static ContentLoadResult Load(JsonObject json) => ContentLoader.Load(new[]{new ContentSource("labelled-crisis-fixture.json",json.ToJsonString())});
    internal static JsonObject Json() => JsonNode.Parse(File.ReadAllText(CompatibilityFixtures.PathFor("crisis-content.json")))!.AsObject();

    [Fact]
    public void ApprovedSingleFamilyLoadsAsOrdinaryContent()
    {
        var result=Load(Json());
        Assert.True(result.Succeeded,string.Join("; ",result.Errors));
    }

    [Fact]
    public void CurrentCoreRequiresTheExactApprovedFamily()
    {
        var directory=ContentLoaderTests.CoreContentDirectory();
        var actual=JsonNode.Parse(File.ReadAllText(Path.Combine(directory,"plagues.json")))!["plagues"]!.ToJsonString();
        var expected=Json()["plagues"]!.ToJsonString();
        Assert.Equal(expected,actual);
        var sources=Directory.GetFiles(directory,"*.json").Select(p=>new ContentSource(Path.GetFileName(p),File.ReadAllText(p)));
        var loaded=ContentLoader.Load(sources,requireSpecies:true,requireRivals:true,requireDevelopment:true,requireCrises:true);
        Assert.True(loaded.Succeeded,string.Join("; ",loaded.Errors));Assert.Single(loaded.Content!.Plagues);
    }

    [Theory]
    [InlineData("check_interval",0)] [InlineData("grace_turns",0)]
    [InlineData("purge_denominator",0)] [InlineData("illness_factor_bp",10001)]
    [InlineData("quarantine_factor_bp",-1)] [InlineData("civic_max_level",101)]
    [InlineData("initial_severity",101)] [InlineData("check_chance_percent",101)]
    public void InvalidBoundsFailUsefully(string field,int value)
    {
        var json=Json();json["plagues"]![0]![field]=value;
        var result=Load(json);Assert.False(result.Succeeded);
        Assert.Contains(result.Errors,e=>e.Field?.Contains(field)==true);
    }

    [Fact]
    public void UnknownParameterIsRejected()
    {
        var json=Json();json["plagues"]![0]!["mystery"]=1;
        Assert.False(Load(json).Succeeded);
    }

    [Fact]
    public void RequiredSingleFamilyCannotBeMissingOrMultiplied()
    {
        var json=Json();json.Remove("plagues");
        Assert.False(ContentLoader.Load(new[]{new ContentSource("missing.json",json.ToJsonString())},requireCrises:true).Succeeded);
        json=Json();var duplicate=json["plagues"]![0]!.DeepClone();duplicate["id"]="other_family";json["plagues"]!.AsArray().Add(duplicate);
        Assert.False(Load(json).Succeeded);
        json=Json();json["plagues"]!.AsArray().Add(json["plagues"]![0]!.DeepClone());Assert.False(Load(json).Succeeded);
    }

    [Theory]
    [InlineData("initial_severity",101)] [InlineData("mortality_threshold",101)]
    [InlineData("civic_factor_loss_bp",4000)] [InlineData("cure_per_population",9223372036854775807L)]
    public void ImpossibleCombinedParametersAreRejected(string field,long value)
    {
        var json=Json();json["plagues"]![0]![field]=value;Assert.False(Load(json).Succeeded);
    }

    [Fact]
    public void FamilyMayBeRenamedAndRetunedWithoutCodeChanges()
    {
        var json=Json();json["plagues"]![0]!["id"]="silver_fever";json["plagues"]![0]!["grace_turns"]=3;
        var result=Load(json);Assert.True(result.Succeeded,string.Join("; ",result.Errors));
        Assert.Equal(3,result.Content!.Plagues["silver_fever"].GraceTurns);
    }
}
