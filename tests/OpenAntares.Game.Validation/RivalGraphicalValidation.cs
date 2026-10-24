using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.GameValidation;

public partial class RivalGraphicalValidation : GraphicalValidation
{
    public override async void _Ready()
    {
        string? error=null;
        try
        {
            _output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output=",StringComparison.Ordinal))[20..];Directory.CreateDirectory(_output);
            Check(DisplayServer.GetName()!="headless","Rival validation requires a graphical display");
            await SetupChecks();
            await ObservationUiChecks();
        }
        catch(Exception ex){error=ex.ToString();GD.PrintErr(error);}
        finally
        {
            if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{runId=_runId,scenario="Rival",method="Actual control signals and commands; no physical OS input or human-comprehension claim.",passed=error is null,error,results=_results,retainedQaSavePaths=_savePaths},new JsonSerializerOptions{WriteIndented=true}));
            GetTree().Quit(error is null?0:1);
        }
    }
    private static OptionButton Picker(Node screen,string name)=>Descendants<OptionButton>(screen).Single(p=>p.Name==name);
    private async Task Drop(Node node){RemoveChild(node);node.QueueFree();await Frames();}
    private async Task SetupChecks()
    {
        var core=ContentFiles.LoadCore().Content!;
        var setup=new NewGameScreen(core){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(setup);await Frames();
        Check(Descendants<OptionButton>(setup).Any(p=>p.Name=="RivalCountPicker"),"Visible rival count selector exists");
        var count=Picker(setup,"RivalCountPicker");var human=Picker(setup,"SpeciesPicker");var rival=Picker(setup,"RivalSpeciesPicker");
        Check(count.GetSelectedMetadata().AsString()=="1" && human.GetSelectedMetadata().AsString()=="harl" && rival.GetSelectedMetadata().AsString()=="peldra","Visible defaults are One / Harl / Peldra");
        NewGameSettings? selected=null;setup.StartRequested+=s=>selected=s;setup.Configure("small",42);Press(Button(setup,"Start"));
        Check(selected is {RulesVersion:4,RivalCount:1,SpeciesId:"harl",RivalSpeciesId:"peldra"},"Visible defaults reach simulation unchanged");
        Select(human,"peldra");Check(rival.GetSelectedMetadata().AsString()=="peldra","Human selection does not swap rival; mirrors legal");Press(Button(setup,"Start"));Check(selected?.RivalSpeciesId=="peldra" && selected.SpeciesId=="peldra","Mirror settings reach simulation");
        Select(count,"0");Press(Button(setup,"Start"));Check(rival.Disabled && selected is {RivalCount:0,RivalSpeciesId:null},"Solo disables rival and passes null identity");
        Select(count,"1");Check(!rival.Disabled,"One enables the retained rival choice");
        foreach(var dimensions in new[]{new Vector2I(800,600),new(1152,648),new(1280,720),new(1600,900),new(1920,1080)})
        {
            GetTree().Root.Size=dimensions;await Frames();rival.GrabFocus();await Frames();
            Check(GetViewport().GuiGetFocusOwner()==rival && VisibleInWindow(rival) && VisibleInWindow(Button(setup,"Start")),"Setup controls/focus fit "+dimensions);
            await Capture("rival-setup-"+dimensions.X+"x"+dimensions.Y);
        }
        await Drop(setup);
        foreach(var ids in new[]{new[]{"peldra","sulan","wenth"},new[]{"sulan","wenth"},new[]{"wenth"},Array.Empty<string>()})
        {
            var alternative=core with{Species=core.Species.Where(p=>ids.Contains(p.Key)).ToImmutableSortedDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal)};
            var ui=new NewGameScreen(alternative){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(ui);await Frames();
            if(ids.Length==0){Check(Button(ui,"Start").Disabled,"No species disables Start");bool loaded=false;ui.LoadRequested+=()=>loaded=true;Press(Button(ui,"Load Game"));Check(loaded,"No species retains Load");}
            else{Check(Picker(ui,"SpeciesPicker").GetSelectedMetadata().AsString()==ids[0],"Fallback human is ordinal first");Check(Picker(ui,"RivalSpeciesPicker").GetSelectedMetadata().AsString()==(ids.Contains("peldra")?"peldra":ids.Length>1?ids[1]:ids[0]),"Fallback rival chooses Peldra or first other or single mirror");}
            await Drop(ui);
        }
        var invalid=new NewGameScreen(core with{RivalAi=null}){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(invalid);await Frames();
        Check(Button(invalid,"Start").Disabled,"Missing policy disables new rules-4 game");bool loadAvailable=false;invalid.LoadRequested+=()=>loadAvailable=true;Press(Button(invalid,"Load Game"));Check(loadAvailable,"Missing policy retains Load");await Drop(invalid);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();var current=One<NewGameScreen>(main);
        var cramped=core with{GalaxySizes=core.GalaxySizes.SetItem("small",core.GalaxySizes["small"] with{Width=1,Height=1})};
        typeof(Main).GetField("_content",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(main,cramped);
        current.Configure("small",42);Press(Button(current,"Start"));await Frames();
        Check(ReferenceEquals(current,One<NewGameScreen>(main)) && !Descendants<GameScreen>(main).Any(),"Failed generation retains setup without replacing a session");
        Check(Descendants<Label>(current).Any(l=>l.IsVisibleInTree() && l.ThemeTypeVariation=="WarningLabel" && l.Text.Length>10),"Failed generation shows an actionable error");await Drop(main);
    }
    private async Task ObservationUiChecks()
    {
        var core=ContentFiles.LoadCore().Content!;var state=GalaxyGenerator.CreateNewGame(core,new("small",42,4,"harl",1,"peldra")).State!;
        var before=state.Clone();var human=state.Empires[0];var ai=state.Empires[1];var home=state.FindPlanet(state.Colonies.Single(c=>c.EmpireId==ai.Id).PlanetId)!.StarId;
        state.Vessels.Single(v=>v.EmpireId==human.Id).StationaryStarId=home;ObservationUpdater.Refresh(state,0);ContactUpdater.Refresh(before,state);
        Check(GameStateValidator.Validate(core,state).Count==0,"Labelled pre-session observation fixture validates");
        string? text=null;
        foreach(bool changed in new[]{false,true})
        {
            var fixture=state.Clone();if(changed){fixture.FindEmpire(ai.Id)!.ResearchReserve=long.MaxValue;fixture.Colonies.Single(c=>c.EmpireId==ai.Id).ProductionReserve=long.MaxValue;fixture.Vessels.Single(v=>v.EmpireId==ai.Id).PendingMove=fixture.Stars.First(s=>s.Id!=home).Id;}
            var screen=new GameScreen(new GameSession(core,fixture),human.Id){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(screen);await Frames();One<GalaxyMap>(screen).Select(home);await Frames();
            string rendered=string.Join("\n",Descendants<Label>(screen).Select(l=>l.Text).Concat(Descendants<Control>(screen).Select(c=>c.TooltipText)));
            Check(Descendants<Label>(One<ContactsPanel>(screen)).Any(l=>l.Text.Contains(core.Species["peldra"].CultureName,StringComparison.Ordinal)),"Contact UI shows observed culture identity");
            Check(Descendants<Label>(One<StarPanel>(screen)).Any(l=>l.Text.Contains("(turn 0)",StringComparison.Ordinal)),"Ownership UI dates its observation");
            if(text is null){text=rendered;await Capture("rival-observed-contact");}else Check(text==rendered,"Rendered labels and tooltips ignore hidden rival reserves and orders");
            await Drop(screen);
        }
    }
    private bool VisibleInWindow(Control control)=>new Rect2(Vector2.Zero,GetViewport().GetVisibleRect().Size).Encloses(control.GetGlobalRect());
}
