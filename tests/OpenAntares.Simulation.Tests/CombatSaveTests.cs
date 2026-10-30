using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatSaveTests
{
    [Fact] public void PriorRulesLoadThroughWriter11()
    {
        string[] old={"legacy-v2-save.json","legacy-rules2-v3-save.json","legacy-rules3-v4-save.json","legacy-rules4-v5-save.json","legacy-rules5-v6-save.json"};int count=0;
        var sources=Enumerable.Range(1,9).ToDictionary(r=>r,r=>r==9?SaveGame.Serialize(CombatFixtures.Game().Content,CombatFixtures.Game().State):r==8?SaveGame.Serialize(FleetFixtures.Game().Content,FleetFixtures.Game().State):r==7?SaveGame.Serialize(ScoutingFixtures.Content(),ScoutingFixtures.Game().State):r==6?SaveGame.Serialize(CrisisFixtures.Content(),CrisisFixtures.Game().State):File.ReadAllText(CompatibilityFixtures.PathFor(old[r-1])));
        for(int schema=1;schema<=11;schema++)for(int rules=1;rules<=10;rules++)
        {
            var node=JsonNode.Parse(sources[Math.Min(rules,9)])!;node["schema_version"]=schema;node["rules_version"]=rules;
            if(schema==1&&rules==1)node=JsonNode.Parse(File.ReadAllText(CompatibilityFixtures.PathFor("legacy-v1-save.json")))!;
            bool expected=(schema,rules) is (1,1) or (2,1)||schema is >=3 and <=9&&rules<schema||schema is 10 or 11&&rules<=9;
            var loaded=SaveGame.Deserialize(node.ToJsonString());Assert.True(expected==loaded.Succeeded,$"({schema},{rules}): {string.Join(";",loaded.Errors)}");if(loaded.Succeeded)count++;
        }
        Assert.Equal(55,count);Assert.Equal(11,SaveGame.SchemaVersion);
    }
    internal static LoadedGame Next(LoadedGame g)
    {var n=TurnResolver.Resolve(g.Content,g.State);Assert.True(n.Succeeded,string.Join(";",n.Errors));return new(g.Content,n.State);}
    internal static IEnumerable<LoadedGame> Cases()
    {
        yield return CombatFixtures.Game(enabled:false);var convoy=CombatFixtures.Convoy();yield return new(convoy.Content,CombatCommandTests.Commit(convoy,CombatCommandTests.Attack(convoy.State,true)));
        var active=Next(CombatTurnTests.Pending(doctrine:CombatDoctrine.Hold));yield return active;
        yield return Next(CombatTurnTests.Pending(doctrine:CombatDoctrine.HitAndRun));
        foreach(var arrival in new[]{CombatArrivalOrder.Repair,CombatArrivalOrder.Hold})
        {
            var queued=CombatWithdrawalTests.Queued(CombatWithdrawalTests.Battle(new[]{100,25}),StragglerPolicy.DetachCrippled);var e=queued.State.Combat!.Encounters[0];queued=new(queued.Content,CombatCommandTests.Commit(queued,new CombatAction.ChangeCombatOrders(e.Id,e.Revision,e.Orders with{OnArrival=arrival},false)));e=queued.State.Combat!.Encounters[0];queued=new(queued.Content,CombatCommandTests.Commit(queued,new CombatAction.WithdrawCombat(e.Id,e.Revision)));yield return queued;
            var departed=Next(queued);yield return departed;
            for(int i=0;i<10&&departed.State.Vessels.Any(v=>v.Transit is not null);i++)departed=Next(departed);yield return departed;yield return Next(departed);
        }
        var blocked=CombatWithdrawalTests.Queued(CombatWithdrawalTests.Battle(new[]{100,100},new[]{true,false},crises:true),StragglerPolicy.KeepTogether);blocked.State.Colonies[0].Crisis!.Quarantined=true;var battle=blocked.State.Combat!.Encounters[0];var context=CombatResolutionContext.Capture(blocked.Content,blocked.State);var candidate=blocked.State.Clone();CombatWithdrawal.Resolve(blocked.Content,blocked.State,candidate,context);candidate.Turn++;OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(candidate,candidate.Turn);CombatObservations.Refresh(blocked.Content,candidate,candidate.Turn);yield return new(blocked.Content,candidate);
        var death=CombatTurnTests.Pending(1);var officer=death.State.Empires[0].Fleets!.Officers[0];officer.Status=OfficerStatus.Commanding;officer.PostedColony=null;officer.Flagship=death.State.Vessels[0].Id;death.State.Empires[0].Fleets!.Fleets[0].Officer=officer.Id;yield return Next(death);
    }
    [Fact] public void EveryLivePhaseLoadsWithoutActingAndContinuesIdentically()
    {
        foreach(var g in Cases())
        {
            string before=CombatFixtures.Bytes(g.State),text=SaveGame.Serialize(g.Content,g.State);Assert.Equal(11,JsonNode.Parse(text)!["schema_version"]!.GetValue<int>());var loaded=SaveGame.Deserialize(text);Assert.True(loaded.Succeeded,string.Join(";",loaded.Errors));Assert.Equal(text,SaveGame.Serialize(loaded.Game!.Content,loaded.Game.State));Assert.Equal(before,CombatFixtures.Bytes(loaded.Game.State));Assert.Equal(before,CombatFixtures.Bytes(g.State));
            var direct=g;var restored=loaded.Game;
            for(int i=0;i<3;i++)
            {
                var a=TurnResolver.Resolve(direct.Content,direct.State);var b=TurnResolver.Resolve(restored.Content,restored.State);Assert.Equal(a.Succeeded,b.Succeeded);Assert.Equal(CombatFixtures.Bytes(a.State),CombatFixtures.Bytes(b.State));if(!a.Succeeded){Assert.Same(direct.State,a.State);Assert.Same(restored.State,b.State);break;}direct=new(direct.Content,a.State);restored=new(restored.Content,b.State);
            }
        }
    }
    [Fact] public void CombatShapeNullEnumsDuplicateIdsAndRangesRejectWithoutThrowing()
    {
        var g=CombatTurnTests.Pending();string text=SaveGame.Serialize(g.Content,g.State);var encoded=JsonNode.Parse(text)!;Assert.NotNull(encoded["state"]!["combat"]);
        foreach(var edit in new Action<JsonNode>[]
        {
            n=>n["state"]!.AsObject().Remove("combat"),n=>n["state"]!["combat"]=null,n=>n["state"]!["combat"]!["pockets"]=null,
            n=>n["state"]!["combat"]!["surprise"]=0,n=>n["state"]!["combat"]!["encounters"]![0]!["phase"]=0,n=>n["state"]!["combat"]!["encounters"]![0]!["phase"]="Pending",
            n=>n["state"]!["combat"]!["encounters"]![0]!.AsObject().Remove("consent_revision"),n=>n["state"]!["combat"]!["encounters"]![0]!["members"]!.AsArray().Add(9999),
            n=>n["state"]!["combat"]!["encounters"]![0]!["orders"]=null,n=>n["state"]!["combat"]!["encounters"]![0]!["orders"]!["return_colony"]=99999,
            n=>n["state"]!["combat"]!["encounters"]![0]!["round"]=-1,n=>n["state"]!["combat"]!["next_encounter_id"]=1,
            n=>n["state"]!["combat"]!["pockets"]![0]!["sentries"]![0]!["hull"]=81,n=>n["state"]!["combat"]!["pockets"]![0]!["sentries"]![0]!["id"]=0,
            n=>n["state"]!["combat"]!["pockets"]![0]!["sentries"]!.AsArray().Add(n["state"]!["combat"]!["pockets"]![0]!["sentries"]![0]!.DeepClone()),
            n=>n["state"]!["empires"]![0]!.AsObject().Remove("combat"),n=>n["state"]!["empires"]![0]!["combat"]!["responses"]=null,n=>n["state"]!["empires"]![0]!["combat"]!["lifetime_hull_repaired"]=-1,
            n=>n["state"]!["vessels"]![0]!.AsObject().Remove("combat"),n=>n["state"]!["vessels"]![0]!["combat"]!["hull"]=0,n=>n["state"]!["vessels"]![0]!["combat"]!["cohesion"]=101,
            n=>n["state"]!["vessels"]![0]!["combat"]!["repairing"]=true,n=>n["state"]!["vessels"]![0]!["combat"]!["last_repair_turn"]=99,
            n=>n["content"]!.AsObject().Remove("combat"),n=>n["content"]!["combat"]!["role_profiles"]!.AsObject().Remove("escort"),n=>n["content"]!["combat"]!["unknown"]=0
        }){var node=JsonNode.Parse(text)!;edit(node);var result=SaveGame.Deserialize(node.ToJsonString());Assert.False(result.Succeeded);Assert.Null(result.Game);Assert.NotEmpty(result.Errors);}
        string duplicate=text.Replace("\"next_pocket_id\": 2","\"next_pocket_id\": 2, \"next_pocket_id\": 2",StringComparison.Ordinal);Assert.NotEqual(text,duplicate);Assert.False(SaveGame.Deserialize(duplicate).Succeeded);
    }
    [Fact] public void AllCombatFieldsAreOmittedAndRejectedUnderOldRules()
    {
        var old=FleetFixtures.Game();string oldText=SaveGame.Serialize(old.Content,old.State);var oldNode=JsonNode.Parse(oldText)!;Assert.Null(oldNode["state"]!["combat"]);Assert.Null(oldNode["content"]!["combat"]);Assert.DoesNotContain(oldNode["content"]!["vessels"]!.AsArray(),v=>v!["id"]!.GetValue<string>()=="escort");
        var combat=CombatFixtures.Game();var newNode=JsonNode.Parse(SaveGame.Serialize(combat.Content,combat.State))!;Assert.NotNull(newNode["state"]!["combat"]);
        foreach(string location in new[]{"game","empire","vessel","content"})
        {var changed=JsonNode.Parse(oldText)!;switch(location){case "game":changed["state"]!["combat"]=newNode["state"]!["combat"]!.DeepClone();break;case "empire":changed["state"]!["empires"]![0]!["combat"]=newNode["state"]!["empires"]![0]!["combat"]!.DeepClone();break;case "vessel":changed["state"]!["vessels"]![0]!["combat"]=newNode["state"]!["vessels"]![0]!["combat"]!.DeepClone();break;default:changed["content"]!["combat"]=newNode["content"]!["combat"]!.DeepClone();break;}Assert.False(SaveGame.Deserialize(changed.ToJsonString()).Succeeded);}
    }
    [Fact] public void FailedReadAndWritePreservePreviousSessionAndFile()
    {
        var g=CombatTurnTests.Pending();var session=new GameSession(g.Content,g.State);string before=CombatFixtures.Bytes(session.State);string folder=Path.Combine(Path.GetTempPath(),"openantares-combat-codec-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try{string path=Path.Combine(folder,"good.json"),bad=Path.Combine(folder,"bad.json");session.Save(path);string bytes=File.ReadAllText(path);File.WriteAllText(bad,bytes.Replace("\"hull\": 100","\"hull\": -1",StringComparison.Ordinal));Assert.False(session.Load(bad).Succeeded);Assert.Equal(before,CombatFixtures.Bytes(session.State));var invalid=g.State.Clone();invalid.Vessels[0].Combat!.Hull=0;Assert.Throws<ArgumentException>(()=>SaveGame.WriteFile(path,g.Content,invalid));Assert.Equal(bytes,File.ReadAllText(path));Assert.False(File.Exists(path+".tmp"));}finally{Directory.Delete(folder,true);}
    }
    [Fact] public void ForgedRoundProfilesShotsForecastsParticipantsAndGeometryReject()
    {
        var g=Next(CombatTurnTests.Pending(doctrine:CombatDoctrine.Hold));string text=SaveGame.Serialize(g.Content,g.State);int index=0;
        foreach(var edit in new Action<JsonNode>[]
        {
            n=>n["before"]!["ships"]![0]!["max_hull"]=999,n=>n["before"]!["ships"]![0]!["attack"]=99,n=>n["before"]!["ships"]![0]!["armor"]=99,n=>n["before"]!["ships"]![0]!["effective_speed"]=299,
            n=>n["before"]!["ships"]![0]!["role_id"]="missing",n=>n["before"]!["ships"]![0]!["flagship"]=false,n=>n["before"]!["ships"]![0]!["vessel_id"]=999999,
            n=>n["before"]!["sentries"]![0]!["max_hull"]=99,n=>n["before"]!["sentries"]![0]!["attack"]=99,n=>n["before"]!["sentries"]![0]!["armor"]=99,n=>n["before"]!["sentries"]![0]!["pursuit_speed"]=251,
            n=>n["before"]!["route"]!["origin_x"]=int.MinValue,n=>n["before"]!["owner"]=99999,
            n=>n["result"]!["shots"]![0]!["damage"]=1,n=>n["result"]!["shots"]![0]!["target"]!["id"]=99999,
            n=>n["result"]!["ships"]![0]!["after"]=99,n=>n["result"]!["ships"]![0]!["actual_loss"]=1,n=>n["result"]!["destroyed_vessels"]!.AsArray().Add(99999),
            n=>n["forecasts"]![0]!["cohesion_after"]=99,n=>n["participants"]!.AsArray().Clear(),n=>n["participants"]!.AsArray().Add(99999)
        }){var root=JsonNode.Parse(text)!;edit(root["state"]!["combat"]!["encounters"]![0]!["rounds"]![0]!);var result=SaveGame.Deserialize(root.ToJsonString());Assert.False(result.Succeeded,$"Forged round mutation {index++} accepted");}
    }
    [Fact] public void GuardedFoundingAndUnarmedActiveAuthorityCannotBeForged()
    {
        var g=CombatFixtures.Convoy();var founder=g.State.Vessels.Single(v=>v.CarriedPopulation>0);var target=g.State.Planets.First(p=>p.StarId==founder.StationaryStarId);target.PlanetTypeId="verdant_world";target.EnvironmentId="dry";OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(g.State,g.State.Turn);founder.PendingFounding=target.Id;
        Assert.Contains(GameStateValidator.Validate(g.Content,g.State),e=>e.Contains("combat",StringComparison.Ordinal));
        var pending=CombatTurnTests.Pending();var ship=pending.State.Vessels[0];ship.RoleId="scout";ship.Combat!.Hull=60;pending.State.Combat!.Encounters[0].Phase=CombatPhase.Active;
        Assert.Contains(GameStateValidator.Validate(pending.Content,pending.State),e=>e.Contains("combat",StringComparison.Ordinal));
    }
    [Fact] public void EverySerializedCombatMemberIsRequiredIncludingNullableLeaves()
    {
        var g=Next(CombatTurnTests.Pending(doctrine:CombatDoctrine.Hold));string text=SaveGame.Serialize(g.Content,g.State);var source=JsonNode.Parse(text)!;int count=0;
        foreach(var prefix in new[]{new[]{"state","combat"},new[]{"state","empires","0","combat"},new[]{"state","vessels","0","combat"},new[]{"content","combat"}})
        foreach(var path in Fields(At(source,prefix),prefix))
        {
            var changed=JsonNode.Parse(text)!;At(changed,path[..^1]).AsObject().Remove(path[^1]);var loaded=SaveGame.Deserialize(changed.ToJsonString());Assert.False(loaded.Succeeded,"Missing required "+string.Join(".",path));count++;
        }
        Assert.True(count>150);
        static JsonNode At(JsonNode root,string[] path){foreach(string part in path)root=root is JsonArray a?a[int.Parse(part,System.Globalization.CultureInfo.InvariantCulture)]!:root[part]!;return root;}
        static IEnumerable<string[]> Fields(JsonNode? n,string[] path)
        {
            if(n is JsonObject o)foreach(var pair in o){var next=path.Append(pair.Key).ToArray();yield return next;foreach(var field in Fields(pair.Value,next))yield return field;}
            else if(n is JsonArray a)for(int i=0;i<a.Count;i++)foreach(var field in Fields(a[i],path.Append(i.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray()))yield return field;
        }
    }
    [Fact] public void RenamedCombatRolesContinueEverySavedPhaseFromEmbeddedContent()
    {
        // Explicit pre-session alternate-content supplement; no generated gameplay claim.
        static string Rename(string text) => text
            .Replace("\"escort\"", "\"guardian\"", StringComparison.Ordinal)
            .Replace("\"raider_sentry\"", "\"gate_machine\"", StringComparison.Ordinal);
        foreach (var source in Cases())
        {
            string alternate = Rename(SaveGame.Serialize(source.Content, source.State));
            var loaded = SaveGame.Deserialize(alternate);
            Assert.True(loaded.Succeeded, string.Join(";", loaded.Errors));
            var direct = loaded.Game!;
            Assert.True(direct.Content.Vessels.ContainsKey("guardian"));
            Assert.False(direct.Content.Vessels.ContainsKey("escort"));
            Assert.Equal("gate_machine", direct.Content.Combat!.SentryRoleId);
            var copy = SaveGame.Deserialize(SaveGame.Serialize(direct.Content, direct.State)).Game!;
            var reference = source;
            for (int i = 0; i < 3; i++)
            {
                var expected = TurnResolver.Resolve(reference.Content, reference.State);
                var a = TurnResolver.Resolve(direct.Content, direct.State);
                var b = TurnResolver.Resolve(copy.Content, copy.State);
                Assert.Equal(expected.Succeeded, a.Succeeded);
                Assert.Equal(a.Succeeded, b.Succeeded);
                Assert.Equal(CombatFixtures.Bytes(a.State), CombatFixtures.Bytes(b.State));
                var normalized = SaveGame.Deserialize(Rename(SaveGame.Serialize(reference.Content, expected.State))).Game!;
                Assert.Equal(SaveGame.Serialize(normalized.Content, normalized.State), SaveGame.Serialize(direct.Content, a.State));
                if (!a.Succeeded) break; // Explicit response remains blocking on both continuations.
                direct = new(direct.Content, a.State);
                copy = new(copy.Content, b.State);
                reference = new(reference.Content, expected.State);
                Assert.Empty(GameStateValidator.Validate(direct.Content, direct.State));
            }
        }
    }
    [Fact] public void TrimmedRoundAndEventHistoryLoadsWithoutResetOrReplay()
    {
        var g=CombatTurnTests.Pending(doctrine:CombatDoctrine.Hold);var d=g.Content.Combat! with{RoleProfiles=g.Content.Combat.RoleProfiles.SetItem("escort",new(int.MaxValue,1,0)),SentryProfile=new(int.MaxValue,1,0)};g=new(g.Content with{Combat=d},g.State);g.State.Vessels[0].Combat!.Hull=int.MaxValue;
        foreach(var sentry in g.State.Combat!.Pockets[0].Sentries){sentry.Profile=d.SentryProfile;sentry.Hull=int.MaxValue;}g.State.Empires[0].Combat!.Observations.Clear();CombatObservations.Refresh(g.Content,g.State,g.State.Turn);
        for(int i=0;i<25;i++)g=Next(g);
        var encounter=g.State.Combat!.Encounters[0];Assert.Equal(25,encounter.Round);Assert.Equal(20,encounter.Rounds.Count);Assert.Equal(6,encounter.Rounds[0].Round);var owner=g.State.Empires[0].Combat!;Assert.Equal(20,owner.PendingEvents.Count+owner.RecentEvents.Count);long eventId=owner.NextEventId;long hullLost=owner.LifetimeHullLost;
        string text=SaveGame.Serialize(g.Content,g.State);var loaded=SaveGame.Deserialize(text);Assert.True(loaded.Succeeded,string.Join(";",loaded.Errors));Assert.Equal(text,SaveGame.Serialize(loaded.Game!.Content,loaded.Game.State));
        var next=Next(loaded.Game);Assert.Equal(26,next.State.Combat!.Encounters[0].Round);Assert.Equal(eventId+1,next.State.Empires[0].Combat!.NextEventId);Assert.Equal(hullLost+2,next.State.Empires[0].Combat!.LifetimeHullLost);Assert.Equal(CombatFixtures.Bytes(Next(g).State),CombatFixtures.Bytes(next.State));
        loaded.Game.State.Combat!.Encounters[0].Rounds.Clear();Assert.Equal(20,g.State.Combat.Encounters[0].Rounds.Count);Assert.Equal(20,next.State.Combat.Encounters[0].Rounds.Count);
    }
}
