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
using OpenAntares.Simulation.ShipDesigns;
namespace OpenAntares.Game.Validation;
public partial class ShipDesignGraphicalValidation
{
    private string? _campaignLabel;private string _campaignFolder="";private readonly List<object> _commandTrace=new(),_campaignSaves=new();private readonly List<(string Label,GameSession Session,int Through)> _replayBranches=new();private readonly List<string> _verifiedStages=new();private int _generatedCases;private long _earnedProduction,_earnedResearch;private ColonyId _campaignHome;
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
    private async Task Load(Control main,string name)
    {
        var dialog=Descendants<LoadGameDialog>(main).Single();var list=Descendants<ItemList>(dialog).Single();await Select(list,Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemText(i).StartsWith(name+"    ",StringComparison.Ordinal)));await Click(dialog.GetOkButton());await Frames(this);
    }
    private async Task<GameScreen> SaveReload(Control main,GameScreen screen,string label)
    {
        string before=Snapshot(Live(screen)),name="qa-ship-design-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+label;await Click(Descendants<Button>(screen).Single(b=>b.Text=="Save"));var dialog=Descendants<SaveGameDialog>(screen).Single();Descendants<LineEdit>(dialog).Single().Text=name;await Click(dialog.GetOkButton());Check(File.Exists(SaveFiles.PathFor(name)),"Actual combat Save dialog wrote unique retained QA save");await Click(Descendants<Button>(screen).Single(b=>b.Text=="Load"));await Load(main,name);screen=Descendants<GameScreen>(main).Single();Paths(screen,label);Check(before==Snapshot(Live(screen)),"Actual Main Save/Load preserves pending combat, damage, consent and all streams");_campaignSaves.Add(new{source="Actual ordinary Main Save/Load controls",file=SaveFiles.PathFor(name)});return screen;
    }
    private async Task<AcceptDialog> Combat(GameScreen screen)
    {
        await Click(screen,"OpenFleetCommand");var fleet=Descendants<FleetCommandDialog>(screen).Single();await Tab(fleet,1);await Select(Named<ItemList>(fleet,"FleetGroupList"),0);await Click(fleet,"FleetCombatOrders");return Named<AcceptDialog>(screen,"CombatOrdersDialog");
    }
    private async Task End(GameScreen screen)
    {
        var session=Live(screen);int before=session.State.Turn;var prior=session.State;await Click(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));RecordUi(screen,prior,new SetReadyCommand(prior.Empires.First(e=>e.Controller==ControllerKind.Human).Id,before,true),"End Turn");Check(session.State.Turn==before+1,"Actual End Turn resolves one automatic boundary");Check(Descendants<TurnReportDialog>(screen).Single().Visible,"Actual combat turn report is visible");
    }
    private static string Hash(string text)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    private void RecordUi(GameScreen screen,GameState before,Command command,string intent)
    {
        if(_campaignLabel is null)return;var actual=Live(screen);var probe=CommandProcessor.Execute(actual.Content,before,command);Check(probe.Accepted&&SaveGame.Serialize(actual.Content,probe.State)==Snapshot(actual),"Actual generated UI command matches ordinary command: "+intent);
        if(probe.Report is {} report){_earnedProduction=checked(_earnedProduction+report.Economy.Colony(_campaignHome).NetProduction.Total);_earnedResearch=checked(_earnedResearch+report.Economy.Empire(before.Empires[0].Id).NetResearch.Total);}
        object payload=command switch
        {
            FleetCommand f=>new{kind=command.GetType().Name,f.Issuer,f.ExpectedTurn,actionKind=f.Action.GetType().Name,action=JsonSerializer.SerializeToElement(f.Action,f.Action.GetType()),f.Fingerprint},
            CombatActionCommand c=>new{kind=command.GetType().Name,c.Issuer,c.ExpectedTurn,actionKind=c.Action.GetType().Name,action=JsonSerializer.SerializeToElement(c.Action,c.Action.GetType()),c.PreviewToken},
            AssemblyCommand a=>new{kind=command.GetType().Name,a.Issuer,a.ExpectedTurn,actionKind=a.Action.GetType().Name,action=JsonSerializer.SerializeToElement(a.Action,a.Action.GetType())},
            ShipDesignCommand d=>new{kind=command.GetType().Name,d.Issuer,d.ExpectedTurn,d.CatalogueRevision,actionKind=d.Action.GetType().Name,action=JsonSerializer.SerializeToElement(d.Action,d.Action.GetType())},
            RefitCommand r=>new{kind=command.GetType().Name,r.Issuer,r.ExpectedTurn,actionKind=r.Action.GetType().Name,action=JsonSerializer.SerializeToElement(r.Action,r.Action.GetType()),r.Token},
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

    private async Task CampaignProductionWorker(GameScreen screen,bool increase)
    {
        var session=Live(screen);var before=session.State;var colony=before.FindColony(_campaignHome)!;var w=colony.Workforce;var next=increase?w with{Production=w.Production+1,Support=w.Support-1}:w with{Production=w.Production-1,Support=w.Support+1};string tip=increase?"Move a worker from support to production":"Move a worker from production to support";await Click(Descendants<Button>(screen).Single(b=>b.TooltipText==tip));RecordUi(screen,before,new SetWorkforceCommand(before.Empires[0].Id,before.Turn,_campaignHome,next),"Manual workforce: "+tip);
    }
    private async Task CampaignResearch(GameScreen screen,string id)
    {
        var session=Live(screen);var before=session.State;var picker=Descendants<OptionButton>(Descendants<ResearchBar>(screen).Single()).Single();await PickKey(picker,id);RecordUi(screen,before,new SetResearchTargetCommand(before.Empires[0].Id,before.Turn,id),"Select earned research "+id);
        for(int n=0;n<1000&&!session.State.Empires[0].KnownTechnologyIds.Contains(id);n++)await StepGenerated(screen);
        Check(session.State.Empires[0].KnownTechnologyIds.Contains(id),"Ordinary paid research unlocks "+id);
    }
    private async Task CampaignDesign(GameScreen screen,string name,bool fast)
    {
        await Click(screen,"OpenFleetCommand");var fleet=Descendants<FleetCommandDialog>(screen).Single();await Click(fleet,"OpenShipDesigns");var catalogue=Descendants<ShipDesignDialog>(screen).Single();await Click(catalogue,"CopyDesign");var editor=Descendants<ShipDesignEditor>(screen).Single();SetDesignName(editor,name);
        if(fast)await Pick(editor,"DesignEngine","vector_drive");else{await Pick(editor,"DesignArmor","");await Pick(editor,"DesignBank2","pulse_bank");}
        var session=Live(screen);var before=session.State;ShipRecipe? recipe=null;editor.Saved+=(n,r)=>recipe=r;await Click(editor,"SaveDesign");Check(recipe is not null,"Actual designer emits immutable recipe "+name);RecordUi(screen,before,new ShipDesignCommand(before.Empires[0].Id,before.Turn,before.Empires[0].ShipDesigns!.CatalogueRevision,new ShipDesignAction.Save(name,recipe!)),"Save explicit design "+name);await Close(catalogue);await Close(fleet);
    }
    private async Task CampaignQueue(GameScreen screen)
    {
        await Click(Descendants<Button>(screen).Single(b=>b.Text=="Edit queue"));var dialog=Descendants<ConstructionQueueEditor>(screen).Single();await Pick(dialog,"QueueTargetPicker","ship_design|2");await Click(dialog,"AddQueueEntry");var session=Live(screen);var before=session.State;Command? issued=null;dialog.CommandIssued+=c=>issued=c;await Click(dialog.GetOkButton());Check(issued is not null,"Finite queue Save emits ordinary command");RecordUi(screen,before,issued!,"Save finite advanced-design queue");var toggle=Named<CheckBox>(screen,"ConstructionGovernorToggle");before=session.State;Command? toggled=null;Descendants<ColonyOrdersPanel>(screen).Single().CommandIssued+=c=>toggled=c;toggle.SetPressedNoSignal(true);toggle.EmitSignal(BaseButton.SignalName.Toggled,true);await Frames(this);Check(toggled is not null,"Explicit queue automation emits ordinary command");RecordUi(screen,before,toggled!,"Enable explicitly ordered finite queue");
    }
    private async Task CampaignRequest(GameScreen screen)
    {
        await Click(screen,"OpenFleetCommand");var fleet=Descendants<FleetCommandDialog>(screen).Single();await Click(fleet,"FleetNewRequest");var dialog=Descendants<AssemblyRequestDialog>(screen).Single();await Click(dialog,"RequestClearRoles");await Pick(dialog,"RequestRole","ship_design|2");Named<SpinBox>(dialog,"RequestRoleCount").Value=2;await Click(dialog,"RequestAddRole");var idle=Named<ItemList>(dialog,"RequestIdleShips");Check(idle.ItemCount==1,"Exactly one previously paid advanced hull is offered");await Select(idle,0);await Click(dialog,"RequestPreview");Check(!Named<Button>(dialog,"RequestAccept").Disabled,"Actual finite request preview permits exact advanced recipe");await Click(dialog,"RequestAccept");var session=Live(screen);var before=session.State;Command? issued=null;dialog.CommandIssued+=c=>issued=c;await Click(Named<ConfirmationDialog>(dialog,"RequestConfirmation").GetOkButton());Check(issued is not null,"Request confirmation emits ordinary command");RecordUi(screen,before,issued!,"Accept two exact advanced hulls, one paid existing / one new");await Close(dialog);await Close(fleet);
    }
    private async Task CampaignRefit(GameScreen screen,VesselId vessel)
    {
        await PickId(Named<OptionButton>(screen,"VesselPicker"),vessel.Value);await Click(screen,"VesselOpenRefit");var dialog=Descendants<RefitDialog>(screen).Single();await PickId(Named<OptionButton>(dialog,"RefitTarget"),3);await Click(dialog,"RefitPreview");Check(!Named<Button>(dialog,"RefitApply").Disabled,"Normally repaired earned hull can refit to Gun");var session=Live(screen);var before=session.State;RefitCommand? issued=null;dialog.ActionRequested+=(a,t)=>issued=new(before.Empires[0].Id,before.Turn,a,t);await Click(dialog,"RefitApply");Check(issued is not null,"Explicit refit Apply emits token-bound command");RecordUi(screen,before,issued!,"Start paid same-frame Gun refit");await Close(dialog);
    }
    private async Task GenuineCampaign(string species)
    {
        GetTree().Root.Size=new(1920,1080);_campaignLabel=species+"-small";_campaignFolder=Path.Combine(_output,"generated11",_campaignLabel);Directory.CreateDirectory(_campaignFolder);_commandTrace.Clear();_replayBranches.Clear();_verifiedStages.Clear();_campaignSaves.Clear();_earnedProduction=0;_earnedResearch=0;Control? main=null;string? error=null,sourceHash=null;long initialReserve=0,initialResearch=0;GD.Print("Genuine ship-design campaign start: "+_campaignLabel);
        try
        {
            main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames(this);var setup=Descendants<NewGameScreen>(main).Single();setup.Configure("small",42,species,0,crisesEnabled:false,amebasEnabled:false,sentriesEnabled:true);await Click(Descendants<Button>(setup).Single(b=>b.Text=="Start"));var screen=Descendants<GameScreen>(main).Single();Paths(screen,_campaignLabel);var session=Live(screen);Check(session.State.RulesVersion==11&&session.State.Turn==0&&session.State.Vessels.Count==1&&session.State.Empires[0].ShipDesigns!.Designs.Count==1,"Untouched actual Main New Game starts rules11 with no manufactured equipment");sourceHash=Hash(Snapshot(session));SaveGame.WriteFile(Path.Combine(_campaignFolder,"start.json"),session.Content,session.State);_campaignHome=session.State.Colonies.Single().Id;initialReserve=session.State.Colonies[0].ProductionReserve;initialResearch=session.State.Empires[0].ResearchReserve;
            int initialProduction=session.State.Colonies[0].Workforce.Production;for(int n=0;n<initialProduction;n++)await CampaignProductionWorker(screen,false);
            await CampaignResearch(screen,"measurement_methods");await CampaignResearch(screen,"industrial_coordination");await CampaignResearch(screen,"vector_drives");Check(initialResearch+_earnedResearch==session.State.Empires[0].ResearchReserve+7200,"Research ledger pays1200+2400+3600 through ordinary output");for(int n=0;n<initialProduction;n++)await CampaignProductionWorker(screen,true);await CampaignDesign(screen,"Earned Fast Escort",true);screen=await Checkpoint(main,screen,"catalogue");session=Live(screen);await CampaignQueue(screen);await StepGenerated(screen);Check(session.State.Colonies[0].Development!.ActiveQueueToken is not null&&session.State.Colonies[0].ProjectKind=="ship_design","Genuine checkpoint has an actual unfinished active queue entry");screen=await Checkpoint(main,screen,"active-queue");session=Live(screen);
            for(int n=0;n<1000&&session.State.Vessels.Count(v=>v.Design?.Value==2)<1;n++)await StepGenerated(screen);Check(session.State.Vessels.Count(v=>v.Design?.Value==2)==1,"Finite queue earns exactly one advanced Fast Escort");await CampaignRequest(screen);screen=await Checkpoint(main,screen,"live-request");session=Live(screen);
            for(int n=0;n<1000&&session.State.Empires[0].Fleets!.Requests.Single().Status!=RequestStatus.Complete;n++)await StepGenerated(screen);Check(session.State.Empires[0].Fleets!.Requests.Single().Status==RequestStatus.Complete,"Finite exact-design request earns second advanced hull");var paid=session.State.Vessels.Where(v=>v.Design?.Value==2).Select(v=>v.Id).OrderBy(id=>id).ToImmutableArray();Check(paid.Length==2&&session.State.Empires[0].Fleets!.Requests.Single().Slots.All(slot=>slot.Design?.Value==2),"Both real paid hulls retain exact advanced request identity");Check(initialReserve+_earnedProduction==session.State.Colonies[0].ProductionReserve+5600,"Production ledger pays two exact2800 advanced recipes");
            var owner=session.State.Empires[0].Id;var homeStar=session.State.FindPlanet(session.State.Colonies[0].PlanetId)!.StarId;var scout=session.State.Vessels.Single(v=>v.Design is null).Id;
            for(int n=0;n<1000&&session.View(owner).Combat!.Pockets.IsEmpty;n++)
            {
                var view=session.View(owner);var explorer=view.Vessels.Single(v=>v.Id==scout);if(explorer.Transit is null&&explorer.PendingMove is null){var home=view.Galaxy.Stars.Single(st=>st.Id==homeStar);Int128 Distance(ChartedStar st){Int128 x=(Int128)st.X-home.X,y=(Int128)st.Y-home.Y;return x*x+y*y;}var target=view.Stars.Where(st=>st.Planets is null&&st.Chart.Id!=explorer.StationaryStarId).OrderByDescending(st=>Distance(st.Chart)).ThenBy(st=>st.Chart.Id).First();await ScoutTo(screen,scout,target.Chart.Id);}await StepGenerated(screen);
            }
            var pocket=session.View(owner).Combat!.Pockets.Single();Check(pocket.Current&&!pocket.Cleared,"Ordinary public-chart scouting discovers real generated sentries");var fleetId=session.State.Empires[0].Fleets!.Fleets.Single().Id;await FleetActionUi(screen,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(fleetId),pocket.Star),"FleetPreviewGroupMove");Check(paid.All(id=>OpenAntares.Simulation.ShipDesigns.VesselLoadouts.Resolve(session.Content,session.State,session.State.FindVessel(id)!).BaseSpeed==400),"Earned advanced equipment drives the first400-speed trip");
            for(int n=0;n<1000&&paid.Any(id=>session.State.FindVessel(id)!.StationaryStarId!=pocket.Star);n++)await StepGenerated(screen);await AttackUi(screen,_campaignHome);await StepGenerated(screen);Check(session.State.Combat!.Encounters.Single().Round==1&&paid.Any(id=>session.State.FindVessel(id)!.Combat!.Hull<100),"Real earned advanced force fights and takes ordinary first-volley damage");await WithdrawUi(screen);await StepGenerated(screen);
            for(int n=0;n<1000&&paid.Any(id=>session.State.FindVessel(id)!.Transit is not null||session.State.FindVessel(id)!.Combat!.Repairing);n++)await StepGenerated(screen);Check(paid.All(id=>session.State.FindVessel(id)!.Combat!.Hull==100&&session.State.FindVessel(id)!.Combat!.Cohesion==100),"Ordinary return and time-only repair restore full condition");await CampaignDesign(screen,"Refitted Gun Escort",false);
            await CampaignRefit(screen,paid[0]);screen=await Checkpoint(main,screen,"active-refit");session=Live(screen);await StepGenerated(screen);Check(session.State.FindVessel(paid[0])!.Design?.Value==3,"First paid refit installs only at a full boundary");await CampaignRefit(screen,paid[1]);await StepGenerated(screen);Check(paid.All(id=>session.State.FindVessel(id)!.Design?.Value==3),"Both surviving IDs receive paid Gun equipment");Check(initialReserve+_earnedProduction==session.State.Colonies[0].ProductionReserve+7000,"Exact campaign ledger pays5600 new construction plus1400 refits once");await FleetActionUi(screen,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(fleetId),pocket.Star),"FleetPreviewGroupMove");Check(paid.All(id=>VesselLoadouts.Resolve(session.Content,session.State,session.State.FindVessel(id)!).BaseSpeed==300),"Subsequent committed trip uses changed300-speed Gun profile");
            for(int n=0;n<1000&&paid.Any(id=>session.State.FindVessel(id)!.StationaryStarId!=pocket.Star);n++)await StepGenerated(screen);await AttackUi(screen,_campaignHome);await StepGenerated(screen);var receipt=session.State.Combat!.Encounters.SingleOrDefault()?.Rounds.Last()??session.State.Empires[0].Combat!.History.Last().Details.Last();Check(receipt.Before.Ships.All(ship=>ship.Attack==48),"Later actual volley uses refitted48-attack copied profile");
            for(int n=0;n<10&&_replayBranches.Count>0;n++)await StepGenerated(screen);Check(new[]{"catalogue","active-queue","live-request","active-refit"}.All(_verifiedStages.Contains),"Every genuine saved stage matches three legal uninterrupted future turns");SaveGame.WriteFile(Path.Combine(_campaignFolder,"after-refit-volley.json"),session.Content,session.State);Check(initialReserve+_earnedProduction==session.State.Colonies[0].ProductionReserve+7000,"Final genuine production ledger balances without outcome injection");_generatedCases++;GD.Print("Genuine ship-design campaign complete: "+_campaignLabel+" turn "+session.State.Turn);
        }
        catch(Exception e){error=e.ToString();throw;}
        finally{File.WriteAllText(Path.Combine(_campaignFolder,"commands-and-proof.json"),JsonSerializer.Serialize(new{source="Untouched actual Main New Game seed42 Small Solo, crisesOff/amebaOff/sentriesStandard; ordinary controls only",species,seed=42,sourceHash,error,initialReserve,initialResearch,earnedProduction=_earnedProduction,earnedResearch=_earnedResearch,liveOutcomeInjections=0,verifiedStages=_verifiedStages.ToArray(),mainSaves=_campaignSaves.ToArray(),commands=_commandTrace.ToArray()},new JsonSerializerOptions{WriteIndented=true}));_campaignLabel=null;_replayBranches.Clear();if(main is not null){RemoveChild(main);main.QueueFree();await Frames(this);}}
    }

    private async Task PausedMainCheckpoint()
    {
        var prepared=OrderBoard();SavePreparedDesign(prepared,"Paused Fast",prepared.Content.ShipDesigns!.StartingRecipe with{EngineId="vector_drive"});var owner=prepared.State.Empires[0].Id;var action=new RefitAction.Start(prepared.State.Vessels[0].Id,prepared.State.Colonies[0].Id,new(2));var preview=RefitQueries.Preview(prepared.Content,prepared.View(owner),action);Check(prepared.Execute(new RefitCommand(owner,0,action,preview.Token)).Accepted,"Public-command-prepared paused checkpoint source");var job=prepared.State.Empires[0].ShipDesigns!.Refits.Single();job.Status=RefitStatus.Paused;job.Reason="Labelled pre-session paused case; no natural earned-campaign claim";job.Revision++;
        _campaignLabel="labelled-paused";_campaignFolder=Path.Combine(_output,"labelled-paused");Directory.CreateDirectory(_campaignFolder);_campaignHome=prepared.State.Colonies[0].Id;_commandTrace.Clear();_campaignSaves.Clear();_replayBranches.Clear();_verifiedStages.Clear();_earnedProduction=0;_earnedResearch=0;
        string name="qa-ship-design-paused-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff");SaveGame.WriteFile(SaveFiles.PathFor(name),prepared.Content,prepared.State);var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames(this);
        try{var setup=Descendants<NewGameScreen>(main).Single();await Click(Descendants<Button>(setup).Single(b=>b.Text=="Load Game"));await Load(main,name);var screen=Descendants<GameScreen>(main).Single();Paths(screen,"labelled-paused");screen=await Checkpoint(main,screen,"paused-refit");var session=Live(screen);await Click(screen,"VesselOpenRefit");var dialog=Descendants<RefitDialog>(screen).Single();await Click(dialog,"RefitResume");var before=session.State;RefitCommand? issued=null;dialog.ActionRequested+=(a,t)=>issued=new(owner,before.Turn,a,t);await Click(dialog,"RefitApply");Check(issued is not null,"Paused Main checkpoint resumes through actual Apply");RecordUi(screen,before,issued!,"Resume labelled paused job");await Close(dialog);for(int n=0;n<3;n++)await StepGenerated(screen);Check(_verifiedStages.Contains("paused-refit"),"Paused actual Main checkpoint matches3 uninterrupted legal future turns");}
        finally{File.WriteAllText(Path.Combine(_campaignFolder,"commands-and-proof.json"),JsonSerializer.Serialize(new{source="Explicit labelled pre-session researched/reserve/equipment/paused supplement; separate from four genuine campaigns",liveOutcomeInjections=0,verifiedStages=_verifiedStages,mainSaves=_campaignSaves,commands=_commandTrace},new JsonSerializerOptions{WriteIndented=true}));_campaignLabel=null;_replayBranches.Clear();RemoveChild(main);main.QueueFree();await Frames(this);}
    }

}
