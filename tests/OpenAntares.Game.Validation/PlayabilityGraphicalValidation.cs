using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Screens;
using OpenAntares.Game.Components;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Saves;

namespace OpenAntares.Game.Validation;

/// <summary>Separate native M13 harness: assertions use real controls; no physical-input claim.</summary>
public partial class PlayabilityGraphicalValidation : Node
{
    private string _output = "";
    private readonly List<object> _checks = new();
    private Control? _main;
    private string _case = "Baseline";

    private void Check(bool passed, string name)
    {
        _checks.Add(new { passed, name });
        if (!passed) throw new InvalidOperationException(name);
    }

    public override async void _Ready()
    {
        string? error = null;
        try
        {
            var args = OS.GetCmdlineUserArgs();
            _output = args.Single(a => a.StartsWith("--validation-output="))[20..];
            _case = args.Single(a => a.StartsWith("--playability-case="))[19..];
            Directory.CreateDirectory(_output);
            Check(DisplayServer.GetName() != "headless", "Actual graphical display is required");
            if (_case == "Baseline")
            {
                await Baseline();
                if(args.Contains("--playability-lifetime-probe"))await LifetimeProbe();
            }
            else if (_case == "Map")
            {
                var live = Live(await NewMain("harl", 42, 1));
                string before = Snapshot(live);
                await PlayabilityMapChecks.Run(this, _output, Check);
                Check(before == Snapshot(live), "Map selection fixtures preserve the actual Main campaign and PRNG");
            }
            else if (_case == "Selection") await Selection();
            else if (_case == "Setup") await Setup();
            else if (_case == "Colony") await Colonies();
            else if (_case == "Navigation") { var screen = await NewMain("harl", 42, 1); await PlayabilityMenuChecks.Run(this, screen, Live(screen), _output, Check); await PlayabilityNavigationChecks.Run(this, screen, Live(screen), _output, Check);
                foreach (var size in new[] { new Vector2I(800,600), new Vector2I(1152,648), new Vector2I(1600,900), new Vector2I(1920,1080) })
                { GetTree().Root.Size=size; await Frames(); screen=await SaveReload(screen,"navigation-"+size.X); }
            }
            else if (_case == "Guidance")
            {
                var screen = await NewMain("harl",42,1);var content=Live(screen).Content;
                await PlayabilityGuidanceChecks.Run(this,screen,Live(screen),_output,Check);
                await PlayabilityGuidanceChecks.EngineRejections(this,content,MountSupplement,Check);
            }
            else if (_case == "Campaigns") await PlayabilityCampaignChecks.Run(this,_output,NewMain,SaveReload,Check,args.Contains("--playability-diagnostic-wenth-solo"),args.Contains("--playability-diagnostic-hide-console"));
            else throw new InvalidOperationException("Case is not implemented: " + _case);
        }
        catch (Exception e) { error = e.ToString(); GD.PrintErr(error); }
        finally
        {
            if (_output.Length > 0)
                File.WriteAllText(Path.Combine(_output, "results.json"), JsonSerializer.Serialize(new
                {
                    @case = _case, passed = error is null, error,
                    method = "Actual native Godot controls, requester views and retained snapshots; no physical-input or human-acceptance claim",
                    provenance = _case is "Map" or "Selection" or "Colony" or "Guidance" ? "Labelled requester-projection supplements plus actual Main state/command checks" : "Actual native control baseline; human acceptance remains separate",
                    campaignMatrix = _case == "Campaigns" && File.Exists(Path.Combine(_output,"campaign-matrix.json")) ? EvidenceArtifact(Path.Combine(_output,"campaign-matrix.json")) : null,
                    checks = _checks
                }, new JsonSerializerOptions { WriteIndented = true }));
            GetTree().Quit(error is null ? 0 : 1);
        }
    }

