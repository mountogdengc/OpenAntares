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
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;

namespace OpenAntares.Game.Validation;

/// <summary>Focused native checks for the orbital system view that floats over the galaxy map.</summary>
public partial class SystemViewValidation : Control
{
    private readonly List<string> _checks = new();
    private string _output = "";
    private void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        _checks.Add(name);
    }
    private async Task Frames(int count = 4)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (Node child in root.GetChildren(true))
        {
            if (child is T item) yield return item;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    private void Capture(string name) => Check(GetTree().Root.GetTexture().GetImage().SavePng(Path.Combine(_output, name + ".png")) == Error.Ok, "Capture " + name);

    public override async void _Ready()
    {
        string? failure = null;
        try
        {
            _output = OS.GetCmdlineUserArgs().Single(a => a.StartsWith("--validation-output="))[20..];
            Check(DisplayServer.GetName() != "headless", "Native rendered display");
            await Frames();
            foreach (var size in new[] { new Vector2I(800, 600), new Vector2I(1920, 1080) }) await RunAt(size);
        }
        catch (Exception error) { failure = error.ToString(); GD.PrintErr(failure); }
        finally
        {
            File.WriteAllText(Path.Combine(_output, "results.json"), JsonSerializer.Serialize(new { passed = failure is null, error = failure, checks = _checks }, new JsonSerializerOptions { WriteIndented = true }));
            GetTree().Quit(failure is null ? 0 : 1);
        }
    }

    private async Task RunAt(Vector2I size)
    {
        GetTree().Root.Size = size;
        var main = GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();
        main.GetType().GetProperty("PresentationPreferencesPath")?.SetValue(main, Path.Combine(_output, "presentation-" + size.X + ".cfg"));
        AddChild(main); await Frames(5);
        var setup = Descendants<NewGameScreen>(main).Single();
        setup.Configure("small", 42, "harl", 1, "peldra");
        Descendants<Button>(setup).Single(b => b.Text == "Start").EmitSignal(BaseButton.SignalName.Pressed); await Frames(8);
        var screen = Descendants<GameScreen>(main).Single();
        var game = Field<GameSession>(screen, "_session"); var viewer = Field<EmpireId>(screen, "_viewer");
        var view = game.View(viewer); var home = view.Colonies[0];
        var star = view.Stars.Single(s => s.Chart.Id == home.StarId);
        string before = SaveGame.Serialize(game.Content, game.State);
        var map = Descendants<GalaxyMap>(screen).Single();
        var panel = Descendants<StarPanel>(screen).Single();
        Check(!panel.IsVisibleInTree(), $"{size}: system view starts closed");

        map.Select(home.StarId); await Frames();
        var inspect = Descendants<Button>(screen).Single(b => b.Name == "InspectSelectedSystem");
        inspect.EmitSignal(BaseButton.SignalName.Pressed); await Frames(6);
        Check(panel.IsVisibleInTree() && map.IsVisibleInTree(), $"{size}: system view opens over a visible galaxy map");
        Check(map.GetGlobalRect().Encloses(panel.GetGlobalRect()), $"{size}: system view fits inside the map");
        Check(Descendants<Label>(panel).Any(l => l.Text == star.Chart.Name), $"{size}: title names the star");
        var keys = Descendants<Button>(panel).Where(b => b.Name.ToString().StartsWith("OrbitPlanet_")).ToArray();
        Check(keys.Length == star.Planets!.Value.Length && keys.All(k => Descendants<PlanetGlobe2D>(k).Count() == 1), $"{size}: one globe button per known planet");
        var orbit = Descendants<SystemOrbitView>(panel).Single();
        Check(keys.All(k => orbit.GetGlobalRect().Grow(1).Encloses(k.GetGlobalRect())), $"{size}: every planet sits inside the diagram");
        Check(keys.All(k => k.Size.X >= 14), $"{size}: planets are large enough to click");
        Capture("system-home-" + size.X);

        var target = star.Planets!.Value.OrderBy(p => p.Orbit).Last();
        keys.Single(k => k.Name == "OrbitPlanet_" + target.Orbit).EmitSignal(BaseButton.SignalName.Pressed); await Frames(6);
        Check(Field<PlanetId?>(screen, "_selectedPlanet") == target.Id, $"{size}: clicking a planet selects that exact world");
        Check(panel.IsVisibleInTree() && Descendants<Label>(panel).Any(l => l.Text.StartsWith($"Orbit {target.Orbit}:", StringComparison.Ordinal)), $"{size}: detail strip explains the selected world");
        Check(Descendants<Button>(panel).Single(b => b.Name == "OrbitPlanet_" + target.Orbit).ButtonPressed, $"{size}: selected planet shows pressed");
        Capture("system-selected-" + size.X);

        var press = new InputEventKey { Keycode = Key.Escape, PhysicalKeycode = Key.Escape, Pressed = true };
        GetViewport().PushInput(press); await Frames(4);
        Check(!panel.IsVisibleInTree() && map.Selected == home.StarId, $"{size}: Esc closes the view but keeps the star selected (open={panel.IsVisibleInTree()}, selected={map.Selected}, windows={string.Join(",", Descendants<Window>(screen).Where(w => w.Visible).Select(w => w.Name))}, focus={GetViewport().GuiGetFocusOwner()?.Name})");

        var unknown = view.Stars.FirstOrDefault(s => s.Planets is null);
        if (unknown is not null)
        {
            map.Select(unknown.Chart.Id); await Frames();
            inspect.EmitSignal(BaseButton.SignalName.Pressed); await Frames(6);
            Check(Descendants<Label>(panel).Any(l => l.Text == "Not surveyed") && !Descendants<Button>(panel).Any(b => b.Name.ToString().StartsWith("OrbitPlanet_")), $"{size}: unsurveyed star shows no fabricated planets");
            Capture("system-unsurveyed-" + size.X);
            Descendants<Button>(panel).Single(b => b.Name == "CloseSystemView").EmitSignal(BaseButton.SignalName.Pressed); await Frames();
            Check(!panel.IsVisibleInTree(), $"{size}: Close button closes the view");
        }
        Check(SaveGame.Serialize(game.Content, game.State) == before, $"{size}: inspecting never changes game state");
        RemoveChild(main); main.QueueFree(); await Frames();
    }
}
