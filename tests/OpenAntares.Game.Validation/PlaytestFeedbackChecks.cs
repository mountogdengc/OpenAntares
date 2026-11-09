using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;

namespace OpenAntares.Game.Validation;

public partial class VisualOverhaulValidation
{
    private async Task PlaytestFeedback()
    {
        var failures=new List<string>();
        void Expect(bool passed,string name){_checks.Add(new{passed,name});if(!passed)failures.Add(name);}
        var screen=await Start();var session=Live(screen);string unchanged=Snapshot(session);
        Expect(!All<Control>(screen).Any(c=>c.Name=="FirstTurnHint"),"No automatic campaign-start banner");
        foreach(string route in new[]{"OpenColonyWorkspace","OpenFleetCommand","OpenResearch","OpenShipDesigns","OpenDiplomacy"})
        {
            Press(Route("OpenGalaxyWorkspace"));await Frames();Press(Route(route));await Frames();
            if(route=="OpenResearch")Expect(!Named<OptionButton>("ResearchTargetPicker").GetPopup().Visible,"Opening Research leaves the choice list closed");
            Expect(!All<Button>(screen).Any(b=>b.Name=="ReturnToGalaxy"&&b.IsVisibleInTree()),"No Back-to-X button in "+route);
            foreach(var picker in All<OptionButton>(screen))picker.GetPopup().Hide();
            Press(Route(route));await Frames();Expect(All<GalaxyMap>(screen).Single().IsVisibleInTree(),"Repeated command key closes "+route+" to Galaxy");
        }
        Press(Route("OpenGalaxyWorkspace"));await Frames();
        Press(Route("OpenAttentionConsole"));await Frames();Expect(Named<TabContainer>("ContextConsole").CurrentTab==1,"Decisions key opens the Empire context");
        Press(Route("OpenAttentionConsole"));await Frames();Expect(Named<TabContainer>("ContextConsole").CurrentTab==0,"Repeated Decisions key closes its context");
        var map=All<GalaxyMap>(screen).Single();var view=session.View(session.State.Empires.First(e=>e.Controller==OpenAntares.Simulation.State.ControllerKind.Human).Id);
        var home=view.Colonies[0].StarId;var destination=view.Stars.First(s=>s.Chart.Id!=home&&!view.Scouting!.Habitats.Any(h=>h.Star==s.Chart.Id)).Chart.Id;
        Vector2 Position(StarId id)
        {
            object transform=typeof(GalaxyMap).GetMethod("BuildTransform",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(map,null)!;
            var star=view.Stars.Single(s=>s.Chart.Id==id).Chart;
            return (Vector2)typeof(GalaxyMap).GetMethod("ToScreen",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(map,new object[]{star.X,star.Y,transform})!;
        }
        void ClickStar(StarId id)=>map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=Position(id)});
        ClickStar(destination);await Frames();
        Expect(typeof(GalaxyMap).GetField("_previewRoute",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(map) is null,"System inspection has no fleet route or ETA");
        int stars=0,fleets=0;map.StarSelected+=_=>stars++;map.OwnedVesselsSelected+=_=>fleets++;
        ClickStar(home);await Frames();Expect(stars==1&&fleets==0,"The system glyph selects the system independently");
        Vector2 before=Position(destination),anchor=Position(home);map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.WheelUp,Position=anchor});await Frames();
        Expect(Position(destination).DistanceTo(before)>1,"Mouse wheel zoom changes map geometry");
        Expect(Position(home).DistanceTo(anchor)<1,"Wheel zoom preserves the system under the cursor");
        ClickStar(destination);await Frames();Expect(map.Selected==destination,"System picking uses the zoomed transform");
        Press(Route("OpenGalaxyWorkspace"));await Frames();
        Expect(Position(home).DistanceTo(map.Size/2)<2,"Repeated Galaxy key centers the home system");
        map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.WheelDown,Position=map.Size/2});await Frames();
        Expect(map.ZoomLevel==1,"Zooming back out restores the fitted map");
        Vector2 panBefore=Position(home);map._GuiInput(new InputEventMouseMotion{ButtonMask=MouseButtonMask.Middle,Relative=new Vector2(20,10)});await Frames();
        Expect(Position(home).DistanceTo(panBefore+new Vector2(20,10))<1,"Middle-drag pans with the same picking transform");
        Press(Route("MapZoomFit"));await Frames();
        var marker=map.OwnedMarkerBounds[home];map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=marker.GetCenter()});await Frames();
        Expect(fleets==1,"The fleet flag selects ships independently");
        ClickStar(destination);await Frames();
        Expect(typeof(GalaxyMap).GetField("_previewRoute",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(map) is not null,"An explicit fleet selection enables the route preview");
        Expect(Snapshot(session)==unchanged,"Menu navigation, inspection, selection and zoom issue no orders");
        var course=All<Button>(screen).SingleOrDefault(b=>b.Name=="MapSetCourse");Expect(course is not null&&course.IsVisibleInTree()&&!course.Disabled,"Map has a reachable Set course action");
        if(course is not null&&!course.Disabled)
        {
            var reference=new GameSession(session.Content,session.State.Clone());var ids=view.Vessels.Where(v=>v.StationaryStarId==home).Select(v=>v.Id).ToImmutableArray();
            var action=new FleetAction.MoveSelection(ids,System.Collections.Immutable.ImmutableArray<FleetId>.Empty,destination);
            var preview=FleetQueries.PreviewAction(session.Content,view,action);Check(preview.Allowed,"Ordinary selected-map route is legal");
            Check(reference.Execute(new FleetCommand(view.Viewer,view.Turn,action){Fingerprint=preview.Fingerprint}).Accepted,"Reference map move is accepted");
            Press(course);await Frames();Expect(Snapshot(session)==Snapshot(reference),"Map Set course matches one existing atomic fleet command");
        }
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})await Capture("MapFeedback",size);
        Press(Route("EndTurn"));await Frames(5);var report=All<TurnReportDialog>(screen).Single();
        Check(report.Visible,"One ordinary turn opens a real report");
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})
        {await Capture("CompactReport",size);Expect(report.Size.X<=size.X*0.85&&report.Size.Y<=size.Y*0.85,"Turn Report remains compact at "+size);}
        Press(report.GetOkButton());await Frames();
        var menu=Named<MenuButton>("GameMenu");int item=Enumerable.Range(0,menu.GetPopup().ItemCount).FirstOrDefault(i=>menu.GetPopup().GetItemText(i)=="Main menu",-1);
        Expect(item>=0,"Game options include Main menu");
        if(item>=0)
        {
            string parked=Snapshot(session);menu.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed,(long)menu.GetPopup().GetItemId(item));await Frames();
            var resume=All<Button>(_main!).SingleOrDefault(b=>b.Name=="ResumeCampaign");Expect(resume is not null&&resume.IsVisibleInTree(),"Main menu offers to resume the current campaign");
            if(resume is not null){Press(resume);await Frames();screen=All<GameScreen>(_main!).Single();Expect(Snapshot(Live(screen))==parked,"Main menu and Resume preserve the exact campaign");}
        }
        if(failures.Count>0)throw new InvalidOperationException(string.Join("; ",failures));
    }
}
