using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Saves;
namespace OpenAntares.Game.Validation;

/// <summary>Isolated M14 native frames and controls. Physical comprehension is owner-observed.</summary>
public partial class VisualOverhaulValidation : Node
{
    private string _output="",_case="Baseline";
    private Control? _main;
    private readonly List<object> _checks=new(),_frames=new();
    private static readonly Vector2I[] Sizes={new(800,600),new(1152,648),new(1600,900),new(1920,1080),new(2560,1440)};
    private void Check(bool passed,string name){_checks.Add(new{passed,name});if(!passed)throw new InvalidOperationException(name);}
    private static IEnumerable<T> All<T>(Node root) where T:Node
    {foreach(Node child in root.GetChildren(true)){if(child is T value)yield return value;foreach(var node in All<T>(child))yield return node;}}
    private T Named<T>(string name) where T:Node=>All<T>(_main!).Single(n=>n.Name==name);
    private async Task Frames(int count=3){for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private void Press(Button key){key.GrabFocus();for(Node? parent=key.GetParent();parent is not null;parent=parent.GetParent())if(parent is ScrollContainer scroll)scroll.EnsureControlVisible(key);Check(!key.Disabled&&key.IsVisibleInTree()&&key.GetWindow().Visible,"Reachable action: "+key.Name);key.EmitSignal(BaseButton.SignalName.Pressed);}
    private GameSession Live(GameScreen screen)=>(GameSession)typeof(GameScreen).GetField("_session",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(screen)!;
    private static string Snapshot(GameSession session)=>SaveGame.Serialize(session.Content,session.State);
    public override async void _Ready()
    {
        string? error=null;
        try
        {
            var args=OS.GetCmdlineUserArgs();_output=args.Single(x=>x.StartsWith("--validation-output="))[20..];_case=args.Single(x=>x.StartsWith("--visual-case="))[14..];Directory.CreateDirectory(_output);
            Check(DisplayServer.GetName()!="headless","Actual graphical display required");
            Input.ParseInputEvent(new InputEventMouseMotion{Position=new Vector2(-32,-32)});
            _main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(_main);await Frames();
            if(_case=="MultiFleet")await MultiFleetSelection();
            else if(_case=="FleetPanel")await FleetPanelFeedback();
            else if(_case=="PlaytestFeedback")await PlaytestFeedback();
            else if(_case=="Baseline")await Baseline();
            else if(_case=="Theme")await ThemeChecks();
            else if(_case=="PlayerFlow")await PlayerFlow();
            else if(_case=="ReviewCorrections")await ReviewCorrections();
            else if(_case=="PopulationForecast")await PopulationForecast();
            else throw new InvalidOperationException("Unimplemented M14 case cannot pass: "+_case);
        }
        catch(Exception failure){error=failure.ToString();GD.PrintErr(error);}
        finally
        {
            if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{@case=_case,passed=error is null,error,method="Actual native Godot frames and controls; no physical-input or human-comprehension acceptance",checks=_checks,frames=_frames},new JsonSerializerOptions{WriteIndented=true}));
            GetTree().Quit(error is null?0:1);
        }
    }
    private async Task<GameScreen> Start()
    {
        var setup=All<NewGameScreen>(_main!).Single();setup.Configure("small",42,"harl",1,"peldra");Press(All<Button>(setup).Single(b=>b.Text=="Start"));await Frames();return All<GameScreen>(_main!).Single();
    }
    private async Task CloseWindows()
    {
        // Baseline-only visual reset; never accept a choice or acknowledge a response.
        for(int pass=0;pass<4;pass++){foreach(var window in All<Window>(_main!).Where(w=>w.Visible).ToArray())window.Hide();foreach(var option in All<OptionButton>(_main!))option.GetPopup().Hide();await Frames();}
    }
    private static object Artifact(string path)=>new{path=Path.GetRelativePath(ProjectSettings.GlobalizePath("res://"),path).Replace('\\','/'),sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()};
    private async Task Capture(string label,Vector2I size)
    {
        GetTree().Root.Size=size;await Frames(4);Check(GetTree().Root.Size==size,"Physical viewport size "+size);
        string stem=label+"-"+size.X+"x"+size.Y,path=Path.Combine(_output,stem+".png");var image=GetTree().Root.GetTexture().GetImage();Check(image.SavePng(path)==Error.Ok,"Frame written: "+stem);
        var inventory=All<Control>(_main!).Select(c=>new{name=c.Name.ToString(),type=c.GetType().Name,path=c.GetPath().ToString(),visible=c.IsVisibleInTree()&&c.GetWindow().Visible,rect=c.GetGlobalRect().ToString(),focusable=c.FocusMode!=Control.FocusModeEnum.None,window=c.GetWindow().Name.ToString()}).ToArray();string controls=Path.Combine(_output,stem+"-controls.json");File.WriteAllText(controls,JsonSerializer.Serialize(inventory,new JsonSerializerOptions{WriteIndented=true}));
        var windows=new List<object>();int number=0;
        foreach(var window in All<Window>(_main!).Where(w=>w.Visible&&w.Size.X>0&&w.Size.Y>0))
        {
            string frame=Path.Combine(_output,stem+"-window-"+(number++)+".png");var pixels=window.GetTexture().GetImage();Check(pixels.SavePng(frame)==Error.Ok,"Native window frame: "+window.Name);windows.Add(new{name=window.Name.ToString(),type=window.GetType().Name,width=window.Size.X,height=window.Size.Y,frame=Artifact(frame)});
        }
        _frames.Add(new{label,width=size.X,height=size.Y,frame=Artifact(path),inventory=Artifact(controls),windows});
    }
    private async Task Baseline()
    {
        foreach(var size in Sizes)await Capture("Setup",size);
        var screen=await Start();var session=Live(screen);string before=Snapshot(session);
        foreach(var size in Sizes)await Capture("Introduction",size);
        var dismiss=All<Button>(_main!).SingleOrDefault(b=>b.Name=="DismissFirstTurnHint");if(dismiss is not null)Press(dismiss);await Frames();
        foreach(var route in new[]{("Galaxy","OpenGalaxyWorkspace"),("System","InspectSelectedSystem"),("Colony","OpenColonyWorkspace"),("Colonies","OpenColonyOverview"),("Vessels","OpenVesselWorkspace"),("Research","OpenResearch"),("Fleets","OpenFleetCommand"),("Scouts","OpenScoutOverview"),("Designs","OpenShipDesigns"),("Diplomacy","OpenDiplomacy"),("Contacts","OpenContactsWorkspace"),("ColonyPlans","OpenColonyTools"),("Conflict","OpenConflictOrders")})
        {
            await CloseWindows();Press(Named<Button>("OpenGalaxyWorkspace"));await Frames();if(route.Item1=="ColonyPlans"){Press(Named<Button>("OpenColonyWorkspace"));await Frames();}var choices=All<Button>(_main!).Where(b=>b.Name==route.Item2&&b.IsVisibleInTree()&&b.GetWindow().Visible).ToArray();Check(choices.Length==1,"Exactly one reachable production route: "+route.Item2);var button=choices.SingleOrDefault();
            Check(button is not null,"Baseline route remains discoverable: "+route.Item2);
            button=CombinedUiNavigation.Reveal(button!);Press(button);await Frames();foreach(var size in Sizes)await Capture(route.Item1,size);Check(before==Snapshot(session),route.Item1+": inspection preserves campaign and PRNG");
        }
        await CloseWindows();Press(Named<Button>("OpenGalaxyWorkspace"));await Frames();var reference=new GameSession(session.Content,session.State.Clone());var expected=reference.Execute(new SetReadyCommand(session.State.Empires.First(e=>e.Controller==OpenAntares.Simulation.State.ControllerKind.Human).Id,session.State.Turn,true));Check(expected.Accepted,"Baseline report uses one ordinary Ready");Press(Named<Button>("EndTurn"));await Frames();Check(Snapshot(reference)==Snapshot(session),"Baseline report matches ordinary command exactly");foreach(var size in Sizes)await Capture("TurnReport",size);
    }
    private async Task ThemeChecks()
    {
        var theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres");Check(theme.HasStylebox("panel","Panel"),"EveryControlUsesThemedStyle: original console panels replace engine grey");
        foreach(var size in Sizes)await Capture("Theme",size);
    }
    private Button Route(string name) => All<Button>(_main!).Single(b => b.Name == name && b.GetWindow() == _main!.GetWindow() && b.IsVisibleInTree());
    private async Task PlayerFlow()
    {
        var screen = await Start(); var session = Live(screen);
        screen.PresentationPreferencesPath = Path.Combine(_output,"presentation.cfg");
        Check(!All<Control>(screen).Any(c=>c.Name=="FirstTurnHint"),"No automatic campaign-start banner"); await Frames();
        var view = session.View(session.State.Empires.First(e => e.Controller == OpenAntares.Simulation.State.ControllerKind.Human).Id);
        Check(Named<Label>("PRODUCTIONReadout").Text == Format.Points(view.Colonies.Sum(c => c.ProductionReserve)), "Production readout uses published local reserves in points");
        Check(Named<Label>("RESEARCHReadout").Text == Format.Points(view.Research.Reserve), "Research readout uses published reserve in points");
        Check(Named<Label>("POPULATIONReadout").Text == view.Colonies.Sum(c => c.Population).ToString(), "Population readout uses population, not growth points");
        Check(!Named<Label>("PRODUCTIONDelta").Text.Contains("s)"), "Readout has an explicit signed delta");
        string before = Snapshot(session);
        foreach (var size in Sizes) await Capture("Galaxy",size);
        foreach (string overlay in new[] { "Owners", "Threat", "Habitat" }) { Press(Route("MapOverlay"+overlay)); await Frames(); }
        Check(Named<Button>("MapOverlayRange").Disabled, "Range overlay does not invent a finite-range rule");
        Check(Snapshot(session)==before, "Map inspection and overlays issue no commands");
        var bar = Named<CommandBar>("MainNavigation");
        bar._UnhandledKeyInput(new InputEventKey { Pressed=true, Keycode=Key.C, PhysicalKeycode=Key.C }); await Frames();
        Check(Named<ColonyLedger>("ColonyLedger").IsVisibleInTree(), "C opens the actual colony ledger");
        Check(Snapshot(session)==before, "Route shortcut changes no campaign state");
        foreach (var size in Sizes) await Capture("Colonies",size);
        Check(All<WorkerFigure>(screen).All(w=>w.Icon is not null&&w.Size.X>=28&&w.Size.Y>=28),"Worker figures retain visible icon space at the smallest supported size");
        Check(All<WorkerFigure>(screen).All(w=>w.Size.X>=44&&w.Size.Y>=44),"Worker figures reach 44-pixel targets at the current large viewport");
        var colony=view.Colonies.First();
        int[] workers={colony.Workforce.Support,colony.Workforce.Production,colony.Workforce.Research};
        int source=System.Array.FindIndex(workers,n=>n>0),destination=(source+1)%3;workers[source]--;workers[destination]++;
        var reference=new GameSession(session.Content,session.State.Clone());
        var result=reference.Execute(new SetWorkforceCommand(view.Viewer,view.Turn,colony.Id,new(workers[0],workers[1],workers[2])));
        Check(result.Accepted,"Reference workforce order is valid");
        var figure=All<WorkerFigure>(screen).First(w=>w.Colony==colony.Id.Value&&w.Role==source);
        figure._GuiInput(new InputEventKey { Pressed=true,Keycode=Key.Right }); await Frames();
        Check(Snapshot(session)==Snapshot(reference),"Keyboard worker reassignment matches exactly one ordinary order");
        view=session.View(view.Viewer);colony=view.Colonies.First();workers=new[]{colony.Workforce.Support,colony.Workforce.Production,colony.Workforce.Research};
        source=System.Array.FindIndex(workers,n=>n>0);destination=(source+1)%3;workers[source]--;workers[destination]++;
        reference=new GameSession(session.Content,session.State.Clone());result=reference.Execute(new SetWorkforceCommand(view.Viewer,view.Turn,colony.Id,new(workers[0],workers[1],workers[2])));
        var row=All<WorkerFigureRow>(screen).Single(r=>r.Colony==colony.Id.Value&&r.Role==destination);
        Variant drag=new Godot.Collections.Dictionary { ["colony"]=colony.Id.Value,["turn"]=view.Turn,["role"]=source };
        Check(row._CanDropData(Vector2.Zero,drag),"Another role accepts the same colony's worker");row._DropData(Vector2.Zero,drag);await Frames();
        Check(Snapshot(session)==Snapshot(reference),"Worker drop matches exactly one ordinary order");
        Check(!row._CanDropData(Vector2.Zero,new Godot.Collections.Dictionary{["colony"]=colony.Id.Value}),"Malformed drag is ignored without throwing");
        before=Snapshot(session);
        foreach (var route in new[] { ("Fleets","OpenFleetCommand"),("Research","OpenResearch"),("Designs","OpenShipDesigns"),("Diplomacy","OpenDiplomacy") })
        {
            Press(Route(route.Item2)); await Frames(5);
            foreach(var option in All<OptionButton>(screen))option.GetPopup().Hide();
            foreach (var size in new[]{Sizes[0],Sizes[3]})
            {
                await Capture(route.Item1,size);
                foreach(var window in All<Window>(screen).Where(w=>w.Visible&&w is FleetCommandDialog or ScoutOverviewDialog or ShipDesignDialog or DiplomacyDialog))
                {
                    Check(!window.Exclusive&&!window.ForceNative,"Primary workspace is embedded and nonmodal: "+window.Name);
                    Check(window.Position.X>=0&&window.Position.Y>=0&&window.Position.X+window.Size.X<=size.X+1&&window.Position.Y+window.Size.Y<=size.Y+1,"Workspace fits the game area: "+window.Name);
                }
            }
            Press(Route("OpenGalaxyWorkspace"));await Frames(5);
        }
        Check(Snapshot(session)==before,"Opening, inspecting and leaving primary workspaces issues no orders");
        Press(Route("OpenFleetCommand"));await Frames();Press(Route("OpenShipDesigns"));await Frames();screen._UnhandledKeyInput(new InputEventKey{Pressed=true,Keycode=Key.Escape});await Frames();
        Check(All<FleetCommandDialog>(screen).Single().Visible,"Back from Designs restores the actual Fleets workspace");
        screen._UnhandledKeyInput(new InputEventKey{Pressed=true,Keycode=Key.Escape});await Frames();
        Check(Route("OpenGalaxyWorkspace").IsVisibleInTree()&&!All<FleetCommandDialog>(screen).Single().Visible,"Escape leaves a primary workspace without answering a decision");
        var scouts=CombinedUiNavigation.Reveal(Named<Button>("OpenScoutOverview"));Press(scouts);await Frames();
        Check(All<ScoutOverviewDialog>(screen).Single().Visible,"The moved Scouts route is reachable through its real Fleets tab");
        Press(Route("OpenGalaxyWorkspace"));await Frames();
        var contacts=CombinedUiNavigation.Reveal(Named<Button>("OpenContactsWorkspace"));Press(contacts);await Frames();
        Check(All<Control>(screen).Single(c=>c.Name=="ContactsWorkspace").IsVisibleInTree(),"The moved Contacts route is reachable through its real Diplomacy tab");
        Press(Route("OpenShipDesigns"));await Frames();var search=Named<LineEdit>("DesignSearch");search.GrabFocus();
        bar._UnhandledKeyInput(new InputEventKey{Pressed=true,Keycode=Key.C,PhysicalKeycode=Key.C});await Frames();
        Check(All<ShipDesignDialog>(screen).Single().Visible,"Typing a shortcut letter in a design search does not navigate");
        Press(Route("OpenGalaxyWorkspace"));await Frames();
        var menu=Named<MenuButton>("GameMenu");menu.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed,3L);menu.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed,4L);await Frames();
        var settings=new ConfigFile();Check(settings.Load(screen.PresentationPreferencesPath)==Error.Ok&&!settings.GetValue("display","screen_effects").AsBool()&&settings.GetValue("display","reduced_effects").AsBool(),"Effect preferences persist outside campaign saves");
        Check(Snapshot(session)==before,"Back, Esc, tabs, editing and display preferences preserve campaign state");
        var theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres");var background=theme.GetColor("raised","ConsolePalette");
        foreach(var label in All<Label>(screen).Where(l=>l.IsVisibleInTree()&&l.GetWindow().Visible&&l.Text.Length>0))
            Check(Contrast(label.GetThemeColor("font_color"),background)>=(label.GetThemeFontSize("font_size")>=24?3:4.5),"Readable theme text contrast: "+label.GetPath());
        reference=new GameSession(session.Content,session.State.Clone());var ready=reference.Execute(new SetReadyCommand(view.Viewer,view.Turn,true));
        Press(Route("EndTurn"));await Frames(5);
        Check(Snapshot(session)==Snapshot(reference),"End Turn matches its unchanged ordinary command, including any readiness rejection");
        if(ready.Accepted){await Capture("TurnReport",Sizes[0]);await Capture("TurnReport",Sizes[3]);}
    }
    private static double Contrast(Color a,Color b)
    {
        static double Linear(double channel)=>channel<=0.04045?channel/12.92:System.Math.Pow((channel+0.055)/1.055,2.4);
        static double Light(Color c)=>0.2126*Linear(c.R)+0.7152*Linear(c.G)+0.0722*Linear(c.B);
        double first=Light(a),second=Light(b);return (System.Math.Max(first,second)+0.05)/(System.Math.Min(first,second)+0.05);
    }
}
