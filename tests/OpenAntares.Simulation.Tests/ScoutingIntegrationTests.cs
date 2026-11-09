using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ScoutingIntegrationTests
{
    [Fact] public void FinalAcceptanceMapsEveryCriterionToExecutedArtifactsAndPreservation()
    {
        string root=new DirectoryInfo(ContentLoaderTests.CoreContentDirectory()).Parent!.Parent!.FullName;
        string EvidencePath(string relative)=>Path.Combine(relative.StartsWith("reports/",StringComparison.Ordinal)?Path.Combine(root,"tests/OpenAntares.Simulation.Tests/Fixtures/scout-acceptance-20261025"):root,relative);
        string path=EvidencePath("reports/scout-execution/final-machine-summary.json");
        Assert.True(File.Exists(path),"Final acceptance requires actual criterion, runtime and preservation artifacts.");
        var summary=JsonNode.Parse(File.ReadAllText(path))!;
        var criteria=summary["criteria"]!.AsArray();
        Assert.Equal(Enumerable.Range(1,15).Select(n=>$"SCT-AC-{n:000}"),criteria.Select(c=>c!["id"]!.GetValue<string>()));
        foreach(var criterion in criteria)
        {
            var evidence=criterion!["executedEvidence"]!.AsArray();Assert.NotEmpty(evidence);
            foreach(var item in evidence)Assert.True(File.Exists(EvidencePath(item!.GetValue<string>())),item.GetValue<string>());
        }
        var runs=summary["runtime"]!.AsArray();Assert.Equal(7,runs.Count);
        foreach(var run in runs)
        {
            Assert.Equal(0,run!["exitCode"]!.GetValue<int>());Assert.Empty(run["diagnostics"]!.AsArray());
            string folder=EvidencePath(run["artifact"]!.GetValue<string>());
            var result=JsonNode.Parse(File.ReadAllText(Path.Combine(folder,"results.json")))!;Assert.True(result["passed"]!.GetValue<bool>());
            var optIn=JsonNode.Parse(File.ReadAllText(Path.Combine(folder,"opt-in-metadata.json")))!;
            var ordinary=JsonNode.Parse(File.ReadAllText(Path.Combine(folder,"normal-metadata.json")))!;
            Assert.Equal(7,optIn["presentScenarioTypes"]!.AsArray().Count);Assert.Empty(ordinary["presentScenarioTypes"]!.AsArray());Assert.Empty(ordinary["presentHelpers"]!.AsArray());
            foreach(var file in run["files"]!.AsArray())Assert.Equal(file!["sha256"]!.GetValue<string>(),Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(EvidencePath(file["path"]!.GetValue<string>())))).ToLowerInvariant());
        }
        var preservation=summary["preservation"]!;Assert.True(preservation["primaryOwnerHashesUnchanged"]!.GetValue<bool>());
        Assert.Equal(7,preservation["worktrees"]!.AsArray().Count);Assert.Equal(4,preservation["ownerFiles"]!.AsArray().Count);
        Assert.Equal("42e1282b9f38fd8d51d4c99ed0f7481437efcee1",preservation["main"]!.GetValue<string>());
        Assert.True(preservation["indexEmpty"]!.GetValue<bool>());
    }
    private static ContentSet Core()=>ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
    [Fact] public void CurrentCoreStartsExplicitStandardWithSeparateObserveAndSurvivalDefaults()
    {
        Assert.Equal(12,Rules.CurrentVersion);var c=Core();Assert.NotNull(c.SpaceAmeba);Assert.Equal(200,c.SpaceAmeba.PursuitSpeed);Assert.Equal(600,c.SpaceAmeba.ResearchReward);
        var result=GalaxyGenerator.CreateNewGame(c,new("small",42,8,SpeciesId:"harl",RivalCount:1,RivalSpeciesId:"peldra",AmebasEnabled:true));Assert.True(result.Succeeded,result.Error);var s=result.State!;Assert.True(s.Scouting!.Enabled);Assert.Equal(1,s.Scouting.SelectionCount);
        Assert.All(s.Vessels,v=>{Assert.Equal(ScoutPosture.Observe,v.Scout!.Posture);Assert.True(v.Scout.WithdrawFromAmeba);});var session=new GameSession(c,s);string before=SaveGame.Serialize(c,s);for(int n=0;n<3;n++){var view=session.View(s.Empires[0].Id);Assert.Empty(view.Scouting!.Habitats);Assert.Empty(view.Scouting.Attention);session.Forecast();}Assert.Equal(before,SaveGame.Serialize(c,s));
        foreach(bool enabled in new[]{false,true})Assert.False(GalaxyGenerator.CreateNewGame(c with{SpaceAmeba=null},new("small",42,7,"harl",AmebasEnabled:enabled)).Succeeded);
    }
    [Fact] public void TwentyFourUntouchedCoreCampaignsDiscoverWithdrawBuildResearchAndCureByOrdinaryCommands()
    {
        var c=Core();var rows=new List<object>();bool passed=false;
        try
        {
            foreach(string species in new[]{"harl","peldra","sulan","wenth"})foreach(string size in new[]{"small","medium","large"})foreach(int rivals in new[]{0,1})
            {
                var start=GalaxyGenerator.CreateNewGame(c,new(size,42,7,species,rivals,rivals==1?"peldra":null,AmebasEnabled:true));Assert.True(start.Succeeded,start.Error);var initial=start.State!;string initialHash=Hash(SaveGame.Serialize(c,initial));var session=new GameSession(c,initial);var owner=initial.Empires[0].Id;ulong risk=initial.Empires[0].Scouting!.RiskRandom!.State;var commands=new Dictionary<string,int>(StringComparer.Ordinal);
                Apply(session,new AssignGovernorTemplateCommand(owner,0,session.View(owner).Colonies.Select(col=>col.Id).ToImmutableArray(),"industrial",true,true),commands);
                int? warning=null,departure=null;bool built=false,researched=false,cured=false;
                while(session.State.Turn<160&&!(warning.HasValue&&departure.HasValue&&built&&researched&&cured))
                {
                    var view=session.View(owner);var scout=view.Vessels.FirstOrDefault(v=>!c.Vessels[v.RoleId].CanFoundColony&&v.CarriedPopulation==0);Assert.NotNull(scout);
                    if(!view.Scouting!.Attention.IsEmpty)warning??=view.Turn;
                    if(warning is null&&scout.Transit is null&&scout.PendingMove is null)
                    {
                        var origin=view.Galaxy.Stars.Single(star=>star.Id==scout.StationaryStarId);var next=view.Stars.Where(star=>star.Planets is null&&star.Chart.Id!=origin.Id).OrderBy(star=>TravelCalculator.Duration(origin.X,origin.Y,star.Chart.X,star.Chart.Y,c.Vessels[scout.RoleId].Speed)).ThenBy(star=>star.Chart.Id).FirstOrDefault();Assert.NotNull(next);Apply(session,new SetMoveOrderCommand(owner,view.Turn,scout.Id,next.Chart.Id),commands);
                    }
                    OrdinaryPlanning(session,commands);
                    foreach(var token in session.View(owner).Scouting!.Attention.Where(t=>t.Action=="response_required"))
                    {
                        var row=session.View(owner).Scouting!.Vessels.Single(v=>v.Id==token.Vessel);var target=session.View(owner).Galaxy.Stars.First(star=>star.Id!=row.Station&&!session.View(owner).Scouting!.Habitats.Any(h=>h.Star==star.Id));Apply(session,new SetMoveOrderCommand(owner,session.State.Turn,row.Id,target.Id),commands);
                    }
                    Acknowledge(session,commands);int turn=session.State.Turn;var outcome=Apply(session,new SetReadyCommand(owner,turn,true),commands);Assert.Equal(turn+1,session.State.Turn);Assert.NotNull(outcome.Report);
                    built|=outcome.Report.Economy.Colonies.Any(col=>col.Empire==owner&&col.Production.CompletedId is not null);researched|=outcome.Report.Economy.Empire(owner).Research.CompletedId is not null;cured|=outcome.Report.CrisisEvents.Any(e=>e.Key.Owner==owner&&e.Kind=="cure_completed");
                    if(warning is not null&&session.View(owner).Scouting!.Attention.IsEmpty)departure??=session.State.Turn;
                }
                Assert.True(warning.HasValue&&departure.HasValue&&built&&researched&&cured,$"{species}/{size}/{rivals}: warning={warning}, departure={departure}, built={built}, researched={researched}, cured={cured}, turn={session.State.Turn}");Assert.Equal(0,session.State.Empires[0].Scouting!.RiskRollCount);Assert.Equal(risk,session.State.Empires[0].Scouting!.RiskRandom!.State);
                rows.Add(new{species,size,rivals,seed=42,rules=7,amebas=true,crises=true,source="Untouched simulation-generated complete core; shared commands only",initialHash,warningTurn=warning,departureTurn=departure,endTurn=session.State.Turn,built,researched,cured,scoutRiskRolls=0,liveOutcomeInjections=0,commands});
            }
            Assert.Equal(24,rows.Count);passed=true;
        }
        finally{Proof("generated",new{passed,expected=24,rows});}
    }
    [Fact] public void TwentyFourOffStartsAndSoloObserveFuturesPreserveRules6GalaxyAndOldStreams()
    {
        var c=Core();var frozen=ContentLoader.Load(new[]{new ContentSource("frozen-rules6",File.ReadAllText(CompatibilityFixtures.PathFor("rules6-content.json")))},requireCrises:true).Content!;int starts=0;var futures=new List<object>();bool passed=false;
        try
        {
            foreach(string species in new[]{"harl","peldra","sulan","wenth"})foreach(string size in new[]{"small","medium","large"})foreach(int rivals in new[]{0,1})
            {
                var old=GalaxyGenerator.CreateNewGame(frozen,new(size,42,6,species,rivals,rivals==1?"peldra":null));var current=GalaxyGenerator.CreateNewGame(c,new(size,42,7,species,rivals,rivals==1?"peldra":null,AmebasEnabled:false));Assert.True(old.Succeeded,old.Error);Assert.True(current.Succeeded,current.Error);Assert.Equal(Normalize(frozen,old.State!),Normalize(c,current.State!));Assert.False(current.State!.Scouting!.Enabled);Assert.Null(current.State.Scouting.PlacementRandom);Assert.All(current.State.Empires,e=>Assert.Null(e.Scouting!.RiskRandom));starts++;
            }
            foreach(string species in new[]{"harl","peldra","sulan","wenth"})foreach(string size in new[]{"small","medium","large"})foreach(bool plague in new[]{false,true})
            {
                var old=GalaxyGenerator.CreateNewGame(frozen,new(size,42,6,species,CrisesEnabled:plague));var current=GalaxyGenerator.CreateNewGame(c,new(size,42,7,species,CrisesEnabled:plague,AmebasEnabled:false));Assert.True(old.Succeeded,old.Error);Assert.True(current.Succeeded,current.Error);var a=new GameSession(frozen,old.State!);var b=new GameSession(c,current.State!);var owner=a.State.Empires[0].Id;
                foreach(var session in new[]{a,b})Apply(session,new AssignGovernorTemplateCommand(owner,0,session.View(owner).Colonies.Select(col=>col.Id).ToImmutableArray(),"industrial",true,true));
                for(int turn=0;turn<30;turn++){foreach(var session in new[]{a,b}){OrdinaryPlanning(session);Acknowledge(session);Apply(session,new SetReadyCommand(owner,session.State.Turn,true));}Assert.Equal(Normalize(a.Content,a.State),Normalize(b.Content,b.State));}
                futures.Add(new{species,size,plague,amebas=false,boundaries=30,canonicalEqual=true});
            }
            Assert.Equal(24,starts);Assert.Equal(24,futures.Count);passed=true;
        }
        finally{Proof("parity",new{passed,starts,expectedStarts=24,futures});}
    }
    private static void OrdinaryPlanning(GameSession session,Dictionary<string,int>? commands=null)
    {
        var owner=session.State.Empires[0].Id;var view=session.View(owner);int turn=view.Turn;
        if(view.Research.TargetId is null)
        {
            var next=session.Content.Technologies.Values.Where(t=>Rules.TechnologyAvailable(session.Content,session.State.RulesVersion,t.Id)&&!view.Research.KnownTechnologyIds.Contains(t.Id)&&t.PrerequisiteTechnologyIds.All(view.Research.KnownTechnologyIds.Contains)).OrderBy(t=>t.Cost).ThenBy(t=>t.Id,StringComparer.Ordinal).FirstOrDefault();if(next is not null)Apply(session,new SetResearchTargetCommand(owner,turn,next.Id),commands);
        }
        var crisis=session.View(owner).Crisis;
        if(crisis?.Episode is not null)foreach(var colony in session.View(owner).Colonies.Where(col=>col.Development?.Template?.Id!="research"))Apply(session,new AssignGovernorTemplateCommand(owner,turn,ImmutableArray.Create(colony.Id),"research",true,true),commands);
        if(crisis?.Episode is {} episode&&episode.ResearchShare!=100)Apply(session,new SetCureShareCommand(owner,turn,episode.Id,100),commands);
        var cases=crisis?.Cases.Where(c=>!c.Quarantined&&c.Key is not null).Select(c=>c.Location.Colony).ToImmutableArray()??ImmutableArray<ColonyId>.Empty;
        if(!cases.IsEmpty){var preview=CrisisQueries.PreviewQuarantine(session.Content,session.State,owner,cases,true,true);Assert.True(preview.Allowed);Apply(session,new SetQuarantineCommand(owner,turn,preview),commands);}
    }
    private static void Acknowledge(GameSession session,Dictionary<string,int>? commands=null)
    {
        var owner=session.State.Empires[0].Id;int turn=session.State.Turn;var crisis=CrisisQueries.PendingAttention(session.State,owner);if(!crisis.IsEmpty)Apply(session,new AcknowledgeCrisisCommand(owner,turn,crisis),commands);
        if(Rules.HasScouting(session.State.RulesVersion)){var scout=ScoutQueries.Project(session.Content,session.State,owner).Attention;if(!scout.IsEmpty)Apply(session,new AcknowledgeAmebaCommand(owner,turn,scout),commands);}
    }
    private static CommandOutcome Apply(GameSession session,Command command,Dictionary<string,int>? counts=null)
    {var result=session.Execute(command);Assert.True(result.Accepted,result.Rejection?.Message);if(counts is not null){string kind=command.GetType().Name;counts.TryGetValue(kind,out int before);counts[kind]=before+1;}return result;}
    private static string Normalize(ContentSet c,OpenAntares.Simulation.State.GameState s)
    {
        var root=JsonNode.Parse(SaveGame.Serialize(c,s))!;root["rules_version"]=6;Strip(root);return root.ToJsonString();
        static void Strip(JsonNode node){if(node is JsonObject o){foreach(string field in new[]{"space_ameba","scouting","scout","ameba_encounter"})o.Remove(field);foreach(var property in o)if(property.Value is {} child)Strip(child);}else if(node is JsonArray a)foreach(var child in a)if(child is not null)Strip(child);}
    }
    private static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    private static void Proof(string kind,object data)
    {
        string root=new DirectoryInfo(ContentLoaderTests.CoreContentDirectory()).Parent!.Parent!.FullName;string folder=Path.Combine(root,"reports","scout-execution");Directory.CreateDirectory(folder);string name="task14-"+kind+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+".json";File.WriteAllText(Path.Combine(folder,name),JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));File.WriteAllText(Path.Combine(folder,"task14-"+kind+"-latest.json"),JsonSerializer.Serialize(new{artifact=name,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(folder,name)))).ToLowerInvariant()}));
    }
}

