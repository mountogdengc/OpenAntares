using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Knowledge;

string root=Directory.GetCurrentDirectory();
if(args.Length!=4||args[0]!="--sources"||args[2]!="--output")throw new ArgumentException("Use --sources RELATIVE --output RELATIVE");
string sources=Inside(args[1]),output=Inside(args[3]);
if(Directory.Exists(output))throw new InvalidOperationException("Never regenerate frozen expectations.");
if(Rules.CurrentVersion!=11||SaveGame.SchemaVersion!=12||Git("diff","5df255b","--","src","game","content").Length!=0||Git("ls-files","--others","--exclude-standard","src","game","content").Length!=0)throw new InvalidOperationException("Capture requires unchanged rules11/writer12 product.");
string stage=output+".attempt-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff",CultureInfo.InvariantCulture);Directory.CreateDirectory(stage);
var opts=new JsonSerializerOptions{WriteIndented=true};var cases=new List<object>();var origins=new List<object>();int generated=0,supplemental=0;
foreach(string folder in Directory.EnumerateDirectories(sources).OrderBy(x=>x,StringComparer.Ordinal))
foreach(string phase in new[]{"start","pending-offer","queued-answer","blocked-founder","trade-earmark","warmup","accord-notice","trade-notice"})
{Add(Path.Combine(folder,phase+".json"),Path.GetFileName(folder)+"-"+phase,"generated","Actual earned rules11 diplomacy campaign; original bytes from accepted main evidence.");generated++;}
if(generated!=32)throw new InvalidOperationException("Expected four generated campaigns with eight saved stages.");
var disk=ContentLoader.LoadDirectory(Path.Combine(root,"content/core"));if(!disk.Succeeded)throw new InvalidOperationException(string.Join("; ",disk.Errors));var content=disk.Content!;
var created=GalaxyGenerator.CreateNewGame(content,new("small",42,11,"harl",1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));
if(!created.Succeeded)throw new InvalidOperationException(created.Error);
var two=created.State!;two.Empires[1].Controller=ControllerKind.Human;two.Empires[1].AiPolicyId=null;
var session=new GameSession(content,two);var ready=session.Execute(new SetReadyCommand(two.Empires[0].Id,0,true));if(!ready.Accepted)throw new InvalidOperationException(ready.Rejection!.Message);
string primed=Path.Combine(stage,"primed","first-human-ready.json");Directory.CreateDirectory(Path.GetDirectoryName(primed)!);File.WriteAllText(primed,SaveGame.Serialize(content,session.State));
Add(primed,"first-human-ready","supplemental","Labelled pre-session two-human controller fixture; real first-human Ready; no turn or earned reward injected.");supplemental++;

var hazardSetup=GalaxyGenerator.CreateNewGame(content,new("small",42,11,"harl",CrisesEnabled:true,AmebasEnabled:true,SentriesEnabled:true));
if(!hazardSetup.Succeeded)throw new InvalidOperationException(hazardSetup.Error);
FreezeSupplement(new GameSession(content,hazardSetup.State!),"all-hazards-setup","Labelled ordinary rules11 setup with plague, ameba and sentries enabled; unchanged future streams; no earned-campaign claim.");
var refit=MakeMilitary("harl",100,false);var re=refit.State.Empires[0];
var savedDesign=refit.Execute(new ShipDesignCommand(re.Id,refit.State.Turn,re.ShipDesigns!.CatalogueRevision,new ShipDesignAction.Save("Supplement vector escort",content.ShipDesigns!.StartingRecipe with{EngineId="vector_drive"})));
if(!savedDesign.Accepted)throw new InvalidOperationException(savedDesign.Rejection!.Message);
var refitAction=new RefitAction.Start(refit.State.Vessels[0].Id,refit.State.Colonies[0].Id,new(2));var refitPreview=RefitQueries.Preview(content,refit.View(re.Id),refitAction);
if(!refitPreview.Allowed)throw new InvalidOperationException("Supplement refit preview: "+refitPreview.RejectionCode);
var refitStarted=refit.Execute(new RefitCommand(re.Id,refit.State.Turn,refitAction,refitPreview.Token));if(!refitStarted.Accepted)throw new InvalidOperationException(refitStarted.Rejection!.Message);
FreezeSupplement(refit,"refit-completing","Labelled pre-session armed hull/technology/bank fixture; real unpaid refit command and actual unchanged-engine completion accounting, not earned resources.");
foreach(var supplement in new[]{("harl",100,"sentry-pending"),("harl",25,"officer-loss-harl"),("wenth",25,"officer-loss-wenth")})
{
    var military=MakeMilitary(supplement.Item1,supplement.Item2,true);var ms=military.State;var me=ms.Empires[0];var v=ms.Vessels[0];var pocket=ms.Combat!.Pockets[0];
    v.StationaryStarId=pocket.Star;
    me.Combat!.Observations.Add(new(pocket.Id,pocket.Star,ms.Turn,false,pocket.Sentries.Select(a=>new ObservedSentry(a.Id,a.Profile,a.Hull,content.Combat!.SentryPursuitSpeed)).ToImmutableArray()));
    ObservationUpdater.Refresh(ms,ms.Turn);
    var act=new CombatAction.AttackPocket(me.Fleets!.Fleets[0].Id,pocket.Id,new(CombatDoctrine.Cautious,ms.Colonies[0].Id,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair),false);
    var preview=CombatQueries.Preview(content,military.View(me.Id),act);if(!preview.Allowed)throw new InvalidOperationException("Supplement attack: "+preview.RejectionCode);
    var attack=military.Execute(new CombatActionCommand(me.Id,ms.Turn,act,preview.Token));if(!attack.Accepted)throw new InvalidOperationException(attack.Rejection!.Message);
    FreezeSupplement(military,supplement.Item3,"Labelled pre-session positioned/conditioned armed hull with real fleet/officer/attack commands; tests unchanged sentry/officer mortality or Wenth link, not an earned campaign.");
}

