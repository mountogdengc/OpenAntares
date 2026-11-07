using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Components;

/// <summary>Requester-only inspection. Events navigate/select; this component cannot submit orders.</summary>
public partial class SelectionSummaryPanel : VBoxContainer
{
    public event Action<StarId>? StarSelected;
    public event Action<StarId>? InspectSystemRequested;
    public event Action<VesselId>? VesselSelected;
    public event Action<VesselId>? OrdersRequested;
    public bool ShowInspectLink { get; set; } = true;

    public void ShowSelection(ContentSet content, EmpireView view, StarId? star, VesselId? vessel)
    {
        foreach (Node child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        AddChild(new HSeparator());
        AddChild(Text("Browse known systems"));
        var systems = new OptionButton
        {
            Name = "KnownSystemPicker", FitToLongestItem = false, ClipText = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        AddChild(systems);
        foreach (var known in view.Galaxy.Stars.OrderBy(s => s.Id))
        {
            systems.AddItem(known.Name);
            systems.SetItemMetadata(systems.ItemCount - 1, known.Id.Value);
            if (known.Id == star) systems.Select(systems.ItemCount - 1);
        }
        if (star is null || !view.Galaxy.Stars.Any(s => s.Id == star)) systems.Select(-1);
        systems.TooltipText = view.Galaxy.Stars.FirstOrDefault(s => s.Id == star)?.Name ?? "Choose a charted system; this does not issue orders.";
        systems.ItemSelected += index =>
        {
            if (index >= 0 && index < systems.ItemCount) StarSelected?.Invoke(new(systems.GetItemMetadata((int)index).AsInt32()));
        };
        if (ShowInspectLink)
        {
            var inspect = new Button { Name = "SelectionInspectSystem", Text = "Inspect known system", Disabled = systems.Selected < 0 };
            AddChild(inspect);
            inspect.Pressed += () => { if (star is { } id) InspectSystemRequested?.Invoke(id); };
        }
        if (!Rules.HasExpansion(view.RulesVersion)) return;

        var owned = view.Vessels.Where(v => v.StationaryStarId == star && star is not null).OrderBy(v => v.Id).ToArray();
        if (owned.Length > 0)
        {
            AddChild(Text("Owned vessels at this system"));
            var chooser = new OptionButton
            {
                Name = "SelectionVesselChooser", FitToLongestItem = false, ClipText = true,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            AddChild(chooser);
            foreach (var unit in owned)
            {
                chooser.AddItem(VesselName(content, view, unit));
                chooser.SetItemMetadata(chooser.ItemCount - 1, unit.Id.Value);
            }
            int selection = Array.FindIndex(owned, v => v.Id == vessel);
            chooser.Select(selection);
            chooser.ItemSelected += index =>
            {
                if (index >= 0 && index < chooser.ItemCount) VesselSelected?.Invoke(new(chooser.GetItemMetadata((int)index).AsInt32()));
            };
        }
        var selected = view.Vessels.FirstOrDefault(v => v.Id == vessel);
        if (selected is null)
        {
            AddChild(Text(vessel is not null ? "Selected vessel is unavailable. Choose an owned vessel." :
                owned.Length > 0 ? "Choose the owned vessel to inspect." : "No owned vessel selected at this system."));
            return;
        }
        string NameOf(StarId id) => view.Galaxy.Stars.FirstOrDefault(s => s.Id == id)?.Name ?? "an unavailable system";
        AddChild(Text("Selected vessel: " + VesselName(content, view, selected)));
        if (selected.Transit is { } transit)
            AddChild(Text($"In transit to {NameOf(transit.Destination)}: {transit.RemainingTurns} turn(s) remaining."));
        else if (selected.StationaryStarId is { } station)
            AddChild(Text("Stationed at " + NameOf(station)));
        if (selected.PendingMove is { } destination)
            AddChild(Text("Course queued: " + NameOf(destination) + ". Review the route and estimate in vessel orders."));
        if (selected.PendingFounding is not null) AddChild(Text("Colony founding queued; review in vessel orders."));
        var orders = new Button
        {
            Name = "SelectionReviewOrders", Text = "Review vessel orders",
            TooltipText = "Inspect the selected owned vessel and its existing route controls. This does not submit an order."
        };
        AddChild(orders);
        orders.Pressed += () => OrdersRequested?.Invoke(selected.Id);
    }

    private static string VesselName(ContentSet content, EmpireView view, OwnedVesselView vessel) =>
        $"{(vessel.Loadout?.Design is { } id ? ShipDesignUi.DesignName(content, view, id) : content.Vessels[vessel.RoleId].Name)} {vessel.Id.Value}";

    private static Label Text(string text) => new()
    {
        Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart,
        ThemeTypeVariation = "SubtleLabel", SizeFlagsHorizontal = SizeFlags.ExpandFill
    };
}
