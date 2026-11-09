using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;

if(args.Length!=2)throw new ArgumentException("Expected source root and a new output directory.");
string root=Path.GetFullPath(args[0]),output=Path.GetFullPath(args[1]);
if(Directory.Exists(output))throw new IOException("Keep existing fixtures; use a new output directory.");
Directory.CreateDirectory(output);
var loaded=ContentLoader.LoadDirectory(Path.Combine(root,"content/core"));
if(!loaded.Succeeded)throw new InvalidOperationException(string.Join("; ",loaded.Errors));
var content=loaded.Content!;
var generated=GalaxyGenerator.CreateNewGame(content,new("small",42,Rules.CurrentVersion,"harl",0,null,false,false,false));
if(!generated.Succeeded)throw new InvalidOperationException(generated.Error);
var state=generated.State!;
var owner=state.Empires.Single(e=>e.Controller==ControllerKind.Human).Id;
var scout=state.Vessels.First(v=>v.EmpireId==owner&&v.RoleId=="scout");
// Explicitly staged player fixture, never represented as an earned campaign.
while(state.Vessels.Count(v=>v.EmpireId==owner&&v.RoleId=="scout")<10)
{var clone=scout.Clone();clone.Id=new(state.AllocateId());state.Vessels.Add(clone);}
var session=new GameSession(content,state);
var ships=state.Vessels.Where(v=>v.EmpireId==owner&&v.RoleId=="scout").OrderBy(v=>v.Id).Select(v=>v.Id).ToArray();
void Apply(GameSession game,FleetAction action)
{
 var view=game.View(owner);var preview=FleetQueries.PreviewAction(game.Content,view,action);
 if(!preview.Allowed)throw new InvalidOperationException(preview.Rejection);
 var outcome=game.Execute(new FleetCommand(owner,view.Turn,action){Fingerprint=preview.Fingerprint});
 if(!outcome.Accepted)throw new InvalidOperationException("Fixture command rejected.");
}
Apply(session,new FleetAction.Create("Alpha",ships.Take(4).ToImmutableArray()));
Apply(session,new FleetAction.Create("Beta",ships.Skip(4).Take(3).ToImmutableArray()));
Apply(session,new FleetAction.Create("Gamma",ships.Skip(7).Take(2).ToImmutableArray()));
string file=Path.Combine(output,"Fleet playground - Alpha Beta Gamma - 2026-11-09.json");
session.Save(file);
var reread=SaveGame.ReadFile(file);
if(!reread.Succeeded)throw new InvalidOperationException(string.Join("; ",reread.Errors));
var practice=new GameSession(reread.Game!.Content,reread.Game.State.Clone());
var view=practice.View(owner);var home=view.Colonies[0].StarId;
if(view.Fleets!.Fleets.Length!=3||view.Vessels.Any(v=>v.StationaryStarId!=home))throw new InvalidOperationException("Fixture roster is not co-located.");
if(SaveGame.Serialize(session.Content,session.State)!=SaveGame.Serialize(practice.Content,practice.State))throw new InvalidOperationException("Save round trip differs.");
var alpha=view.Fleets.Fleets.Single(f=>f.Name=="Alpha");var beta=view.Fleets.Fleets.Single(f=>f.Name=="Beta");
Apply(practice,new FleetAction.Split(alpha.Id,"Alpha split",ImmutableArray.Create(alpha.Members[0],alpha.Members[1])));
Apply(practice,new FleetAction.Merge(beta.Id,ImmutableArray.Create(alpha.Id)));
var result=new{fixture=true,provenance="New Solo Harl seed42 campaign with staged scout hulls; fleets created through ordinary commands. No existing campaign modified.",save=file,home=view.Stars.Single(s=>s.Chart.Id==home).Chart.Name,turn=view.Turn,fleets=view.Fleets.Fleets.Select(f=>new{f.Name,ships=f.Members.Length}),independentScouts=1,roundTrip=true,splitAndMergeAllowed=true};
File.WriteAllText(Path.Combine(output,"manifest.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine(JsonSerializer.Serialize(result));
