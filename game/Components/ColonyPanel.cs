using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Components;

/// <summary>
/// Manage one colony: population and growth, workforce allocation with the resulting outputs, and
/// the construction project. Every number comes from the simulation forecast, with its breakdown in
/// a tooltip. Changes are sent as commands; the panel never edits state itself.
/// </summary>
public partial class ColonyPanel : VBoxContainer
{
    private OptionButton? _projectPicker;
    private OpenAntares.Game.Presentation.PlanetStyleCatalog? _styles;
    private OpenAntares.Game.Presentation.PlanetPortraitCache? _cache;
    public void ConfigurePortraits(OpenAntares.Game.Presentation.PlanetStyleCatalog styles,OpenAntares.Game.Presentation.PlanetPortraitCache cache){_styles=styles;_cache=cache;}

    /// <summary>Raised when the player changes something. The screen executes it and refreshes.</summary>
    public event Action<Command>? CommandIssued;

    public void ShowColony(ContentSet content, OwnedColonyView colony, EmpireView? view = null)
    {
        Clear();
        ColonyEconomy economy = colony.Economy;

        AddChild(new HSeparator());
        AddChild(new Label { Text = "Colony", ThemeTypeVariation = "HeaderLabel" });

        if (view is not null) { var orders = new ColonyOrdersPanel(); AddChild(orders); orders.CommandIssued += c => CommandIssued?.Invoke(c); orders.ShowColony(content, view, colony); }

        if (view is not null && Rules.HasDevelopment(view.RulesVersion)) { var known=view.Stars.FirstOrDefault(s=>s.Chart.Id==colony.StarId)?.Planets?.FirstOrDefault(p=>p.Id==colony.PlanetId);var portrait=new PlanetPortrait();AddChild(portrait);if(_styles is not null&&_cache is not null)portrait.Configure(_styles,_cache);portrait.ShowPlanet(known); }

        // Population and growth.
        string unit = colony.PopulationUnitLabel;
        string title = char.ToUpperInvariant(unit[0]) + unit[1..];
        AddChild(new Label { Text = $"{title} {colony.Population} of {economy.Growth.Capacity}" });
        if (colony.SpeciesId is { } speciesId)
        {
            var species = content.Species[speciesId];
            AddChild(Wrapped($"{species.Name} — {species.CultureName}; {colony.HabitatMode} habitat.", "SubtleLabel"));
        }
        AddChild(Readout(
            "Growth",
            $"{BreakdownText.Signed(economy.GrowthEarned.Total)} per turn, progress {Format.Points(colony.GrowthProgress)} / 1",
            BreakdownText.Describe(economy.GrowthEarned, content, "Growth this turn")));

        // Workforce. Support is the remainder; production and research move workers to and from it.
        AddChild(new Label { Text = "Workforce", ThemeTypeVariation = "SubtleLabel" });
        var grid = new GridContainer { Columns = 5 };
        AddChild(grid);
        Workforce workforce = colony.Workforce;

        AddWorkforceRow(grid, "Support", workforce.Support, null, null,
            $"{Format.Points(economy.SupportProduced.Total)} of {Format.Points(economy.SupportRequired.Total)} needed",
            BreakdownText.Describe(economy.SupportProduced, content, "Support produced")
                + "\n\n" + BreakdownText.Describe(economy.SupportRequired, content, "Support needed"));

        AddWorkforceRow(grid, "Production", workforce.Production,
            workforce.Production > 0 ? workforce with { Production = workforce.Production - 1, Support = workforce.Support + 1 } : null,
            workforce.Support > 0 ? workforce with { Production = workforce.Production + 1, Support = workforce.Support - 1 } : null,
            $"{Format.Points(economy.NetProduction.Total)} per turn",
            BreakdownText.Describe(economy.NetProduction, content, "Production per turn"),
            colony, colony.Turn);

        AddWorkforceRow(grid, "Research", workforce.Research,
            workforce.Research > 0 ? workforce with { Research = workforce.Research - 1, Support = workforce.Support + 1 } : null,
            workforce.Support > 0 ? workforce with { Research = workforce.Research + 1, Support = workforce.Support - 1 } : null,
            $"{Format.Points(economy.NetResearch.Total)} per turn",
            BreakdownText.Describe(economy.NetResearch, content, "Research per turn"),
            colony, colony.Turn);

        if (economy.SupportDeficit > 0)
        {
            AddChild(Wrapped(
                $"Support shortage of {Format.Points(economy.SupportDeficit)}: production and research are reduced and growth is paused.",
                "WarningLabel"));
        }
        else if (economy.GrowthEarned.Lines.Any(line => line.Kind == BreakdownKinds.CapacityLimit))
        {
            AddChild(Wrapped("At capacity: growth is blocked. The growth breakdown shows potential growth before this limit.", "SubtleLabel"));
        }
        else if (economy.UnusedSupport > 0)
        {
            AddChild(Wrapped(
                $"{Format.Points(economy.UnusedSupport)} support is unused: growth is already at its bonus limit.",
                "SubtleLabel"));
        }

        // Construction.
        AddChild(new HSeparator());
        AddChild(new Label { Text = "Construction", ThemeTypeVariation = "HeaderLabel" });
        _projectPicker = ProjectPicker(content, colony, colony.Turn);
        AddChild(_projectPicker);

        ReserveOutcome production = economy.Production;
        string status = production.TargetId is null
            ? $"No project: production is saved (reserve {Format.Points(production.Starting)})."
            : production.BlockedReasonCode is not null ? $"{BreakdownText.Progress(production)}; banking until embarkation is possible" : $"{BreakdownText.Progress(production)}, {BreakdownText.Estimate(production.Estimate)}";
        AddChild(Wrapped(status, "SubtleLabel"));
        if (colony.ProjectKind == ProductionKinds.Vessel && colony.ProjectId is { } roleId && content.Vessels[roleId].CanFoundColony)
        {
            var role = content.Vessels[roleId];
            string full = Rules.HasSpecies(colony.RulesVersion) && role.RequiresFullColony ? $" Requires a full colony at current capacity {economy.Growth.Capacity}." : string.Empty;
            AddChild(Wrapped($"{role.Name}: requires {unit} {role.MinimumSourcePopulation} and {role.EmbarkPopulation} support workers; transfers {role.EmbarkPopulation} {unit} on completion. Delays construction of a building." + full, "SubtleLabel"));
        }
        if (production.BlockedReasonCode is not null && colony.ProjectId is { } blockedRoleId)
        {
            var role = content.Vessels[blockedRoleId];
            if (Rules.HasSpecies(colony.RulesVersion))
            {
                AddChild(Wrapped("Awaiting embarkation. Production is retained.", "WarningLabel"));
                foreach (var requirement in production.BlockedRequirements) AddChild(Wrapped(BreakdownText.EmbarkRequirement(requirement, unit), "WarningLabel"));
            }
            else AddChild(Wrapped($"Awaiting embarkation: requires population {role.MinimumSourcePopulation} and {role.EmbarkPopulation} support workers. Production is retained.", "WarningLabel"));
        }
        if (economy.EmbarkOutcome is { } embark)
        {
            AddChild(Wrapped($"After embarkation: {unit} {embark.PopulationAfter}, support {embark.WorkforceAfter.Support}, production {embark.WorkforceAfter.Production}, research {embark.WorkforceAfter.Research}. Next support {Format.Points(embark.NextSupportProduced.Total)} / {Format.Points(embark.NextSupportRequired.Total)}.", embark.ReassignmentNeeded ? "WarningLabel" : "SubtleLabel"));
            if (embark.ReassignmentNeeded) AddChild(Wrapped("Reassign workers after completion to avoid a support shortage.", "WarningLabel"));
        }

        if (colony.CompletedBuildingIds.Length > 0)
        {
            AddChild(Wrapped("Built: " + string.Join(", ", BreakdownText.Names(colony.CompletedBuildingIds, content)), "SubtleLabel"));
        }
    }

