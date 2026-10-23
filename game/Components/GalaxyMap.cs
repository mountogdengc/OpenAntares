using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Components;

/// <summary>
/// Draws the stars of a galaxy, scaled to fit the control, and lets the player select one.
/// Colours, sizes, and the label font come from the theme type <c>GalaxyMap</c>.
/// </summary>
public partial class GalaxyMap : Control
{
    private const string ThemeType = "GalaxyMap";

    private IReadOnlyList<ChartedStar> _stars = Array.Empty<ChartedStar>();
    private IReadOnlyList<(int X, int Y)> _backgroundPoints = Array.Empty<(int X, int Y)>();
    private System.Collections.Immutable.ImmutableArray<MapVesselMarker> _units;
    private HashSet<StarId> _homeStars = new();
    private StarId? _selected;
    private int _minX;
    private int _minY;
    private int _maxX = 1;
    private int _maxY = 1;

    /// <summary>Raised when the player clicks a star.</summary>
    public event Action<StarId>? StarSelected;

    public StarId? Selected => _selected;

    public override void _Ready()
    {
        FocusMode = FocusModeEnum.Click;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public void ShowGalaxy(GalaxyMapView state)
    {
        _stars = state.Stars;
        _units = state.Units;
        _homeStars = state.HomeMarkers.ToHashSet();
        if (state.Seed is { } seed && state.ShapeId == "spiral"
            && state.Width is { } width && state.Height is { } height)
        {
            _minX = 0;
            _minY = 0;
            _maxX = width - 1;
            _maxY = height - 1;
            _backgroundPoints = GalaxyBackgroundPoints.Generate(seed, width, height);
        }
        else
        {
            _minX = _stars.Count > 0 ? _stars.Min(s => s.X) : 0;
            _maxX = _stars.Count > 0 ? _stars.Max(s => s.X) : 1;
            _minY = _stars.Count > 0 ? _stars.Min(s => s.Y) : 0;
            _maxY = _stars.Count > 0 ? _stars.Max(s => s.Y) : 1;
            _backgroundPoints = Array.Empty<(int X, int Y)>();
        }
        QueueRedraw();
    }

    public void Select(StarId star)
    {
        _selected = star;
        QueueRedraw();
        StarSelected?.Invoke(star);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized || what == NotificationThemeChanged)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), GetThemeColor("background", ThemeType));
        if (_stars.Count == 0)
        {
            return;
        }

        Color starColor = GetThemeColor("star", ThemeType);
        Color homeColor = GetThemeColor("home_star", ThemeType);
        Color selectionColor = GetThemeColor("selection", ThemeType);
        Color labelColor = GetThemeColor("label", ThemeType);
        Color backgroundStarColor = GetThemeColor("background_star", ThemeType);
        int starRadius = GetThemeConstant("star_radius", ThemeType);
        int backgroundStarRadius = GetThemeConstant("background_star_radius", ThemeType);
        int homeRadius = GetThemeConstant("home_star_radius", ThemeType);
        int selectionRadius = GetThemeConstant("selection_radius", ThemeType);
        int labelOffset = GetThemeConstant("label_offset", ThemeType);
        Font font = GetThemeFont("font", "Label");
        int fontSize = GetThemeFontSize("label", ThemeType);
        const float LabelWidth = 160;
        MapTransform transform = BuildTransform();

        foreach (var point in _backgroundPoints)
        {
            DrawCircle(ToScreen(point.X, point.Y, transform), backgroundStarRadius, backgroundStarColor);
        }

        foreach (var unit in _units.Where(v => v.Owned && v.Origin.HasValue && v.Destination.HasValue))
        {
            var origin = _stars.First(s => s.Id == unit.Origin); var destination = _stars.First(s => s.Id == unit.Destination);
            DrawLine(ToScreen(origin.X, origin.Y, transform), ToScreen(destination.X, destination.Y, transform), selectionColor, 1, true);
        }
        foreach (ChartedStar star in _stars)
        {
            Vector2 position = ToScreen(star.X, star.Y, transform);
            bool home = _homeStars.Contains(star.Id);
            DrawCircle(position, home ? homeRadius : starRadius, home ? homeColor : starColor);

            if (star.Id == _selected)
            {
                DrawArc(position, selectionRadius, 0, Mathf.Tau, 48, selectionColor, 2, antialiased: true);
            }

            int units = _units.Count(v => v.Station == star.Id);
            string label = units > 0 ? $"{star.Name} [{units}]" : star.Name;
            DrawString(font, position + new Vector2(-LabelWidth / 2, labelOffset), label,
                HorizontalAlignment.Center, LabelWidth, fontSize, labelColor);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            return;
        }

        float pickRadius = GetThemeConstant("selection_radius", ThemeType) * 1.5f;
        MapTransform transform = BuildTransform();
        ChartedStar? nearest = _stars
            .Select(star => (star, distance: ToScreen(star.X, star.Y, transform).DistanceTo(click.Position)))
            .Where(candidate => candidate.distance <= pickRadius)
            .OrderBy(candidate => candidate.distance)
            .Select(candidate => candidate.star)
            .FirstOrDefault();

        if (nearest is not null)
        {
            Select(nearest.Id);
            AcceptEvent();
        }
    }

    /// <summary>Maps galaxy coordinates into the control, preserving aspect ratio and centring the result.</summary>
    private MapTransform BuildTransform()
    {
        float margin = GetThemeConstant("margin", ThemeType);
        float spanX = Math.Max(1, _maxX - _minX);
        float spanY = Math.Max(1, _maxY - _minY);

        Vector2 available = new(Math.Max(1, Size.X - 2 * margin), Math.Max(1, Size.Y - 2 * margin));
        float scale = Math.Min(available.X / spanX, available.Y / spanY);
        Vector2 offset = new Vector2(margin, margin) + (available - new Vector2(spanX, spanY) * scale) / 2;

        return new MapTransform(scale, offset);
    }

    private Vector2 ToScreen(int x, int y, MapTransform transform) =>
        transform.Offset + new Vector2(x - _minX, y - _minY) * transform.Scale;

    private readonly record struct MapTransform(float Scale, Vector2 Offset);
}
