using System.Linq;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Knowledge;

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
    private readonly ContactsPanel _contacts = new();
    private readonly ColonyList _colonies = new();
    private readonly VesselPanel _vessels = new();
    private ColonyId? _selectedColony;
    private PlanetId? _selectedPlanet;
    private VesselId? _selectedVessel;
    private StarId? _courseDestination;
    private EmpireView View => _session.View(_viewer);
    private readonly TurnReportDialog _report = new();
    private readonly SaveGameDialog _saveDialog = new();

    /// <summary>Raised when the player wants to load a saved game. The current game is untouched unless a load succeeds.</summary>
    public event System.Action? LoadRequested;

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
        var saveButton = new Button { Text = "Save" };
        saveButton.Pressed += () => _saveDialog.Open(_session, SuggestedSaveName());
        topRow.AddChild(saveButton);
        var loadButton = new Button { Text = "Load" };
        loadButton.Pressed += () => LoadRequested?.Invoke();
        topRow.AddChild(loadButton);
        _endTurn.Pressed += EndTurn;
        topRow.AddChild(_endTurn);

        AddChild(_saveDialog);
        _saveDialog.Saved += name => ShowMessage($"Saved as \"{name}\".", isWarning: false);

        AddChild(_report);
        _report.ChooseProjectRequested += ChooseProject;
        _report.StarRequested += star => _map.Select(star);
        _report.ColonyRequested += SelectColony;
        _report.VesselRequested += vessel => { _selectedVessel = vessel; _courseDestination = null; Callable.From(Refresh).CallDeferred(); };
        _report.ChooseResearchRequested += _researchBar.OpenPicker;

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        AddChild(body);

        _map.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _map.SizeFlagsVertical = SizeFlags.ExpandFill;
        _map.StarSelected += star => { _courseDestination = star; Refresh(); };
        body.AddChild(_map);

        var side = new PanelContainer { CustomMinimumSize = new Vector2(380, 0) };
        body.AddChild(side);
        var sidePadding = new MarginContainer { ThemeTypeVariation = "PanelPadding" };
        side.AddChild(sidePadding);
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true,
        };
        sidePadding.AddChild(scroll);
        scroll.Resized += () => KeepFocusVisibleAfterResize(scroll);
        var sideColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(sideColumn);
        sideColumn.AddChild(_contacts);
        sideColumn.AddChild(_colonies);
        _colonies.ColonySelected += SelectColony;
        sideColumn.AddChild(_starPanel);
        _starPanel.PlanetSelected += planet => { _selectedPlanet = planet; _selectedColony = View.Colonies.FirstOrDefault(c => c.PlanetId == planet)?.Id; Callable.From(Refresh).CallDeferred(); };
        sideColumn.AddChild(_colonyPanel);
        _colonyPanel.CommandIssued += Execute;
        sideColumn.AddChild(_vessels);
        _vessels.CommandIssued += Execute;
        _vessels.VesselSelected += vessel => { _selectedVessel = vessel; _courseDestination = null; Callable.From(Refresh).CallDeferred(); };
        _vessels.DestinationSelected += star => { _courseDestination = star; _map.Select(star); };

        _map.ShowGalaxy(View.Galaxy);
        if (HomeStar() is { } start)
        {
            _map.Select(start);
        }

        Refresh();
    }

    private async void KeepFocusVisibleAfterResize(ScrollContainer scroll)
    {
        // FollowFocus runs when focus changes. A resize can hide a control that already owns focus.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(scroll) || !IsInsideTree()) return;
        Control? focused = GetViewport().GuiGetFocusOwner();
        if (focused is not null && scroll.IsAncestorOf(focused)) scroll.EnsureControlVisible(focused);
    }

    private async void ChooseProject(ColonyId colony)
    {
        SelectColony(colony);
        // Selection rebuilds the colony panel. Let containers lay out its controls before
        // focusing the picker so the scroll container reveals it and the popup aligns with it.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _colonyPanel.OpenProjectPicker();
    }

    private void Execute(Command command)
    {
        CommandOutcome outcome = _session.Execute(command);
        ShowMessage(outcome.Rejection?.Message ?? string.Empty, isWarning: true);

        // Rebuild after the triggering control's signal has finished.
        Callable.From(Refresh).CallDeferred();

        if (outcome.Report is { } report)
        {
            _report.ShowReport(_session.Content, EmpireViewProjector.ProjectReport(_session.Content, _session.State, report, _viewer));
        }
    }

    /// <summary>
    /// Marks the viewer's empire ready, which resolves the turn once every human empire is ready.
    /// Pressing again while waiting withdraws readiness.
    /// </summary>
    private void EndTurn()
    {
        var view = View;
        Execute(new SetReadyCommand(_viewer, view.Turn, !view.Research.Ready));
    }

    private void Refresh()
    {
        var view = View;
        _turnLabel.Text = $"Turn {view.Turn}";
        _endTurn.Text = view.Research.Ready ? "Waiting for others (cancel)" : "End Turn";
        _researchBar.ShowEmpire(_session.Content, view.Research);
        _map.ShowGalaxy(view.Galaxy);
        _contacts.Show(view);
        if (view.RulesVersion == Rules.LegacyVersion) ShowMessage("Legacy colony game - start a new game for exploration", false);
        if (_map.Selected is not { } selected) return;
        var star = view.Stars.Single(s => s.Chart.Id == selected);
        if (star.Planets is { } planets && !planets.Any(p => p.Id == _selectedPlanet))
        {
            var preferred = view.Colonies.FirstOrDefault(c => c.Id == _selectedColony && c.StarId == selected) ?? view.Colonies.FirstOrDefault(c => c.StarId == selected);
            _selectedPlanet = preferred?.PlanetId ?? (planets.Length > 0 ? planets[0].Id : null);
        }
        if (star.Planets is null) _selectedPlanet = null;
        var colony = view.Colonies.FirstOrDefault(c => c.PlanetId == _selectedPlanet && c.StarId == selected);
        _selectedColony = colony?.Id;
        if (colony is not null) _selectedPlanet = colony.PlanetId;
        _starPanel.ShowStar(_session.Content, star, view);
        _colonies.ShowColonies(view, _selectedColony);
        if (colony is null) _colonyPanel.Clear(); else _colonyPanel.ShowColony(_session.Content, colony);
        if (!view.Vessels.Any(v => v.Id == _selectedVessel)) _selectedVessel = view.Vessels.Length > 0 ? view.Vessels[0].Id : null;
        _vessels.ShowVessels(_session.Content, view, _selectedVessel, _courseDestination, _selectedPlanet);
    }

    private void SelectColony(ColonyId id)
    {
        var colony = View.Colonies.FirstOrDefault(c => c.Id == id); if (colony is null) return;
        _selectedColony = id; _selectedPlanet = colony.PlanetId; _map.Select(colony.StarId);
    }

    private void ShowMessage(string text, bool isWarning)
    {
        _message.Text = text;
        _message.TooltipText = text;
        _message.ThemeTypeVariation = isWarning ? "WarningLabel" : "SubtleLabel";
    }

    private string SuggestedSaveName()
    {
        var view = View;
        string star = HomeStar() is { } home ? view.Stars.Single(s => s.Chart.Id == home).Chart.Name : "Game";
        return $"{star} turn {view.Turn}";
    }

    /// <summary>The star of the viewer's first colony.</summary>
    private StarId? HomeStar()
    {
        return View.Colonies.FirstOrDefault()?.StarId;
    }
}
