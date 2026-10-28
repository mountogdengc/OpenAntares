using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisSaveTests
{
    public static TheoryData<int,int,bool> Pairs
    {
        get
        {
            var data=new TheoryData<int,int,bool>();
            for(int s=0;s<=8;s++)for(int r=0;r<=7;r++)
                data.Add(s,r,(s,r) is (1,1) or (2,1) or (3,1) or (3,2) or (4,1) or (4,2) or (4,3) or (5,1) or (5,2) or (5,3) or (5,4) or (6,1) or (6,2) or (6,3) or (6,4) or (6,5) or (7,1) or (7,2) or (7,3) or (7,4) or (7,5) or (7,6) or (8,1) or (8,2) or (8,3) or (8,4) or (8,5) or (8,6));
            return data;
        }
    }
    [Theory][MemberData(nameof(Pairs))]
    public void HistoricalCampaignLayoutsHaveExplicitSchemaPairs(int schema,int rules,bool allowed)
    {
        string text;
        if(rules==6){var g=CrisisFixtures.Game();text=SaveGame.Serialize(g.Content,g.State);}
        else text=File.ReadAllText(CompatibilityFixtures.PathFor(rules==5?"legacy-rules5-v6-save.json":rules==4?"legacy-rules4-v5-save.json":rules==3?"legacy-rules3-v4-save.json":rules==2?"legacy-rules2-v3-save.json":schema==1?"legacy-v1-save.json":"legacy-v2-save.json"));
        var root=JsonNode.Parse(text)!;root["schema_version"]=schema;root["rules_version"]=rules;
        Assert.Equal(allowed,SaveGame.Deserialize(root.ToJsonString()).Succeeded);
    }
    [Fact]
    public void CurrentWriterRoundTripsAllExplicitCrisisFacts()
    {
        foreach(var g in new[]{CrisisFixtures.Game(),CrisisFixtures.Game(enabled:false),CrisisFixtures.Warning(),CrisisFixtures.Active(),HeldAndWaived(),ResolvedReceipt(),Immune()})
        {
            Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
            string text=SaveGame.Serialize(g.Content,g.State);var json=JsonNode.Parse(text)!;Assert.Equal(9,json["schema_version"]!.GetValue<int>());
            var loaded=SaveGame.Deserialize(text);Assert.True(loaded.Succeeded,string.Join("; ",loaded.Errors));
            Assert.Equal(text,SaveGame.Serialize(loaded.Game!.Content,loaded.Game.State));
            loaded.Game.State.Empires[0].Crisis!.Random.NextUInt32();
            Assert.NotEqual(g.State.Empires[0].Crisis!.Random.State,loaded.Game.State.Empires[0].Crisis!.Random.State);
        }
    }
    [Fact]
    public void MissingCrisisFieldNeverMeansConsent()
    {
        foreach(var g in new[]{HeldAndWaived(),ResolvedReceipt(),Immune()})
        {
            var root=JsonNode.Parse(SaveGame.Serialize(g.Content,g.State))!;var paths=new List<string[]>();
            foreach(var path in new[]{new[]{"state","crisis"},new[]{"state","empires","0","crisis"},new[]{"state","vessels","0","crisis"},new[]{"content","plagues"}})
            {paths.Add(path);Collect(At(root,path),path,paths);}
            if(g.State.Colonies.Count>0){string[] path={"state","colonies","0","crisis"};paths.Add(path);Collect(At(root,path),path,paths);}
            foreach(var path in paths)
            {
                var changed=root.DeepClone();var parent=At(changed,path.Take(path.Length-1).ToArray()).AsObject();parent.Remove(path[^1]);
                var result=SaveGame.Deserialize(changed.ToJsonString());Assert.False(result.Succeeded,"Missing "+string.Join(".",path));
            }
        }
    }
    [Theory]
    [InlineData("authority")] [InlineData("family")] [InlineData("owner")] [InlineData("unknown")] [InlineData("plagues")] [InlineData("revision")]
    public void InvalidCrisisLoadPreservesSessionReferences(string kind)
    {
        var g=kind=="family"?HeldAndWaived():ResolvedReceipt();var json=JsonNode.Parse(SaveGame.Serialize(g.Content,g.State))!;var e=json["state"]!["empires"]![0]!["crisis"]!;
        switch(kind){case "authority":e["authority"]="destroy_everything";break;case "family":e["episode"]!["family_id"]="unknown";break;case "owner":e["pending_receipts"]![0]!["key"]!["owner"]=999;break;case "unknown":e["surprise"]=true;break;case "plagues":json["content"]!.AsObject().Remove("plagues");break;case "revision":e["pending_receipts"]![0]!["revision"]=0;break;}
        string folder=Path.Combine(Path.GetTempPath(),"OpenAntares-crisis-load-"+Guid.NewGuid());Directory.CreateDirectory(folder);string file=Path.Combine(folder,"invalid.json");File.WriteAllText(file,json.ToJsonString());
        try{var session=new GameSession(g.Content,g.State);var loaded=session.Load(file);Assert.False(loaded.Succeeded);Assert.Same(g.Content,session.Content);Assert.Same(g.State,session.State);}finally{File.Delete(file);Directory.Delete(folder);}
    }
    [Theory][InlineData("state")][InlineData("empire")][InlineData("colony")][InlineData("vessel")][InlineData("content")]
    public void LegacyRulesRejectCrisisInjectionEvenNull(string field)
    {
        for(int rules=1;rules<=5;rules++)
        {
            string name=rules==5?"legacy-rules5-v6-save.json":rules==4?"legacy-rules4-v5-save.json":rules==3?"legacy-rules3-v4-save.json":rules==2?"legacy-rules2-v3-save.json":"legacy-v2-save.json";
            var root=JsonNode.Parse(File.ReadAllText(CompatibilityFixtures.PathFor(name)))!;root["schema_version"]=7;
            var parent=field switch{"state"=>root["state"]!,"empire"=>root["state"]!["empires"]![0]!,"colony"=>root["state"]!["colonies"]![0]!,"vessel"=>root["state"]!["vessels"] is JsonArray a&&a.Count>0?a[0]!:null,_=>root["content"]!};
            if(parent is null)continue;parent[field=="content"?"plagues":"crisis"]=null;
            Assert.False(SaveGame.Deserialize(root.ToJsonString()).Succeeded);
        }
    }
    [Fact]
    public void FailedCrisisWriteKeepsPriorDestination()
    {
        var g=CrisisFixtures.Active();string dir=Path.Combine(Path.GetTempPath(),"OpenAntares-crisis-write-"+Guid.NewGuid());Directory.CreateDirectory(dir);string path=Path.Combine(dir,"save.json");File.WriteAllText(path,"prior");
        try{using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))Assert.True(Record.Exception(()=>SaveGame.WriteFile(path,g.Content,g.State)) is IOException or UnauthorizedAccessException);Assert.Equal("prior",File.ReadAllText(path));Assert.Single(Directory.GetFiles(dir));}finally{File.Delete(path);Directory.Delete(dir);}
    }
    private static JsonNode At(JsonNode node,string[] path){foreach(string part in path)node=node is JsonArray array?array[int.Parse(part)]!:node[part]!;return node;}
    private static void Collect(JsonNode? node,string[] path,List<string[]> fields)
    {
        if(node is JsonObject obj)foreach(var p in obj){var child=path.Append(p.Key).ToArray();fields.Add(child);Collect(p.Value,child,fields);}
        else if(node is JsonArray array)for(int i=0;i<array.Count;i++)Collect(array[i],path.Append(i.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray(),fields);
    }
    private static LoadedGame Immune()
    {
        var g=CrisisFixtures.Active();var e=g.State.Empires[0].Crisis!;e.Episode=null;e.NextEligibleTurn=g.State.Turn+30;
        var c=g.State.Colonies[0].Crisis!;c.Case=null;c.ImmunityFamilyId="ash_bloom";c.ImmuneUntilTurn=g.State.Turn+12;return g;
    }
    private static LoadedGame HeldAndWaived()
    {
        var g=CrisisFixtures.Active();var e=g.State.Empires[0].Crisis!;e.RequireAcknowledgement=false;e.CivicLevel=2;e.CivicExpiresAt=g.State.Turn+5;e.Episode!.ResearchShare=50;e.Episode.ResearchOverride=true;e.ResearchPolicyPaused=true;
        var illness=g.State.Colonies[0].Crisis!.Case!;illness.HandledRevision=0;illness.WaivedRevision=illness.AttentionRevision;
        g.State.Vessels[0].RoleId="harl_settler";g.State.Vessels[0].CarriedPopulation=1;g.State.Vessels[0].Crisis!.InspectionDueTurn=g.State.Turn+1;
        return g;
    }
    private static LoadedGame ResolvedReceipt()
    {
        var g=CrisisFixtures.Active();var colony=g.State.Colonies[0];var e=g.State.Empires[0].Crisis!;var location=CrisisFixtures.Location(g.State,colony);
        e.PendingReceipts.Add(new(new(colony.EmpireId,1,colony.Id),3,g.State.Turn,location,"Colony lost"));
        e.CompletedDigests.Add(new(g.State.Turn-1,ImmutableArray.Create(new CrisisDigestEntry(new(colony.EmpireId,1,colony.Id),location,"casualty",2,"pending",0,1,"Population lost"))));
        e.LostColonyLocations.Add(location);e.Episode=null;e.NextEligibleTurn=g.State.Turn+30;
        g.State.Colonies.Clear();OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(g.State,g.State.Turn);return g;
    }
}
