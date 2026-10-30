using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ShipDesignSaveTests
{
    [Fact]
    public void All56PairsAndOnlyThosePairsLoad()
    {
        string[] old={"legacy-v2-save.json","legacy-rules2-v3-save.json","legacy-rules3-v4-save.json","legacy-rules4-v5-save.json","legacy-rules5-v6-save.json"};
        var sources=Enumerable.Range(1,10).ToDictionary(r=>r,r=>r==10?Encode(ShipDesignFixtures.Session()):r==9?SaveGame.Serialize(CombatFixtures.Game().Content,CombatFixtures.Game().State):r==8?SaveGame.Serialize(FleetFixtures.Game().Content,FleetFixtures.Game().State):r==7?SaveGame.Serialize(ScoutingFixtures.Content(),ScoutingFixtures.Game().State):r==6?SaveGame.Serialize(CrisisFixtures.Content(),CrisisFixtures.Game().State):File.ReadAllText(CompatibilityFixtures.PathFor(old[r-1])));int accepted=0;
        for(int schema=1;schema<=12;schema++)for(int rules=1;rules<=11;rules++)
        {
            var n=JsonNode.Parse(sources[Math.Min(rules,10)])!;n["schema_version"]=schema;n["rules_version"]=rules;if(schema==1&&rules==1)n=JsonNode.Parse(File.ReadAllText(CompatibilityFixtures.PathFor("legacy-v1-save.json")))!;
            bool expected=(schema,rules) is (1,1) or (2,1)||schema is >=3 and <=11&&rules<schema;var loaded=SaveGame.Deserialize(n.ToJsonString());Assert.True(expected==loaded.Succeeded,$"({schema},{rules}): {string.Join("; ",loaded.Errors)}");if(loaded.Succeeded)accepted++;
        }
        Assert.Equal(56,accepted);Assert.Equal(11,SaveGame.SchemaVersion);
    }
    public static IEnumerable<object[]> Phases()=>new[]{"off","catalogue","queue","request","completed_request","completed_request_refitted","active_refit","paused_refit","insufficient_refit","first_ready","gun_battle","frigate_damaged"}.Select(s=>new object[]{s});
    [Theory,MemberData(nameof(Phases))]
    public void CatalogueQueueRequestAndRefitRoundTripContinueExactly(string phase)
    {
        var game=Case(phase);var bytes=JsonSerializer.Serialize(game.State);string source=Encode(game);var restored=Read(source);Assert.Equal(source,Encode(restored));Assert.Equal(bytes,JsonSerializer.Serialize(restored.State));Assert.Equal(bytes,JsonSerializer.Serialize(game.State));
        for(int i=0;i<3;i++)
        {
            var a=TurnResolver.Resolve(game.Content,game.State);var b=TurnResolver.Resolve(restored.Content,restored.State);Assert.True(a.Succeeded,string.Join(";",a.Errors));Assert.True(b.Succeeded,string.Join(";",b.Errors));Assert.Equal(ReportFacts(a.Report!),ReportFacts(b.Report!));Assert.Equal(SaveGame.Serialize(game.Content,a.State),SaveGame.Serialize(restored.Content,b.State));game=new(game.Content,a.State);restored=Read(SaveGame.Serialize(restored.Content,b.State));
        }
    }
    [Fact]
    public void MissingUnknownNullForgedAndDuplicateFieldsReject()
    {
        string text=Encode(Case("active_refit"));
        foreach(var edit in new Action<JsonNode>[]
        {
            n=>n["state"]!["empires"]![0]!.AsObject().Remove("ship_designs"),n=>n["state"]!["empires"]![0]!["ship_designs"]=null,n=>Data(n).AsObject().Remove("next_refit_id"),n=>Data(n)["designs"]=null,n=>Data(n)["unknown"]=1,n=>Data(n)["next_design_id"]=2,n=>Data(n)["catalogue_revision"]=0,n=>Data(n)["designs"]![1]!["name"]="standard escort",n=>Data(n)["designs"]![1]!["recipe"]!["engine_id"]="missing",n=>Data(n)["designs"]![1]!["recipe"]!.AsObject().Remove("armor_id"),
            n=>Data(n)["refits"]![0]!["status"]="Active",n=>Data(n)["refits"]![0]!["status"]=0,n=>Data(n)["refits"]![0]!["price"]=699,n=>Data(n)["refits"]![0]!["target_design"]=99,n=>Data(n)["refits"]!.AsArray().Add(Data(n)["refits"]![0]!.DeepClone()),n=>Data(n)["pending_events"]![0]!["after"]!["stats"]!["base_speed"]=999,n=>Data(n)["pending_events"]![0]!["paid"]=1,n=>Data(n)["pending_events"]![0]!["kind"]="invented_success",
            n=>n["state"]!["vessels"]![0]!.AsObject().Remove("design"),n=>n["state"]!["vessels"]![0]!["design"]=999,n=>n["content"]!.AsObject().Remove("ship_designs"),n=>n["state"]!["empires"]![0]!["known_technologies"]!.AsArray().Clear()
        }){var n=JsonNode.Parse(text)!;edit(n);var loaded=SaveGame.Deserialize(n.ToJsonString());Assert.False(loaded.Succeeded);Assert.NotEmpty(loaded.Errors);}
        var duplicate=text.Replace("\"next_refit_id\": 2","\"next_refit_id\": 2, \"next_refit_id\": 2",StringComparison.Ordinal);Assert.NotEqual(text,duplicate);Assert.False(SaveGame.Deserialize(duplicate).Succeeded);
    }
    [Fact]
    public void RulesOneThroughNineOmitAndRejectEveryNewFieldAndTechnologyFlag()
    {
        var current=JsonNode.Parse(Encode(Case("catalogue")))!;
        foreach(var version in Enumerable.Range(1,9))
        {
            var source=version<=5?File.ReadAllText(CompatibilityFixtures.PathFor(new[]{"legacy-v2-save.json","legacy-rules2-v3-save.json","legacy-rules3-v4-save.json","legacy-rules4-v5-save.json","legacy-rules5-v6-save.json"}[version-1])):version==6?SaveGame.Serialize(CrisisFixtures.Content(),CrisisFixtures.Game().State):version==7?SaveGame.Serialize(ScoutingFixtures.Content(),ScoutingFixtures.Game().State):version==8?SaveGame.Serialize(FleetFixtures.Game().Content,FleetFixtures.Game().State):SaveGame.Serialize(CombatFixtures.Game().Content,CombatFixtures.Game().State);
            var old=Read(source);source=Encode(old);Assert.DoesNotContain("ship_design",source);Assert.DoesNotContain("vector_drive",source);Assert.DoesNotContain("catalogue_revision",source);
            foreach(var field in new[]{"content","empire","vessel","technology"})
            {
                var n=JsonNode.Parse(source)!;if(field=="content")n["content"]!["ship_designs"]=current["content"]!["ship_designs"]!.DeepClone();else if(field=="empire")n["state"]!["empires"]![0]!["ship_designs"]=Data(current).DeepClone();else if(field=="vessel")n["state"]!["vessels"]?.AsArray().FirstOrDefault()?.AsObject().Add("design",null);else n["content"]!["technologies"]![0]!["ship_design_only"]=false;
                if(field!="vessel"||(n["state"]!["vessels"]?.AsArray().Count??0)>0)Assert.False(SaveGame.Deserialize(n.ToJsonString()).Succeeded);
            }
        }
    }
    [Fact]
    public void HistoricalGovernorTargetsSurviveLegalCatalogueDeletion()
    {
        var game=RefitResolutionTests.Economic(0,0);var owner=game.State.Empires[0];var port=game.State.Colonies[0];Do(game,new SetColonyQueueCommand(owner.Id,10,port.Id,ImmutableArray.Create(new QueueEntryDraft(null,ShipDesignTargets.Build(new(2))))));Do(game,new SetColonyAutomationCommand(owner.Id,10,port.Id,false,true));Do(game,new SetReadyCommand(owner.Id,10,true));
        Assert.Contains(game.State.Empires[0].Development!.CompletedReports.SelectMany(r=>r.Entries),e=>e.AfterTarget==ShipDesignTargets.Build(new(2)));Do(game,new SetProductionTargetCommand(owner.Id,11,port.Id,null));Do(game,new SetColonyQueueCommand(owner.Id,11,port.Id,ImmutableArray<QueueEntryDraft>.Empty));Design(game,new ShipDesignAction.Archive(new(2)));Design(game,new ShipDesignAction.Delete(new(2)));var restored=Read(Encode(game));Assert.DoesNotContain(restored.State.Empires[0].ShipDesigns!.Designs,d=>d.Id.Value==2);Assert.Equal(Encode(game),Encode(restored));
    }
    [Theory]
    [InlineData(1)][InlineData(45)][InlineData(46)][InlineData(90)][InlineData(91)][InlineData(135)][InlineData(136)][InlineData(180)]
    public void SavedFrigatePreservesEveryDamageBandBoundary(int hull)
    {
        var game=Case("frigate_damaged");game.State.Vessels[0].Combat!.Hull=hull;var before=game.View(game.State.Empires[0].Id).Combat!.Ships.Single();var restored=Read(Encode(game));var after=restored.View(restored.State.Empires[0].Id).Combat!.Ships.Single();Assert.Equal(before,after);Assert.Equal(hull,restored.State.Vessels[0].Combat!.Hull);Assert.Equal(180,VesselLoadouts.Resolve(restored.Content,restored.State,restored.State.Vessels[0]).Profile!.MaxHull);
    }
    [Fact]
    public void EmbeddedAlternateRoleAndComponentNamesRemainIndependentOfNewContent()
    {
        var game=ShipLoadoutIntegrationTests.Game("fast");var c=game.Content;var d=c.ShipDesigns!;var vector=d.Components["vector_drive"];var roles=c.Vessels.Remove(d.WarshipRoleId).Add("patrol_hull",c.Vessels[d.WarshipRoleId] with{Id="patrol_hull"});var profiles=c.Combat!.RoleProfiles.Remove(d.WarshipRoleId).Add("patrol_hull",c.Combat.RoleProfiles[d.WarshipRoleId]);var parts=d.Components.Remove(vector.Id).Add("unusual_engine",vector with{Id="unusual_engine"});
        c=c with{Vessels=roles,Combat=c.Combat with{RoleProfiles=profiles},ShipDesigns=d with{WarshipRoleId="patrol_hull",Components=parts}};game.State.Vessels[0].RoleId="patrol_hull";var data=game.State.Empires[0].ShipDesigns!;data.Designs[1]=data.Designs[1] with{Recipe=data.Designs[1].Recipe with{EngineId="unusual_engine"}};game=new(c,game.State);var saved=Encode(game);var restored=Read(saved);
        var later=c with{ShipDesigns=c.ShipDesigns! with{Components=parts.SetItem("unusual_engine",parts["unusual_engine"] with{Value=900})}};Assert.Equal(900,VesselLoadouts.Resolve(later,game.State,game.State.Vessels[0]).BaseSpeed);Assert.Equal(400,VesselLoadouts.Resolve(restored.Content,restored.State,restored.State.Vessels[0]).BaseSpeed);Assert.Equal(saved,Encode(restored));Assert.Equal("patrol_hull",restored.State.Vessels[0].RoleId);Assert.Contains("unusual_engine",saved);
    }
    [Fact]
    public void OldCombatReceiptRejectsEvenNullEquipmentField()
    {
        var old=CombatSaveTests.Next(CombatTurnTests.Pending());var n=JsonNode.Parse(SaveGame.Serialize(old.Content,old.State))!;n["state"]!["combat"]!["encounters"]![0]!["rounds"]![0]!["before"]!["ships"]![0]!["equipment"]=null;Assert.False(SaveGame.Deserialize(n.ToJsonString()).Succeeded);
    }
    [Fact]
    public void OversizedDesignIdInSavedRequestRejectsWithoutThrowing()
    {
        var n=JsonNode.Parse(Encode(Case("request")))!;n["state"]!["empires"]![0]!["fleet_command"]!["requests"]![0]!["slots"]![0]!["design"]=long.MaxValue;
        SaveLoadResult? loaded=null;var error=Record.Exception(()=>loaded=SaveGame.Deserialize(n.ToJsonString()));Assert.Null(error);Assert.NotNull(loaded);Assert.False(loaded!.Succeeded);
    }
    [Fact]
    public void FailedReadWriteAndReplacementPreservePreviousSessionAndDestination()
    {
        var game=Case("active_refit");var folder=Path.Combine(Path.GetTempPath(),"openantares-design-save-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            string path=Path.Combine(folder,"save.json"),bad=Path.Combine(folder,"bad.json");game.Save(path);string good=File.ReadAllText(path),before=JsonSerializer.Serialize(game.State);File.WriteAllText(bad,"{}");Assert.False(game.Load(bad).Succeeded);Assert.Equal(before,JsonSerializer.Serialize(game.State));var invalid=game.State.Clone();invalid.Empires[0].ShipDesigns!.NextDesignId=1;Assert.Throws<ArgumentException>(()=>SaveGame.WriteFile(path,game.Content,invalid));Assert.Equal(good,File.ReadAllText(path));
            using(var hold=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){var error=Record.Exception(()=>game.Save(path));Assert.True(error is IOException or UnauthorizedAccessException,error?.ToString());}Assert.Equal(good,File.ReadAllText(path));Assert.False(File.Exists(path+".tmp"));
        }finally{Directory.Delete(folder,true);}
    }
    internal static GameSession Case(string phase)
    {
        if(phase=="first_ready"){var multi=ShipDesignAtomicityTests.MultiRefit();Do(multi,new SetReadyCommand(multi.State.Empires[0].Id,0,true));return multi;}
        if(phase=="gun_battle"){var battle=ShipLoadoutIntegrationTests.Game("gun",false);var action=CombatCommandTests.Attack(battle.State);var p=CombatQueries.Preview(battle.Content,battle.View(battle.State.Empires[0].Id),action);Do(battle,new CombatActionCommand(battle.State.Empires[0].Id,0,action,p.Token));Do(battle,new SetReadyCommand(battle.State.Empires[0].Id,0,true));return battle;}
        if(phase=="frigate_damaged"){var frigate=ShipLoadoutIntegrationTests.Game("frigate");frigate.State.Vessels[0].Combat!.Hull=45;return frigate;}
        if(phase=="off")return ShipDesignFixtures.Session();
        var game=RefitResolutionTests.Economic(phase=="insufficient_refit"?0:700,0);var owner=game.State.Empires[0];var port=game.State.Colonies[0];
        if(phase is "active_refit" or "paused_refit" or "insufficient_refit"){RefitResolutionTests.Start(game);if(phase=="paused_refit"){var job=game.State.Empires[0].ShipDesigns!.Refits.Single();job.Status=RefitStatus.Paused;job.Reason="Labelled saved pause";job.Revision++;}return game;}
        var unused=ShipDesignFixtures.SaveDesign(game,"Archive",game.Content.ShipDesigns!.StartingRecipe);Design(game,new ShipDesignAction.Archive(unused));var deleted=ShipDesignFixtures.SaveDesign(game,"Delete",game.Content.ShipDesigns.StartingRecipe);Design(game,new ShipDesignAction.Archive(deleted));Design(game,new ShipDesignAction.Delete(deleted));
        if(phase=="queue")Do(game,new SetColonyQueueCommand(owner.Id,10,port.Id,ImmutableArray.Create(new QueueEntryDraft(null,ShipDesignTargets.Build(new(2))))));
        if(phase is "request" or "completed_request" or "completed_request_refitted")
        {
            var draft=new AssemblyDraft("Saved exact request",game.State.Vessels[0].StationaryStarId!.Value,null,ImmutableArray<string>.Empty,ImmutableArray.Create(port.Id),RequestPriority.Normal,phase.StartsWith("completed_request",StringComparison.Ordinal)?ImmutableArray.Create(game.State.Vessels[0].Id):ImmutableArray<VesselId>.Empty,false,null){Quotas=ImmutableArray.Create(new AssemblyQuota(game.Content.ShipDesigns.WarshipRoleId,phase.StartsWith("completed_request",StringComparison.Ordinal)?new(1):new(2)))};var preview=AssemblyPlanner.Preview(FleetPlanningQueries.Create(game.Content,game.View(owner.Id)),draft);Assert.True(preview.Allowed,string.Join(";",preview.Rejections));Do(game,new AssemblyCommand(owner.Id,10,new AssemblyAction.Accept(draft,preview.Fingerprint)));if(phase.StartsWith("completed_request",StringComparison.Ordinal))Do(game,new SetReadyCommand(owner.Id,10,true));if(phase=="completed_request_refitted"){RefitResolutionTests.Start(game);Do(game,new SetReadyCommand(owner.Id,11,true));Assert.Equal(new DesignId(1),game.State.Empires[0].Fleets!.Requests.Single().Slots.Single().Design);Assert.Equal(new DesignId(2),game.State.Vessels[0].Design);}
        }
        return game;
    }
    private static JsonNode Data(JsonNode n)=>n["state"]!["empires"]![0]!["ship_designs"]!;
    private static string ReportFacts(TurnReport report)=>JsonSerializer.Serialize(new{Report=report with{ColonyLocations=ImmutableDictionary<ColonyId,OpenAntares.Simulation.Crises.CrisisLocation>.Empty},Locations=report.ColonyLocations.OrderBy(p=>p.Key).ToArray()});
    private static string Encode(GameSession game)=>SaveGame.Serialize(game.Content,game.State);
    private static GameSession Read(string text){var read=SaveGame.Deserialize(text);Assert.True(read.Succeeded,string.Join(";",read.Errors));return new(read.Game!.Content,read.Game.State);}
    private static void Design(GameSession game,ShipDesignAction a)=>Do(game,new ShipDesignCommand(game.State.Empires[0].Id,game.State.Turn,game.State.Empires[0].ShipDesigns!.CatalogueRevision,a));
    private static void Do(GameSession game,Command c){var r=game.Execute(c);Assert.True(r.Accepted,r.Rejection?.Message);}
}
