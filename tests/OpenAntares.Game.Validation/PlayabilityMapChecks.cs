using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Validation;

internal static class PlayabilityMapChecks
{
    public static async Task Run(Node host, string output, Action<bool, string> check)
    {
        Assembly assembly = typeof(GalaxyMap).Assembly;
        Type? candidate = assembly.GetType("OpenAntares.Game.Presentation.MapLabelCandidate");
        Type? layout = assembly.GetType("OpenAntares.Game.Presentation.MapLabelLayout");
        check(candidate is not null && layout is not null, "CrowdedLabelsAreStableAndBounded: deterministic label layout exists");
        var ctor = candidate!.GetConstructor(new[] { typeof(StarId), typeof(Rect2), typeof(bool), typeof(bool) })!;
        var inputs = Array.CreateInstance(candidate, 5);
        object Row(int id, Rect2 rect, bool selected = false, bool home = false) => ctor.Invoke(new object[] { new StarId(id), rect, selected, home });
        inputs.SetValue(Row(3, new Rect2(10, 10, 80, 20)), 0);
        inputs.SetValue(Row(2, new Rect2(10, 10, 80, 20), selected: true), 1);
        inputs.SetValue(Row(1, new Rect2(10, 10, 80, 20), home: true), 2);
        inputs.SetValue(Row(4, new Rect2(190, 20, 80, 20)), 3);
        inputs.SetValue(Row(5, new Rect2(295, 190, 80, 20)), 4);
        var place = layout!.GetMethod("Place", BindingFlags.Public | BindingFlags.Static)!;
        StarId[] Place(Array rows) => ((IEnumerable)place.Invoke(null, new object[] { rows, new Rect2(0, 0, 300, 200) })!).Cast<StarId>().ToArray();
        check(Place(inputs).SequenceEqual(new[] { new StarId(2), new StarId(4) }), "Selected label wins overlap, routine labels are bounded");
        Array.Reverse(inputs);
        check(Place(inputs).SequenceEqual(new[] { new StarId(2), new StarId(4) }), "Input ordering cannot change label placement");

        EventInfo? selected = typeof(GalaxyMap).GetEvent("OwnedVesselsSelected");
        check(selected is not null, "OwnedMarkerChooserIsStableAndStarFallbackWorks: explicit marker selection exists");
        foreach (Vector2I size in new[] { new Vector2I(800, 600), new Vector2I(1152, 648), new Vector2I(1600, 900), new Vector2I(1920, 1080) })
        {
            host.GetTree().Root.Size = size;
            var map = new GalaxyMap { Position = new(12, 12), Size = new(size.X - 24, size.Y - 24) };
            host.AddChild(map);
            var view = new GalaxyMapView(ImmutableArray.Create(
                new ChartedStar(new(1), "Known home with a very long but fully discoverable system name", 0, 0, null),
                new ChartedStar(new(2), "Known second system", 100, 0, null),
                new ChartedStar(new(3), "Known transit destination", 50, 100, null)), null, null, null, null, ImmutableArray.Create(new StarId(1)))
            {
                Units = ImmutableArray.Create(
                    new MapVesselMarker(new(9), "scout", true, new StarId(1), null, null, 0),
                    new MapVesselMarker(new(2), "scout", true, new StarId(1), null, null, 0),
                    new MapVesselMarker(new(10), "scout", false, new StarId(2), null, null, 0),
                    new MapVesselMarker(new(11), "scout", true, null, new StarId(2), new StarId(3), 3))
            };
            map.ShowGalaxy(view);
            var chosen = new List<StarId>();
            Action<StarId> handler = star => chosen.Add(star);
            selected!.AddEventHandler(map, handler);
            for (int i = 0; i < 4; i++) await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            PropertyInfo? markerBounds = typeof(GalaxyMap).GetProperty("OwnedMarkerBounds");
            check(markerBounds is not null, "Drawn owned marker bounds define the actual hit areas");
            var bounds = (IReadOnlyDictionary<StarId, Rect2>)markerBounds!.GetValue(map)!;
            var labels = (IReadOnlyList<Rect2>)typeof(GalaxyMap).GetProperty("VisibleLabelBounds")!.GetValue(map)!;
            check(labels.Count > 0, $"{size}: actual native draw recorded visible labels");
            check(labels.All(r => new Rect2(Vector2.Zero, map.Size).Encloses(r)), $"{size}: actual measured rendered label bounds stay inside map");
            check(!labels.Where((a,i) => labels.Skip(i+1).Any(b => a.Intersects(b))).Any(), $"{size}: rendered labels do not overlap");
            check(bounds.Keys.SequenceEqual(new[] { new StarId(1) }), $"{size}: foreign and transit markers do not acquire owned interaction");
            var hit = bounds[new StarId(1)];
            check(new Rect2(Vector2.Zero, map.Size).Encloses(hit), $"{size}: marker hit area fits actual map");
            map._GuiInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left, Position = hit.GetCenter() });
            check(chosen.SequenceEqual(new[] { new StarId(1) }), $"{size}: owned-at-star marker opens chooser once, without choosing arbitrary vessel");
            var transform = typeof(GalaxyMap).GetMethod("BuildTransform", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(map, null);
            var toScreen = typeof(GalaxyMap).GetMethod("ToScreen", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var starPoint = (Vector2)toScreen.Invoke(map, new object[] { 100, 0, transform! })!;
            map._GuiInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left, Position = starPoint });
            check(map.Selected == new StarId(2) && chosen.Count == 1, $"{size}: visible star fallback remains selection-only");
            check(map.TooltipText.Contains(view.Stars[1].Name, StringComparison.Ordinal), $"{size}: selected known name remains fully discoverable");
            for (int i = 0; i < 3; i++) await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            check(labels.Count > 0 && labels.All(r => new Rect2(Vector2.Zero, map.Size).Encloses(r)), $"{size}: selected-state rendered bounds remain valid");
            check(map.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(output, $"map-{size.X}x{size.Y}.png")) == Error.Ok, $"{size}: selected-state native frame retained");
            // Labelled projection-only overlap supplement; actual Main canonical equality is checked by the enclosing case.
            var crowded=new GalaxyMapView(ImmutableArray.Create(
                new ChartedStar(new(1),"Near A",500,500,null),new ChartedStar(new(2),"Near B",501,500,null),
                new ChartedStar(new(3),"Extent A",0,0,null),new ChartedStar(new(4),"Extent B",1000,1000,null)),null,null,null,null,ImmutableArray<StarId>.Empty)
                {Units=ImmutableArray.Create(new MapVesselMarker(new(20),"scout",true,new StarId(1),null,null,0),new MapVesselMarker(new(21),"scout",true,new StarId(2),null,null,0))};
            map.ShowGalaxy(crowded);for(int i=0;i<4;i++)await host.ToSignal(host.GetTree(),SceneTree.SignalName.ProcessFrame);
            var left=map.OwnedMarkerBounds[new StarId(1)];var right=map.OwnedMarkerBounds[new StarId(2)];
            check(left.HasPoint(right.GetCenter())&&right.HasPoint(left.GetCenter()),$"{size}: actual drawn owned marker rectangles overlap in the labelled dense fixture");
            map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=right.GetCenter()});
            check(chosen.Last()==new StarId(2),$"{size}: nearest owned marker center wins overlap before stable ID");
            // Coincident drawn centers make the distance tie exact, avoiding float-midpoint rounding in the fixture.
            map.ShowGalaxy(crowded with{Stars=crowded.Stars.SetItem(1,crowded.Stars[1] with{X=500})});
            for(int i=0;i<4;i++)await host.ToSignal(host.GetTree(),SceneTree.SignalName.ProcessFrame);
            map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=map.OwnedMarkerBounds[new StarId(2)].GetCenter()});
            check(chosen.Last()==new StarId(1),$"{size}: equally distant owned marker centers use stable ID tie break");
            transform=typeof(GalaxyMap).GetMethod("BuildTransform",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(map,null);
            starPoint=(Vector2)toScreen.Invoke(map,new object[]{0,0,transform!})!;int priorChosen=chosen.Count;
            map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=starPoint});
            check(map.Selected==new StarId(3)&&chosen.Count==priorChosen,$"{size}: dense-marker selection preserves ordinary star fallback outside owned hit areas");
            host.RemoveChild(map); map.QueueFree();
        }
    }
}
