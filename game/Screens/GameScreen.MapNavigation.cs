using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Screens;

public partial class GameScreen
{
    private string? _commandPage;
    private readonly HashSet<VesselId> _mapFleetSelection = new();
    private readonly VBoxContainer _mapOrders = new() { Name = "MapFleetOrders", Visible = false };
    private readonly Label _mapRoute = new() { Name = "MapRouteSummary", AutowrapMode = TextServer.AutowrapMode.WordSmart, ThemeTypeVariation = "SubtleLabel" };

    private void ToggleCommandPage(string name, Action open)
    {
        if (name == "OpenGalaxyWorkspace") { _commandPage = null; open(); return; }
        if (_commandPage == name && _workspace != "Galaxy")
        { _commandPage = null; ShowWorkspace("Galaxy"); return; }
        open(); _commandPage = name;
    }
    private void OpenGalaxyPage()
    {
        bool alreadyThere = _workspace == "Galaxy";
        ShowWorkspace("Galaxy");
        if (alreadyThere && HomeStar() is { } home)
        {
            ClearMapFleetSelection();
            _map.CenterOn(home); _map.Select(home);
        }
    }
    private void DismissReportForNavigation()
    {
        if (!_report.Visible) return;
        _reportOrigin = null;
        _report.Hide();
    }
    private void AddMapZoomKeys(HFlowContainer toolbar)
    {
        void Key(string text, string name, string help, Action action)
        {
            var key = new Button { Text = text, Name = name, TooltipText = help, ThemeTypeVariation = "CompactCommandKey" };
            key.Pressed += action; toolbar.AddChild(key);
        }
        Key("−", "MapZoomOut", "Zoom out. You can also use the mouse wheel.", () => _map.ZoomAt(_map.Size / 2, false));
        Key("+", "MapZoomIn", "Zoom in. Drag with the middle mouse button to pan.", () => _map.ZoomAt(_map.Size / 2, true));
        Key("Fit", "MapZoomFit", "Fit the whole galaxy in view.", _map.ResetView);
    }
    private void BuildMapOrders(VBoxContainer console)
    {
        console.AddChild(_mapOrders);
        _mapOrders.AddChild(new Label { Text = "FLEET COURSE", ThemeTypeVariation = "ConsoleHeading" });
        _mapOrders.AddChild(_mapRoute);
        var cancel = new Button { Name = "ClearMapFleetSelection", Text = "Deselect fleet", ThemeTypeVariation = "CompactCommandKey" };
        cancel.Pressed += () => { ClearMapFleetSelection(); Refresh(); };
        _mapOrders.AddChild(cancel);
    }
    private void ClearMapFleetSelection()
    {
        _mapFleetSelection.Clear(); _courseDestination = null; _hoverDestination=null;
    }
    private void SelectMapVessel(VesselId id)
    {
        var view = View; _mapFleetSelection.Clear();
        if (view.Fleets?.Ships.FirstOrDefault(s => s.Id == id)?.Fleet is { } fleet)
            _mapFleetSelection.UnionWith(view.Fleets.Fleets.Single(f => f.Id == fleet).Members);
        else _mapFleetSelection.Add(id);
    }
    private void RefreshMapOrders(EmpireView view)
    {
        _mapFleetSelection.RemoveWhere(id => !view.Vessels.Any(v => v.Id == id));
        _mapOrders.Visible = _mapFleetSelection.Count > 0;
        if (_mapFleetSelection.Count == 0) return;
        string selection = _mapFleetSelection.Count == 1 ? "1 ship selected." : _mapFleetSelection.Count + " ships selected.";
        var ships = view.Vessels.Where(v => _mapFleetSelection.Contains(v.Id)).OrderBy(v => v.Id).ToArray();
        if (ships.Any(v => v.Transit is not null)) { _mapRoute.Text = selection + " Travel is underway; the course is committed until arrival."; return; }
        if ((_hoverDestination ?? _courseDestination) is not { } target || ships.All(v => v.StationaryStarId == target))
        { _mapRoute.Text = selection + " Click a system to set course."; return; }
        string destination = view.Stars.Single(s => s.Chart.Id == target).Chart.Name;
        if (view.Fleets is not null)
        {
            var preview = FleetQueries.PreviewAction(_session.Content, view, MapFleetOrders.Selection(view, _mapFleetSelection, target));
            if (!preview.Allowed) { _mapRoute.Text = selection + " " + preview.Rejection; return; }
            var times = preview.Routes.Select(r => r.Turns).ToArray();
            string eta = times.Length == 0 ? "" : times.Min() == times.Max() ? $"{times.Min()} " + (times.Min() == 1 ? "turn" : "turns") : $"{times.Min()}–{times.Max()} turns";
            bool queued = ships.All(v => v.PendingMove == target);
            _mapRoute.Text = selection + $"\n{destination} · {eta}" + (queued ? "\nCourse queued for next turn." : "\nClick the system to set course.");
        }
        else
        {
            if (ships.Length != 1) { _mapRoute.Text = "Choose one ship below to set its course."; return; }
            if (view.Scouting?.Habitats.Any(h => h.Star == target) == true) { _mapRoute.Text = "Known ameba habitat. Review the ship's approach in its orders."; return; }
            _mapRoute.Text = selection + "\nDestination: " + destination + ". Travel begins next turn.";
        }
    }
    private void SetMapCourse()
    {
        var view = View;
        if (_courseDestination is not { } target || _mapFleetSelection.Count == 0) return;
        if (view.Fleets is not null)
        {
            var action = MapFleetOrders.Selection(view, _mapFleetSelection, target);
            var preview = FleetQueries.PreviewAction(_session.Content, view, action);
            if (!preview.Allowed) { ShowMessage(preview.Rejection ?? "This course is unavailable.", true); return; }
            Execute(new FleetCommand(view.Viewer, view.Turn, action) { Fingerprint = preview.Fingerprint });
        }
        else if (_mapFleetSelection.Count == 1)
            Execute(new SetMoveOrderCommand(view.Viewer, view.Turn, _mapFleetSelection.Single(), target));
    }
}
