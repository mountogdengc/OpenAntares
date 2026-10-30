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

if(args.Length==3&&args[0]=="--actions"&&args[1]=="--output"){ActionCapture.Run(Directory.GetCurrentDirectory(),args[2]);return;}
string root=Directory.GetCurrentDirectory();
if(args.Length!=4||args[0]!="--sources"||args[2]!="--output")throw new ArgumentException("Use --sources RELATIVE --output RELATIVE; archive is create-only.");
string sources=Inside(args[1]),output=Inside(args[3]);
if(Directory.Exists(output))throw new InvalidOperationException("Never regenerate frozen expectations.");
if(Rules.CurrentVersion!=9||SaveGame.SchemaVersion!=10||Git("diff","4c58403","--","src","game","content").Length!=0||Git("ls-files","--others","--exclude-standard","src","game","content").Length!=0)throw new InvalidOperationException("Capture requires unchanged rules9/writer10 product.");
string stage=output+".attempt-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff",CultureInfo.InvariantCulture);Directory.CreateDirectory(stage);
var opts=new JsonSerializerOptions{WriteIndented=true};var cases=new List<object>();var origins=new List<object>();var coverage=new SortedDictionary<string,bool>(StringComparer.Ordinal);int generated=0,supplemental=0;
foreach(string folder in Directory.EnumerateDirectories(sources).OrderBy(x=>x,StringComparer.Ordinal))
foreach(string phase in new[]{"start","pending","active","return","repair","cleared"})
{
    Add(Path.Combine(folder,phase+".json"),Path.GetFileName(folder)+"-"+phase,"generated","Actual previously earned paid rules9 Combat campaign; original bytes from accepted main native evidence.");generated++;
}
if(generated!=72)throw new InvalidOperationException("Expected twelve genuine generated campaigns and six saved phases each.");
var disk=ContentLoader.LoadDirectory(Path.Combine(root,"content/core"));if(!disk.Succeeded)throw new InvalidOperationException(string.Join("; ",disk.Errors));var content=disk.Content!;
// Extra source creation stays on the old engine and is labelled independently of the72 generated earned saves.
var off=Start(false,false,0);Freeze("off-start",off,"Normally generated unchanged explicit9 seed42 Solo, hazards Off.");
var queued=Start(false,false,0);var own=queued.State.Empires[0].Id;var home=queued.View(own).Colonies.Single();
var queueEntries=ImmutableArray.Create(new QueueEntryDraft(null,new(ProductionKinds.Vessel,"escort")),new QueueEntryDraft(null,new(ProductionKinds.Vessel,"scout")));
ApplyPrime(queued,new SetColonyQueueCommand(own,0,home.Id,queueEntries));
ApplyPrime(queued,new SetProductionTargetCommand(own,0,home.Id,new(ProductionKinds.Vessel,"escort")));
Freeze("manual-project-queue",queued,"Normally generated9 plus real finite queue/manual project commands, no production injection.");
var requested=Start(false,false,0);own=requested.State.Empires[0].Id;home=requested.View(own).Colonies.Single();
var draft=new AssemblyDraft("Frozen finite request",home.StarId,null,ImmutableArray.Create("escort","scout"),ImmutableArray.Create(home.Id),RequestPriority.Normal,ImmutableArray<VesselId>.Empty,false,null);
var preview=AssemblyPlanner.Preview(FleetPlanningQueries.Create(content,requested.View(own)),draft);if(!preview.Allowed)throw new InvalidOperationException(string.Join("; ",preview.Rejections));
ApplyPrime(requested,new AssemblyCommand(own,0,new AssemblyAction.Accept(draft,preview.Fingerprint)));Freeze("active-request",requested,"Normally generated9 plus an accepted finite paid construction request, no completed hull injected.");
var request=requested.State.FindEmpire(own)!.Fleets!.Requests.Single();ApplyPrime(requested,new AssemblyCommand(own,0,new AssemblyAction.Pause(request.Id,request.Revision)));Freeze("paused-request",requested,"Same public request explicitly paused; reservations and unspent queues retained.");
// Actual previously retained Main-load/save supplements remain labelled as such.
foreach(string report in new[]{"reports/combat-integration-20261029/combat-01/results.json","reports/combat-execution/final-review-native-green-01/results.json"})
{
    var doc=JsonNode.Parse(File.ReadAllText(Path.Combine(root,report)))!;
    foreach(var provenance in doc["provenance"]!.AsArray())
    {
        string? file=provenance?["file"]?.GetValue<string>();if(file is null||!File.Exists(file))continue;
        string full=Path.GetFullPath(file);string qa=Path.GetFullPath("C:/Users/david/AppData/Roaming/Godot/app_userdata/OpenAntares/saves")+Path.DirectorySeparatorChar;
        if(!full.StartsWith(qa,StringComparison.OrdinalIgnoreCase)&&!full.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Out-of-scope retained source.");
        var saved=SaveGame.ReadFile(full);if(!saved.Succeeded||saved.Game!.State.RulesVersion!=9)continue;
        if(Facts(saved.Game.Content,saved.Game.State).Any(p=>p.Value&&!coverage.GetValueOrDefault(p.Key)))Add(full,"retained-"+supplemental++.ToString("D3",CultureInfo.InvariantCulture),"supplemental","Actual retained Main fixture/save; original scenario provenance: "+provenance!["source"]);
    }
}
if(!coverage.GetValueOrDefault("plague"))
{
    var s=Start(true,false,0);for(int n=0;n<200&&!s.State.Colonies.Any(c=>c.Crisis?.Case?.Stage==CrisisStage.Active);n++)Advance(s,new());
    if(!s.State.Colonies.Any(c=>c.Crisis?.Case?.Stage==CrisisStage.Active))throw new InvalidOperationException("No normally earned old-engine Active plague within200 turns.");
    Freeze("ordinary-plague",s,"Normally generated9, ordinary acknowledged Ready progression to Active plague; no state/resource edits.");
}
if(!coverage.GetValueOrDefault("ameba"))
{
    var s=Start(false,true,0);own=s.State.Empires[0].Id;
    for(int n=0;n<150&&!s.State.Vessels.Any(v=>v.AmebaEncounter is not null);n++)
    {
        var v=s.View(own);var ship=v.Vessels.Single();
        if(ship.Transit is null&&ship.PendingMove is null)
        {
            var target=v.Stars.Where(t=>t.LastObservedTurn is null&&t.Chart.Id!=ship.StationaryStarId).OrderBy(t=>t.Chart.Id).FirstOrDefault()??throw new InvalidOperationException("No unseen chart target before ameba warning.");
            var action=new FleetAction.MoveSelection(ImmutableArray.Create(ship.Id),ImmutableArray<FleetId>.Empty,target.Chart.Id);var p=FleetQueries.PreviewAction(content,v,action);if(!p.Allowed)throw new InvalidOperationException(p.Rejection);
            ApplyPrime(s,new FleetCommand(own,v.Turn,action){Fingerprint=p.Fingerprint});
        }
        Advance(s,new());
    }
    if(!s.State.Vessels.Any(v=>v.AmebaEncounter is not null))throw new InvalidOperationException("No ordinary old-engine ameba warning.");
    Freeze("ordinary-ameba",s,"Normally generated9 and real public-chart exploration reaches ameba; no teleport/outcome injection.");
}
var two=Start(false,false,1);var twoState=two.State.Clone();twoState.Empires[1].Controller=ControllerKind.Human;twoState.Empires[1].AiPolicyId=null;
var twoErrors=GameStateValidator.Validate(content,twoState);if(twoErrors.Count>0)throw new InvalidOperationException(string.Join("; ",twoErrors));
var twoSession=new GameSession(content,twoState);
ApplyPrime(twoSession,new SetReadyCommand(twoState.Empires[0].Id,0,true));Freeze("first-human-ready",twoSession,"Labelled pre-session two-human controller fixture; first human used actual Ready, no boundary occurred.");
var alternate=content with{Vessels=content.Vessels.Remove("escort").Add("custom_patrol",content.Vessels["escort"] with{Id="custom_patrol",Name="Custom patrol"}),Combat=content.Combat! with{RoleProfiles=content.Combat!.RoleProfiles.Remove("escort").Add("custom_patrol",content.Combat.RoleProfiles["escort"])}};
var alt=GalaxyGenerator.CreateNewGame(alternate,new("small",42,9,"harl",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));if(!alt.Succeeded)throw new InvalidOperationException(alt.Error);
var patrol=alt.State!.Vessels[0];patrol.RoleId="custom_patrol";CombatInitializer.InitializeVessel(alternate,9,patrol);Freeze("alternative-armed-role",new(alternate,alt.State),"Labelled pre-session alternate-content armed-role fixture; not earned production.");
foreach(string key in new[]{"off","officer","project","queue","activeRequest","pausedRequest","cargo","plague","ameba","firstHumanReady","alternativeRole","pendingCombat","activeCombat","withdrawal","return","repair","damage"})if(!coverage.GetValueOrDefault(key))throw new InvalidOperationException("Missing original9 coverage: "+key);
var hashes=Directory.EnumerateFiles(stage,"*",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal).ToDictionary(p=>Path.GetRelativePath(stage,p).Replace('\\','/'),p=>Hash(File.ReadAllBytes(p)),StringComparer.Ordinal);
Write("manifest.json",JsonSerializer.Serialize(new{rulesVersion=9,writerVersion=10,generatedSources=generated,generatedCheckpoints=generated*2,supplementalSources=supplemental,sourceCommit=Git("rev-parse","HEAD"),approvedPlanCommit="4c58403b107063a616645905b6f6f86f86676ca6",productTree=Git("rev-parse","4c58403:src/OpenAntares.Simulation"),compiler=Environment.Version.ToString(),engineSha256=Hash(File.ReadAllBytes(typeof(Rules).Assembly.Location)),programSha256=Hash(File.ReadAllBytes(Path.Combine(root,"tools/ShipDesignBaselineCapture/Program.cs"))),coverage,origins,files=hashes,cases},opts));
Directory.Move(stage,output);Console.WriteLine(JsonSerializer.Serialize(new{generatedSources=generated,generatedCheckpoints=generated*2,supplementalSources=supplemental,cases=cases.Count,coverage,output}));

GameSession Start(bool plague,bool ameba,int rivals)
{var result=GalaxyGenerator.CreateNewGame(content,new("small",42,9,"harl",rivals,rivals>0?"peldra":null,CrisesEnabled:plague,AmebasEnabled:ameba,SentriesEnabled:false));if(!result.Succeeded)throw new InvalidOperationException(result.Error);return new(content,result.State!);}
void ApplyPrime(GameSession s,Command c){var r=s.Execute(c);if(!r.Accepted)throw new InvalidOperationException("Old-engine source priming "+c.GetType().Name+": "+r.Rejection!.Message);}
void Freeze(string name,GameSession session,string provenance)
{string path=Path.Combine(stage,"primed",name+".json");Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,SaveGame.Serialize(session.Content,session.State));Add(path,name,"supplemental",provenance);supplemental++;}
void Add(string source,string name,string category,string provenance)
{
    byte[] bytes=File.ReadAllBytes(source);var load=SaveGame.Deserialize(System.Text.Encoding.UTF8.GetString(bytes));if(!load.Succeeded||load.Game!.State.RulesVersion!=9)throw new InvalidOperationException(name+": source must be valid original9: "+string.Join("; ",load.Errors));
    string save="sources/"+name+".json";WriteBytes(save,bytes);origins.Add(new{source=Path.GetFullPath(source),save,sha256=Hash(bytes),category,provenance});
    var session=new GameSession(load.Game.Content,load.Game.State);Observe(session);var commands=new List<List<object>>();var checkpoints=new SortedDictionary<string,string>(StringComparer.Ordinal);
    for(int n=0;n<10;n++){var steps=new List<object>();Advance(session,steps);commands.Add(steps);Observe(session);if(n is 2 or 9){var node=JsonNode.Parse(SaveGame.Serialize(session.Content,session.State))!.AsObject();node.Remove("schema_version");checkpoints[(n+1).ToString(CultureInfo.InvariantCulture)]=node.ToJsonString();}}
    string command="commands/"+name+".json",checkpoint="checkpoints/"+name+".json";Write(command,JsonSerializer.Serialize(commands,opts));Write(checkpoint,JsonSerializer.Serialize(checkpoints,opts));cases.Add(new{name,save,commands=command,checkpoints=checkpoint,category,provenance});Console.WriteLine("Captured original9 "+name);
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
        var r=session.Execute(command);if(!r.Accepted)throw new InvalidOperationException("Original9 turn"+before+" "+command.GetType().Name+": "+r.Rejection!.Code+" "+r.Rejection.Message);
        steps.Add(command is CombatActionCommand combat?(object)new{kind=command.GetType().Name,json=JsonSerializer.Serialize(command,command.GetType()),actionKind=combat.Action.GetType().Name,actionJson=JsonSerializer.Serialize(combat.Action,combat.Action.GetType())}:command is FleetCommand fleet?new{kind=command.GetType().Name,json=JsonSerializer.Serialize(command,command.GetType()),actionKind=fleet.Action.GetType().Name,actionJson=JsonSerializer.Serialize(fleet.Action,fleet.Action.GetType())}:new{kind=command.GetType().Name,json=JsonSerializer.Serialize(command,command.GetType())});
    }
}
void Observe(GameSession s){foreach(var p in Facts(s.Content,s.State))coverage[p.Key]=p.Value||coverage.GetValueOrDefault(p.Key);}
Dictionary<string,bool> Facts(ContentSet c,GameState s)=>new(StringComparer.Ordinal){["off"]=!s.Combat!.SentriesEnabled,["officer"]=s.Empires.Any(e=>e.Fleets!.Officers.Count>0),["project"]=s.Colonies.Any(p=>p.ProjectId is not null),["queue"]=s.Colonies.Any(p=>p.Development!.Queue.Count>0),["activeRequest"]=s.Empires.Any(e=>e.Fleets!.Requests.Any(r=>r.Status==RequestStatus.Active)),["pausedRequest"]=s.Empires.Any(e=>e.Fleets!.Requests.Any(r=>r.Status==RequestStatus.Paused)),["cargo"]=s.Vessels.Any(v=>v.CarriedPopulation>0),["plague"]=s.Colonies.Any(p=>p.Crisis?.Case is not null),["ameba"]=s.Vessels.Any(v=>v.AmebaEncounter is not null),["firstHumanReady"]=s.Empires.Any(e=>e.Controller==ControllerKind.Human&&e.Ready),["alternativeRole"]=c.Vessels.ContainsKey("custom_patrol"),["pendingCombat"]=s.Combat.Encounters.Any(e=>e.Phase==CombatPhase.Pending),["activeCombat"]=s.Combat.Encounters.Any(e=>e.Phase==CombatPhase.Active),["withdrawal"]=s.Combat.Encounters.Any(e=>e.Phase==CombatPhase.WithdrawalQueued),["return"]=s.Vessels.Any(v=>v.Combat?.ReturnColony is not null),["repair"]=s.Vessels.Any(v=>v.Combat?.Repairing==true),["damage"]=s.Vessels.Any(v=>v.Combat is {} b&&b.Hull<c.Combat!.RoleProfiles[v.RoleId].MaxHull)};
void Write(string path,string text)=>WriteBytes(path,System.Text.Encoding.UTF8.GetBytes(text));
void WriteBytes(string path,byte[] bytes){string full=Path.Combine(stage,path);Directory.CreateDirectory(Path.GetDirectoryName(full)!);File.WriteAllBytes(full,bytes);}
string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
string Inside(string relative){string p=Path.GetFullPath(Path.Combine(root,relative));if(!p.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Path must remain inside checkout.");return p;}
string Git(params string[] args){var p=new ProcessStartInfo("git"){WorkingDirectory=root,RedirectStandardOutput=true,UseShellExecute=false,CreateNoWindow=true};foreach(string arg in args)p.ArgumentList.Add(arg);using var process=Process.Start(p)!;string text=process.StandardOutput.ReadToEnd();process.WaitForExit();if(process.ExitCode!=0)throw new InvalidOperationException("Provenance Git check failed.");return text.Trim();}
