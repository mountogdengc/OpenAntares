using UiNavigation = OpenAntares.Game.Validation.CombinedUiNavigation;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Reflection;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Diplomacy;
using static OpenAntares.Game.Validation.ScoutUiControlChecks;
namespace OpenAntares.Game.Validation;
public partial class DiplomacyGraphicalValidation
{
    private static GameSession Live(GameScreen screen)=>(GameSession)typeof(GameScreen).GetField("_session",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(screen)!;
    private void Paths(GameScreen screen,string folder){screen.CombatPreferencesPath=Path.Combine(folder,"combat-prefs.json");screen.FleetPreferencesPath=Path.Combine(folder,"fleet-prefs.json");screen.ScoutPreferencesPath=Path.Combine(folder,"scout-prefs.json");screen.CrisisPreferencesPath=Path.Combine(folder,"crisis-prefs.json");screen.NotificationPreferencesPath=Path.Combine(folder,"colony-prefs.json");}
    private async Task PressControl(Button button){button=UiNavigation.Reveal(button);Check(!button.Disabled&&button.IsVisibleInTree(),"Reachable "+button.Text);button.EmitSignal(Button.SignalName.Pressed);await Frames(this);}
    private async Task<GameScreen> SaveReload(Control main,GameScreen screen,string folder,string label)
    {
        string before=Snapshot(Live(screen)),name="qa-diplomacy-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+label;
        foreach(var window in Descendants<Window>(screen))window.Hide();
        UiNavigation.GameMenu(screen,0);await Frames(this);var save=Descendants<SaveGameDialog>(screen).Single();Descendants<LineEdit>(save).Single().Text=name;await PressControl(save.GetOkButton());Check(File.Exists(SaveFiles.PathFor(name)),"Actual Save wrote unique retained QA file");File.AppendAllText(Path.Combine(folder,"saved-files.jsonl"),JsonSerializer.Serialize(new{label,path=SaveFiles.PathFor(name),sha256=Digest(before)})+"\n");
        UiNavigation.GameMenu(screen,1);await Frames(this);var load=Descendants<LoadGameDialog>(main).Single();var list=Descendants<ItemList>(load).Single();int index=Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemText(i).StartsWith(name+"    ",StringComparison.Ordinal));list.Select(index);list.EmitSignal(ItemList.SignalName.ItemSelected,(long)index);await PressControl(load.GetOkButton());screen=Descendants<GameScreen>(main).Single();Paths(screen,folder);Check(Snapshot(Live(screen))==before,"Actual Main Save/Load exactly preserves "+label);Check(!Descendants<TurnReportDialog>(screen).Single().Visible,"Loading a planning save does not replay historical turn reports");return screen;
    }
    private async Task EarnedCampaignUi(string species)
    {
        GetTree().Root.Size=new(1920,1080);var folder=Path.Combine(_output,"earned",species);Directory.CreateDirectory(folder);EarnedCampaign? campaign=null;
        foreach(ulong seed in new ulong[]{42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57}){try{campaign=BuildCampaign(CompleteContent(),species,seed);File.AppendAllText(Path.Combine(folder,"seed-search.jsonl"),JsonSerializer.Serialize(new{seed,passed=true})+"\n");break;}catch(Exception e){File.AppendAllText(Path.Combine(folder,"seed-search.jsonl"),JsonSerializer.Serialize(new{seed,passed=false,error=e.Message})+"\n");}await Frames(this);}
        Check(campaign is not null,"Finite seed search earns complete "+species+" campaign");var plan=campaign!;Control main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames(this);var replays=new List<(string Label,GameSession Session,int Through)>();var verified=new HashSet<string>();
        try
        {
            var setup=Descendants<NewGameScreen>(main).Single();setup.Configure("small",plan.Seed,species,1,"wenth",crisesEnabled:false,amebasEnabled:false,sentriesEnabled:false);await PressControl(Descendants<Button>(setup).Single(b=>b.Text=="Start"));var screen=Descendants<GameScreen>(main).Single();Paths(screen,folder);var session=Live(screen);Check(session.State.RulesVersion==12&&session.State.Turn==0&&session.State.Colonies.Count==2,"Untouched actual Main New Game rules12 "+species);SaveGame.WriteFile(Path.Combine(folder,"start.json"),session.Content,session.State);
            foreach(var step in plan.Steps)
            {
                session=Live(screen);bool accepted;if(step.Command is DiplomacyCommand diplomacy){await ReplayDiplomacyUi(screen,diplomacy);accepted=true;}else accepted=session.Execute(step.Command).Accepted;Check(accepted==step.Accepted&&Digest(Snapshot(session))==step.Hash,"Ordinary command follows deterministic earned tape: "+step.Command.GetType().Name);
                foreach(var branch in replays.ToArray()){var outcome=branch.Session.Execute(step.Command);Check(outcome.Accepted==step.Accepted&&Snapshot(branch.Session)==Snapshot(session),"Exact saved future for "+branch.Label);if(session.State.Turn>=branch.Through){verified.Add(branch.Label);replays.Remove(branch);}}
                File.AppendAllText(Path.Combine(folder,"commands.jsonl"),JsonSerializer.Serialize(new{type=step.Command.GetType().Name,command=JsonSerializer.SerializeToElement(step.Command,step.Command.GetType()),actionKind=step.Command is DiplomacyCommand dc?dc.Action.GetType().Name:null,action=step.Command is DiplomacyCommand dd?JsonSerializer.SerializeToElement(dd.Action,dd.Action.GetType()):(JsonElement?)null,accepted=step.Accepted,step.Hash,step.Stage})+"\n");
                if(step.Stage is {} stage)
                {
                    var uninterrupted=new GameSession(session.Content,session.State.Clone());SaveGame.WriteFile(Path.Combine(folder,stage+".json"),session.Content,session.State);screen=await SaveReload(main,screen,folder,species+"-"+stage);replays.Add((stage,uninterrupted,session.State.Turn+3));
                    if(stage=="warmup")
                    {
                        await Click(screen,"OpenDiplomacy");var dialog=Descendants<DiplomacyDialog>(screen).Single();var text=string.Join(" ",Descendants<Label>(dialog).Select(l=>l.Text));var ownView=Live(screen).View(Live(screen).State.Empires[0].Id);var active=ownView.Diplomacy!.Contacts.Single().Agreements.Single(a=>a.Kind==AgreementKind.Trade);ReviewCheck(text.Contains("Your trade hub: "+ownView.Research.ColonyNames[active.OwnHub!.Value.ToString()]),"Active trade identifies own hub");ReviewCheck(text.Contains("First payout resolves at turn"),"Active trade has first payout date");dialog.Hide();
                        var quantity=OpenAntares.Simulation.Economy.Quantity.From(new OpenAntares.Simulation.Economy.BreakdownLine("diplomacy_trade","trade_agreement",null,100));ReviewCheck(!OpenAntares.Game.BreakdownText.Describe(quantity,Live(screen).Content,"Production").Contains("diplomacy_trade"),"Trade income has a human label");
                    }
                    if(stage=="blocked-founder")
                    {
                        var founder=((SetFoundingOrderCommand)step.Command).Vessel;var picker=Named<OptionButton>(screen,"VesselPicker");int i=Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsInt32()==founder.Value);picker.Select(i);picker.EmitSignal(OptionButton.SignalName.ItemSelected,(long)i);await Frames(this);Check(Descendants<Label>(screen).Any(l=>l.Text.Contains("settlement accord blocks further founding")),"Actual founder Attention preserves blocked cargo");await Click(screen,"FoundingDiplomacy");Check(Descendants<DiplomacyDialog>(screen).Single().Visible,"Founding Attention opens the governing accord");Descendants<DiplomacyDialog>(screen).Single().Hide();
                    }
                    if(stage.Contains("notice")){await Click(screen,"OpenDiplomacy");var dialog=Descendants<DiplomacyDialog>(screen).Single();Check(string.Join(" ",Descendants<Label>(dialog).Select(l=>l.Text)).Contains("ends"),"Notice end date displayed");await Capture(this,dialog,Path.Combine(folder,stage+"-embedded.png"));dialog.Hide();}
                }
                if(session.State.Turn%10==0)await Frames(this);
            }
            session=Live(screen);SaveGame.WriteFile(Path.Combine(folder,"budget-final.json"),session.Content,session.State);for(int n=0;n<3;n++){var command=new SetReadyCommand(session.State.Empires[0].Id,session.State.Turn,true);Check(session.Execute(command).Accepted,"Final checkpoint continuation Ready");foreach(var branch in replays.ToArray()){Check(branch.Session.Execute(command).Accepted&&Snapshot(branch.Session)==Snapshot(session),"Final exact saved future "+branch.Label);if(session.State.Turn>=branch.Through){verified.Add(branch.Label);replays.Remove(branch);}}}
            SaveGame.WriteFile(Path.Combine(folder,"continuation-final.json"),session.Content,session.State);Check(replays.Count==0&&verified.Count>=5,"All diplomatic stages match at least three full future boundaries");Check(plan.PaidSetup==500&&plan.TradePaid>0&&plan.Founded>=2,"Exactly one earned own setup, actual output and two paid founders");File.WriteAllText(Path.Combine(folder,"summary.json"),JsonSerializer.Serialize(new{passed=true,species,seed=plan.Seed,fromTurn=0,budgetThroughTurn=plan.Final.State.Turn,continuationThroughTurn=session.State.Turn,plan.PaidSetup,plan.TradePaid,plan.Founded,plan.BothSetupPaid,plan.EarnedProduction,plan.FinalBank,plan.HullPayments,verifiedStages=verified,method="Untouched actual Main New Game; ordinary simulation commands, actual Main Save/Load controls at every stage. No injected assets, banks, trust, research, or contacts."},new JsonSerializerOptions{WriteIndented=true}));_earnedCampaigns++;
        }
        finally{RemoveChild(main);main.QueueFree();await Frames(this);}
    }

    private async Task ReplayDiplomacyUi(GameScreen screen,DiplomacyCommand expected)
    {
        foreach(var window in Descendants<Window>(screen))window.Hide();typeof(GameScreen).GetMethod("Refresh",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(screen,null);await Frames(this);await Click(screen,"OpenDiplomacy");
        string button=expected.Action switch{DiplomacyAction.OfferTrade=>"DiplomacyOfferTrade",DiplomacyAction.OfferSettlement=>"DiplomacyOfferSettlement",DiplomacyAction.Answer{Accept:true}=>"DiplomacyAccept",DiplomacyAction.Answer=>"DiplomacyDecline",DiplomacyAction.Terminate t=>"DiplomacyTerminate_"+t.Kind,_=>throw new InvalidOperationException("Unsupported campaign UI action")};await Click(screen,button);
        var offer=Descendants<DiplomacyOfferDialog>(screen).Single();int? target=expected.Action switch{DiplomacyAction.OfferTrade t=>t.Hub.Value,DiplomacyAction.OfferSettlement a=>a.Star.Value,DiplomacyAction.Answer a=>a.Hub?.Value,_=>null};
        if(target is {} id){var picker=Named<OptionButton>(offer,"DiplomacyTarget");int i=Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsInt32()==id);picker.Select(i);picker.EmitSignal(OptionButton.SignalName.ItemSelected,(long)i);await Frames(this);}
        var consent=Named<CheckBox>(offer,"DiplomacyConsent");if(consent.Visible){Check(Named<Button>(offer,"DiplomacySubmit").Disabled,"Disclosure or notice requires confirmation");if(expected.Action is DiplomacyAction.Terminate)Check(Named<Label>(offer,"DiplomacyTerms").Text.Contains("Cancellation memory"),"Cancellation warning before actual notice");consent.ButtonPressed=true;consent.EmitSignal(BaseButton.SignalName.Toggled,true);}
        Command? actual=null;offer.CommandIssued+=c=>actual=c;await Click(offer,"DiplomacySubmit");Check(expected==actual,"Actual diplomacy control sends the exact reviewed own terms");foreach(var window in Descendants<Window>(screen))window.Hide();
    }
}
