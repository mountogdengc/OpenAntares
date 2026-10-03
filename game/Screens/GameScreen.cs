using System.Linq;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Screens;

/// <summary>
/// The main in-game screen: a top bar with the turn and research, the galaxy map, and a side panel
/// for the selected star and, when it holds one of the viewer's colonies, that colony.
/// </summary>
public partial class GameScreen : VBoxContainer
{
    private readonly GameSession _session;
    private readonly EmpireId _viewer;
    private readonly Label _turnLabel = new();
    private readonly ResearchBar _researchBar = new();
    private readonly Label _message = new() { ThemeTypeVariation = "WarningLabel" };
    private readonly GalaxyMap _map = new();
    private readonly StarPanel _starPanel = new();
    private readonly ColonyPanel _colonyPanel = new();
    private readonly Button _endTurn = new();
    private readonly TurnReportDialog _report = new();

    /// <param name="viewer">The empire this screen is shown to. Presentation-only; the simulation has no "current player".</param>
    public GameScreen(GameSession session, EmpireId viewer)
    {
        _session = session;
        _viewer = viewer;
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var topBar = new PanelContainer();
        AddChild(topBar);
        var topPadding = new MarginContainer { ThemeTypeVariation = "PanelPadding" };
        topBar.AddChild(topPadding);
        var topRow = new HBoxContainer();
        topPadding.AddChild(topRow);
        topRow.AddChild(_turnLabel);
        topRow.AddChild(new VSeparator());
        topRow.AddChild(_researchBar);
        _researchBar.CommandIssued += Execute;
        _message.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _message.HorizontalAlignment = HorizontalAlignment.Right;
        _message.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _message.MouseFilter = MouseFilterEnum.Stop;
        topRow.AddChild(_message);
        _endTurn.Pressed += EndTurn;
        topRow.AddChild(_endTurn);

        AddChild(_report);
        _report.ChooseProjectRequested += star => _map.Select(star);
        _report.ChooseResearchRequested += _researchBar.OpenPicker;

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        AddChild(body);

        _map.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _map.SizeFlagsVertical = SizeFlags.ExpandFill;
        _map.StarSelected += _ => Refresh();
        body.AddChild(_map);

        var side = new PanelContainer { CustomMinimumSize = new Vector2(380, 0) };
        body.AddChild(side);
        var sidePadding = new MarginContainer { ThemeTypeVariation = "PanelPadding" };
        side.AddChild(sidePadding);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        sidePadding.AddChild(scroll);
        var sideColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(sideColumn);
        sideColumn.AddChild(_starPanel);
        sideColumn.AddChild(_colonyPanel);
        _colonyPanel.CommandIssued += Execute;

        _map.ShowGalaxy(_session.State.Stars, HomeStar() is { } home ? new[] { home } : System.Array.Empty<StarId>());
        if (HomeStar() is { } start)
        {
            _map.Select(start);
        }

        Refresh();
    }

    private void Execute(Command command)
    {
        CommandOutcome outcome = _session.Execute(command);
        _message.Text = outcome.Rejection?.Message ?? string.Empty;
        _message.TooltipText = _message.Text;

        // Rebuild after the triggering control's signal has finished.
        Callable.From(Refresh).CallDeferred();

        if (outcome.Report is { } report)
        {
            _report.ShowReport(_session.Content, _session.State, report, _viewer);
        }
    }

    /// <summary>
    /// Marks the viewer's empire ready, which resolves the turn once every human empire is ready.
    /// Pressing again while waiting withdraws readiness.
    /// </summary>
    private void EndTurn()
    {
        bool ready = _session.State.FindEmpire(_viewer)!.Ready;
        Execute(new SetReadyCommand(_viewer, _session.State.Turn, !ready));
    }

    private void Refresh()
    {
        GameState state = _session.State;
        TurnEconomy forecast = _session.Forecast();
        _turnLabel.Text = $"Turn {state.Turn}";
        bool waiting = state.FindEmpire(_viewer)!.Ready;
        _endTurn.Text = waiting ? "Waiting for others (cancel)" : "End Turn";
        _researchBar.ShowEmpire(_session.Content, state, _viewer, forecast);

        if (_map.Selected is not { } selected)
        {
            return;
        }

        _starPanel.ShowStar(_session.Content, state, selected, _viewer);
        ColonyState? colony = state.Colonies.FirstOrDefault(c =>
            c.EmpireId == _viewer && state.FindPlanet(c.PlanetId)?.StarId == selected);
        if (colony is null)
        {
            _colonyPanel.Clear();
        }
        else
        {
            _colonyPanel.ShowColony(_session.Content, state, colony.Id, forecast);
        }
    }

    /// <summary>The star of the viewer's first colony.</summary>
    private StarId? HomeStar()
    {
        GameState state = _session.State;
        ColonyState? colony = state.Colonies.FirstOrDefault(c => c.EmpireId == _viewer);
        return colony is null ? null : state.FindPlanet(colony.PlanetId)?.StarId;
    }
}
