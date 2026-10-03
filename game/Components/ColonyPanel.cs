using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Components;

/// <summary>
/// Manage one colony: population and growth, workforce allocation with the resulting outputs, and
/// the construction project. Every number comes from the simulation forecast, with its breakdown in
/// a tooltip. Changes are sent as commands; the panel never edits state itself.
/// </summary>
public partial class ColonyPanel : VBoxContainer
{
    /// <summary>Raised when the player changes something. The screen executes it and refreshes.</summary>
    public event Action<Command>? CommandIssued;

    public void ShowColony(ContentSet content, GameState state, ColonyId colonyId, TurnEconomy forecast)
    {
        Clear();
        ColonyState colony = state.FindColony(colonyId)!;
        ColonyEconomy economy = forecast.Colony(colonyId);
        EmpireState empire = state.FindEmpire(colony.EmpireId)!;

        AddChild(new HSeparator());
        AddChild(new Label { Text = "Colony", ThemeTypeVariation = "HeaderLabel" });

        // Population and growth.
        AddChild(new Label { Text = $"Population {colony.Population} of {economy.Growth.Capacity}" });
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
            colony, state.Turn);

        AddWorkforceRow(grid, "Research", workforce.Research,
            workforce.Research > 0 ? workforce with { Research = workforce.Research - 1, Support = workforce.Support + 1 } : null,
            workforce.Support > 0 ? workforce with { Research = workforce.Research + 1, Support = workforce.Support - 1 } : null,
            $"{Format.Points(economy.NetResearch.Total)} per turn",
            BreakdownText.Describe(economy.NetResearch, content, "Research per turn"),
            colony, state.Turn);

        if (economy.SupportDeficit > 0)
        {
            AddChild(Wrapped(
                $"Support shortage of {Format.Points(economy.SupportDeficit)}: production and research are reduced and growth is paused.",
                "WarningLabel"));
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
        AddChild(ProjectPicker(content, colony, empire, state.Turn));

        ReserveOutcome production = economy.Production;
        string status = production.TargetId is null
            ? $"No project: production is saved (reserve {Format.Points(production.Starting)})."
            : $"{BreakdownText.Progress(production)}, {BreakdownText.Estimate(production.Estimate)}";
        AddChild(Wrapped(status, "SubtleLabel"));

        if (colony.CompletedBuildingIds.Count > 0)
        {
            AddChild(Wrapped("Built: " + string.Join(", ", BreakdownText.Names(colony.CompletedBuildingIds, content)), "SubtleLabel"));
        }
    }

    public void Clear()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
    }

    private void AddWorkforceRow(
        GridContainer grid, string name, int count, Workforce? fewer, Workforce? more, string output, string tooltip,
        ColonyState? colony = null, int turn = 0)
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

    private Button StepButton(string text, Workforce? target, ColonyState colony, int turn, string tooltip)
    {
        var button = new Button { Text = text, Disabled = target is null, TooltipText = tooltip };
        if (target is { } workforce)
        {
            button.Pressed += () => CommandIssued?.Invoke(new SetWorkforceCommand(colony.EmpireId, turn, colony.Id, workforce));
        }

        return button;
    }

    /// <summary>Lists buildable projects, then locked ones (disabled) with what they need.</summary>
    private OptionButton ProjectPicker(ContentSet content, ColonyState colony, EmpireState empire, int turn)
    {
        var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        picker.AddItem("No project");
        picker.SetItemMetadata(0, string.Empty);

        var remaining = content.Buildings.Values.Where(b => !colony.CompletedBuildingIds.Contains(b.Id)).ToList();
        foreach (BuildingDefinition building in remaining.Where(b => Rules.PrerequisitesKnown(empire, b.PrerequisiteTechnologyIds)))
        {
            picker.AddItem($"{building.Name} ({Format.Points(building.Cost)})");
            picker.SetItemMetadata(picker.ItemCount - 1, building.Id);
            if (building.Id == colony.ProjectId)
            {
                picker.Select(picker.ItemCount - 1);
            }
        }

        foreach (BuildingDefinition building in remaining.Where(b => !Rules.PrerequisitesKnown(empire, b.PrerequisiteTechnologyIds)))
        {
            string needs = string.Join(", ", building.PrerequisiteTechnologyIds
                .Where(id => !empire.KnownTechnologyIds.Contains(id))
                .Select(id => content.Technologies[id].Name));
            picker.AddItem($"{building.Name}: needs {needs}");
            picker.SetItemMetadata(picker.ItemCount - 1, building.Id);
            picker.SetItemDisabled(picker.ItemCount - 1, true);
        }

        picker.ItemSelected += index =>
        {
            string id = picker.GetItemMetadata((int)index).AsString();
            CommandIssued?.Invoke(new SetProjectCommand(colony.EmpireId, turn, colony.Id, id.Length == 0 ? null : id));
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
