using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacySaveTests
{
    [Fact] public void All67PairsAndLiveDiplomacyStagesRoundTrip()
    {
        Assert.Equal(12,SaveGame.SchemaVersion);
        string[] old={"legacy-v2-save.json","legacy-rules2-v3-save.json","legacy-rules3-v4-save.json","legacy-rules4-v5-save.json","legacy-rules5-v6-save.json"};
        var sources=Enumerable.Range(1,11).ToDictionary(r=>r,r=>r==11?Encode(Case("offer")):r==10?SaveGame.Serialize(ShipDesignFixtures.Session().Content,ShipDesignFixtures.Session().State):r==9?SaveGame.Serialize(CombatFixtures.Game().Content,CombatFixtures.Game().State):r==8?SaveGame.Serialize(FleetFixtures.Game().Content,FleetFixtures.Game().State):r==7?SaveGame.Serialize(ScoutingFixtures.Content(),ScoutingFixtures.Game().State):r==6?SaveGame.Serialize(CrisisFixtures.Content(),CrisisFixtures.Game().State):File.ReadAllText(CompatibilityFixtures.PathFor(old[r-1])));
        int accepted=0;
        for(int schema=1;schema<=13;schema++)for(int rules=1;rules<=12;rules++)
        {
            var n=JsonNode.Parse(sources[Math.Min(rules,11)])!;n["schema_version"]=schema;n["rules_version"]=rules;
            if(schema==1&&rules==1)n=JsonNode.Parse(File.ReadAllText(CompatibilityFixtures.PathFor("legacy-v1-save.json")))!;
            bool expected=(schema,rules) is (1,1) or (2,1)||schema is >=3 and <=12&&rules<schema;
            var loaded=SaveGame.Deserialize(n.ToJsonString());Assert.True(expected==loaded.Succeeded,$"({schema},{rules}): {string.Join("; ",loaded.Errors)}");if(loaded.Succeeded)accepted++;
        }
        Assert.Equal(67,accepted);
    }
    [Theory] [InlineData("unmet")][InlineData("offer")][InlineData("answered")][InlineData("declined")][InlineData("revoked")][InlineData("warmup")][InlineData("notice")][InlineData("cooldown")][InlineData("first_ready")][InlineData("lost_hub")]
    public void EveryLivePhaseRoundTripsAndContinuesExactly(string phase)
    {
        var a=Case(phase);string source=Encode(a);var loaded=SaveGame.Deserialize(source);Assert.True(loaded.Succeeded,string.Join("; ",loaded.Errors));var b=new GameSession(loaded.Game!.Content,loaded.Game.State);Assert.Equal(source,Encode(b));
        for(int n=0;n<3;n++)
        {
            var x=TurnResolver.Resolve(a.Content,a.State);var y=TurnResolver.Resolve(b.Content,b.State);
            Assert.True(x.Succeeded,string.Join("; ",x.Errors));Assert.True(y.Succeeded,string.Join("; ",y.Errors));
            Assert.Equal(SaveGame.Serialize(a.Content,x.State),SaveGame.Serialize(b.Content,y.State));Assert.Equal(JsonSerializer.Serialize(x.Report!.DiplomacyEvents),JsonSerializer.Serialize(y.Report!.DiplomacyEvents));
            a=new(a.Content,x.State);var round=SaveGame.Deserialize(SaveGame.Serialize(b.Content,y.State));Assert.True(round.Succeeded);b=new(round.Game!.Content,round.Game.State);
        }
    }
    [Theory] [InlineData("missing")][InlineData("unknown")][InlineData("null")][InlineData("counter")][InlineData("duplicate_pair")][InlineData("earmark")][InlineData("asymmetric")][InlineData("algorithm")][InlineData("old_rules")][InlineData("origin_wrongtype")][InlineData("event_payment")][InlineData("event_duplicate")]
    public void MissingForgedUnsupportedAndOverdrawnStateRejects(string mutation)
    {
        var game=Case(mutation is "origin_wrongtype" or "event_payment" or "event_duplicate"?"warmup":"answered");var node=JsonNode.Parse(Encode(game))!;var d=node["state"]!["diplomacy"]!;var pair=d["pairs"]![0]!;
        switch(mutation)
        {
            case "missing":d.AsObject().Remove("blocked_founders");break;
            case "unknown":d["free_trade_bonus"]=100;break;
            case "null":d["pairs"]=null;break;
            case "counter":pair["next_offer_id"]=0;break;
            case "duplicate_pair":d["pairs"]!.AsArray().Add(pair.DeepClone());break;
            case "earmark":pair["pending"]!["proposer_hub"]!["reserved"]=501;break;
            case "asymmetric":node["state"]!["empires"]![0]!["contacts"]!.AsArray().Clear();break;
            case "algorithm":node["content"]!["diplomacy"]!["algorithm_id"]="unknown_v2";break;
            case "old_rules":node["rules_version"]=10;break;
            case "event_payment":pair["last_results"]![0]!["paid"]=-1;break;
            case "event_duplicate":pair["last_results"]!.AsArray().Add(pair["last_results"]![0]!.DeepClone());break;
            case "origin_wrongtype":pair["agreements"]![0]!["low_hub"]!["hub"]=game.State.Vessels[0].Id.Value;break;
        }
        Assert.False(SaveGame.Deserialize(node.ToJsonString()).Succeeded);
    }
    [Fact] public void DuplicateFieldsAndUnknownEnumReject()
    {
        string source=Encode(Case("offer"));Assert.False(SaveGame.Deserialize(source.Replace("\"next_offer_id\": 2","\"next_offer_id\": 2, \"next_offer_id\": 2")).Succeeded);
        var n=JsonNode.Parse(source)!;n["state"]!["diplomacy"]!["pairs"]![0]!["pending"]!["kind"]="TRADE";Assert.False(SaveGame.Deserialize(n.ToJsonString()).Succeeded);
    }
    internal static GameSession Case(string phase)
    {
        if(phase=="unmet")return DiplomacyFixtures.Create();
        if(phase is "warmup" or "notice" or "lost_hub")
        {
            var s=DiplomacyTradeTests.Activated();
            if(phase=="notice"){s.State.Turn=11;ObservationUpdater.Refresh(s.State,11);Assert.True(DiplomacyCommandTests.Act(s,s.State.Empires[0].Id,new DiplomacyAction.Terminate(AgreementKind.Trade)).Accepted);}
            if(phase=="lost_hub")
            {
                // Labelled certified historical-loss supplement; not earned campaign progression.
                s.State.Crisis!.Enabled=true;foreach(var e in s.State.Empires)e.Crisis!.NextEligibleTurn=10000;
                var pair=s.State.Diplomacy!.Pairs[0];var owner=s.State.FindEmpire(pair.Pair.Low)!;var home=s.State.Colonies.First(c=>c.EmpireId==owner.Id);
                var historical=new ColonyId(s.State.AllocateId());var location=CrisisQueries.Location(s.State,home) with{Colony=historical};owner.Crisis!.LostColonyLocations.Add(location);
                pair.Agreements[0].LowHub=new(historical,owner.Id,1);
                for(int i=0;i<pair.LastResults.Count;i++)if(pair.LastResults[i].Viewer==owner.Id)pair.LastResults[i]=pair.LastResults[i] with{OwnHub=historical};
            }
            Assert.Empty(GameStateValidator.Validate(s.Content,s.State));return s;
        }
        var game=DiplomacyOfferResolutionTests.Offered(phase is "answered" or "revoked" or "first_ready",phase=="cooldown"?2:0);var p=game.State.Diplomacy!.Pairs[0];
        if(phase=="declined")Assert.True(DiplomacyCommandTests.Act(game,p.Pending!.Recipient,new DiplomacyAction.Answer(p.Pending.Id,false,null)).Accepted);
        if(phase=="revoked")Assert.True(DiplomacyCommandTests.Act(game,p.Pending!.Recipient,new DiplomacyAction.RevokeAnswer(p.Pending.Id)).Accepted);
        if(phase=="first_ready")Assert.True(game.Execute(new SetReadyCommand(game.State.Empires[0].Id,0,true)).Accepted);
        if(phase=="cooldown"){var c=game.State.Clone();DiplomacyResolver.ResolveOffers(game.Content,game.State,c);c.Turn=3;ObservationUpdater.Refresh(c,3);return new(game.Content,c);}
        return game;
    }
    private static string Encode(GameSession s)=>SaveGame.Serialize(s.Content,s.State);
}
