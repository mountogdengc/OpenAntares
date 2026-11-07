using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;

namespace OpenAntares.Game.Validation;

internal static class PlayabilitySelectionChecks
{
    public static async Task Run(Node host, GameSession game, string output, Action<bool, string> check)
    {
        string before = SaveGame.Serialize(game.Content, game.State);
        var view = game.View(game.State.Empires.First(e => e.Controller == OpenAntares.Simulation.State.ControllerKind.Human).Id);
        Type? type = typeof(VesselPanel).Assembly.GetType("OpenAntares.Game.Components.SelectionSummaryPanel");
        check(type is not null, "SelectionAndPreviewNeverIssueCommands: contextual selection component exists");
        var card = (Control)Activator.CreateInstance(type!)!;
        host.AddChild(card); card.Position = new(20, 20); card.Size = new(420, 400);
        var method = type!.GetMethod("ShowSelection", new[] { typeof(ContentSet), typeof(EmpireView), typeof(StarId?), typeof(VesselId?) })!;
        var starting = view.Vessels.First();
        var synthetic = starting with { Id = new VesselId(starting.Id.Value + 10000) };
        var projectionSupplement = view with { Vessels = view.Vessels.Add(synthetic) };
        var chosen = new List<VesselId>();
        var inspected = new List<StarId>();
        type.GetEvent("VesselSelected")!.AddEventHandler(card, (Action<VesselId>)(id => chosen.Add(id)));
        type.GetEvent("InspectSystemRequested")!.AddEventHandler(card, (Action<StarId>)(id => inspected.Add(id)));
        method.Invoke(card, new object?[] { game.Content, projectionSupplement, starting.StationaryStarId, null });
        for (int i = 0; i < 3; i++) await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
        var picker = Descendants<OptionButton>(card).Single(n => n.Name == "SelectionVesselChooser");
        check(picker.ItemCount == 2 && picker.Selected == -1, "MultipleVesselsRequireExplicitStableSelection: no arbitrary vessel is selected");
        var ids = Enumerable.Range(0, picker.ItemCount).Select(i => picker.GetItemMetadata(i).AsInt32()).ToArray();
        check(ids.SequenceEqual(ids.Order()), "The chooser is ordered by stable owned vessel ID");
        picker.Select(1); picker.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
        check(chosen.SequenceEqual(new[] { new VesselId(ids[1]) }), "One explicit selection emits the exact owned vessel once");
        method.Invoke(card, new object?[] { game.Content, projectionSupplement, starting.StationaryStarId, synthetic.Id });
        var inspect = Descendants<Button>(card).Single(b => b.Name == "SelectionInspectSystem");
        inspect.EmitSignal(Button.SignalName.Pressed);
        check(inspected.SequenceEqual(new[] { starting.StationaryStarId!.Value }), "Inspect opens the actual selected system once");
        method.Invoke(card, new object?[] { game.Content, view, starting.StationaryStarId, new VesselId(int.MaxValue) });
        string text = string.Join("\n", Descendants<Label>(card).Select(l => l.Text));
        check(text.Contains("unavailable", StringComparison.OrdinalIgnoreCase), "Lost/foreign selection is explained rather than falling through to another vessel");
        check(before == SaveGame.Serialize(game.Content, game.State), "All selection, chooser and inspection signals preserve campaign state and PRNG");
        card.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(output, "selection-projection-supplement.png"));
        host.RemoveChild(card); card.QueueFree();
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
