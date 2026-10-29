using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using static OpenAntares.Game.Validation.ScoutUiControlChecks;
namespace OpenAntares.Game.Validation;
public partial class CombatGraphicalValidation:Node
{
    private string? _campaignLabel;private string _campaignFolder="";private readonly List<object> _commandTrace=new();private readonly List<(string Label,GameSession Session,int Through)> _replayBranches=new();private readonly List<string> _verifiedStages=new();private int _generatedCases;private long _earnedProduction;private ColonyId _campaignHome;
    private string _output="";private readonly List<object> _checks=new(),_provenance=new();
    private void Check(bool condition,string name){_checks.Add(new{name,passed=condition});if(!condition)throw new InvalidOperationException(name);}
    public override async void _Ready()
    {
        string? error=null;
        try
        {
            _output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output="))[20..];Directory.CreateDirectory(_output);Check(DisplayServer.GetName()!="headless","Actual graphical Godot display");
            await CurrentSetup();await LegacyAndInvalidSetup();
            foreach(string species in CompleteContent().Species.Keys.OrderBy(id=>id,StringComparer.Ordinal))foreach(var size in CompleteContent().GalaxySizes.Values.OrderBy(d=>d.StarCount))await GeneratedCampaign(species,size.Id);
            Check(_generatedCases==12,"All4 species ×3 galaxy sizes completed genuine paid combat campaigns");
            foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)}){await Primary(size);await CivilianResponse(size);await BulkRepair(size);await Privacy(size);}
        }
        catch(Exception failure){error=failure.ToString();GD.PrintErr(error);}
        finally{if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{scenario="Combat",passed=error is null,error,method="Actual graphical Godot controls/signals; no physical input or player-comprehension claim",generatedCampaigns=_generatedCases,checks=_checks,provenance=_provenance},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(error is null?0:1);}
    }
    private LoadedGame Board(bool cargo=false,int hull=100,bool secondHuman=false)
    {
        var c=CompleteContent();var made=GalaxyGenerator.CreateNewGame(c,new("small",42,9,"harl",secondHuman?1:0,secondHuman?"peldra":null,CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:true));Check(made.Succeeded,"Explicit rules9 fixture generation");var s=made.State!;var e=s.Empires[0];var p=s.Combat!.Pockets[0];var ship=s.Vessels[0];ship.RoleId="escort";ship.StationaryStarId=p.Star;ship.Fleet=new(1);CombatInitializer.InitializeVessel(c,9,ship);ship.Combat!.Hull=hull;
        var members=new List<VesselId>{ship.Id};
        if(cargo){var settler=new VesselState{Id=new(s.AllocateId()),EmpireId=e.Id,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=p.Star,Fleet=new(1),Crisis=new()};CombatInitializer.InitializeVessel(c,9,settler);s.Vessels.Add(settler);members.Add(settler.Id);}
        e.Fleets!.Fleets.Add(new(){Id=new(1),Name="Labelled combat board",Members=members,Flagship=ship.Id});e.Fleets.NextFleetId=2;
        if(secondHuman){var other=s.Empires[1];other.Controller=ControllerKind.Human;other.AiPolicyId=null;var observer=s.Vessels.Single(v=>v.EmpireId==other.Id);observer.RoleId="escort";observer.StationaryStarId=p.Star;CombatInitializer.InitializeVessel(c,9,observer);}
        var before=s.Clone();ObservationUpdater.Refresh(s,0);ContactUpdater.Refresh(before,s);CombatObservations.Refresh(c,s,0);Check(GameStateValidator.Validate(c,s).Count==0,"Labelled pre-session hull/co-location/fleet fixture validates");_provenance.Add(new{source="Generated explicit rules9 seed42, labelled pre-session escort/cargo/hull/co-location/group supplement; no earned production or discovery claim",cargo,hull,liveOutcomeInjections=0});return new(c,s);
    }
    private void Paths(GameScreen screen,string label)
    {
        screen.CombatPreferencesPath=Path.Combine(_output,label+"-combat-prefs.json");screen.FleetPreferencesPath=Path.Combine(_output,label+"-fleet-prefs.json");screen.ScoutPreferencesPath=Path.Combine(_output,label+"-scout-prefs.json");screen.CrisisPreferencesPath=Path.Combine(_output,label+"-crisis-prefs.json");screen.NotificationPreferencesPath=Path.Combine(_output,label+"-colony-prefs.json");
    }
    private static GameSession Live(GameScreen screen)=>(GameSession)typeof(GameScreen).GetField("_session",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(screen)!;
    private async Task Click(Control control)
    {
        Check(control.IsVisibleInTree()&&control.GetWindow().Visible,"Reachable "+control.Name);for(Node? p=control.GetParent();p is not null;p=p.GetParent())if(p is ScrollContainer scroll)scroll.EnsureControlVisible(control);control.GrabFocus();await Frames(this);if(control is Button b)Press(b);else throw new InvalidOperationException("Expected button");await Frames(this);
    }
    private Task Click(Node root,string name)=>Click(Named<Button>(root,name));
    private async Task Pick(OptionButton picker,int i){picker.Select(i);picker.EmitSignal(OptionButton.SignalName.ItemSelected,(long)i);await Frames(this);}
    private Task PickId(OptionButton picker,int id)=>Pick(picker,Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsInt32()==id));
    private async Task Select(ItemList list,int index){list.DeselectAll();list.Select(index);if(list.SelectMode==ItemList.SelectModeEnum.Multi)list.EmitSignal(ItemList.SignalName.MultiSelected,(long)index,true);else list.EmitSignal(ItemList.SignalName.ItemSelected,(long)index);await Frames(this);}
    private async Task Tab(Node root,int index){Named<TabContainer>(root,"FleetTabs").CurrentTab=index;await Frames(this);}
    private async Task Close(AcceptDialog dialog){if(dialog.Visible)await Click(dialog.GetOkButton());}
    private async Task Shot(Window window,string name)
    {
        Check(window.Size.X<=GetTree().Root.Size.X&&window.Size.Y<=GetTree().Root.Size.Y,"Native popup clamped: "+name);File.WriteAllText(Path.Combine(_output,name+"-text.txt"),string.Join("\n",Descendants<Label>(window).Where(l=>l.IsVisibleInTree()).Select(l=>l.Text)));_checks.Add(new{name="native popup geometry "+name,passed=true,width=window.Size.X,height=window.Size.Y,forceNative=window.ForceNative});
        await Capture(this,window,Path.Combine(_output,name+"-embedded.png"));Check(window.Size.X<=GetTree().Root.Size.X&&window.Size.Y<=GetTree().Root.Size.Y,"Embedded visual popup clamped: "+name);
    }
    private async Task<(Control Main,GameScreen Screen)> Mount(LoadedGame fixture,string label)
    {
        string name="qa-combat-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+label;string file=SaveFiles.PathFor(name);Check(!File.Exists(file),"Unique retained combat QA fixture");SaveGame.WriteFile(file,fixture.Content,fixture.State);_provenance.Add(new{source="Labelled fixture mounted through ordinary Main Load control",file});
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames(this);var setup=Descendants<NewGameScreen>(main).Single();await Click(Descendants<Button>(setup).Single(b=>b.Text.Contains("Load",StringComparison.Ordinal)));await Load(main,name);var screen=Descendants<GameScreen>(main).Single();Paths(screen,label);Check(Snapshot(Live(screen))==SaveGame.Serialize(fixture.Content,fixture.State),"Ordinary Main Load restores exact explicit9 board without acting");return(main,screen);
    }
    private async Task Load(Control main,string name)
    {
        var dialog=Descendants<LoadGameDialog>(main).Single();var list=Descendants<ItemList>(dialog).Single();await Select(list,Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemText(i).StartsWith(name+"    ",StringComparison.Ordinal)));await Click(dialog.GetOkButton());await Frames(this);
    }
    private async Task<GameScreen> SaveReload(Control main,GameScreen screen,string label)
    {
        string before=Snapshot(Live(screen)),name="qa-combat-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+label;await Click(Descendants<Button>(screen).Single(b=>b.Text=="Save"));var dialog=Descendants<SaveGameDialog>(screen).Single();Descendants<LineEdit>(dialog).Single().Text=name;await Click(dialog.GetOkButton());Check(File.Exists(SaveFiles.PathFor(name)),"Actual combat Save dialog wrote unique retained QA save");await Click(Descendants<Button>(screen).Single(b=>b.Text=="Load"));await Load(main,name);screen=Descendants<GameScreen>(main).Single();Paths(screen,label);Check(before==Snapshot(Live(screen)),"Actual Main Save/Load preserves pending combat, damage, consent and all streams");_provenance.Add(new{source="Actual ordinary Main Save/Load controls",file=SaveFiles.PathFor(name)});return screen;
    }
    private async Task<AcceptDialog> Combat(GameScreen screen)
    {
        await Click(screen,"OpenFleetCommand");var fleet=Descendants<FleetCommandDialog>(screen).Single();await Tab(fleet,1);await Select(Named<ItemList>(fleet,"FleetGroupList"),0);await Click(fleet,"FleetCombatOrders");return Named<AcceptDialog>(screen,"CombatOrdersDialog");
    }
    private async Task End(GameScreen screen)
    {
        var session=Live(screen);int before=session.State.Turn;var prior=session.State;await Click(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));RecordUi(screen,prior,new SetReadyCommand(prior.Empires.First(e=>e.Controller==ControllerKind.Human).Id,before,true),"End Turn");Check(session.State.Turn==before+1,"Actual End Turn resolves one automatic boundary");Check(Descendants<TurnReportDialog>(screen).Single().Visible,"Actual combat turn report is visible");
    }
    private async Task Primary(Vector2I size)
    {
        GetTree().Root.Size=size;var fixture=Board();var (main,screen)=await Mount(fixture,"primary-"+size.X);var session=Live(screen);string initial=Snapshot(session);await Click(screen,"OpenFleetCommand");var fleet=Descendants<FleetCommandDialog>(screen).Single();var filter=Named<OptionButton>(fleet,"FleetShipFilter");
        Check(Enumerable.Range(0,filter.ItemCount).Any(i=>filter.GetItemText(i)=="Warships"),"Warship filter missing from actual Fleet Command controls");await Pick(filter,7);Check(Named<ItemList>(fleet,"FleetShipList").ItemCount==1,"Warship capability filter selects the owned escort");Check(Snapshot(session)==initial,"Opening and filtering combat makes no command or draw");await Select(Named<ItemList>(fleet,"FleetShipList"),0);await Click(fleet,"FleetLocateShip");Check(Descendants<Label>(screen).Any(l=>l.Text.Contains("Founding blocked; ordinary passage",StringComparison.Ordinal)),"Actual star inspector shows observed founding restriction and safe passage");await Close(fleet);
        var combat=await Combat(screen);Check(Named<OptionButton>(combat,"CombatDoctrine").Selected==0&&Named<OptionButton>(combat,"CombatStragglers").Selected==0&&Named<OptionButton>(combat,"CombatOnArrival").Selected==0,"Visible Cautious / Keep together / Repair defaults");Check(Named<OptionButton>(combat,"CombatReturnColony").GetSelectedMetadata().AsInt32()==0&&!Named<CheckBox>(combat,"CombatCargoConsent").ButtonPressed,"Return destination and civilian risk are explicit choices");
        await Click(combat,"CombatPreview");Check(Named<Button>(combat,"CombatApply").Disabled&&Snapshot(session)==initial,"Missing explicit return colony rejects without mutation");await PickId(Named<OptionButton>(combat,"CombatReturnColony"),session.State.Colonies[0].Id.Value);await Click(combat,"CombatPreview");Check(!Named<Button>(combat,"CombatApply").Disabled,"Exact legal attack preview is actionable");string text=Named<Label>(combat,"CombatPreviewDetails").Text;Check(text.Contains("On arrival")&&text.Contains("damage")&&text.Contains("cohesion")&&text.Contains("repair interval"),"Exact shot, band, cohesion, route and repair authority text");await Shot(combat,"attack-preview-"+size.X);
        var apply=Named<Button>(combat,"CombatApply");var scroll=Descendants<ScrollContainer>(combat).First();scroll.EnsureControlVisible(apply);apply.GrabFocus();await Frames(this);Check(apply.GetGlobalRect().Intersects(scroll.GetGlobalRect())&&apply.HasFocus(),"Focused apply reachable in scroll at "+size.X);await Click(apply);Check(session.State.Turn==0&&session.State.Combat!.Encounters.Single().Phase==CombatPhase.Pending,"Applying attack only reserves, never fires");
        screen=await SaveReload(main,screen,"pending-"+size.X);session=Live(screen);await End(screen);Check(session.State.Vessels[0].Combat!.Hull==72&&session.State.Combat!.Encounters[0].Round==1,"Actual first simultaneous exchange exact hull and round");var report=Descendants<TurnReportDialog>(screen).Single();await Click(report,"CombatExpandDetails");Check(Descendants<Label>(report).Any(l=>l.Text.Contains("22 damage",StringComparison.Ordinal)),"Actual expandable volley calculation shown");await Shot(report,"first-round-report-"+size.X);await Close(report);
        await Click(screen,"OpenFleetCommand");fleet=Descendants<FleetCommandDialog>(screen).Single();await Pick(Named<OptionButton>(fleet,"FleetShipFilter"),8);Check(Named<ItemList>(fleet,"FleetShipList").ItemCount==1&&Named<ItemList>(fleet,"FleetShipList").GetItemText(0).Contains("In combat"),"Actual In combat status filter and numeric hull row");await Tab(fleet,3);await Click(fleet,"CombatExpandDetails");Check(Descendants<Label>(fleet).Any(l=>l.Text.Contains("22 damage",StringComparison.Ordinal)),"Active round calculations persist in Fleet Command after turn report closes");await Close(fleet);
        combat=await Combat(screen);Check(Named<Button>(combat,"CombatWithdraw").Visible&&!Named<Button>(combat,"CombatWithdraw").Disabled,"Withdraw is reachable for the committed encounter");await Click(combat,"CombatWithdraw");Check(Named<Label>(combat,"CombatPreviewDetails").Text.Contains("Return cohort"),"Withdrawal previews exact frozen cohorts");await Click(combat,"CombatApply");Check(session.State.Combat!.Encounters[0].Phase==CombatPhase.WithdrawalQueued,"Actual withdrawal is queued until next boundary");await Click(screen,"OpenFleetCommand");fleet=Descendants<FleetCommandDialog>(screen).Single();await Tab(fleet,0);await Pick(Named<OptionButton>(fleet,"FleetShipFilter"),9);Check(Named<ItemList>(fleet,"FleetShipList").ItemCount==1&&Named<ItemList>(fleet,"FleetShipList").GetItemText(0).Contains("Withdrawing"),"Queued withdrawal status and filter are visible");await Close(fleet);screen=await SaveReload(main,screen,"withdrawal-"+size.X);session=Live(screen);await End(screen);Check(session.State.Combat!.Encounters.Count==0&&session.State.Vessels[0].Combat!.Hull==72,"Clean departure has no interception damage and closes encounter");await Close(Descendants<TurnReportDialog>(screen).Single());RemoveChild(main);main.QueueFree();await Frames(this);
    }
    private async Task CivilianResponse(Vector2I size)
    {
        GetTree().Root.Size=size;string label="civilian-"+size.X;var (main,screen)=await Mount(Board(true,1),label);var session=Live(screen);var combat=await Combat(screen);await PickId(Named<OptionButton>(combat,"CombatReturnColony"),session.State.Colonies[0].Id.Value);string before=Snapshot(session);await Click(combat,"CombatPreview");Check(Named<Button>(combat,"CombatApply").Disabled&&Snapshot(session)==before,"Civilian convoy requires explicit risk opt-in without mutation");
        var consent=Named<CheckBox>(combat,"CombatCargoConsent");consent.SetPressedNoSignal(true);consent.EmitSignal(BaseButton.SignalName.Toggled,true);await Frames(this);await Click(combat,"CombatPreview");Check(!Named<Button>(combat,"CombatApply").Disabled&&Named<Label>(combat,"CombatPreviewDetails").Text.Contains("population at risk: 1"),"Explicit convoy consent shows exact embarked population");await Click(combat,"CombatApply");await End(screen);Check(session.State.Combat!.Encounters[0].Phase==CombatPhase.WithdrawalQueued&&session.State.Empires[0].Combat!.Responses.Count==1,"Actual last-warship destruction leaves mandatory withdrawal and specific response");await Close(Descendants<TurnReportDialog>(screen).Single());
        await Click(screen,"OpenFleetCommand");var fleet=Descendants<FleetCommandDialog>(screen).Single();await Tab(fleet,3);await Pick(Named<OptionButton>(fleet,"CombatRoutineDisplay"),1);before=Snapshot(session);await Click(fleet,"CombatApplyDisplay");Check(Snapshot(session)==before,"Local combat Off preference never changes authority");Check(Named<Label>(screen,"CombatAttentionBadge").Visible&&Named<Label>(screen,"CombatAttentionBadge").Text.Contains("Emergency"),"Critical combat badge survives Routine Off");await Shot(fleet,"civilian-emergency-off-"+size.X);await Close(fleet);
        before=Snapshot(session);await Click(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));Check(before==Snapshot(session),"Actual unchanged Ready rejects the outstanding specific combat response");screen=await SaveReload(main,screen,label);session=Live(screen);Check(session.State.Empires[0].Combat!.Responses.Count==1&&Named<Label>(screen,"CombatAttentionBadge").Visible,"Saved required response and persistent badge survive actual Main reload");
        await Click(screen,"OpenCombatResponses");combat=Named<AcceptDialog>(screen,"CombatOrdersDialog");Check(!Named<Button>(combat,"CombatAcceptForce").Disabled,"Specific revised-force response reachable after reload");await Click(combat,"CombatAcceptForce");await Shot(combat,"specific-response-"+size.X);await Click(combat,"CombatApply");Check(session.State.Empires[0].Combat!.Responses.Count==0&&session.State.Combat!.Encounters[0].Phase==CombatPhase.WithdrawalQueued,"Explicit force response retains mandatory withdrawal, never unarmed offense");await End(screen);Check(session.State.Combat!.Encounters.Count==0&&session.State.Vessels.Count==1&&session.State.Vessels[0].CarriedPopulation==1,"Actual civilian return retains surviving cargo after one interception");await Close(Descendants<TurnReportDialog>(screen).Single());RemoveChild(main);main.QueueFree();await Frames(this);
    }
    private async Task BulkRepair(Vector2I size)
    {
        GetTree().Root.Size=size;var fixture=Board();var s=fixture.State;var group=s.Empires[0].Fleets!.Fleets[0];var first=s.Vessels[0];first.StationaryStarId=s.FindPlanet(s.Colonies[0].PlanetId)!.StarId;first.Combat!.Hull=46;first.Combat.Cohesion=30;
        for(int i=1;i<30;i++){var ship=first.Clone();ship.Id=new(s.AllocateId());if(i==29)ship.Combat!.Hull=24;s.Vessels.Add(ship);group.Members.Add(ship.Id);}ObservationUpdater.Refresh(s,s.Turn);Check(GameStateValidator.Validate(fixture.Content,s).Count==0,"Labelled30-ship owned-port damage board validates");_provenance.Add(new{source="Labelled pre-session30 paid-status hulls at own port, HP46 (one HP24)/cohesion30; no earned production/damage claim",liveOutcomeInjections=0});
        var (main,screen)=await Mount(fixture,"bulk-"+size.X);var session=Live(screen);await Click(screen,"OpenFleetCommand");var fleet=Descendants<FleetCommandDialog>(screen).Single();await Pick(Named<OptionButton>(fleet,"FleetShipFilter"),10);Check(Named<ItemList>(fleet,"FleetShipList").ItemCount==30,"Thirty damaged owned hulls are selectable without map visits");Check(Named<ItemList>(fleet,"FleetShipList").GetItemText(0).Contains("Damaged"),"Damage status includes a readable numeric hull row");await Pick(Named<OptionButton>(fleet,"FleetShipFilter"),11);Check(Named<ItemList>(fleet,"FleetShipList").ItemCount==1&&Named<ItemList>(fleet,"FleetShipList").GetItemText(0).Contains("Crippled"),"Crippled status and filter use the lowest hull band");await Pick(Named<OptionButton>(fleet,"FleetShipFilter"),10);await Click(fleet,"FleetSelectAll");await Click(fleet,"FleetRepair");var combat=Named<AcceptDialog>(screen,"CombatOrdersDialog");await Click(combat,"CombatPreview");Check(Named<Label>(combat,"CombatPreviewDetails").Text.Contains("Affected ships: 30")&&!Named<Button>(combat,"CombatApply").Disabled,"Finite bulk repair preview names all30 owned ships");await Shot(combat,"bulk-repair-"+size.X);await Click(combat,"CombatApply");Check(session.State.Vessels.All(v=>v.Combat!.Repairing),"Bulk Repair authorizes only selected owned hulls");await End(screen);Check(session.State.Vessels.Count(v=>v.Combat!.Hull==71)==29&&session.State.Vessels.Count(v=>v.Combat!.Hull==49)==1&&session.State.Vessels.All(v=>v.Combat!.Cohesion==50),"Actual first full stationary interval repairs exact25 hull/20 cohesion for all30");await Close(Descendants<TurnReportDialog>(screen).Single());
        await Click(screen,"OpenFleetCommand");fleet=Descendants<FleetCommandDialog>(screen).Single();await Pick(Named<OptionButton>(fleet,"FleetShipFilter"),12);Check(Named<ItemList>(fleet,"FleetShipList").ItemCount==30&&Named<ItemList>(fleet,"FleetShipList").GetItemText(0).Contains("Repairing"),"Repairing filter and numeric statuses are visible");await Tab(fleet,1);await Select(Named<ItemList>(fleet,"FleetGroupList"),0);await Pick(Named<OptionButton>(fleet,"FleetStandingOrder"),0);await Click(fleet,"FleetSetStandingOrder");var confirmation=Named<ConfirmationDialog>(fleet,"FleetActionConfirmation");await Click(confirmation.GetOkButton());Check(session.State.Vessels.All(v=>!v.Combat!.Repairing&&v.Combat.ReturnColony is null),"Explicit ordinary fleet Hold stops finite repair even when the prior fleet order was Hold");await Close(fleet);await End(screen);Check(session.State.Vessels.Count(v=>v.Combat!.Hull==71)==29&&session.State.Vessels.Count(v=>v.Combat!.Hull==49)==1&&session.State.Vessels.All(v=>v.Combat!.Cohesion==50),"Manual Hold override prevents a later repair tick");await Close(Descendants<TurnReportDialog>(screen).Single());screen=await SaveReload(main,screen,"bulk-"+size.X);Check(Live(screen).State.Vessels.Count==30,"All30 owned hulls persist through ordinary Main Save/Load");RemoveChild(main);main.QueueFree();await Frames(this);
    }
    private async Task Privacy(Vector2I size)
    {
        GetTree().Root.Size=size;var fixture=Board(secondHuman:true);var (main,screen)=await Mount(fixture,"two-human-"+size.X);var session=Live(screen);var combat=await Combat(screen);await PickId(Named<OptionButton>(combat,"CombatReturnColony"),session.State.Colonies.First(c=>c.EmpireId==session.State.Empires[0].Id).Id.Value);await Click(combat,"CombatPreview");await Click(combat,"CombatApply");await Click(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));Check(session.State.Turn==0&&session.State.Empires[0].Ready&&!session.State.Empires[1].Ready,"First human Ready saves private planning without prematurely firing");screen=await SaveReload(main,screen,"two-human-"+size.X);session=Live(screen);Check(session.State.Empires[0].Ready&&session.State.Combat!.Encounters[0].Phase==CombatPhase.Pending,"Actual Main reload retains first-human Ready and pending attack");screen.Hide();
        var viewer=session.State.Empires[1].Id;var other=Screen(session,viewer,_output);Paths(other,"other-human-"+size.X);AddChild(other);await Frames(this);string before=Snapshot(session);await Click(other,"OpenFleetCommand");var fleet=Descendants<FleetCommandDialog>(other).Single();var rows=Named<ItemList>(fleet,"FleetShipList");Check(rows.ItemCount==1&&session.State.FindVessel(new(rows.GetItemMetadata(0).AsInt32()))!.EmpireId==viewer,"Other-human command list contains only its own hull and selection");await Close(fleet);combat=await Combat(other);await PickId(Named<OptionButton>(combat,"CombatReturnColony"),session.State.Colonies.First(c=>c.EmpireId==viewer).Id.Value);await Click(combat,"CombatPreview");Check(Named<Button>(combat,"CombatApply").Disabled&&Named<Label>(combat,"CombatError").Text.Contains("unavailable for a new commitment"),"Competing current pocket commitment gives generic reserved result");Check(!Named<Label>(combat,"CombatPreviewDetails").Text.Contains("Labelled combat board",StringComparison.Ordinal)&&Snapshot(session)==before&&session.State.Empires[0].Ready,"Foreign fleet identity, hull and accepted Ready do not leak or change");await Shot(combat,"two-human-reservation-"+size.X);await Close(combat);RemoveChild(other);other.QueueFree();RemoveChild(main);main.QueueFree();await Frames(this);
    }
    private async Task CurrentSetup()
    {
        var setup=new NewGameScreen(CompleteContent()){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(setup);await Frames(this);var sentries=Descendants<OptionButton>(setup).SingleOrDefault(p=>p.Name=="SentriesModePicker");Check(sentries is not null&&sentries.IsVisibleInTree(),"Actual current New Game has explicit Sentries Standard / Off");Check(sentries!.Selected==0,"Sentries initially Standard");NewGameSettings? choice=null;setup.StartRequested+=s=>choice=s;setup.Configure("small",42);await Click(Descendants<Button>(setup).Single(b=>b.Text=="Start"));Check(choice is{RulesVersion:9,SentriesEnabled:true,AmebasEnabled:true,CrisesEnabled:true,RivalCount:1},"Actual setup submits independent rules9 Standard sentries/ameba/plague and One rival");await Pick(sentries,1);setup.Configure("small",42,"harl",0,crisesEnabled:false,amebasEnabled:false);await Click(Descendants<Button>(setup).Single(b=>b.Text=="Start"));Check(choice is{RulesVersion:9,SentriesEnabled:false,AmebasEnabled:false,CrisesEnabled:false,RivalCount:0},"Actual Off and Solo choices stay independent");RemoveChild(setup);setup.QueueFree();await Frames(this);
    }
    private static string Hash(string text)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    private void RecordUi(GameScreen screen,GameState before,Command command,string intent)
    {
        if(_campaignLabel is null)return;var actual=Live(screen);var probe=CommandProcessor.Execute(actual.Content,before,command);Check(probe.Accepted&&SaveGame.Serialize(actual.Content,probe.State)==Snapshot(actual),"Actual generated UI command matches ordinary command: "+intent);
        if(probe.Report is {} report)_earnedProduction=checked(_earnedProduction+report.Economy.Colony(_campaignHome).NetProduction.Total);
        object payload=command switch
        {
            FleetCommand f=>new{kind=command.GetType().Name,f.Issuer,f.ExpectedTurn,actionKind=f.Action.GetType().Name,action=JsonSerializer.SerializeToElement(f.Action,f.Action.GetType()),f.Fingerprint},
            CombatActionCommand c=>new{kind=command.GetType().Name,c.Issuer,c.ExpectedTurn,actionKind=c.Action.GetType().Name,action=JsonSerializer.SerializeToElement(c.Action,c.Action.GetType()),c.PreviewToken},
            _=>JsonSerializer.SerializeToElement(command,command.GetType())
        };
        _commandTrace.Add(new{intent,fromTurn=before.Turn,toTurn=actual.State.Turn,command=payload,beforeHash=Hash(SaveGame.Serialize(actual.Content,before)),afterHash=Hash(Snapshot(actual))});
        foreach(var replay in _replayBranches.ToArray())
        {
            var result=replay.Session.Execute(command);Check(result.Accepted&&Snapshot(replay.Session)==Snapshot(actual),"Saved "+replay.Label+" exact future through actual UI command "+intent);
            if(actual.State.Turn>=replay.Through){_verifiedStages.Add(replay.Label);_replayBranches.Remove(replay);}
        }
    }
    private async Task<GameScreen> Checkpoint(Control main,GameScreen screen,string stage)
    {
        var source=Live(screen);var uninterrupted=new GameSession(source.Content,source.State.Clone());int through=source.State.Turn+3;string label=_campaignLabel+"-"+stage;
        SaveGame.WriteFile(Path.Combine(_campaignFolder,stage+".json"),source.Content,source.State);screen=await SaveReload(main,screen,label);_replayBranches.Add((stage,uninterrupted,through));return screen;
    }
    private async Task PickKey(OptionButton picker,string key)=>await Pick(picker,Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsString()==key));
    private async Task SelectHome(GameScreen screen,ColonyId home)=>await PickId(Descendants<OptionButton>(Descendants<ColonyList>(screen).Single()).Single(),home.Value);
    private async Task BuildWarship(GameScreen screen,string role)
    {
        await SelectHome(screen,_campaignHome);var session=Live(screen);var before=session.State;var command=new SetProductionTargetCommand(before.Empires[0].Id,before.Turn,_campaignHome,new ProductionTarget(ProductionKinds.Vessel,role));await PickKey(Named<OptionButton>(screen,"ColonyProjectPicker"),"vessel|"+role);RecordUi(screen,before,command,"Select paid warship construction");Check(session.State.FindColony(_campaignHome)!.ProjectId==role,"Actual colony construction selector accepts paid warship role");
    }
    private async Task ScoutTo(GameScreen screen,VesselId scout,StarId destination)
    {
        await PickId(Named<OptionButton>(screen,"VesselPicker"),scout.Value);await PickId(Named<OptionButton>(screen,"DestinationPicker"),destination.Value);var session=Live(screen);var before=session.State;await Click(screen,"SetVesselCourse");RecordUi(screen,before,new SetMoveOrderCommand(before.Empires[0].Id,before.Turn,scout,destination),"Scout ordinary course from public chart policy");
    }
    private async Task FleetActionUi(GameScreen screen,FleetAction action,string button,Action<FleetCommandDialog>? configure=null)
    {
        await Click(screen,"OpenFleetCommand");var fleet=Descendants<FleetCommandDialog>(screen).Single();
        if(action is FleetAction.Create create)
        {
            await Tab(fleet,0);var list=Named<ItemList>(fleet,"FleetShipList");list.DeselectAll();foreach(var id in create.Members){int index=Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemMetadata(i).AsInt32()==id.Value);list.Select(index,false);}list.EmitSignal(ItemList.SignalName.MultiSelected,0L,true);Named<LineEdit>(fleet,"FleetName").Text=create.Name;await Frames(this);
        }
        else
        {
            await Tab(fleet,1);await Select(Named<ItemList>(fleet,"FleetGroupList"),0);
            if(action is FleetAction.MoveSelection move)await PickId(Named<OptionButton>(fleet,"FleetGroupDestination"),move.Destination!.Value.Value);
        }
        configure?.Invoke(fleet);var session=Live(screen);var before=session.State;var preview=FleetQueries.PreviewAction(session.Content,session.View(before.Empires[0].Id),action);Check(preview.Allowed,"Generated fleet action preview is legal");var command=new FleetCommand(before.Empires[0].Id,before.Turn,action){Fingerprint=preview.Fingerprint};await Click(fleet,button);await Click(Named<ConfirmationDialog>(fleet,"FleetActionConfirmation").GetOkButton());RecordUi(screen,before,command,"Fleet "+action.GetType().Name);await Close(fleet);
    }
    private async Task AttackUi(GameScreen screen,ColonyId home)
    {
        var dialog=await Combat(screen);await Pick(Named<OptionButton>(dialog,"CombatDoctrine"),3);await PickId(Named<OptionButton>(dialog,"CombatReturnColony"),home.Value);await Click(dialog,"CombatPreview");var session=Live(screen);var before=session.State;var view=session.View(before.Empires[0].Id);var fleet=view.Fleets!.Fleets.Single();var pocket=view.Combat!.Pockets.Single(p=>p.Current&&!p.Cleared);var action=new CombatAction.AttackPocket(fleet.Id,pocket.Id,new(CombatDoctrine.Hold,home,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair),false);var preview=CombatQueries.Preview(session.Content,view,action);Check(preview.Allowed,"Generated paid force can attack its own currently observed pocket");await Click(dialog,"CombatApply");RecordUi(screen,before,new CombatActionCommand(view.Viewer,view.Turn,action,preview.Token),"Commit reviewed automatic attack");
    }
    private async Task WithdrawUi(GameScreen screen)
    {
        var dialog=await Combat(screen);var session=Live(screen);var before=session.State;var encounter=before.Combat!.Encounters.Single();var action=new CombatAction.WithdrawCombat(encounter.Id,encounter.Revision);var view=session.View(encounter.Owner);var preview=CombatQueries.Preview(session.Content,view,action);Check(preview.Allowed,"Generated withdrawal has a legal explicit return");await Click(dialog,"CombatWithdraw");await Click(dialog,"CombatApply");RecordUi(screen,before,new CombatActionCommand(encounter.Owner,before.Turn,action,preview.Token),"Withdraw under reviewed return authority");
    }
    private async Task StepGenerated(GameScreen screen){await End(screen);await Close(Descendants<TurnReportDialog>(screen).Single());}
    private async Task GeneratedCampaign(string species,string size)
    {
        GetTree().Root.Size=new(1920,1080);_campaignLabel=species+"-"+size;_campaignFolder=Path.Combine(_output,"generated",_campaignLabel);Directory.CreateDirectory(_campaignFolder);_commandTrace.Clear();_replayBranches.Clear();_verifiedStages.Clear();_earnedProduction=0;GD.Print("Generated combat campaign start: "+_campaignLabel);Control? main=null;string? sourceHash=null;long initialReserve=0;string? error=null;
        try
        {
            main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames(this);var setup=Descendants<NewGameScreen>(main).Single();Check(Named<OptionButton>(setup,"SentriesModePicker").Selected==0,"Generated campaign retains initial Standard sentries");setup.Configure(size,42,species,0,crisesEnabled:false,amebasEnabled:false,sentriesEnabled:true);await Click(Descendants<Button>(setup).Single(b=>b.Text=="Start"));var screen=Descendants<GameScreen>(main).Single();Paths(screen,_campaignLabel);var session=Live(screen);Check(session.State.RulesVersion==9&&session.State.Turn==0&&session.State.Combat!.SentriesEnabled,"Actual New Game created untouched rules9 turn0");var source=Snapshot(session);sourceHash=Hash(source);SaveGame.WriteFile(Path.Combine(_campaignFolder,"start.json"),session.Content,session.State);_campaignHome=session.State.Colonies.Single().Id;initialReserve=session.State.FindColony(_campaignHome)!.ProductionReserve;
            var owner=session.State.Empires[0].Id;var homeStar=session.State.FindPlanet(session.State.FindColony(_campaignHome)!.PlanetId)!.StarId;var scout=session.State.Vessels.Single().Id;string warship=session.Content.Vessels.Values.Where(v=>v.CombatOnly&&session.Content.Combat!.RoleProfiles[v.Id].Attack>0).OrderBy(v=>v.Id,StringComparer.Ordinal).First().Id;long cost=session.Content.Vessels[warship].Cost;
            for(int boundary=0;boundary<240;boundary++)
            {
                var view=session.View(owner);int built=view.Vessels.Count(v=>v.RoleId==warship);if(built>=2&&!view.Combat!.Pockets.IsEmpty)break;
                if(built<2&&session.State.FindColony(_campaignHome)!.ProjectId!=warship)await BuildWarship(screen,warship);
                var explorer=view.Vessels.Single(v=>v.Id==scout);
                if(view.Combat!.Pockets.IsEmpty&&explorer.Transit is null&&explorer.PendingMove is null)
                {
                    var home=view.Galaxy.Stars.Single(s=>s.Id==homeStar);Int128 Distance(ChartedStar a){Int128 x=(Int128)a.X-home.X,y=(Int128)a.Y-home.Y;return x*x+y*y;}
                    var target=view.Stars.Where(s=>s.Planets is null&&s.Chart.Id!=explorer.StationaryStarId).OrderByDescending(s=>Distance(s.Chart)).ThenBy(s=>s.Chart.Id).FirstOrDefault();Check(target is not null,"Public unsurveyed chart offers another scout destination");await ScoutTo(screen,scout,target!.Chart.Id);
                }
                await StepGenerated(screen);
            }
            var known=session.View(owner).Combat!.Pockets.Single();Check(known.Current&&!known.Cleared&&session.State.FindVessel(scout)!.Combat!.Hull==60&&session.State.Combat!.Encounters.Count==0,"Own ordinary scout discovered the generated guarded pocket without an automatic battle");var paid=session.State.Vessels.Where(v=>v.RoleId==warship).Select(v=>v.Id).OrderBy(id=>id).ToImmutableArray();Check(paid.Length==2,"Two warships completed through actual paid colony controls");Check(initialReserve+_earnedProduction==session.State.FindColony(_campaignHome)!.ProductionReserve+2*cost,"Actual earned production exactly funds two warships and retained reserve");
            await FleetActionUi(screen,new FleetAction.Create("Paid combat force",paid),"FleetCreate");var fleetId=session.State.Empires[0].Fleets!.Fleets.Single().Id;await FleetActionUi(screen,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(fleetId),known.Star),"FleetPreviewGroupMove");
            for(int i=0;i<40&&paid.Any(id=>session.State.FindVessel(id)!.StationaryStarId!=known.Star);i++)await StepGenerated(screen);Check(paid.All(id=>session.State.FindVessel(id)!.StationaryStarId==known.Star),"Paid named fleet arrives by ordinary committed travel");await AttackUi(screen,_campaignHome);screen=await Checkpoint(main,screen,"pending");session=Live(screen);await StepGenerated(screen);Check(session.State.Combat!.Encounters[0].Round==1&&session.State.FindVessel(paid[0])!.Combat!.Hull==72,"Genuine paid first volley has exact core damage");screen=await Checkpoint(main,screen,"active");session=Live(screen);await WithdrawUi(screen);await StepGenerated(screen);Check(session.State.Combat!.Encounters.Count==0,"Genuine reviewed withdrawal closes first encounter");screen=await Checkpoint(main,screen,"return");session=Live(screen);
            for(int i=0;i<40&&paid.Any(id=>session.State.FindVessel(id)!.Transit is not null);i++)await StepGenerated(screen);Check(session.State.FindVessel(paid[0])!.Combat!.Hull==72,"Actual return arrival earns no immediate healing");Check(session.State.FindVessel(paid[0])!.Combat!.Repairing,"Finite Repair starts only at the reached owned port");screen=await Checkpoint(main,screen,"repair");session=Live(screen);
            for(int i=0;i<10&&paid.Any(id=>session.State.FindVessel(id)!.Combat!.Repairing);i++)await StepGenerated(screen);Check(paid.All(id=>session.State.FindVessel(id)!.Combat!.Hull==100&&session.State.FindVessel(id)!.Combat!.Cohesion==100),"Genuine full stationary repair restores the paid force");Check(session.State.Empires[0].Combat!.LifetimeHullRepaired==28,"Genuine repair caps actual restored hull at28");
            await FleetActionUi(screen,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(fleetId),known.Star),"FleetPreviewGroupMove");for(int i=0;i<40&&paid.Any(id=>session.State.FindVessel(id)!.StationaryStarId!=known.Star);i++)await StepGenerated(screen);await AttackUi(screen,_campaignHome);for(int i=0;i<10&&session.State.Combat!.Encounters.Count>0;i++)await StepGenerated(screen);Check(session.State.Combat!.Pockets.Single().Cleared&&paid.All(id=>session.State.FindVessel(id) is not null),"Genuine repaired force returns and clears the original persistent sentries");Check(session.State.Colonies.Count==1&&!session.State.Colonies.Any(c=>session.State.FindPlanet(c.PlanetId)!.StarId==known.Star),"Clearing grants access without ownership or automatic founding");Check(session.State.Empires[0].Fleets!.Officers.All(o=>o.Xp==0&&o.Rank==0)&&session.State.Empires[0].Fleets!.RecoveryRollCount==0,"No manufactured officer progression or combat XP / recovery draws");
            for(int i=0;i<5&&_replayBranches.Count>0;i++)await StepGenerated(screen);Check(new[]{"pending","active","return","repair"}.All(_verifiedStages.Contains),"Every saved combat phase matches3 future turns of uninterrupted commands and actual UI continuation");SaveGame.WriteFile(Path.Combine(_campaignFolder,"cleared.json"),session.Content,session.State);Check(initialReserve+_earnedProduction==session.State.FindColony(_campaignHome)!.ProductionReserve+2*cost,"Full genuine campaign keeps the exact production ledger without free ships");_generatedCases++;
            _provenance.Add(new{source="Untouched actual Main New Game seed42, Solo, plague Off, ameba Off, Sentries Standard; only actual ordinary controls after Start",species,size,sourceHash,commands=_commandTrace.Count,turns=session.State.Turn,verifiedStages=_verifiedStages.ToArray(),liveOutcomeInjections=0});GD.Print("Generated combat campaign complete: "+_campaignLabel+" turn "+session.State.Turn);
        }
        catch(Exception e){error=e.ToString();throw;}
        finally
        {
            File.WriteAllText(Path.Combine(_campaignFolder,"commands-and-proof.json"),JsonSerializer.Serialize(new{species,size,seed=42,sourceHash,error,initialReserve,earnedProduction=_earnedProduction,verifiedStages=_verifiedStages,commands=_commandTrace},new JsonSerializerOptions{WriteIndented=true}));_campaignLabel=null;_replayBranches.Clear();if(main is not null){RemoveChild(main);main.QueueFree();await Frames(this);}
        }
    }
    private async Task LegacyAndInvalidSetup()
    {
        var c=CompleteContent();var old=GalaxyGenerator.CreateNewGame(c,new("small",42,8,"harl",AmebasEnabled:false)).State!;var (main,screen)=await Mount(new(c,old),"legacy8");await Click(screen,"OpenFleetCommand");var fleet=Descendants<FleetCommandDialog>(screen).Single();Check(Named<OptionButton>(fleet,"FleetShipFilter").ItemCount==7&&!Named<Button>(fleet,"FleetCombatOrders").Visible,"Rules8 Main load keeps original filters and no combat controls");Check(Live(screen).View(old.Empires[0].Id).Combat is null&&!Live(screen).Content.Vessels.Values.Any(v=>v.CombatOnly),"Rules8 embedded roles/state exclude combat despite current disk content");await Close(fleet);RemoveChild(main);main.QueueFree();await Frames(this);
        var small=c.GalaxySizes["small"] with{StarCount=2};var alternative=c with{GalaxySizes=c.GalaxySizes.SetItem("small",small)};var prior=new GameSession(c,old);string before=Snapshot(prior);var setup=new NewGameScreen(alternative){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(setup);await Frames(this);setup.Configure("small",42,"harl",1,"peldra",false,false,true);NewGameResult? result=null;setup.StartRequested+=settings=>{result=GalaxyGenerator.CreateNewGame(alternative,settings);if(!result.Succeeded)setup.ShowError(result.Error!);};await Click(Descendants<Button>(setup).Single(b=>b.Text=="Start"));Check(result is{Succeeded:false}&&result.Error!.Contains("Sentries",StringComparison.Ordinal)&&Snapshot(prior)==before,"Labelled2-home alternative has no legal Standard pocket and preserves prior session without reroll");Check(Descendants<Button>(setup).Single(b=>b.Text=="Load Game").IsVisibleInTree(),"Load remains available after invalid Standard setup");await Pick(Named<OptionButton>(setup,"SentriesModePicker"),1);await Click(Descendants<Button>(setup).Single(b=>b.Text=="Start"));Check(result is{Succeeded:true},"Same labelled geometry can explicitly choose Sentries Off without reroll");RemoveChild(setup);setup.QueueFree();await Frames(this);
    }
}
