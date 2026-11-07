using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Saves;

namespace OpenAntares.Game.Validation;

internal static class PlayabilityNavigationChecks
{
    public static async Task Run(Node host, GameScreen screen, GameSession game, string output, Action<bool,string> check)
    {
        await ConsoleWidth(host,screen,game,check);
        await RebuiltFocus(host,screen,game,check);
        await NestedOverviewReturn(host,screen,game,check);
        var type=typeof(GameScreen).Assembly.GetType("OpenAntares.Game.Presentation.PlayabilityContext");
        check(type is not null,"Semantic navigation context exists without campaign persistence");
        check(type!.GetProperties().All(p=>!typeof(GodotObject).IsAssignableFrom(p.PropertyType)),"Context retains no Godot node reference");
        var context=Activator.CreateInstance(type)!;
        type.GetProperty("Vessel")!.SetValue(context,new VesselId(int.MaxValue));
        type.GetProperty("Colony")!.SetValue(context,new ColonyId(int.MaxValue));
        var owner=game.State.Empires.First(e=>e.Controller==OpenAntares.Simulation.State.ControllerKind.Human).Id;
        type.GetMethod("Reconcile")!.Invoke(context,new object[]{game.View(owner)});
        check(type.GetProperty("Vessel")!.GetValue(context) is null&&type.GetProperty("Colony")!.GetValue(context) is null,"MissingSelectionExplainsAndNeverFallsThroughToAnotherEntity: reconciliation clears unavailable semantic IDs");
        string before=SaveGame.Serialize(game.Content,game.State);
        Field("_selectedVessel").SetValue(screen,new VesselId(int.MaxValue));
        Refresh(screen); await Frames(host);
        check(Field("_selectedVessel").GetValue(screen) is null&&(bool)Field("_requireVesselChoice").GetValue(screen)!,"Unavailable selected vessel never falls through to a different owned vessel");
        check(Descendants<Label>(screen).Any(l=>l.Visible&&(l.Text.Contains("no longer",StringComparison.OrdinalIgnoreCase)||l.Text.Contains("unavailable",StringComparison.OrdinalIgnoreCase))),"Lost selection has a visible explanation");
        typeof(GameScreen).GetMethod("SelectColony",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(screen,new object[]{new ColonyId(int.MaxValue)});
        await Frames(host);
        check(Field("_selectedColony").GetValue(screen) is null,"Unavailable report colony never leaves another colony selected for its action");
        check(before==SaveGame.Serialize(game.Content,game.State),"Lost-context recovery issues no command or PRNG change");
        Named<Button>(screen,"OpenColonyWorkspace").EmitSignal(Button.SignalName.Pressed);await Frames(host);
        check(Field("_selectedColony").GetValue(screen) is null,"Opening Colonies after a lost selection requires an explicit colony choice");
        var fleet=Descendants<FleetCommandDialog>(screen).Single();
        fleet.ShowEmpire(game.Content,game.View(owner));await Frames(host);
        var edit=Named<LineEdit>(fleet,"FleetName");edit.Text="Unsubmitted fleet draft";edit.GrabFocus();await Frames(host);
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1152,648),new Vector2I(1600,900),new Vector2I(1920,1080)})
        {
            host.GetTree().Root.Size=size;Refresh(screen);await Frames(host,4);
            check(edit.Text=="Unsubmitted fleet draft"&&edit.HasFocus(),$"ResizeDoesNotDoubleScrollOrStealEditedFocus: {size} preserves the active text draft and focus");
        }
        fleet.Hide();await Frames(host);
        check(before==SaveGame.Serialize(game.Content,game.State),"Typing and refreshing an unsubmitted draft changes no campaign");
        var end=Named<Button>(screen,"EndTurn");end.EmitSignal(Button.SignalName.Pressed);await Frames(host,5);
        check(game.State.Turn==1,"Report round-trip fixture advances exactly one explicit ordinary boundary");
        var report=Descendants<TurnReportDialog>(screen).Single();
        var scroll=Descendants<ScrollContainer>(report).Single();
        var link=Named<Button>(report,"ReportOpenFleetCommand");link.GrabFocus();scroll.EnsureControlVisible(link);await Frames(host);
        int offset=scroll.ScrollVertical;string published=SaveGame.Serialize(game.Content,game.State);
        link.EmitSignal(Button.SignalName.Pressed);await Frames(host);
        check(fleet.Visible&&!report.Visible,"Report link opens the requested fleet overview");
        var officerLink=Named<Button>(fleet,"FleetOpenOfficers");officerLink.GrabFocus();officerLink.EmitSignal(Button.SignalName.Pressed);await Frames(host,5);
        Descendants<OfficerDetailDialog>(screen).Single().GetOkButton().EmitSignal(Button.SignalName.Pressed);await Frames(host,5);
        check(fleet.Visible&&!report.Visible&&officerLink.HasFocus(),"Report to Fleet to Officers returns one level without skipping the invoking Fleet overview");
        fleet.Hide();await Frames(host,5);
        check(report.Visible&&scroll.ScrollVertical==offset&&link.HasFocus(),"ReportRoundTripRestoresValidScrollAndFocus: closing the linked overview returns to the exact report position and action");
        check(published==SaveGame.Serialize(game.Content,game.State),"Report navigation neither acknowledges a required response nor submits another Ready command");
        // Labelled semantic navigation supplement invokes the production report event; no simulation fixture or order.
        var starRequest=(Action<StarId>)typeof(TurnReportDialog).GetField("StarRequested",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(report)!;
        starRequest(game.View(owner).Stars.First().Chart.Id);await Frames(host,5);
        var reportReturn=Descendants<Button>(screen).SingleOrDefault(b=>b.Name=="ReturnToInvokingReport");
        check(reportReturn is not null&&reportReturn.IsVisibleInTree(),"Report map excursion exposes an explicit Return to report action");
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1152,648),new Vector2I(1600,900),new Vector2I(1920,1080)})
        {
            host.GetTree().Root.Size=size;await Frames(host,4);var bounds=reportReturn!.GetGlobalRect();
            host.GetViewport().GetTexture().GetImage().SavePng(System.IO.Path.Combine(output,$"report-map-return-{size.X}.png"));
            check(bounds.Position.X>=0&&bounds.Position.Y>=0&&bounds.End.X<=size.X+1&&bounds.End.Y<=size.Y+1,$"Return to report remains reachable at {size}");
        }
        reportReturn!.EmitSignal(Button.SignalName.Pressed);await Frames(host,5);
        check(report.Visible&&scroll.ScrollVertical==offset&&link.HasFocus()&&published==SaveGame.Serialize(game.Content,game.State),"Map excursion returns to the exact report position without a command");
        report.Hide();await Frames(host,5);
        check(Field("_selectedColony").GetValue(screen) is null&&Field("_selectedVessel").GetValue(screen) is null,"Closing the report preserves explicit lost-object choice requirements");
        Named<Button>(screen,"OpenGalaxyWorkspace").EmitSignal(Button.SignalName.Pressed);await Frames(host);
        var galaxy=Named<Button>(screen,"OpenGalaxyWorkspace");galaxy.GrabFocus();
        Input.ParseInputEvent(new InputEventKey{Keycode=Key.Tab,Pressed=true});Input.ParseInputEvent(new InputEventKey{Keycode=Key.Tab,Pressed=false});await Frames(host);
        var tabbed=screen.GetViewport().GuiGetFocusOwner();
        check(tabbed is not null&&tabbed.IsVisibleInTree()&&tabbed!=galaxy,"Tab reaches a visible enabled navigation control");
        Input.ParseInputEvent(new InputEventKey{Keycode=Key.Tab,ShiftPressed=true,Pressed=true});Input.ParseInputEvent(new InputEventKey{Keycode=Key.Tab,ShiftPressed=true,Pressed=false});await Frames(host);
        check(galaxy.HasFocus(),"Shift+Tab returns through the same visible navigation order");
        string after=SaveGame.Serialize(game.Content,game.State);
        Named<Button>(screen,"InspectSelectedSystem").EmitSignal(Button.SignalName.Pressed);await Frames(host);
        screen._UnhandledKeyInput(new InputEventKey{Keycode=Key.Escape,Pressed=true});await Frames(host);
        check((string)Field("_workspace").GetValue(screen)! == "Galaxy"&&after==SaveGame.Serialize(game.Content,game.State),"Escape closes the inspection workspace without submitting or acknowledging anything");
    }
    private static async Task ConsoleWidth(Node host,GameScreen screen,GameSession game,Action<bool,string> check)
    {
        string before=SaveGame.Serialize(game.Content,game.State);
        Named<Button>(screen,"OpenGalaxyWorkspace").EmitSignal(Button.SignalName.Pressed);await Frames(host);
        var scroll=Named<ScrollContainer>(screen,"ConsoleScroll");var content=(Control)scroll.GetChild(0);
        var original=content.GetChildren().OfType<Control>().Select(c=>(Control:c,Visible:c.Visible)).ToArray();
        var mode=scroll.VerticalScrollMode;var spacer=new Control{CustomMinimumSize=new(1,40),SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};
        try
        {
            // Labelled geometry supplement on the real console; it issues no game command.
            foreach(var entry in original)entry.Control.Hide();content.AddChild(spacer);
            foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1152,648),new Vector2I(1600,900),new Vector2I(1920,1080)})
            {
                host.GetTree().Root.Size=size;await Frames(host,5);
                async Task<(float Width,bool Bar)> Measure(int height)
                {
                    spacer.CustomMinimumSize=new(1,height);await Frames(host,5);
                    return(content.Size.X,scroll.GetVScrollBar().Visible);
                }
                scroll.VerticalScrollMode=ScrollContainer.ScrollMode.Auto;
                var autoShort=await Measure(40);var autoLong=await Measure(size.Y+1000);
                check(!autoShort.Bar&&autoLong.Bar&&Math.Abs(autoShort.Width-autoLong.Width)>1,$"{size}: negative control proves automatic scrollbar changes the real console column width");
                scroll.VerticalScrollMode=mode;
                var shortContent=await Measure(40);var longContent=await Measure(size.Y+1000);
                check(!shortContent.Bar&&longContent.Bar&&Math.Abs(shortContent.Width-longContent.Width)<0.5,$"{size}: console wrapping width remains stable as content crosses the scrollbar threshold");
            }
        }
        finally
        {
            scroll.VerticalScrollMode=mode;content.RemoveChild(spacer);spacer.QueueFree();
            foreach(var entry in original)entry.Control.Visible=entry.Visible;
            Refresh(screen);await Frames(host,5);
        }
        check(before==SaveGame.Serialize(game.Content,game.State),"Console sizing supplement leaves campaign and PRNG unchanged");
    }

    private static async Task RebuiltFocus(Node host, GameScreen screen, GameSession game, Action<bool,string> check)
    {
        var owner=game.State.Empires.First(e=>e.Controller==OpenAntares.Simulation.State.ControllerKind.Human).Id;
        string original=SaveGame.Serialize(game.Content,game.State);
        Named<Button>(screen,"OpenColonyWorkspace").EmitSignal(Button.SignalName.Pressed);await Frames(host);
        var panel=Descendants<ColonyPanel>(screen).Single();int issued=0;
        Action<OpenAntares.Simulation.Commands.Command> observe=_=>issued++;
        panel.CommandIssued+=observe;
        try
        {
            var increase=Descendants<Button>(panel).Single(b=>b.TooltipText=="Move a worker from support to production");
            for(Node? node=increase.GetParent();node is not null;node=node.GetParent())if(node is ScrollContainer scroll)scroll.EnsureControlVisible(increase);
            increase.GrabFocus();await Frames(host);
            int prior=game.View(owner).Colonies[0].Workforce.Production;
            Input.ParseInputEvent(new InputEventKey{Keycode=Key.Space,Pressed=true});Input.ParseInputEvent(new InputEventKey{Keycode=Key.Space,Pressed=false});await Frames(host,5);
            check(issued==1&&game.View(owner).Colonies[0].Workforce.Production==prior+1,"Space activates the explicit workforce command once");
            var rebuilt=Descendants<Button>(panel).Single(b=>b.TooltipText=="Move a worker from support to production");
            check(rebuilt.HasFocus(),"Rebuilt workforce action restores its semantic focus after the accepted command");
            var decrease=Descendants<Button>(panel).Single(b=>b.TooltipText=="Move a worker from production to support");decrease.GrabFocus();await Frames(host);
            Input.ParseInputEvent(new InputEventKey{Keycode=Key.Enter,Pressed=true});Input.ParseInputEvent(new InputEventKey{Keycode=Key.Enter,Pressed=false});await Frames(host,5);
            check(issued==2&&original==SaveGame.Serialize(game.Content,game.State),"Enter activates one ordinary inverse workforce command without an extra submission");
        }
        finally{panel.CommandIssued-=observe;}
        Named<Button>(screen,"OpenGalaxyWorkspace").EmitSignal(Button.SignalName.Pressed);await Frames(host,4);
        var map=Descendants<GalaxyMap>(screen).Single();
        var marker=map.OwnedMarkerBounds.OrderBy(p=>p.Key).First();
        map._GuiInput(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true,Position=marker.Value.GetCenter()});await Frames(host,5);
        var review=Named<Button>(screen,"SelectionReviewOrders");
        check(review.HasFocus()&&review.IsVisibleInTree(),"Actual owned marker reveals and focuses the existing order-review action");
        check(original==SaveGame.Serialize(game.Content,game.State),"Actual owned marker focus and inspection issue no order or PRNG change");
    }

    private static async Task NestedOverviewReturn(Node host,GameScreen screen,GameSession game,Action<bool,string> check)
    {
        string before=SaveGame.Serialize(game.Content,game.State);
        Named<Button>(screen,"OpenFleetCommand").EmitSignal(Button.SignalName.Pressed);await Frames(host,4);
        var fleet=Descendants<FleetCommandDialog>(screen).Single();
        var officers=Named<Button>(fleet,"FleetOpenOfficers");officers.GrabFocus();officers.EmitSignal(Button.SignalName.Pressed);await Frames(host,5);
        var detail=Descendants<OfficerDetailDialog>(screen).Single();
        check(detail.Visible&&!fleet.Visible,"Nested overview opens the selected officer page from Fleet command");
        detail.GetOkButton().EmitSignal(Button.SignalName.Pressed);await Frames(host,5);
        check(fleet.Visible&&officers.HasFocus(),"Closing the officer overview returns to its invoking Fleet command and action focus");
        var designs=Named<Button>(fleet,"OpenShipDesigns");designs.GrabFocus();designs.EmitSignal(Button.SignalName.Pressed);await Frames(host,5);
        var catalogue=Descendants<ShipDesignDialog>(screen).Single();
        check(catalogue.Visible&&!fleet.Visible,"Nested overview opens the existing catalogue from Fleet command");
        catalogue.GetOkButton().EmitSignal(Button.SignalName.Pressed);await Frames(host,5);
        check(fleet.Visible&&designs.HasFocus(),"Closing the catalogue returns to its invoking Fleet command and action focus");
        fleet.GetOkButton().EmitSignal(Button.SignalName.Pressed);await Frames(host);
        check(before==SaveGame.Serialize(game.Content,game.State),"Nested menu return emits no command or campaign/PRNG change");
    }

    private static FieldInfo Field(string name)=>typeof(GameScreen).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!;
    private static void Refresh(GameScreen screen)=>typeof(GameScreen).GetMethod("Refresh",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(screen,null);
    private static T Named<T>(Node root,string name) where T:Node=>Descendants<T>(root).Single(n=>n.Name==name);
    private static async Task Frames(Node host,int count=3){for(int i=0;i<count;i++)await host.ToSignal(host.GetTree(),SceneTree.SignalName.ProcessFrame);}
    private static IEnumerable<T> Descendants<T>(Node root) where T:Node
    {foreach(Node child in root.GetChildren()){if(child is T typed)yield return typed;foreach(var nested in Descendants<T>(child))yield return nested;}}
}
