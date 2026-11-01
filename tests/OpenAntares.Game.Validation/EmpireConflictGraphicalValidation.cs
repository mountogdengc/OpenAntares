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
        try{_output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output="))[20..];Directory.CreateDirectory(_output);Check(DisplayServer.GetName()!="headless","Actual graphical Godot display");await PublishedWithdrawalAndSolo();await WarPeaceAndOwnPendingAnswersUseRealControls();}
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
    private static void HideWindows(Node screen){foreach(var window in Descendants<Window>(screen))window.Hide();}
    private static string Text(Node screen)=>string.Join(" ",Descendants<Label>(screen).Where(l=>l.IsVisibleInTree()).Select(l=>l.Text));
    private async Task End(GameScreen screen){HideWindows(screen);await Click(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));}
    private async Task<GameScreen> Other(GameScreen primary,EmpireId viewer){HideWindows(primary);primary.Hide();var other=new GameScreen(Live(primary),viewer);Paths(other,"other-"+viewer.Value);AddChild(other);await Frames(this);return other;}
    private async Task RemoveOther(GameScreen other,GameScreen primary){HideWindows(other);RemoveChild(other);other.QueueFree();primary.Show();typeof(GameScreen).GetMethod("Refresh",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(primary,null);await Frames(this);}
    private async Task PublishedWithdrawalAndSolo()
    {
        GetTree().Root.Size=new(800,600);var fixture=PoliticalFixture();var state=fixture.State;state.Turn=10;OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(state,10);ConflictObservations.Refresh(fixture.Content,state);var pair=state.Conflict!.Pairs.Single();pair.Relation=RelationKind.War;pair.WarTurn=5;pair.Declarers.Add(pair.Pair.Low);pair.Parties[1].DeclarationGrievance=true;pair.Parties[1].LastCounterpartDeclarationTurn=5;pair.PendingPeace=new(){Id=new(1),Proposer=pair.Pair.Low,CreatedTurn=10,ExpiryTurn=13,Answer=true};pair.NextPeaceOfferId=2;pair.Revision=3;var errors=GameStateValidator.Validate(fixture.Content,state);Check(errors.Count==0,"Labelled published-offer/queued-answer supplement validates: "+string.Join("; ",errors));var (main,screen)=await Mount(fixture,"published-withdrawal");
        try{var session=Live(screen);await Click(screen,"OpenDiplomacy");Check(!Text(screen).Contains("Your Accept is queued"),"Proposer does not learn pre-session recipient answer");await Click(screen,"ConflictWithdrawPeace");Check(session.State.Conflict!.Pairs.Single().PendingPeace is null&&session.State.Conflict.Pairs.Single().Relation==RelationKind.War,"Actual outgoing Withdraw wins a queued answer without silently accepting peace");screen=await SaveReload(main,screen,"withdrawn-published-peace");Check(Live(screen).State.Conflict!.Pairs.Single().Parties[0].NextPeaceOfferTurn==15,"Outgoing peace cooldown survives Main Save/Load");}
        finally{RemoveChild(main);main.QueueFree();await Frames(this);}
        var content=CompleteContent();var solo=GalaxyGenerator.CreateNewGame(content,new("small",42,12,"harl",0,null,CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));var soloScreen=new GameScreen(new(content,solo.State!),solo.State!.Empires[0].Id);Paths(soloScreen,"solo");AddChild(soloScreen);await Frames(this);
        try{await Click(soloScreen,"OpenDiplomacy");Check(Text(soloScreen).Contains("No other civilization")&&!Descendants<Button>(soloScreen).Any(b=>b.Name=="ConflictDeclareWar"),"Solo/unmet view invents no contact or war control");}
        finally{RemoveChild(soloScreen);soloScreen.QueueFree();await Frames(this);}
    }
}
