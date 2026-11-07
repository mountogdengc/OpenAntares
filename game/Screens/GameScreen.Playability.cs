using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Screens;

public partial class GameScreen
{
    private readonly OpenAntares.Game.Components.TurnGuidancePanel _guidance = new() { Name = "TurnGuidance" };
    private readonly HFlowContainer _feedbackActions = new() { Name = "CommandFeedbackActions", Visible = false };
    private readonly Button _dismissFeedback = new() { Name = "DismissCommandFeedback", Text = "Dismiss explanation" };
    private readonly Button _reviewFeedback = new() { Name = "ReviewRejectedResponse", Text = "Review response", Visible = false };
    private string? _feedbackDestination;
    private bool _readyRejectionVisible;

    private void RouteGuidance(string destination)
    {
        switch (destination)
        {
            case "Research": OpenResearch(); break;
            case "Colonies": OpenColonies(); break;
            case "Scouts" when View.Scouting is { } scouting: _scoutOverview.ShowScouting(_session.Content, scouting); break;
            case "Crisis" when View.Crisis is { } crisis: _crisisOverview.ShowCrisis(_session.Content, crisis); break;
            case "Combat" when View.Combat is not null: OpenCombatResponse(); break;
            case "Conflict" when View.Conflict is not null: OpenConflictOrders(null); break;
            case "Diplomacy" when View.Diplomacy is not null: _diplomacy.ShowEmpire(_session.Content, View); break;
            default: ShowMessage("That overview is unavailable for this campaign.", true); break;
        }
    }

    private void ExplainReadyRejection(string code)
    {
        _readyRejectionVisible = true;
        _feedbackDestination = code.StartsWith("combat_", StringComparison.Ordinal) ? "Combat"
            : code.StartsWith("ameba_", StringComparison.Ordinal) ? "Scouts"
            : code.StartsWith("crisis_", StringComparison.Ordinal) ? "Crisis"
            : code.StartsWith("conflict_", StringComparison.Ordinal) ? "Conflict" : null;
        _reviewFeedback.Visible = _feedbackDestination is not null;
        _feedbackActions.Show();
        _dismissFeedback.GrabFocus();
    }

    private readonly Button _returnReportEntry = new() { Name = "ReturnToInvokingReport", Text = "Return to report", Visible = false };
    private readonly PlayabilityContext _context = new();
    private bool _selectionInitialized, _requireColonyChoice, _restoringNavigation;
    private int _focusRevision;
    private PlayabilityReturn? _reportOrigin, _reportPosition;
    private bool _returnToReport, _reportWorkspaceExcursion;
    private int _reportThroughTurn;

