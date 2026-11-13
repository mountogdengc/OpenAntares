using System;
using System.Linq;
using Godot;
using OpenAntares.Game.Components;

namespace OpenAntares.Game.Screens;

/// <summary>The system inspector floats over the galaxy map instead of replacing it.</summary>
public partial class GameScreen
{
    private bool _systemViewOpen;

    private void WireSystemView()
    {
        _starPanel.Visible = false;
        _map.AddChild(_starPanel);
        _map.Resized += FitSystemView;
        _starPanel.CloseRequested += CloseSystemView;
        _starPanel.PlanetSelected += planet =>
        {
            _selectedPlanet = planet; _requireColonyChoice = false;
            _selectedColony = View.Colonies.FirstOrDefault(c => c.PlanetId == planet)?.Id;
            Callable.From(Refresh).CallDeferred();
        };
        _starPanel.ManageRequested += colony => { CloseSystemView(); SelectColony(colony); };
    }

    private void OpenSystemView()
    {
        if (_map.Selected is null) return;
        if (_workspace != "Galaxy") ShowWorkspace("Galaxy");
        _systemViewOpen = true;
        _starPanel.Visible = true;
        FitSystemView();
        Refresh();
    }

    private void CloseSystemView()
    {
        if (!_systemViewOpen) return;
        _systemViewOpen = false;
        _starPanel.Visible = false;
    }

    private void FitSystemView()
    {
        if (!_systemViewOpen) return;
        float margin = GetThemeConstant("map_margin", "SystemViewMetrics");
        var size = new Vector2(
            Math.Min(_map.Size.X - 2 * margin, GetThemeConstant("max_width", "SystemViewMetrics")),
            Math.Min(_map.Size.Y - 2 * margin, GetThemeConstant("max_height", "SystemViewMetrics")));
        size = new Vector2(Math.Max(size.X, 1), Math.Max(size.Y, 1));
        _starPanel.SetAnchorsPreset(LayoutPreset.Center);
        _starPanel.OffsetLeft = -size.X / 2; _starPanel.OffsetRight = size.X / 2;
        _starPanel.OffsetTop = -size.Y / 2; _starPanel.OffsetBottom = size.Y / 2;
    }
}
