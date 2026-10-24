using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Movement;
namespace OpenAntares.Game.Components;

public partial class VesselPanel : VBoxContainer
{
    public event Action<Command>? CommandIssued;
    public event Action<VesselId>? VesselSelected;
    public event Action<StarId>? DestinationSelected;
    public void ShowVessels(ContentSet content, EmpireView view, VesselId? selected, StarId? destination, PlanetId? selectedPlanet)
    {
        foreach (Node child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        Visible = Rules.HasExpansion(view.RulesVersion); if (!Visible) return;
        AddChild(new HSeparator()); AddChild(new Label { Text = "Expeditions", ThemeTypeVariation = "HeaderLabel" });
        foreach (var foreign in view.ForeignVessels) AddChild(Text($"Observed {foreign.EmpireId}: {content.Vessels[foreign.RoleId].Name} at {view.Stars.First(s => s.Chart.Id == foreign.StarId).Chart.Name}"));
        if (view.Vessels.Length == 0) { AddChild(Text("No vessels. Build a scout or settler in a colony.")); return; }
        var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, Name = "VesselPicker" }; AddChild(picker);
        foreach (var unit in view.Vessels) { picker.AddItem($"{content.Vessels[unit.RoleId].Name} {unit.Id.Value}"); picker.SetItemMetadata(picker.ItemCount - 1, unit.Id.Value); if (unit.Id == selected) picker.Select(picker.ItemCount - 1); }
        picker.ItemSelected += index => VesselSelected?.Invoke(new(picker.GetItemMetadata((int)index).AsInt32()));
        var vessel = view.Vessels.FirstOrDefault(v => v.Id == selected) ?? view.Vessels[0]; var role = content.Vessels[vessel.RoleId];
        AddChild(Text(vessel.CarriedPopulation > 0 ? $"Carried {view.PopulationUnitLabel}: {vessel.CarriedPopulation}" : "Scout: surveys a system on arrival."));
        if (vessel.Transit is { } transit)
        {
            AddChild(Text($"In transit to {view.Stars.First(s => s.Chart.Id == transit.Destination).Chart.Name}: {transit.RemainingTurns} turns remaining."));
            AddChild(Text("Committed until arrival; stopping, returning and redirection are unavailable.", "WarningLabel"));
            AddChild(new Button { Text = "Set course", Disabled = true, TooltipText = "Travel is committed until arrival." }); return;
        }
        var at = view.Stars.First(s => s.Chart.Id == vessel.StationaryStarId).Chart; AddChild(Text($"Stationed at {at.Name}"));
        var targets = view.Stars.Where(s => s.Chart.Id != at.Id).ToArray(); var target = targets.FirstOrDefault(s => s.Chart.Id == destination) ?? targets.FirstOrDefault(s => s.Chart.Id == vessel.PendingMove) ?? targets.FirstOrDefault();
        var destinations = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, Name = "DestinationPicker" }; AddChild(destinations);
        foreach (var star in targets) { destinations.AddItem(star.Chart.Name); destinations.SetItemMetadata(destinations.ItemCount - 1, star.Chart.Id.Value); if (star == target) destinations.Select(destinations.ItemCount - 1); }
        destinations.ItemSelected += index => DestinationSelected?.Invoke(new(destinations.GetItemMetadata((int)index).AsInt32()));
        if (target is not null)
        {
            var preview = TravelCalculator.Preview(at.Id, target.Chart.Id, at.X, at.Y, target.Chart.X, target.Chart.Y, role.Speed);
            AddChild(Text($"Distance rounded up: {preview.CeilingDistance}; speed {preview.Speed}; travel {preview.Turns} turn(s)."));
            AddChild(Text("Launching commits this route until arrival.", "WarningLabel"));
        }
        var move = new Button { Text = "Set course", Disabled = target is null, TooltipText = target is null ? "No other charted system." : "Travel begins on the next End Turn." }; AddChild(move); move.Pressed += () => CommandIssued?.Invoke(new SetMoveOrderCommand(view.Viewer, view.Turn, vessel.Id, target?.Chart.Id));
        if (vessel.PendingMove is { } pending) { AddChild(Text($"Pending move to {view.Stars.First(s => s.Chart.Id == pending).Chart.Name}")); var clear = new Button { Text = "Cancel move" }; clear.Pressed += () => CommandIssued?.Invoke(new SetMoveOrderCommand(view.Viewer, view.Turn, vessel.Id, null)); AddChild(clear); }
        if (role.CanFoundColony)
        {
            var preview = vessel.FoundingPreviews.FirstOrDefault(p => p.Target == selectedPlanet);
            string reason = preview is null ? "Select a surveyed planet." : BreakdownText.FoundingReason(preview.RejectionCode);
            AddChild(Text(view.RulesVersion == Rules.SpeciesVersion
                ? $"Founding transfers cargo into {vessel.CarriedPopulation} {view.PopulationUnitLabel}, all in support. Output begins in the following economic snapshot."
                : $"Founding transfers the cargo into population {vessel.CarriedPopulation}, all in support. Output begins in the following economic snapshot."));
            if (preview is { RejectionCode: null })
            {
                AddChild(Text($"Founded habitat: {preview.HabitatMode}; base capacity {preview.Capacity}."));
                if (preview.Economy is { } economy) AddChild(Text($"First economic snapshot: support {Format.Points(economy.SupportProduced.Total)} / {Format.Points(economy.SupportRequired.Total)}, growth +{Format.Points(economy.GrowthEarned.Total)}."));
            }
            if (reason.Length > 0) AddChild(Text(reason));
            var found = new Button { Text = "Found colony", Disabled = reason.Length > 0, TooltipText = reason.Length > 0 ? reason : "Queues founding for the next End Turn." }; AddChild(found); found.Pressed += () => CommandIssued?.Invoke(new SetFoundingOrderCommand(view.Viewer, view.Turn, vessel.Id, selectedPlanet));
        }
        if (vessel.PendingFounding.HasValue) { AddChild(Text("Founding queued for next End Turn.")); var clear = new Button { Text = "Cancel founding" }; clear.Pressed += () => CommandIssued?.Invoke(new SetFoundingOrderCommand(view.Viewer, view.Turn, vessel.Id, null)); AddChild(clear); }
    }
    private static Label Text(string text, string style = "SubtleLabel") => new() { Text = text, ThemeTypeVariation = style, AutowrapMode = TextServer.AutowrapMode.WordSmart };
}
