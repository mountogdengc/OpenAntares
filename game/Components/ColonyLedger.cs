using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Components;

public partial class ColonyLedger : VBoxContainer
{
    public event Action<Command>? CommandIssued;
    public event Action<ColonyId>? ColonySelected;
    private EmpireView? _view;
    public void ShowColonies(ContentSet content, EmpireView view)
    {
        // Keep focused worker controls stable until the published planning view changes.
        if (ReferenceEquals(_view, view)) return;
        _view = view;
        foreach (Node child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        AddChild(new Label { Text = "COLONY WORKFORCE", ThemeTypeVariation = "ConsoleHeading" });
        AddChild(new Label { Text = "Drag a worker to another role. Or focus a worker and press Left / Right to move one worker. Select a colony name for construction and detailed forecasts.", AutowrapMode = TextServer.AutowrapMode.WordSmart, ThemeTypeVariation = "SubtleLabel" });
        foreach (var colony in view.Colonies.OrderBy(c => c.Id))
        {
            var frame = new PanelContainer { ThemeTypeVariation = "ScreenInset" };
            AddChild(frame);
            var row = new VBoxContainer(); frame.AddChild(row);
            var title = new Button { Name = "LedgerColony" + colony.Id.Value, Text = view.Research.ColonyNames.GetValueOrDefault(colony.Id.ToString(), "Colony " + colony.Id.Value), ThemeTypeVariation = "TabKey" };
            title.Pressed += () => ColonySelected?.Invoke(colony.Id); row.AddChild(title);
            var roles = new HBoxContainer(); row.AddChild(roles);
            int[] counts = { colony.Workforce.Support, colony.Workforce.Production, colony.Workforce.Research };
            string[] names = { "Support", "Production", "Research" };
            string[] icons = { "worker_support", "worker_production", "worker_research" };
            for (int role = 0; role < 3; role++)
            {
                int target = role;
                var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                roles.AddChild(column);
                column.AddChild(new Label { Text = names[role] + "  " + counts[role], ThemeTypeVariation = role == 1 ? "ResourceProduction" : role == 2 ? "ResourceResearch" : "ResourcePopulation" });
                var figures = new WorkerFigureRow { Colony = colony.Id.Value, Turn = view.Turn, Role = role, SizeFlagsHorizontal = SizeFlags.ExpandFill };
                column.AddChild(figures);
                figures.MoveRequested += (source, destination) => Move(colony, source, destination, view.Turn);
                // Empty roles remain a real drop target, including when their count is zero.
                var empty = new Button { Text = counts[role] == 0 ? "Drop here" : "← drop", ThemeTypeVariation = "CompactCommandKey", Disabled = true, MouseFilter = MouseFilterEnum.Ignore };
                figures.AddChild(empty);
                for (int i = 0; i < counts[role]; i++)
                {
                    var worker = new WorkerFigure { Colony = colony.Id.Value, Turn = view.Turn, Role = target, Name = $"Worker{colony.Id.Value}_{role}_{i}", Icon = GetThemeIcon(icons[role], "Icons"), ExpandIcon = true, ThemeTypeVariation = "IconKey", TooltipText = names[role] + " worker. Drag to a role, or Left / Right to reassign one." };
                    worker.ThemeTypeVariation = "WorkerKey";
                    int figureSize = GetThemeConstant(GetViewportRect().Size.X < 1100 ? "compact_worker_size" : "worker_size", "ConsoleMetrics");
                    worker.CustomMinimumSize = new Vector2(figureSize, figureSize);
                    worker.MoveRequested += destination => Move(colony, target, destination, view.Turn);
                    figures.AddChild(worker);
                }
            }
            var economy = colony.Economy;
            string project = colony.ProjectId is null ? "Saving production" : colony.ProjectKind == "building" && content.Buildings.TryGetValue(colony.ProjectId, out var building) ? building.Name : ShipDesignUi.Name(content,view,new ProductionTarget(colony.ProjectKind,colony.ProjectId));
            row.AddChild(new Label { Text = $"{Format.Points(economy.NetProduction.Total)} production / turn  ·  {Format.Points(economy.NetResearch.Total)} research / turn  ·  {colony.Population} {colony.PopulationUnitLabel}", AutowrapMode = TextServer.AutowrapMode.WordSmart, ThemeTypeVariation = "LedgerReadout" });
            row.AddChild(new Label { Text = project + "  ·  " + BreakdownText.Progress(economy.Production) + "  ·  " + BreakdownText.Estimate(economy.Production.Estimate), AutowrapMode = TextServer.AutowrapMode.WordSmart, ThemeTypeVariation = "SubtleLabel" });
            if (economy.Production.TargetCost is > 0)
                row.AddChild(new ProgressBar { Value = Math.Clamp(100.0 * colony.ProductionReserve / economy.Production.TargetCost.Value, 0, 100), ShowPercentage = true });
        }
    }
    private void Move(OwnedColonyView colony, int source, int destination, int turn)
    {
        if (_view is null || turn != _view.Turn || source == destination || destination is < 0 or > 2) return;
        int[] workers = { colony.Workforce.Support, colony.Workforce.Production, colony.Workforce.Research };
        if (workers[source] == 0) return;
        workers[source]--; workers[destination]++;
        CommandIssued?.Invoke(new SetWorkforceCommand(colony.EmpireId, turn, colony.Id, new Workforce(workers[0], workers[1], workers[2])));
    }
}

public partial class WorkerFigureRow : HFlowContainer
{
    public int Colony { get; set; }
    public int Turn { get; set; }
    public int Role { get; set; }
    public event Action<int, int>? MoveRequested;
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary) return false;
        var values = data.AsGodotDictionary();
        return values.TryGetValue("colony", out var colony) && values.TryGetValue("turn", out var turn) && values.TryGetValue("role", out var role)
            && colony.VariantType == Variant.Type.Int && turn.VariantType == Variant.Type.Int && role.VariantType == Variant.Type.Int
            && colony.AsInt32() == Colony && turn.AsInt32() == Turn && role.AsInt32() is >= 0 and <= 2 && role.AsInt32() != Role;
    }
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (_CanDropData(atPosition, data)) MoveRequested?.Invoke(data.AsGodotDictionary()["role"].AsInt32(), Role);
    }
}

public partial class WorkerFigure : Button
{
    public int Colony { get; set; }
    public int Turn { get; set; }
    public int Role { get; set; }
    public event Action<int>? MoveRequested;
    public override Variant _GetDragData(Vector2 atPosition)
    {
        SetDragPreview(new TextureRect { Texture = Icon });
        return new Godot.Collections.Dictionary { ["colony"] = Colony, ["turn"] = Turn, ["role"] = Role };
    }
    public override void _GuiInput(InputEvent input)
    {
        if (input is InputEventKey { Pressed: true, Echo: false } key && key.Keycode is Key.Left or Key.Right)
        {
            MoveRequested?.Invoke((Role + (key.Keycode == Key.Left ? 2 : 1)) % 3);
            AcceptEvent();
        }
    }
}
