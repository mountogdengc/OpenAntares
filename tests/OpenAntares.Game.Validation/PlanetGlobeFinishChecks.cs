using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Presentation;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Validation;

public partial class PlanetGlobeValidation
{
    private async Task ReviewRegressions()
    {
        var fixture=Fixture();var builder=new PlanetVisualBuilder(PlanetVisualCatalog.Load());
        var stale=fixture.Star with{Current=false};
        bool staleSafe=builder.Build(stale,fixture.Planet,fixture.Colony).CityLevel==0 && builder.Build(stale,fixture.Planet,fixture.Colony with{Population=20}).CityLevel==0;
        var viewport=new SubViewport{Size=new(256,128),Disable3D=true,TransparentBg=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Disabled};
        var material=PlanetGlobeMaterialBinder.Create("res://tests/OpenAntares.Game.Validation/PlanetGlobeSampling.gdshader");
        AddChild(viewport);viewport.AddChild(new ColorRect{Size=new(256,128),Material=material});
        PlanetGlobeMaterialBinder.Bind(material,builder.Build(fixture.Star,fixture.Planet,fixture.Colony));
        material.SetShaderParameter("structural_variant",1);viewport.RenderTargetUpdateMode=SubViewport.UpdateMode.Once;await Frames(5);
        byte[] first=viewport.GetTexture().GetImage().GetData();
        material.SetShaderParameter("structural_variant",5);viewport.RenderTargetUpdateMode=SubViewport.UpdateMode.Once;await Frames(5);
        bool variantsDiffer=!first.SequenceEqual(viewport.GetTexture().GetImage().GetData());
        viewport.QueueFree();await Frames();material.Dispose();
        File.WriteAllText(Path.Combine(_output,"review-regressions.json"),JsonSerializer.Serialize(new{staleSafe,variantsDiffer}));
        Check(staleSafe,"Stale observation refuses current population even when an owned-colony input is supplied");
        Check(variantsDiffer,"Same-seed saved structural signatures1 and5 remain distinct");
    }
    private async Task LayerLifecycleChecks()
    {
        RenderingServer.GlobalShaderParameterSet("planet_clock_turn",0f);
        var fixture=Fixture(); var visual=new PlanetVisualBuilder(PlanetVisualCatalog.Load()).Build(fixture.Star,fixture.Planet,fixture.Colony);
        var host=new Control { Position=new(30,30),Size=new(600,400) }; AddChild(host);
        var background=new ColorRect { Size=host.Size }; host.AddChild(background);
        var canvas=new PlanetGlobe2D { Size=new(256,256) }; host.AddChild(canvas);
        var spatial=new PlanetGlobe3D { Position=new(280,0),Size=new(256,256) }; host.AddChild(spatial);
        canvas.SetVisual(visual); spatial.SetVisual(visual); spatial.SetStatic(true); spatial.SetActive(true); await Frames(8);
        ulong viewport=spatial.GlobeViewport.GetInstanceId(),material=spatial.SurfaceMaterial.GetInstanceId(),quad=canvas.GlobeMaterial.GetInstanceId();
        foreach (int side in new[] { 32,40,64,128,512,1000 }) {
            spatial.Size=new(side,side); canvas.Size=new(Math.Min(side,128),Math.Min(side,128)); await Frames(6);
            Check(spatial.GlobeViewport.Size.X==Math.Clamp(side,32,256),"Viewport uses displayed size and themed resolution cap "+side);
            Check(canvas.GlobeMaterial.GetShaderParameter("show_clouds").AsBool()==(Math.Min(side,128)>=64),"Cloud size cutoff "+side);
        }
        canvas.Size=new(256,256);spatial.Size=new(256,256); await Frames(6);
        var metrics=new List<object>();
        foreach(float backdrop in new[] {0f,1f}) {
            background.Color=new(backdrop,backdrop,backdrop);canvas.SetVisual(visual);spatial.SetVisual(visual);await Frames(8);
            using var frame=GetTree().Root.GetTexture().GetImage(); using var raw=spatial.GlobeViewport.GetTexture().GetImage();
            double sum=0,alpha=0;int samples=0;
            for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
                float radius=((new Vector2((x+0.5f)/256,(y+0.5f)/256)*2-Vector2.One)/0.85f).Length();
                if(radius<1.03f||radius>1.065f)continue;
                Color a=frame.GetPixel(30+x,30+y),b=frame.GetPixel(310+x,30+y);
                sum+=(Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B))/3;
                alpha+=raw.GetPixel(x,y).A;samples++;
            }
            double error=sum/samples,coverage=alpha/samples;
            metrics.Add(new{backdrop,haloRgbError=error,haloAlpha=coverage,samples});
            Check(coverage>0 && coverage<0.1,"Transparent spatial halo has sparse nonzero coverage");
            Check(error<0.012,"Canvas/spatial halo compositing agrees over "+backdrop);
            Check(frame.SavePng(Path.Combine(_output,"halo-"+backdrop+".png"))==Error.Ok,"Transparent halo capture");
        }
        File.WriteAllText(Path.Combine(_output,"halo-metrics.json"),JsonSerializer.Serialize(metrics,new JsonSerializerOptions{WriteIndented=true}));
        var airless=visual with{CloudCover=0,AtmosphereDensity=0};canvas.SetVisual(airless);spatial.SetVisual(airless);await Frames(6);
        Check(spatial.GlobeViewport.GetChildren().OfType<MeshInstance3D>().Where(m=>m.Name!="Planetsurface").All(m=>!m.Visible),"Zero cloud/atmosphere values disable spatial layers");
        host.Hide();await Frames();Check(!canvas.IsVisibleInTree()&&spatial.GlobeViewport.RenderTargetUpdateMode==SubViewport.UpdateMode.Disabled,"Hidden ancestor stops both views and viewport work");
        host.Show();await Frames();
        RemoveChild(host); await Frames();AddChild(host);await Frames(8);
        Check(viewport==spatial.GlobeViewport.GetInstanceId()&&material==spatial.SurfaceMaterial.GetInstanceId()&&quad==canvas.GlobeMaterial.GetInstanceId(),"Tree re-entry retains viewport and both materials");
        Check(Descendants<ColorRect>(canvas).Single().Material==canvas.GlobeMaterial,"Canvas re-entry remains bound");
        // A real-size gallery includes every supported type plus clearly labelled renderer-only modes.
        spatial.SetActive(false);host.QueueFree();await Frames();
        var gallery=new VBoxContainer {Position=new(20,20)};AddChild(gallery);
        var builder=new PlanetVisualBuilder(PlanetVisualCatalog.Load());
        var examples=new List<(string,PlanetVisual)>();
        foreach(string type in new[]{"verdant_world","forge_world","crystal_world","barren_rock"}) {
            var planet=fixture.Planet with{PlanetTypeId=type};
            examples.Add((type,builder.Build(fixture.Star with{Planets=ImmutableArray.Create(planet)},planet,fixture.Colony)));
        }
        examples.Add(("Banded synthetic",visual with{SurfaceMode=PlanetSurfaceMode.Banded}));
        examples.Add(("All-water synthetic",visual with{AllWater=true}));
        foreach(var mode in examples) {
            var row=new HBoxContainer();gallery.AddChild(row);row.AddChild(new Label{Text=mode.Item1,CustomMinimumSize=new(180,0)});
            foreach(int size in new[]{32,40,64,128}) {var globe=new PlanetGlobe2D{CustomMinimumSize=new(size,size)};row.AddChild(globe);globe.SetVisual(mode.Item2);}
        }
        await Frames(8);Check(GetTree().Root.GetTexture().GetImage().SavePng(Path.Combine(_output,"real-size-gallery.png"))==Error.Ok,"32/40/64/128 pixel globe gallery");
        gallery.QueueFree();await Frames();
        var rotation=new HBoxContainer {Position=new(30,30)};AddChild(rotation);
        var small=new PlanetGlobe2D {CustomMinimumSize=new(40,40)};rotation.AddChild(small);
        var detail=new PlanetGlobe3D {CustomMinimumSize=new(128,128)};rotation.AddChild(detail);detail.SetStatic(true);detail.SetActive(true);
        var counts=new List<object>();int smallTotal=0,detailTotal=0,best=0;
        for(int phase=0;phase<24;phase++) {
            var current=visual with{Phase=phase/24f,CityLevel=0,CloudCover=0,AtmosphereDensity=0};
            small.SetVisual(current);detail.SetVisual(current);await Frames(4);
            using var dark=GetTree().Root.GetTexture().GetImage();
            current=current with{CityLevel=PlanetVisualBuilder.CityLevel(1)};
            small.SetVisual(current);detail.SetVisual(current);await Frames(4);
            using var lit=GetTree().Root.GetTexture().GetImage();
            int Count(Control control) {
                int count=0;for(int y=0;y<(int)control.Size.Y;y++)for(int x=0;x<(int)control.Size.X;x++) {
                    Vector2 p=control.GlobalPosition+new Vector2(x,y);
                    Color a=dark.GetPixel((int)p.X,(int)p.Y),b=lit.GetPixel((int)p.X,(int)p.Y);
                    if(b.R-a.R>1f/255||b.G-a.G>1f/255||b.B-a.B>1f/255)count++;
                }return count;
            }
            int smallLit=Count(small),detailLit=Count(detail);smallTotal+=smallLit;detailTotal+=detailLit;
            counts.Add(new{phase=phase/24f,smallLitPixels=smallLit,detailLitPixels=detailLit});
            if(smallLit>best){best=smallLit;Check(lit.SavePng(Path.Combine(_output,"small-colony-lights.png"))==Error.Ok,"Small colony night-light capture");}
        }
        File.WriteAllText(Path.Combine(_output,"small-colony-rotation.json"),JsonSerializer.Serialize(counts,new JsonSerializerOptions{WriteIndented=true}));
        Check(smallTotal>0&&detailTotal>0,"One-population colony shows night lights over a full rotation at40/128px");
        rotation.QueueFree();await Frames();
    }
    private sealed record Timing(string Mode,int Frames,double Seconds,double FrameP95,double CpuP95,double RenderCpuP95,double? GpuP95,double DrawCalls,int Nodes,int Resources);
    private static double Percentile(List<double> values) { values.Sort(); return values[(int)Math.Ceiling(values.Count*0.95)-1]; }
    private async Task<Timing> Measure(string mode,PlanetGlobe3D? globe)
    {
        var root=GetTree().Root; var rid=root.GetViewportRid();
        RenderingServer.ViewportSetMeasureRenderTime(rid,true);
        bool drawGlobe=globe is not null && globe.IsVisibleInTree() && globe.GlobeViewport.RenderTargetUpdateMode==SubViewport.UpdateMode.Always;
        if(drawGlobe)RenderingServer.ViewportSetMeasureRenderTime(globe!.GlobeViewport.GetViewportRid(),true);
        var warm=Stopwatch.StartNew();while(warm.Elapsed.TotalSeconds<2)await Frames(1);
        var wall=new List<double>();var cpu=new List<double>();var render=new List<double>();var gpu=new List<double>();var calls=new List<double>();
        var timer=Stopwatch.StartNew();double previous=timer.Elapsed.TotalMilliseconds;
        while(timer.Elapsed.TotalSeconds<30) {
            await Frames(1); double now=timer.Elapsed.TotalMilliseconds;wall.Add(now-previous);previous=now;
            cpu.Add(Performance.GetMonitor(Performance.Monitor.TimeProcess)*1000);
            double renderCpu=RenderingServer.GetFrameSetupTimeCpu()+RenderingServer.ViewportGetMeasuredRenderTimeCpu(rid);
            double renderGpu=RenderingServer.ViewportGetMeasuredRenderTimeGpu(rid);
            if(drawGlobe) { var child=globe!.GlobeViewport.GetViewportRid();renderCpu+=RenderingServer.ViewportGetMeasuredRenderTimeCpu(child);renderGpu+=RenderingServer.ViewportGetMeasuredRenderTimeGpu(child); }
            render.Add(renderCpu);gpu.Add(renderGpu);
            calls.Add(RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame));
        }
        var result=new Timing(mode,wall.Count,timer.Elapsed.TotalSeconds,Percentile(wall),Percentile(cpu),Percentile(render),gpu.Any(x=>x>0)?Percentile(gpu):null,calls.Average(),(int)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),(int)Performance.GetMonitor(Performance.Monitor.ObjectResourceCount));
        File.WriteAllText(Path.Combine(_output,mode+"-timing.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
        GD.Print("Measured "+mode+": p95 frame "+result.FrameP95+"ms, CPU "+result.CpuP95+"ms, GPU "+result.GpuP95);
        Check(result.Seconds>=30&&result.Frames>100,"Complete warmed 30-second measurement "+mode);
        return result;
    }
    private static int VisibleCanvasCount(Node screen)
    {
        return Descendants<PlanetGlobe2D>(screen).Count(globe=> {
            if(!globe.IsVisibleInTree())return false;
            Rect2 area=globe.GetGlobalRect();
            for(Node? parent=globe.GetParent();parent is not null;parent=parent.GetParent())
                if(parent is Control control && control.ClipContents)area=area.Intersection(control.GetGlobalRect());
            return area.HasArea();
        });
    }
    private async Task PerformanceChecks()
    {
        GetTree().Root.Size=new(1920,1080);
        var results=new List<object>();bool budgets=true;
        foreach(int planets in new[]{4,50}) {
            // Loader-validated synthetic content, entirely in memory. Core JSON files are never changed.
            var sources=DirAccess.GetFilesAt(ContentFiles.CoreDirectory).Where(n=>n.EndsWith(".json",StringComparison.Ordinal)).Select(name=> {
                var json=System.Text.Json.Nodes.JsonNode.Parse(Godot.FileAccess.GetFileAsString(ContentFiles.CoreDirectory+"/"+name))!;
                if(json["galaxy"] is {} galaxy) {galaxy["min_planets_per_star"]=planets;galaxy["max_planets_per_star"]=planets;}
                return new ContentSource("synthetic-perf-"+name,json.ToJsonString());
            }).ToArray();
            var loaded=ContentLoader.Load(sources,requireSpecies:true,requireRivals:true,requireDevelopment:true,requireCrises:true,requireScouting:true,requireFleets:true,requireCombat:true,requireShipDesigns:true,requireDiplomacy:true,requireConflict:true);
            Check(loaded.Content is not null,"Loader accepts synthetic "+planets+"-planet fixture");
            var generated=GalaxyGenerator.CreateNewGame(loaded.Content!,new NewGameSettings("small",42,SpeciesId:"harl",RivalCount:1,RivalSpeciesId:"peldra",AmebasEnabled:false,SentriesEnabled:false));
            Check(generated.State is not null,"Generated valid stress campaign: "+generated.Error);
            var game=new GameSession(loaded.Content!,generated.State!);
            var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();
            main.GetType().GetProperty("PresentationPreferencesPath")!.SetValue(main,Path.Combine(_output,"perf-"+planets+".cfg"));
            AddChild(main);await Frames(5);Invoke(main,"ShowGame",game);await Frames(8);
            var screen=Descendants<GameScreen>(main).Single();var view=game.View(Field<EmpireId>(screen,"_viewer"));var own=view.Colonies[0];var star=view.Stars.Single(s=>s.Chart.Id==own.StarId);
            string snapshot=OpenAntares.Simulation.Saves.SaveGame.Serialize(game.Content,game.State);
            var globes=Descendants<PlanetGlobe2D>(screen).ToArray();Check(globes.Length==planets,"Actual system card has "+planets+" globes");
            var styles=PlanetStyleCatalog.Load("res://art/planets/planet_styles.json");var cache=Field<PlanetPortraitCache>(screen,"_portraitCache");
            var staticKeys=new List<TextureRect>();
            foreach(var globe in globes) {
                int id=int.Parse(globe.GetParent().Name.ToString()[13..],System.Globalization.CultureInfo.InvariantCulture);
                var known=star.Planets!.Value.Single(p=>p.Id.Value==id);var appearance=known.Appearance!;
                var image=new TextureRect {Texture=cache.Get(appearance,styles.Resolve(known.PlanetTypeId,known.EnvironmentId,appearance.Variant),new(96,96)),Size=globe.Size,Position=globe.Position,ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=MouseFilterEnum.Ignore,Visible=false};
                globe.GetParent().AddChild(image);staticKeys.Add(image);
            }
            foreach(string page in new[]{"Sidebar","Colony"}) {
                if(page=="Colony") {
                    PressKey(screen,"ManageSelectedColony");await Frames(8);
                    globes=Descendants<PlanetGlobe2D>(screen).ToArray();staticKeys.Clear();
                }
                var colony=Descendants<PlanetGlobe3D>(screen).SingleOrDefault();TextureRect? staticColony=null;
                if(colony is not null) {
                    var known=star.Planets!.Value.Single(p=>p.Id==own.PlanetId);var appearance=known.Appearance!;
                    staticColony=new TextureRect {Texture=cache.Get(appearance,styles.Resolve(known.PlanetTypeId,known.EnvironmentId,appearance.Variant),new(256,256)),CustomMinimumSize=colony.CustomMinimumSize,ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=MouseFilterEnum.Ignore,Visible=false};colony.GetParent().AddChild(staticColony);
                }
                int visible=VisibleCanvasCount(screen);
                foreach(var globe in globes)globe.Hide();foreach(var image in staticKeys)image.Show();
                if(colony is not null){colony.Hide();colony.SetActive(false);staticColony!.Show();}
                var baseline=await Measure("p"+planets+"-"+page+"-static-portrait",colony);
                foreach(var image in staticKeys)image.Hide();foreach(var globe in globes)globe.Show();
                if(colony is not null){staticColony!.Hide();colony.Show();colony.SetStatic(false);colony.SetActive(true);}
                var feature=await Measure("p"+planets+"-"+page+"-globes",colony);
                double cpuAdded=feature.CpuP95-baseline.CpuP95;
                double? gpuAdded=feature.GpuP95.HasValue&&baseline.GpuP95.HasValue?feature.GpuP95-baseline.GpuP95:null;
                bool passed=feature.FrameP95-baseline.FrameP95<=2 && cpuAdded<=2 && (!gpuAdded.HasValue||gpuAdded<=2) && (baseline.FrameP95>16.7||feature.FrameP95<=16.7);
                budgets&=passed;
                results.Add(new{planets,page,totalCanvasGlobes=globes.Length,visibleCanvasGlobes=visible,colonyViewport=colony?.GlobeViewport.Size.ToString(),baseline,feature,cpuAdded,gpuAdded,passed,method="Same actual GameScreen, matched layout, warmed cached static-portrait baseline; driver, VSync and frame cap unchanged"});
                File.WriteAllText(Path.Combine(_output,"performance.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
                Check(Descendants<PlanetGlobe3D>(screen).Count()<=1,"Performance route retains at most one colony viewport");
                if(staticColony is not null){staticColony.QueueFree();await Frames();}
            }
            Check(OpenAntares.Simulation.Saves.SaveGame.Serialize(game.Content,game.State)==snapshot,"Performance rendering preserves campaign and RNG");
            RemoveChild(main);main.QueueFree();await Frames(6);
        }
        Check(budgets,"Four/50-planet matched 30-second rendering budgets");
    }
}
