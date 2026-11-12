using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Validation;

public partial class VisualOverhaulValidation
{
    private async Task FleetPanelFeedback()
    {
        var failures=new List<string>();void Expect(bool ok,string name){_checks.Add(new{passed=ok,name});if(!ok)failures.Add(name);}
        var screen=await Start();var session=Live(screen);var view=session.View(session.State.Empires.First(e=>e.Controller==ControllerKind.Human).Id);
        var map=All<GalaxyMap>(screen).Single();var home=view.Colonies[0].StarId;var target=view.Stars.First(s=>s.Chart.Id!=home&&!view.Scouting!.Habitats.Any(h=>h.Star==s.Chart.Id)).Chart.Id;
        Vector2 At(StarId id){var s=view.Stars.Single(s=>s.Chart.Id==id).Chart;var t=typeof(GalaxyMap).GetMethod("BuildTransform",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(map,null)!;return (Vector2)typeof(GalaxyMap).GetMethod("ToScreen",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(map,new object[]{s.X,s.Y,t})!;}
        void Click(StarId id)=>map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=At(id)});
        string before=Snapshot(session);map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=map.OwnedMarkerBounds[home].GetCenter()});await Frames();
        var rows=All<Button>(screen).Where(b=>b.Name.ToString().StartsWith("MapShipRow_",StringComparison.Ordinal)).ToArray();
        Expect(rows.Length==1,"Flag opens a list containing the independent starting ship");if(rows.Length==1)Press(rows[0]);await Frames();
        map._GuiInput(new InputEventMouseMotion{Position=At(target)});await Frames();Expect(Snapshot(session)==before,"Hovering a destination never issues an order");
        var reference=new GameSession(session.Content,session.State.Clone());var move=new FleetAction.MoveSelection(ImmutableArray.Create(view.Vessels[0].Id),ImmutableArray<FleetId>.Empty,target);var preview=FleetQueries.PreviewAction(session.Content,view,move);Check(preview.Allowed,"Reference destination is legal");reference.Execute(new FleetCommand(view.Viewer,view.Turn,move){Fingerprint=preview.Fingerprint});
        Click(target);await Frames();Expect(Snapshot(session)==Snapshot(reference),"A destination click submits exactly one existing move without a confirmation button");
        Expect(!All<Button>(screen).Any(b=>b.Name=="MapSetCourse"&&b.IsVisibleInTree()),"No separate Set course button remains");
        var ship=view.Vessels[0];var transit=new TransitState(home,target,4,3);var blocked=view with{Vessels=ImmutableArray.Create(ship with{StationaryStarId=null,Transit=transit}),Fleets=view.Fleets! with{Ships=view.Fleets.Ships.Select(s=>s with{Station=null,Transit=transit}).ToImmutableArray()}};
        var blockedAction=MapFleetOrders.Selection(blocked,new[]{ship.Id},home);Check(!FleetQueries.PreviewAction(session.Content,blocked,blockedAction).Allowed,"Published transit fixture rejects redirection under existing rules");
        map.ShowFleetPresentation(blocked,new[]{ship.Id},home);await Frames();
        Expect(typeof(GalaxyMap).GetField("_routeBlocked",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(map) is true,"Rejected hover route uses the blocked/red presentation");
        map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Right,Position=map.Size/2});await Frames();
        Expect(map.Selected is null&&typeof(GalaxyMap).GetField("_previewRoute",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(map) is null,"Right-click clears system, fleet and route selection");
        Press(Route("EndTurn"));await Frames(5);var report=All<TurnReportDialog>(screen).Single();Check(report.Visible,"One ordinary turn opens the report");
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})
        {
            await Capture("DockedReport",size);var console=Named<PanelContainer>("EmpireConsole").GetGlobalRect();
            Expect(report.Position.X>=console.Position.X-1&&report.Size.X<=console.Size.X+1,"Turn Report occupies the right panel at "+size);
            Expect(All<Label>(report.FindChild("TurnReportScroll",true,false)).All(l=>l.AutowrapMode!=TextServer.AutowrapMode.Off&&l.CustomMinimumSize.X==0),"Docked report labels wrap within the narrow panel at "+size);
            var close=report.GetOkButton();float bottom=report.Position.Y+close.GetGlobalRect().End.Y;float turnTop=Route("EndTurn").GetGlobalRect().Position.Y;
            Expect(close.Text=="Close report"&&Math.Abs(turnTop-bottom)<110,"Report close control stays near End Turn at "+size);
        }
        Press(report.GetOkButton());await Frames();Expect(!report.Visible,"Report close returns the normal console");
        // Labelled pre-session roster: three hulls, then ordinary existing Create commands.
        var generated=GalaxyGenerator.CreateNewGame(session.Content,new("small",42,12,"harl",0,null,false,false,false));Check(generated.State is not null,"Fleet UI fixture generates normally");
        var state=generated.State!;for(int i=0;i<2;i++){var clone=state.Vessels[0].Clone();clone.Id=new(state.AllocateId());state.Vessels.Add(clone);}var fixture=new GameSession(session.Content,state);var owner=state.Empires.First(e=>e.Controller==ControllerKind.Human).Id;
        void Apply(GameSession game,FleetAction action){var v=game.View(owner);var p=FleetQueries.PreviewAction(game.Content,v,action);Check(p.Allowed,"Fixture/reference fleet action is legal");Check(game.Execute(new FleetCommand(owner,v.Turn,action){Fingerprint=p.Fingerprint}).Accepted,"Fixture/reference action accepted");}
        Apply(fixture,new FleetAction.Create("First",state.Vessels.Take(2).Select(v=>v.Id).ToImmutableArray()));Apply(fixture,new FleetAction.Create("Second",ImmutableArray.Create(state.Vessels[2].Id)));
        var original=_main!;original.Hide();var fleetScreen=new GameScreen(fixture,owner);_main=fleetScreen;AddChild(fleetScreen);await Frames();map=All<GalaxyMap>(fleetScreen).Single();home=fixture.View(owner).Colonies[0].StarId;
        Expect(map.OwnedMarkerBounds.Count==1,"Several fleets at one system still produce one map flag");map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=map.OwnedMarkerBounds[home].GetCenter()});await Frames();
        var named=All<Button>(fleetScreen).Where(b=>b.Name.ToString().StartsWith("MapFleetRow_",StringComparison.Ordinal)).ToArray();Expect(named.Length==2,"Flag lists the two named fleets individually");
        if(named.Length==2)
        {
            var first=fixture.View(owner).Fleets!.Fleets.Single(f=>f.Name=="First");var second=fixture.View(owner).Fleets!.Fleets.Single(f=>f.Name=="Second");var source=named.Single(b=>b.Name=="MapFleetRow_"+first.Id.Value);var destination=named.Single(b=>b.Name=="MapFleetRow_"+second.Id.Value);
            Press(source);await Frames();source=All<Button>(fleetScreen).Single(b=>b.Name=="MapFleetRow_"+first.Id.Value);source._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Right,Position=new(4,4)});await Frames();
            var context=Named<PopupMenu>("MapFleetContext");Check(context.Visible,"Fleet right-click opens its context menu");context.EmitSignal(PopupMenu.SignalName.IdPressed,0L);context.Hide();await Frames();
            var split=All<ConfirmationDialog>(fleetScreen).Single(d=>d.Name=="MapFleetSplit");var members=Named<ItemList>("MapSplitMembers");members.Select(0);members.EmitSignal(ItemList.SignalName.MultiSelected,0L,true);await Frames();var name=Named<LineEdit>("MapSplitName").Text;
            var expected=new GameSession(fixture.Content,fixture.State.Clone());Apply(expected,new FleetAction.Split(first.Id,name,ImmutableArray.Create(first.Members[0])));Press(split.GetOkButton());await Frames();Expect(Snapshot(fixture)==Snapshot(expected),"Split picker submits one existing reviewed split command");
            source=All<Button>(fleetScreen).Single(b=>b.Name=="MapFleetRow_"+first.Id.Value);destination=All<Button>(fleetScreen).Single(b=>b.Name=="MapFleetRow_"+second.Id.Value);var drag=source._GetDragData(Vector2.Zero);Check(destination._CanDropData(Vector2.Zero,drag),"A legal fleet merge is accepted by the target row");expected=new GameSession(fixture.Content,fixture.State.Clone());Apply(expected,new FleetAction.Merge(second.Id,ImmutableArray.Create(first.Id)));destination._DropData(Vector2.Zero,drag);await Frames();Expect(Snapshot(fixture)==Snapshot(expected),"Drag merge submits one existing merge with the drop target retained");
        }
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})await Capture("FleetList",size);
        RemoveChild(fleetScreen);fleetScreen.QueueFree();_main=original;original.Show();await Frames();
        if(failures.Count>0)throw new InvalidOperationException(string.Join("; ",failures));
    }
    private async Task MultiFleetSelection()
    {
        var initial=await Start();var original=_main!;var session=Live(initial);
        // Pre-session roster fixture; actions below use ordinary existing fleet commands.
        var generated=GalaxyGenerator.CreateNewGame(session.Content,new("small",42,12,"harl",0,null,false,false,false));
        var state=generated.State!;for(int i=0;i<2;i++){var clone=state.Vessels[0].Clone();clone.Id=new(state.AllocateId());state.Vessels.Add(clone);}
        var fixture=new GameSession(session.Content,state);var owner=state.Empires.First(e=>e.Controller==ControllerKind.Human).Id;
        void Apply(GameSession game,FleetAction action){var v=game.View(owner);var p=FleetQueries.PreviewAction(game.Content,v,action);Check(p.Allowed,"Reference action legal");Check(game.Execute(new FleetCommand(owner,v.Turn,action){Fingerprint=p.Fingerprint}).Accepted,"Reference action accepted");}
        Apply(fixture,new FleetAction.Create("First",ImmutableArray.Create(state.Vessels[0].Id)));Apply(fixture,new FleetAction.Create("Second",ImmutableArray.Create(state.Vessels[1].Id)));
        original.Hide();var screen=new GameScreen(fixture,owner);_main=screen;AddChild(screen);await Frames();var map=All<GalaxyMap>(screen).Single();var view=fixture.View(owner);var home=view.Colonies[0].StarId;
        map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=map.OwnedMarkerBounds[home].GetCenter()});await Frames();
        var first=view.Fleets!.Fleets.Single(f=>f.Name=="First");var second=view.Fleets.Fleets.Single(f=>f.Name=="Second");
        Button Row(FleetId id)=>All<Button>(screen).Single(b=>b.Name=="MapFleetRow_"+id.Value);
        async Task Choose(FleetId id,bool shift){var row=Row(id);row._GuiInput(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true,ShiftPressed=shift});Press(row);await Frames();}
        string before=Snapshot(fixture);await Choose(first.Id,false);await Choose(second.Id,true);
        Check(Row(first.Id).ButtonPressed&&Row(second.Id).ButtonPressed,"Shift-click adds a fleet and highlights both selected rows");
        await Choose(first.Id,true);Check(!Row(first.Id).ButtonPressed&&Row(second.Id).ButtonPressed,"Shift-click removes only the clicked fleet");
        await Choose(first.Id,true);await Choose(second.Id,false);Check(!Row(first.Id).ButtonPressed&&Row(second.Id).ButtonPressed,"Plain click replaces the multi-selection");
        await Choose(second.Id,true);Check(!Row(first.Id).ButtonPressed&&!Row(second.Id).ButtonPressed,"Shift-click can remove the last selected fleet");
        await Choose(first.Id,true);await Choose(second.Id,true);Check(Snapshot(fixture)==before,"Selection changes never submit game orders");
        var independent=All<MapFleetRow>(screen).Single(b=>b.Name.ToString().StartsWith("MapShipRow_"));
        independent._GuiInput(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true,ShiftPressed=true});Press(independent);await Frames();
        Check(All<MapFleetRow>(screen).Single(b=>b.Name.ToString().StartsWith("MapShipRow_")).ButtonPressed&&Row(first.Id).ButtonPressed&&Row(second.Id).ButtonPressed,"Shift-click adds an independent ship alongside named fleets");
        independent=All<MapFleetRow>(screen).Single(b=>b.Name.ToString().StartsWith("MapShipRow_"));independent._GuiInput(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true,ShiftPressed=true});Press(independent);await Frames();
        Check(!All<MapFleetRow>(screen).Single(b=>b.Name.ToString().StartsWith("MapShipRow_")).ButtonPressed,"Shift-click removes the independent ship without changing named fleets");
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})await Capture("MultiFleet",size);
        var target=view.Stars.First(s=>s.Chart.Id!=home).Chart.Id;var expected=new GameSession(fixture.Content,fixture.State.Clone());Apply(expected,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(first.Id,second.Id),target));
        var transform=typeof(GalaxyMap).GetMethod("BuildTransform",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(map,null);var star=view.Stars.Single(s=>s.Chart.Id==target).Chart;var at=(Vector2)typeof(GalaxyMap).GetMethod("ToScreen",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(map,new object[]{star.X,star.Y,transform!})!;
        map._GuiInput(new InputEventMouseMotion{Position=at});await Frames();Check(Snapshot(fixture)==before,"Multi-fleet hover is read-only");
        map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=at});await Frames();Check(Snapshot(fixture)==Snapshot(expected),"One destination click submits the existing atomic move for both selected fleets only");
        RemoveChild(screen);screen.QueueFree();_main=original;original.Show();await Frames();
    }

    private async Task MapKeyboardFeedback()
    {
        var screen=await Start();var session=Live(screen);var view=session.View(session.State.Empires.First(e=>e.Controller==ControllerKind.Human).Id);var map=All<GalaxyMap>(screen).Single();var home=view.Colonies[0].StarId;
        Vector2 At(StarId id){var star=view.Stars.Single(s=>s.Chart.Id==id).Chart;var t=typeof(GalaxyMap).GetMethod("BuildTransform",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(map,null)!;return (Vector2)typeof(GalaxyMap).GetMethod("ToScreen",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(map,new object[]{star.X,star.Y,t})!;}
        async Task Keypress(Key code,Key physical=Key.None){Input.ParseInputEvent(new InputEventKey{Pressed=true,Keycode=code,PhysicalKeycode=physical});await Frames();Input.ParseInputEvent(new InputEventKey{Pressed=false,Keycode=code,PhysicalKeycode=physical});await Frames();}
        void Flag()=>map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=map.OwnedMarkerBounds[home].GetCenter()});
        string before=Snapshot(session);map.Select(view.Stars.First(s=>s.Chart.Id!=home).Chart.Id);await Frames();Flag();await Frames();
        Check(map.Selected is null,"Fleet flag clears the system highlight without selecting that system");Check(All<MapFleetRow>(screen).Any(r=>r.IsVisibleInTree()),"Fleet flag still opens individual fleet rows");
        map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=At(home)});await Frames();Check(map.Selected==home,"System glyph still selects the system independently");
        Flag();await Frames();Press(All<MapFleetRow>(screen).First(r=>r.IsVisibleInTree()));await Frames();Check(map.Selected is null,"Selecting a fleet row does not highlight its system");
        float zoom=map.ZoomLevel;await Keypress(Key.KpAdd);Check(map.ZoomLevel>zoom,"Numpad plus zooms in with a fleet row focused");
        var position=At(home);await Keypress(Key.Kp6);Check(At(home).X<position.X,"Numpad6 pans right");position=At(home);await Keypress(Key.Kp4);Check(At(home).X>position.X,"Numpad4 pans left");
        position=At(home);await Keypress(Key.Kp8);Check(At(home).Y>position.Y,"Numpad8 pans up");position=At(home);await Keypress(Key.Kp2);Check(At(home).Y<position.Y,"Numpad2 pans down");
        position=At(home);await Keypress(Key.Up,Key.Kp8);Check(At(home).Y>position.Y,"Physical numpad key pans when NumLock-off reports an arrow key");
        position=At(home);await Keypress(Key.Key6);Check(At(home).IsEqualApprox(position),"Ordinary number-row6 does not pan");
        var entry=new LineEdit{Name="MapKeyTextEntry"};screen.AddChild(entry);entry.GrabFocus();await Frames();position=At(home);zoom=map.ZoomLevel;await Keypress(Key.Kp6);await Keypress(Key.KpAdd);Check(At(home).IsEqualApprox(position)&&map.ZoomLevel==zoom,"Typing focus suppresses numpad pan and zoom");screen.RemoveChild(entry);entry.QueueFree();map.GrabFocus();await Frames();
        var popup=All<MenuButton>(screen).Single(m=>m.Name=="GameMenu").GetPopup();popup.Popup();await Frames();position=At(home);await Keypress(Key.Kp6);Check(At(home).IsEqualApprox(position),"Open Game popup suppresses map keys");popup.Hide();await Frames();
        Press(Route("OpenResearch"));await Frames();position=At(home);zoom=map.ZoomLevel;await Keypress(Key.Kp6);await Keypress(Key.KpAdd);Check(At(home).IsEqualApprox(position)&&map.ZoomLevel==zoom,"Research workspace suppresses map keys");Press(Route("OpenGalaxyWorkspace"));await Frames();map.GrabFocus();
        zoom=map.ZoomLevel;await Keypress(Key.KpSubtract);Check(map.ZoomLevel<zoom,"Numpad minus zooms out");Check(Snapshot(session)==before,"Flag and keyboard navigation never submit gameplay orders");
        await Capture("MapKeys",new Vector2I(1920,1080));
    }

}
