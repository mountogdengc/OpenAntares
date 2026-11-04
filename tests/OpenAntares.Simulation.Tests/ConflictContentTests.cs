using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Content;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictContentTests
{
    private const string DefinitionJson = """
{
  "conflict": {
    "algorithm_id": "limited_conflict_v1",
    "truce": 10,
    "offer_lifetime": 3,
    "declaration_grievance": 30,
    "grievance_peace_lifetime": 20,
    "blockade_retained_percent": 50,
    "peace_hull_target": 2,
    "war_hull_target": 4,
    "military_hull_cap": 6,
    "formation_limit": 4,
    "active_encounter_limit": 64,
    "history_limit": 20,
    "round_history_limit": 20,
    "contact_gate": 5,
    "claim_gate": 5,
    "peace_earliest": 5,
    "peace_deadline": 15,
    "peace_inactivity": 10,
    "peace_cooldown": 5,
    "repair_hull_percent": 75,
    "healthy_cohesion": 60,
    "grouping_command_limit": 2,
    "fleet_command_limit": 1
  }
}
""";
    [Fact] public void StrictDefinitionsSupportAlternateValuesWithoutChangingOldRules()
    {
        var loaded=Load(DefinitionJson);Assert.True(loaded.Succeeded,string.Join("; ",loaded.Errors));
        Assert.NotNull(loaded.Content!.Conflict);Assert.Equal(10,loaded.Content.Conflict!.Truce);Assert.Equal(50,loaded.Content.Conflict.BlockadeRetainedPercent);
        var n=JsonNode.Parse(DefinitionJson)!;n["conflict"]!["truce"]=7;n["conflict"]!["blockade_retained_percent"]=65;
        var alternate=Load(n.ToJsonString());Assert.True(alternate.Succeeded,string.Join("; ",alternate.Errors));
        using var stream=new MemoryStream();using(var w=new Utf8JsonWriter(stream))ContentWriter.Write(w,alternate.Content!);
        var embedded=ContentLoader.Load(new[]{new ContentSource("embedded.json",Encoding.UTF8.GetString(stream.ToArray()))},requireConflict:true);
        Assert.True(embedded.Succeeded,string.Join("; ",embedded.Errors));Assert.Equal(7,embedded.Content!.Conflict!.Truce);Assert.Equal(65,embedded.Content.Conflict.BlockadeRetainedPercent);
        using var old=new MemoryStream();using(var w=new Utf8JsonWriter(old))ContentWriter.Write(w,alternate.Content!,11);
        Assert.DoesNotContain("\"conflict\"",Encoding.UTF8.GetString(old.ToArray()));
        Assert.Equal(12,Rules.CurrentVersion);
    }
    [Fact] public void CoreDefinitionAvailableForCurrentConflictRules()
    {var c=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory());Assert.True(c.Succeeded);Assert.NotNull(c.Content!.Conflict);Assert.Equal(4,c.Content.Conflict!.WarHullTarget);}
    [Theory]
    [InlineData("truce",0)][InlineData("offer_lifetime",1001)][InlineData("declaration_grievance",1001)]
    [InlineData("blockade_retained_percent",0)][InlineData("blockade_retained_percent",101)]
    [InlineData("formation_limit",33)][InlineData("active_encounter_limit",65)]
    [InlineData("peace_hull_target",7)][InlineData("war_hull_target",1)]
    [InlineData("healthy_cohesion",101)][InlineData("history_limit",21)]
    public void InvalidBoundHasLocatedField(string field,int value)
    {var n=JsonNode.Parse(DefinitionJson)!;n["conflict"]![field]=value;var c=Load(n.ToJsonString());Assert.False(c.Succeeded);Assert.Contains(c.Errors,e=>e.File=="conflict-test.json"&&e.Field=="conflict."+field);}
    [Theory][InlineData("missing")][InlineData("unknown")][InlineData("duplicate")][InlineData("algorithm")]
    public void MalformedDefinitionsAreRejected(string kind)
    {
        var n=JsonNode.Parse(DefinitionJson)!;
        if(kind=="missing")n["conflict"]!.AsObject().Remove("truce");
        if(kind=="unknown")n["conflict"]!["hidden_bonus"]=1;
        if(kind=="algorithm")n["conflict"]!["algorithm_id"]="secret_v9";
        string j=n.ToJsonString();if(kind=="duplicate")j=j.Replace("\"truce\":10","\"truce\":10,\"truce\":10");
        Assert.False(Load(j).Succeeded);
    }
    [Fact] public void RequiredConflictRejectsMissingSoloDefinition()
    {var c=ContentLoader.Load(Sources(),requireConflict:true);Assert.False(c.Succeeded);Assert.Contains(c.Errors,e=>e.Field=="conflict");}
    [Fact] public void ExplicitRules12SoloMissingDefinitionNeverCreatesState()
    {
        var loaded=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory());Assert.True(loaded.Succeeded);
        var g=OpenAntares.Simulation.Galaxy.GalaxyGenerator.CreateNewGame(loaded.Content! with{Conflict=null},new("small",42,12,"harl",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));
        Assert.False(g.Succeeded);Assert.Null(g.State);Assert.Contains("Conflict",g.Error);
    }
    private static ContentSource[] Sources()=>Directory.GetFiles(ContentLoaderTests.CoreContentDirectory(),"*.json").Where(p=>Path.GetFileName(p)!="conflict.json").Select(p=>new ContentSource(Path.GetFileName(p),File.ReadAllText(p))).ToArray();
    private static ContentLoadResult Load(string json)=>ContentLoader.Load(Sources().Append(new ContentSource("conflict-test.json",json)),requireConflict:true);
}
