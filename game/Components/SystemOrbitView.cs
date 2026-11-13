using System;
using System.Collections.Generic;
using Godot;

namespace OpenAntares.Game.Components;

/// <summary>
/// Draws a star with one tilted ring per orbit and places caller-supplied planet controls on those rings.
/// Pure presentation: positions come from orbit numbers and planet IDs, never from game state.
/// </summary>
public partial class SystemOrbitView : Control
{
    public const string ThemeType = "SystemOrbitView";

    /// <summary>One planet marker to place on its ring.</summary>
    public sealed record Body(int Orbit, int Id, Control Marker, Control? Caption, OwnerMark Owner, bool Selected);
    public enum OwnerMark { None, Own, Other }

    private readonly List<Body> _bodies = new();
    private int _rings;
    private int _speckSeed;

    public SystemOrbitView()
    {
        Name = "SystemOrbitView";
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Pass;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
    }

    public override void _Ready() => Resized += Arrange;

    /// <summary>Replaces the bodies. <paramref name="rings"/> is the outermost orbit to draw.</summary>
    public void Show(int rings, int speckSeed, IEnumerable<Body> bodies)
    {
        foreach (var body in _bodies)
        {
            RemoveChild(body.Marker); body.Marker.QueueFree();
            if (body.Caption is { } caption) { RemoveChild(caption); caption.QueueFree(); }
        }
        _bodies.Clear();
        _bodies.AddRange(bodies);
        _rings = Math.Max(rings, 1);
        _speckSeed = speckSeed;
        foreach (var body in _bodies)
        {
            AddChild(body.Marker);
            if (body.Caption is { } caption) AddChild(caption);
        }
        Arrange();
        QueueRedraw();
    }

    private (Vector2 Center, float Rx, float Ry, float Inner) Frame()
    {
        // Small screens shrink the margin so the rings keep most of the space.
        float margin = Math.Min(GetThemeConstant("margin", ThemeType), Size.Y * 0.08f);
        float tilt = Math.Clamp(GetThemeConstant("ring_tilt_percent", ThemeType), 10, 100) / 100f;
        float rx = Math.Max(1, Size.X / 2 - margin);
        float ry = Math.Max(1, Size.Y / 2 - margin);
        rx = Math.Min(rx, ry / tilt);
        // Keep the outermost planet, its selection ring and its orbit label inside the diagram.
        float pad = SizeFor(rx, StarRadius(rx) * 2.2f) / 2 + 18;
        float wide = Math.Max(1, Size.X / 2 - pad), tall = Math.Max(1, Size.Y / 2 - pad);
        // Short, wide windows flatten the rings (down to a floor) instead of shrinking them.
        tilt = Math.Max(Math.Min(tilt, 0.3f), Math.Min(tilt, tall / wide));
        rx = Math.Max(1, Math.Min(wide, tall / tilt));
        ry = rx * tilt;
        return (Size / 2, rx, ry, StarRadius(rx) * 2.2f);
    }

    private float StarRadius(float rx) => Math.Max(4, Math.Min(GetThemeConstant("star_radius", ThemeType), rx * 0.09f));

    private float RingRadius(int orbit, float inner, float outer) => inner + (outer - inner) * orbit / _rings;

    private float PlanetSize(float rx) => SizeFor(rx, Frame().Inner);

    private float SizeFor(float rx, float inner)
    {
        float spacing = Math.Max(0, rx - inner) / _rings;
        float size = Math.Min(GetThemeConstant("max_planet_size", ThemeType), spacing * 1.6f);
        return Math.Max(GetThemeConstant("min_planet_size", ThemeType), size);
    }

    /// <summary>Stable angle per planet: golden-angle spread by orbit plus a small ID offset, so neighbours rarely line up.</summary>
    private static float Angle(int orbit, int id) => Mathf.DegToRad(orbit * 137.5f + 40f + (id % 29) * 3f);

    private Vector2 BodyAt(Body body)
    {
        var (center, rx, ry, inner) = Frame();
        float angle = Angle(body.Orbit, body.Id);
        float scale = RingRadius(body.Orbit, inner, rx) / rx;
        return center + new Vector2(Mathf.Cos(angle) * rx * scale, Mathf.Sin(angle) * ry * scale);
    }

    private void Arrange()
    {
        if (_bodies.Count == 0) return;
        float size = PlanetSize(Frame().Rx);
        foreach (var body in _bodies)
        {
            var at = BodyAt(body);
            body.Marker.Size = new Vector2(size, size);
            body.Marker.Position = at - body.Marker.Size / 2;
            if (body.Caption is { } caption)
            {
                caption.Size = caption.GetCombinedMinimumSize();
                caption.Position = at + new Vector2(-caption.Size.X / 2, size / 2 + 9);
            }
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), GetThemeColor("background", ThemeType));
        // Background specks: presentation-only scatter seeded by the star so each system keeps its own sky.
        var speck = GetThemeColor("speck", ThemeType);
        uint state = (uint)_speckSeed * 2654435761u + 1u;
        float Next() { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return (state & 0xFFFFFF) / (float)0x1000000; }
        int specks = GetThemeConstant("specks", ThemeType);
        for (int i = 0; i < specks; i++)
            DrawCircle(new Vector2(Next() * Size.X, Next() * Size.Y), Next() < 0.85f ? 1f : 1.6f, speck);

        var (center, rx, ry, inner) = Frame();
        float star = StarRadius(rx);
        var glow = GetThemeColor("star_glow", ThemeType);
        for (int layer = 4; layer >= 1; layer--) DrawCircle(center, star * (1 + layer * 0.45f), glow);
        float width = GetThemeConstant("ring_width", ThemeType);
        var ring = GetThemeColor("ring", ThemeType);
        var ringSelected = GetThemeColor("ring_selected", ThemeType);
        int selectedOrbit = _bodies.Find(b => b.Selected)?.Orbit ?? -1;
        var occupied = new HashSet<int>();
        foreach (var body in _bodies) occupied.Add(body.Orbit);
        foreach (int orbit in occupied)
        {
            float scale = RingRadius(orbit, inner, rx) / rx;
            DrawEllipse(center, rx * scale, ry * scale, orbit == selectedOrbit ? ringSelected : ring, width);
        }

        DrawCircle(center, star,GetThemeColor("star_core", ThemeType));
        DrawCircle(center, star * 0.55f, GetThemeColor("star_heart", ThemeType));

        float size = _bodies.Count > 0 ? PlanetSize(rx) : 0;
        foreach (var body in _bodies)
        {
            var at = BodyAt(body);
            if (body.Owner != OwnerMark.None)
                DrawArc(at, size / 2 + 3, 0, Mathf.Tau, 48, GetThemeColor(body.Owner == OwnerMark.Own ? "own_colony" : "other_colony", ThemeType), width);
            if (body.Selected)
                DrawArc(at, size / 2 + 7, 0, Mathf.Tau, 48, GetThemeColor("selection", ThemeType), width);
        }
    }

    private void DrawEllipse(Vector2 center, float rx, float ry, Color color, float width)
    {
        const int Points = 96;
        var line = new Vector2[Points + 1];
        for (int i = 0; i <= Points; i++)
        {
            float a = Mathf.Tau * i / Points;
            line[i] = center + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
        }
        DrawPolyline(line, color, width, true);
    }
}
