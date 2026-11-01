using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.Json;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using static OpenAntares.Game.Validation.ScoutUiControlChecks;
namespace OpenAntares.Game.Validation;
public partial class EmpireConflictGraphicalValidation:Node
{
    private string _output="";private readonly List<object> _checks=new(),_provenance=new();
    private void Check(bool value,string text){_checks.Add(new{passed=value,text});if(!value)throw new InvalidOperationException(text);}
    public override async void _Ready()
    {
        string? error=null;
        try{_output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output="))[20..];Directory.CreateDirectory(_output);Check(DisplayServer.GetName()!="headless","Actual graphical Godot display");EmpireConflictCampaignUiChecks.MixedDefenseAndLocalFallbackAreExplained(Check);await AttackChangeBlockadeAndLiftUseReviewedControls();await BlockadeIncomeAndReportsAgreeWithActualTurn();await MixedDefenseAndLocalFallbackAreExplained();await PublishedWithdrawalAndSolo();await WarPeaceAndOwnPendingAnswersUseRealControls();if(OS.GetCmdlineUserArgs().Contains("--validation-earned-conflict"))foreach(var species in new[]{"harl","peldra","sulan","wenth"})await EarnedMainCampaign(species);}
        catch(Exception e){error=e.ToString();GD.PrintErr(error);}
        finally{if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{scenario="EmpireConflict",passed=error is null,error,method="Actual Godot control signals and Main Save/Load; labelled pre-session multi-human supplements; no physical-input or comprehension claim",checks=_checks,provenance=_provenance},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(error is null?0:1);}
    }
    private static GameSession Live(GameScreen screen)=>(GameSession)typeof(GameScreen).GetField("_session",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(screen)!;
    private static string Snapshot(GameSession session)=>SaveGame.Serialize(session.Content,session.State);
    private void Paths(GameScreen screen,string label){screen.CombatPreferencesPath=Path.Combine(_output,label+"-combat-prefs.json");screen.FleetPreferencesPath=Path.Combine(_output,label+"-fleet-prefs.json");screen.ScoutPreferencesPath=Path.Combine(_output,label+"-scout-prefs.json");screen.CrisisPreferencesPath=Path.Combine(_output,label+"-crisis-prefs.json");screen.NotificationPreferencesPath=Path.Combine(_output,label+"-colony-prefs.json");}
    private async Task Click(Button button){Check(!button.Disabled&&button.IsVisibleInTree(),"Reachable "+button.Name);for(Node? node=button.GetParent();node is not null;node=node.GetParent())if(node is ScrollContainer scroll)scroll.EnsureControlVisible(button);button.GrabFocus();await Frames(this);button.EmitSignal(Button.SignalName.Pressed);await Frames(this);}
    private Task Click(Node root,string name)=>Click(Named<Button>(root,name));
    private async Task Load(Control main,string name){var load=Descendants<LoadGameDialog>(main).Single();var list=Descendants<ItemList>(load).Single();int index=Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemText(i).StartsWith(name+"    ",StringComparison.Ordinal));list.Select(index);list.EmitSignal(ItemList.SignalName.ItemSelected,(long)index);await Click(load.GetOkButton());}
    private async Task<(Control Main,GameScreen Screen)> Mount(LoadedGame fixture,string label)
    {
        string name="qa-empire-conflict-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+label;string file=SaveFiles.PathFor(name);Check(!File.Exists(file),"Unique retained conflict fixture");SaveGame.WriteFile(file,fixture.Content,fixture.State);var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames(this);await Click(Descendants<Button>(Descendants<NewGameScreen>(main).Single()).Single(b=>b.Text.Contains("Load",StringComparison.Ordinal)));await Load(main,name);var screen=Descendants<GameScreen>(main).Single();Paths(screen,label);Check(Snapshot(Live(screen))==SaveGame.Serialize(fixture.Content,fixture.State),"Ordinary Main Load preserves the complete labelled source");_provenance.Add(new{source="Labelled pre-session two-human contact supplement, no earned campaign claim",file});return(main,screen);
    }
    private async Task<GameScreen> SaveReload(Control main,GameScreen screen,string label)
    {
        string before=Snapshot(Live(screen)),name="qa-empire-conflict-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+label;foreach(var window in Descendants<Window>(screen))window.Hide();await Click(Descendants<Button>(screen).Single(b=>b.Text=="Save"));var save=Descendants<SaveGameDialog>(screen).Single();Descendants<LineEdit>(save).Single().Text=name;await Click(save.GetOkButton());Check(File.Exists(SaveFiles.PathFor(name)),"Actual Save dialog wrote "+label);await Click(Descendants<Button>(screen).Single(b=>b.Text=="Load"));await Load(main,name);screen=Descendants<GameScreen>(main).Single();Paths(screen,label);Check(Snapshot(Live(screen))==before,"Actual Main Save/Load preserves "+label);_provenance.Add(new{source="Actual Main Save/Load controls",file=SaveFiles.PathFor(name)});return screen;
    }
    private LoadedGame PoliticalFixture()
    {
        var content=CompleteContent();var generated=GalaxyGenerator.CreateNewGame(content,new("small",42,12,"harl",1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));Check(generated.Succeeded,"Explicit12 source generation");var state=generated.State!;var a=state.Empires[0];var b=state.Empires[1];b.Controller=ControllerKind.Human;b.AiPolicyId=null;a.Contacts.Add(new(b.Id,b.SpeciesId!,0));b.Contacts.Add(new(a.Id,a.SpeciesId!,0));var pair=DiplomacyPair.For(a.Id,b.Id);state.Diplomacy!.Pairs.Add(new(){Pair=pair,Parties=new(){new(){Owner=pair.Low},new(){Owner=pair.High}}});state.Conflict!.Pairs.Add(new(){Pair=pair,Parties=new(){new(){Owner=pair.Low},new(){Owner=pair.High}}});Check(GameStateValidator.Validate(content,state).Count==0,"Labelled multi-human political supplement validates before mounting");return new(content,state);
    }
    private async Task MixedDefenseAndLocalFallbackAreExplained()
    {
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})
        {
            GetTree().Root.Size=size;var fixture=PoliticalFixture();var s=fixture.State;var pair=s.Conflict!.Pairs.Single();pair.Relation=RelationKind.War;pair.WarTurn=0;pair.Declarers.Add(pair.Pair.Low);pair.Parties[1].DeclarationGrievance=true;pair.Parties[1].LastCounterpartDeclarationTurn=0;
            var star=s.FindPlanet(s.Colonies.First(c=>c.EmpireId==pair.Pair.Low).PlanetId)!.StarId;
            VesselState Armed(EmpireId owner){var v=new VesselState{Id=new(s.AllocateId()),EmpireId=owner,RoleId=fixture.Content.ShipDesigns!.WarshipRoleId,Design=new(1),StationaryStarId=star,Crisis=new()};OpenAntares.Simulation.Scouting.ScoutInitializer.InitializeVessel(fixture.Content,s.RulesVersion,v);OpenAntares.Simulation.Combat.CombatInitializer.InitializeVessel(fixture.Content,s,v);s.Vessels.Add(v);return v;}
            var low=Armed(pair.Pair.Low);var high=Armed(pair.Pair.High);var role=fixture.Content.Species[s.Empires[0].SpeciesId!].AllowedVesselIds.Select(id=>fixture.Content.Vessels[id]).First(r=>r.CanFoundColony);
            var civilian=new VesselState{Id=new(s.AllocateId()),EmpireId=pair.Pair.Low,RoleId=role.Id,CarriedPopulation=role.EmbarkPopulation,StationaryStarId=star,Crisis=new()};OpenAntares.Simulation.Combat.CombatInitializer.InitializeVessel(fixture.Content,s,civilian);s.Vessels.Add(civilian);
            var fleets=s.Empires[0].Fleets!;var fleet=new OpenAntares.Simulation.Fleets.FleetRecord{Id=new(fleets.NextFleetId++),Name="Labelled mixed defense supplement",Flagship=civilian.Id,Members=new(){low.Id,civilian.Id}};fleets.Fleets.Add(fleet);low.Fleet=fleet.Id;civilian.Fleet=fleet.Id;
            s.Conflict.Encounters.Add(new(){Id=new(1),Pair=pair.Pair,Star=star,CreatedTurn=0,LowSide=new(){Owner=pair.Pair.Low,Fleet=fleet.Id,Anchor=low.Id,Flagship=civilian.Id,Members=new(){low.Id},ProtectedCivilians=new(){civilian.Id},NeedsResponse=true},HighSide=new(){Owner=pair.Pair.High,Anchor=high.Id,Flagship=high.Id,Members=new(){high.Id}}});s.Conflict.NextEncounterId=2;
            OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(s,0);ConflictObservations.Refresh(fixture.Content,s);Check(GameStateValidator.Validate(fixture.Content,s).Count==0,"Labelled mixed-defense/revised-response supplement validates");
            var (main,screen)=await Mount(fixture,"mixed-defense-"+size.X);
            try
            {
                var session=Live(screen);Check(Descendants<Button>(screen).Single(b=>b.Text=="End Turn").Disabled,"Own response blocks Ready without accepting orders");screen=await SaveReload(main,screen,"pending-response-"+size.X);session=Live(screen);string before=Snapshot(session);
                await Click(screen,"OpenConflictOrders");var dialog=Named<ConflictOrdersDialog>(screen,"ConflictOrdersDialog");Check(Named<Label>(dialog,"ConflictOwnDetails").Text.Contains("1 protected"),"Actual mixed-force panel names protected civilians");Check(!Named<Label>(dialog,"ConflictForeignDetails").Text.Contains("cohesion",StringComparison.OrdinalIgnoreCase),"Actual foreign details contain no private cohesion");Check(Snapshot(session)==before,"Opening conflict orders has no side effects");Check(Named<OptionButton>(dialog,"ConflictReturnPort").GetItemText(0).Contains("same system"),"Actual fallback picker explains same-system break");
                await Click(dialog,"ConflictReviewRespond");Check(Snapshot(session)==before,"Review does not accept a response");await Click(dialog,"ConflictOrdersApply");Check(!session.View(pair.Pair.Low).Conflict!.Encounters.Single().OwnNeedsResponse,"Explicit apply accepts only own current force");Check(!Named<Button>(dialog,"ConflictOrdersApply").IsVisibleInTree()||Named<Button>(dialog,"ConflictOrdersApply").Disabled,"Submission cannot be duplicated");
                await Click(screen,"OpenConflictOrders");await Click(dialog,"ConflictReviewWithdraw");Check(Named<Label>(dialog,"ConflictOrderPreview").Text.Contains("same system"),"Local withdrawal review describes actual fallback");await Capture(this,dialog,Path.Combine(_output,"mixed-defense-"+size.X+".png"));await Click(dialog,"ConflictReviewWithdraw");await Click(dialog,"ConflictOrdersApply");Check(session.View(pair.Pair.Low).Conflict!.Encounters.Single().OwnWithdrawQueued,"Explicit withdrawal queues own committed order");Check(!session.View(pair.Pair.High).Conflict!.Encounters.Single().OwnWithdrawQueued,"Independent opponent does not inherit own withdrawal");
            }
            finally{RemoveChild(main);main.QueueFree();await Frames(this);}
        }
    }
    private LoadedGame MilitaryControlFixture(bool defender)
    {
        var fixture=PoliticalFixture();var state=fixture.State;var pair=state.Conflict!.Pairs.Single();pair.Relation=RelationKind.War;pair.WarTurn=0;pair.Declarers.Add(pair.Pair.Low);pair.Parties[1].DeclarationGrievance=true;pair.Parties[1].LastCounterpartDeclarationTurn=0;
        var star=state.FindPlanet(state.Colonies.First(c=>c.EmpireId==pair.Pair.High).PlanetId)!.StarId;
        VesselState Armed(EmpireId owner){var ship=new VesselState{Id=new(state.AllocateId()),EmpireId=owner,RoleId=fixture.Content.ShipDesigns!.WarshipRoleId,Design=new(1),StationaryStarId=star,Crisis=new()};OpenAntares.Simulation.Scouting.ScoutInitializer.InitializeVessel(fixture.Content,state.RulesVersion,ship);OpenAntares.Simulation.Combat.CombatInitializer.InitializeVessel(fixture.Content,state,ship);state.Vessels.Add(ship);return ship;}
        var own=Armed(pair.Pair.Low);if(defender)Armed(pair.Pair.High);var fleets=state.Empires[0].Fleets!;var fleet=new OpenAntares.Simulation.Fleets.FleetRecord{Id=new(fleets.NextFleetId++),Name="Labelled armed control supplement",Members=new(){own.Id},Flagship=own.Id};fleets.Fleets.Add(fleet);own.Fleet=fleet.Id;
        OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(state,0);ConflictObservations.Refresh(fixture.Content,state);Check(GameStateValidator.Validate(fixture.Content,state).Count==0,"Labelled armed control supplement validates before mounting");return fixture;
    }
    private async Task SetPicker(Node root,string name,int index)
    {var picker=Named<OptionButton>(root,name);Check(index>=0&&index<picker.ItemCount,"Valid choice in "+name);picker.Select(index);picker.EmitSignal(OptionButton.SignalName.ItemSelected,(long)index);await Frames(this);}
    private async Task FinishTwoHumanBoundary(GameScreen screen,EmpireId other)
    {await End(screen);var second=await Other(screen,other);try{await End(second);if(Live(second).View(other).Conflict!.Encounters.Any(e=>e.Round>0)){var report=Descendants<TurnReportDialog>(second).Single();Check(Text(report).Contains("armed ship")&&Text(report).Contains("armor")&&Text(report).Contains("no spill"),"Actual owning perspective report exposes permitted shot calculations");}}finally{await RemoveOther(second,screen);}}
    private async Task AttackChangeBlockadeAndLiftUseReviewedControls()
    {
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})
        {
            GetTree().Root.Size=size;var fixture=MilitaryControlFixture(true);var a=fixture.State.Empires[0].Id;var b=fixture.State.Empires[1].Id;var (main,screen)=await Mount(fixture,"attack-controls-"+size.X);
            try
            {
                await Click(screen,"OpenConflictOrders");var dialog=Named<ConflictOrdersDialog>(screen,"ConflictOrdersDialog");await SetPicker(dialog,"ConflictReturnPort",1);await SetPicker(dialog,"ConflictDoctrine",3);string before=Snapshot(Live(screen));
                await Click(dialog,"ConflictReviewAttack");Check(Snapshot(Live(screen))==before,"Attack review has no commitment side effect");await SetPicker(dialog,"ConflictReturnPort",0);Check(Named<Button>(dialog,"ConflictOrdersApply").Disabled,"Changing the return invalidates the reviewed attack");await SetPicker(dialog,"ConflictReturnPort",1);await Click(dialog,"ConflictReviewAttack");await Capture(this,dialog,Path.Combine(_output,"attack-review-"+size.X+".png"));await Click(dialog,"ConflictReviewAttack");await Click(dialog,"ConflictOrdersApply");Check(Live(screen).View(a).Conflict!.OwnAttacks.Length==1,"Actual Attack Apply queues one attack");
                screen=await SaveReload(main,screen,"pending-attack-"+size.X);await FinishTwoHumanBoundary(screen,b);Check(Live(screen).View(a).Conflict!.Encounters.Single().Round==1,"Actual final Ready applies one exchange after queued attack");
                var report=Descendants<TurnReportDialog>(screen).Single();HideWindows(screen);await Click(screen,"OpenConflictOrders");dialog=Named<ConflictOrdersDialog>(screen,"ConflictOrdersDialog");await SetPicker(dialog,"ConflictDoctrine",1);await Click(dialog,"ConflictReviewChange");await Click(dialog,"ConflictOrdersApply");Check(Live(screen).View(a).Conflict!.Encounters.Single().OwnOrders.Doctrine==OpenAntares.Simulation.Combat.CombatDoctrine.Press,"Actual Change applies only the selected own orders");Check(Live(screen).View(b).Conflict!.Encounters.Single().OwnOrders.Doctrine==OpenAntares.Simulation.Combat.CombatDoctrine.Cautious,"Other human's doctrine remains independent");
            }
            finally{RemoveChild(main);main.QueueFree();await Frames(this);}
            fixture=MilitaryControlFixture(false);(main,screen)=await Mount(fixture,"blockade-controls-"+size.X);
            try
            {
                await Click(screen,"OpenConflictOrders");var dialog=Named<ConflictOrdersDialog>(screen,"ConflictOrdersDialog");await Click(dialog,"ConflictReviewBlockade");Check(Named<Label>(dialog,"ConflictOrderPreview").Text.Contains("next publication"),"Blockade review explains its activation boundary");await Click(dialog,"ConflictOrdersApply");Check(Live(screen).View(a).Conflict!.Blockades.Single().Status=="Pending","Actual Blockade Apply waits for publication");await FinishTwoHumanBoundary(screen,b);Check(Live(screen).View(a).Conflict!.Blockades.Single().Status=="Effective","Published stationary blockade becomes effective");
                screen=await SaveReload(main,screen,"effective-blockade-"+size.X);await Click(screen,"OpenConflictOrders");dialog=Named<ConflictOrdersDialog>(screen,"ConflictOrdersDialog");string victim=System.Text.Json.JsonSerializer.Serialize(Live(screen).View(b));await Click(dialog,"ConflictReviewLift");await Capture(this,dialog,Path.Combine(_output,"lift-review-"+size.X+".png"));await Click(dialog,"ConflictReviewLift");await Click(dialog,"ConflictOrdersApply");Check(Live(screen).View(a).Conflict!.Blockades.IsEmpty,"Actual Lift Apply removes only own blockade");Check(System.Text.Json.JsonSerializer.Serialize(Live(screen).View(b))==victim,"Private Lift does not reveal itself in the other human's forecasts");await FinishTwoHumanBoundary(screen,b);Check(Live(screen).View(b).Conflict!.OwnPressure.IsEmpty,"Lift is visible to the victim after publication");
            }
            finally{RemoveChild(main);main.QueueFree();await Frames(this);}
        }
    }
    private async Task WarPeaceAndOwnPendingAnswersUseRealControls()
    {
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})
        {
            GD.Print("Conflict political controls start: "+size.X);GetTree().Root.Size=size;var (main,screen)=await Mount(PoliticalFixture(),"politics-"+size.X);
            try
            {
                var session=Live(screen);var a=session.State.Empires[0].Id;var b=session.State.Empires[1].Id;await Click(screen,"OpenDiplomacy");Check(!Descendants<DiplomacyDialog>(screen).Single().IsEmbedded(),"New-rule diplomacy has a readable native window at "+size.X);Check(Descendants<Button>(screen).Any(b=>b.Name=="ConflictDeclareWar"),"Explicit Declare war control exists");string untouched=Snapshot(session);await Click(screen,"OpenDiplomacy");Check(Snapshot(session)==untouched,"Duplicate open issues zero commands");
                await Click(screen,"ConflictDeclareWar");var declaration=Named<AcceptDialog>(screen,"ConflictDeclarationDialog");Check(declaration.Visible&&!declaration.IsEmbedded(),"Separate native declaration confirmation");var terms=Named<Label>(declaration,"ConflictDeclarationTerms").Text;Check(terms.Contains("turn 1")&&terms.Contains("setup")&&terms.Contains("protected")&&terms.Contains("officers"),"Declaration names effective date, treaty/setup consequences and risks");Check(Snapshot(session)==untouched,"Opening declaration does not issue it");var apply=Named<Button>(declaration,"ConflictDeclarationApply");apply.GrabFocus();var scroll=Descendants<ScrollContainer>(declaration).Single();scroll.EnsureControlVisible(apply);await Frames(this);Check(apply.GetGlobalRect().Intersects(scroll.GetGlobalRect()),"Declaration confirmation reachable with keyboard focus at "+size.X);await Capture(this,declaration,Path.Combine(_output,"declaration-"+size.X+".png"));await Click(declaration,"ConflictDeclarationApply");Check(session.View(a).Conflict!.Relations.Single().OwnDeclarationQueued,"Actual confirmation queues only own declaration");
                screen=await SaveReload(main,screen,"queued-declaration-"+size.X);session=Live(screen);await Click(screen,"OpenDiplomacy");await Click(screen,"ConflictRevokeDeclaration");Check(!session.View(a).Conflict!.Relations.Single().OwnDeclarationQueued,"Actual revoke removes own declaration");await Click(screen,"ConflictDeclareWar");await Click(Named<AcceptDialog>(screen,"ConflictDeclarationDialog"),"ConflictDeclarationApply");HideWindows(screen);await End(screen);Check(session.State.Turn==0&&session.State.FindEmpire(a)!.Ready,"First human Ready does not publish declaration");screen=await SaveReload(main,screen,"first-human-ready-"+size.X);session=Live(screen);
                var other=await Other(screen,b);
                try{await Click(other,"OpenDiplomacy");Check(Named<Label>(other,"ConflictRelationText").Text=="Peace"&&!Text(other).Contains("Your declaration is queued"),"Other human cannot see unsubmitted declaration");HideWindows(other);await End(other);Check(session.State.Turn==1&&session.State.Conflict!.Pairs.Single().Relation==RelationKind.War,"Final human Ready publishes War exactly once");await Click(other,"ReportOpenConflictDiplomacy_"+a.Value);Check(Named<Label>(other,"ConflictRelationText").Text=="War","Own war report opens its named diplomacy contact");}
                finally{await RemoveOther(other,screen);}
                await Click(screen,"OpenDiplomacy");Check(Named<Label>(screen,"ConflictPeaceTerms").Text.Contains("10 complete"),"Peace terms state ten complete peaceful boundaries");Check(!Descendants<Button>(screen).Any(x=>x.Name=="DiplomacyOfferTrade"),"Ordinary agreement offers suppressed during War");await Click(screen,"ConflictProposePeace");await Click(screen,"ConflictWithdrawPeace");Check(!session.View(a).Conflict!.Relations.Single().OwnPeaceProposed,"Own unpublished peace proposal can be withdrawn without disclosure");await Click(screen,"ConflictProposePeace");screen=await SaveReload(main,screen,"queued-peace-"+size.X);session=Live(screen);HideWindows(screen);await End(screen);other=await Other(screen,b);
                try
                {
                    await Click(other,"OpenDiplomacy");Check(!Descendants<Button>(other).Any(x=>x.Name=="ConflictAcceptPeace"),"Other human has no answer control for an unpublished proposal");HideWindows(other);await End(other);Check(session.State.Turn==2&&session.State.Conflict!.Pairs.Single().PendingPeace?.Answer is null,"Ready never automatically answers incoming peace");HideWindows(other);await Click(other,"OpenDiplomacy");await Click(other,"ConflictDeclinePeace");Check(Text(other).Contains("Your Decline is queued"),"Exact own queued Decline is visible");await Click(other,"ConflictRevokePeaceAnswer");await Click(other,"ConflictAcceptPeace");Check(Text(other).Contains("Your Accept is queued"),"Exact own queued Accept is visible");Check(session.View(a).Conflict!.Relations.Single().Peace!.OwnAnswer is null,"Outgoing viewer cannot see another human's queued answer");await Capture(this,Descendants<DiplomacyDialog>(other).Single(),Path.Combine(_output,"queued-answer-"+size.X+".png"));
                }
                finally{await RemoveOther(other,screen);}
                screen=await SaveReload(main,screen,"queued-peace-answer-"+size.X);session=Live(screen);await Click(screen,"OpenDiplomacy");Check(!Text(screen).Contains("Your Accept is queued"),"Main reload preserves answer privacy for the proposer");HideWindows(screen);await End(screen);other=await Other(screen,b);
                try{HideWindows(other);await End(other);Check(session.State.Turn==3&&session.State.Conflict!.Pairs.Single().Relation==RelationKind.Peace,"Explicit accepted peace resolves on final Ready");HideWindows(other);await Click(other,"OpenDiplomacy");Check(Named<Label>(other,"ConflictRelationText").Text=="Peace"&&Text(other).Contains("Truce"),"Peace and dated truce are shown");Check(Named<Button>(other,"ConflictDeclareWar").Disabled,"Truce disables another declaration");await Capture(this,Descendants<DiplomacyDialog>(other).Single(),Path.Combine(_output,"truce-"+size.X+".png"));}
                finally{await RemoveOther(other,screen);}
                screen=await SaveReload(main,screen,"truce-"+size.X);Check(Live(screen).State.Conflict!.Pairs.Single().TruceEndTurn==13,"Actual Main truce save preserves exact expiry");GD.Print("Conflict political controls complete: "+size.X);
            }
            finally{RemoveChild(main);main.QueueFree();await Frames(this);}
        }
    }
    private async Task BlockadeIncomeAndReportsAgreeWithActualTurn()
    {
        string manifestPath=ProjectSettings.GlobalizePath("res://reports/conflict-execution/task-19/blockade-ui-supplement.json");
        Check(File.Exists(manifestPath),"Queued simulation must first emit the labelled blockade supplement");using var manifest=JsonDocument.Parse(File.ReadAllText(manifestPath));
        string savePath=ProjectSettings.GlobalizePath("res://"+manifest.RootElement.GetProperty("save").GetString());string raw=File.ReadAllText(savePath);
        string hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();Check(hash==manifest.RootElement.GetProperty("hash").GetString(),"Native blockade supplement content hash matches its retained source");
        var load=SaveGame.Deserialize(raw);Check(load.Succeeded,"Labelled blockade supplement loads");var fixture=load.Game!;var a=fixture.State.Empires[0].Id;var b=fixture.State.Empires[1].Id;var neutral=fixture.State.Empires[2].Id;
        var (main,screen)=await Mount(fixture,"blockade-report");
        try
        {
            var session=Live(screen);Check(session.View(b).Colonies.Single().Economy.NetProduction.Total==450,"Own native source breakdown equals450");string blockader=ConflictBlockadePanel.Explain(session.View(a));Check(!blockader.Contains(session.View(b).Colonies.Single().ProductionReserve.ToString()),"Blockader presentation excludes target reserve");
            await End(screen);var third=await Other(screen,neutral);try{await End(third);}finally{await RemoveOther(third,screen);}
            var target=await Other(screen,b);
            try
            {
                await End(target);var report=(OpenAntares.Simulation.Knowledge.EmpireTurnReportView)typeof(GameScreen).GetField("_latestReport",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(target)!;
                Check(report.Colonies.Single().Economy.NetProduction.Total==450,"Actual final human Ready applies450 production");Check(report.Conflict!.OwnPressure.Length==1,"Actual owner report retains its own pressure");
                await Click(target,"ReportOpenBlockadeDiplomacy_"+a.Value);Check(Named<Label>(target,"ConflictRelationText").Text=="War","Blockade report opens the exact permitted opposing contact");
                await Capture(this,Descendants<DiplomacyDialog>(target).Single(),Path.Combine(_output,"blockade-report-contact.png"));
            }
            finally{await RemoveOther(target,screen);}
        }
        finally{RemoveChild(main);main.QueueFree();await Frames(this);}
    }
    private static void HideWindows(Node screen){foreach(var window in Descendants<Window>(screen))window.Hide();}
    private static string Text(Node screen)=>string.Join(" ",Descendants<Label>(screen).Where(l=>l.IsVisibleInTree()).Select(l=>l.Text));
    private async Task End(GameScreen screen){HideWindows(screen);await Click(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));}
    private async Task<GameScreen> Other(GameScreen primary,EmpireId viewer){HideWindows(primary);primary.Hide();var other=new GameScreen(Live(primary),viewer);Paths(other,"other-"+viewer.Value);AddChild(other);await Frames(this);return other;}
    private async Task RemoveOther(GameScreen other,GameScreen primary){HideWindows(other);RemoveChild(other);other.QueueFree();primary.Show();typeof(GameScreen).GetMethod("Refresh",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(primary,null);await Frames(this);}
    private async Task PublishedWithdrawalAndSolo()
    {
        GetTree().Root.Size=new(800,600);var fixture=PoliticalFixture();var state=fixture.State;state.Turn=10;OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(state,10);ConflictObservations.Refresh(fixture.Content,state);var pair=state.Conflict!.Pairs.Single();pair.Relation=RelationKind.War;pair.WarTurn=5;pair.Declarers.Add(pair.Pair.Low);pair.Parties[1].DeclarationGrievance=true;pair.Parties[1].LastCounterpartDeclarationTurn=5;pair.PendingPeace=new(){Id=new(1),Proposer=pair.Pair.Low,CreatedTurn=10,ExpiryTurn=13,Answer=true};pair.NextPeaceOfferId=2;pair.Revision=3;var errors=GameStateValidator.Validate(fixture.Content,state);Check(errors.Count==0,"Labelled published-offer/queued-answer supplement validates: "+string.Join("; ",errors));var (main,screen)=await Mount(fixture,"published-withdrawal");
        try{var session=Live(screen);await Click(screen,"OpenDiplomacy");Check(!Text(screen).Contains("Your Accept is queued"),"Proposer does not learn pre-session recipient answer");await Click(screen,"ConflictWithdrawPeace");Check(session.State.Conflict!.Pairs.Single().PendingPeace is null&&session.State.Conflict.Pairs.Single().Relation==RelationKind.War,"Actual outgoing Withdraw wins a queued answer without silently accepting peace");screen=await SaveReload(main,screen,"withdrawn-published-peace");Check(Live(screen).State.Conflict!.Pairs.Single().Parties[0].NextPeaceOfferTurn==0,"Human unconditional peace remains available after Main Save/Load");}
        finally{RemoveChild(main);main.QueueFree();await Frames(this);}
        var content=CompleteContent();var solo=GalaxyGenerator.CreateNewGame(content,new("small",42,12,"harl",0,null,CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));var soloScreen=new GameScreen(new(content,solo.State!),solo.State!.Empires[0].Id);Paths(soloScreen,"solo");AddChild(soloScreen);await Frames(this);
        try{await Click(soloScreen,"OpenDiplomacy");Check(Text(soloScreen).Contains("No other civilization")&&!Descendants<Button>(soloScreen).Any(b=>b.Name=="ConflictDeclareWar"),"Solo/unmet view invents no contact or war control");}
        finally{RemoveChild(soloScreen);soloScreen.QueueFree();await Frames(this);}
    }
}