    private static IEnumerable<T> PresentationNodes<T>(Node root) where T : Node
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is T value) yield return value;
            foreach (var nested in PresentationNodes<T>(child)) yield return nested;
        }
    }

    private string CurrentFocusKey()
    {
        var focus = GetViewport().GuiGetFocusOwner();
        return focus is not null && IsAncestorOf(focus) && !focus.Name.ToString().StartsWith("@", StringComparison.Ordinal)
            ? focus.Name.ToString() : "OpenGalaxyWorkspace";
    }

    private PlayabilityReturn CaptureWorkspace() => new(_workspace, CurrentFocusKey(), _workspaceScroll.ScrollVertical)
    { Star = _map.Selected, Planet = _selectedPlanet, Colony = _selectedColony, Vessel = _selectedVessel, Destination = _courseDestination, RequireColonyChoice = _requireColonyChoice };

    private void ReconcileSelections(EmpireView view)
    {
        bool lostVessel = _selectedVessel is { } vessel && !view.Vessels.Any(v => v.Id == vessel);
        bool lostColony = _selectedColony is { } colony && !view.Colonies.Any(c => c.Id == colony);
        _context.Workspace = _workspace; _context.Star = _map.Selected; _context.Planet = _selectedPlanet;
        _context.Colony = _selectedColony; _context.Vessel = _selectedVessel;
        _context.Reconcile(view);
        if (lostVessel) { _selectedVessel = null; _requireVesselChoice = true; _courseDestination = null; }
        if (lostColony) { _selectedColony = null; _selectedPlanet = null; _requireColonyChoice = true; }
        if (_context.LostSelection) ShowMessage("The selected object is no longer available. Choose an owned colony or vessel before reviewing another order.", true);
        if (!_selectionInitialized)
        {
            _selectionInitialized = true;
            if (!_requireVesselChoice) _selectedVessel = view.Vessels.FirstOrDefault()?.Id;
        }
    }

    private void RecordWorkspaceChange(string destination)
    {
        if (!_restoringNavigation && destination != _workspace)
        {
            if (_context.Returns.Count >= 32) _context.Returns.Clear();
            _context.Returns.Push(CaptureWorkspace());
        }
        if (_returnToReport) _reportWorkspaceExcursion = true;
        _context.Workspace = destination;
    }

    private void ReturnFromWorkspace()
    {
        if (_returnToReport) { ReturnToReport(); return; }
        if (_context.Returns.TryPop(out var previous)) RestoreWorkspace(previous);
        else ShowWorkspace("Galaxy");
    }

    private void RestoreWorkspace(PlayabilityReturn previous)
    {
        var view = View;
        _selectedColony = view.Colonies.Any(c => c.Id == previous.Colony) ? previous.Colony : null;
        _selectedVessel = view.Vessels.Any(v => v.Id == previous.Vessel) ? previous.Vessel : null;
        _requireVesselChoice = _selectedVessel is null;
        _requireColonyChoice = previous.RequireColonyChoice || previous.Colony is not null && _selectedColony is null;
        _selectedPlanet = view.Stars.Any(s => s.Planets?.Any(p => p.Id == previous.Planet) == true) ? previous.Planet : null;
        if (_requireColonyChoice) _selectedPlanet = null;
        _restoringNavigation = true;
        try
        {
            if (previous.Star is { } star && view.Stars.Any(s => s.Chart.Id == star)) _map.Select(star);
            _courseDestination = previous.Destination;
            ShowWorkspace(previous.Workspace == "Galaxy" || _pages.ContainsKey(previous.Workspace) ? previous.Workspace : "Galaxy");
            Refresh();
        }
        finally { _restoringNavigation = false; }
        RestoreSemanticFocus(previous.FocusKey, 0, previous.Workspace, previous.ScrollVertical);
    }

    private async void RestoreSemanticFocus(string key, ulong originalId, string workspace, int? scrollOffset = null)
    {
        int revision = ++_focusRevision;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!IsInstanceValid(this) || !IsInsideTree() || revision != _focusRevision || workspace != _workspace || PresentationNodes<Window>(this).Any(w => w.Visible)) return;
        var current = GetViewport().GuiGetFocusOwner();
        if (current is not null && current.IsVisibleInTree() && current.GetInstanceId() != originalId && current.Name != key && originalId != 0) return;
        var target = PresentationNodes<Control>(this).FirstOrDefault(c => c.Name == key && c.IsVisibleInTree() && c.FocusMode != FocusModeEnum.None && (c is not BaseButton button || !button.Disabled));
        target ??= PresentationNodes<Button>(this).FirstOrDefault(c => c.Name == "OpenGalaxyWorkspace");
        target?.GrabFocus();
        if (scrollOffset is { } offset) _workspaceScroll.ScrollVertical = offset;
    }

    private async void FocusOwnedSelection(StarId star)
    {
        ulong original = GetViewport().GuiGetFocusOwner()?.GetInstanceId() ?? 0;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!IsInstanceValid(this) || !IsInsideTree() || _workspace != "Galaxy" || _map.Selected != star || PresentationNodes<Window>(this).Any(w => w.Visible)) return;
        var focused = GetViewport().GuiGetFocusOwner();
        if (focused is not null && focused.GetInstanceId() != original) return;
        Control? target = PresentationNodes<OptionButton>(_selectionCard).FirstOrDefault(c => c.Name == "SelectionVesselChooser" && c.ItemCount > 1);
        target ??= PresentationNodes<Button>(_selectionCard).FirstOrDefault(c => c.Name == "SelectionReviewOrders" && !c.Disabled);
        target?.GrabFocus();
    }

    private void WireReportReturns()
    {
        _report.NavigationStarting += BeginReportExcursion;
        _report.VisibilityChanged += () =>
        {
            if (!_report.Visible && !_returnToReport && _reportOrigin is { } origin)
            { _reportOrigin = null; RestoreWorkspace(origin); }
        };
        foreach (var window in PresentationNodes<Window>(this).Where(w => w != _report))
            window.VisibilityChanged += () => { if (!window.Visible && _returnToReport) TryReturnFromOverview(); };
    }

    private void BeginReportExcursion()
    {
        if (!_returnToReport)
        {
            _reportPosition = _report.CapturePosition();
            _reportThroughTurn = _latestReport?.ToTurn ?? View.Turn;
            _reportWorkspaceExcursion = false;
            _returnToReport = true;
        }
        _returnReportEntry.Show();
        _report.Hide();
    }

    private async void TryReturnFromOverview()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!IsInstanceValid(this) || !IsInsideTree() || _context.OverviewReturns.Count > 0 || !_returnToReport || _reportWorkspaceExcursion || PresentationNodes<Window>(this).Any(w => w.Visible)) return;
        ReturnToReport();
    }

    private void ReturnToReport()
    {
        _returnToReport = false; _reportWorkspaceExcursion = false;
        _returnReportEntry.Hide();
        if (_reportPosition is { } position) _report.RestorePosition(position);
    }

    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape } || PresentationNodes<Window>(this).Any(w => w.Visible)) return;
        if (_workspace == "Galaxy" && !_returnToReport) return;
        ReturnFromWorkspace();
        GetViewport().SetInputAsHandled();
    }
}
