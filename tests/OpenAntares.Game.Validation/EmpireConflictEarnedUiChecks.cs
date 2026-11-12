using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using static OpenAntares.Game.Validation.ScoutUiControlChecks;
namespace OpenAntares.Game.Validation;
public partial class EmpireConflictGraphicalValidation
{
    private sealed record NativeFuture(string Stage,GameSession Uninterrupted,int Through,List<object> Hashes,List<object> PendingCommands,string MainHash,string Folder);
    private static string RepoPath(string path)=>Path.GetRelativePath(ProjectSettings.GlobalizePath("res://"),path).Replace('\\','/');
    private static object Artifact(string path)=>new{path=RepoPath(path),sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()};
    private static Command TapeCommand(JsonElement step)=>EmpireConflictCampaignChecks.ReadJournalCommand(step);
    private async Task NativeConflictCommand(GameScreen screen,ConflictCommand expected)
    {
        HideWindows(screen);typeof(GameScreen).GetMethod("Refresh",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(screen,null);await Frames(this);
        if(expected.Action is ConflictAction.DeclareWar)
        {await Click(screen,"OpenDiplomacy");await Click(screen,"ConflictDeclareWar");await Click(Named<ConflictDeclarationDialog>(screen,"ConflictDeclarationDialog"),"ConflictDeclarationApply");return;}
        string? politics=expected.Action switch{ConflictAction.ProposePeace=>"ConflictProposePeace",ConflictAction.AnswerPeace{Accept:true}=>"ConflictAcceptPeace",ConflictAction.AnswerPeace=>"ConflictDeclinePeace",ConflictAction.WithdrawPeace=>"ConflictWithdrawPeace",ConflictAction.RevokePeaceAnswer=>"ConflictRevokePeaceAnswer",ConflictAction.RevokeDeclaration=>"ConflictRevokeDeclaration",_=>null};
        if(politics is not null){await Click(screen,"OpenDiplomacy");await Click(screen,politics);return;}
        var view=Live(screen).View(expected.Issuer);var own=view.Conflict!;VesselId? anchor=expected.Action switch
        {
            ConflictAction.ConfigureDefense a=>a.Anchor,
            ConflictAction.Attack a=>own.Ships.First(s=>s.Fleet==a.Fleet).Id,
            ConflictAction.Blockade a=>own.Ships.First(s=>s.Fleet==a.Fleet).Id,
            ConflictAction.LiftBlockade a=>own.Ships.First(s=>s.Fleet==a.Fleet).Id,
            ConflictAction.CancelAttack a=>own.Ships.First(s=>s.Fleet==a.Fleet).Id,
            ConflictAction.ChangeOrders a=>own.Encounters.Single(e=>e.Id==a.Encounter).OwnMembers[0],
            ConflictAction.Withdraw a=>own.Encounters.Single(e=>e.Id==a.Encounter).OwnMembers[0],
            ConflictAction.Respond a=>own.Encounters.Single(e=>e.Id==a.Encounter).OwnMembers[0],_=>null
        };
        await Click(screen,"DecisionsConflictOrders");var dialog=Named<ConflictOrdersDialog>(screen,"ConflictOrdersDialog");var force=Named<OptionButton>(dialog,"ConflictOwnForce");var selected=own.Ships.Single(s=>s.Id==anchor);int index=Enumerable.Range(0,force.ItemCount).Single(i=>{var id=new VesselId(force.GetItemMetadata(i).AsInt32());var ship=own.Ships.Single(s=>s.Id==id);return id==anchor||selected.Fleet is {} fleet&&ship.Fleet==fleet;});await SetPicker(dialog,"ConflictOwnForce",index);
        var orders=expected.Action switch{ConflictAction.Attack a=>a.Orders,ConflictAction.ConfigureDefense a=>a.Orders,ConflictAction.ChangeOrders a=>a.Orders,_=>null};
        if(orders is not null)
        {
            await SetPicker(dialog,"ConflictDoctrine",(int)orders.Doctrine);await SetPicker(dialog,"ConflictStragglers",(int)orders.Stragglers);await SetPicker(dialog,"ConflictArrival",(int)orders.OnArrival);
            var ports=Named<OptionButton>(dialog,"ConflictReturnPort");int port=Enumerable.Range(0,ports.ItemCount).Single(i=>ports.GetItemMetadata(i).AsInt32()==(orders.ReturnColony?.Value??0));await SetPicker(dialog,"ConflictReturnPort",port);
        }
        if(expected.Action is ConflictAction.Attack attack){var targets=Named<OptionButton>(dialog,"ConflictArmedTarget");await SetPicker(dialog,"ConflictArmedTarget",Enumerable.Range(0,targets.ItemCount).Single(i=>targets.GetItemMetadata(i).AsInt32()==attack.Target.Value));}
        string review=expected.Action switch{ConflictAction.Attack=>"ConflictReviewAttack",ConflictAction.ConfigureDefense=>"ConflictReviewDefense",ConflictAction.ChangeOrders=>"ConflictReviewChange",ConflictAction.Withdraw=>"ConflictReviewWithdraw",ConflictAction.Respond=>"ConflictReviewRespond",ConflictAction.Blockade=>"ConflictReviewBlockade",ConflictAction.LiftBlockade=>"ConflictReviewLift",ConflictAction.CancelAttack=>"ConflictCancelAttack",_=>throw new InvalidOperationException("Unsupported conflict UI tape action")};
        await Click(dialog,review);await Click(dialog,"ConflictOrdersApply");
    }
    private async Task NativeDiplomacyCommand(GameScreen screen,DiplomacyCommand expected)
    {
        HideWindows(screen);typeof(GameScreen).GetMethod("Refresh",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(screen,null);await Frames(this);await Click(screen,"OpenDiplomacy");
        string button=expected.Action switch{DiplomacyAction.OfferSettlement=>"DiplomacyOfferSettlement",DiplomacyAction.OfferTrade=>"DiplomacyOfferTrade",DiplomacyAction.Answer{Accept:true}=>"DiplomacyAccept",DiplomacyAction.Answer=>"DiplomacyDecline",_=>throw new InvalidOperationException("Unsupported cooperation continuation")};await Click(screen,button);var offer=Descendants<DiplomacyOfferDialog>(screen).Single();
        int? target=expected.Action switch{DiplomacyAction.OfferSettlement a=>a.Star.Value,DiplomacyAction.OfferTrade a=>a.Hub.Value,DiplomacyAction.Answer a=>a.Hub?.Value,_=>null};
        if(target is {} id){var picker=Named<OptionButton>(offer,"DiplomacyTarget");picker.Select(Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsInt32()==id));picker.EmitSignal(OptionButton.SignalName.ItemSelected,(long)picker.Selected);await Frames(this);}
        var consent=Named<CheckBox>(offer,"DiplomacyConsent");if(consent.Visible){consent.ButtonPressed=true;consent.EmitSignal(BaseButton.SignalName.Toggled,true);await Frames(this);}await Click(offer,"DiplomacySubmit");
    }
    private async Task NativeTapeStep(GameScreen screen,Command command,bool accepted)
    {
        if(accepted&&command is ConflictCommand conflict)await NativeConflictCommand(screen,conflict);
        else if(accepted&&command is DiplomacyCommand diplomacy)await NativeDiplomacyCommand(screen,diplomacy);
        else if(accepted&&command is SetReadyCommand{Ready:true})await End(screen);
        else{Check(Live(screen).Execute(command).Accepted==accepted,"Ordinary paid/preparation or negative command: "+command.GetType().Name);typeof(GameScreen).GetMethod("Refresh",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(screen,null);await Frames(this);}
    }
    private async Task EarnedMainCampaign(string species)
    {
        GetTree().Root.Size=new(1920,1080);string folder=Path.Combine(_output,"earned",species);Directory.CreateDirectory(folder);ulong? chosen=null;
        foreach(ulong seed in Enumerable.Range(42,8).Select(n=>(ulong)n))
        {string attempt=Path.Combine(folder,"engine-candidate-"+seed);Check(!Directory.Exists(attempt),"Fresh bounded candidate directory");bool passed=EmpireConflictCampaignChecks.Run(CompleteContent(),species,seed,attempt);File.AppendAllText(Path.Combine(folder,"seed-search.jsonl"),JsonSerializer.Serialize(new{seed,passed,policy="paid-relief-and-legal-embarkation",turnBudget=160})+"\n");await Frames(this);if(passed){chosen=seed;break;}}
        Check(chosen is not null,"Finite engine rehearsal search earns all required stages for "+species);string candidate=Path.Combine(folder,"engine-candidate-"+chosen);using var tape=JsonDocument.Parse(File.ReadAllText(Path.Combine(candidate,"journal.json")));
        var stageHashes=EmpireConflictCampaignChecks.RequiredStages.ToDictionary(stage=>stage,stage=>EmpireConflictCampaignChecks.Hash(File.ReadAllText(Path.Combine(candidate,stage+".engine-save.json"))));
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames(this);var branches=new List<NativeFuture>();var captured=new Dictionary<string,object>();var verified=new HashSet<string>();
        try
        {
            var setup=Descendants<NewGameScreen>(main).Single();setup.Configure("small",chosen!.Value,species,1,"wenth",crisesEnabled:false,amebasEnabled:false,sentriesEnabled:false);await Click(Descendants<Button>(setup).Single(b=>b.Text=="Start"));var screen=Descendants<GameScreen>(main).Single();Paths(screen,"earned-"+species);var owner=Live(screen).State.Empires[0].Id;
            Check(Live(screen).State.RulesVersion==12,"Earned Main campaign requires separately gated rules12 new-game activation");Check(Snapshot(Live(screen))==File.ReadAllText(Path.Combine(candidate,"start.engine-save.json")),"Untouched actual Main New Game matches generated source, without injected assets");
            void AdvanceBranches(Command command,bool accepted)
            {
                foreach(var branch in branches.ToArray())
                {
                    branch.PendingCommands.Add(new{command=EmpireConflictCampaignChecks.JournalCommand(command),accepted});Check(branch.Uninterrupted.Execute(command).Accepted==accepted,"Uninterrupted command acceptance matches Main-loaded play");
                    Check(Snapshot(branch.Uninterrupted)==Snapshot(Live(screen))&&JsonSerializer.Serialize(branch.Uninterrupted.State)==JsonSerializer.Serialize(Live(screen).State),"Uninterrupted and Main-loaded state match: "+branch.Stage);
                    if(command is SetReadyCommand&&branch.Uninterrupted.State.Turn>command.ExpectedTurn)
                    {branch.Hashes.Add(new{turn=branch.Uninterrupted.State.Turn,save=EmpireConflictCampaignChecks.Hash(Snapshot(branch.Uninterrupted)),state=EmpireConflictCampaignChecks.Hash(JsonSerializer.Serialize(branch.Uninterrupted.State)),commands=branch.PendingCommands.ToArray()});branch.PendingCommands.Clear();if(branch.Uninterrupted.State.Turn>=branch.Through){verified.Add(branch.Stage);File.WriteAllText(Path.Combine(branch.Folder,"future-"+branch.Stage+".json"),JsonSerializer.Serialize(new{stage=branch.Stage,equalFutureBoundaries=branch.Hashes.Count,hashes=branch.Hashes,mainSaveSha256=branch.MainHash},new JsonSerializerOptions{WriteIndented=true}));branches.Remove(branch);}}
                }
            }
            foreach(var step in tape.RootElement.EnumerateArray())
            {
                var command=TapeCommand(step);bool accepted=step.GetProperty("Accepted").GetBoolean();await NativeTapeStep(screen,command,accepted);Check(EmpireConflictCampaignChecks.Hash(Snapshot(Live(screen)))==step.GetProperty("hash").GetString(),"Actual Main/UI play follows concrete paid command tape");AdvanceBranches(command,accepted);
                foreach(var stage in stageHashes.Where(s=>s.Value==step.GetProperty("hash").GetString()&&!captured.ContainsKey(s.Key)).Select(s=>s.Key).ToArray())
                {
                    var uninterrupted=new GameSession(Live(screen).Content,Live(screen).State.Clone());int through=Live(screen).State.Turn+3;screen=await SaveReload(main,screen,"earned-"+species+"-"+stage);string actual=JsonSerializer.SerializeToElement(_provenance.Last()).GetProperty("file").GetString()!;string copy=Path.Combine(folder,"main-"+stage+".json");File.Copy(actual,copy,false);Check(File.ReadAllText(copy)==Snapshot(Live(screen)),"Retained artifact is the actual Main-written save");string hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(copy))).ToLowerInvariant();branches.Add(new(stage,uninterrupted,through,new(),new(),hash,folder));captured.Add(stage,new{name=stage,mainSave=Artifact(copy),actualMainPath=actual,futureEvidencePath=RepoPath(Path.Combine(folder,"future-"+stage+".json")),equalFutureBoundaries=3});await Frames(this);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using(var image=GetViewport().GetTexture().GetImage())Check(image.SavePng(Path.Combine(folder,stage+".png"))==Error.Ok,"Actual Main stage screenshot retained");
                }
            }
            for(int i=0;i<3;i++){var command=new SetReadyCommand(owner,Live(screen).State.Turn,true);await End(screen);AdvanceBranches(command,true);}
            Check(branches.Count==0&&verified.SetEquals(EmpireConflictCampaignChecks.RequiredStages),"All7 actual Main stages match at least3 uninterrupted full boundaries");
            bool expired=false;for(int i=0;i<40;i++)
            {
                var relation=Live(screen).View(owner).Conflict!.Relations.Single();if(relation.Relation==RelationKind.Peace&&relation.TruceEndTurn<=Live(screen).State.Turn&&relation.OwnGrievance==0&&relation.OtherGrievance==0){expired=true;break;}
                var expected=new GameSession(Live(screen).Content,Live(screen).State.Clone());var commands=new List<(Command Command,bool Accepted)>();EmpireConflictCampaignChecks.Future(expected.Content,expected,owner,(command,accepted)=>commands.Add((command,accepted)));foreach(var command in commands)await NativeTapeStep(screen,command.Command,command.Accepted);Check(Snapshot(expected)==Snapshot(Live(screen)),"Actual continuation agrees through truce/grievance expiry");File.AppendAllText(Path.Combine(folder,"expiry.jsonl"),JsonSerializer.Serialize(new{turn=Live(screen).State.Turn,relation.Relation,relation.TruceEndTurn,relation.OwnGrievance,relation.OtherGrievance,hash=EmpireConflictCampaignChecks.Hash(Snapshot(Live(screen)))})+"\n");
            }
            Check(expired,"Actual Main continuation witnesses complete truce and both grievance expiry");string source=Path.Combine(_output,"source.json");Check(File.Exists(source),"Native run records its compiled source identity");var mainStages=captured.Select(pair=>{var element=JsonSerializer.SerializeToElement(pair.Value);return new{name=pair.Key,mainSave=element.GetProperty("mainSave"),actualMainPath=element.GetProperty("actualMainPath").GetString(),futureEvidence=Artifact(Path.Combine(folder,"future-"+pair.Key+".json")),screenshot=Artifact(Path.Combine(folder,pair.Key+".png")),equalFutureBoundaries=3};}).ToArray();
            File.WriteAllText(Path.Combine(folder,"summary.json"),JsonSerializer.Serialize(new{passed=true,species,seed=chosen,method="Actual Main New Game; real conflict controls and Ready; actual Main Save/Load",injectedAssets=false,mainSaves=true,expiryWitnessed=true,verifiedStages=verified,mainStages,sourceSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(source))).ToLowerInvariant(),paidEngineLedger=JsonDocument.Parse(File.ReadAllText(Path.Combine(candidate,"result.json"))).RootElement},new JsonSerializerOptions{WriteIndented=true}));
        }
        finally{RemoveChild(main);main.QueueFree();await Frames(this);}
    }
}
