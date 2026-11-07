using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using OpenAntares.Game.Presentation;
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
    private HashSet<StarId> _knownAmebas = new();
    public void ShowKnownAmebas(OpenAntares.Simulation.Scouting.ScoutView? view){_knownAmebas=view?.Habitats.Select(h=>h.Star).ToHashSet()??new();QueueRedraw();}
    private StarId? _selected;
    private int _minX;
    private int _minY;
    private int _maxX = 1;
    private int _maxY = 1;
    private readonly Dictionary<StarId, Rect2> _ownedMarkerBounds = new();
    private readonly List<Rect2> _visibleLabelBounds = new();

    /// <summary>Raised when the player clicks a star.</summary>
    public event Action<StarId>? StarSelected;
    /// <summary>Inspect owned stationary ships at this star; no order is implied.</summary>
    public event Action<StarId>? OwnedVesselsSelected;

    public IReadOnlyDictionary<StarId, Rect2> OwnedMarkerBounds => _ownedMarkerBounds;
    public IReadOnlyList<Rect2> VisibleLabelBounds => _visibleLabelBounds;

    public StarId? Selected => _selected;

    public override void _Ready()
    {
        FocusMode = FocusModeEnum.Click;
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
    }

    public void ShowGalaxy(GalaxyMapView state)
    {
        _ownedMarkerBounds.Clear();
        _visibleLabelBounds.Clear();
        _stars = state.Stars;
        _units = state.Units;
        _homeStars = state.HomeMarkers.ToHashSet();
        TooltipText = _stars.FirstOrDefault(s => s.Id == _selected)?.Name ?? "";
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
        var charted = _stars.FirstOrDefault(s => s.Id == star);
        if (charted is null) return;
        _selected = star;
        TooltipText = charted.Name;
        QueueRedraw();
        StarSelected?.Invoke(star);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized || what == NotificationThemeChanged)
        {
            _ownedMarkerBounds.Clear();
            _visibleLabelBounds.Clear();
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        _ownedMarkerBounds.Clear();
        _visibleLabelBounds.Clear();
        DrawRect(new Rect2(Vector2.Zero, Size), GetThemeColor("background", ThemeType));
        if (_stars.Count == 0 || Size.X <= 0 || Size.Y <= 0)
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
        var labels = new List<(StarId Id, string Text, Rect2 Bounds)>();
        float textHeight = font.GetHeight(fontSize);
        float ascent = font.GetAscent(fontSize);

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

            if (_units.Any(v => v.Owned && v.Station == star.Id) && Size.X >= 8 && Size.Y >= 8)
            {
                var marker = new Rect2(new Vector2(
                    Mathf.Clamp(position.X + (home ? homeRadius : starRadius) + 3, 0, Size.X - 8),
                    Mathf.Clamp(position.Y - 4, 0, Size.Y - 8)), new Vector2(8, 8));
                _ownedMarkerBounds.Add(star.Id, marker);
                DrawRect(marker, selectionColor, filled: false, width: 2);
            }

            int units = _units.Count(v => v.Station == star.Id);
            string label = (units > 0 ? $"{star.Name} [{units}]" : star.Name)+(_knownAmebas.Contains(star.Id)?" [ameba]":"");
            if (Size.Y < textHeight) continue;
            string fitted = FitLabel(font, label, fontSize, Math.Min(LabelWidth, Size.X));
            if (fitted.Length == 0) continue;
            float width = font.GetStringSize(fitted, HorizontalAlignment.Left, -1, fontSize).X;
            var rect = new Rect2(new Vector2(
                Mathf.Clamp(position.X - width / 2, 0, Math.Max(0, Size.X - width)),
                Mathf.Clamp(position.Y + labelOffset - ascent, 0, Math.Max(0, Size.Y - textHeight))), new Vector2(width, textHeight));
            labels.Add((star.Id, fitted, rect));
        }
        var placed = MapLabelLayout.Place(labels.Select(l => new MapLabelCandidate(l.Id, l.Bounds, l.Id == _selected, _homeStars.Contains(l.Id))).ToArray(), new Rect2(Vector2.Zero, Size)).ToHashSet();
        foreach (var label in labels.Where(l => placed.Contains(l.Id)))
        {
            _visibleLabelBounds.Add(label.Bounds);
            DrawString(font, label.Bounds.Position + new Vector2(0, ascent), label.Text,
                HorizontalAlignment.Left, label.Bounds.Size.X, fontSize, labelColor);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            return;
        }

        foreach (var marker in _ownedMarkerBounds.Where(p => p.Value.HasPoint(click.Position))
            .OrderBy(p => p.Value.GetCenter().DistanceSquaredTo(click.Position)).ThenBy(p => p.Key))
        {
            OwnedVesselsSelected?.Invoke(marker.Key);
            AcceptEvent();
            return;
        }

        float pickRadius = GetThemeConstant("selection_radius", ThemeType) * 1.5f;
        MapTransform transform = BuildTransform();
        ChartedStar? nearest = _stars
            .Select(star => (star, distance: ToScreen(star.X, star.Y, transform).DistanceTo(click.Position)))
            .Where(candidate => candidate.distance <= pickRadius)
            .OrderBy(candidate => candidate.distance)
            .ThenBy(candidate => candidate.star.Id)
            .Select(candidate => candidate.star)
            .FirstOrDefault();

        if (nearest is not null)
        {
            Select(nearest.Id);
            AcceptEvent();
        }
    }

    private static string FitLabel(Font font, string text, int fontSize, float width)
    {
        if (font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize).X <= width) return text;
        const string ellipsis = "…";
        if (font.GetStringSize(ellipsis, HorizontalAlignment.Left, -1, fontSize).X > width) return "";
        // Truncate by text elements rather than splitting a surrogate or combining mark.
        int[] elements = StringInfo.ParseCombiningCharacters(text);
        for (int count = elements.Length - 1; count > 0; count--)
        {
            string candidate = text[..elements[count]] + ellipsis;
            if (font.GetStringSize(candidate, HorizontalAlignment.Left, -1, fontSize).X <= width) return candidate;
        }
        return ellipsis;
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
