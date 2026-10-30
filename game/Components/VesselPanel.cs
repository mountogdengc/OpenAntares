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
    public event Action? ScoutOverviewRequested;
    public event Action? FleetCommandRequested;
    public event Action<VesselId>? RefitRequested;
    public void ShowVessels(ContentSet content, EmpireView view, VesselId? selected, StarId? destination, PlanetId? selectedPlanet)
    {
        foreach (Node child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        Visible = Rules.HasExpansion(view.RulesVersion); if (!Visible) return;
        if(Rules.HasFleets(view.RulesVersion)){var fleets=new Button{Name="InspectFleetCommand",Text="Fleet command"};AddChild(fleets);fleets.Pressed+=()=>FleetCommandRequested?.Invoke();}
        AddChild(new HSeparator()); AddChild(new Label { Text = "Expeditions", ThemeTypeVariation = "HeaderLabel" });
        foreach (var foreign in view.ForeignVessels) AddChild(Text($"Observed {view.Contacts.FirstOrDefault(c=>c.OtherEmpireId==foreign.EmpireId)?.CultureName ?? foreign.EmpireId.ToString()}: {content.Vessels[foreign.RoleId].Name} at {view.Stars.First(s => s.Chart.Id == foreign.StarId).Chart.Name} (turn {view.Turn})"));
        if (view.Vessels.Length == 0) { AddChild(Text("No vessels. Build a scout or settler in a colony.")); return; }
        var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, Name = "VesselPicker" }; AddChild(picker);
        foreach (var unit in view.Vessels) { picker.AddItem($"{(unit.Loadout?.Design is {} id?ShipDesignUi.DesignName(content,view,id):content.Vessels[unit.RoleId].Name)} {unit.Id.Value}"); picker.SetItemMetadata(picker.ItemCount - 1, unit.Id.Value); if (unit.Id == selected) picker.Select(picker.ItemCount - 1); }
        picker.ItemSelected += index => VesselSelected?.Invoke(new(picker.GetItemMetadata((int)index).AsInt32()));
        var vessel = view.Vessels.FirstOrDefault(v => v.Id == selected) ?? view.Vessels[0]; var role = content.Vessels[vessel.RoleId];
        if(Rules.HasShipDesigns(view.RulesVersion)&&vessel.Loadout?.Design is not null){var refit=new Button{Name="VesselOpenRefit",Text="Review equipment refit"};AddChild(refit);refit.Pressed+=()=>RefitRequested?.Invoke(vessel.Id);}
        if(view.Combat?.Ships.FirstOrDefault(v=>v.Id==vessel.Id) is {} combat)AddChild(Text($"{CombatReportPanel.Status(combat)} — hull {combat.Hull}/{combat.MaxHull}, cohesion {combat.Cohesion}, {combat.DamageBand}; attack {combat.Firepower}, armor {combat.Armor}, effective speed {combat.EffectiveSpeed}."));
        AddChild(Text(view.Combat is not null&&content.Combat!.RoleProfiles[vessel.RoleId].Attack>0?"Warship: combat requires a separate named-fleet attack order.":vessel.CarriedPopulation > 0 ? $"Carried {view.PopulationUnitLabel}: {vessel.CarriedPopulation}" : "Scout: surveys a system on arrival."));
        var scouting=view.Scouting?.Vessels.FirstOrDefault(v=>v.Id==vessel.Id);
        if(scouting is {Posture:{} posture})
        {
            AddChild(Text("Withdraw from ameba: enabled. Neutral contact posture is separate."));var posturePicker=new OptionButton{Name="ScoutPosturePicker",SizeFlagsHorizontal=SizeFlags.ExpandFill};posturePicker.AddItem("Observe (neutral default)");posturePicker.AddItem("Avoid encounters");posturePicker.Select((int)posture);AddChild(posturePicker);posturePicker.ItemSelected+=index=>CommandIssued?.Invoke(new SetScoutPostureCommand(view.Viewer,view.Turn,vessel.Id,(OpenAntares.Simulation.Scouting.ScoutPosture)index));
        }
        if(scouting is {Stage:{} stage}){AddChild(Text($"Ameba {stage}: encounter {scouting.EncounterNumber}, revision {scouting.Revision}. {(stage==OpenAntares.Simulation.Scouting.AmebaStage.Warning?"Departure this planning turn is 100% safe.":$"Late escape survival {scouting.LateEscapePercent}%.")}","WarningLabel"));var inspect=new Button{Name="InspectScoutEncounter",Text="Review ameba responses"};AddChild(inspect);inspect.Pressed+=()=>ScoutOverviewRequested?.Invoke();}
        else if(scouting?.Blocker is {} wait)AddChild(Text(wait,"WarningLabel"));
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
            int speed=role.Speed;if(view.Combat is {} combatView){speed=combatView.Ships.Single(s=>s.Id==vessel.Id).EffectiveSpeed;var fleet=view.Fleets!.Ships.Single(s=>s.Id==vessel.Id).Fleet;if(fleet is {} f)speed=view.Fleets.Fleets.Single(g=>g.Id==f).Members.Min(id=>combatView.Ships.Single(s=>s.Id==id).EffectiveSpeed);}
            var preview = TravelCalculator.Preview(at.Id, target.Chart.Id, at.X, at.Y, target.Chart.X, target.Chart.Y, speed);
            AddChild(Text($"Distance rounded up: {preview.CeilingDistance}; speed {preview.Speed}; travel {preview.Turns} turn(s)."));
            AddChild(Text("Launching commits this route until arrival.", "WarningLabel"));
        }
        var knownAmeba=view.Scouting?.Habitats.FirstOrDefault(h=>h.Star==target?.Chart.Id);
        bool neutralExcluded=target is not null&&(target.Planets?.Any(p=>p.OwnerId is {} owner&&owner!=view.Viewer)==true||view.ForeignVessels.Any(v=>v.StarId==target.Chart.Id));
        bool excluded=scouting?.Posture==OpenAntares.Simulation.Scouting.ScoutPosture.AvoidEncounters&&(knownAmeba is not null||neutralExcluded)||knownAmeba is not null&&scouting?.Posture is null;
        if(view.Scouting is not null&&target is not null)AddChild(Text(knownAmeba is not null?$"Known permanent ameba habitat, observed turn {knownAmeba.ObservedTurn}.":neutralExcluded?$"Foreign presence recorded; survey turn {target.LastObservedTurn?.ToString()??"current ship sighting"}.":"Hazard status unknown; no foreign presence recorded. An old or missing survey does not guarantee safety."));
        var move = new Button { Name="SetVesselCourse",Text = knownAmeba is null?"Set course":"Review ameba approach…", Disabled = target is null||excluded, TooltipText = target is null ? "No other charted system." : excluded?"Standing posture or population-expedition safety excludes this known destination.":"Travel begins on the next End Turn." }; AddChild(move); move.Pressed += () =>
        {
            if(knownAmeba is null){CommandIssued?.Invoke(new SetMoveOrderCommand(view.Viewer,view.Turn,vessel.Id,target?.Chart.Id));return;}
            var confirmation=new ConfirmationDialog{Name="AmebaApproachConfirmation",Title="Approach known space ameba",DialogText="This permanent habitat is dangerous. This exact unstarted route is accepted for this Observe scout only. Arrival provides one safe departure opportunity; remaining or studying risks this paid vessel. Ordinary travel still applies.",OkButtonText="Accept this exact approach"};AddChild(confirmation);confirmation.Confirmed+=()=>CommandIssued?.Invoke(new SetAmebaApproachOrderCommand(view.Viewer,view.Turn,vessel.Id,knownAmeba.Star,knownAmeba.HabitatId));confirmation.PopupCenteredClamped(new(620,380));
        };
        if (vessel.PendingMove is { } pending) { AddChild(Text($"Pending move to {view.Stars.First(s => s.Chart.Id == pending).Chart.Name}")); var clear = new Button { Text = "Cancel move" }; clear.Pressed += () => CommandIssued?.Invoke(new SetMoveOrderCommand(view.Viewer, view.Turn, vessel.Id, null)); AddChild(clear); }
        if (role.CanFoundColony)
        {
            var preview = vessel.FoundingPreviews.FirstOrDefault(p => p.Target == selectedPlanet);
            string reason = preview is null ? "Select a surveyed planet." : BreakdownText.FoundingReason(preview.RejectionCode);
            AddChild(Text(Rules.HasSpecies(view.RulesVersion)
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