    private async Task LifetimeProbe()
    {
        var screen=Descendants<GameScreen>(_main!).Single();var game=Live(screen);string before=Snapshot(game);
        GD.Print("Lifetime probe: schedule actual semantic focus then dispose the GameScreen, as during replacement");
        typeof(GameScreen).GetMethod("RestoreSemanticFocus",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(screen,new object?[]{"OpenGalaxyWorkspace",0UL,"Galaxy",null});
        screen.QueueFree();await Frames(5);
        Check(before==Snapshot(game),"Diagnostic screen disposal issues no command or PRNG change");
        GD.Print("Lifetime probe: stable host completed five frames after screen disposal");
    }

    private async Task Baseline()
    {
        var screen = await NewMain("harl", 42, 1);
        var session = Live(screen);
        string before = Snapshot(session);
        Check(session.State.RulesVersion == 12, "Untouched current Main starts rules12");
        Check(Find<MenuButton>(screen, "GameMenu").GetPopup().ItemCount == 3, "Baseline Game menu has its three original actions");
        Check(Find<Control>(screen, "EndTurn").IsVisibleInTree(), "Baseline End Turn is visible");
        File.WriteAllText(Path.Combine(_output, "baseline-current-snapshot.json"), before);
        File.WriteAllText(Path.Combine(_output, "baseline-current-state.json"), JsonSerializer.Serialize(session.State));
        Check(before == Snapshot(session), "Read-only baseline capture changes neither campaign nor PRNG");
        await Frames(4);
        GetViewport().GetTexture().GetImage().SavePng(Path.Combine(_output, "baseline-main.png"));
    }

    private async Task Setup()
    {
        var screen = await NewMain("harl", 42, 1);
        var game = Live(screen); string before = Snapshot(game);
        await PlayabilitySetupChecks.Run(this, game.Content, _output, Check);
        Check(before == Snapshot(game), "Setup inspection and submitted fixture requests do not mutate an existing Main campaign");
        var hint = Descendants<Control>(screen).SingleOrDefault(n => n.Name == "FirstTurnHint");
        Check(hint is not null && hint.IsVisibleInTree(), "Actual new Main campaign has one concise first-turn orientation");
        Find<Button>(hint!, "DismissFirstTurnHint").EmitSignal(Button.SignalName.Pressed);
        await Frames();
        Check(!hint!.Visible && before == Snapshot(game), "Dismissing first-turn orientation changes no campaign state");
    }

    private async Task Colonies()
    {
        foreach (var profile in new[] { (Species: "harl", Seed: 42UL), (Species: "peldra", Seed: 45UL), (Species: "sulan", Seed: 43UL), (Species: "wenth", Seed: 42UL) })
        {
            var screen = await NewMain(profile.Species, profile.Seed, 1);
            var game = Live(screen);
            foreach (var size in new[] { new Vector2I(800, 600), new Vector2I(1152, 648), new Vector2I(1600, 900), new Vector2I(1920, 1080) })
            {
                GetTree().Root.Size = size; await Frames(4);
                await PlayabilityColonyChecks.Run(this, screen, game, _output, Check);
            }
            await PlayabilityColonyChecks.Knowledge(this, game, _output, Check);
            await PlayabilityColonyChecks.Shortage(this, game, Check);
        }
    }

    private async Task Selection()
    {
        var screen = await NewMain("harl", 42, 1);
        var game = Live(screen);
        await PlayabilitySelectionChecks.Run(this, game, _output, Check);
        var panel = Descendants<VesselPanel>(screen).Single();
        var issued = new List<object>();
        panel.CommandIssued += command => issued.Add(command);
        string before = Snapshot(game);
        var map = Descendants<GalaxyMap>(screen).Single();
        var view = game.View(game.State.Empires.First(e => e.Controller == OpenAntares.Simulation.State.ControllerKind.Human).Id);
        map.Select(view.Galaxy.Stars.Last().Id);
        await Frames();
        var destination = CombinedUiNavigation.Reveal(Find<OptionButton>(panel, "DestinationPicker"));
        destination.Select(0);
        var target = new StarId(destination.GetItemMetadata(0).AsInt32());
        destination.EmitSignal(OptionButton.SignalName.ItemSelected, 0L);
        await Frames();
        Check(issued.Count == 0 && before == Snapshot(game), "SelectionAndPreviewNeverIssueCommands: actual Main inspection and destination preview issue no orders");
        var course = CombinedUiNavigation.Reveal(Descendants<Button>(panel).Single(b => b.Text == "Set course"));
        for (Node? node = course.GetParent(); node is not null; node = node.GetParent()) if (node is ScrollContainer scroll) scroll.EnsureControlVisible(course);
        course.GrabFocus(); await Frames();
        Check(!course.Disabled && course.IsVisibleInTree(), "Existing explicit Set course is available after review");
        course.EmitSignal(Button.SignalName.Pressed); await Frames();
        Check(issued.Count == 1 && game.View(view.Viewer).Vessels.Single(v => v.Id == view.Vessels[0].Id).PendingMove == target,
            "Exactly one existing route command submits the reviewed destination for the selected owned vessel");
        Check(game.State.Turn == view.Turn, "Reviewing and applying a course does not advance a boundary");
    }

    private async Task<GameScreen> MountSupplement(LoadedGame fixture,string label)
    {
        Check(OpenAntares.Simulation.State.GameStateValidator.Validate(fixture.Content,fixture.State).Count==0,"Labelled non-earned guidance supplement validates before mounting");
        if(_main is not null){RemoveChild(_main);_main.QueueFree();await Frames(3);}
        string name="qa-m13-supplement-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+label;
        string path=SaveFiles.PathFor(name);Check(!File.Exists(path),"Labelled supplement QA path is unique and retained");
        SaveGame.WriteFile(path,fixture.Content,fixture.State);
        string crisis=Path.Combine(_output,label+"-crisis-prefs.json"),scout=Path.Combine(_output,label+"-scout-prefs.json");
        new OpenAntares.Game.Presentation.CrisisNotificationPreferences(OpenAntares.Game.Presentation.CrisisDisplayLevel.Digest,OpenAntares.Game.Presentation.NotificationLevel.Summary,System.Collections.Immutable.ImmutableArray<string>.Empty).Save(crisis);
        new OpenAntares.Game.Presentation.ScoutNotificationPreferences(OpenAntares.Game.Presentation.ScoutDisplayLevel.Digest).Save(scout);
        _main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();
        _main.ChildEnteredTree+=node=>
        {
            if(node is not GameScreen added)return;
            added.CrisisPreferencesPath=crisis;added.ScoutPreferencesPath=scout;
            added.NotificationPreferencesPath=Path.Combine(_output,label+"-colony-prefs.json");
            added.FleetPreferencesPath=Path.Combine(_output,label+"-fleet-prefs.json");added.CombatPreferencesPath=Path.Combine(_output,label+"-combat-prefs.json");
        };
        AddChild(_main);await Frames(5);
        Descendants<Button>(Descendants<NewGameScreen>(_main).Single()).Single(b=>b.Text=="Load Game").EmitSignal(Button.SignalName.Pressed);await Frames();
        var load=Descendants<LoadGameDialog>(_main).Single();var list=Descendants<ItemList>(load).Single();
        int index=Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemText(i).StartsWith(name+"    ",StringComparison.Ordinal));
        list.Select(index);list.EmitSignal(ItemList.SignalName.ItemSelected,(long)index);load.GetOkButton().EmitSignal(Button.SignalName.Pressed);await Frames(5);
        var screen=Descendants<GameScreen>(_main).Single();
        Check(Snapshot(Live(screen))==SaveGame.Serialize(fixture.Content,fixture.State),"Actual Main Load exactly preserves the labelled supplement; not earned campaign evidence");
        File.WriteAllText(Path.Combine(_output,label+"-supplement.json"),File.ReadAllText(path));
        File.WriteAllText(Path.Combine(_output,label+"-provenance.json"),JsonSerializer.Serialize(new{method="Labelled pre-session non-earned Ready integration supplement; actual Main Load",actualPath=path,rules=fixture.State.RulesVersion,injectedFixture=true,earnedCampaign=false}));
        return screen;
    }

