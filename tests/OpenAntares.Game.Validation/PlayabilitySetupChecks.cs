using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;

namespace OpenAntares.Game.Validation;

internal static class PlayabilitySetupChecks
{
    public static async Task Run(Node host, ContentSet content, string output, Action<bool, string> check)
    {
        foreach (var size in new[] { new Vector2I(800, 600), new Vector2I(1152, 648), new Vector2I(1600, 900), new Vector2I(1920, 1080) })
        {
            host.GetTree().Root.Size = size;
            var setup = new NewGameScreen(content);
            host.AddChild(setup);
            for (int i = 0; i < 5; i++) await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            setup.Configure("small", ulong.MaxValue, "sulan", 1, "harl", false, false, false);
            var advanced = Descendants<Control>(setup).SingleOrDefault(n => n.Name == "AdvancedSetupOptions");
            check(advanced is not null, "GroupedSetupPreservesSettingsAndStartsOnce: existing advanced settings are grouped");
            var toggle = Descendants<Button>(setup).Single(n => n.Name == "AdvancedSetupToggle");
            var start = Descendants<Button>(setup).Single(n => n.Text == "Start");
            var load = Descendants<Button>(setup).Single(n => n.Text.Contains("Load", StringComparison.Ordinal));
            var submitted = new List<NewGameSettings>();
            setup.StartRequested += submitted.Add;
            check(!advanced!.Visible && submitted.Count == 0, $"{size}: advanced settings initially collapse without starting");
            toggle.EmitSignal(Button.SignalName.Pressed);
            check(advanced.Visible, $"{size}: explicit toggle expands advanced choices");
            toggle.EmitSignal(Button.SignalName.Pressed);
            var review = Descendants<Label>(setup).Single(n => n.Name == "SetupReviewSummary");
            check(review.Text.Contains(ulong.MaxValue.ToString(), StringComparison.Ordinal), $"{size}: review includes actual seed even while its input is collapsed");
            foreach (var action in new[] { start, load })
                check(action.IsVisibleInTree() && new Rect2(Vector2.Zero, size).Encloses(action.GetGlobalRect()), $"{size}: {action.Text} fits physical viewport");
            start.EmitSignal(Button.SignalName.Pressed);
            start.EmitSignal(Button.SignalName.Pressed);
            check(submitted.Count == 1, $"{size}: repeated same-frame Start emits exactly one campaign request");
            var actual = submitted.Single();
            check(actual.GalaxySizeId == "small" && actual.Seed == ulong.MaxValue && actual.SpeciesId == "sulan" && actual.RivalCount == 1 && actual.RivalSpeciesId == "harl", $"{size}: collapsed setup preserves every identity choice");
            check(!actual.CrisesEnabled && actual.AmebasEnabled == false && actual.SentriesEnabled == false && actual.RulesVersion == 12, $"{size}: collapsed settings retain all hazard values and rules");
            setup.ShowError("Synthetic generation error: original choices remain available.");
            check(!start.Disabled, $"{size}: a reported generation failure restores Start authority");
            var seed = Descendants<LineEdit>(setup).Single(); seed.Text = "-1";
            start.EmitSignal(Button.SignalName.Pressed);
            check(submitted.Count == 1 && !start.Disabled, $"{size}: invalid seed emits no campaign and permits correction");
            check(Descendants<Label>(setup).Any(l => l.Visible && l.Text.Contains("whole number", StringComparison.OrdinalIgnoreCase)), $"{size}: invalid seed has a visible explanation");
            for (int i = 0; i < 4; i++) await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            check(advanced.Visible && seed.HasFocus(), $"{size}: invalid seed reveals and focuses its editable advanced input");
            check(review.Text.Contains("valid seed", StringComparison.OrdinalIgnoreCase), $"{size}: invalid seed never leaves a stale valid settings review");
            check(setup.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(output, $"setup-{size.X}x{size.Y}.png")) == Error.Ok, $"{size}: actual corrected-input frame retained");
            setup.Configure("small", 42, "sulan", 0, "harl", true, true, true);
            start.EmitSignal(Button.SignalName.Pressed);
            check(submitted.Count == 2 && submitted[1].RivalCount == 0 && submitted[1].RivalSpeciesId is null && submitted[1].CrisesEnabled && submitted[1].AmebasEnabled == true && submitted[1].SentriesEnabled == true, $"{size}: explicit Solo submits no rival while preserving all Standard hazards");
            host.RemoveChild(setup); setup.QueueFree();
        }
    }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is T typed) yield return typed;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
