using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
namespace OpenAntares.Game.Validation;
public partial class CrisisGraphicalValidation:Node
{
    private string _output="";private readonly List<object> _results=new();private readonly List<string> _savePaths=new();
    private readonly string _runId=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
    public override async void _Ready()
    {
        string? error=null;
        try{_output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output="))[20..];Directory.CreateDirectory(_output);Check(DisplayServer.GetName()!="headless","Real graphical display");await Responses();}
        catch(Exception e){error=e.ToString();GD.PrintErr(error);}
        finally{if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{runId=_runId,scenario="Crisis",method="Actual Godot controls/signals; labelled pre-session fixtures, no physical input or human acceptance claim.",passed=error is null,error,results=_results,retainedQaSavePaths=_savePaths},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(error is null?0:1);}
    }
    private static ContentSet Content()=>ContentLoader.Load(new[]{new ContentSource("labelled-crisis",File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/crisis-content.json")))},requireCrises:true).Content!;
    private static GameState Labelled(ContentSet content,int count=1,int population=5)
    {
        var state=GalaxyGenerator.CreateNewGame(content,new("small",42,6,"harl",1,"peldra")).State!;state.Turn=22;var owner=state.Empires[0];var source=state.Colonies.First(c=>c.EmpireId==owner.Id);source.Population=population;source.Workforce=new(population,0,0);source.ProductionReserve=901;
        var root=CrisisQueries.Location(state,source);owner.Crisis!.NextEpisodeNumber=2;owner.Crisis.Episode=new(){Id=1,FamilyId="ash_bloom",Source=root,SourcePopulation=population,CureCost=600+100*population};
        source.Crisis!.Case=new(){EpisodeId=1,Stage=CrisisStage.Active,Severity=20,AttentionRevision=2,HandledRevision=2,Origin=root};
        for(int i=1;i<count;i++){var planet=state.FindPlanet(source.PlanetId)!.Clone();planet.Id=new(state.AllocateId());planet.Orbit=state.Planets.Where(p=>p.StarId==planet.StarId).Max(p=>p.Orbit)+1;planet.Appearance=OpenAntares.Simulation.Appearance.PlanetAppearance.Create(42,planet.Id);state.Planets.Add(planet);var colony=source.Clone();colony.Id=new(state.AllocateId());colony.PlanetId=planet.Id;state.Colonies.Add(colony);}
        ObservationUpdater.Refresh(state,state.Turn);return state;
    }
    private async Task Responses()
    {
        var content=Content();var state=Labelled(content,2);Check(GameStateValidator.Validate(content,state).Count==0,"N5 reserve901 labelled fixture validates before session");
        var session=new GameSession(content,state);var owner=state.Empires[0].Id;var first=state.Colonies.First(c=>c.EmpireId==owner);var other=state.Colonies.Last(c=>c.EmpireId==owner);
        var screen=Screen(session,owner);AddChild(screen);await Frames();
        var open=Descendants<Button>(screen).FirstOrDefault(b=>b.Name=="OpenCrisisOverview");Check(open is not null,"Actual crisis overview control exists");Press(open!);await Frames();
        var overview=One<CrisisOverviewDialog>(screen);var list=Named<ItemList>(overview,"CrisisCaseList");list.Select(0);string before=Snapshot(session);
        Press(Named<Button>(overview,"CrisisPurge"));await Frames();var response=One<CrisisResponseDialog>(screen);
        Check(Descendants<Label>(response).Any(l=>l.Text.Contains(Format.Points(450),StringComparison.Ordinal)),"Preview shows kept reserve4.5 points");
        Check(Descendants<Label>(response).Any(l=>l.Text.Contains("lose 3",StringComparison.Ordinal))&&Descendants<Label>(response).Any(l=>l.Text.Contains("expires at turn 27",StringComparison.Ordinal)),"Preview shows N5 loss3 and civic expiry T+5");
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)}){GetTree().Root.Size=size;response.PopupCenteredClamped(new(660,510));await Frames();await Reach(Named<Button>(response,"CrisisConfirmPurge"));await Capture("purge-preview-"+size.X);}
        Press(response.GetOkButton());response.Hide();await Frames();Check(before==Snapshot(session),"Cancel preserves exact campaign");
        Press(Named<Button>(overview,"CrisisPurge"));await Frames();
        var changed=session.Execute(new SetQuarantineCommand(owner,state.Turn,CrisisQueries.PreviewQuarantine(content,session.State,owner,ImmutableArray.Create(other.Id),true,false)));Check(changed.Accepted,"Ordinary other-case quarantine changes full preview consequences");string changedBefore=Snapshot(session);
        Press(Named<Button>(response,"CrisisConfirmPurge"));await Frames();Check(changedBefore==Snapshot(session)&&session.State.FindColony(first.Id)!.Population==5,"Stale confirmation rejects without loss");
        list=Named<ItemList>(overview,"CrisisCaseList");list.Select(0);Press(Named<Button>(overview,"CrisisPurge"));await Frames();Press(Named<Button>(response,"CrisisConfirmPurge"));await Frames();
        Check(session.State.FindColony(first.Id)!.Population==2&&session.State.FindColony(first.Id)!.ProductionReserve==450&&session.State.FindEmpire(owner)!.Crisis!.CivicLevel==1,"Actual fresh purge applies displayed costs");
        var afterPurge=Snapshot(session);var allFilter=Named<OptionButton>(overview,"CrisisFilter");allFilter.Select(1);allFilter.EmitSignal(OptionButton.SignalName.ItemSelected,1);list=Named<ItemList>(overview,"CrisisCaseList");list.Select(0);Press(Named<Button>(overview,"CrisisQuarantineKeep"));await Frames();
        Check(response.Name=="CrisisQuarantinePreview"&&Descendants<Label>(response).Any(l=>l.Text.Contains("Owned cargo ports closed",StringComparison.Ordinal)),"Normal containment preview describes shared port and economics");
        Press(response.GetOkButton());response.Hide();await Frames();Check(afterPurge==Snapshot(session),"Quarantine preview cancel preserves canonical state");
        Press(Named<Button>(overview,"CrisisQuarantineKeep"));await Frames();Press(Named<Button>(response,"CrisisConfirmQuarantine"));await Frames();Check(session.State.FindColony(first.Id)!.Crisis!.Quarantined,"Actual quarantine response follows its full preview");
        Press(Named<Button>(overview,"CrisisSetAuthority"));await Frames();Check(Named<OptionButton>(overview,"CrisisAuthorityPicker").ItemCount==3,"All three explicit crisis authority choices are available");
        One<CrisisAuthorityDialog>(overview).Hide();
        SaveEvidence(session,"labelled-n5-response");RemoveChild(screen);screen.QueueFree();await Frames();
        state=Labelled(content,1,1);session=new(content,state);screen=Screen(session,owner);AddChild(screen);await Frames();Press(Named<Button>(screen,"OpenCrisisOverview"));await Frames();overview=One<CrisisOverviewDialog>(screen);Named<ItemList>(overview,"CrisisCaseList").Select(0);Press(Named<Button>(overview,"CrisisPurge"));await Frames();response=One<CrisisResponseDialog>(screen);
        Check(Descendants<Label>(response).Any(l=>l.Text.Contains("last colony",StringComparison.Ordinal)&&l.Text.Contains("existing founder",StringComparison.Ordinal)),"N1 confirmation explains assets and recovery");
        Press(Named<Button>(response,"CrisisConfirmPurge"));await Frames();Check(session.View(owner).Colonies.IsEmpty,"Selected N1 colony removal refreshes safely");
        Check(Descendants<Label>(overview).Any(l=>l.Text.Contains("No colonies remain",StringComparison.Ordinal)),"Zero-colony overview explains surviving founder recovery");
        Check(Descendants<Label>(overview).Any(l=>l.Text.Contains(first.Id.ToString(),StringComparison.Ordinal)&&l.Text.Contains("Lost colony",StringComparison.Ordinal)),"Stored loss location remains visible");await Capture("last-colony-loss");SaveEvidence(session,"labelled-n1-loss");RemoveChild(screen);screen.QueueFree();await Frames();
        foreach(int count in new[]{1,6,11,30})
        {
            state=Labelled(content,count);Check(GameStateValidator.Validate(content,state).Count==0,$"Labelled {count}-case scale fixture validates before session");session=new(content,state);screen=Screen(session,owner);AddChild(screen);await Frames();before=Snapshot(session);Press(Named<Button>(screen,"OpenCrisisOverview"));await Frames();overview=One<CrisisOverviewDialog>(screen);list=Named<ItemList>(overview,"CrisisCaseList");Check(list.ItemCount==count,$"All {count} own cases shown");
            foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)}){GetTree().Root.Size=size;overview.PopupCenteredClamped(new(760,600));await Frames();list.Select(count-1);list.EnsureCurrentIsVisible();await Reach(list);await Reach(Named<Button>(overview,"CrisisPurge"));await Capture($"overview-{count}-{size.X}");Check(overview.GetOkButton().Visible,$"Close reachable {count}/{size.X}");}
            Check(before==Snapshot(session),$"Opening/selecting/scrolling {count} cases preserves exact state");RemoveChild(screen);screen.QueueFree();await Frames();
        }
    }
    private GameScreen Screen(GameSession session,EmpireId owner)=>new(session,owner){NotificationPreferencesPath=Path.Combine(_output,"colony-prefs.json")};
    private void SaveEvidence(GameSession session,string label){string path=SaveFiles.PathFor($"qa-{_runId}-crisis-{label}");SaveGame.WriteFile(path,session.Content,session.State);_savePaths.Add(path);}
    private static T Named<T>(Node n,string name)where T:Node=>Descendants<T>(n).Single(c=>c.Name==name);
    private async Task Reach(Control c){c.GrabFocus();await Frames();for(Node? p=c.GetParent();p is not null;p=p.GetParent())if(p is ScrollContainer scroll)scroll.EnsureControlVisible(c);await Frames();Check(c.IsVisibleInTree(),"Reach control "+c.Name);}
    private void Check(bool yes,string name){_results.Add(new{name,passed=yes});if(!yes)throw new InvalidOperationException(name);}
    private static IEnumerable<T> Descendants<T>(Node n)where T:Node{foreach(Node c in n.GetChildren()){if(c is T t)yield return t;foreach(var d in Descendants<T>(c))yield return d;}}
    private static void Press(Button b){if(b.Disabled)throw new InvalidOperationException("Disabled: "+b.Text);b.EmitSignal(BaseButton.SignalName.Pressed);}
    private async Task Frames(){await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private async Task Capture(string name){await Frames();await Frames();await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);GetViewport().GetTexture().GetImage().SavePng(Path.Combine(_output,name+".png"));}
    private static T One<T>(Node n)where T:Node=>Descendants<T>(n).Single();
    private static string Snapshot(GameSession s)=>SaveGame.Serialize(s.Content,s.State);
}