    private async Task<GameScreen> SaveReload(GameScreen screen, string label)
    {
        ulong previousScreenId=screen.GetInstanceId();
        string before=Snapshot(Live(screen));
        string name="qa-m13-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+label;
        string path=SaveFiles.PathFor(name); Check(!File.Exists(path),"Retained M13 QA save name is unique");
        foreach(var window in Descendants<Window>(screen))window.Hide();
        CombinedUiNavigation.GameMenu(screen,0);await Frames();
        var save=Descendants<SaveGameDialog>(screen).Single();
        Descendants<LineEdit>(save).Single().Text=name;
        Check(save.Visible&&!save.GetOkButton().Disabled,"Actual Main Save action is available");
        save.GetOkButton().EmitSignal(Button.SignalName.Pressed);await Frames();
        Check(!save.Visible&&File.Exists(path)&&File.ReadAllText(path)==before,"Actual Main Save writes the exact current campaign without UI context fields");
        CombinedUiNavigation.GameMenu(screen,1);await Frames();
        var load=Descendants<LoadGameDialog>(_main!).Single();var list=Descendants<ItemList>(load).Single();
        int index=Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemText(i).StartsWith(name+"    ",StringComparison.Ordinal));
        list.Select(index);list.EmitSignal(ItemList.SignalName.ItemSelected,(long)index);
        Check(load.Visible&&!load.GetOkButton().Disabled,"Actual Main Load selects the retained QA copy");
        load.GetOkButton().EmitSignal(Button.SignalName.Pressed);await Frames(5);
        var loaded=Descendants<GameScreen>(_main!).Single();
        Check(before==Snapshot(Live(loaded)),"LoadResetsTransientContextWithoutCommands: actual Main load preserves canonical campaign and PRNG");
        var hoverBefore=Descendants<Window>(loaded).Where(w=>w.Visible).Select(w=>new{type=w.GetType().Name,name=w.Name.ToString(),labels=Descendants<Label>(w).Select(l=>new{l.Text,theme=l.ThemeTypeVariation.ToString()}).ToArray()}).ToArray();
        // Clear simulated engine hover before testing stale dialog context; this does not warp the OS pointer.
        Input.ParseInputEvent(new InputEventMouseMotion{Position=new Vector2(-32,-32),GlobalPosition=new Vector2(-32,-32)});await Frames(2);
        var loadUi=new{label,hoverBefore,previousScreen=previousScreenId,loadedScreen=loaded.GetInstanceId(),firstTurnHint=Descendants<Control>(loaded).Any(n=>n.Name=="FirstTurnHint"),visibleWindows=Descendants<Window>(loaded).Where(w=>w.Visible).Select(w=>new{type=w.GetType().Name,name=w.Name.ToString(),w.Title}).ToArray(),loadDialogVisible=load.Visible,loadErrors=Descendants<Label>(load).Where(l=>l.IsVisibleInTree()).Select(l=>l.Text).ToArray()};
        File.WriteAllText(Path.Combine(_output,label+"-load-ui.json"),JsonSerializer.Serialize(loadUi));
        Check(!loadUi.firstTurnHint&&loadUi.visibleWindows.Length==0,"Loaded campaign has no new-game hint or stale popup");
        var context=typeof(GameScreen).GetField("_context",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(loaded)!;
        var returns=context.GetType().GetProperty("Returns")!.GetValue(context)!;
        Check((int)returns.GetType().GetProperty("Count")!.GetValue(returns)! == 0,"Loaded campaign starts with an empty semantic return stack");
        File.WriteAllText(Path.Combine(_output,label+"-main-save.json"),File.ReadAllText(path));
        File.WriteAllText(Path.Combine(_output,label+"-provenance.json"),JsonSerializer.Serialize(new{method="Actual Main Save and Load controls",actualMainPath=path,saveSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()}));
        return loaded;
    }

