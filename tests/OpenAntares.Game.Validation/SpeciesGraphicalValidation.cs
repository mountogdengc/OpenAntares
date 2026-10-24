using System;
using System.IO;
using System.Collections.Immutable;
using OpenAntares.Game;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
namespace OpenAntares.GameValidation;

public partial class SpeciesGraphicalValidation : GraphicalValidation
{
    public override async void _Ready()
    {
        string? error=null;
        try
        {
            _output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output=",StringComparison.Ordinal))[20..];Directory.CreateDirectory(_output);
            Check(DisplayServer.GetName()!="headless","Species validation requires a graphical display");
            await DefaultSetup();
            await AlternativeContent();
        }
        catch(Exception ex){error=ex.ToString();GD.PrintErr(error);}
        finally
        {
            if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{runId=_runId,scenario="Species",method="Actual controls/signals/commands; no physical OS input or human-comprehension claim.",passed=error is null,error,results=_results,retainedQaSavePaths=_savePaths},new JsonSerializerOptions{WriteIndented=true}));
            GetTree().Quit(error is null?0:1);
        }
    }
    private async Task DefaultSetup()
    {
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();
        var setup=One<NewGameScreen>(main);var picker=Descendants<OptionButton>(setup).FirstOrDefault(p=>p.Name=="SpeciesPicker");
        Check(picker is not null && picker.ItemCount==4,"New Game exposes four species choices");
        Check(picker!.GetSelectedMetadata().AsString()=="harl","Harl is visibly selected by default");
        Check(Descendants<Label>(setup).Any(l=>l.Name=="SpeciesSummary" && l.Text.Length>60),"Starting tradeoffs and flavor appear before Start");
        Press(Button(setup,"Start"));await Frames();var screen=One<GameScreen>(main);var session=Session(screen);
        Check(session.State.RulesVersion==3 && session.State.Empires[0].SpeciesId=="harl","Actual Start launches a rules-3 Harl campaign");
        await Capture("default-harl-start");RemoveChild(main);main.QueueFree();await Frames();
    }
    private async Task AlternativeContent()
    {
        var core=ContentFiles.LoadCore().Content!;
        var alternative=core with {Species=core.Species.Remove("harl")};
        var setup=new NewGameScreen(alternative){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(setup);await Frames();
        var picker=Descendants<OptionButton>(setup).Single(p=>p.Name=="SpeciesPicker");
        Check(picker.ItemCount==3 && picker.GetSelectedMetadata().AsString()=="peldra","Valid content without Harl selects the ordinal first species");
        NewGameSettings? selected=null;setup.StartRequested+=settings=>selected=settings;setup.Configure("small",42);Press(Button(setup,"Start"));await Frames();
        Check(selected?.SpeciesId=="peldra" && selected.RulesVersion==3,"Alternative-content Start passes its actual species identity");
        RemoveChild(setup);setup.QueueFree();await Frames();
        var empty=new NewGameScreen(core with{Species=ImmutableSortedDictionary<string,SpeciesDefinition>.Empty}){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(empty);await Frames();
        Check(Button(empty,"Start").Disabled,"Missing species disables New Game");bool load=false;empty.LoadRequested+=()=>load=true;Press(Button(empty,"Load Game"));Check(load,"Missing species still permits Load Game");
        RemoveChild(empty);empty.QueueFree();await Frames();
        var edited=core with{Vessels=core.Vessels.SetItem("harbor_settler",core.Vessels["harbor_settler"] with{Name="Harbor Ark",EmbarkPopulation=3,MinimumSourcePopulation=5})};
        var state=GalaxyGenerator.CreateNewGame(edited,new("small",42,3,"sulan")).State!;
        var target=state.Planets.First(p=>edited.PlanetTypes[p.PlanetTypeId].Colonizable && state.Colonies.All(c=>c.PlanetId!=p.Id));
        var ship=new VesselState{Id=new(state.AllocateId()),EmpireId=state.Empires[0].Id,RoleId="harbor_settler",CarriedPopulation=3,StationaryStarId=target.StarId};state.Vessels.Add(ship);
        state.Colonies[0].ProjectKind="vessel";state.Colonies[0].ProjectId="harbor_settler";state.Colonies[0].ProductionReserve=3600;ObservationUpdater.Refresh(state,0);
        Check(GameStateValidator.Validate(edited,state).Count==0,"Labelled alternative-content UI fixture is structurally valid");
        var screen=new GameScreen(new GameSession(edited,state),state.Empires[0].Id){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(screen);await Frames();
        var projects=Descendants<OptionButton>(One<ColonyPanel>(screen)).Single();
        Check(Enumerable.Range(0,projects.ItemCount).Any(i=>projects.GetItemText(i).StartsWith("Harbor Ark",StringComparison.Ordinal) && projects.GetItemTooltip(i).Contains("population 5",StringComparison.Ordinal)),"Role picker uses edited names and requirements");
        Check(Descendants<Label>(One<ColonyPanel>(screen)).Any(l=>l.Text=="Support workers for cargo: 2 / 3."),"Blocked conditions use edited cargo and actual support workers");
        SelectNumber(Descendants<OptionButton>(One<VesselPanel>(screen)).Single(p=>p.Name=="VesselPicker"),ship.Id.Value);await Frames();One<GalaxyMap>(screen).Select(target.StarId);await Frames();Press(Button(One<StarPanel>(screen),$"Select planet {target.Orbit}"));await Frames();
        Check(Descendants<Label>(One<VesselPanel>(screen)).Any(l=>l.Text.Contains("3 population",StringComparison.Ordinal)),"Founding description uses edited carried population");
        Check(!Button(One<VesselPanel>(screen),"Found colony").Disabled,"Prepared expedition eligibility comes from its shared permitted preview");
        await Capture("alternative-content-harbor");RemoveChild(screen);screen.QueueFree();await Frames();
    }
    private static void SelectNumber(OptionButton picker,int id)
    {
        int index=Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsInt32()==id);picker.Select(index);picker.EmitSignal(OptionButton.SignalName.ItemSelected,index);
    }

}