    /// <summary>Opens the construction list, e.g. after a building completes.</summary>
    public async void OpenProjectPicker()
    {
        OptionButton? picker = _projectPicker;
        if (picker is null)
        {
            return;
        }

        picker.GrabFocus();
        // FollowFocus scrolls the panel during layout. Position the popup only after
        // that scroll finishes, otherwise it stays at the control's old position.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (GodotObject.IsInstanceValid(picker) && picker == _projectPicker && picker.IsVisibleInTree())
        {
            picker.ShowPopup();
        }
    }

    public void Clear()
    {
        _projectPicker = null;
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
    }

    private void AddWorkforceRow(
        GridContainer grid, string name, int count, Workforce? fewer, Workforce? more, string output, string tooltip,
        OwnedColonyView? colony = null, int turn = 0)
    {
        grid.AddChild(new Label { Text = name });
        grid.AddChild(colony is null ? new Control() : StepButton("−", fewer, colony, turn, $"Move a worker from {name.ToLowerInvariant()} to support"));
        grid.AddChild(new Label { Text = count.ToString(System.Globalization.CultureInfo.InvariantCulture), HorizontalAlignment = HorizontalAlignment.Center, CustomMinimumSize = new Vector2(32, 0) });
        grid.AddChild(colony is null ? new Control() : StepButton("+", more, colony, turn, $"Move a worker from support to {name.ToLowerInvariant()}"));
        grid.AddChild(new Label
        {
            Text = output,
            TooltipText = tooltip,
            MouseFilter = MouseFilterEnum.Stop,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
    }

    private Button StepButton(string text, Workforce? target, OwnedColonyView colony, int turn, string tooltip)
    {
        var button = new Button { Text = text, Disabled = target is null, TooltipText = tooltip };
        if (target is { } workforce)
        {
            button.Pressed += () => CommandIssued?.Invoke(new SetWorkforceCommand(colony.EmpireId, turn, colony.Id, workforce));
        }

        return button;
    }

    /// <summary>Lists buildable projects, then locked ones (disabled) with what they need.</summary>
    private OptionButton ProjectPicker(ContentSet content, OwnedColonyView colony, int turn)
    {
        var picker = new OptionButton { Name = "ColonyProjectPicker", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        picker.AddItem("No project");
        picker.SetItemMetadata(0, string.Empty);

        var remaining = content.Buildings.Values.Where(b => !colony.CompletedBuildingIds.Contains(b.Id)).ToList();
        foreach (BuildingDefinition building in remaining.Where(b => b.PrerequisiteTechnologyIds.All(id => colony.KnownTechnologyIds.Contains(id))))
        {
            picker.AddItem($"{building.Name} ({Format.Points(building.Cost)})");
            picker.SetItemMetadata(picker.ItemCount - 1, building.Id);
            if (building.Id == colony.ProjectId && colony.ProjectKind == ProductionKinds.Building)
            {
                picker.Select(picker.ItemCount - 1);
            }
        }

        if (Rules.HasExpansion(colony.RulesVersion))
        {
            foreach (var role in content.Vessels.Values.Where(r => colony.AvailableVesselRoleIds.Contains(r.Id)))
            {
                picker.AddItem($"{role.Name} ({Format.Points(role.Cost)})"); picker.SetItemMetadata(picker.ItemCount - 1, "vessel|" + role.Id);
                picker.SetItemTooltip(picker.ItemCount - 1, role.CanFoundColony ? $"Requires {colony.PopulationUnitLabel} {role.MinimumSourcePopulation} and {role.EmbarkPopulation} support workers. Transfers those existing workers into cargo." +
                    (Rules.HasSpecies(colony.RulesVersion) && role.RequiresFullColony ? $" Requires a full colony at current capacity {colony.Economy.Growth.Capacity}." : string.Empty) +
                    (Rules.HasSpecies(colony.RulesVersion) ? role.FoundsSealedHabitat ? " Founds a sealed habitat." : " Founds a native habitat." : string.Empty) : $"Speed {role.Speed} map units per turn.");
                if (colony.ProjectKind == ProductionKinds.Vessel && colony.ProjectId == role.Id) picker.Select(picker.ItemCount - 1);
            }
        }

        foreach (BuildingDefinition building in remaining.Where(b => !b.PrerequisiteTechnologyIds.All(id => colony.KnownTechnologyIds.Contains(id))))
        {
            string needs = string.Join(", ", building.PrerequisiteTechnologyIds
                .Where(id => !colony.KnownTechnologyIds.Contains(id))
                .Select(id => content.Technologies[id].Name));
            picker.AddItem($"{building.Name}: needs {needs}");
            picker.SetItemMetadata(picker.ItemCount - 1, building.Id);
            picker.SetItemDisabled(picker.ItemCount - 1, true);
        }

        picker.ItemSelected += index =>
        {
            string id = picker.GetItemMetadata((int)index).AsString();
            CommandIssued?.Invoke(new SetProductionTargetCommand(colony.EmpireId, turn, colony.Id, id.Length == 0 ? null : id.StartsWith("vessel|", StringComparison.Ordinal) ? new(ProductionKinds.Vessel, id[7..]) : new(ProductionKinds.Building, id)));
        };
        return picker;
    }

    private static HBoxContainer Readout(string name, string value, string tooltip)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = name });
        row.AddChild(new Label
        {
            Text = value,
            TooltipText = tooltip,
            MouseFilter = MouseFilterEnum.Stop,
            ThemeTypeVariation = "SubtleLabel",
        });
        return row;
    }

    private static Label Wrapped(string text, string variation) => new()
    {
        Text = text,
        ThemeTypeVariation = variation,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };
}
