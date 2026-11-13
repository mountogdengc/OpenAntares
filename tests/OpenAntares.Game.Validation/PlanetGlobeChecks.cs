using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Appearance;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Validation;

public partial class PlanetGlobeValidation
{
    private static (StarView Star, KnownPlanetView Planet, OwnedColonyView Colony) Fixture()
    {
        var planet = new KnownPlanetView(new(7),1,"verdant_world",300,200,200,true,new ColonyId(2),new EmpireId(1)) {
            Appearance = new PlanetAppearanceDescriptor(1,3,0xfedcba9876543210UL), EnvironmentId = "shelf_sea"
        };
        var star = new StarView(new(new(1),"Synthetic",0,0,null),ImmutableArray.Create(planet),0,true);
        // Economy is intentionally absent: the builder must never inspect it.
        var colony = new OwnedColonyView(new(2),new(7),new(1),new(1),5,new Workforce(5,0,0),0,0,null,"",ImmutableArray<string>.Empty,null!);
        return (star,planet,colony);
    }
    private async Task RunCase()
    {
        if (_case is "Review" or "Feature") await ReviewRegressions();
        if (_case is "Builder" or "Prototype" or "Layers" or "Cities" or "Feature") BuilderChecks();
        if (_case is "Clock" or "Prototype" or "Feature") ClockChecks();
        if (_case is "Prototype" or "Layers" or "Feature") await PrototypeChecks();
        if (_case is "Brightness" or "Prototype" or "Feature") await BrightnessChecks();
        if (_case is "Layers" or "Cities" or "Feature") await CityChecks();
        if (_case is "Layers" or "Feature") await LayerLifecycleChecks();
        if (_case is "Sidebar" or "Colony" or "Feature") await IntegrationChecks(_case != "Sidebar");
        if (_case is "Performance" or "Feature") await PerformanceChecks();
    }
    private void BuilderChecks()
    {
        var builder = new PlanetVisualBuilder(PlanetVisualCatalog.Load());
        var (star,planet,colony) = Fixture();
        string before = JsonSerializer.Serialize(new { star, colony });
        var visual = builder.Build(star,planet,colony);
        Check(visual == builder.Build(star,planet,colony), "Identical published observations produce identical visuals");
        Check(visual.SourceSeed == planet.Appearance!.SurfaceSeed && visual.Variant == planet.Appearance.Variant, "Saved descriptor retained");
        Check(visual.TerrainSeed != visual.CloudSeed && visual.TerrainSeed != visual.CitySeed && visual.CloudSeed != visual.CitySeed, "Independent sub-seeds");
        var larger = builder.Build(star,planet,colony with { Population = 20 });
        Check((larger with { CityLevel = visual.CityLevel }) == visual, "Population changes lights only");
        Check(builder.Build(star,planet,null).CityLevel == 0, "Unknown population produces no lights");
        Check(builder.Build(star with { Current = false },planet,null) == builder.Build(star,planet,null), "Stale observation never fetches live data");
        Check(builder.Build(star with { Current=false },planet,colony).CityLevel==0 && builder.Build(star with{Current=false},planet,colony with{Population=40}).CityLevel==0,"Stale observations suppress mixed current owned-population inputs");
        var foreign=planet with{OwnerId=new(99),ColonyId=new(88)};
        Check(builder.Build(star with{Planets=ImmutableArray.Create(foreign)},foreign,null).CityLevel==0 && builder.Build(star with{Current=false,Planets=ImmutableArray.Create(foreign)},foreign,null).CityLevel==0,"Foreign current/stale published worlds never infer population");
        foreach (var pair in new[] { (0,0f), (5,0.5f), (20,1f), (40,1f), (-1,0f) })
            Check(PlanetVisualBuilder.CityLevel(pair.Item1) == pair.Item2, "Development at population " + pair.Item1);
        Check(PlanetVisualBuilder.PackSeed(0xfedcba9876543210UL) == new Vector2I(unchecked((int)0x76543210),unchecked((int)0xfedcba98)), "Seed packing preserves both halves");
        Check(PlanetVisualBuilder.PackSeed(1) != PlanetVisualBuilder.PackSeed(1UL<<32), "High and low seed halves distinct");
        bool rejected = false;
        try { builder.Build(star,planet,colony with { PlanetId = new(99) }); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "Mismatched colony rejected");
        rejected = false;
        try { builder.Build(star with { Planets = null },planet,null); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "Planet outside published observation rejected");
        var unknown = builder.Build(star with { Planets = null },null,null);
        Check(unknown.Knowledge == PlanetVisualKnowledge.Unknown && unknown.SourceSeed == 0 && unknown.CityLevel == 0, "Unknown data carries no hidden identity");
        foreach (var changed in new[] { planet with { Appearance = null }, planet with { Appearance = new(99,0,123) }, planet with { EnvironmentId = "unknown" }, planet with { PlanetTypeId = "unknown" } })
        {
            var fallback = builder.Build(star with { Planets = ImmutableArray.Create(changed) },changed,null);
            Check(fallback.Knowledge == PlanetVisualKnowledge.LegacyPlaceholder && fallback.SourceSeed == 0 && fallback.CityLevel == 0 && fallback.AtmosphereDensity == 0, "Safe fallback for unsupported fields");
        }
        foreach (string type in new[] { "verdant_world", "forge_world", "crystal_world", "barren_rock" })
        foreach (string environment in new[] { "dry", "shelf_sea", "open_ocean" })
        {
            var item = planet with { PlanetTypeId = type, EnvironmentId = environment };
            var result = builder.Build(star with { Planets = ImmutableArray.Create(item) },item,null);
            Check(result.Knowledge == PlanetVisualKnowledge.Known, "Supported preset " + type + "/" + environment);
            if (environment == "dry" || type == "barren_rock") Check(!result.WaterEnabled, "Water disabled for " + type + "/" + environment);
            if (type == "barren_rock") Check(result.CloudCover == 0 && result.AtmosphereDensity == 0, "Airless layers disabled");
        }
        _ = PlanetStyleCatalog.Load("res://art/planets/planet_styles.json");
        Check(before == JsonSerializer.Serialize(new { star, colony }), "Published data unchanged; portrait catalog remains readable");
    }
    private void ClockChecks()
    {
        // A Node is not reference counted: Dispose alone would leave an orphan, so it is freed below.
        var clock = new PlanetAnimationClock();
        try {
        clock.Advance(4096-0.001); double before = clock.ClockTurn; clock.Advance(0.002);
        foreach (int rate in new[] { 32,64,67,85 })
        {
            double difference = ((clock.ClockTurn-before)*rate + 1000) % 1;
            Check(Math.Abs(difference-0.002/4096*rate) < 0.0001, "Clock wrap continuous at " + rate);
        }
        clock.Advance(7*24*60*60);
        Check(clock.ClockTurn >= 0 && clock.ClockTurn < 1, "Seven-day time stays bounded");
        clock.SetStatic(true); double phase = clock.ClockTurn; clock.Advance(123456);
        Check(clock.ClockTurn == phase, "Static mode freezes current phase");
        clock.SetStatic(false); clock.Advance(0);
        Check(clock.ClockTurn == phase, "Resume excludes frozen interval");
        clock.Advance(1);
        Check(Math.Abs(clock.ClockTurn-phase-1/4096.0) < 1e-10, "Clock resumes normally");
        } finally { clock.Free(); }
    }
    private async Task PrototypeChecks()
    {
        RenderingServer.GlobalShaderParameterSet("planet_clock_turn",0f);
        var fixture = Fixture();
        var visual = new PlanetVisualBuilder(PlanetVisualCatalog.Load()).Build(fixture.Star,fixture.Planet,fixture.Colony);
        var pair = new HBoxContainer { Position = new(30,30) }; AddChild(pair);
        var canvas = new PlanetGlobe2D { CustomMinimumSize = new(256,256) };
        var spatial = new PlanetGlobe3D { CustomMinimumSize = new(256,256) };
        pair.AddChild(canvas); pair.AddChild(spatial);
        canvas.SetVisual(visual); spatial.SetVisual(visual); spatial.SetStatic(true); spatial.SetActive(true);
        await Frames(8);
        ulong viewportId = spatial.GlobeViewport.GetInstanceId(), materialId = spatial.SurfaceMaterial.GetInstanceId();
        canvas.GlobeMaterial.SetShaderParameter("diagnostic",true); spatial.SurfaceMaterial.SetShaderParameter("diagnostic",true);
        foreach (var state in new[] { (0f,0f), (0.25f,0f), (0.125f,0.3f), (1f,0f) })
        {
            var current = visual with { Phase = state.Item1, Tilt = state.Item2, CloudCover = 0, AtmosphereDensity = 0 };
            canvas.SetVisual(current); spatial.SetVisual(current); await Frames(8);
            var frame = GetTree().Root.GetTexture().GetImage();
            var texture = spatial.GlobeViewport.GetTexture().GetImage();
            int matches = 0, compared = 0;
            for (int y = 3; y < 17; y++) for (int x = 3; x < 17; x++)
            {
                Vector2 uv = new(x/20f,y/20f);
                if ((uv-new Vector2(0.5f,0.5f)).Length() > 0.36f) continue;
                Vector2 point = canvas.GlobalPosition + uv*canvas.Size;
                Color a = frame.GetPixel((int)point.X,(int)point.Y);
                Color b = texture.GetPixel((int)(uv.X*texture.GetWidth()),(int)(uv.Y*texture.GetHeight()));
                if (Classification(a) == Classification(b)) matches++;
                compared++;
            }
            Check(compared > 50 && matches/(double)compared > 0.94, "Matching asymmetric pattern at " + state);
            string stem = state.Item1.ToString(System.Globalization.CultureInfo.InvariantCulture)+"-"+state.Item2.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Check(frame.SavePng(Path.Combine(_output,"diagnostic-"+stem+".png")) == Error.Ok, "Diagnostic capture " + stem);
        }
        canvas.GlobeMaterial.SetShaderParameter("diagnostic",false); spatial.SurfaceMaterial.SetShaderParameter("diagnostic",false);
        canvas.SetVisual(visual);
        for (int i = 0; i < 100; i++) spatial.SetVisual(visual with { CityLevel = i/100f });
        Check(viewportId == spatial.GlobeViewport.GetInstanceId() && materialId == spatial.SurfaceMaterial.GetInstanceId(), "100 switches retain viewport/material identities");
        Check(spatial.GlobeViewport.GetChildren().OfType<MeshInstance3D>().Count() == 3, "Three persistent sphere layers");
        spatial.SetActive(false);
        Check(spatial.GlobeViewport.RenderTargetUpdateMode == SubViewport.UpdateMode.Disabled, "Inactive viewport disabled");
        spatial.SetActive(true); await Frames(8);
        // Godot retains the requested Once property after the renderer consumes the frame.
        // Verify actual pixels instead of treating that cached property as render-completion evidence.
        byte[] frozen = spatial.GlobeViewport.GetTexture().GetImage().GetData();
        spatial.SurfaceMaterial.SetShaderParameter("diagnostic",true);
        await Frames(8);
        Check(frozen.SequenceEqual(spatial.GlobeViewport.GetTexture().GetImage().GetData()), "Static frame remains frozen after material change");
        spatial.SetVisual(visual);
        Check(spatial.GlobeViewport.RenderTargetUpdateMode == SubViewport.UpdateMode.Once, "Static data change requests another frame");
        await Frames(8);
        Check(!frozen.SequenceEqual(spatial.GlobeViewport.GetTexture().GetImage().GetData()), "Static data change renders a fresh frame");
        spatial.SurfaceMaterial.SetShaderParameter("diagnostic",false);
        spatial.SetStatic(false);
        Check(spatial.GlobeViewport.RenderTargetUpdateMode == SubViewport.UpdateMode.Always, "Animation enables updates");
        spatial.Hide();
        Check(spatial.GlobeViewport.RenderTargetUpdateMode == SubViewport.UpdateMode.Disabled, "Hidden viewport disabled");
        spatial.Show(); spatial.SetStatic(true); await Frames(8);
        Check(GetTree().Root.GetTexture().GetImage().SavePng(Path.Combine(_output,"surface-clouds-atmosphere.png")) == Error.Ok, "Layered prototype capture");
        pair.QueueFree(); await Frames();
    }
    private static int Classification(Color c)
    {
        if (c.R > c.G*1.8f && c.R > c.B*1.8f) return 1;
        if (c.G > c.R*1.8f && c.G > c.B*1.8f) return 2;
        if (c.B > c.R*1.8f && c.B > c.G*1.8f) return 3;
        if (c.R > 0.6f && c.G > 0.6f && c.B < 0.3f) return 4;
        return 0;
    }
    private async Task BrightnessChecks()
    {
        RenderingServer.GlobalShaderParameterSet("planet_clock_turn",0f);
        var fixture = Fixture();
        var original = new PlanetVisualBuilder(PlanetVisualCatalog.Load()).Build(fixture.Star,fixture.Planet,fixture.Colony);
        var uniform = original with { Lowland = new Color(0.5f,0.5f,0.5f), Highland = new Color(0.5f,0.5f,0.5f),
            WaterEnabled = false, IceCap = 0, CloudCover = 0, AtmosphereDensity = 0, CityLevel = 0, Phase = 0, Tilt = 0 };
        var host = new Control { Position = new(30,30), Size = new(540,256) }; AddChild(host);
        var background = new ColorRect { Size = host.Size }; host.AddChild(background);
        var pair = new HBoxContainer(); host.AddChild(pair);
        var canvas = new PlanetGlobe2D { CustomMinimumSize = new(256,256) };
        var spatial = new PlanetGlobe3D { CustomMinimumSize = new(256,256) };
        pair.AddChild(canvas); pair.AddChild(spatial);
        spatial.SetStatic(true); spatial.SetActive(true);
        var metrics = new System.Collections.Generic.List<object>();
        bool parity = true;
        foreach (var scenario in new[] { ("diagnostic",uniform,true), ("midtone",uniform,false), ("layers",original,false) })
        foreach (float backdrop in new[] { 0f,0.5f,1f })
        {
            background.Color = new(backdrop,backdrop,backdrop);
            canvas.GlobeMaterial.SetShaderParameter("diagnostic",scenario.Item3);
            canvas.SetVisual(scenario.Item2); spatial.SetVisual(scenario.Item2);
            spatial.SurfaceMaterial.SetShaderParameter("diagnostic",scenario.Item3);
            await Frames(8);
            using var frame = GetTree().Root.GetTexture().GetImage();
            using var raw = spatial.GlobeViewport.GetTexture().GetImage();
            string stem = scenario.Item1 + "-" + backdrop.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Check(frame.SavePng(Path.Combine(_output,"brightness-"+stem+".png")) == Error.Ok, "Brightness capture " + stem);
            Check(raw.SavePng(Path.Combine(_output,"brightness-raw-"+stem+".png")) == Error.Ok, "Raw viewport capture " + stem);
            double total = 0, nightTotal = 0, dayTotal = 0; int count = 0, nightCount = 0, dayCount = 0;
            for (int y = 20; y < 236; y += 3) for (int x = 20; x < 236; x += 3)
            {
                Vector2 xy = (new Vector2((x+0.5f)/256,(y+0.5f)/256)*2-Vector2.One)/0.85f;
                if (xy.Length() > 0.85f) continue;
                Vector2 aPos = canvas.GlobalPosition + new Vector2(x,y), bPos = spatial.GlobalPosition + new Vector2(x,y);
                Color a = frame.GetPixel((int)aPos.X,(int)aPos.Y), b = frame.GetPixel((int)bPos.X,(int)bPos.Y);
                double delta = (Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B))/3;
                total += delta; count++;
                float light = new Vector3(xy.X,-xy.Y,Mathf.Sqrt(1-xy.LengthSquared())).Dot(new(-0.696526f,0.398015f,0.597022f));
                if (light < 0) { nightTotal += delta; nightCount++; }
                if (light > 0.5f) { dayTotal += delta; dayCount++; }
            }
            double mean = total/count, nightMean = nightTotal/nightCount, dayMean = dayTotal/dayCount;
            // Uniform/diagnostic fields isolate colour conversion; layered noise also includes mesh sampling differences.
            // Compatibility internally decodes ALBEDO with a cubic approximation and re-encodes it.
            // For this gray swatch that round trip alone differs by up to 2.503/255 from exact sRGB;
            // allow four levels including pixel quantization and the sphere's interpolated normals.
            // Diagnostic colours retain the tighter bound; textured layers also have mesh/noise sampling error.
            double tolerance = scenario.Item1 switch { "midtone" => 4.0/255, "layers" => 0.025, _ => 0.008 };
            bool passed = mean < tolerance && nightMean < tolerance && dayMean < tolerance;
            parity &= passed;
            metrics.Add(new { scenario=stem, samples=count, meanRgbError=mean, nightRgbError=nightMean, dayRgbError=dayMean, tolerance, passed });
        }
        File.WriteAllText(Path.Combine(_output,"brightness-metrics.json"),JsonSerializer.Serialize(metrics,new JsonSerializerOptions { WriteIndented = true }));
        host.QueueFree(); await Frames();
        Check(parity,"Canvas and spatial brightness agree on dark/midtone/layered fixtures over black/gray/white");
    }
    private async Task CityChecks()
    {
        var fixture = Fixture();
        var visual = new PlanetVisualBuilder(PlanetVisualCatalog.Load()).Build(fixture.Star,fixture.Planet,fixture.Colony);
        var viewport = new SubViewport { Size = new(256,128), Disable3D = true, TransparentBg = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled };
        var material = PlanetGlobeMaterialBinder.Create("res://tests/OpenAntares.Game.Validation/PlanetGlobeSampling.gdshader");
        AddChild(viewport); viewport.AddChild(new ColorRect { Size = new(256,128), Material = material });
        byte[]? previous = null, initial = null;
        foreach (float level in new[] { 0f,0.25f,0.5f,1f,0.25f })
        {
            PlanetGlobeMaterialBinder.Bind(material,visual with { CityLevel = level });
            viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once; await Frames(5);
            var bytes = viewport.GetTexture().GetImage().GetData(); int lit = 0; bool monotonic = true, stable = true, eligible = true;
            for (int i = 0; i < bytes.Length; i += 4)
            {
                if (bytes[i+1] > 0) { lit++; eligible &= bytes[i] > 0; }
                if (previous is not null) { monotonic &= bytes[i+1] >= previous[i+1]; stable &= bytes[i] == previous[i] && bytes[i+2] == previous[i+2]; }
            }
            Check(eligible,"City sites obey the shared land mask at "+level);
            if (previous is not null) Check(stable, "Population preserves GPU terrain and mask at " + level);
            if (level != 0.25f && previous is not null) Check(monotonic, "GPU candidate field is monotonic at " + level);
            Check(level == 0 ? lit == 0 : lit > 0 && lit < 32768/3, "Sparse capped city coverage at " + level);
            if (level == 0.25f && initial is null) initial = bytes;
            else if (level == 0.25f) Check(bytes.SequenceEqual(initial!), "Regrowth recovers identical sites");
            previous = bytes;
        }
        viewport.QueueFree(); await Frames(); material.Dispose();
    }
}
