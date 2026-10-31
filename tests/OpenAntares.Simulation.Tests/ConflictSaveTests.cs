using System;
using System.Linq;
using System.IO;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Turns;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictSaveTests
{
    [Fact] public void HistoricalLossAndNearLimitDatesRoundTripWithoutUpgrade()
    {
        var g=ConflictFixtures.WithEncounter();var lost=g.State.Conflict!.Encounters[0].LowSide.Flagship;g.State.FindVessel(lost)!.Combat!.Hull=1;ConflictObservations.Refresh(g.Content,g.State);ConflictAtomicTurnTests.Advance(g);Assert.Null(g.State.FindVessel(lost));RoundTrip(g);
        g.State.Turn=int.MaxValue-1;foreach(var owner in g.State.Empires)owner.Scouting!.PendingDigest=null;var pair=g.State.Conflict!.Pairs[0];pair.PendingPeace=new(){Id=new(1),Proposer=pair.Pair.Low,CreatedTurn=g.State.Turn,ExpiryTurn=(long)g.State.Turn+g.Content.Conflict!.OfferLifetime};pair.NextPeaceOfferId=2;ObservationUpdater.Refresh(g.State,g.State.Turn);ConflictObservations.Refresh(g.Content,g.State);var near=RoundTrip(g);Assert.True(near.State.Conflict!.Pairs[0].PendingPeace!.ExpiryTurn>int.MaxValue);Assert.NotNull(near.View(pair.Pair.Low).Conflict);
    }
    internal static GameSession RoundTrip(GameSession g)
    {
        string before=SaveGame.Serialize(g.Content,g.State);var loaded=SaveGame.Deserialize(before);Assert.True(loaded.Succeeded,string.Join(";",loaded.Errors));var result=new GameSession(loaded.Game!.Content,loaded.Game.State);Assert.Equal(before,SaveGame.Serialize(result.Content,result.State));Assert.Equal(12,result.State.RulesVersion);return result;
    }
    public static object[][] Phases=>new[]{"start","declaration","revoked","war","preferences","attack","pending","active","withdraw","response","localbreak","peace-intents","offer","answer","expiry","truce","blockade","contested","refit-interrupted","destroyed"}.Select(s=>new object[]{s}).ToArray();
    // Labelled pre-session save-shape supplements, never earned campaign evidence.
    internal static GameSession Case(string phase)
    {
        if(phase=="start")return ConflictFixtures.Create(humans:2);
        if(phase is "declaration" or "revoked" or "war")
        {
            var s=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));var a=s.State.Empires[0].Id;var b=s.State.Empires[1].Id;ConflictDeclarationTests.Issue(s,a,new ConflictAction.DeclareWar(b));if(phase=="revoked")ConflictDeclarationTests.Issue(s,a,new ConflictAction.RevokeDeclaration(b));if(phase=="war")ConflictAtomicTurnTests.Advance(s);return s;
        }
        if(phase is "preferences" or "attack")
        {
            var s=ConflictOrderTests.LocalForces();var attack=ConflictOrderTests.Attack(s);ConflictDeclarationTests.Issue(s,s.State.Empires[0].Id,phase=="attack"?attack:new ConflictAction.ConfigureDefense(s.State.FindEmpire(s.State.Empires[0].Id)!.Fleets!.Fleets[0].Flagship,attack.Orders with{Doctrine=CombatDoctrine.Press}));return s;
        }
        if(phase is "blockade" or "contested")
        {
            var s=ConflictBlockadeTests.Activate(ConflictBlockadeTests.Economic());if(phase=="contested"){var b=s.State.Empires[1].Id;var colony=s.State.Colonies.First(c=>c.EmpireId==b);ConflictFixtures.AddEscort(s,b,s.State.FindPlanet(colony.PlanetId)!.StarId);ConflictObservations.Refresh(s.Content,s.State);}return s;
        }
        if(phase=="refit-interrupted")
        {
            var s=ConflictAftermathTests.CompletingRefit();ConflictDeclarationTests.Issue(s,s.State.Empires[0].Id,ConflictOrderTests.Attack(s));ConflictAtomicTurnTests.Advance(s);return s;
        }
        var g=ConflictFixtures.WithEncounter();var low=g.State.Empires[0].Id;var high=g.State.Empires[1].Id;
        if(phase=="destroyed"){g.State.FindVessel(g.State.Conflict!.Encounters[0].LowSide.Flagship)!.Combat!.Hull=1;ConflictObservations.Refresh(g.Content,g.State);ConflictAtomicTurnTests.Advance(g);return g;}
        if(phase is "peace-intents" or "truce")
        {foreach(var owner in new[]{low,high})ConflictDeclarationTests.Issue(g,owner,new ConflictAction.ProposePeace(owner==low?high:low));if(phase=="truce")ConflictAtomicTurnTests.Advance(g);return g;}
        if(phase is "offer" or "answer" or "expiry")
        {
            if(phase=="expiry")g=new(g.Content with{Conflict=g.Content.Conflict! with{OfferLifetime=1}},g.State);ConflictDeclarationTests.Issue(g,low,new ConflictAction.ProposePeace(high));ConflictAtomicTurnTests.Advance(g);if(phase=="answer")ConflictDeclarationTests.Issue(g,high,new ConflictAction.AnswerPeace(low,g.State.Conflict!.Pairs[0].PendingPeace!.Id,true));return g;
        }
        if(phase!="pending")ConflictAtomicTurnTests.Advance(g);
        if(phase=="response"){var side=g.State.Conflict!.Encounters[0].LowSide;side.NeedsResponse=true;side.ConsentRevision++;}
        if(phase is "withdraw" or "localbreak")
        {
            foreach(var owner in phase=="withdraw"?new[]{low}:new[]{low,high}){var e=g.View(owner).Conflict!.Encounters[0];ConflictDeclarationTests.Issue(g,owner,new ConflictAction.Withdraw(e.Id,e.Revision));}if(phase=="localbreak")ConflictAtomicTurnTests.Advance(g);
        }
        return g;
    }
    [Theory,MemberData(nameof(Phases))] public void EveryLivePhaseContinuesForThreeIdenticalBoundaries(string phase)
    {
        var a=Case(phase);var b=RoundTrip(a);
        for(int i=0;i<3;i++)
        {
            foreach(var g in new[]{a,b})
            {
                foreach(var owner in g.State.Empires.Where(e=>e.Controller==ControllerKind.Human).Select(e=>e.Id).ToArray())foreach(var e in g.View(owner).Conflict!.Encounters.Where(e=>e.OwnNeedsResponse).ToArray())ConflictDeclarationTests.Issue(g,owner,new ConflictAction.Respond(e.Id,e.Revision));
                ConflictAtomicTurnTests.Advance(g);
            }
            Assert.Equal(SaveGame.Serialize(a.Content,a.State),SaveGame.Serialize(b.Content,b.State));
        }
    }
    [Fact] public void OldRulesRemainOldUnderWriter13AndTwelveTwelveIsRejected()
    {
        var manifest=JsonNode.Parse(File.ReadAllText(CompatibilityFixtures.PathFor("rules11-conflict-baselines/manifest.json")))!;string source=manifest["cases"]![0]!["save"]!.GetValue<string>();var loaded=SaveGame.ReadFile(CompatibilityFixtures.PathFor("rules11-conflict-baselines/"+source));Assert.True(loaded.Succeeded);var before=CompatibilityFixtures.CanonicalWithoutSchema(loaded.Game!.Content,loaded.Game.State);var saved=SaveGame.Serialize(loaded.Game.Content,loaded.Game.State);Assert.Equal(13,JsonNode.Parse(saved)!["schema_version"]!.GetValue<int>());var old=SaveGame.Deserialize(saved);Assert.True(old.Succeeded);Assert.Equal(before,CompatibilityFixtures.CanonicalWithoutSchema(old.Game!.Content,old.Game.State));var continuation=TurnResolver.Resolve(old.Game.Content,old.Game.State);Assert.True(continuation.Succeeded);Assert.Equal(11,continuation.State.RulesVersion);Assert.Null(continuation.State.Conflict);
        var node=JsonNode.Parse(SaveGame.Serialize(Case("active").Content,Case("active").State))!;node["schema_version"]=12;Assert.False(SaveGame.Deserialize(node.ToJsonString()).Succeeded);
    }
    [Theory][InlineData("missing-state")][InlineData("null-state")][InlineData("missing-nested")][InlineData("unknown-nested")][InlineData("duplicate-property")][InlineData("numeric-phase")][InlineData("algorithm")][InlineData("future-date")][InlineData("live-duplicate")][InlineData("counter-global")][InlineData("counter-owner")][InlineData("counter-peace")][InlineData("civilian-shot")][InlineData("empty-shots")][InlineData("forged-observation")][InlineData("attack-civilian")][InlineData("repair-in-battle")]
    public void MalformedConflictIsRejectedWithoutPublishingIt(string corruption)
    {
        var g=Case(corruption=="attack-civilian"?"attack":"active");string source=SaveGame.Serialize(g.Content,g.State);var node=JsonNode.Parse(source)!;var conflict=node["state"]!["conflict"]!;var encounter=conflict["encounters"]!.AsArray().FirstOrDefault();
        switch(corruption)
        {
            case "missing-state":node["state"]!.AsObject().Remove("conflict");break;
            case "null-state":node["state"]!["conflict"]=null;break;
            case "missing-nested":encounter!["low_side"]!["orders"]!.AsObject().Remove("on_arrival");break;
            case "unknown-nested":encounter!["low_side"]!["orders"]!["conquest"]=true;break;
            case "duplicate-property":string key="\"next_encounter_id\": "+conflict["next_encounter_id"]!.ToJsonString();Assert.Contains(key,source);Assert.False(SaveGame.Deserialize(source.Replace(key,key+", "+key,StringComparison.Ordinal)).Succeeded);return;
            case "numeric-phase":encounter!["phase"]=0;break;
            case "algorithm":node["content"]!["conflict"]!["algorithm_id"]="conquest_v1";break;
            case "future-date":encounter!["created_turn"]=g.State.Turn+1;break;
            case "live-duplicate":encounter!["high_side"]!["members"]![0]=encounter["low_side"]!["members"]![0]!.DeepClone();break;
            case "counter-global":conflict["next_encounter_id"]=long.MaxValue;break;
            case "counter-owner":conflict["owners"]![0]!["next_event_id"]=long.MaxValue;break;
            case "counter-peace":conflict["pairs"]![0]!["next_peace_offer_id"]=long.MaxValue;break;
            case "civilian-shot":encounter!["rounds"]![0]!["shots"]![0]!["target"]!["id"]=g.State.Vessels.First(v=>v.Design is null).Id.Value;break;
            case "empty-shots":encounter!["rounds"]![0]!["shots"]=new JsonArray();break;
            case "forged-observation":conflict["owners"]![0]!["observations"]![0]!["hull"]=99;break;
            case "attack-civilian":var owner=g.State.Empires[1].Id;conflict["owners"]![0]!["attacks"]![0]!["target"]=g.State.Vessels.First(v=>v.EmpireId==owner&&v.Design is null).Id.Value;break;
            case "repair-in-battle":var id=encounter!["low_side"]!["members"]![0]!.GetValue<int>();var ship=node["state"]!["vessels"]!.AsArray().Single(v=>v!["id"]!.GetValue<int>()==id)!;ship["combat"]!["repairing"]=true;ship["combat"]!["return_colony"]=g.State.Colonies.First(c=>c.EmpireId==g.State.Empires[0].Id).Id.Value;ship["combat"]!["on_arrival"]="repair";break;
        }
        Assert.False(SaveGame.Deserialize(node.ToJsonString()).Succeeded,corruption);Assert.Equal(source,SaveGame.Serialize(g.Content,g.State));
    }
    [Fact] public void FailedLoadKeepsSessionAndFailedWriteKeepsDestinationBytes()
    {
        var g=Case("active");var original=g.State;string repo=Path.GetFullPath(Path.Combine(ContentLoaderTests.CoreContentDirectory(),"../.."));string folder=Path.Combine(repo,"reports/conflict-execution/save-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string path=Path.Combine(folder,"retained.txt");File.WriteAllText(path,"retained destination bytes");Assert.False(g.Load(path).Succeeded);Assert.Same(original,g.State);var invalid=g.State.Clone();invalid.Conflict!.NextEncounterId=0;Assert.Throws<ArgumentException>(()=>SaveGame.WriteFile(path,g.Content,invalid));Assert.Equal("retained destination bytes",File.ReadAllText(path));
    }
    [Fact] public void AnOwnRefitCannotAlsoReserveAnAttack()
    {
        var g=ConflictAftermathTests.CompletingRefit();var high=g.State.Empires[1];var job=high.ShipDesigns!.Refits[0];var ship=g.State.FindVessel(job.Vessel)!;var data=high.Fleets!;var fleet=new FleetRecord{Id=new(data.NextFleetId++),Name="Labelled refit-bound force",Flagship=ship.Id,Members=new(){ship.Id}};data.Fleets.Add(fleet);ship.Fleet=fleet.Id;Assert.Empty(GameStateValidator.Validate(g.Content,g.State));var node=JsonNode.Parse(SaveGame.Serialize(g.Content,g.State))!;
        node["state"]!["conflict"]!["owners"]![1]!["attacks"]!.AsArray().Add(new JsonObject{["fleet"]=fleet.Id.Value,["target"]=g.State.Vessels.First(v=>v.EmpireId!=high.Id&&v.Design is not null).Id.Value,["turn"]=g.State.Turn,["revision"]=1,["orders"]=new JsonObject{["doctrine"]="cautious",["return_colony"]=job.Port.Value,["stragglers"]="keep_together",["on_arrival"]="repair"}});Assert.False(SaveGame.Deserialize(node.ToJsonString()).Succeeded);
    }
}
