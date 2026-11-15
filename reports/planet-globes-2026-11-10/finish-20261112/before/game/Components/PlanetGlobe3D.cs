using System;
using Godot;
using OpenAntares.Game.Presentation;

namespace OpenAntares.Game.Components;

/// <summary>Owns one persistent private world. Planet changes only bind uniforms.</summary>
public partial class PlanetGlobe3D : Control
{
    private readonly TextureRect _image = new() {
        Name = "ColonyGlobeImage", MouseFilter = MouseFilterEnum.Ignore,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = TextureFilterEnum.Nearest,
        // Layers alpha-blended onto a transparent viewport leave premultiplied colour in its texture.
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha }
    };
    private readonly SubViewport _viewport = new() {
        Name = "PlanetViewport", OwnWorld3D = true, TransparentBg = true,
        RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
        Size = new(256,256), GuiDisableInput = true
    };
    private readonly MeshInstance3D[] _layers = new MeshInstance3D[3];
    private readonly ShaderMaterial[] _materials = new ShaderMaterial[3];
    private PlanetVisual? _visual;
    private bool _active, _static, _built;
    public SubViewport GlobeViewport => _viewport;
    public ShaderMaterial SurfaceMaterial => _materials[0];
    public PlanetGlobe3D()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        // Parented at construction so both are freed with this control even if it never enters the tree.
        AddChild(_viewport);
        AddChild(_image);
    }
    public override void _Ready()
    {
        var environment = new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.ClearColor,
            TonemapMode = Godot.Environment.ToneMapper.Linear
        } };
        _viewport.AddChild(environment);
        var camera = new Camera3D { Name = "PlanetCamera", Projection = Camera3D.ProjectionType.Orthogonal,
            Size = 2 / 0.85f, Position = new(0,0,4), Current = true, Near = 0.1f, Far = 8 };
        _viewport.AddChild(camera);
        string[] names = { "surface", "clouds", "atmosphere" };
        // The atmosphere shell must reach the 1.10 outer radius of planet_halo.
        float[] radii = { 1, 1.006f, 1.10f };
        for (int i = 0; i < names.Length; i++)
        {
            _materials[i] = PlanetGlobeMaterialBinder.Create("res://game/theme/planets/planet_" + names[i] + ".gdshader");
            _materials[i].RenderPriority = i;
            _layers[i] = new MeshInstance3D {
                Name = "Planet" + names[i], MaterialOverride = _materials[i],
                Mesh = new SphereMesh { Radius = radii[i], Height = 2*radii[i], RadialSegments = 96, Rings = 48 }
            };
            _viewport.AddChild(_layers[i]);
        }
        _image.Texture = _viewport.GetTexture();
        _image.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Resized += ResizeTarget;
        VisibilityChanged += UpdateRendering;
        _built = true;
        ResizeTarget();
        if (_visual is not null) SetVisual(_visual);
        UpdateRendering();
    }
    public void SetVisual(PlanetVisual visual)
    {
        _visual = visual;
        if (!_built) return;
        foreach (var material in _materials) PlanetGlobeMaterialBinder.Bind(material, visual);
        _layers[1].Visible = visual.CloudCover > 0;
        _layers[2].Visible = visual.AtmosphereDensity > 0;
        UpdateRendering();
    }
    public void SetActive(bool active) { _active = active; UpdateRendering(); }
    public void SetStatic(bool isStatic) { _static = isStatic; UpdateRendering(); }
    private void ResizeTarget()
    {
        int maximum = HasThemeConstant("colony_resolution", "PlanetGlobeMetrics") ? GetThemeConstant("colony_resolution", "PlanetGlobeMetrics") : 512;
        int side = Math.Clamp((int)Mathf.Ceil(Mathf.Min(Size.X,Size.Y)), 32, Math.Clamp(maximum,32,768));
        var size = new Vector2I(side,side);
        if (_viewport.Size != size) { _viewport.Size = size; UpdateRendering(); }
    }
    private void UpdateRendering()
    {
        if (!_built) return;
        _viewport.RenderTargetUpdateMode = !_active || !IsVisibleInTree() ? SubViewport.UpdateMode.Disabled
            : _static ? SubViewport.UpdateMode.Once : SubViewport.UpdateMode.Always;
    }
    // _Ready runs only once, so leaving the tree must not tear down anything re-entry cannot rebuild.
    public override void _EnterTree() => UpdateRendering();
    public override void _ExitTree() => _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
}
