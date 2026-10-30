using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;

internal static class ActionCapture
{
    internal static void Run(string root,string relative)
    {
        string output=Path.GetFullPath(Path.Combine(root,relative));
        if(!output.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Use a fresh in-checkout action archive.");
        if(Rules.CurrentVersion!=9||SaveGame.SchemaVersion!=10||Git("diff","4c58403","--","src","game","content").Length!=0)throw new InvalidOperationException("Action capture needs unchanged9 product.");
        string stage=output+".attempt-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff",CultureInfo.InvariantCulture);Directory.CreateDirectory(stage);
        var load=ContentLoader.LoadDirectory(Path.Combine(root,"content/core"));if(!load.Succeeded)throw new InvalidOperationException(string.Join("; ",load.Errors));
        var content=load.Content!;var made=GalaxyGenerator.CreateNewGame(content,new("small",42,9,"harl",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));if(!made.Succeeded)throw new InvalidOperationException(made.Error);
        var session=new GameSession(content,made.State!);var owner=session.State.Empires[0].Id;var options=new JsonSerializerOptions{WriteIndented=true};
        File.WriteAllText(Path.Combine(stage,"source.json"),SaveGame.Serialize(content,session.State));
        var transcript=new List<List<object>>();var checkpoints=new SortedDictionary<string,string>(StringComparer.Ordinal);
        for(int n=0;n<10;n++)
        {
            var steps=new List<object>();
            if(n==0)
            {
                var view=session.View(owner);var create=new FleetAction.Create("Frozen command expedition",ImmutableArray.Create(view.Vessels[0].Id),view.Vessels[0].Id);var p=FleetQueries.PreviewAction(content,view,create);if(!p.Allowed)throw new InvalidOperationException(p.Rejection);
                Execute(new FleetCommand(owner,view.Turn,create){Fingerprint=p.Fingerprint});
                view=session.View(owner);var fleet=view.Fleets!.Fleets.Single();var officer=view.Fleets.Officers.Single(o=>o.Status==OfficerStatus.Posted);
                Execute(new OfficerCommand(owner,view.Turn,new OfficerAction.AssignFlagship(officer.Id,fleet.Id)));
                view=session.View(owner);var home=view.Colonies.Single();var draft=new AssemblyDraft("Frozen scout request",home.StarId,null,ImmutableArray.Create("scout","scout"),ImmutableArray.Create(home.Id),RequestPriority.Normal,ImmutableArray<VesselId>.Empty,false,null);
                var accepted=AssemblyPlanner.Preview(FleetPlanningQueries.Create(content,view),draft);if(!accepted.Allowed)throw new InvalidOperationException(string.Join("; ",accepted.Rejections));
                Execute(new AssemblyCommand(owner,view.Turn,new AssemblyAction.Accept(draft,accepted.Fingerprint)));
                var request=session.View(owner).Fleets!.Requests.Single();Execute(new AssemblyCommand(owner,view.Turn,new AssemblyAction.Pause(request.Id,request.Revision)));
                view=session.View(owner);request=view.Fleets!.Requests.Single();var resumed=AssemblyPlanner.PreviewRetained(FleetPlanningQueries.Create(content,view),request.Id);if(!resumed.Allowed)throw new InvalidOperationException(string.Join("; ",resumed.Rejections));
                Execute(new AssemblyCommand(owner,view.Turn,new AssemblyAction.Resume(request.Id,request.Revision,resumed.Fingerprint)));
                view=session.View(owner);var hold=new FleetAction.SetOrder(ImmutableArray.Create(fleet.Id),new(FleetOrderKind.Hold));var hp=FleetQueries.PreviewAction(content,view,hold);if(!hp.Allowed)throw new InvalidOperationException(hp.Rejection);
                Execute(new FleetCommand(owner,view.Turn,hold){Fingerprint=hp.Fingerprint});
            }
            int turn=session.State.Turn;Execute(new SetReadyCommand(owner,turn,true));if(session.State.Turn!=turn+1)throw new InvalidOperationException("Expected one original boundary.");transcript.Add(steps);
            if(n is 2 or 9){var json=JsonNode.Parse(SaveGame.Serialize(content,session.State))!.AsObject();json.Remove("schema_version");checkpoints[(n+1).ToString(CultureInfo.InvariantCulture)]=json.ToJsonString();}
            void Execute(Command command)
            {
                var r=session.Execute(command);if(!r.Accepted)throw new InvalidOperationException(command.GetType().Name+": "+r.Rejection!.Message);
                object? action=command switch{FleetCommand f=>f.Action,OfficerCommand o=>o.Action,AssemblyCommand a=>a.Action,_=>null};
                steps.Add(action is null?(object)new{kind=command.GetType().Name,json=JsonSerializer.Serialize(command,command.GetType())}:new{kind=command.GetType().Name,json=JsonSerializer.Serialize(command,command.GetType()),actionKind=action.GetType().Name,actionJson=JsonSerializer.Serialize(action,action.GetType())});
            }
        }
        File.WriteAllText(Path.Combine(stage,"commands.json"),JsonSerializer.Serialize(transcript,options));File.WriteAllText(Path.Combine(stage,"checkpoints.json"),JsonSerializer.Serialize(checkpoints,options));
        var hashes=Directory.GetFiles(stage).OrderBy(p=>p,StringComparer.Ordinal).ToDictionary(p=>Path.GetFileName(p)!,p=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))).ToLowerInvariant());
        File.WriteAllText(Path.Combine(stage,"manifest.json"),JsonSerializer.Serialize(new{rulesVersion=9,writerVersion=10,sourceCommit=Git("rev-parse","HEAD"),productTree=Git("rev-parse","4c58403:src/OpenAntares.Simulation"),provenance="Normally generated old9 seed42 Solo/Off; real fleet creation, officer assignment and request accept/pause/resume; no source/resource/outcome edits",files=hashes},options));Directory.Move(stage,output);
        Console.WriteLine("Captured one additional original9 action source, all3 command families, two future checkpoints; original72-source archive unchanged.");
        string Git(params string[] args){var p=new ProcessStartInfo("git"){WorkingDirectory=root,RedirectStandardOutput=true,UseShellExecute=false,CreateNoWindow=true};foreach(string arg in args)p.ArgumentList.Add(arg);using var process=Process.Start(p)!;string text=process.StandardOutput.ReadToEnd();process.WaitForExit();if(process.ExitCode!=0)throw new InvalidOperationException("Git provenance failed.");return text.Trim();}
    }
}
