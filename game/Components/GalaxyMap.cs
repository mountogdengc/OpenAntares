using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Components;

/// <summary>
/// Draws the stars of a galaxy, scaled to fit the control, and lets the player select one.
/// Colours, sizes, and the label font come from the theme type <c>GalaxyMap</c>.
/// </summary>
public partial class GalaxyMap : Control
{
    private const string ThemeType = "GalaxyMap";

    private IReadOnlyList<StarState> _stars = Array.Empty<StarState>();
    private HashSet<StarId> _homeStars = new();
    private StarId? _selected;

    /// <summary>Raised when the player clicks a star.</summary>
    public event Action<StarId>? StarSelected;

    public StarId? Selected => _selected;

    public override void _Ready()
    {
        FocusMode = FocusModeEnum.Click;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public void ShowGalaxy(IReadOnlyList<StarState> stars, IEnumerable<StarId> homeStars)
    {
        _stars = stars;
        _homeStars = homeStars.ToHashSet();
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
        int starRadius = GetThemeConstant("star_radius", ThemeType);
        int homeRadius = GetThemeConstant("home_star_radius", ThemeType);
        int selectionRadius = GetThemeConstant("selection_radius", ThemeType);
        int labelOffset = GetThemeConstant("label_offset", ThemeType);
        Font font = GetThemeFont("font", "Label");
        int fontSize = GetThemeFontSize("label", ThemeType);
        const float LabelWidth = 160;

        foreach (StarState star in _stars)
        {
            Vector2 position = ToScreen(star);
            bool home = _homeStars.Contains(star.Id);
            DrawCircle(position, home ? homeRadius : starRadius, home ? homeColor : starColor);

            if (star.Id == _selected)
            {
                DrawArc(position, selectionRadius, 0, Mathf.Tau, 48, selectionColor, 2, antialiased: true);
            }

            DrawString(font, position + new Vector2(-LabelWidth / 2, labelOffset), star.Name,
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
        StarState? nearest = _stars
            .Select(star => (star, distance: ToScreen(star).DistanceTo(click.Position)))
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
    private Vector2 ToScreen(StarState star)
    {
        float margin = GetThemeConstant("margin", ThemeType);
        int minX = _stars.Min(s => s.X);
        int maxX = _stars.Max(s => s.X);
        int minY = _stars.Min(s => s.Y);
        int maxY = _stars.Max(s => s.Y);
        float spanX = Math.Max(1, maxX - minX);
        float spanY = Math.Max(1, maxY - minY);

        Vector2 available = new(Math.Max(1, Size.X - 2 * margin), Math.Max(1, Size.Y - 2 * margin));
        float scale = Math.Min(available.X / spanX, available.Y / spanY);
        Vector2 offset = new Vector2(margin, margin) + (available - new Vector2(spanX, spanY) * scale) / 2;

        return offset + new Vector2(star.X - minX, star.Y - minY) * scale;
    }
}