GameSession MakeMilitary(string species,int hull,bool sentries)
{
    var newGame=GalaxyGenerator.CreateNewGame(content,new("small",42,11,species,CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:sentries));if(!newGame.Succeeded)throw new InvalidOperationException(newGame.Error);
    var s=newGame.State!;var e=s.Empires[0];var ship=s.Vessels[0];ship.RoleId=content.ShipDesigns!.WarshipRoleId;ship.Design=new(1);CombatInitializer.InitializeVessel(content,s,ship);ship.Combat!.Hull=hull;
    e.KnownTechnologyIds=e.KnownTechnologyIds.Concat(new[]{"industrial_coordination","vector_drives"}).Distinct().Order(StringComparer.Ordinal).ToList();
    s.Colonies[0].ProductionReserve=1000;s.Colonies[0].ProjectId=null;s.Colonies[0].ProjectKind=ProductionKinds.Building;
    var session=new GameSession(content,s);var create=new FleetAction.Create("Labelled supplement fleet",ImmutableArray.Create(ship.Id),ship.Id);var fp=FleetQueries.PreviewAction(content,session.View(e.Id),create);
    if(!fp.Allowed)throw new InvalidOperationException("Supplement fleet: "+fp.Rejection);var fc=session.Execute(new FleetCommand(e.Id,s.Turn,create){Fingerprint=fp.Fingerprint});if(!fc.Accepted)throw new InvalidOperationException(fc.Rejection!.Message);
    var officer=session.State.Empires[0].Fleets!.Officers[0];var assigned=session.Execute(new OfficerCommand(e.Id,s.Turn,new OfficerAction.AssignFlagship(officer.Id,session.State.Empires[0].Fleets!.Fleets[0].Id)));
    if(!assigned.Accepted)throw new InvalidOperationException("Supplement officer: "+assigned.Rejection!.Message);return session;
}
void FreezeSupplement(GameSession s,string name,string provenance)
{
    string path=Path.Combine(stage,"primed",name+".json");Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,SaveGame.Serialize(s.Content,s.State));Add(path,name,"supplemental",provenance);supplemental++;
}

