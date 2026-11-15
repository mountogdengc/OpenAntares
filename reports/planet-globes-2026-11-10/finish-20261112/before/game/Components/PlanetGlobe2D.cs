using Godot;
using OpenAntares.Game.Presentation;

namespace OpenAntares.Game.Components;

/// <summary>One canvas quad/material. No viewport, selection state or frame callback.</summary>
public partial class PlanetGlobe2D : Control
{
    private readonly ColorRect _quad = new() { Name = "GlobeQuad", MouseFilter = MouseFilterEnum.Ignore };
    public ShaderMaterial GlobeMaterial { get; } = PlanetGlobeMaterialBinder.Create("res://game/theme/planets/planet_canvas.gdshader");
    public PlanetGlobe2D()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        // Parented and bound at construction: the quad is freed with this control even if it never enters
        // the tree, and stays bound when the control is removed and re-added (_Ready runs only once).
        _quad.Material = GlobeMaterial;
        AddChild(_quad);
    }
    public override void _Ready()
    {
        Resized += ResizeQuad;
        ResizeQuad();
    }
    public void SetVisual(PlanetVisual visual) => PlanetGlobeMaterialBinder.Bind(GlobeMaterial, visual);
    private void ResizeQuad()
    {
        float side = Mathf.Min(Size.X, Size.Y);
        _quad.Size = new(side, side);
        _quad.Position = (Size - _quad.Size) / 2;
        GlobeMaterial.SetShaderParameter("show_clouds", side >= 64);
    }
}
