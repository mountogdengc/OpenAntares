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
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Validation;

public partial class VisualOverhaulValidation
{
    private async Task UnifiedFleetRoster()
    {
        async Task RosterFrame(string label,Vector2I size,Control? target)
        {
            GetTree().Root.Size=size;await Frames(4);
            if(target is not null)for(Node? p=target.GetParent();p is not null;p=p.GetParent())if(p is ScrollContainer scroll)scroll.EnsureControlVisible(target);
            await Frames(4);Check(target is null||target.GetWindow().Visible,"Roster frame uses a visible workspace: "+label+size);await Capture(label,size);
        }
        var failures=new List<string>();
        void Expect(bool ok,string name){_checks.Add(new{passed=ok,name});if(!ok)failures.Add(name);}
        var initial=await Start();var content=Live(initial).Content;var original=_main!;
        var generated=GalaxyGenerator.CreateNewGame(content,new("small",42,12,"harl",0,null,false,false,false));
        var state=generated.State!;
        for(int i=0;i<101;i++){var copy=state.Vessels[0].Clone();copy.Id=new(state.AllocateId());state.Vessels.Add(copy);}
        var game=new GameSession(content,state);var owner=state.Empires.First(e=>e.Controller==ControllerKind.Human).Id;
        var creation=new FleetAction.Create("First",state.Vessels.Take(2).Select(v=>v.Id).ToImmutableArray());
        var preview=FleetQueries.PreviewAction(content,game.View(owner),creation);
        Check(preview.Allowed&&game.Execute(new FleetCommand(owner,state.Turn,creation){Fingerprint=preview.Fingerprint}).Accepted,"Labelled 102-ship fixture uses an ordinary command to create one two-ship fleet");
        original.Hide();var screen=new GameScreen(game,owner);_main=screen;AddChild(screen);await Frames();
        var view=game.View(owner);var map=All<GalaxyMap>(screen).Single();var home=view.Colonies[0].StarId;
        var independent=view.Vessels.Where(v=>view.Fleets!.Ships.Single(s=>s.Id==v.Id).Fleet is null).ToArray();
        string before=Snapshot(game);
        Press(Route("OpenFleetCommand"));await Frames();var dialog=All<FleetCommandDialog>(screen).Single();var list=Named<FleetList>("FleetShipList");
        Expect(list.ItemCount==102,"Unified Ships workspace retains all 102 exact ship IDs");
        Expect(All<MapFleetRow>(list).Any(r=>r.Text.Contains("Scout ×100")),"Workspace uses the same counted MapFleetRow for 100 equivalent independent ships");
        Expect(!typeof(Tree).IsAssignableFrom(list.GetType()),"Workspace no longer uses a separate Tree roster");
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})await RosterFrame("UnifiedShips",size,All<MapFleetRow>(list).LastOrDefault());
        var stack=All<MapFleetRow>(list).SingleOrDefault(r=>r.Text.Contains("Scout ×100"));
        if(stack is not null)
        {
            Press(stack);await Frames();
            Expect(list.GetSelectedItems().Length==100,"Workspace stack click selects every underlying independent ship");
            var details=All<Label>(dialog).Single(l=>l.Text.StartsWith("100 selected;",StringComparison.Ordinal));
            Expect(details.Text.Split('\n').Length<=3&&details.Text.Contains("Scout ×100")&&details.Text.Contains("hull 60/60"),"Selected workspace stack keeps equivalent ship details compact without losing hull/status");
            foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})await RosterFrame("UnifiedSelection",size,details);
            Press(All<Button>(list).Single(b=>b.Name=="ExpandShipStack_"+independent[0].Id.Value));await Frames();
            var leaf=All<MapFleetRow>(list).Single(r=>r.Name=="FleetShipMember_"+independent[0].Id.Value);
            leaf._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,ShiftPressed=true});Press(leaf);await Frames();
            var selected=list.GetSelectedItems().Select(i=>list.GetItemMetadata(i).AsInt32()).ToHashSet();
            Expect(selected.Count==99&&!selected.Contains(independent[0].Id.Value),"Workspace Shift-click removes exactly one expanded stack member");
            var sort=Named<OptionButton>("FleetSort");sort.Select(2);sort.EmitSignal(OptionButton.SignalName.ItemSelected,2L);await Frames();
            Expect(list.GetSelectedItems().Select(i=>list.GetItemMetadata(i).AsInt32()).ToHashSet().SetEquals(selected),"Workspace sorting preserves the exact partial stack selection");
            Expect(All<MapFleetRow>(list).Any(r=>r.Name=="FleetShipMember_"+independent[0].Id.Value&&r.IsVisibleInTree()),"Sorting preserves expanded member access");
            var filter=Named<OptionButton>("FleetShipFilter");filter.Select(4);filter.EmitSignal(OptionButton.SignalName.ItemSelected,4L);await Frames();Expect(list.ItemCount==0,"Existing Travelling filter excludes stationary ships");
            filter.Select(0);filter.EmitSignal(OptionButton.SignalName.ItemSelected,0L);await Frames();Expect(list.ItemCount==102,"Existing All filter restores every ship");
        }
        // Projection supplements pin facts that must not disappear inside a stack.
        var reserved=view with{Fleets=view.Fleets! with{Ships=view.Fleets.Ships.Select(s=>s.Id==independent[^1].Id?s with{Request=new RequestId(20000)}:s).ToImmutableArray()}};
        dialog.ShowEmpire(content,reserved);await Frames();
        Expect(All<MapFleetRow>(list).Count(r=>r.Name.ToString().StartsWith("FleetShipRow_"))==3,"A different request commitment stays outside the equivalent independent stack");
        var cargo=view with{Vessels=view.Vessels.Select(s=>s.Id==independent[^1].Id?s with{CarriedPopulation=2}:s).ToImmutableArray()};
        dialog.ShowEmpire(content,cargo);await Frames();Expect(All<MapFleetRow>(list).Count(r=>r.Name.ToString().StartsWith("FleetShipRow_"))==3,"Different carried population stays in a separate stack");
        var damage=view with{Combat=view.Combat! with{Ships=view.Combat.Ships.Select(s=>s.Id==independent[^1].Id?s with{Hull=s.Hull-1}:s).ToImmutableArray()}};
        dialog.ShowEmpire(content,damage);await Frames();Expect(All<MapFleetRow>(list).Count(r=>r.Name.ToString().StartsWith("FleetShipRow_"))==3,"A damaged hull stays outside the full-hull stack");
        var course=view with{Vessels=view.Vessels.Select(s=>s.Id==independent[^1].Id?s with{PendingMove=view.Stars.First(t=>t.Chart.Id!=home).Chart.Id}:s).ToImmutableArray()};
        dialog.ShowEmpire(content,course);await Frames();Expect(All<MapFleetRow>(list).Count(r=>r.Name.ToString().StartsWith("FleetShipRow_"))==3,"Different queued orders cannot merge into the idle stack");
        dialog.ShowEmpire(content,view);await Frames();Named<TabContainer>("FleetTabs").CurrentTab=1;await Frames();
        var groups=Named<Control>("FleetGroupList");
        Expect(All<MapFleetRow>(groups).Count()==1,"Named-fleet workspace list also uses the shared row control");
        if(All<MapFleetRow>(groups).FirstOrDefault() is {} group){Press(group);await Frames();Expect(All<MapFleetRow>(Named<Control>("FleetMemberList")).Count()==2,"Named-fleet member choices use the shared ship row control");}
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})await RosterFrame("UnifiedNamedFleet",size,All<MapFleetRow>(Named<Control>("FleetMemberList")).LastOrDefault());
        Press(Route("OpenGalaxyWorkspace"));await Frames();
        Expect(Snapshot(game)==before,"Workspace grouping, expanding, filtering and inspection issue no orders or PRNG draws");
        map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=map.OwnedMarkerBounds[home].GetCenter()});await Frames();
        var card=Named<Control>("MapFleetList");
        Expect(All<MapFleetRow>(card).Count(r=>r.Name.ToString().StartsWith("MapShipRow_"))==1,"Fleet card collapses 100 equivalent independent ships into one row");
        var cardStack=All<MapFleetRow>(card).SingleOrDefault(r=>r.Text.Contains("Scout ×100"));
        if(cardStack is not null)
        {
            Press(cardStack);await Frames();
            Expect(All<Label>(Named<PanelContainer>("FleetCardDetails")).Any(l=>l.Text.Contains("100 SHIPS SELECTED")),"Fleet-card stack selects all 100 exact members");
            Expect(All<Label>(Named<PanelContainer>("FleetCardDetails")).Any(l=>l.Text.Contains("Idle")&&l.Text.Contains("hull 60/60")),"Large selected-stack details retain status and hull rather than dropping them");
            Press(All<Button>(card).Single(b=>b.Name=="ExpandShipStack_"+independent[0].Id.Value));await Frames();
            var first=All<MapFleetRow>(card).Single(r=>r.Name=="MapShipMember_"+independent[0].Id.Value);Press(first);await Frames();
            Expect(All<Label>(Named<PanelContainer>("FleetCardDetails")).Any(l=>l.Text=="1 SHIP SELECTED"),"Expanded map member selects exactly one ship");
            var second=All<MapFleetRow>(card).Single(r=>r.Name=="MapShipMember_"+independent[1].Id.Value);second._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,ShiftPressed=true});Press(second);await Frames();
            Expect(All<Label>(Named<PanelContainer>("FleetCardDetails")).Any(l=>l.Text=="2 SHIPS SELECTED"),"Expanded map members retain Shift additive selection");
            first=All<MapFleetRow>(card).Single(r=>r.Name=="MapShipMember_"+independent[0].Id.Value);
            first._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Right,Position=new(4,4)});await Frames();
            Expect(Named<PopupMenu>("MapFleetContext").Visible,"An expanded independent member keeps its exact Form fleet context action");Named<PopupMenu>("MapFleetContext").Hide();await Frames();
            Press(All<Button>(card).Single(b=>b.Name=="ExpandShipStack_"+independent[0].Id.Value));await Frames();
            foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})await Capture("StackedFleetCard",size);
            var destination=view.Stars.First(s=>s.Chart.Id!=home&&!view.Scouting!.Habitats.Any(h=>h.Star==s.Chart.Id)).Chart;
            var expected=new GameSession(content,game.State.Clone());var action=new FleetAction.MoveSelection(independent.Take(2).Select(s=>s.Id).ToImmutableArray(),ImmutableArray<FleetId>.Empty,destination.Id);var p=FleetQueries.PreviewAction(content,expected.View(owner),action);
            Check(p.Allowed&&expected.Execute(new FleetCommand(owner,state.Turn,action){Fingerprint=p.Fingerprint}).Accepted,"Reference partial-stack course is legal");
            var transform=typeof(GalaxyMap).GetMethod("BuildTransform",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(map,null)!;
            var at=(Vector2)typeof(GalaxyMap).GetMethod("ToScreen",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(map,new object[]{destination.X,destination.Y,transform})!;
            map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=at});await Frames();
            Expect(Snapshot(game)==Snapshot(expected),"One destination click orders only the two explicitly selected members of a stack");
            Expect(All<MapFleetRow>(card).Count(r=>r.Name.ToString().StartsWith("MapShipRow_"))==2,"Queued orders split moved members from the remaining idle stack");
            Press(Route("OpenFleetCommand"));await Frames();Named<TabContainer>("FleetTabs").CurrentTab=0;await Frames();list=Named<FleetList>("FleetShipList");
            var remaining=independent.Skip(2).Take(2).ToArray();
            if(!All<MapFleetRow>(list).Any(r=>r.Name=="FleetShipMember_"+remaining[0].Id.Value)){Press(All<Button>(list).Single(b=>b.Name=="ExpandShipStack_"+remaining[0].Id.Value));await Frames();}
            Press(All<MapFleetRow>(list).Single(r=>r.Name=="FleetShipMember_"+remaining[0].Id.Value));await Frames();
            var next=All<MapFleetRow>(list).Single(r=>r.Name=="FleetShipMember_"+remaining[1].Id.Value);next._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,ShiftPressed=true});Press(next);await Frames();
            var destinationPicker=Named<OptionButton>("FleetDestination");int choice=Enumerable.Range(0,destinationPicker.ItemCount).Single(i=>destinationPicker.GetItemMetadata(i).AsInt32()==destination.Id.Value);destinationPicker.Select(choice);destinationPicker.EmitSignal(OptionButton.SignalName.ItemSelected,(long)choice);await Frames();
            string priorReview=Snapshot(game);Press(Named<Button>("FleetPreviewMove"));await Frames();
            Expect(Snapshot(game)==priorReview,"Unified workspace still requires review before a move changes the game");
            expected=new GameSession(content,game.State.Clone());action=new FleetAction.MoveSelection(remaining.Select(s=>s.Id).ToImmutableArray(),ImmutableArray<FleetId>.Empty,destination.Id);p=FleetQueries.PreviewAction(content,expected.View(owner),action);
            Check(p.Allowed&&expected.Execute(new FleetCommand(owner,state.Turn,action){Fingerprint=p.Fingerprint}).Accepted,"Reference workspace member move is legal");
            Press(Named<ConfirmationDialog>("FleetActionConfirmation").GetOkButton());await Frames();
            Expect(Snapshot(game)==Snapshot(expected),"Reviewed workspace move submits exactly the two chosen expanded member IDs");
        }
        else foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})await Capture("StackedFleetCard",size);
        RemoveChild(screen);screen.QueueFree();_main=original;original.Show();await Frames();
        if(failures.Count>0)throw new InvalidOperationException(string.Join("; ",failures));
    }
}
