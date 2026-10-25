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
using OpenAntares.Simulation.Turns;
namespace OpenAntares.Game.Validation;
public partial class CrisisGraphicalValidation:Node
{
    private string _output="";private readonly List<object> _results=new();private readonly List<string> _savePaths=new();private bool _combinedReportSeen;
    private readonly string _runId=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
    public override async void _Ready()
    {
        string? error=null;
        try{_output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output="))[20..];Directory.CreateDirectory(_output);Check(DisplayServer.GetName()!="headless","Real graphical display");await CurrentSetup();await Attention();await AttentionScaleAndReload();await ResolvedReceiptReload();await FreshInterrupt();await WaivedInterrupt();await GeneratedCampaigns();await GeneratedNeglect();await GeneratedCargoFounding();await LabelledPortWait();await ResponseReviewRegressions();await CompletionLossNavigation();await MinorReviewRegressions();await Responses();}
        catch(Exception e){error=e.ToString();GD.PrintErr(error);}
        finally{if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{runId=_runId,scenario="Crisis",method="Actual Godot controls/signals; labelled pre-session fixtures, no physical input or human acceptance claim.",passed=error is null,error,results=_results,retainedQaSavePaths=_savePaths},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(error is null?0:1);}
    }
    private static ContentSet CoreWithApprovedPlague()
    {
        var files=DirAccess.GetFilesAt("res://content/core").Where(f=>f.EndsWith(".json",StringComparison.Ordinal)).OrderBy(f=>f,StringComparer.Ordinal).Select(f=>new ContentSource(f,Godot.FileAccess.GetFileAsString("res://content/core/"+f)));
        var result=ContentLoader.Load(files,requireSpecies:true,requireRivals:true,requireDevelopment:true,requireCrises:true);
        return result.Content??throw new InvalidOperationException("Complete crisis core did not load: "+string.Join("; ",result.Errors));
    }
    private static ContentSet Content()=>ContentLoader.Load(new[]{new ContentSource("labelled-crisis",File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/crisis-content.json")))},requireCrises:true).Content!;
    private async Task CurrentSetup()
    {
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();var setup=One<NewGameScreen>(main);setup.Configure("small",42);
        Check(Named<OptionButton>(setup,"SpeciesPicker").GetSelectedMetadata().AsString()=="harl"&&Named<OptionButton>(setup,"RivalCountPicker").Selected==1&&Named<OptionButton>(setup,"RivalSpeciesPicker").GetSelectedMetadata().AsString()=="peldra"&&Named<OptionButton>(setup,"CrisisModePicker").Selected==0,"Actual current setup defaults Harl, One Peldra rival and Standard crises");
        Press(Descendants<Button>(setup).Single(b=>b.Text=="Start"));await Frames();var session=Session(One<GameScreen>(main));
        Check(session.State.RulesVersion==6&&session.State.Crisis!.Enabled&&session.State.FindEmpire(session.State.Empires[0].Id)!.Crisis is {Authority:CrisisAuthority.Manual,RequireAcknowledgement:true},"Actual current Start uses rules6 Standard and manual required acknowledgement");
        RemoveChild(main);main.QueueFree();await Frames();
        main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();setup=One<NewGameScreen>(main);setup.Configure("small",42,"harl",1,"peldra",false);
        Check(Named<OptionButton>(setup,"CrisisModePicker").Selected==1&&Descendants<Label>(setup).Any(l=>l.Name=="SpeciesSummary"&&l.Text.Contains("Colony crises: Off",StringComparison.Ordinal)),"Actual Off control updates new-game preview without starting a campaign");
        Press(Descendants<Button>(setup).Single(b=>b.Text=="Start"));await Frames();session=Session(One<GameScreen>(main));
        Check(session.State.RulesVersion==6&&!session.State.Crisis!.Enabled,"Actual Off Start keeps rules6 crisis streams dormant");
        RemoveChild(main);main.QueueFree();await Frames();
        var incomplete=CoreWithApprovedPlague() with{Plagues=ImmutableSortedDictionary<string,PlagueDefinition>.Empty};
        setup=new NewGameScreen(incomplete);AddChild(setup);await Frames();Check(Descendants<Button>(setup).Single(b=>b.Text=="Start").Disabled,"Missing current plague prevents even an Off new-game start");bool canLoad=false;setup.LoadRequested+=()=>canLoad=true;Press(Descendants<Button>(setup).Single(b=>b.Text=="Load Game"));Check(canLoad,"Missing current plague still offers old-save Load recovery");RemoveChild(setup);setup.QueueFree();await Frames();
    }
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
    private async Task WaivedInterrupt()
    {
        var content=Content();var state=Labelled(content);var owner=state.Empires[0].Id;var illness=state.Colonies.First(c=>c.EmpireId==owner).Crisis!.Case!;
        illness.Severity=70;illness.WaivedRevision=illness.AttentionRevision;state.Empires[0].Crisis!.RequireAcknowledgement=false;
        Check(GameStateValidator.Validate(content,state).Count==0,"Waived Interrupt fixture validates");
        OpenAntares.Game.Presentation.CrisisNotificationPreferences.Defaults.Save(Path.Combine(_output,"crisis-prefs.json"));
        var session=new GameSession(content,state);var screen=Screen(session,owner);AddChild(screen);await Frames();
        Check(!One<CrisisOverviewDialog>(screen).Visible,"Waived initial threat does not interrupt on load");
        Press(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));await Frames();var report=One<TurnReportDialog>(screen);
        Check(report.Visible&&session.View(owner).Crisis!.PendingReceipts.IsEmpty,"New escalation is waived without a Ready blocker");
        report.Hide();await Frames();Check(One<CrisisOverviewDialog>(screen).Visible,"New waived material still honors local Interrupt preference");
        RemoveChild(screen);screen.QueueFree();await Frames();
    }
    private async Task ResponseReviewRegressions()
    {
        var content=Content();var state=Labelled(content,1,1);var owner=state.Empires[0].Id;var source=state.Colonies.First(c=>c.EmpireId==owner);
        source.Crisis!.Case!.Severity=80;source.Crisis.Case.ActiveAge=3;state.Empires[0].Crisis!.Episode!.ResearchShare=0;
        var session=new GameSession(content,state);var screen=Screen(session,owner);AddChild(screen);await Frames();Press(Named<Button>(screen,"OpenCrisisOverview"));await Frames();var overview=One<CrisisOverviewDialog>(screen);
        Check(Named<ItemList>(overview,"CrisisCaseList").GetItemText(0).Contains("IMMINENT COLONY LOSS",StringComparison.Ordinal),"N1 overview names imminent colony loss");
        Named<ItemList>(overview,"CrisisCaseList").Select(0);Press(Named<Button>(overview,"CrisisQuarantineKeep"));await Frames();var response=One<CrisisResponseDialog>(screen);
        Check(Descendants<Label>(response).Any(l=>l.Text.Contains("Imminent colony loss",StringComparison.Ordinal)&&l.Text.Contains("does not reset severity",StringComparison.Ordinal)),"Late quarantine warns that the colony will be lost despite containment");
        response.Hide();await Frames();RemoveChild(screen);screen.QueueFree();await Frames();

        var plague=content.Plagues["ash_bloom"];content=content with{Plagues=content.Plagues.SetItem(plague.Id,plague with{CivicFactorLossBp=2000})};state=Labelled(content);session=new(content,state);screen=Screen(session,owner);AddChild(screen);await Frames();Press(Named<Button>(screen,"OpenCrisisOverview"));await Frames();overview=One<CrisisOverviewDialog>(screen);Named<ItemList>(overview,"CrisisCaseList").Select(0);Press(Named<Button>(overview,"CrisisPurge"));await Frames();response=One<CrisisResponseDialog>(screen);
        Check(Descendants<Label>(response).Any(l=>l.Text.Contains("penalty: 20%",StringComparison.Ordinal)),"Edited plague civic factor is stated exactly before purge");
        response.Hide();await Frames();RemoveChild(screen);screen.QueueFree();await Frames();

        content=Content();state=Labelled(content);owner=state.Empires[0].Id;var ship=state.Vessels.Single(v=>v.EmpireId==owner);ship.RoleId="harl_settler";ship.CarriedPopulation=1;ship.StationaryStarId=state.FindPlanet(state.Colonies.First(c=>c.EmpireId==owner).PlanetId)!.StarId;ship.Transit=null;ship.Crisis!.CarrierEpisode=1;ship.PendingMove=state.Stars.First(s=>s.Id!=ship.StationaryStarId).Id;
        Check(GameStateValidator.Validate(content,state).Count==0,"Pending infectious founder fixture validates");
        var vesselView=CrisisQueries.Project(content,state,owner).Vessels.Single(v=>v.Id==ship.Id);
        Check(vesselView.CanDecontaminate,$"Pending founder is inspectable at owned port: ship {ship.StationaryStarId}, port {state.FindPlanet(state.Colonies.First(c=>c.EmpireId==owner).PlanetId)!.StarId}, carrier {ship.Crisis.CarrierEpisode}, hold {ship.Crisis.InspectionDueTurn}");
        session=new(content,state);screen=Screen(session,owner);AddChild(screen);await Frames();Press(Named<Button>(screen,"OpenCrisisOverview"));await Frames();overview=One<CrisisOverviewDialog>(screen);string before=Snapshot(session);
        Named<ItemList>(overview,"CrisisCaseList").Select(0);Press(Named<Button>(overview,"CrisisQuarantineKeep"));await Frames();response=One<CrisisResponseDialog>(screen);
        Check(Descendants<Label>(response).Any(l=>l.Text.Contains("Cancel vessel "+ship.Id,StringComparison.Ordinal)&&l.Text.Contains("move to system",StringComparison.Ordinal)),"Quarantine lists the exact pending cargo order it would cancel");
        response.Hide();await Frames();Check(Snapshot(session)==before,"Cancelling quarantine preserves infectious founder order");
        var inspect=Descendants<Button>(overview).FirstOrDefault(b=>b.Name.ToString().StartsWith("CrisisInspectVessel_",StringComparison.Ordinal));Check(inspect is not null,"Inspect control exists: "+string.Join(",",Descendants<Button>(overview).Select(b=>b.Name.ToString())));Press(inspect!);await Frames();response=One<CrisisResponseDialog>(screen);
        Check(response.Visible&&Descendants<Label>(response).Any(l=>l.Text.Contains("Cancel pending move",StringComparison.Ordinal)),"Inspection previews exact cancelled move before mutation");
        Check(Descendants<Label>(response).Any(l=>l.Text.Contains("contaminate the cargo again",StringComparison.Ordinal)),"Open-port inspection warns of continuing contact risk");
        response.Hide();await Frames();Check(Snapshot(session)==before,"Cancelling inspection preserves campaign and pending order");
        Press(Descendants<Button>(overview).Single(b=>b.Name.ToString().StartsWith("CrisisInspectVessel_",StringComparison.Ordinal)));await Frames();Press(Named<Button>(response,"CrisisConfirmInspection"));await Frames();
        Check(session.State.FindVessel(ship.Id) is {PendingMove:null,Crisis:{InspectionDueTurn:23}},"Confirmed inspection cancels displayed move and holds full interval");
        RemoveChild(screen);screen.QueueFree();await Frames();
    }
    private async Task CompletionLossNavigation()
    {
        var content=Content();var state=Labelled(content,2,1);var owner=state.Empires[0].Id;var lost=state.Colonies.First(c=>c.EmpireId==owner);var survivor=state.Colonies.Last(c=>c.EmpireId==owner);
        survivor.Crisis!.Case=null;lost.Crisis!.Case!.Severity=80;lost.Crisis.Case.ActiveAge=3;
        lost.CompletedBuildingIds.Add("fabrication_hall");state.FindEmpire(owner)!.KnownTechnologyIds=content.Technologies.Keys.ToList();lost.ProjectKind="building";lost.ProjectId="analysis_lab";lost.ProductionReserve=1200;
        Check(GameStateValidator.Validate(content,state).Count==0,"Completion then colony-loss fixture validates");
        var resolved=TurnResolver.Resolve(content,state);Check(resolved.Succeeded&&resolved.State.FindColony(lost.Id) is null&&resolved.State.FindColony(survivor.Id) is not null,"Real resolution completes then loses only source colony");
        var report=EmpireViewProjector.ProjectReport(content,resolved.State,resolved.Report!,owner);Check(report.Colonies.Any(c=>c.Id==lost.Id&&c.Economy.Production.CompletedId=="analysis_lab"),"Historical report retains lost colony completion");
        var dialog=new TurnReportDialog();AddChild(dialog);await Frames();dialog.ShowReport(content,report,EmpireViewProjector.Project(content,resolved.State,owner));await Frames();
        Check(Descendants<Label>(dialog).Any(l=>l.Text.Contains("colony was lost after completion",StringComparison.Ordinal))&&!Descendants<Button>(dialog).Any(b=>b.Text=="Choose next project"),"Lost completion remains visible without a stale project action for surviving colony");
        RemoveChild(dialog);dialog.QueueFree();await Frames();
    }
    private async Task MinorReviewRegressions()
    {
        var content=Content();var state=Labelled(content);var owner=state.Empires[0].Id;state.Colonies.First(c=>c.EmpireId==owner).Crisis!.Case=null;
        var ship=state.Vessels.Single(v=>v.EmpireId==owner);ship.RoleId="harl_settler";ship.CarriedPopulation=1;ship.Crisis!.CarrierEpisode=1;
        Check(GameStateValidator.Validate(content,state).Count==0,"Carrier-only episode validates");var session=new GameSession(content,state);var screen=Screen(session,owner);AddChild(screen);await Frames();
        Check(Descendants<Label>(screen).Any(l=>l.Name=="CrisisCriticalBadge"&&l.Text.Contains("1 infectious cargo",StringComparison.Ordinal)),"Carrier-only episode keeps persistent Critical threat badge");
        RemoveChild(screen);screen.QueueFree();await Frames();
        state=Labelled(content,2);owner=state.Empires[0].Id;var source=state.Colonies.First(c=>c.EmpireId==owner);source.Crisis!.Case!.Stage=CrisisStage.Warning;source.Crisis.Case.GraceRemaining=2;source.Crisis.Case.ActiveAge=0;source.Crisis.Case.Severity=0;
        var protectedColony=state.Colonies.Last(c=>c.EmpireId==owner);protectedColony.Crisis!.Case=null;protectedColony.Crisis.ImmunityFamilyId="ash_bloom";protectedColony.Crisis.ImmuneUntilTurn=30;
        Check(GameStateValidator.Validate(content,state).Count==0,"Warning and immune colony view validates");session=new(content,state);screen=Screen(session,owner);AddChild(screen);await Frames();Press(Named<Button>(screen,"OpenCrisisOverview"));await Frames();var overview=One<CrisisOverviewDialog>(screen);
        Check(Named<ItemList>(overview,"CrisisCaseList").GetItemText(0).Contains("recovery in 8 intervals including warning grace",StringComparison.Ordinal),"Warning recovery count includes grace");
        var filter=Named<OptionButton>(overview,"CrisisFilter");filter.Select(1);filter.EmitSignal(OptionButton.SignalName.ItemSelected,1);
        Check(Enumerable.Range(0,Named<ItemList>(overview,"CrisisCaseList").ItemCount).Any(i=>Named<ItemList>(overview,"CrisisCaseList").GetItemText(i).Contains("immune to ash_bloom until turn 30",StringComparison.Ordinal)),"All-colony overview displays current immunity expiry");
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
    private async Task GeneratedCampaigns()
    {
        var content=CoreWithApprovedPlague();
        foreach(string species in new[]{"harl","peldra","sulan","wenth"})
        {
            string label="generated-"+species;var generated=GalaxyGenerator.CreateNewGame(content,new("small",42,6,species,1,"peldra",true));Check(generated.Succeeded,label+": explicit rules6 Standard seed42 generation succeeds");
            var initial=generated.State!;string initialCanonical=SaveGame.Serialize(content,initial);string initialHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(initialCanonical))).ToLowerInvariant();
            Check(initial.Turn==0&&initial.Empires.Count(e=>e.Controller==ControllerKind.Ai)==1&&initial.Crisis!.Enabled,label+": untouched generated turn0 with one rival and Standard crises");
            var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();var setup=One<NewGameScreen>(main);setup.Configure("small",42,species,1,"peldra",true);Press(Descendants<Button>(setup).Single(b=>b.Text=="Start"));await Frames();
            var screen=One<GameScreen>(main);var session=Session(screen);var owner=session.State.Empires.Single(e=>e.Controller==ControllerKind.Human).Id;
            screen.CrisisPreferencesPath=Path.Combine(_output,"crisis-prefs.json");Check(Snapshot(session)==initialCanonical,label+": actual New Game Start produces untouched generated initial state and complete core content");
            await SaveThroughDialog(screen,label+"-initial");
            _results.Add(new{name=label+": generated provenance",passed=true,source="complete content/core/*.json",species,seed=42,rivals=1,rules=6,initialHash,turn=0,liveOutcomeInjections=0});
            Select(Picker(screen,"GovernorTemplatePicker"),"industrial");Press(Descendants<Button>(One<ColonyPanel>(screen)).Single(b=>b.Text=="Apply template"));await Frames();
            Toggle(screen,"WorkforceGovernorToggle",true);await Frames();Toggle(screen,"ConstructionGovernorToggle",true);await Frames();
            await QueueGeneratedProject(screen,"building","fabrication_hall");
            var homeId=session.State.Colonies.Single(c=>c.EmpireId==owner).Id;
            Check(session.State.FindColony(homeId)!.Development is {WorkforceEnabled:true,ConstructionEnabled:true},label+": actual colony controls enable Industrial workforce and construction");
            for(int i=0;i<20;i++)
            {
                await ChooseResearch(screen,owner);await EndGeneratedTurn(screen,owner);
            }
            var onset=session.View(owner).Crisis!;Check(session.State.Turn==20&&onset.Cases.Length==1&&onset.Cases[0].Stage==CrisisStage.Warning&&onset.Cases[0].GraceRemaining==2,label+": spontaneous human Warning2 occurs exactly at turn20");
            Check(session.State.FindColony(homeId)!.CompletedBuildingIds.Contains("fabrication_hall"),label+": ordinary foundation completes before the spontaneous crisis");
            SaveEvidence(session,label+"-onset");
            await AcknowledgeGenerated(screen,owner);await ChooseResearch(screen,owner);await EndGeneratedTurn(screen,owner);
            Check(session.State.Turn==21&&session.View(owner).Crisis!.Cases.Single().GraceRemaining==1,label+": first warning interval retains grace1");
            await AcknowledgeGenerated(screen,owner);await ChooseResearch(screen,owner);await EndGeneratedTurn(screen,owner);
            Check(session.State.Turn==22&&session.View(owner).Crisis!.Cases.Single().Stage==CrisisStage.Active,label+": second warning interval becomes Active");
            await AcknowledgeGenerated(screen,owner);
            Select(Picker(screen,"GovernorTemplatePicker"),"research");Press(Descendants<Button>(One<ColonyPanel>(screen)).Single(b=>b.Text=="Apply template"));await Frames();
            Press(Named<Button>(screen,"OpenCrisisOverview"));await Frames();var overview=One<CrisisOverviewDialog>(screen);Named<ItemList>(overview,"CrisisCaseList").Select(0);
            Press(Named<Button>(overview,"CrisisQuarantine"));await Frames();var response=One<CrisisResponseDialog>(screen);Press(Named<Button>(response,"CrisisConfirmQuarantine"));await Frames();
            Check(session.State.FindColony(homeId)!.Crisis!.Quarantined,label+": actual preview and confirm contain the generated source");
            overview=One<CrisisOverviewDialog>(screen);var share=Named<OptionButton>(overview,"CrisisResearchShare");share.Select(3);share.EmitSignal(OptionButton.SignalName.ItemSelected,3);await Frames();
            Check(session.View(owner).Crisis!.Episode!.ResearchShare==100,label+": actual cure-share control allocates future research");
            var beforeCure=session.View(owner).Crisis!.Episode!.CureProgress;
            await AcknowledgeGenerated(screen,owner);await ChooseResearch(screen,owner);await EndGeneratedTurn(screen,owner);
            Check(session.View(owner).Crisis!.Episode!.CureProgress>beforeCure,label+": ordinary research increases cure progress after containment; before="+beforeCure+", after="+session.View(owner).Crisis!.Episode!.CureProgress+", forecast="+session.View(owner).Crisis!.Episode!.Forecast.Diverted);
            await AcknowledgeGenerated(screen,owner);
            string saved=await SaveThroughDialog(screen,label+"-contained");var reference=new GameSession(session.Content,session.State.Clone());
            screen=await LoadThroughDialog(main,saved);session=Session(screen);screen.CrisisPreferencesPath=Path.Combine(_output,"crisis-prefs.json");
            Check(Snapshot(session)==Snapshot(reference),label+": actual UI load preserves complete generated state");
            for(int i=0;i<3;i++)
            {
                await AcknowledgeGenerated(screen,owner);var pending=reference.View(owner).Crisis!.PendingReceipts;if(!pending.IsEmpty)Check(reference.Execute(new AcknowledgeCrisisCommand(owner,reference.State.Turn,pending)).Accepted,label+": reference acknowledges exact current facts");
                await ChooseResearch(screen,owner);var target=session.View(owner).Research.TargetId;
                if(target!=reference.View(owner).Research.TargetId&&target is not null)Check(reference.Execute(new SetResearchTargetCommand(owner,reference.State.Turn,target)).Accepted,label+": reference follows ordinary selected research");
                Check(reference.Execute(new SetReadyCommand(owner,reference.State.Turn,true)).Accepted,label+": uninterrupted reference resolves next turn");
                await EndGeneratedTurn(screen,owner);Check(Snapshot(session)==Snapshot(reference),label+": UI reload future turn "+(i+1)+" equals uninterrupted canonical state");
            }
            SaveEvidence(session,label+"-continued");RemoveChild(main);main.QueueFree();await Frames();
        }
    }
    private async Task ChooseResearch(GameScreen screen,EmpireId owner)
    {
        if(Session(screen).View(owner).Research.TargetId is not null)return;
        var picker=One<OptionButton>(One<ResearchBar>(screen));if(picker.ItemCount>1){picker.Select(1);picker.EmitSignal(OptionButton.SignalName.ItemSelected,1);await Frames();}
    }
    private async Task AcknowledgeGenerated(GameScreen screen,EmpireId owner)
    {
        One<CrisisOverviewDialog>(screen).Hide();await Frames();if(Session(screen).View(owner).Crisis!.PendingReceipts.IsEmpty)return;
        Press(Named<Button>(screen,"OpenCrisisOverview"));await Frames();var overview=One<CrisisOverviewDialog>(screen);Press(Named<Button>(overview,"CrisisSelectAllAttention"));Press(Named<Button>(overview,"CrisisAcknowledgeSelected"));await Frames();
        Check(Session(screen).View(owner).Crisis!.PendingReceipts.IsEmpty,"Generated campaign acknowledges every current own urgent fact through the actual grouped UI");overview.Hide();await Frames();
    }
    private async Task EndGeneratedTurn(GameScreen screen,EmpireId owner)
    {
        int before=Session(screen).State.Turn;One<CrisisOverviewDialog>(screen).Hide();await Frames();Press(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));await Frames();
        Check(Session(screen).State.Turn==before+1,"Generated campaign resolves one ordinary turn through End Turn");var report=One<TurnReportDialog>(screen);if(report.Visible)
        {
            var lines=Descendants<Label>(report).Select(l=>l.Text).ToArray();
            if(lines.Any(s=>s.Contains("Colony founded",StringComparison.Ordinal))&&lines.Any(s=>s.Contains("exposure",StringComparison.Ordinal)))
            {
                _combinedReportSeen=true;Check(true,"Actual turn report combines colony founding with secondary crisis exposure");var scroll=Descendants<ScrollContainer>(report).First();scroll.ScrollVertical=int.MaxValue;await Frames();await Capture("combined-founding-crisis-report");
            }
            report.Hide();await Frames();
        }
        One<CrisisOverviewDialog>(screen).Hide();await Frames();
    }
    private static OptionButton Picker(Node node,string name)=>Named<OptionButton>(node,name);
    private static void Select(OptionButton picker,string id){int index=Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsString()==id);picker.Select(index);picker.EmitSignal(OptionButton.SignalName.ItemSelected,index);}
    private static void Toggle(Node node,string name,bool value){var box=Named<CheckBox>(node,name);box.SetPressedNoSignal(value);box.EmitSignal(BaseButton.SignalName.Toggled,value);}
    private async Task QueueGeneratedProject(GameScreen screen,string kind,string id)
    {
        Press(Descendants<Button>(One<ColonyPanel>(screen)).Single(b=>b.Text=="Edit queue"));await Frames();var editor=One<ConstructionQueueEditor>(screen);Select(Picker(editor,"QueueTargetPicker"),kind+"|"+id);Press(Descendants<Button>(editor).Single(b=>b.Text=="Add project"));Press(editor.GetOkButton());await Frames();
    }
    private async Task GeneratedNeglect()
    {
        var content=CoreWithApprovedPlague();var generated=GalaxyGenerator.CreateNewGame(content,new("small",42,6,"harl",1,"peldra",true));Check(generated.Succeeded,"Generated Harl neglect starts from untouched Standard seed42");
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();var setup=One<NewGameScreen>(main);setup.Configure("small",42,"harl",1,"peldra",true);Press(Descendants<Button>(setup).Single(b=>b.Text=="Start"));await Frames();
        var screen=One<GameScreen>(main);var session=Session(screen);var owner=session.State.Empires.Single(e=>e.Controller==ControllerKind.Human).Id;var home=session.State.Colonies.Single(c=>c.EmpireId==owner).Id;
        Check(Snapshot(session)==SaveGame.Serialize(content,generated.State!),"Actual Standard Start matches untouched Harl neglect seed");await SaveThroughDialog(screen,"generated-harl-neglect-initial");
        Select(Picker(screen,"GovernorTemplatePicker"),"industrial");Press(Descendants<Button>(One<ColonyPanel>(screen)).Single(b=>b.Text=="Apply template"));await Frames();Toggle(screen,"WorkforceGovernorToggle",true);await Frames();Toggle(screen,"ConstructionGovernorToggle",true);await Frames();await QueueGeneratedProject(screen,"building","fabrication_hall");
        for(int turn=0;turn<22;turn++){await AcknowledgeGenerated(screen,owner);await ChooseResearch(screen,owner);await EndGeneratedTurn(screen,owner);}
        Check(session.State.Turn==22&&session.View(owner).Crisis!.Cases.Single().Stage==CrisisStage.Active,"Generated Harl neglect reaches Active with open source and no cure allocation");
        int activePopulation=session.State.FindColony(home)!.Population,minimum=activePopulation;bool naturallyClosed=false;
        for(int turn=0;turn<12;turn++)
        {
            await AcknowledgeGenerated(screen,owner);await ChooseResearch(screen,owner);await EndGeneratedTurn(screen,owner);
            if(session.State.FindColony(home) is { } colony)minimum=Math.Min(minimum,colony.Population);
            if(session.View(owner).Crisis!.Episode is null){naturallyClosed=true;break;}
        }
        Check(naturallyClosed&&minimum<activePopulation,"Generated open Harl plague causes actual population loss before natural recovery");
        Check(session.State.Turn<=34,"Generated neglect and natural closure remain within 120 crisis turns");
        _results.Add(new{name="generated-harl-neglect provenance",passed=true,source="complete content/core/*.json",seed=42,rivals=1,rules=6,openSource=true,cureShare=0,activePopulation,minimumPopulation=minimum,resolvedTurn=session.State.Turn,liveOutcomeInjections=0});
        SaveEvidence(session,"generated-harl-neglect-recovered");RemoveChild(main);main.QueueFree();await Frames();
    }
    private async Task GeneratedCargoFounding()
    {
        var content=CoreWithApprovedPlague();var generated=GalaxyGenerator.CreateNewGame(content,new("small",42,6,"harl",1,"peldra",true));Check(generated.Succeeded,"Generated Harl cargo starts from untouched Standard seed42");
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();var setup=One<NewGameScreen>(main);setup.Configure("small",42,"harl",1,"peldra",true);Press(Descendants<Button>(setup).Single(b=>b.Text=="Start"));await Frames();
        var screen=One<GameScreen>(main);var session=Session(screen);var owner=session.State.Empires.Single(e=>e.Controller==ControllerKind.Human).Id;var home=session.State.Colonies.Single(c=>c.EmpireId==owner).Id;
        Check(Snapshot(session)==SaveGame.Serialize(content,generated.State!),"Actual Standard Start matches untouched Harl cargo seed");await SaveThroughDialog(screen,"generated-harl-cargo-initial");
        var scout=session.View(owner).Vessels.Single(v=>v.RoleId=="scout").Id;var homeStar=session.View(owner).Colonies.Single().StarId;var chart=session.View(owner).Stars.Single(s=>s.Chart.Id==homeStar).Chart;
        Select(Picker(screen,"GovernorTemplatePicker"),"industrial");Press(Descendants<Button>(One<ColonyPanel>(screen)).Single(b=>b.Text=="Apply template"));await Frames();Toggle(screen,"WorkforceGovernorToggle",true);await Frames();Toggle(screen,"ConstructionGovernorToggle",true);await Frames();await QueueGeneratedProject(screen,"building","fabrication_hall");
        StarId? destination=null;PlanetId? targetPlanet=null;var visited=new HashSet<StarId>{homeStar};
        for(int i=0;i<22;i++)
        {
            var view=session.View(owner);var unit=view.Vessels.Single(v=>v.Id==scout);
            if(unit.Transit is null&&unit.PendingMove is null&&targetPlanet is null)
            {
                var stationed=view.Stars.Single(s=>s.Chart.Id==unit.StationaryStarId);
                if(stationed.Chart.Id!=homeStar&&stationed.Planets is { } planets)
                {
                    var viable=planets.Where(p=>p.OwnerId is null&&p.Colonizable&&p.SupportPerWorker>0&&p.Potential?.NativeHabitatAllowed==true).OrderByDescending(p=>p.SupportPerWorker).ThenBy(p=>p.Id).FirstOrDefault();
                    if(viable is not null){destination=stationed.Chart.Id;targetPlanet=viable.Id;}
                }
                if(targetPlanet is null)
                {
                    var next=view.Stars.Where(s=>!visited.Contains(s.Chart.Id)).OrderBy(s=>Math.Pow(s.Chart.X-chart.X,2)+Math.Pow(s.Chart.Y-chart.Y,2)).ThenBy(s=>s.Chart.Id).FirstOrDefault();
                    if(next is not null){visited.Add(next.Chart.Id);SelectNumber(Picker(One<VesselPanel>(screen),"VesselPicker"),scout.Value);await Frames();One<GalaxyMap>(screen).Select(next.Chart.Id);await Frames();Press(Descendants<Button>(One<VesselPanel>(screen)).Single(b=>b.Text=="Set course"));await Frames();}
                }
            }
            await AcknowledgeGenerated(screen,owner);await ChooseResearch(screen,owner);await EndGeneratedTurn(screen,owner);
        }
        Check(targetPlanet is not null&&destination is not null&&session.View(owner).Crisis!.Cases.Single().Stage==CrisisStage.Active,"Generated scout surveys an eligible remote world before Harl source becomes Active; visited="+visited.Count);
        await AcknowledgeGenerated(screen,owner);SelectNumber(One<OptionButton>(One<ColonyList>(screen)),home.Value);await Frames();await QueueGeneratedProject(screen,"vessel","harl_settler");
        VesselId? founderId=null;bool departedTagged=false,secondaryWarning=false;int completionTurn=-1,departureTurn=-1;
        for(int i=0;i<35;i++)
        {
            var view=session.View(owner);var founder=view.Vessels.FirstOrDefault(v=>v.RoleId=="harl_settler"&&v.CarriedPopulation>0);if(founder is not null)
            {
                founderId=founder.Id;if(completionTurn<0){completionTurn=session.State.Turn;Check(completionTurn>22,"Generated Harl founder completes after source became Active");}
                SelectNumber(Picker(One<VesselPanel>(screen),"VesselPicker"),founder.Id.Value);await Frames();
                if(founder.Transit is null&&founder.PendingMove is null&&founder.StationaryStarId==homeStar)
                {
                    One<GalaxyMap>(screen).Select(destination!.Value);await Frames();Press(Descendants<Button>(One<VesselPanel>(screen)).Single(b=>b.Text=="Set course"));await Frames();departureTurn=session.State.Turn+1;
                }
                else if(founder.Transit is null&&founder.PendingMove is null&&founder.StationaryStarId==destination)
                {
                    var planet=view.Stars.Single(s=>s.Chart.Id==destination).Planets!.Value.Single(p=>p.Id==targetPlanet);One<GalaxyMap>(screen).Select(destination!.Value);await Frames();Press(Descendants<Button>(One<StarPanel>(screen)).Single(b=>b.Text=="Select planet "+planet.Orbit));await Frames();
                    var found=Descendants<Button>(One<VesselPanel>(screen)).Single(b=>b.Text=="Found colony");Check(!found.Disabled,"Generated infected founder has legal surveyed destination");Press(found);await Frames();
                }
            }
            await AcknowledgeGenerated(screen,owner);await ChooseResearch(screen,owner);await EndGeneratedTurn(screen,owner);view=session.View(owner);
            if(founderId is { } id&&view.Vessels.FirstOrDefault(v=>v.Id==id) is { } current&&current.Crisis?.CarrierEpisode is not null)departedTagged=true;
            var child=view.Colonies.FirstOrDefault(c=>c.Id!=home);if(child?.Crisis is {Stage:CrisisStage.Warning,GraceRemaining:2}){secondaryWarning=true;break;}
        }
        Check(departedTagged&&secondaryWarning&&_combinedReportSeen&&session.State.Turn<=57,"Generated founder departs infected open source and creates secondary Warning2 with combined report within120 turns; completion="+completionTurn+", departure="+departureTurn+", final="+session.State.Turn);
        _results.Add(new{name="generated-harl-cargo provenance",passed=true,source="complete content/core/*.json",seed=42,rivals=1,rules=6,scoutVisited=visited.Count,targetStar=destination!.Value.Value,targetPlanet=targetPlanet!.Value.Value,completionTurn,departureTurn,secondaryTurn=session.State.Turn,liveOutcomeInjections=0});
        SaveEvidence(session,"generated-harl-cargo-secondary-warning");RemoveChild(main);main.QueueFree();await Frames();
    }
    private static void SelectNumber(OptionButton picker,int id){int index=Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsInt32()==id);picker.Select(index);picker.EmitSignal(OptionButton.SignalName.ItemSelected,index);}
    private async Task LabelledPortWait()
    {
        var content=Content();var state=Labelled(content);var ship=state.Vessels.Single(v=>v.EmpireId==state.Empires[0].Id);ship.RoleId="harl_settler";ship.CarriedPopulation=1;ship.Crisis!.CarrierEpisode=1;ship.Crisis.InspectionDueTurn=23;
        Check(GameStateValidator.Validate(content,state).Count==0,"Labelled held-carrier fixture validates before UI session: "+string.Join("; ",GameStateValidator.Validate(content,state)));
        var session=new GameSession(content,state);var owner=state.Empires[0].Id;var screen=Screen(session,owner);AddChild(screen);await Frames();Press(Named<Button>(screen,"OpenCrisisOverview"));await Frames();var overview=One<CrisisOverviewDialog>(screen);
        Check(Descendants<Label>(overview).Any(l=>l.Text.Contains("inspection until turn 23",StringComparison.Ordinal)),"Actual overview shows labelled carrier port wait and due turn");
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)}){GetTree().Root.Size=size;overview.PopupCenteredClamped(new(760,600));Descendants<ScrollContainer>(overview).First().ScrollVertical=int.MaxValue;await Frames();await Capture("labelled-port-wait-"+size.X);}
        SaveEvidence(session,"labelled-port-wait");RemoveChild(screen);screen.QueueFree();await Frames();
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
        Check(Named<OptionButton>(overview,"CrisisAuthorityShare").ItemCount==3&&Enumerable.Range(0,3).All(i=>Named<OptionButton>(overview,"CrisisAuthorityShare").GetItemMetadata(i).AsInt32()>0),"Automatic authority offers only valid25/50/100 research shares");
        var authority=One<CrisisAuthorityDialog>(overview);Named<OptionButton>(authority,"CrisisAuthorityPicker").Select(2);Named<OptionButton>(authority,"CrisisAuthorityShare").Select(2);Press(Named<Button>(authority,"CrisisApplyAuthority"));await Frames();
        Check(session.View(owner).Crisis!.Authority==CrisisAuthority.ContainAndResearch,"Actual authority dialog sets automatic containment and research");
        allFilter=Named<OptionButton>(overview,"CrisisFilter");allFilter.Select(0);allFilter.EmitSignal(OptionButton.SignalName.ItemSelected,0);Named<ItemList>(overview,"CrisisCaseList").Select(0);
        Press(Named<Button>(overview,"CrisisQuarantineKeep"));await Frames();Press(Named<Button>(response,"CrisisConfirmQuarantine"));await Frames();
        Check(session.State.FindColony(other.Id)!.Crisis!.PolicyPaused,"Actual manual response overrides the automatic case policy without changing colony governor");await Capture("crisis-policy-override");
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


