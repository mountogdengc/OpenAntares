using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Saves;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ColonySaveTests
{
    public static TheoryData<int,int,bool> Pairs {
        get {var data=new TheoryData<int,int,bool>();for(int s=0;s<=6;s++)for(int r=0;r<=6;r++)data.Add(s,r,(s,r) is (1,1) or (2,1) or (3,1) or (3,2) or (4,1) or (4,2) or (4,3) or (5,1) or (5,2) or (5,3) or (5,4) or (6,1) or (6,2) or (6,3) or (6,4) or (6,5));return data;}
    }
    [Theory][MemberData(nameof(Pairs))] public void AcceptsExactlySixteenPairs(int schema,int rules,bool allowed) {
        string text;if(rules==5){var g=ColonyFixtures.Game();text=SaveGame.Serialize(g.Content,g.State);}else text=File.ReadAllText(CompatibilityFixtures.PathFor(rules==4?"legacy-rules4-v5-save.json":rules==3?"legacy-rules3-v4-save.json":rules==2?"legacy-rules2-v3-save.json":schema==1?"legacy-v1-save.json":"legacy-v2-save.json"));
        var root=JsonNode.Parse(text)!;root["schema_version"]=schema;root["rules_version"]=rules;Assert.Equal(allowed,SaveGame.Deserialize(root.ToJsonString()).Succeeded);
    }
    [Fact] public void PendingReadyRevisionsQueuesAndAppearanceRoundTripExactly() {
        var g=ColonyFixtures.TwoHumansAndAi();var owner=g.State.Empires[0];var c=g.State.Colonies[0];var draft=ColonyFixtures.Snapshot(g.Content,"industrial").Definition with{Name="Custom"};
        owner.Development!.CustomTemplates.Add(new("custom_1",2,draft));owner.Development.NextTemplateNumber=2;c.Development!.Template=new("custom_1",1,draft);owner.Development.NewColonyPlan=new(c.Development.Template,true,true);
        c.Development.Queue.Add(new(1,new("building","production_complex")));c.Development.NextQueueToken=2;
        var ready=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(owner.Id,0,true));Assert.True(ready.Accepted);string text=SaveGame.Serialize(g.Content,ready.State);var root=JsonNode.Parse(text)!;Assert.Equal(11,root["schema_version"]!.GetValue<int>());Assert.NotNull(root["content"]!["governor_templates"]);
        var reload=SaveGame.Deserialize(text);Assert.True(reload.Succeeded,string.Join("; ",reload.Errors));Assert.Equal(text,SaveGame.Serialize(reload.Game!.Content,reload.Game.State));Assert.True(reload.Game.State.Empires[0].Ready);Assert.NotNull(reload.Game.State.Empires[0].Development!.PendingReport);
        var next=CommandProcessor.Execute(g.Content,reload.Game.State,new SetReadyCommand(reload.Game.State.Empires[1].Id,0,true));Assert.True(next.Accepted,next.Rejection?.Message);var completed=SaveGame.Deserialize(SaveGame.Serialize(g.Content,next.State));Assert.True(completed.Succeeded,string.Join("; ",completed.Errors));Assert.Single(completed.Game!.State.Empires[0].Development!.CompletedReports);
    }
    [Theory][InlineData("empire")][InlineData("colony")][InlineData("appearance")][InlineData("survey")][InlineData("content")][InlineData("issue")]
    public void MissingOrInvalidRules5FieldsReject(string kind) {
        var g=ColonyFixtures.TwoHumansAndAi();var ready=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,0,true));var root=JsonNode.Parse(SaveGame.Serialize(g.Content,ready.State))!;
        if(kind=="empire")root["state"]!["empires"]![0]!.AsObject().Remove("development");
        if(kind=="colony")root["state"]!["colonies"]![0]!["development"]=null;
        if(kind=="appearance")root["state"]!["planets"]![0]!.AsObject().Remove("appearance");
        if(kind=="survey")root["state"]!["empires"]![0]!["surveys"]![0]!["planets"]![0]!["appearance"]=null;
        if(kind=="content")root["content"]!.AsObject().Remove("governor_templates");
        if(kind=="issue")root["state"]!["empires"]![0]!["development"]!["pending_report"]!["entries"]![0]!["issues"]=JsonNode.Parse("[{\"code\":\"unknown\",\"content_id\":null}]");
        Assert.False(SaveGame.Deserialize(root.ToJsonString()).Succeeded);
    }
    [Theory][InlineData("null")][InlineData("{}")][InlineData("[]")]
    public void OldRulesRejectNewContentEvenEmpty(string value) {
        var root=JsonNode.Parse(File.ReadAllText(CompatibilityFixtures.PathFor("legacy-rules4-v5-save.json")))!;root["schema_version"]=6;root["content"]!["governor_templates"]=JsonNode.Parse(value);Assert.False(SaveGame.Deserialize(root.ToJsonString()).Succeeded);
    }
    [Fact] public void FailedDevelopmentWritePreservesDestination() {
        var g=ColonyFixtures.Game();string dir=Path.Combine(Path.GetTempPath(),"OpenAntares-colony-"+Guid.NewGuid());Directory.CreateDirectory(dir);string path=Path.Combine(dir,"save.json");File.WriteAllText(path,"prior");
        try {using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))Assert.True(Record.Exception(()=>SaveGame.WriteFile(path,g.Content,g.State)) is IOException or UnauthorizedAccessException);Assert.Equal("prior",File.ReadAllText(path));Assert.Single(Directory.GetFiles(dir));}finally{File.Delete(path);Directory.Delete(dir);}
    }
    public static TheoryData<int,int,string> OldInjections {
        get {var data=new TheoryData<int,int,string>();foreach(var pair in new[]{(1,1),(2,1),(3,1),(3,2),(4,1),(4,2),(4,3),(5,1),(5,2),(5,3),(5,4),(6,1),(6,2),(6,3),(6,4)})foreach(string field in new[]{"empire_null","colony_empty","planet_empty","content_empty"})data.Add(pair.Item1,pair.Item2,field);return data;}
    }
    [Theory][MemberData(nameof(OldInjections))]
    public void EveryOldPairRejectsNewFieldsEvenNullOrEmpty(int schema,int rules,string field) {
        string name=rules==4?"legacy-rules4-v5-save.json":rules==3?"legacy-rules3-v4-save.json":rules==2?"legacy-rules2-v3-save.json":schema==1?"legacy-v1-save.json":"legacy-v2-save.json";
        var root=JsonNode.Parse(File.ReadAllText(CompatibilityFixtures.PathFor(name)))!;root["schema_version"]=schema;Assert.True(SaveGame.Deserialize(root.ToJsonString()).Succeeded);
        if(field=="empire_null")root["state"]!["empires"]![0]!["development"]=null;
        if(field=="colony_empty")root["state"]!["colonies"]![0]!["development"]=new JsonObject();
        if(field=="planet_empty")root["state"]!["planets"]![0]!["appearance"]=new JsonObject();
        if(field=="content_empty")root["content"]!["governor_templates"]=new JsonArray();
        Assert.False(SaveGame.Deserialize(root.ToJsonString()).Succeeded);
    }
}