    private async Task<GameScreen> NewMain(string species, ulong seed, int rivals)
    {
        if (_main is not null)
        {
            RemoveChild(_main); _main.QueueFree(); await Frames(3);
        }
        _main = GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();
        string prefs=Path.Combine(_output,"main-"+species+"-"+rivals);
        if(_case=="Campaigns")
        {
            if(!File.Exists(prefs+"-crisis.json"))new OpenAntares.Game.Presentation.CrisisNotificationPreferences(OpenAntares.Game.Presentation.CrisisDisplayLevel.Digest,OpenAntares.Game.Presentation.NotificationLevel.Summary,System.Collections.Immutable.ImmutableArray<string>.Empty).Save(prefs+"-crisis.json");
            if(!File.Exists(prefs+"-scout.json"))new OpenAntares.Game.Presentation.ScoutNotificationPreferences(OpenAntares.Game.Presentation.ScoutDisplayLevel.Digest).Save(prefs+"-scout.json");
        }
        _main.ChildEnteredTree+=node=>
        {
            if(node is not GameScreen added)return;
            added.NotificationPreferencesPath=prefs+"-colony.json";added.CombatPreferencesPath=prefs+"-combat.json";added.FleetPreferencesPath=prefs+"-fleet.json";
            added.ScoutPreferencesPath=prefs+"-scout.json";added.CrisisPreferencesPath=prefs+"-crisis.json";
        };
        AddChild(_main); await Frames(5);
        var setup = Descendants<NewGameScreen>(_main).Single();
        setup.Configure("small", seed, species, rivals, species == "peldra" ? "harl" : "peldra", true, true, true);
        var start = Descendants<Button>(setup).Single(b => b.Text == "Start");
        Check(!start.Disabled && start.IsVisibleInTree(), "Actual Main Start is reachable");
        start.EmitSignal(Button.SignalName.Pressed); await Frames(5);
        var screen = Descendants<GameScreen>(_main).Single();
        return screen;
    }

    private static object EvidenceArtifact(string path) => new { path=Path.GetRelativePath(ProjectSettings.GlobalizePath("res://"),path).Replace('\\','/'), sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant() };

    private async Task Frames(int count = 3)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is T typed) yield return typed;
            foreach (T nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static T Find<T>(Node root, string name) where T : Node => Descendants<T>(root).Single(n => n.Name == name);
    private static GameSession Live(GameScreen screen) => (GameSession)typeof(GameScreen).GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(screen)!;
    private static string Snapshot(GameSession game) => SaveGame.Serialize(game.Content, game.State);
}