var hashes=Directory.EnumerateFiles(stage,"*",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal).ToDictionary(p=>Path.GetRelativePath(stage,p).Replace('\\','/'),p=>Hash(File.ReadAllBytes(p)),StringComparer.Ordinal);
Write("manifest.json",JsonSerializer.Serialize(new{rulesVersion=11,writerVersion=12,generatedSources=generated,generatedCheckpoints=generated*2,supplementalSources=supplemental,sourceCommit=Git("rev-parse","HEAD"),approvedPlanCommit="5df255b",productTree=Git("rev-parse","5df255b:src/OpenAntares.Simulation"),compiler=Environment.Version.ToString(),engineSha256=Hash(File.ReadAllBytes(typeof(Rules).Assembly.Location)),programSha256=Hash(File.ReadAllBytes(Path.Combine(root,"tools/ConflictBaselineCapture/Program.cs"))),origins,files=hashes,cases},opts));
Directory.Move(stage,output);Console.WriteLine(JsonSerializer.Serialize(new{generatedSources=generated,generatedCheckpoints=generated*2,supplementalSources=supplemental,cases=cases.Count,output}));
void Add(string source,string name,string category,string provenance)
{
    byte[] bytes=File.ReadAllBytes(source);var load=SaveGame.Deserialize(System.Text.Encoding.UTF8.GetString(bytes));if(!load.Succeeded||load.Game!.State.RulesVersion!=11)throw new InvalidOperationException(name+": source must be valid original11: "+string.Join("; ",load.Errors));
    string save="sources/"+name+".json";WriteBytes(save,bytes);origins.Add(new{source=Path.GetFullPath(source),save,sha256=Hash(bytes),category,provenance});
    var session=new GameSession(load.Game.Content,load.Game.State);var commands=new List<List<object>>();var checkpoints=new SortedDictionary<string,string>(StringComparer.Ordinal);
    for(int n=0;n<10;n++){var steps=new List<object>();Advance(session,steps);commands.Add(steps);if(n is 2 or 9){var node=JsonNode.Parse(SaveGame.Serialize(session.Content,session.State))!.AsObject();node.Remove("schema_version");checkpoints[(n+1).ToString(CultureInfo.InvariantCulture)]=node.ToJsonString();}}
    string command="commands/"+name+".json",checkpoint="checkpoints/"+name+".json";Write(command,JsonSerializer.Serialize(commands,opts));Write(checkpoint,JsonSerializer.Serialize(checkpoints,opts));cases.Add(new{name,save,commands=command,checkpoints=checkpoint,category,provenance});Console.WriteLine("Captured original11 "+name);
}
void Advance(GameSession session,List<object> steps)
{
    int before=session.State.Turn;
    foreach(var owner in session.State.Empires.Where(e=>e.Controller==ControllerKind.Human&&!e.Ready).OrderBy(e=>e.Id).Select(e=>e.Id).ToArray())
    {
        foreach(var row in ScoutQueries.Project(session.Content,session.State,owner).Vessels.Where(v=>v.Stage is not null&&v.EffectiveDestination is null&&v.RiskAction is null).ToArray())
        {Command? safe=row.Actions.OfType<SetScoutRetreatOrderCommand>().FirstOrDefault();safe??=row.Actions.OfType<SetAmebaRiskOrderCommand>().FirstOrDefault(c=>c.Action==AmebaRiskAction.Remain);if(safe is not null)Execute(safe);}
        foreach(var row in ScoutQueries.Project(session.Content,session.State,owner).Vessels.Where(v=>v.Stage is null&&v.Blocker is not null&&v.Posture is not null).ToArray())Execute(new SetScoutPostureCommand(owner,session.State.Turn,row.Id,ScoutPosture.Observe));
        var health=CrisisQueries.PendingAttention(session.State,owner);if(!health.IsEmpty)Execute(new AcknowledgeCrisisCommand(owner,session.State.Turn,health));
        var attention=ScoutQueries.Project(session.Content,session.State,owner).Attention;if(!attention.IsEmpty)Execute(new AcknowledgeAmebaCommand(owner,session.State.Turn,attention));
        for(int responses=0;responses<8;responses++)
        {
            var view=session.View(owner);bool acted=false;
            foreach(var encounter in view.Combat!.Encounters)
            foreach(var response in new[]{CombatResponse.AcceptRevisedForce,CombatResponse.RemainWithBlockedReturn})
            {
                var action=new CombatAction.RespondCombat(encounter.Id,encounter.Revision,response);var p=CombatQueries.Preview(session.Content,view,action);if(!p.Allowed)continue;
                Execute(new CombatActionCommand(owner,view.Turn,action,p.Token));acted=true;break;
            }
            if(!acted)break;
        }
        Execute(new SetReadyCommand(owner,session.State.Turn,true));
    }
    if(session.State.Turn!=before+1)throw new InvalidOperationException("Old-engine replay did not advance exactly one boundary.");
    void Execute(Command command)
    {
        var r=session.Execute(command);if(!r.Accepted)throw new InvalidOperationException("Original11 turn"+before+" "+command.GetType().Name+": "+r.Rejection!.Code+" "+r.Rejection.Message);
        steps.Add(command is CombatActionCommand combat?(object)new{kind=command.GetType().Name,json=JsonSerializer.Serialize(command,command.GetType()),actionKind=combat.Action.GetType().Name,actionJson=JsonSerializer.Serialize(combat.Action,combat.Action.GetType())}:command is FleetCommand fleet?new{kind=command.GetType().Name,json=JsonSerializer.Serialize(command,command.GetType()),actionKind=fleet.Action.GetType().Name,actionJson=JsonSerializer.Serialize(fleet.Action,fleet.Action.GetType())}:new{kind=command.GetType().Name,json=JsonSerializer.Serialize(command,command.GetType())});
    }
}
void Write(string path,string text)=>WriteBytes(path,System.Text.Encoding.UTF8.GetBytes(text));
void WriteBytes(string path,byte[] bytes){string full=Path.Combine(stage,path);Directory.CreateDirectory(Path.GetDirectoryName(full)!);File.WriteAllBytes(full,bytes);}
string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
string Inside(string relative){string p=Path.GetFullPath(Path.Combine(root,relative));if(!p.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Path must remain inside checkout.");return p;}
string Git(params string[] args){var p=new ProcessStartInfo("git"){WorkingDirectory=root,RedirectStandardOutput=true,UseShellExecute=false,CreateNoWindow=true};foreach(string arg in args)p.ArgumentList.Add(arg);using var process=Process.Start(p)!;string text=process.StandardOutput.ReadToEnd();process.WaitForExit();if(process.ExitCode!=0)throw new InvalidOperationException("Provenance Git check failed.");return text.Trim();}
