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
        Key("−", "MapZoomOut", "Zoom out with numpad − or the mouse wheel.", () => _map.ZoomAt(_map.Size / 2, false));
        Key("+", "MapZoomIn", "Zoom in with numpad + or the mouse wheel. Pan with numpad 8/2/4/6 or middle-button drag.", () => _map.ZoomAt(_map.Size / 2, true));
        Key("Fit", "MapZoomFit", "Fit the whole galaxy in view.", _map.ResetView);
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
    private void ReconcileMapFleetSelection(EmpireView view)
    {
        _mapFleetSelection.RemoveWhere(id => !view.Vessels.Any(v => v.Id == id));
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
    public override void _Input(InputEvent input)
    {
        if(_workspace!="Galaxy"||!_map.IsVisibleInTree()||input is not InputEventKey{Pressed:true} key||key.CtrlPressed||key.AltPressed||key.MetaPressed)return;
        for(Node? focus=GetViewport().GuiGetFocusOwner();focus is not null;focus=focus.GetParent())
            if(focus is LineEdit or TextEdit or SpinBox)return;
        if(GetViewport().GetEmbeddedSubwindows().Any(w=>w.Visible)||PresentationNodes<Window>(this).Any(w=>w.Visible))return;
        Key code=key.PhysicalKeycode==Key.None?key.Keycode:key.PhysicalKeycode;
        switch(code)
        {
            case Key.Kp8:_map.PanBy(Vector2.Up);break;
            case Key.Kp2:_map.PanBy(Vector2.Down);break;
            case Key.Kp4:_map.PanBy(Vector2.Left);break;
            case Key.Kp6:_map.PanBy(Vector2.Right);break;
            case Key.KpAdd:_map.ZoomAt(_map.Size/2,true);break;
            case Key.KpSubtract:_map.ZoomAt(_map.Size/2,false);break;
            default:return;
        }
        GetViewport().SetInputAsHandled();
    }

}
