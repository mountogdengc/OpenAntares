using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Content;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class ScoutingContentTests
{
    internal static JsonObject Json()=>JsonNode.Parse(File.ReadAllText(CompatibilityFixtures.PathFor("scouting-content.json")))!.AsObject();
    internal static ContentLoadResult Load(JsonObject json,bool required=true)=>ContentLoader.Load(new[]{new ContentSource("scouting-fixture.json",json.ToJsonString())},requireScouting:required);
    internal static string Write(ContentSet c,int rules){using var stream=new MemoryStream();using(var writer=new Utf8JsonWriter(stream))ContentWriter.Write(writer,c,rules);return Encoding.UTF8.GetString(stream.ToArray());}
    [Fact] public void SevenRetainsEveryEarlierFeatureAndCurrentEightIncludesIt()
    {
        Assert.True(Rules.HasScouting(7));Assert.False(Rules.HasScouting(6));Assert.Equal(10,Rules.CurrentVersion);
        Assert.True(Rules.IsSupported(7));Assert.True(Rules.HasExpansion(7));Assert.True(Rules.HasSpecies(7));Assert.True(Rules.HasRivals(7));Assert.True(Rules.HasDevelopment(7));Assert.True(Rules.HasCrises(7));
    }
    [Fact] public void ApprovedHazardLoadsAndRoundTripsWithExactValues()
    {
        var result=Load(Json());Assert.True(result.Succeeded,string.Join("; ",result.Errors));
        var json=JsonNode.Parse(Write(result.Content!,7))!;var d=json["space_ameba"]!;
        Assert.Equal(200,d["pursuit_speed"]!.GetValue<int>());Assert.Equal(600,d["research_reward"]!.GetValue<long>());
        Assert.Equal(95,d["late_escape"]!["maximum_percent"]!.GetValue<int>());Assert.Equal(50,d["study"]!["relative_speed_coefficient"]!.GetValue<int>());
        Assert.True(Load(json.AsObject()).Succeeded);
    }
    [Fact] public void LegacyContentWritingOmitsTheWholeHazardSection()
    {
        var result=Load(Json());Assert.True(result.Succeeded,string.Join("; ",result.Errors));
        var old=CrisisFixtures.Content();
        foreach(int rules in Enumerable.Range(1,6)){Assert.DoesNotContain("space_ameba",Write(result.Content!,rules));Assert.Equal(Write(old,rules),Write(result.Content!,rules));}
    }
    [Theory] [InlineData("pursuit_speed",0)] [InlineData("pursuit_speed",-1)] [InlineData("research_reward",0)]
    [InlineData("warning_success",100)] [InlineData("reward_count",1)] [InlineData("mystery",3)]
    public void InvalidOrInvariantOverrideFieldsRejectWithLocation(string field,int value)
    {
        var json=Json();json["space_ameba"]![field]=value;var result=Load(json);Assert.False(result.Succeeded);Assert.Contains(result.Errors,e=>e.File=="scouting-fixture.json"&&e.Field!.Contains(field));
    }
    [Theory] [InlineData("base_percent",-1)] [InlineData("base_percent",101)] [InlineData("relative_speed_coefficient",101)]
    [InlineData("minimum_percent",96)] [InlineData("maximum_percent",4)]
    public void InvalidProbabilityShapeRejects(string field,int value)
    {
        var json=Json();json["space_ameba"]!["late_escape"]![field]=value;Assert.False(Load(json).Succeeded);
    }
    [Fact] public void MissingDuplicateAndWrongTypesReject()
    {
        var json=Json();json.Remove("space_ameba");Assert.False(Load(json).Succeeded);
        json=Json();json["space_ameba"]!["pursuit_speed"]="200";Assert.False(Load(json).Succeeded);
        json=Json();json["space_ameba"]!["study"]!["minimum_percent"]=null;Assert.False(Load(json).Succeeded);
        json=Json();var result=ContentLoader.Load(new[]{new ContentSource("a.json",json.ToJsonString()),new ContentSource("b.json",new JsonObject{{"space_ameba",json["space_ameba"]!.DeepClone()}}.ToJsonString())},requireScouting:true);Assert.False(result.Succeeded);
    }
    [Fact] public void OrdinaryModsCanChangeBalanceWithoutChangingGuarantees()
    {
        var json=Json();json["space_ameba"]!["pursuit_speed"]=int.MaxValue;json["space_ameba"]!["research_reward"]=900;
        foreach(string name in new[]{"late_escape","study"}){json["space_ameba"]![name]!["minimum_percent"]=0;json["space_ameba"]![name]!["maximum_percent"]=100;}
        Assert.True(Load(json).Succeeded);
    }
}
