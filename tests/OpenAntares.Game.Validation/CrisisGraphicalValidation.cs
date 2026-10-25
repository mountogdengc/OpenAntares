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
        try{_output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output="))[20..];Directory.CreateDirectory(_output);Check(DisplayServer.GetName()!="headless","Real graphical display");await Attention();await AttentionScaleAndReload();await ResolvedReceiptReload();await FreshInterrupt();await Responses();}
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
    private async Task Attention()
    {
        var content=Content();var state=Labelled(content);var owner=state.Empires[0].Id;
        state.Colonies.First(c=>c.EmpireId==owner).Crisis!.Case!.AttentionRevision=3;
        Check(GameStateValidator.Validate(content,state).Count==0,"Urgent labelled case validates before UI session");
        string path=Path.Combine(_output,"crisis-prefs.json");new OpenAntares.Game.Presentation.CrisisNotificationPreferences(OpenAntares.Game.Presentation.CrisisDisplayLevel.Digest,OpenAntares.Game.Presentation.NotificationLevel.Off,ImmutableArray<string>.Empty).Save(path);
        var session=new GameSession(content,state);var screen=Screen(session,owner);AddChild(screen);await Frames();
        Check(!One<CrisisOverviewDialog>(screen).Visible,"Digest does not interrupt for initial urgent facts");
        Check(Descendants<Label>(screen).Any(l=>l.Name=="CrisisCriticalBadge"&&l.Visible&&l.Text.Contains("1 endangered",StringComparison.Ordinal)),"Digest retains persistent Critical badge and endangered count");
        string initial=Snapshot(session);Press(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));await Frames();Check(session.State.Turn==22&&Snapshot(session)==initial,"Unacknowledged emergency blocks next turn before any preparation");
        Press(Named<Button>(screen,"OpenCrisisNotifications"));await Frames();var notification=One<CrisisNotificationDialog>(screen);
        Check(Named<OptionButton>(notification,"CrisisDisplayLevel").Selected==2&&Named<OptionButton>(notification,"CrisisRoutineLevel").Selected==2&&Named<CheckBox>(notification,"CrisisRequireAcknowledgement").ButtonPressed,"Local Digest/Off and saved requirement appear as separate settings");
        foreach(int level in new[]{1,0,2})
        {
            Named<OptionButton>(notification,"CrisisDisplayLevel").Select(level);Press(Named<Button>(notification,"CrisisApplyDisplay"));await Frames();
            Check(Snapshot(session)==initial&&OpenAntares.Game.Presentation.CrisisNotificationPreferences.Load(path).Critical==(OpenAntares.Game.Presentation.CrisisDisplayLevel)level,$"Local display level {level} saves without touching canonical campaign");
            Check(Descendants<Label>(screen).Any(l=>l.Name=="CrisisCriticalBadge"&&l.Visible),$"Critical badge persists at display level {level}");
        }
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)}){GetTree().Root.Size=size;notification.PopupCenteredClamped(new(660,540));await Capture("attention-digest-"+size.X);}
        var requirement=Named<CheckBox>(notification,"CrisisRequireAcknowledgement");requirement.SetPressedNoSignal(false);requirement.EmitSignal(BaseButton.SignalName.Toggled,false);Press(Named<Button>(notification,"CrisisReviewAcknowledgement"));await Frames();
        var confirm=One<ConfirmationDialog>(notification);Check(confirm.DialogText.Contains("waived, not read",StringComparison.Ordinal)&&Snapshot(session)==initial,"Explicit waiver consequence shown before saved change");Press(confirm.GetOkButton());await Frames();
        Check(!session.State.FindEmpire(owner)!.Crisis!.RequireAcknowledgement&&session.State.Colonies.First(c=>c.EmpireId==owner).Crisis!.Case!.WaivedRevision==3,"Saved opt-out records waived revision, never read");
        Press(Named<Button>(screen,"OpenCrisisNotifications"));await Frames();notification=One<CrisisNotificationDialog>(screen);requirement=Named<CheckBox>(notification,"CrisisRequireAcknowledgement");requirement.SetPressedNoSignal(true);requirement.EmitSignal(BaseButton.SignalName.Toggled,true);Press(Named<Button>(notification,"CrisisReviewAcknowledgement"));await Frames();confirm=One<ConfirmationDialog>(notification);Press(confirm.GetOkButton());await Frames();
        Check(session.State.FindEmpire(owner)!.Crisis!.RequireAcknowledgement&&session.View(owner).Crisis!.PendingReceipts.Length==1,"Re-enable renews current urgent revision");
        Press(Named<Button>(screen,"OpenCrisisOverview"));await Frames();var overview=One<CrisisOverviewDialog>(screen);
        Check(Named<ItemList>(overview,"CrisisAttentionList").ItemCount==1,"Exact pending revision listed for acknowledgement");
        long displayedRevision=session.View(owner).Crisis!.PendingReceipts.Single().Revision;Press(Named<Button>(overview,"CrisisSelectAllAttention"));Press(Named<Button>(overview,"CrisisAcknowledgeSelected"));await Frames();
        Check(session.View(owner).Crisis!.PendingReceipts.IsEmpty&&session.State.Colonies.First(c=>c.EmpireId==owner).Crisis!.Case!.HandledRevision==displayedRevision,"Acknowledge selected handles the displayed revision once");
        Check(Descendants<Label>(screen).Any(l=>l.Name=="CrisisCriticalBadge"&&l.Visible),"Active endangered colony remains visible after acknowledgement");
        SaveEvidence(session,"labelled-attention-handled");RemoveChild(screen);screen.QueueFree();await Frames();
    }
    private async Task AttentionScaleAndReload()
    {
        var content=Content();var state=Labelled(content,30);var owner=state.Empires[0].Id;
        foreach(var c in state.Colonies.Where(c=>c.EmpireId==owner))c.Crisis!.Case!.AttentionRevision=3;
        var peer=state.Empires.Single(e=>e.Id!=owner);peer.Controller=ControllerKind.Human;peer.AiPolicyId=null;peer.Ready=true;
        Check(GameStateValidator.Validate(content,state).Count==0,"Labelled30-case two-human attention fixture validates before session: "+string.Join("; ",GameStateValidator.Validate(content,state)));
        string fixtureName=$"qa-{_runId}-attention-thirty-pending",fixturePath=SaveFiles.PathFor(fixtureName);SaveGame.WriteFile(fixturePath,content,state);_savePaths.Add(fixturePath);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();Press(Descendants<Button>(One<NewGameScreen>(main)).Single(b=>b.Text=="Start"));await Frames();
        var screen=await LoadThroughDialog(main,fixtureName);var session=Session(screen);Check(session.View(owner).Crisis!.PendingReceipts.Length==30,"Actual UI load retains all30 pending revisions");
        One<CrisisOverviewDialog>(screen).Hide();string before=Snapshot(session);Press(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));await Frames();
        Check(Snapshot(session)==before&&session.State.Empires.Single(e=>e.Id!=owner).Ready,"Blocked Ready preserves prior other-human Ready and all planning");
        string pendingName=await SaveThroughDialog(screen,"attention-thirty-still-pending");screen=await LoadThroughDialog(main,pendingName);session=Session(screen);
        Check(Snapshot(session)==before&&session.View(owner).Crisis!.PendingReceipts.Length==30,"Actual Save/Load never implicitly acknowledges thirty cases");
        One<CrisisOverviewDialog>(screen).Hide();Press(Named<Button>(screen,"OpenCrisisOverview"));await Frames();var overview=One<CrisisOverviewDialog>(screen);
        var itemList=Named<ItemList>(overview,"CrisisAttentionList");Check(itemList.ItemCount==30,"One grouped overview lists all30 own urgent facts");
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)}){GetTree().Root.Size=size;overview.PopupCenteredClamped(new(760,600));await Frames();itemList.Select(29);itemList.EnsureCurrentIsVisible();await Reach(itemList);await Capture("attention-thirty-"+size.X);}
        Press(Named<Button>(overview,"CrisisSelectAllAttention"));Check(itemList.GetSelectedItems().Length==30,"All30 exact revisions selected in one batch");
        Press(Named<Button>(overview,"CrisisAcknowledgeSelected"));await Frames();
        Check(session.View(owner).Crisis!.PendingReceipts.IsEmpty&&session.State.Colonies.Count(c=>c.EmpireId==owner&&c.Crisis!.Case!.HandledRevision==3)==30,"Grouped acknowledgement handles each displayed revision exactly once");
        Check(session.State.Empires.Single(e=>e.Id!=owner).Ready,"Other human's accepted Ready remains intact after acknowledgement");
        string handled=await SaveThroughDialog(screen,"attention-thirty-handled");string canonical=Snapshot(session);screen=await LoadThroughDialog(main,handled);session=Session(screen);
        Check(Snapshot(session)==canonical&&session.View(owner).Crisis!.PendingReceipts.IsEmpty,"Actual UI reload preserves handled revisions and canonical state");
        RemoveChild(main);main.QueueFree();await Frames();
    }
    private async Task ResolvedReceiptReload()
    {
        var content=Content();var state=Labelled(content);var owner=state.Empires[0].Id;var colony=state.Colonies.First(c=>c.EmpireId==owner);var location=CrisisQueries.Location(state,colony);
        state.Turn=40;state.Empires[0].Crisis!.Episode=null;state.Empires[0].Crisis!.PendingReceipts.Add(new(new(owner,1,colony.Id),3,20,location,"Resolved colony crisis awaiting acknowledgement."));colony.Crisis!.Case=null;ObservationUpdater.Refresh(state,state.Turn);
        Check(GameStateValidator.Validate(content,state).Count==0,"Labelled resolved receipt older than ten-turn history validates: "+string.Join("; ",GameStateValidator.Validate(content,state)));
        string name=$"qa-{_runId}-resolved-old-receipt",path=SaveFiles.PathFor(name);SaveGame.WriteFile(path,content,state);_savePaths.Add(path);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();Press(Descendants<Button>(One<NewGameScreen>(main)).Single(b=>b.Text=="Start"));await Frames();var screen=await LoadThroughDialog(main,name);var session=Session(screen);
        Check(session.View(owner).Crisis!.History.IsEmpty&&session.View(owner).Crisis!.PendingReceipts.Length==1,"Actual UI load retains resolved receipt beyond ten-turn history");
        One<CrisisOverviewDialog>(screen).Hide();string before=Snapshot(session);Press(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));await Frames();Check(Snapshot(session)==before&&session.State.Turn==40,"Resolved old receipt still blocks Ready after UI reload");
        Press(Named<Button>(screen,"OpenCrisisOverview"));await Frames();var overview=One<CrisisOverviewDialog>(screen);Check(Named<ItemList>(overview,"CrisisAttentionList").ItemCount==1,"Resolved lost-history receipt remains available for deliberate acknowledgement");
        Press(Named<Button>(overview,"CrisisSelectAllAttention"));Press(Named<Button>(overview,"CrisisAcknowledgeSelected"));await Frames();Check(session.View(owner).Crisis!.PendingReceipts.IsEmpty,"Explicit acknowledgement clears the old resolved receipt");
        RemoveChild(main);main.QueueFree();await Frames();
    }
    private async Task FreshInterrupt()
    {
        var content=Content();var state=Labelled(content);var owner=state.Empires[0].Id;state.Colonies.First(c=>c.EmpireId==owner).Crisis!.Case!.Severity=70;
        Check(GameStateValidator.Validate(content,state).Count==0,"Labelled threshold-crossing crisis validates before session");
        OpenAntares.Game.Presentation.CrisisNotificationPreferences.Defaults.Save(Path.Combine(_output,"crisis-prefs.json"));
        var session=new GameSession(content,state);var screen=Screen(session,owner);AddChild(screen);await Frames();Check(!One<CrisisOverviewDialog>(screen).Visible,"Initial screen restores saved facts without treating them as a new popup");
        Press(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));await Frames();var report=One<TurnReportDialog>(screen);Check(session.State.Turn==23&&report.Visible&&session.View(owner).Crisis!.PendingReceipts.Length==1,"Actual turn creates a new critical revision alongside one turn report");
        Press(report.GetOkButton());report.Hide();await Frames();var overview=One<CrisisOverviewDialog>(screen);Check(overview.Visible,"Interrupt opens one crisis overview after the report closes");
        overview.Hide();await Frames();Press(Named<Button>(screen,"ViewLatestTurnReport"));await Frames();report=One<TurnReportDialog>(screen);report.Hide();await Frames();Check(!overview.Visible,"Reopening and closing a report does not repeat an already-shown interruption");
        RemoveChild(screen);screen.QueueFree();await Frames();
    }
    private static GameSession Session(GameScreen screen)=>(GameSession)typeof(GameScreen).GetField("_session",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(screen)!;
    private async Task<string> SaveThroughDialog(GameScreen screen,string label)
    {
        string name=$"qa-{_runId}-{label}",path=SaveFiles.PathFor(name);if(File.Exists(path))throw new InvalidOperationException("QA save collision");
        One<CrisisOverviewDialog>(screen).Hide();await Frames();Press(Descendants<Button>(screen).Single(b=>b.Text=="Save"));await Frames();var dialog=One<SaveGameDialog>(screen);One<LineEdit>(dialog).Text=name;Press(dialog.GetOkButton());await Frames();Check(File.Exists(path),label+": UI Save writes file");_savePaths.Add(path);return name;
    }
    private async Task<GameScreen> LoadThroughDialog(Control main,string name)
    {
        var current=One<GameScreen>(main);One<CrisisOverviewDialog>(current).Hide();await Frames();Press(Descendants<Button>(current).Single(b=>b.Text=="Load"));await Frames();var dialog=One<LoadGameDialog>(main);var list=One<ItemList>(dialog);int index=Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemText(i).StartsWith(name+"    ",StringComparison.Ordinal));list.Select(index);list.EmitSignal(ItemList.SignalName.ItemSelected,index);Press(dialog.GetOkButton());await Frames();return One<GameScreen>(main);
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
    private GameScreen Screen(GameSession session,EmpireId owner)=>new(session,owner){NotificationPreferencesPath=Path.Combine(_output,"colony-prefs.json"),CrisisPreferencesPath=Path.Combine(_output,"crisis-prefs.json")};
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


