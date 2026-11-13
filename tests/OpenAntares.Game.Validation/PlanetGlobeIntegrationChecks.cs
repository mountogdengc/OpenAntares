using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Presentation;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Validation;

public partial class PlanetGlobeValidation
{
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (Node child in root.GetChildren(true)) {
            if (child is T item) yield return item;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static T Field<T>(object target,string name) => (T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(target)!;
    private static void Invoke(object target,string name,params object[] args) => target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(target,args);
    private static Button Key(Node root,string name) => Descendants<Button>(root).Single(b => b.Name == name);
    private void PressKey(Node root,string name) { var key=Key(root,name); Check(!key.Disabled,"Enabled route "+name); key.EmitSignal(BaseButton.SignalName.Pressed); }
    private async Task IntegrationChecks(bool colonyChecks)
    {
        foreach (var size in new[] { new Vector2I(800,600),new Vector2I(1920,1080) })
        {
            GetTree().Root.Size=size;
            var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();
            string preferences=Path.Combine(_output,"presentation-"+size.X+".cfg");
            main.GetType().GetProperty("PresentationPreferencesPath")?.SetValue(main,preferences);
            AddChild(main); await Frames(5);
            var setup=Descendants<NewGameScreen>(main).Single();
            setup.Configure("small",42,"harl",1,"peldra");
            Descendants<Button>(setup).Single(b=>b.Text=="Start").EmitSignal(BaseButton.SignalName.Pressed); await Frames(8);
            var screen=Descendants<GameScreen>(main).Single();
            var game=Field<GameSession>(screen,"_session"); var viewer=Field<EmpireId>(screen,"_viewer");
            var view=game.View(viewer); var home=view.Colonies[0]; var star=view.Stars.Single(s=>s.Chart.Id==home.StarId);
            string before=SaveGame.Serialize(game.Content,game.State);
            var keys=Descendants<Button>(screen).Where(b=>b.Name.ToString().StartsWith("SystemPlanet_")).ToArray();
            Check(keys.Length==star.Planets!.Value.Length && keys.All(k=>Descendants<PlanetGlobe2D>(k).Count()==1),"Every permitted planet key owns one canvas globe at "+size);
            foreach (var key in keys) {
                var globe=Descendants<PlanetGlobe2D>(key).Single();
                Check(globe.Size.X>=40 && globe.Size.Y>=40 && key.Size.Y>=44,"Real globe and touch target size "+key.Name);
                Check(globe.MouseFilter==MouseFilterEnum.Ignore && Descendants<Control>(globe).All(c=>c.MouseFilter==MouseFilterEnum.Ignore),"Globe children pass selection input through");
                Check(globe.GetGlobalRect().End.X<=key.GetGlobalRect().End.X+1 && globe.GetGlobalRect().End.Y<=key.GetGlobalRect().End.Y+1,"Globe fits its button");
                var style=key.GetThemeStylebox("normal");
                float required=key.GetThemeFont("font").GetStringSize(key.Text,fontSize:key.GetThemeFontSize("font_size")).X+style.GetContentMargin(Side.Left)+style.GetContentMargin(Side.Right)+globe.Size.X+key.GetThemeConstant("key_gap","PlanetGlobeMetrics");
                Check(required<=key.Size.X+1,"Orbit text and globe fit without overlap at "+size);
                Check(!globe.IsProcessing(),"No per-globe frame callback");
                Check(!globe.GlobeMaterial.GetShaderParameter("show_clouds").AsBool(),"Small sidebar globes omit clouds");
            }
            Check(!Descendants<PlanetGlobe3D>(screen).Any(),"Hidden colony refresh does not construct a viewport");
            var unowned=star.Planets.Value.First(p=>p.OwnerId!=viewer);
            var target=Key(screen,"SystemPlanet_"+unowned.Id.Value);
            foreach (var scroll in Descendants<ScrollContainer>(screen)) if (scroll.IsAncestorOf(target)) scroll.EnsureControlVisible(target);
            await Frames(5);
            Vector2 point=Descendants<PlanetGlobe2D>(target).Single().GetGlobalRect().GetCenter();
            Input.ParseInputEvent(new InputEventMouseMotion{Position=point});
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true,Position=point});
            Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false,Position=point}); await Frames(6);
            Check(Field<PlanetId?>(screen,"_selectedPlanet")==unowned.Id && Key(screen,"SystemPlanet_"+unowned.Id.Value).ButtonPressed,"Clicking globe area selects its existing planet button");
            Check(Key(screen,"ManageSelectedColony").Disabled,"Unowned planet keeps management disabled");
            Check(SaveGame.Serialize(game.Content,game.State)==before,"Globe selection leaves campaign and RNG unchanged");
            var map=Descendants<GalaxyMap>(screen).Single();
            if (view.Stars.FirstOrDefault(s=>s.Planets is null) is {} unknown) {
                map.Select(unknown.Chart.Id); await Frames(5);
                Check(!Descendants<Button>(screen).Any(b=>b.Name.ToString().StartsWith("SystemPlanet_")),"Unsurveyed system does not invent planet keys");
            }
            map.Select(home.StarId); await Frames(5);
            PressKey(screen,"SystemPlanet_"+home.PlanetId.Value); await Frames(4);
            Check(GetTree().Root.GetTexture().GetImage().SavePng(Path.Combine(_output,"sidebar-"+size.X+".png"))==Error.Ok,"Sidebar real-size capture");
            var clock=Descendants<PlanetAnimationClock>(main).Single();
            Check(Descendants<PlanetAnimationClock>(GetTree().Root).Count()==1,"Exactly one Main-owned clock");
            var panel=Descendants<ColonyPanel>(screen).Single();
            if (colonyChecks) {
                PressKey(screen,"ManageSelectedColony"); await Frames(8);
                var globe=Descendants<PlanetGlobe3D>(panel).Single(); ulong viewport=globe.GlobeViewport.GetInstanceId(), material=globe.SurfaceMaterial.GetInstanceId();
                Check(globe.IsVisibleInTree() && globe.GlobeViewport.RenderTargetUpdateMode==SubViewport.UpdateMode.Always,"Opening colony lazily enables its globe");
                Check(!Descendants<PlanetPortrait>(panel).Any(),"Only this colony panel replaces its old portrait");
                Invoke(screen,"Refresh"); await Frames(5);
                Check(viewport==Descendants<PlanetGlobe3D>(panel).Single().GlobeViewport.GetInstanceId(),"Ordinary Refresh retains viewport");
                var production=Key(panel,"ProductionWorkforceMore");
                if (!production.Disabled) { production.EmitSignal(BaseButton.SignalName.Pressed); await Frames(6); Check(game.View(viewer).Colonies[0].Workforce!=home.Workforce,"Real workforce action remains functional"); }
                Check(globe.GlobeViewport.GetInstanceId()==viewport,"Workforce refresh retains viewport");
                PressKey(screen,"EndTurn"); await Frames(8);
                Check(game.State.Turn>view.Turn,"Real End Turn remains functional");
                Check(globe.GlobeViewport.GetInstanceId()==viewport,"End Turn retains viewport");
                Invoke(screen,"ShowWorkspace","Colony"); await Frames(6);
                Check(GetTree().Root.GetTexture().GetImage().SavePng(Path.Combine(_output,"colony-"+size.X+".png"))==Error.Ok,"Colony real-size capture");
                // Clearly labelled published-view supplements; no live state is modified.
                string afterCommands=SaveGame.Serialize(game.Content,game.State);
                var current=game.View(viewer); var own=current.Colonies[0]; var observed=current.Stars.Single(s=>s.Chart.Id==own.StarId);
                for (int i=0;i<100;i++) {
                    panel.ShowColony(game.Content,own with{Population=i%21},current);
                    if(i%10==0) await Frames();
                }
                Check(Math.Abs(globe.SurfaceMaterial.GetShaderParameter("city_level").AsSingle()-PlanetVisualBuilder.CityLevel(15))<0.0001,"Open colony population refresh updates lights in place");
                Check(globe.GlobeViewport.GetInstanceId()==viewport && globe.SurfaceMaterial.GetInstanceId()==material,"100 published population refreshes retain viewport/material");
                panel.ShowColony(game.Content,own,current);
                float cityBefore=globe.SurfaceMaterial.GetShaderParameter("city_level").AsSingle();
                panel.ShowColony(game.Content,own with{Economy=own.Economy with{Growth=own.Economy.Growth with{Capacity=own.Economy.Growth.Capacity+100}}},current);
                Check(globe.SurfaceMaterial.GetShaderParameter("city_level").AsSingle()==cityBefore,"Capacity-only published update does not dim city lights");
                var ownPlanet=observed.Planets!.Value.Single(p=>p.Id==own.PlanetId);
                var altPlanet=observed.Planets.Value.First(p=>p.Id!=own.PlanetId) with{OwnerId=viewer,ColonyId=new(99999),PlanetTypeId="barren_rock",Appearance=new(1,5,ownPlanet.Appearance!.SurfaceSeed^0x5a5a5a5a5a5a5a5aUL)};
                var altColony=own with{Id=new(99999),PlanetId=altPlanet.Id};
                var switchedStar=observed with{Planets=observed.Planets.Value.Select(p=>p.Id==altPlanet.Id?altPlanet:p).ToImmutableArray()};
                var twoOwned=current with{Stars=current.Stars.Select(s=>s.Chart.Id==observed.Chart.Id?switchedStar:s).ToImmutableArray(),Colonies=current.Colonies.Add(altColony)};
                var layers=globe.GlobeViewport.GetChildren().OfType<MeshInstance3D>().OrderBy(m=>m.Name.ToString(),StringComparer.Ordinal).ToArray();
                var nodeIds=layers.Select(m=>m.GetInstanceId()).ToArray();var meshIds=layers.Select(m=>m.Mesh.GetInstanceId()).ToArray();var materialIds=layers.Select(m=>m.MaterialOverride.GetInstanceId()).ToArray();
                var visualBuilder=new PlanetVisualBuilder(PlanetVisualCatalog.Load());
                for(int i=0;i<100;i++) {
                    var selected=i%2==0?own:altColony;var planet=i%2==0?ownPlanet:altPlanet;
                    panel.ShowColony(game.Content,selected,twoOwned);
                    var expected=visualBuilder.Build(switchedStar,planet,selected);
                    Check(globe.SurfaceMaterial.GetShaderParameter("terrain_seed").AsVector2I()==expected.TerrainSeed && globe.SurfaceMaterial.GetShaderParameter("surface_mode").AsInt32()==(int)expected.SurfaceMode,"Alternating owned planet updates visible seed/preset "+i);
                    var now=globe.GlobeViewport.GetChildren().OfType<MeshInstance3D>().OrderBy(m=>m.Name.ToString(),StringComparer.Ordinal).ToArray();
                    Check(now.Select(m=>m.GetInstanceId()).SequenceEqual(nodeIds)&&now.Select(m=>m.Mesh.GetInstanceId()).SequenceEqual(meshIds)&&now.Select(m=>m.MaterialOverride.GetInstanceId()).SequenceEqual(materialIds),"All three layer nodes/meshes/materials survive planet switch "+i);
                    if(i%10==0)await Frames();
                }
                panel.ShowColony(game.Content,own,current);
                var legacy=observed with{Planets=observed.Planets!.Value.Select(p=>p.Id==own.PlanetId?p with{Appearance=null}:p).ToImmutableArray()};
                panel.ShowColony(game.Content,own,current with{Stars=current.Stars.Select(s=>s.Chart.Id==legacy.Chart.Id?legacy:s).ToImmutableArray()});
                Check(globe.SurfaceMaterial.GetShaderParameter("city_level").AsSingle()==0,"Legacy appearance placeholder carries no lights");
                panel.Clear(); await Frames(); Check(!globe.Visible && globe.GlobeViewport.RenderTargetUpdateMode==SubViewport.UpdateMode.Disabled,"No-selection Clear disables retained globe");
                panel.ShowColony(game.Content,own,current); await Frames();
                Invoke(screen,"ShowWorkspace","Galaxy"); await Frames();
                Check(globe.GlobeViewport.RenderTargetUpdateMode==SubViewport.UpdateMode.Disabled,"Leaving colony page stops viewport updates");
                Invoke(screen,"ShowWorkspace","Colony"); await Frames(); Check(globe.GlobeViewport.GetInstanceId()==viewport,"Reopening colony reuses viewport");
                Check(SaveGame.Serialize(game.Content,game.State)==afterCommands,"Published fixture refreshes/navigation do not mutate campaign or RNG");
                var loaded=SaveGame.Deserialize(afterCommands);
                Check(loaded.Game is not null,"Current saved campaign loads unchanged after globe navigation");
                var restored=new GameSession(loaded.Game!.Content,loaded.Game.State).View(viewer);
                var restoredStar=restored.Stars.Single(s=>s.Chart.Id==own.StarId);
                var catalog=PlanetVisualCatalog.Load(); var builder=new PlanetVisualBuilder(catalog);
                Check(builder.Build(observed,observed.Planets!.Value.Single(p=>p.Id==own.PlanetId),own)==builder.Build(restoredStar,restoredStar.Planets!.Value.Single(p=>p.Id==own.PlanetId),restored.Colonies.Single(c=>c.Id==own.Id)),"Save/load reconstructs identical globe geography and sites");
            }
            var menu=Descendants<MenuButton>(screen).Single(m=>m.Name=="GameMenu"); var popup=menu.GetPopup();
            Check(popup.GetItemIndex(6)>=0 && popup.IsItemChecked(popup.GetItemIndex(6)),"Animation enabled by default in old/missing preferences");
            popup.EmitSignal(PopupMenu.SignalName.IdPressed,6L); await Frames(); double frozen=clock.ClockTurn;
            await Frames(6); Check(clock.IsStatic && clock.ClockTurn==frozen,"Animation setting freezes shared phase");
            popup.EmitSignal(PopupMenu.SignalName.IdPressed,4L); await Frames();
            var saved=new ConfigFile(); Check(saved.Load(preferences)==Error.Ok && !saved.GetValue("display","planet_animation").AsBool(),"Reduced effects preserves stored animation choice");
            popup.EmitSignal(PopupMenu.SignalName.IdPressed,4L); await Frames();
            ulong clockId=clock.GetInstanceId(); popup.EmitSignal(PopupMenu.SignalName.IdPressed,5L); await Frames(6);
            Check(!Descendants<PlanetGlobe3D>(main).Any() && clock.GetInstanceId()==clockId,"Main menu frees game viewport and retains one clock");
            PressKey(main,"ResumeCampaign"); await Frames(6);
            var resumed=Descendants<GameScreen>(main).Single();
            var resumedPopup=Descendants<MenuButton>(resumed).Single(m=>m.Name=="GameMenu").GetPopup();
            Check(!resumedPopup.IsItemChecked(resumedPopup.GetItemIndex(6)) && clock.IsStatic,"Persisted animation preference applies to future screen");
            resumedPopup.EmitSignal(PopupMenu.SignalName.IdPressed,6L); await Frames(6); Check(!clock.IsStatic && clock.ClockTurn!=frozen,"Animation resumes without advancing through frozen interval");
            foreach(string legacyName in new[]{"legacy-v1-save.json","legacy-v2-save.json"}) {
                var old=SaveGame.Deserialize(Godot.FileAccess.GetFileAsString("res://tests/OpenAntares.Simulation.Tests/Fixtures/"+legacyName));
                Check(old.Game is not null,"Actual legacy save loads: "+legacyName);
                var legacyGame=new GameSession(old.Game!.Content,old.Game.State);string legacyBefore=SaveGame.Serialize(legacyGame.Content,legacyGame.State);
                Invoke(main,"ShowGame",legacyGame);await Frames(6);
                var legacyScreen=Descendants<GameScreen>(main).Single();PressKey(legacyScreen,"ManageSelectedColony");await Frames(6);
                var legacyGlobe=Descendants<PlanetGlobe3D>(legacyScreen).Single();
                Check(legacyGlobe.SurfaceMaterial.GetShaderParameter("city_level").AsSingle()==0,"Actual older save uses neutral unlit fallback: "+legacyName);
                Check(SaveGame.Serialize(legacyGame.Content,legacyGame.State)==legacyBefore,"Legacy globe rendering leaves save and RNG unchanged");
            }
            RemoveChild(main); main.QueueFree(); await Frames(6);
            Check(!Descendants<PlanetAnimationClock>(this).Any() && !Descendants<PlanetGlobe3D>(this).Any(),"Game teardown releases feature node ownership");
        }
    }
}
