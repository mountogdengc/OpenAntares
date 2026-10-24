using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;

namespace OpenAntares.GameValidation;

/// <summary>
/// Opt-in runtime checks of the actual game controls and command/save handlers.
/// Signals and map handlers are dispatched in-process; this does not test OS input or comprehension.
/// </summary>
public partial class GraphicalValidation : Node
{
    protected readonly List<object> _results = new();
    protected readonly List<string> _savePaths = new();
    protected readonly string _runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", System.Globalization.CultureInfo.InvariantCulture);
    protected string _output = string.Empty;

    public override async void _Ready()
    {
        string? error = null;
        try
        {
            _output = OS.GetCmdlineUserArgs().Single(a => a.StartsWith("--validation-output=", StringComparison.Ordinal))[20..];
            Directory.CreateDirectory(_output);
            Check(DisplayServer.GetName() != "headless", "A graphical display is required");
            Check(GetTree().Root.Size == new Vector2I(1920, 1080), "Actual default window is 1920x1080");
            foreach (string size in new[] { "small", "medium", "large" })
            {
                await CheckLoop(size);
            }
        }
        catch (Exception exception)
        {
            error = exception.ToString();
            GD.PrintErr(error);
        }
        finally
        {
            if (_output.Length > 0)
            {
                File.WriteAllText(Path.Combine(_output, "results.json"), JsonSerializer.Serialize(new
                {
                    runId = _runId,
                    godot = Engine.GetVersionInfo()["string"].AsString(),
                    display = DisplayServer.GetName(),
                    method = "In-process real UI control signals/handlers, graphical layout and framebuffer assertions; no OS input claims",
                    passed = error is null,
                    error,
                    results = _results,
                    retainedQaSavePaths = _savePaths,
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            GetTree().Quit(error is null ? 0 : 1);
        }
    }

    protected async Task CheckLoop(string size)
    {
        GetTree().Root.Size = new Vector2I(1920, 1080);
        var main = GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();
        AddChild(main);
        await Frames();
        NewGameScreen setup = One<NewGameScreen>(main);
        setup.Configure(size, 42);
        Press(Button(setup, "Start"));
        await Frames();
        GameScreen screen = One<GameScreen>(main);
        var legacyContent = SaveGame.ReadFile(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/legacy-v2-save.json")).Game!.Content;
        var legacyState = GalaxyGenerator.CreateNewGame(legacyContent,new NewGameSettings(size,42,Rules.LegacyVersion)).State!;
        string legacyFixtureName=$"qa-{_runId}-{size}-legacy-loop";
        string legacyFixturePath=SaveFiles.PathFor(legacyFixtureName);
        SaveGame.WriteFile(legacyFixturePath,legacyContent,legacyState);_savePaths.Add(legacyFixturePath);
        screen=await LoadThroughDialog(main,legacyFixtureName);
        GameSession session = Session(screen);
        Check(session.State.RulesVersion==Rules.LegacyVersion,$"{size}: old-loop fixture is legacy rules 1");
        int expectedCount = size == "small" ? 12 : size == "medium" ? 16 : 20;
        Check(session.State.Stars.Count == expectedCount, $"{size}: exact selectable system count {expectedCount}");
        Check(session.State.Colonies.Single().Population == 6, $"{size}: starting population");
        CheckMap(screen, session, size);
        await Capture($"{size}-initial");
        string initialState = Snapshot(session);
        foreach (Vector2I dimensions in new[] { new Vector2I(800, 600), new Vector2I(1152, 648), new Vector2I(1600, 900), new Vector2I(1920, 1080) })
        {
            GetTree().Root.Size = dimensions;
            await Frames();
            Check(GetTree().Root.Size == dimensions, $"{size}: live resize to {dimensions.X}x{dimensions.Y}");
            Check(Snapshot(session) == initialState, $"{size}: resize leaves authoritative state unchanged");
            Check(One<GalaxyMap>(screen).Size.X > 0 && One<GalaxyMap>(screen).Size.Y > 0, $"{size}: map remains laid out after resize");
            if (dimensions.X is 800 or 1600) await Capture($"{size}-resize-{dimensions.X}x{dimensions.Y}");
        }

        ColonyPanel colony = One<ColonyPanel>(screen);
        Press(Step(colony, "Move a worker from support to production"));
        await Frames();
        Check(session.Forecast().Colonies.Single().SupportDeficit == 300, $"{size}: shortage forecast");
        Check(Descendants<Label>(colony).Any(l => l.Text.StartsWith("Support shortage of 3", StringComparison.Ordinal)), $"{size}: visible shortage warning");
        Press(Step(colony, "Move a worker from production to support"));
        await Frames();
        Press(Step(colony, "Move a worker from production to support"));
        await Frames();
        Check(session.Forecast().Colonies.Single().GrowthEarned.Total == 40, $"{size}: supported allocation increases growth");
        Press(Step(colony, "Move a worker from support to production"));
        await Frames();

        Select(Descendants<OptionButton>(colony).Single(), "fabrication_hall");
        Select(Descendants<OptionButton>(One<ResearchBar>(screen)).Single(), "cultivation_methods");
        await Frames();
        for (int turn = 1; turn <= 2; turn++)
        {
            await EndTurn(screen);
            Check(session.State.Turn == turn, $"{size}: end turn {turn}");
            One<TurnReportDialog>(screen).Hide();
        }
        GameState beforeCompletion = session.State.Clone();
        string preSnapshot = Snapshot(session);
        string framing = MapBounds(screen);
        string preName = await SaveThroughDialog(screen, $"{size}-pre");
        await EndTurn(screen);
        Check(session.State.Colonies.Single().CompletedBuildingIds.Contains("fabrication_hall"), $"{size}: building completes");
        Check(session.State.Empires.Single().KnownTechnologyIds.Contains("cultivation_methods"), $"{size}: research completes");
        Check(session.Forecast().Colonies.Single().NetProduction.Total == 500, $"{size}: building applies to next snapshot");
        Check(session.State.Colonies.Single().ProjectId is null && session.State.Empires.Single().ResearchTargetId is null, $"{size}: completed targets clear");
        One<TurnReportDialog>(screen).Hide();
        string postSnapshot = Snapshot(session);
        string postName = await SaveThroughDialog(screen, $"{size}-post");
        screen = await LoadThroughDialog(main, preName);
        Check(Snapshot(Session(screen)) == preSnapshot, $"{size}: UI reload before completion preserves state/content");
        Check(MapBounds(screen) == framing, $"{size}: UI reload preserves map bounds");
        await EndTurn(screen);
        Check(Snapshot(Session(screen)) == postSnapshot, $"{size}: continuation after UI reload matches uninterrupted result");
        One<TurnReportDialog>(screen).Hide();
        screen = await LoadThroughDialog(main, postName);
        Check(Snapshot(Session(screen)) == postSnapshot, $"{size}: UI reload after completion preserves state/content");
        session = Session(screen);
        for (int turn = session.State.Turn; turn < 10; turn++)
        {
            await EndTurn(screen);
            One<TurnReportDialog>(screen).Hide();
        }
        Check(session.State.Colonies.Single().Population == 7, $"{size}: population advances through real End Turn control");
        if (size == "small")
        {
            await CheckInvalidAndLegacyLoad(main, screen);
        }
        RemoveChild(main);
        main.QueueFree();
        await Frames();

        foreach (Vector2I windowSize in new[] { new Vector2I(800, 600), new Vector2I(1152, 648), new Vector2I(1600, 900), new Vector2I(1920, 1080) })
        {
            foreach (bool otherSelected in new[] { false, true })
            {
                await CheckProjectPopup(session.Content, beforeCompletion, size, windowSize, otherSelected);
            }
        }
        // Native popup viewports have no readable texture on this GL driver. Keep native
        // coordinate assertions above, and capture an additional embedded-popup scenario.
        GetTree().Root.GuiEmbedSubwindows = true;
        await CheckProjectPopup(session.Content, beforeCompletion, size, new Vector2I(800, 600), true, embedded: true);
        GetTree().Root.GuiEmbedSubwindows = false;
        var researchSession = new GameSession(session.Content, beforeCompletion.Clone());
        var researchScreen = new GameScreen(researchSession, researchSession.State.Empires.Single().Id);
        AddChild(researchScreen);
        await Frames();
        await EndTurn(researchScreen);
        Press(Button(One<TurnReportDialog>(researchScreen), "Choose next research"));
        await Frames();
        Check(Descendants<OptionButton>(One<ResearchBar>(researchScreen)).Single().GetPopup().Visible, $"{size}: research follow-up opens picker");
        RemoveChild(researchScreen);
        researchScreen.QueueFree();
        await Frames();
    }

    protected async Task CheckProjectPopup(ContentSet content, GameState fixture, string size, Vector2I dimensions, bool otherSelected, bool embedded = false)
    {
        GetTree().Root.Size = dimensions;
        var session = new GameSession(content, fixture.Clone());
        var screen = new GameScreen(session, session.State.Empires.Single().Id);
        AddChild(screen);
        await Frames();
        ScrollContainer scroll = Descendants<ScrollContainer>(screen).Single(s => s.GetWindow()==screen.GetWindow());
        // Canvas scaling preserves logical height at small window sizes. Explicitly force
        // the formerly failing focus-scroll path without changing production layout code.
        scroll.CustomMinimumSize = new Vector2(0, 240);
        scroll.GetParent().GetParent<Control>().SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        await Frames();
        StarId home = session.State.FindPlanet(session.State.Colonies.Single().PlanetId)!.StarId;
        if (otherSelected) One<GalaxyMap>(screen).Select(session.State.Stars.First(s => s.Id != home).Id);
        await EndTurn(screen);
        Press(Button(One<TurnReportDialog>(screen), "Choose next project"));
        await Frames(8);
        OptionButton picker = Descendants<OptionButton>(One<ColonyPanel>(screen)).Single();
        PopupMenu popup = picker.GetPopup();
        string label = $"{size}-{dimensions.X}x{dimensions.Y}-{(otherSelected ? "other" : "home")}-{(embedded ? "embedded" : "native")}";
        Check(popup.Visible && picker.IsVisibleInTree(), $"{label}: FP-001 popup opens");
        Check(scroll.ScrollVertical > 0, $"{label}: focus-scroll path exercised");
        Check(scroll.GetGlobalRect().Intersects(picker.GetGlobalRect()), $"{label}: picker is inside the visible scroll region");
        Check(One<GalaxyMap>(screen).Selected == home, $"{label}: home selected");
        Vector2I automaticPosition = popup.Position;
        popup.Hide();
        picker.ShowPopup();
        await Frames();
        Check(popup.Position.DistanceTo(automaticPosition) <= 1, $"{label}: FP-002 position equals normal reopening");
        _results.Add(new { name = label + ": popup geometry", passed = true, requestedEmbedded = embedded, actualEmbedded = popup.IsEmbedded(), forceNative = popup.ForceNative, automaticX = automaticPosition.X, automaticY = automaticPosition.Y, normalX = popup.Position.X, normalY = popup.Position.Y, pickerScreenX = picker.GetScreenPosition().X, pickerScreenY = picker.GetScreenPosition().Y });
        if (dimensions.X == 800 && otherSelected)
        {
            if (embedded)
            {
                // OptionButton can force a native child window. Override only this
                // supplementary visual fixture after the ordinary geometry comparison.
                popup.Hide();
                popup.ForceNative = false;
                popup.PreferNativeMenu = false;
                picker.ShowPopup();
                await Frames();
                Check(popup.IsEmbedded(), $"{label}: supplementary visual popup is actually embedded");
            }
            await Capture($"{label}-construction-parent");
        }
        popup.Hide();
        Select(picker, "cultivation_hub");
        await Frames();
        Check(session.State.Colonies.Single().ProjectId == "cultivation_hub", $"{label}: selected project reaches command path");
        RemoveChild(screen);
        screen.QueueFree();
        await Frames();
    }

    private void CheckMap(GameScreen screen, GameSession session, string size)
    {
        GalaxyMap map = One<GalaxyMap>(screen);
        MethodInfo transformMethod = typeof(GalaxyMap).GetMethod("BuildTransform", BindingFlags.NonPublic | BindingFlags.Instance)!;
        MethodInfo pointMethod = typeof(GalaxyMap).GetMethod("ToScreen", BindingFlags.NonPublic | BindingFlags.Instance)!;
        object transform = transformMethod.Invoke(map, null)!;
        Vector2 Point(int x, int y) => (Vector2)pointMethod.Invoke(map, new[] { (object)x, y, transform })!;
        foreach (StarState star in session.State.Stars)
        {
            map._GuiInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left, Position = Point(star.X, star.Y) });
            Check(map.Selected == star.Id, $"{size}: map handler selects {star.Name}");
            Check(Descendants<Label>(One<StarPanel>(screen)).Any(l => l.Text == star.Name), $"{size}: inspector updates for {star.Name}");
        }
        StarId home = session.State.FindPlanet(session.State.Colonies.Single().PlanetId)!.StarId;
        map.Select(home);
        float radius = map.GetThemeConstant("selection_radius", "GalaxyMap") * 1.5f;
        var background = GalaxyBackgroundPoints.Generate(42, session.State.GalaxyWidth!.Value, session.State.GalaxyHeight!.Value)
            .First(p => session.State.Stars.All(s => Point(s.X, s.Y).DistanceTo(Point(p.X, p.Y)) > radius));
        map._GuiInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left, Position = Point(background.X, background.Y) });
        Check(map.Selected == home, $"{size}: background point cannot be selected");
    }

    protected async Task<string> SaveThroughDialog(GameScreen screen, string suffix)
    {
        string name = $"qa-{_runId}-{suffix}";
        string path = SaveFiles.PathFor(name);
        Check(!File.Exists(path), "QA save name is unique");
        Press(Button(screen, "Save"));
        await Frames();
        SaveGameDialog dialog = One<SaveGameDialog>(screen);
        Check(dialog.Visible, $"{suffix}: Save dialog opens");
        One<LineEdit>(dialog).Text = name;
        Press(dialog.GetOkButton());
        await Frames();
        Check(!dialog.Visible && File.Exists(path), $"{suffix}: Save dialog writes and closes");
        _savePaths.Add(path);
        Check(SaveGame.Serialize(SaveGame.ReadFile(path).Game!.Content, SaveGame.ReadFile(path).Game!.State) == Snapshot(Session(screen)), $"{suffix}: saved state matches session");
        return name;
    }

    protected async Task<GameScreen> LoadThroughDialog(Control main, string name)
    {
        GameScreen previous = One<GameScreen>(main);
        Press(Button(previous, "Load"));
        await Frames();
        LoadGameDialog dialog = One<LoadGameDialog>(main);
        ItemList list = One<ItemList>(dialog);
        int index = Enumerable.Range(0, list.ItemCount).Single(i => list.GetItemText(i).StartsWith(name + "    ", StringComparison.Ordinal));
        list.Select(index);
        list.EmitSignal(ItemList.SignalName.ItemSelected, index);
        Press(dialog.GetOkButton());
        await Frames();
        return One<GameScreen>(main);
    }

    protected async Task CheckInvalidAndLegacyLoad(Control main, GameScreen current)
    {
        string invalidName = $"qa-{_runId}-invalid";
        string invalidPath = SaveFiles.PathFor(invalidName);
        Check(!File.Exists(invalidPath), "Invalid QA name is unique");
        File.WriteAllText(invalidPath, "{ invalid save");
        _savePaths.Add(invalidPath);
        string before = Snapshot(Session(current));
        GameScreen after = await LoadThroughDialog(main, invalidName);
        Check(ReferenceEquals(current, after) && Snapshot(Session(after)) == before, "Corrupt UI load retains the current screen and state");
        LoadGameDialog dialog = One<LoadGameDialog>(main);
        Check(dialog.Visible && Descendants<Label>(dialog).Any(l => l.Visible && l.Text.Contains("can't be loaded", StringComparison.Ordinal)), "Corrupt UI load shows an error");
        dialog.Hide();

        string legacyName = $"qa-{_runId}-legacy-v1";
        string legacyPath = SaveFiles.PathFor(legacyName);
        File.Copy(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/legacy-v1-save.json"), legacyPath, overwrite: false);
        _savePaths.Add(legacyPath);
        LoadedGame expected = SaveGame.ReadFile(legacyPath).Game!;
        GameScreen loaded = await LoadThroughDialog(main, legacyName);
        Check(Snapshot(Session(loaded)) == SaveGame.Serialize(expected.Content, expected.State), "Legacy schema-1 UI load preserves original state/layout");
        Check(Session(loaded).State.GalaxySeed is null && Session(loaded).State.Stars.All(s => s.RegionId is null), "Legacy UI load does not invent galaxy metadata");
        await Capture("legacy-v1-loaded");
        await EndTurn(loaded);
        Check(Session(loaded).State.Turn == expected.State.Turn + 1, "Legacy UI load can continue a turn");
        One<TurnReportDialog>(loaded).Hide();
    }

    protected async Task EndTurn(GameScreen screen)
    {
        int before = Session(screen).State.Turn;
        Press(Button(screen, "End Turn"));
        await Frames();
        Check(Session(screen).State.Turn == before + 1 && One<TurnReportDialog>(screen).Visible, "End Turn advances once and opens report");
    }

    protected async Task Capture(string name)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Error error = GetViewport().GetTexture().GetImage().SavePng(Path.Combine(_output, name + ".png"));
        Check(error == Error.Ok, $"Screenshot saved: {name}");
    }

    protected async Task Frames(int count = 4)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    protected void Check(bool condition, string name)
    {
        _results.Add(new { name, passed = condition });
        if (!condition) throw new InvalidOperationException(name);
        GD.Print("PASS: " + name);
    }

    protected static IEnumerable<T> Descendants<T>(Node node) where T : Node => node.GetChildren().SelectMany(child =>
        (child is T match ? new[] { match } : Array.Empty<T>()).Concat(Descendants<T>(child)));
    protected static T One<T>(Node node) where T : Node => Descendants<T>(node).Single();
    protected static Button Button(Node node, string text) => Descendants<Button>(node).Single(b => b.Text == text && b.IsVisibleInTree() && b.GetWindow().Visible);
    protected static Button Step(Node node, string tooltip) => Descendants<Button>(node).Single(b => b.TooltipText == tooltip);
    protected static void Press(Button button)
    {
        if (button.Disabled) throw new InvalidOperationException("Cannot press disabled button: " + button.Text);
        button.EmitSignal(Godot.Button.SignalName.Pressed);
    }
    protected static void Select(OptionButton picker, string id)
    {
        int index = Enumerable.Range(0, picker.ItemCount).Single(i => picker.GetItemMetadata(i).AsString() == id);
        if (picker.IsItemDisabled(index)) throw new InvalidOperationException("Cannot select disabled option: " + id);
        picker.Select(index);
        picker.EmitSignal(OptionButton.SignalName.ItemSelected, index);
    }
    protected static GameSession Session(GameScreen screen) => (GameSession)typeof(GameScreen).GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(screen)!;
    protected static string Snapshot(GameSession session) => SaveGame.Serialize(session.Content, session.State);
    protected static string MapBounds(GameScreen screen) => string.Join(",", new[] { "_minX", "_minY", "_maxX", "_maxY" }
        .Select(name => typeof(GalaxyMap).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(One<GalaxyMap>(screen))));
}
