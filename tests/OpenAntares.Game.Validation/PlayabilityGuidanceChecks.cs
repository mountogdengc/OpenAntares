using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;

namespace OpenAntares.Game.Validation;

internal static partial class PlayabilityGuidanceChecks
{
    private const string EmptyCopy="No response notices. End Turn will check readiness.";
    private static object[] Build(Type type,EmpireView view)=>((IEnumerable)type.GetMethod("Build")!.Invoke(null,new object[]{view})!).Cast<object>().ToArray();
    private static string Property(object item,string name)=>item.GetType().GetProperty(name)!.GetValue(item)!.ToString()!;
    public static async Task Run(Node host,GameScreen screen,GameSession game,string output,Action<bool,string> check)
    {
        var type=typeof(GameScreen).Assembly.GetType("OpenAntares.Game.Presentation.TurnGuidance");
        check(type is not null,"RequiredGuidanceMatchesProjectedNotices: projection-only guidance exists");
        var owner=game.State.Empires.First(e=>e.Controller==OpenAntares.Simulation.State.ControllerKind.Human).Id;
        var view=game.View(owner);string before=SaveGame.Serialize(game.Content,game.State);
        var initial=Build(type!,view);
        check(initial.All(i=>Property(i,"Severity")!="Required")&&initial.Any(i=>Property(i,"Destination")=="Research"&&Property(i,"Text").Contains("saved",StringComparison.OrdinalIgnoreCase)),"No research target and no active project are optional and banking remains explicit");
        check(JsonSerializer.Serialize(initial)==JsonSerializer.Serialize(Build(type!,view)),"Repeated guidance build has stable ordering and content");
        var colony=view.Colonies[0];var vessel=view.Vessels[0];
        // Labelled requester-projection supplement: not a game state or earned encounter.
        var receipt=new CrisisReceipt(new(view.Viewer,1,colony.Id),4,view.Turn,new(colony.Id,colony.PlanetId,colony.StarId,"Known owned colony"),"Known urgent fact");
        var token=new AmebaAttentionToken(vessel.Id,1,3,AmebaStage.Warning,"response_required",null,300,0,100,0);
        var supplemental=view with
        {
            Combat=view.Combat! with{Notices=ImmutableArray.Create(new CombatNotice("test/one",CombatNoticeLevel.Emergency,"Known response",vessel.Id),new CombatNotice("test/two",CombatNoticeLevel.Emergency,"Another known response",vessel.Id))},
            Crisis=view.Crisis! with{PendingReceipts=ImmutableArray.Create(receipt)},
            Scouting=view.Scouting! with{Attention=ImmutableArray.Create(token),Vessels=view.Scouting!.Vessels.Select(v=>v.Id==vessel.Id?v with{Stage=AmebaStage.Warning,EncounterNumber=1,Revision=3,Acknowledged=false}:v).ToImmutableArray()},
            Conflict=view.Conflict! with{Encounters=ImmutableArray.Create(new OwnConflictEncounterView(new(1),new(2),colony.StarId,null,7,0,ConflictPhase.ResponseRequired,ImmutableArray.Create(vessel.Id),ImmutableArray<VesselId>.Empty,new(CombatDoctrine.Cautious,colony.Id,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair),false,true,ImmutableArray<MilitaryObservation>.Empty))}
        };
        var required=Build(type!,supplemental).Where(i=>Property(i,"Severity")=="Required").ToArray();
        check(required.Length==4&&required.Select(i=>Property(i,"Destination")).ToHashSet().SetEquals(new[]{"Combat","Conflict","Crisis","Scouts"}),"Four projected response kinds group once into their existing overview routes");
        check(required.Single(i=>Property(i,"Destination")=="Combat").GetType().GetProperty("Text")!.GetValue(required.Single(i=>Property(i,"Destination")=="Combat"))!.ToString()!.Contains("2",StringComparison.Ordinal),"Combat group counts only the two projected emergency notices");
        var changed=supplemental with{Crisis=supplemental.Crisis! with{RequireAcknowledgement=false},Conflict=supplemental.Conflict! with{Encounters=supplemental.Conflict!.Encounters.Select(e=>e with{Revision=8,OwnNeedsResponse=false}).ToImmutableArray()}};
        check(Build(type!,changed).Count(i=>Property(i,"Severity")=="Required")==2,"Changed own revisions and explicit saved crisis waiver remove only their current requirements");
        var foreign=supplemental with{ForeignVessels=supplemental.ForeignVessels.Add(new(new(int.MaxValue),new(int.MaxValue),vessel.RoleId,colony.StarId))};
        check(JsonSerializer.Serialize(Build(type!,foreign))==JsonSerializer.Serialize(Build(type!,supplemental)),"ForeignOrUnobservedFactsNeverEnterPresentation: unrelated foreign projection facts change no guidance text, counts or sorting");
        var panelType=typeof(GameScreen).Assembly.GetType("OpenAntares.Game.Components.TurnGuidancePanel");
        check(panelType is not null,"Actual guidance component exists");
        var panel=(Control)Activator.CreateInstance(panelType!)!;panel.Position=new(8,8);panel.Size=new(260,500);host.AddChild(panel);
        var routes=new List<string>();panelType!.GetEvent("DestinationRequested")!.AddEventHandler(panel,new Action<string>(routes.Add));
        panelType!.GetMethod("ShowGuidance")!.Invoke(panel,new object[]{supplemental});await Frames(host);
        check(Descendants<Button>(panel).Any(b=>b.Name=="ToggleOptionalGuidance"),"Approved Consider guidance has an explicit collapse control");
        check(!Descendants<Control>(panel).Single(c=>c.Name=="OptionalGuidanceBody").Visible,"Optional guidance initially collapses without hiding current response notices");
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1152,648),new Vector2I(1600,900),new Vector2I(1920,1080)})
        {
            host.GetTree().Root.Size=size;panelType.GetMethod("ShowGuidance")!.Invoke(panel,new object[]{supplemental});await Frames(host);
            var optionalToggle=Descendants<Button>(panel).Single(b=>b.Name=="ToggleOptionalGuidance");optionalToggle.GrabFocus();await Frames(host);
            Input.ParseInputEvent(new InputEventKey{Keycode=Key.Space,Pressed=true});Input.ParseInputEvent(new InputEventKey{Keycode=Key.Space,Pressed=false});await Frames(host);
            var optionalBody=Descendants<Control>(panel).Single(c=>c.Name=="OptionalGuidanceBody");
            check(optionalBody.Visible,$"{size}: keyboard Space expands optional explanations exactly once");
            foreach(var action in Descendants<Button>(optionalBody))action.EmitSignal(Button.SignalName.Pressed);
            check(routes.Contains("Research")&&routes.Contains("Colonies"),$"{size}: expanded Consider retains its exact optional overview routes");
            panelType.GetMethod("ShowGuidance")!.Invoke(panel,new object[]{supplemental});await Frames(host);
            check(Descendants<Control>(panel).Single(c=>c.Name=="OptionalGuidanceBody").Visible,$"{size}: routine refresh preserves expanded Consider");
            optionalToggle=Descendants<Button>(panel).Single(b=>b.Name=="ToggleOptionalGuidance");optionalToggle.GrabFocus();await Frames(host);
            Input.ParseInputEvent(new InputEventKey{Keycode=Key.Enter,Pressed=true});Input.ParseInputEvent(new InputEventKey{Keycode=Key.Enter,Pressed=false});await Frames(host);
            panelType.GetMethod("ShowGuidance")!.Invoke(panel,new object[]{supplemental});await Frames(host);
            optionalBody=Descendants<Control>(panel).Single(c=>c.Name=="OptionalGuidanceBody");
            check(!optionalBody.Visible&&Descendants<Button>(panel).Where(b=>b.Name.ToString().StartsWith("Guidance_",StringComparison.Ordinal)&&!optionalBody.IsAncestorOf(b)).All(b=>b.IsVisibleInTree()),$"{size}: Enter collapse survives refresh/resize and leaves Required links visible");
            check(before==SaveGame.Serialize(game.Content,game.State),$"{size}: collapse, refresh and optional navigation emit no command or PRNG change");
        }
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1152,648),new Vector2I(1600,900),new Vector2I(1920,1080)})
        {
            host.GetTree().Root.Size=size;panelType.GetMethod("ShowGuidance")!.Invoke(panel,new object[]{supplemental});await Frames(host);
            foreach(var action in Descendants<Button>(panel).Where(b=>b.IsVisibleInTree()&&b.Name.ToString().StartsWith("Guidance_",StringComparison.Ordinal)))action.EmitSignal(Button.SignalName.Pressed);
            check(new[]{"Combat","Conflict","Crisis","Scouts"}.All(routes.Contains),$"{size}: response links emit only existing overview destinations");
            check(Descendants<Label>(panel).All(l=>l.Size.X<=panel.Size.X+1),$"{size}: guidance descriptions wrap within their actual panel");
        }
        host.RemoveChild(panel);panel.QueueFree();await Frames(host);
        check(before==SaveGame.Serialize(game.Content,game.State),"Repeated guidance, resizing and projection supplement links issue no command or PRNG change");
        var live=Descendants<Control>(screen).SingleOrDefault(n=>n.GetType().Name=="TurnGuidancePanel");
        check(live is not null&&Descendants<Label>(live).Any(l=>l.Text==EmptyCopy),"Empty response notice text exactly defers readiness to End Turn");
        CombinedUiNavigation.Reveal(live!);await Frames(host);
        check(!Descendants<Button>(live!).Any(b=>b.Name=="ToggleOptionalGuidance")&&Descendants<Button>(live!).Where(b=>b.Name.ToString().StartsWith("Guidance_",StringComparison.Ordinal)).All(b=>b.IsVisibleInTree()),"Actual Main Decisions lists optional items directly without a collapse control");
        typeof(GameScreen).GetMethod("Refresh",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(screen,null);await Frames(host,5);
        check(Descendants<Button>(live!).Where(b=>b.Name.ToString().StartsWith("Guidance_",StringComparison.Ordinal)).All(b=>b.IsVisibleInTree())&&before==SaveGame.Serialize(game.Content,game.State),"Actual Main refresh keeps optional items visible without changing campaign/PRNG");
        var malicious=live!.GetType().GetField("DestinationRequested",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(live) as Action<string>;
        malicious!("EndTurn");await Frames(host);
        check(before==SaveGame.Serialize(game.Content,game.State),"A non-allowlisted guidance destination cannot execute Ready or another command");
        var end=Descendants<Button>(screen).Single(b=>b.Name=="EndTurn");check(!end.Disabled,"Optional guidance does not disable existing End Turn authority");
        var reference=new GameSession(game.Content,game.State.Clone());var expected=reference.Execute(new SetReadyCommand(owner,view.Turn,true));
        end.EmitSignal(Button.SignalName.Pressed);await Frames(host,5);
        check(expected.Accepted&&game.State.Turn==view.Turn+1&&SaveGame.Serialize(game.Content,game.State)==SaveGame.Serialize(reference.Content,reference.State),"OptionalGuidanceDoesNotBlockReadyOrSpendResearch: ordinary Ready matches the unchanged engine reference");
        check(game.View(owner).Research.TargetId is null&&game.View(owner).Research.Reserve>view.Research.Reserve,"Untargeted research remains banked after the explicit boundary");
        var report=Descendants<TurnReportDialog>(screen).Single();
        check(Descendants<Control>(report).Any(c=>c.Name=="ReportResponses")&&Descendants<Control>(report).Any(c=>c.Name=="ReportOutcomes")&&Descendants<Control>(report).Any(c=>c.Name=="ReportRoutine"),"Report groups current responses, outcomes and routine details without dropping existing rows");
        await Frames(host);
        check(host.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(output,"guidance-report.png"))==Error.Ok,"Actual guidance report frame retained");
    }
    private static async Task Frames(Node host,int count=3){for(int i=0;i<count;i++)await host.ToSignal(host.GetTree(),SceneTree.SignalName.ProcessFrame);}
    private static IEnumerable<T> Descendants<T>(Node root) where T:Node
    {foreach(Node child in root.GetChildren()){if(child is T typed)yield return typed;foreach(var nested in Descendants<T>(child))yield return nested;}}
}
