using UiNavigation = OpenAntares.Game.Validation.CombinedUiNavigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using static OpenAntares.Game.Validation.ScoutUiControlChecks;
namespace OpenAntares.Game.Validation;
public partial class ScoutGraphicalValidation:Node
{
    private string _output="";private readonly List<object> _checks=new(),_provenance=new();private readonly List<string> _saves=new();private readonly string _run=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");private int _saveIndex;
    public override async void _Ready()
    {
        string? error=null;
        try
        {
            _output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output="))[20..];Directory.CreateDirectory(_output);Check(DisplayServer.GetName()!="headless","Real graphical display");
            string[] types={"GraphicalValidation","ExpansionGraphicalValidation","SpeciesGraphicalValidation","RivalGraphicalValidation","ColonyGraphicalValidation","CrisisGraphicalValidation","ScoutGraphicalValidation","FleetGraphicalValidation","CombatGraphicalValidation"};
            Check(typeof(GameScreen).Assembly.GetTypes().Count(t=>types.Contains(t.Name))==9,"Actual opt-in assembly contains all nine scenario classes");
            Check(File.ReadAllText(Path.Combine(_output,"opt-in-metadata.log")).Contains("ScoutGraphicalValidation",StringComparison.Ordinal),"Normal-assembly negative control explicitly detects the seventh scout type");
            await CurrentSetup();await CustomHabitatSetup();await GeneratedCampaign();await Thresholds();await ScaleAndNoFallback();await ScoutUiControlChecks.Run(this,Check,_output,_run);
        }
        catch(Exception failure){error=failure.ToString();GD.PrintErr(error);}
        finally
        {
            if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{scenario="Scout",runId=_run,passed=error is null,error,method="Actual Godot controls/signals, generated campaigns and separately labelled pre-session supplements; no physical input or human acceptance claim",checks=_checks,provenance=_provenance,retainedQaSavePaths=_saves},new JsonSerializerOptions{WriteIndented=true}));
            GetTree().Quit(error is null?0:1);
        }
    }
    private void Check(bool value,string name){_checks.Add(new{name,passed=value});if(!value)throw new InvalidOperationException(name);}
    private async Task CurrentSetup()
    {
        var content=CompleteContent();var setup=new NewGameScreen(content);AddChild(setup);await Frames(this);
        Check(Rules.CurrentVersion==12&&Named<OptionButton>(setup,"AmebaModePicker").IsVisibleInTree(),"Actual current setup visibly exposes rules12 ameba choice");Check(Named<OptionButton>(setup,"AmebaModePicker").Selected==0,"Actual ameba choice defaults to Standard");
        NewGameSettings? chosen=null;setup.StartRequested+=settings=>chosen=settings;setup.Configure("small",42);Press(Descendants<Button>(setup).Single(b=>b.Text=="Start"));Check(chosen is{RulesVersion:12,SentriesEnabled:true,AmebasEnabled:true,CrisesEnabled:true,RivalCount:1,RivalSpeciesId:"peldra",SpeciesId:"harl"},"Actual default controls submit independent Standard ameba/plague and One Peldra");setup.Configure("small",42,"harl",0,crisesEnabled:false,amebasEnabled:false);Press(Descendants<Button>(setup).Single(b=>b.Text=="Start"));Check(chosen is{AmebasEnabled:false,CrisesEnabled:false,RivalCount:0},"Actual visible Off/Solo choices remain independent");RemoveChild(setup);setup.QueueFree();await Frames(this);
        setup=new NewGameScreen(content with{SpaceAmeba=null});AddChild(setup);await Frames(this);Check(Descendants<Button>(setup).Single(b=>b.Text=="Start").Disabled,"Missing current hazard blocks new campaigns including Off");bool load=false;setup.LoadRequested+=()=>load=true;Press(Descendants<Button>(setup).Single(b=>b.Text=="Load Game"));Check(load,"Missing current hazard retains usable Load Game recovery");RemoveChild(setup);setup.QueueFree();await Frames(this);
    }
    private async Task CustomHabitatSetup()
    {
        var core=CompleteContent();GetTree().Root.Size=new(1920,1080);
        // Literal home/candidate boundaries:2 stars/One has0 candidates,
        //2/Solo and3/One have1, while3/Solo and4/One have2.
        foreach(var entry in new (int Stars,int Rivals,bool Omitted)[]{(2,1,true),(2,0,true),(3,1,true),(3,0,false),(4,1,false)})
        {
            var content=core with{GalaxySizes=core.GalaxySizes.SetItem("small",core.GalaxySizes["small"] with{StarCount=entry.Stars})};
            var setup=new NewGameScreen(content);AddChild(setup);await Frames(this);setup.Configure("small",42,"harl",entry.Rivals,"peldra",crisesEnabled:false,amebasEnabled:true,sentriesEnabled:false);await Frames(this);
            var summary=Named<Label>(setup,"SpeciesSummary");string description=summary.Text[(summary.Text.IndexOf("Space ameba:",StringComparison.Ordinal))..];
            bool explainsOmission=description.Contains("will not place",StringComparison.OrdinalIgnoreCase)&&description.Contains("eligible",StringComparison.OrdinalIgnoreCase);
            Check(explainsOmission==entry.Omitted,$"Custom {entry.Stars}-star/rivals{entry.Rivals} setup explains omitted habitat exactly when necessary");
            NewGameSettings? chosen=null;setup.StartRequested+=settings=>chosen=settings;Press(Descendants<Button>(setup).Single(b=>b.Text=="Start"));Check(chosen is not null,"Small custom setup retains a usable Start");
            var started=GalaxyGenerator.CreateNewGame(content,chosen!);Check(started.Succeeded,started.Error??"Custom campaign starts");Check((started.State!.Scouting!.Habitat is null)==entry.Omitted,"Actual Start settings preserve the explained placement boundary");
            Check(!description.Contains("star:",StringComparison.Ordinal)&&!core.Galaxy.StarNames.Any(name=>description.Contains(name,StringComparison.Ordinal)),"Habitat explanation discloses no candidate identity or location");
            string initial=SaveGame.Serialize(content,started.State);setup.Configure("small",42,"harl",entry.Rivals,"peldra",crisesEnabled:false,amebasEnabled:false,sentriesEnabled:false);await Frames(this);Check(!summary.Text.Contains("will not place",StringComparison.OrdinalIgnoreCase)&&summary.Text.Contains("Space ameba: Off",StringComparison.Ordinal),"Off keeps its own explanation without a Standard omission warning");
            setup.Configure("small",42,"harl",entry.Rivals,"peldra",crisesEnabled:false,amebasEnabled:true,sentriesEnabled:false);await Frames(this);Check(SaveGame.Serialize(content,GalaxyGenerator.CreateNewGame(content,chosen!).State!)==initial,"Repeated setup previews preserve deterministic Start and hazard streams");
            if(entry.Stars==2&&entry.Rivals==1){var scroll=summary.GetParent<ScrollContainer>();scroll.ScrollVertical=int.MaxValue;await Frames(this);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();image.SavePng(Path.Combine(_output,"custom-no-habitat-setup.png"));}
            RemoveChild(setup);setup.QueueFree();await Frames(this);
        }
    }
    private async Task GeneratedCampaign()
    {
        var content=CompleteContent();var opening=GalaxyGenerator.CreateNewGame(content,new("small",42,8,"harl",0,CrisesEnabled:false,AmebasEnabled:true));Check(opening.Succeeded,"Explicit complete-core rules8 generated campaign starts");string canonical=SaveGame.Serialize(content,opening.State!);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames(this);var setup=Descendants<NewGameScreen>(main).Single();
        GameScreen screen=await ScoutUiControlChecks.LoadGeneratedRules8(this,main,new("small",42,8,"harl",0,CrisesEnabled:false,AmebasEnabled:true),_output,"scout",Check);string source="Explicit unchanged rules8 generated seed42, complete embedded core, ordinary Main Load; current10 setup verified separately";
        ConfigurePaths(screen);var session=Session(screen);var owner=session.State.Empires[0].Id;Check(Snapshot(session)==canonical,"Generated initial state is untouched through actual Start/Load");_provenance.Add(new{source,seed=42,size="small",species="harl",rivals=0,crises=false,amebas=true,initialHash=Hash(canonical),liveOutcomeInjections=0});
        await Save(screen,"generated-scout-initial");
        while(session.View(owner).Scouting!.Attention.IsEmpty&&session.State.Turn<160)
        {
            var view=session.View(owner);var vessel=view.Vessels[0];
            if(vessel.Transit is null&&vessel.PendingMove is null)
            {
                var origin=view.Galaxy.Stars.Single(s=>s.Id==vessel.StationaryStarId);int speed=content.Vessels[vessel.RoleId].Speed;
                var next=view.Stars.Where(s=>s.Planets is null&&s.Chart.Id!=origin.Id).OrderBy(s=>TravelCalculator.Duration(origin.X,origin.Y,s.Chart.X,s.Chart.Y,speed)).ThenBy(s=>s.Chart.Id).FirstOrDefault();
                Check(next is not null,"Own-view unknown exploration has a finite next charted destination");SelectDestination(screen,next!.Chart.Id);await Frames(this);Press(Named<Button>(screen,"SetVesselCourse"));await Frames(this);Check(session.State.Vessels[0].PendingMove==next.Chart.Id,"Actual course control queues own-view unknown destination");
            }
            await End(screen);
        }
        Check(session.State.Turn<=160&&!session.View(owner).Scouting!.Attention.IsEmpty,"Untouched generated campaign discovers ameba through own-view exploration within160 turns");
        var warning=session.View(owner).Scouting!;Check(warning.Vessels[0].Stage==AmebaStage.Warning&&warning.Attention[0].Percent==100,"Generated discovery publishes full safe Warning");Check(warning.Habitats.Single().ObservedTurn==session.State.Turn,"Own habitat observation records the actual latest presence turn");
        Named<AcceptDialog>(screen,"ScoutOverview").PopupCenteredClamped(new(740,560));await Frames(this);await Capture(this,Named<AcceptDialog>(screen,"ScoutOverview"),Path.Combine(_output,"generated-arrival-warning.png"));
        string saved=await Save(screen,"generated-scout-warning");string warningBytes=Snapshot(session);screen=await Load(main,saved);ConfigurePaths(screen);session=Session(screen);Check(Snapshot(session)==warningBytes,"Actual UI warning Save/Load preserves exact state and unacknowledged consent");Check(Named<AcceptDialog>(screen,"ScoutOverview").Visible,"Unresolved warning reopens by default after actual Load");
        await CompareFour(screen,"warning");
        screen=await Load(main,saved);ConfigurePaths(screen);session=Session(screen);var travelView=session.View(owner);var unit=travelView.Vessels[0];var voyageOrigin=travelView.Galaxy.Stars.Single(star=>star.Id==unit.StationaryStarId);int voyageSpeed=content.Vessels[unit.RoleId].Speed;
        var distant=travelView.Stars.Where(star=>star.Chart.Id!=voyageOrigin.Id&&!travelView.Scouting!.Habitats.Any(h=>h.Star==star.Chart.Id)).Select(star=>(Star:star,Turns:TravelCalculator.Duration(voyageOrigin.X,voyageOrigin.Y,star.Chart.X,star.Chart.Y,voyageSpeed))).OrderByDescending(pair=>pair.Turns).ThenBy(pair=>pair.Star.Chart.Id).First();Check(distant.Turns>1,"Own chart supports an ordinary committed transit boundary");SelectDestination(screen,distant.Star.Chart.Id);await Frames(this);Press(Named<Button>(screen,"SetVesselCourse"));await Frames(this);await Acknowledge(screen);await End(screen);Check(session.State.Vessels[0].Transit is not null,"Actual safe departure starts the selected multi-turn route");string transitSaved=await Save(screen,"generated-scout-committed-transit");screen=await Load(main,transitSaved);ConfigurePaths(screen);await CompareFour(screen,"transit");
        screen=await Load(main,saved);ConfigurePaths(screen);session=Session(screen);var overview=Named<AcceptDialog>(screen,"ScoutOverview");Press(Named<Button>(overview,"ScoutStudy"));await Frames(this);Press(Named<ConfirmationDialog>(overview,"ScoutConfirmation").GetOkButton());await Frames(this);await Acknowledge(screen);await End(screen);
        Check(session.State.Empires[0].Scouting!.RiskRollCount==1,"Generated Study consumes its first actual owner draw exactly once");Check(session.State.Vessels[0].AmebaEncounter?.Stage==AmebaStage.Exposed,"Generated seed42 actual Study draw55 survives without reroll");_provenance.Add(new{source="Actual generated campaign Study",turn=session.State.Turn,riskRolls=session.State.Empires[0].Scouting!.RiskRollCount,survived=true,researchAward=600});
        string exposed=await Save(screen,"generated-scout-exposed");screen=await Load(main,exposed);ConfigurePaths(screen);session=Session(screen);await CompareFour(screen,"exposed");
        _provenance.Add(new{source="Generated actual UI save/future comparison",warningHash=Hash(warningBytes),fourFuturesPerBoundary=4,boundaries=new[]{"warning","exposed","transit"}});RemoveChild(main);main.QueueFree();await Frames(this);
    }
    private async Task CompareFour(GameScreen screen,string boundary)
    {
        var session=Session(screen);var loaded=SaveGame.Deserialize(Snapshot(session));Check(loaded.Succeeded,boundary+": independent canonical reload succeeds");var reference=new GameSession(loaded.Game!.Content,loaded.Game.State);var owner=reference.State.Empires[0].Id;
        for(int i=0;i<4;i++)
        {
            var attention=ScoutQueries.Project(reference.Content,reference.State,owner).Attention;if(!attention.IsEmpty)Check(reference.Execute(new AcknowledgeAmebaCommand(owner,reference.State.Turn,attention)).Accepted,boundary+": reference current acknowledgement accepted");Check(reference.Execute(new SetReadyCommand(owner,reference.State.Turn,true)).Accepted,boundary+": reference boundary resolves");await Acknowledge(screen);await End(screen);Check(Snapshot(session)==Snapshot(reference),boundary+": actual UI future "+(i+1)+" equals uninterrupted canonical continuation");
            if(i==0&&session.State.Vessels.FirstOrDefault()?.Transit is not null){string transit=await Save(screen,"generated-scout-"+boundary+"-transit");Check(SaveGame.ReadFile(SaveFiles.PathFor(transit)).Succeeded,boundary+": committed transit actual UI save validates");}
        }
    }
    private async Task Thresholds()
    {
        foreach(var row in new[]{("study",74,true,300),("study",75,false,300),("study",49,true,200),("study",50,false,200),("escape",94,true,300),("escape",95,false,300)})
        {
            var g=LabelledWarning(speed:row.Item4);if(row.Item1=="escape")g=StudyBeforeSession(Seek(g,74));g=Seek(g,row.Item2);var session=new GameSession(g.Content,g.State);var owner=g.State.Empires[0].Id;var screen=Screen(session,owner,_output);AddChild(screen);await Frames(this);long rolls=g.State.Empires[0].Scouting!.RiskRollCount;
            var overview=Named<AcceptDialog>(screen,"ScoutOverview");Press(Named<Button>(overview,row.Item1=="study"?"ScoutStudy":"ScoutWithdraw"));await Frames(this);var confirmation=Named<ConfirmationDialog>(overview,"ScoutConfirmation");Check(confirmation.Visible,$"Labelled {row}: actual risk confirmation opens");Press(confirmation.GetOkButton());await Frames(this);await Acknowledge(screen);await End(screen);Check(session.State.Vessels.Count==(row.Item3?1:0),$"Labelled {row}: actual survival outcome matches literal threshold");Check(session.State.Empires[0].Scouting!.RiskRollCount==rolls+1,$"Labelled {row}: one owner draw, no hidden reroll");
            _provenance.Add(new{source="Labelled pre-session threshold fixture; not generated luck",action=row.Item1,nextDraw=row.Item2,expectedSurvived=row.Item3,speed=row.Item4,priorRolls=rolls,liveOutcomeInjections=0});
            var report=Descendants<TurnReportDialog>(screen).Single();overview.Hide();report.PopupCenteredClamped(new(740,540));await Frames(this);await Capture(this,report,Path.Combine(_output,$"labelled-{row.Item1}-{row.Item2}-{row.Item4}.png"));RemoveChild(screen);screen.QueueFree();await Frames(this);
        }
    }
    private async Task ScaleAndNoFallback()
    {
        var g=LabelledWarning(count:30);var s=g.State;var peer=new EmpireState{Id=new(s.AllocateId()),Controller=ControllerKind.Human,SpeciesId="harl",Ready=true,Development=new(),Crisis=new(){Random=CrisisInitializer.CreateRandom(42,new(s.NextEntityId-1))},Scouting=new(){RiskRandom=ScoutInitializer.RiskRandom(42,new(s.NextEntityId-1))}};s.Empires.Add(peer);Check(GameStateValidator.Validate(g.Content,s).Count==0,"Labelled30-vessel/two-human fixture validates before UI session");
        var session=new GameSession(g.Content,s);var screen=Screen(session,s.Empires[0].Id,_output);AddChild(screen);await Frames(this);var overview=Named<AcceptDialog>(screen,"ScoutOverview");Check(Named<ItemList>(overview,"ScoutVesselList").ItemCount==30,"One grouped overview contains all30 exact live responses");
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)}){GetTree().Root.Size=size;overview.PopupCenteredClamped(new(740,560));await Frames(this);var list=Named<ItemList>(overview,"ScoutVesselList");list.Select(29);list.EmitSignal(ItemList.SignalName.ItemSelected,29L);await Frames(this);Check(session.View(s.Empires[0].Id).Scouting!.Attention.Length==30,"Selecting last row preserves all30 tokens at "+size.X);var refreshed=Named<ItemList>(overview,"ScoutVesselList");Check(refreshed.GetSelectedItems().Single()==29&&refreshed.GetVScrollBar().Value>0,"Selected last encounter remains scrolled into view after panel refresh at "+size.X);await Capture(this,overview,Path.Combine(_output,"labelled-thirty-"+size.X+".png"));}
        await Acknowledge(screen);await End(screen);Check(session.State.Vessels.All(v=>v.AmebaEncounter is null)&&session.State.Empires[0].Scouting!.RiskRollCount==0,"Grouped human response safely departs all30 without a risk draw");RemoveChild(screen);screen.QueueFree();await Frames(this);
        g=LabelledWarning(crisesEnabled:true);s=g.State;var colony=s.Colonies[0];s.Empires[0].Crisis!.LostColonyLocations.Add(CrisisQueries.Location(s,colony));s.Colonies.Clear();s.Empires[0].Surveys.RemoveAll(o=>o.StarId!=s.Scouting!.Habitat!.Star);Check(GameStateValidator.Validate(g.Content,s).Count==0,"Labelled no-colony/no-known-retreat fixture validates before UI session");session=new(g.Content,s);screen=Screen(session,s.Empires[0].Id,_output);AddChild(screen);await Frames(this);Descendants<GalaxyMap>(screen).Single().Select(s.Vessels[0].StationaryStarId!.Value);await Frames(this);await Acknowledge(screen);string before=Snapshot(session);Press(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));await Frames(this);Check(Snapshot(session)==before,"Acknowledged no-fallback warning still blocks actual End Turn without a response");var target=session.View(s.Empires[0].Id).Stars.First(star=>star.Chart.Id!=s.Vessels[0].StationaryStarId).Chart.Id;SelectDestination(screen,target);await Frames(this);Press(Named<Button>(screen,"SetVesselCourse"));await Frames(this);await Acknowledge(screen);await End(screen);Check(session.State.Vessels[0].AmebaEncounter is null,"Actual unknown manual route recovers from no known fallback");RemoveChild(screen);screen.QueueFree();await Frames(this);
        g=LabelledWarning(cargo:true);var owner=g.State.Empires[0].Id;var risk=CommandProcessor.Execute(g.Content,g.State,new SetAmebaRiskOrderCommand(owner,10,g.State.Vessels[0].Id,1,1,AmebaRiskAction.Remain));g=g with{State=risk.State};var preSession=new GameSession(g.Content,g.State);for(int i=0;i<13;i++){var attention=ScoutQueries.Project(g.Content,preSession.State,owner).Attention;if(!attention.IsEmpty)Check(preSession.Execute(new AcknowledgeAmebaCommand(owner,preSession.State.Turn,attention)).Accepted,"Labelled history fixture acknowledges its own pre-session encounter");Check(preSession.Execute(new SetReadyCommand(owner,preSession.State.Turn,true)).Accepted,"Labelled history fixture advances by ordinary pre-session simulation");}Check(preSession.State.Empires[0].Scouting!.RetiredVessels.Count==0,"Expired history retires obsolete vessel facts without resurrecting cargo");screen=Screen(preSession,owner,_output);AddChild(screen);await Frames(this);Check(!Descendants<Label>(screen).Any(l=>l.Name=="ScoutCriticalBadge"&&l.Visible),"Expired history leaves no phantom loss Emergency");RemoveChild(screen);screen.QueueFree();await Frames(this);
    }
    private static LoadedGame Seek(LoadedGame source,int draw)
    {
        var g=source with{State=source.State.Clone()};var e=g.State.Empires[0].Scouting!;for(int n=0;n<100000;n++){if(e.RiskRandom!.Clone().NextInt(100)==draw)return g;e.RiskRandom.NextInt(100);e.RiskRollCount++;}throw new InvalidOperationException("No bounded labelled threshold found.");
    }
    private static LoadedGame StudyBeforeSession(LoadedGame g)
    {
        var session=new GameSession(g.Content,g.State);var owner=g.State.Empires[0].Id;var v=g.State.Vessels[0];var a=v.AmebaEncounter!;
        if(!session.Execute(new SetAmebaRiskOrderCommand(owner,g.State.Turn,v.Id,a.Number,a.Revision,AmebaRiskAction.Study)).Accepted||!session.Execute(new AcknowledgeAmebaCommand(owner,g.State.Turn,ScoutQueries.Project(g.Content,session.State,owner).Attention)).Accepted||!session.Execute(new SetReadyCommand(owner,g.State.Turn,true)).Accepted)throw new InvalidOperationException("Labelled pre-session Study failed.");return new(session.Content,session.State);
    }
    private async Task Acknowledge(GameScreen screen){var session=Session(screen);if(session.View(session.State.Empires[0].Id).Scouting!.Attention.IsEmpty)return;var overview=Named<AcceptDialog>(screen,"ScoutOverview");if(!overview.Visible){Press(Named<Button>(screen,"OpenScoutOverview"));await Frames(this);}Press(Named<Button>(overview,"ScoutAcknowledge"));await Frames(this);}
    private async Task End(GameScreen screen)
    {
        var session=Session(screen);int turn=session.State.Turn;Press(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));await Frames(this);Check(session.State.Turn==turn+1,"Actual End Turn advances exactly one boundary");foreach(var report in Descendants<TurnReportDialog>(screen))report.Hide();await Frames(this);
    }
    private async Task<string> Save(GameScreen screen,string label)
    {
        string name=$"qa-{_run}-{++_saveIndex}-{label}",path=SaveFiles.PathFor(name);Check(!File.Exists(path),"Unique QA save name");Named<AcceptDialog>(screen,"ScoutOverview").Hide();foreach(var report in Descendants<TurnReportDialog>(screen))report.Hide();await Frames(this);UiNavigation.GameMenu(screen,0);await Frames(this);var dialog=Descendants<SaveGameDialog>(screen).Single();Descendants<LineEdit>(dialog).Single().Text=name;Press(dialog.GetOkButton());await Frames(this);Check(File.Exists(path),label+": actual UI Save writes flushed campaign");_saves.Add(path);return name;
    }
    private async Task<GameScreen> Load(Control main,string name)
    {
        var current=Descendants<GameScreen>(main).Single();Named<AcceptDialog>(current,"ScoutOverview").Hide();foreach(var report in Descendants<TurnReportDialog>(current))report.Hide();await Frames(this);UiNavigation.GameMenu(current,1);await Frames(this);var dialog=Descendants<LoadGameDialog>(main).Single();var list=Descendants<ItemList>(dialog).Single();int index=Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemText(i).StartsWith(name+"    ",StringComparison.Ordinal));list.Select(index);list.EmitSignal(ItemList.SignalName.ItemSelected,index);Press(dialog.GetOkButton());await Frames(this);return Descendants<GameScreen>(main).Single();
    }
    private void ConfigurePaths(GameScreen screen){screen.ScoutPreferencesPath=Path.Combine(_output,"generated-scout-prefs.json");screen.NotificationPreferencesPath=Path.Combine(_output,"generated-colony-prefs.json");screen.CrisisPreferencesPath=Path.Combine(_output,"generated-crisis-prefs.json");}
    private static GameSession Session(GameScreen screen)=>(GameSession)typeof(GameScreen).GetField("_session",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(screen)!;
    private static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}

