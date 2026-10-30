using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ScoutingSaveTests
{
    internal static IEnumerable<LoadedGame> Cases()
    {
        yield return ScoutingFixtures.Game();yield return ScoutingFixtures.Game(false);yield return ScoutingFixtures.AtAmeba();yield return ScoutingFixtures.AtAmeba(AmebaStage.Exposed,200);yield return ScoutingResolutionTests.Risk(ScoutingFixtures.AtAmeba(),AmebaRiskAction.Study);yield return ScoutingResolutionTests.Risk(ScoutingFixtures.AtAmeba(),AmebaRiskAction.Remain);
        var g=ScoutingFixtures.Game();g.State.Empires[0].Scouting!.KnownHabitats.Add(new(1,new(8),0));var approach=CommandProcessor.Execute(g.Content,g.State,new SetAmebaApproachOrderCommand(g.State.Empires[0].Id,0,g.State.Vessels[0].Id,new(8),1));Assert.True(approach.Accepted);yield return g with{State=approach.State};
        var transit=ScoutingResolutionTests.Ready(ScoutingReadyTests.Acknowledge(ScoutingFixtures.AtAmeba()));Assert.True(transit.Accepted);yield return new(g.Content,transit.State);
        g=ScoutingFixtures.AtAmeba();var colony=g.State.Colonies[0];g.State.Empires[0].Crisis!.LostColonyLocations.Add(CrisisFixtures.Location(g.State,colony));g.State.Colonies.Clear();g.State.Empires[0].Surveys.RemoveAll(o=>o.StarId!=new StarId(8));yield return ScoutingFixtures.Valid(g);
        g=ScoutingResolutionTests.Risk(ScoutingFixtures.WithNextDraw(ScoutingFixtures.AtAmeba(),74),AmebaRiskAction.Study);var success=ScoutingResolutionTests.Ready(g);Assert.True(success.Accepted);yield return g with{State=success.State};g=ScoutingFixtures.WithNextDraw(g with{State=success.State},95);g=ScoutingReadyTests.Acknowledge(g);var loss=ScoutingResolutionTests.Ready(g);Assert.True(loss.Accepted);yield return g with{State=loss.State};
        var shared=ScoutingReportTests.NeutralSharedHome();var waiting=ScoutTurnCoordinator.Prepare(shared.Content,shared.State,shared.State.Empires[0].Id);Assert.True(waiting.Succeeded);yield return shared with{State=waiting.State};
    }
    [Fact] public void CurrentWriterRoundTripsEveryExplicitScoutShape()
    {
        Assert.Equal(11,SaveGame.SchemaVersion);Assert.Equal(9,Rules.CurrentVersion);
        foreach(var g in Cases()){string before=SaveGame.Serialize(g.Content,g.State);var r=SaveGame.Deserialize(before);Assert.True(r.Succeeded,string.Join("; ",r.Errors));Assert.Equal(before,SaveGame.Serialize(r.Game!.Content,r.Game.State));}
    }
    [Fact] public void ExactTwentyNinePairsAcceptAndAllOtherNearbyPairsReject()
    {
        string[] names={"legacy-v1-save.json","legacy-v2-save.json","legacy-rules2-v3-save.json","legacy-rules3-v4-save.json","legacy-rules4-v5-save.json","legacy-rules5-v6-save.json"};int accepted=0;
        for(int schema=1;schema<=8;schema++)for(int rules=1;rules<=7;rules++)
        {
            string text=rules==7?SaveGame.Serialize(ScoutingFixtures.Content(),ScoutingFixtures.Game().State):rules==6?SaveGame.Serialize(CrisisFixtures.Content(),CrisisFixtures.Game().State):File.ReadAllText(CompatibilityFixtures.PathFor(names[Math.Min(rules,5)]));
            var root=JsonNode.Parse(text)!;root["schema_version"]=schema;root["rules_version"]=rules;
            // Choose the actual legacy schema1 fixture only for its original pair.
            if(schema==1&&rules==1)root=JsonNode.Parse(File.ReadAllText(CompatibilityFixtures.PathFor(names[0])))!;
            bool expected=(schema,rules) is (1,1) or (2,1)||schema is >=3 and <=8&&rules>=1&&rules<=schema-1;
            var result=SaveGame.Deserialize(root.ToJsonString());Assert.Equal(expected,result.Succeeded);if(result.Succeeded)accepted++;
        }
        Assert.Equal(29,accepted);
    }
    [Fact] public void ReloadedScoutingShapesKeepFourIdenticalFutureBoundaries()
    {
        foreach(var g in Cases())
        {
            var loaded=SaveGame.Deserialize(SaveGame.Serialize(g.Content,g.State));Assert.True(loaded.Succeeded,string.Join("; ",loaded.Errors));var direct=new GameSession(g.Content,g.State);var restored=new GameSession(loaded.Game!.Content,loaded.Game.State);
            for(int i=0;i<4;i++){Advance(direct);Advance(restored);Assert.Equal(SaveGame.Serialize(direct.Content,direct.State),SaveGame.Serialize(restored.Content,restored.State));}
        }
        static void Advance(GameSession session)
        {
            int turn=session.State.Turn;
            foreach(var owner in session.State.Empires.Where(e=>e.Controller==OpenAntares.Simulation.State.ControllerKind.Human).Select(e=>e.Id).ToArray())
            {
                foreach(var token in ScoutQueries.Project(session.Content,session.State,owner).Attention.Where(t=>t.Action=="response_required"))
                {
                    var command=new SetAmebaRiskOrderCommand(owner,turn,token.Vessel,token.EncounterNumber,token.Revision,AmebaRiskAction.Remain);var risk=session.Execute(command);Assert.True(risk.Accepted,risk.Rejection?.Message);
                }
                var plague=OpenAntares.Simulation.Crises.CrisisQueries.PendingAttention(session.State,owner);if(!plague.IsEmpty)Assert.True(session.Execute(new AcknowledgeCrisisCommand(owner,turn,plague)).Accepted);
                var scout=ScoutQueries.Project(session.Content,session.State,owner).Attention;if(!scout.IsEmpty)Assert.True(session.Execute(new AcknowledgeAmebaCommand(owner,turn,scout)).Accepted);
                var ready=session.Execute(new SetReadyCommand(owner,turn,true));Assert.True(ready.Accepted,ready.Rejection?.Message);
            }
            Assert.Equal(turn+1,session.State.Turn);
        }
    }
    [Fact] public void EveryScoutFieldIncludingNullableOffFieldsIsRequired()
    {
        foreach(var g in Cases())
        {
            var root=JsonNode.Parse(SaveGame.Serialize(g.Content,g.State))!;var paths=new List<string[]>();
            foreach(var path in new[]{new[]{"state","scouting"},new[]{"state","empires","0","scouting"},new[]{"content","space_ameba"}}){paths.Add(path);Collect(At(root,path),path,paths);}
            for(int i=0;i<g.State.Vessels.Count;i++)foreach(string field in new[]{"scout","ameba_encounter"}){string[] path={"state","vessels",i.ToString(),field};paths.Add(path);Collect(At(root,path),path,paths);}
            foreach(var path in paths){var changed=root.DeepClone();At(changed,path[..^1]).AsObject().Remove(path[^1]);Assert.False(SaveGame.Deserialize(changed.ToJsonString()).Succeeded,"Missing "+string.Join(".",path));}
        }
    }
    [Theory][InlineData("state")][InlineData("empire")][InlineData("scout")][InlineData("encounter")][InlineData("content")]
    public void EveryOlderRulesLayoutRejectsNewMetadataEvenNull(string where)
    {
        string[] files={"legacy-v2-save.json","legacy-rules2-v3-save.json","legacy-rules3-v4-save.json","legacy-rules4-v5-save.json","legacy-rules5-v6-save.json"};
        for(int rules=1;rules<=6;rules++)foreach(bool empty in new[]{false,true})
        {
            string text=rules==6?SaveGame.Serialize(CrisisFixtures.Content(),CrisisFixtures.Game().State):File.ReadAllText(CompatibilityFixtures.PathFor(files[rules-1]));var root=JsonNode.Parse(text)!;root["schema_version"]=8;
            JsonNode? parent=where switch{"state"=>root["state"],"empire"=>root["state"]!["empires"]![0],"content"=>root["content"],_=>root["state"]!["vessels"] is JsonArray array&&array.Count>0?array[0]:null};if(parent is null)continue;
            string field=where switch{"state" or "empire"=>"scouting","encounter"=>"ameba_encounter","content"=>"space_ameba",_=>"scout"};parent[field]=empty?new JsonObject():null;Assert.False(SaveGame.Deserialize(root.ToJsonString()).Succeeded);
        }
    }
    [Theory][InlineData("stage")][InlineData("selector")][InlineData("duplicate")][InlineData("owner")][InlineData("receipt")][InlineData("claim")][InlineData("survival")][InlineData("unknown")][InlineData("counter")]
    [InlineData("warning_age")][InlineData("missing_encounter")][InlineData("exposed_age")][InlineData("signature")][InlineData("claim_cargo")]
    public void InvalidReferencesAndProofsRejectBeforeSessionOrFilePublication(string kind)
    {
        var g=ScoutingReadyTests.Acknowledge(ScoutingFixtures.AtAmeba(kind=="exposed_age"?AmebaStage.Exposed:AmebaStage.Warning));var root=JsonNode.Parse(SaveGame.Serialize(g.Content,g.State))!;var e=root["state"]!["empires"]![0]!["scouting"]!;var v=root["state"]!["vessels"]![0]!;Assert.NotNull(e);Assert.NotNull(v["scout"]);Assert.NotNull(v["ameba_encounter"]);
        switch(kind){case "stage":v["ameba_encounter"]!["stage"]="combat";break;case "selector":e["risk_random"]!["increment"]=3;break;case "duplicate":e["known_habitats"]!.AsArray().Add(e["known_habitats"]![0]!.DeepClone());break;case "owner":e["receipts"]![0]!["token"]!["vessel"]=999;break;case "receipt":e["receipts"]![0]!["token"]!["percent"]=0;break;case "claim":e["successful_studies"]!.AsArray().Add(new JsonObject{{"habitat",99},{"turn",10},{"vessel",5}});break;case "survival":v["scout"]!["withdraw_from_ameba"]=false;break;case "unknown":v["scout"]!["auto_study"]=true;break;case "counter":e["next_encounter_number"]=long.MaxValue;break;}
        switch(kind){case "warning_age":v["ameba_encounter"]!["arrival_turn"]=9;break;case "exposed_age":v["ameba_encounter"]!["arrival_turn"]=8;break;case "missing_encounter":v["ameba_encounter"]=null;e["receipts"]!.AsArray().Clear();break;case "signature":v["scout"]!["last_wait_signature"]="x";break;case "claim_cargo":v["role"]="harl_settler";v["carried_population"]=1;v["scout"]=null;e["receipts"]!.AsArray().Clear();e["successful_studies"]!.AsArray().Add(new JsonObject{{"habitat",1},{"turn",10},{"vessel",g.State.Vessels[0].Id.Value}});break;}
        string dir=Path.Combine(Path.GetTempPath(),"OpenAntares-scout-codec-"+Guid.NewGuid());Directory.CreateDirectory(dir);string file=Path.Combine(dir,"invalid.json");File.WriteAllText(file,root.ToJsonString());try{var session=new GameSession(g.Content,g.State);Assert.False(session.Load(file).Succeeded);Assert.Same(g.Content,session.Content);Assert.Same(g.State,session.State);File.WriteAllText(file,"prior");g.State.Vessels[0].Scout!.WithdrawFromAmeba=false;Assert.Throws<ArgumentException>(()=>SaveGame.WriteFile(file,g.Content,g.State));Assert.Equal("prior",File.ReadAllText(file));}finally{File.Delete(file);Directory.Delete(dir);}
    }
    private static JsonNode At(JsonNode n,string[] path){foreach(var part in path)n=n is JsonArray a?a[int.Parse(part)]!:n[part]!;return n;}
    private static void Collect(JsonNode? node,string[] path,List<string[]> fields){if(node is JsonObject o)foreach(var p in o){var child=path.Append(p.Key).ToArray();fields.Add(child);Collect(p.Value,child,fields);}else if(node is JsonArray a)for(int i=0;i<a.Count;i++)Collect(a[i],path.Append(i.ToString()).ToArray(),fields);}
}
