using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Content;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacyContentTests
{
    [Fact] public void CoreDiplomacyDefinitionIsExactAndStrict()
    {
        var loaded=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory());
        Assert.True(loaded.Succeeded,string.Join("; ",loaded.Errors));var d=loaded.Content!.Diplomacy;
        Assert.NotNull(d);Assert.Equal("cooperation_v1",d!.AlgorithmId);
        Assert.Equal(500,d.SetupCost);
        Assert.Equal(100,d.TradeOutput);
        Assert.Equal(3,d.Warmup);
        Assert.Equal(10,d.Minimum);
        Assert.Equal(2,d.Notice);
        Assert.Equal(3,d.OfferLifetime);
        Assert.Equal(5,d.Cooldown);
        Assert.Equal(10,d.StreakCap);
        Assert.Equal(2,d.StreakStep);
        Assert.Equal(10,d.CancellationPenalty);
        Assert.Equal(10,d.MemoryLifetime);
        Assert.Equal(0,d.AccordThreshold);
        Assert.Equal(10,d.TradeThreshold);
    }

    [Theory]
    [InlineData("setup_cost",0)][InlineData("trade_output",1000001)]
    [InlineData("warmup",0)][InlineData("minimum",1001)][InlineData("notice",-1)]
    [InlineData("offer_lifetime",0)][InlineData("cooldown",0)][InlineData("streak_cap",0)]
    [InlineData("streak_step",-1)][InlineData("cancellation_penalty",1001)]
    [InlineData("memory_lifetime",0)][InlineData("accord_threshold",21)][InlineData("trade_threshold",21)]
    public void InvalidBoundHasSourceAndField(string field,int value)
    {
        var node=JsonNode.Parse(File.ReadAllText(Path.Combine(ContentLoaderTests.CoreContentDirectory(),"diplomacy.json")))!;
        node["diplomacy"]![field]=value;var loaded=Load(node.ToJsonString());
        Assert.False(loaded.Succeeded);Assert.Contains(loaded.Errors,e=>e.File=="diplomacy-test.json"&&e.Field=="diplomacy."+field);
    }
    [Theory]
    [InlineData("missing")][InlineData("unknown")][InlineData("duplicate")][InlineData("algorithm")]
    public void MalformedDefinitionIsRejected(string kind)
    {
        var node=JsonNode.Parse(File.ReadAllText(Path.Combine(ContentLoaderTests.CoreContentDirectory(),"diplomacy.json")))!;
        if(kind=="missing")node["diplomacy"]!.AsObject().Remove("setup_cost");
        if(kind=="unknown")node["diplomacy"]!["secret_bonus"]=100;
        if(kind=="algorithm")node["diplomacy"]!["algorithm_id"]="unknown_v2";
        string json=node.ToJsonString();if(kind=="duplicate")json=json.Replace("\"setup_cost\":500","\"setup_cost\":500,\"setup_cost\":500");
        var loaded=Load(json);Assert.False(loaded.Succeeded);Assert.Contains(loaded.Errors,e=>e.File=="diplomacy-test.json");
    }
    [Fact] public void RequiredDiplomacyRejectsMissingDefinitionEvenForSoloContent()
    {var loaded=ContentLoader.Load(Sources(),requireDiplomacy:true);Assert.False(loaded.Succeeded);Assert.Contains(loaded.Errors,e=>e.Field=="diplomacy");}
    [Fact] public void AlternateEmbeddedValuesRoundTripAndOldRulesOmitDiplomacy()
    {
        var node=JsonNode.Parse(File.ReadAllText(Path.Combine(ContentLoaderTests.CoreContentDirectory(),"diplomacy.json")))!;
        node["diplomacy"]!["setup_cost"]=700;var loaded=Load(node.ToJsonString());Assert.True(loaded.Succeeded);
        using var stream=new MemoryStream();using(var writer=new System.Text.Json.Utf8JsonWriter(stream))ContentWriter.Write(writer,loaded.Content!);
        var embedded=ContentLoader.Load(new[]{new ContentSource("embedded.json",System.Text.Encoding.UTF8.GetString(stream.ToArray()))},requireDiplomacy:true);
        Assert.True(embedded.Succeeded,string.Join("; ",embedded.Errors));Assert.Equal(700,embedded.Content!.Diplomacy!.SetupCost);
        using var oldStream=new MemoryStream();using(var writer=new System.Text.Json.Utf8JsonWriter(oldStream))ContentWriter.Write(writer,loaded.Content!,10);
        Assert.DoesNotContain("\"diplomacy\"",System.Text.Encoding.UTF8.GetString(oldStream.ToArray()));
    }
    private static ContentSource[] Sources()=>Directory.GetFiles(ContentLoaderTests.CoreContentDirectory(),"*.json").Where(p=>Path.GetFileName(p)!="diplomacy.json").Select(p=>new ContentSource(Path.GetFileName(p),File.ReadAllText(p))).ToArray();
    private static ContentLoadResult Load(string json)=>ContentLoader.Load(Sources().Append(new ContentSource("diplomacy-test.json",json)),requireDiplomacy:true);
}
