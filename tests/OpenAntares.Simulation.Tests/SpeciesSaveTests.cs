using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class SpeciesSaveTests
{
    private static string FutureSpeciesJson()
    {
        var root=JsonNode.Parse(SaveGame.Serialize(SpeciesFixtures.Content(),SpeciesFixtures.AtHome("harl")))!;
        root["schema_version"]=4;root["content"]=JsonNode.Parse(SpeciesFixtures.ContentJson());
        foreach(var e in root["state"]!["empires"]!.AsArray())
        {
            e!["species_id"]="harl";
            foreach(var o in e["surveys"]!.AsArray())foreach(var p in o!["planets"]!.AsArray())p!["environment_id"]="shelf_sea";
        }
        foreach(var p in root["state"]!["planets"]!.AsArray())p!["environment_id"]="shelf_sea";
        foreach(var c in root["state"]!["colonies"]!.AsArray())c!["habitat_mode"]="native";
        return root.ToJsonString();
    }
    public static TheoryData<int,int,bool> VersionPairs
    {
        get
        {
            var data=new TheoryData<int,int,bool>();
            for(int schema=0;schema<=4;schema++)for(int rules=0;rules<=4;rules++)
                data.Add(schema,rules,(schema,rules) is (1,1) or (2,1) or (3,1) or (3,2) or (4,1) or (4,2) or (4,3));
            return data;
        }
    }
    [Theory][MemberData(nameof(VersionPairs))]
    public void RetainedSevenLegacySchemaRulesPairsAreAccepted(int schema,int rules,bool expected)
    {
        string original=rules==3?FutureSpeciesJson():File.ReadAllText(CompatibilityFixtures.PathFor(rules==2?"legacy-rules2-v3-save.json":schema==1?"legacy-v1-save.json":"legacy-v2-save.json"));
        var root=JsonNode.Parse(original)!;root["schema_version"]=schema;root["rules_version"]=rules;
        var result=SaveGame.Deserialize(root.ToJsonString());Assert.Equal(expected,result.Succeeded);
    }
    [Theory][InlineData("peldra","native")][InlineData("harl","native")][InlineData("sulan","native")][InlineData("sulan","sealed")][InlineData("wenth","native")]
    public void SpeciesDefinitionsStateAndContinuationRoundTrip(string id,string habitat)
    {
        var content=SpeciesFixtures.Content();var state=SpeciesFixtures.AtHome(id);state.Colonies[0].HabitatMode=habitat;
        if(id=="wenth") {state.Colonies[0].Population=5;state.Colonies[0].Workforce=new(3,1,1);state.Colonies[0].ProjectKind=ProductionKinds.Vessel;state.Colonies[0].ProjectId="daughter_seed";state.Colonies[0].ProductionReserve=1200;}
        string json=SaveGame.Serialize(content,state);Assert.Equal(6,JsonNode.Parse(json)!["schema_version"]!.GetValue<int>());
        var load=SaveGame.Deserialize(json);Assert.True(load.Succeeded,string.Join(";",load.Errors));var copy=load.Game!;
        Assert.Equal(SpeciesFixtures.Snapshot(state),SpeciesFixtures.Snapshot(copy.State));
        Assert.Equal(JsonSerializer.Serialize(content),JsonSerializer.Serialize(copy.Content));
        for(int turn=0;turn<4;turn++)
        {
            var a=TurnResolver.Resolve(content,state);var b=TurnResolver.Resolve(copy.Content,copy.State);
            Assert.True(a.Succeeded,string.Join(";",a.Errors));Assert.True(b.Succeeded,string.Join(";",b.Errors));
            state=a.State;copy=new(copy.Content,b.State);Assert.Equal(SaveGame.Serialize(content,state),SaveGame.Serialize(copy.Content,copy.State));
        }
    }
    [Theory][InlineData("move")][InlineData("transit")][InlineData("found")]
    public void SpeciesVesselsOrdersCargoAndCurrentSurveysRoundTrip(string mode)
    {
        var content=SpeciesFixtures.Content();var s=SpeciesFixtures.AtHome("sulan");var v=s.Vessels[0];
        if(mode=="move")v.PendingMove=s.Stars[1].Id;
        if(mode=="transit"){v.StationaryStarId=null;v.Transit=new(Fixtures.HomeStar,s.Stars[1].Id,2,1);}
        if(mode=="found"){v.RoleId="harbor_settler";v.CarriedPopulation=2;v.StationaryStarId=s.Stars[1].Id;v.PendingFounding=s.Planets[1].Id;s.Planets[1].EnvironmentId="dry";}
        ObservationUpdater.Refresh(s,s.Turn);
        var loaded=SaveGame.Deserialize(SaveGame.Serialize(content,s));Assert.True(loaded.Succeeded,string.Join(";",loaded.Errors));Assert.Equal(SpeciesFixtures.Snapshot(s),SpeciesFixtures.Snapshot(loaded.Game!.State));
    }
    [Theory][InlineData("species")][InlineData("unknown_species")][InlineData("environment")][InlineData("habitat")][InlineData("unknown_habitat")][InlineData("observation")][InlineData("required_vessels")][InlineData("required_surveys")][InlineData("required_project_kind")]
    public void MissingOrIllegalSpeciesDataCannotPublish(string mutation)
    {
        var root=JsonNode.Parse(FutureSpeciesJson())!;var state=root["state"]!;
        switch(mutation)
        {
            case "species":state["empires"]![0]!.AsObject().Remove("species_id");break;
            case "unknown_species":state["empires"]![0]!["species_id"]="missing";break;
            case "environment":state["planets"]![0]!.AsObject().Remove("environment_id");break;
            case "habitat":state["colonies"]![0]!.AsObject().Remove("habitat_mode");break;
            case "unknown_habitat":state["colonies"]![0]!["habitat_mode"]="missing";break;
            case "observation":state["empires"]![0]!["surveys"]![0]!["planets"]![0]!.AsObject().Remove("environment_id");break;
            case "required_vessels":state.AsObject().Remove("vessels");break;
            case "required_surveys":state["empires"]![0]!.AsObject().Remove("surveys");break;
            case "required_project_kind":state["colonies"]![0]!.AsObject().Remove("project_kind");break;
        }
        Assert.False(SaveGame.Deserialize(root.ToJsonString()).Succeeded);
    }
    [Fact]
    public void LegacyWriterAddsNoSpeciesDataAndLegacyReaderRejectsInjectedMetadata()
    {
        var game=CompatibilityFixtures.LoadRules2();var json=SaveGame.Serialize(game.Content,game.State);var root=JsonNode.Parse(json)!;
        Assert.Equal(6,root["schema_version"]!.GetValue<int>());Assert.DoesNotContain("species",json);Assert.DoesNotContain("environment",json);Assert.DoesNotContain("habitat_mode",json);
        root["state"]!["empires"]![0]!["species_id"]="harl";Assert.False(SaveGame.Deserialize(root.ToJsonString()).Succeeded);
    }
    [Fact]
    public void FailedSpeciesLoadAndAtomicReplacementPreservePublishedData()
    {
        string directory=Path.Combine(Path.GetTempPath(),"openantares-species-"+Path.GetRandomFileName());Directory.CreateDirectory(directory);
        try
        {
            var content=SpeciesFixtures.Content();var s=SpeciesFixtures.AtHome("harl");var session=new GameSession(content,s);string path=Path.Combine(directory,"keep.json");
            SaveGame.WriteFile(path,content,s);byte[] before=File.ReadAllBytes(path);
            using(var held=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))
            {var error=Assert.ThrowsAny<Exception>(()=>SaveGame.WriteFile(path,content,s));Assert.True(error is IOException or UnauthorizedAccessException);}
            Assert.Equal(before,File.ReadAllBytes(path));Assert.False(File.Exists(path+".tmp"));
            string bad=Path.Combine(directory,"bad.json");File.WriteAllText(bad,"{broken");Assert.False(session.Load(bad).Succeeded);Assert.Same(s,session.State);Assert.Same(content,session.Content);
        }
        finally{Directory.Delete(directory,true);}
    }
}
