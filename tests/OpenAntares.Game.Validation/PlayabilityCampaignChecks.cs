using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Validation;

internal static class PlayabilityCampaignChecks
{
    private sealed record Profile(string Species,ulong Seed,string Mode);
    private sealed record EvidenceFile(string path,string sha256);
    private sealed class Checkpoint
    {
        public int turn {get;init;}
        public EvidenceFile mainSave {get;init;}=null!;
        public string actualMainPath {get;init;}="";
        public bool canonicalEqual {get;init;}
        public EvidenceFile? future {get;set;}
        public EvidenceFile? replay {get;set;}
    }
    private sealed record Branch(GameSession Original,Checkpoint Checkpoint,string MainCopy,string FutureFile,string ReplayFile,List<object> Hashes,List<object> Pending);
    private static readonly JsonSerializerOptions Pretty=new(){WriteIndented=true};
    private static string Snapshot(GameSession game)=>PlayabilityCampaignObserver.Snapshot(game);
    private static string Hash(string text)=>PlayabilityCampaignObserver.Hash(text);
    private static string HashFile(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    private static EvidenceFile Artifact(string path)=>new(Path.GetRelativePath(ProjectSettings.GlobalizePath("res://"),path).Replace('\\','/'),HashFile(path));

    public static async Task Run(Node host,string output,Func<string,ulong,int,Task<GameScreen>> newMain,Func<GameScreen,string,Task<GameScreen>> saveReload,Action<bool,string> check,bool diagnosticWenthSolo=false,bool diagnosticHideConsole=false)
    {
        string source=HashFile(Path.Combine(output,"source.json"));var campaigns=new List<object>();
        void Matrix(bool passed,string? error=null)=>File.WriteAllText(Path.Combine(output,"campaign-matrix.json"),JsonSerializer.Serialize(new{format="OpenAntaresM13CampaignMatrix",version=1,passed,sourceSha256=source,campaigns,error},Pretty));
        Matrix(false);
        try
        {
            var profiles=new[]{new Profile("harl",42,"Standard"),new Profile("harl",42,"Solo"),new Profile("peldra",45,"Standard"),new Profile("peldra",45,"Solo"),new Profile("sulan",43,"Standard"),new Profile("sulan",43,"Solo"),new Profile("wenth",42,"Standard"),new Profile("wenth",42,"Solo")};
            if(diagnosticWenthSolo){profiles=profiles.Where(p=>p.Species=="wenth"&&p.Mode=="Solo").ToArray();GD.Print("Diagnostic single-profile run only; the unchanged eight-profile completion gate must fail");}
            foreach(var profile in profiles)
            {
                host.GetTree().Root.Size=new(1920,1080);string folder=Path.Combine(output,"campaigns",profile.Species+"-"+profile.Mode);Directory.CreateDirectory(folder);
                GD.Print($"M13 actual Main campaign start: {profile.Species}/{profile.Mode}/{profile.Seed}");
                int rivals=profile.Mode=="Solo"?0:1;string? other=rivals==0?null:profile.Species=="peldra"?"harl":"peldra";
                var screen=await newMain(profile.Species,profile.Seed,rivals);var game=Live(screen);
                var generated=GalaxyGenerator.CreateNewGame(game.Content,new("small",profile.Seed,12,profile.Species,rivals,other,true,true,true));
                check(generated.Succeeded&&Snapshot(game)==SaveGame.Serialize(game.Content,generated.State!),"Untouched actual Main start exactly equals fixed generated profile; no injected assets");
                var checkpoints=new List<Checkpoint>();var branches=new List<Branch>();var observations=new List<object>();
                var observer=new PlayabilityCampaignObserver(host,screen,Path.Combine(folder,"journal.json"),check);
                var controls=new PlayabilityCampaignControls(observer,()=>screen);
                observer.Recorded+=(command,accepted)=>
                {
                    if(diagnosticWenthSolo&&command is SetReadyCommand&&observer.Game.State.Turn==7)
                    {
                        File.WriteAllText(Path.Combine(folder,"diagnostic-observed-state-7.json"),Snapshot(observer.Game));
                        var visible=PlayabilityCampaignObserver.Controls<Control>(screen).Where(c=>c.IsVisibleInTree()).Select(c=>new{path=screen.GetPathTo(c).ToString(),type=c.GetType().Name,x=c.Position.X,y=c.Position.Y,width=c.Size.X,height=c.Size.Y,minWidth=c.CustomMinimumSize.X,minHeight=c.CustomMinimumSize.Y,text=c is Label l?l.Text:c is Button b?b.Text:null}).ToArray();
                        File.WriteAllText(Path.Combine(folder,"diagnostic-visible-ui-7.json"),JsonSerializer.Serialize(new{scope="Read-only diagnostic after exact ordinary accepted Ready; not an actual Main checkpoint or earned acceptance",workspace=typeof(GameScreen).GetField("_workspace",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(screen),visible},Pretty));
                        if(diagnosticHideConsole)
                        {
                            ((Control)typeof(GameScreen).GetField("_consolePanel",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(screen)!).Hide();
                            GD.Print("Diagnostic region isolation: console hidden; required report remains visible");
                        }
                        check(PlayabilityCampaignObserver.Controls<OpenAntares.Game.Components.TurnReportDialog>(screen).Single().Visible,"Earned Wenth7 required report remains visible for normal layout");
                    }
                    foreach(var branch in branches.ToArray())
                    {
                        int before=branch.Original.State.Turn;var result=branch.Original.Execute(command);
                        check(result.Accepted==accepted&&Snapshot(branch.Original)==Snapshot(observer.Game)&&JsonSerializer.Serialize(branch.Original.State)==JsonSerializer.Serialize(observer.Game.State),"Actual Main-loaded command equals uninterrupted checkpoint continuation");
                        branch.Pending.Add(new{command=EmpireConflictCampaignChecks.JournalCommand(command),accepted});
                        if(branch.Original.State.Turn==before+1)
                        {
                            branch.Hashes.Add(new{turn=branch.Original.State.Turn,save=Hash(Snapshot(branch.Original)),loadedSave=Hash(Snapshot(observer.Game)),state=Hash(JsonSerializer.Serialize(branch.Original.State)),loadedState=Hash(JsonSerializer.Serialize(observer.Game.State)),commands=branch.Pending.ToArray()});branch.Pending.Clear();
                            if(branch.Hashes.Count==3)
                            {
                                File.WriteAllText(branch.FutureFile,JsonSerializer.Serialize(new{mainSaveSha256=branch.Checkpoint.mainSave.sha256,equalFutureBoundaries=3,hashes=branch.Hashes},Pretty));
                                string method=VerifyReplay(branch.MainCopy,branch.FutureFile);
                                File.WriteAllText(branch.ReplayFile,JsonSerializer.Serialize(new{passed=true,boundaries=3,mainSaveSha256=branch.Checkpoint.mainSave.sha256,futureSha256=HashFile(branch.FutureFile),method},Pretty));
                                branch.Checkpoint.future=Artifact(branch.FutureFile);branch.Checkpoint.replay=Artifact(branch.ReplayFile);branches.Remove(branch);
                            }
                        }
                    }
                };
                async Task Capture(int turn)
                {
                    observer.FlushJournal();var original=new GameSession(observer.Game.Content,observer.Game.State.Clone());
                    string label=$"campaign-{profile.Species}-{profile.Mode}-t{turn}";screen=await saveReload(screen,label);observer.Attach(screen);
                    string copy=Path.Combine(folder,$"main-{turn}.json");File.Copy(Path.Combine(output,label+"-main-save.json"),copy,false);
                    using var provenance=JsonDocument.Parse(File.ReadAllText(Path.Combine(output,label+"-provenance.json")));string actual=provenance.RootElement.GetProperty("actualMainPath").GetString()!;
                    check(File.ReadAllText(copy)==Snapshot(observer.Game)&&Snapshot(original)==Snapshot(observer.Game),"Checkpoint is the actual Main-written save, canonically equal after real Load");
                    var checkpoint=new Checkpoint{turn=turn,mainSave=Artifact(copy),actualMainPath=actual,canonicalEqual=true};checkpoints.Add(checkpoint);
                    branches.Add(new(original,checkpoint,copy,Path.Combine(folder,$"future-{turn}.json"),Path.Combine(folder,$"replay-{turn}.json"),new(),new()));
                    await observer.Frames(4);await host.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    check(host.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(folder,$"checkpoint-{turn}.png"))==Error.Ok,"Actual Main checkpoint frame retained");
                }
                try
                {
                    await Capture(0);
                    for(int boundary=0;boundary<83;boundary++)
                    {
                        int before=observer.Game.State.Turn;
                        await PlanTurn(observer,controls,rivals>0);
                        await controls.Ready();
                        check(observer.Game.State.Turn==before+1,"Fixed audit commits one complete ordinary boundary");
                        var view=observer.Game.View(observer.Owner);
                        if(diagnosticHideConsole&&view.Turn==7)
                        {
                            GD.Print("Diagnostic console-isolated turn7 completed stable frames");
                            throw new InvalidOperationException("Diagnostic stop after region isolation; not complete campaign coverage");
                        }
                        if(profile.Species=="wenth"&&profile.Mode=="Solo"&&view.Turn==7)
                        {
                            var requiredReport=PlayabilityCampaignObserver.Controls<OpenAntares.Game.Components.TurnReportDialog>(screen).Single();
                            string unchanged=Snapshot(observer.Game);
                            foreach(var physical in new[]{new Vector2I(800,600),new Vector2I(1152,648),new Vector2I(1600,900),new Vector2I(1920,1080)})
                            {
                                host.GetTree().Root.Size=physical;requiredReport.PopupCentered(new Vector2I(580,440));await observer.Frames(5);
                                var reportScroll=PlayabilityCampaignObserver.Controls<ScrollContainer>(requiredReport).Single();
                                var wrapped=PlayabilityCampaignObserver.Controls<Label>(requiredReport).Where(l=>l.IsVisibleInTree()&&l.AutowrapMode!=TextServer.AutowrapMode.Off).ToArray();
                                check(requiredReport.Visible&&requiredReport.Size.X<=physical.X&&requiredReport.Size.Y<=physical.Y&&requiredReport.GetOkButton().IsVisibleInTree(),$"{physical}: earned Wenth7 required report and Continue fit the physical viewport");
                                check(wrapped.Length>0&&wrapped.All(l=>l.Size.X>=400&&l.Size.X<=reportScroll.Size.X+1),$"{physical}: nested required-report text has finite viewport-bounded wrapping");
                                check(unchanged==Snapshot(observer.Game),$"{physical}: required-report relayout changes no campaign or PRNG and acknowledges nothing");
                                check(host.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(folder,$"required-report-7-{physical.X}.png"))==Error.Ok,$"{physical}: actual earned required-report frame retained");
                            }
                        }
                        observations.Add(new{turn=view.Turn,ownColonies=view.Colonies.Length,ownedVessels=view.Vessels.Length,stationary=view.Vessels.Count(v=>v.Transit is null),travelling=view.Vessels.Count(v=>v.Transit is not null),shortages=view.Colonies.Where(c=>c.Economy.SupportDeficit>0).Select(c=>c.Id.Value),noProject=view.Colonies.Count(c=>c.ProjectId is null),researchTarget=view.Research.TargetId,crisisCases=view.Crisis?.Cases.Length??0,scoutingResponses=view.Scouting?.Attention.Length??0,conflictBattles=view.Conflict?.Encounters.Length??0,knownWars=view.Conflict?.Relations.Count(r=>r.Relation==RelationKind.War)??0,ownProductionPaid=observer.OwnProductionPaid,ownProductionIncome=observer.OwnProductionIncome});
                        if(new[]{10,25,50,80}.Contains(view.Turn))await Capture(view.Turn);
                        if(view.Turn%10==0)GD.Print($"M13 {profile.Species}/{profile.Mode}: actual boundary {view.Turn}, colonies {view.Colonies.Length}, paid production {observer.OwnProductionPaid}");
                    }
                    observer.FlushJournal();File.WriteAllText(Path.Combine(folder,"observations.json"),JsonSerializer.Serialize(observations,Pretty));
                    check(observer.CommittedBoundaries==83&&checkpoints.Count==5&&branches.Count==0&&checkpoints.All(c=>c.future is not null&&c.replay is not null),"Complete fixed profile has 80 audit boundaries, five actual Main saves and fifteen equal future boundaries");
                    campaigns.Add(new{species=profile.Species,seed=profile.Seed,mode=profile.Mode,galaxySize="small",rivalCount=rivals,rivalSpeciesId=other,rules=12,writer=13,crisesEnabled=true,amebasEnabled=true,sentriesEnabled=true,auditBoundaries=80,finalTurn=observer.Game.State.Turn,initialMainSaveExactGenerated=true,injectedAssets=false,sourceSha256=source,decisionSource="RequesterViewsAndPermittedPlanningQueries",journal=Artifact(Path.Combine(folder,"journal.json")),checkpoints,observations=Artifact(Path.Combine(folder,"observations.json")),ownProductionPaid=observer.OwnProductionPaid,ownProductionIncome=observer.OwnProductionIncome});
                    Matrix(campaigns.Count==8);GD.Print($"M13 actual Main campaign complete: {profile.Species}/{profile.Mode}");
                }
                finally{observer.FlushJournal();File.WriteAllText(Path.Combine(folder,"observations.json"),JsonSerializer.Serialize(observations,Pretty));}
            }
            check(campaigns.Count==8,"All eight fixed actual-Main campaign profiles completed without substitution");Matrix(true);
        }
        catch(Exception exception){Matrix(false,exception.ToString());throw;}
    }

    private static GameSession Live(GameScreen screen)=>(GameSession)typeof(GameScreen).GetField("_session",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(screen)!;
    private static async Task PlanTurn(PlayabilityCampaignObserver observer,PlayabilityCampaignControls controls,bool standard)
    {
        var game=observer.Game;var owner=observer.Owner;var content=game.Content;
        async Task Send(Command command)
        {
            switch(command)
            {
                case SetWorkforceCommand work:await controls.Workforce(work.Colony,work.Workforce);break;
                case SetProductionTargetCommand project:await controls.Production(project.Colony,project.Target);break;
                case SetMoveOrderCommand{Destination:{} star} move:await controls.Course(move.Vessel,star);break;
                case SetFoundingOrderCommand{Planet:{} planet} found:await controls.Found(found.Vessel,planet);break;
                case FleetCommand fleet when FleetQueries.PreviewAction(content,game.View(owner),fleet.Action).Allowed:await controls.Fleet(fleet.Action);break;
                case ConflictCommand conflict when ConflictQueries.Preview(content,game.View(owner),conflict.Action).Allowed:await controls.Conflict(conflict.Action);break;
                case CombatActionCommand combat when CombatQueries.Preview(content,game.View(owner),combat.Action).Allowed:await controls.Combat(combat.Action);break;
                case FleetCommand or ConflictCommand or CombatActionCommand:break; // stale optional planner candidate; never an asserted action
                default:throw new InvalidOperationException("Unsupported campaign ordinary control: "+command.GetType().Name);
            }
        }
        await controls.CrisisResponses();
        var view=game.View(owner);var context=RivalPlanningQueries.Create(content,game.State,owner);
        // Earned M12 support/expansion policy, retaining one research worker when feasible
        // so Standard plague cure uses paid ordinary research rather than a resource gift.
        foreach(var colony in view.Colonies)
        {
            ProductionTarget? target=colony.ProjectId is null?null:new(colony.ProjectKind,colony.ProjectId);
            int cargo=target is {Kind:ProductionKinds.Vessel} vessel?content.Vessels[vessel.ContentId].EmbarkPopulation:0;
            int research=colony.Population>=cargo+3?1:0;
            for(int support=cargo;support<=colony.Population-research;support++)
            {
                var work=new Workforce(support,colony.Population-support-research,research);var forecast=context.Forecast(colony.Id,work,target);
                if(forecast is null||forecast.SupportProduced.Total<forecast.SupportRequired.Total)continue;
                if(colony.Population<colony.Economy.Growth.Capacity&&work.Production>1)work=work with{Support=work.Support+1,Production=work.Production-1};
                if(work!=colony.Workforce)await controls.Workforce(colony.Id,work);break;
            }
        }
        view=game.View(owner);context=RivalPlanningQueries.Create(content,game.State,owner);
        if(view.Colonies.Length<2)
        {
            var role=view.AvailableVesselRoleIds.Select(id=>content.Vessels[id]).Where(r=>r.CanFoundColony).OrderBy(r=>r.Cost).ThenBy(r=>r.Id,StringComparer.Ordinal).First();
            if(!view.Vessels.Any(v=>content.Vessels[v.RoleId].CanFoundColony)&&!view.Colonies.Any(c=>c.ProjectId==role.Id)&&view.Colonies.FirstOrDefault(c=>c.ProjectId is null&&c.Crisis?.Quarantined!=true) is {} port)await controls.Production(port.Id,new(ProductionKinds.Vessel,role.Id));
            foreach(var founder in game.View(owner).Vessels.Where(v=>content.Vessels[v.RoleId].CanFoundColony&&v.Transit is null&&v.PendingMove is null&&v.PendingFounding is null))
            {
                var opportunities=view.Stars.Where(s=>s.Planets is not null).SelectMany(s=>s.Planets!.Value.Where(p=>p.OwnerId is null&&context.PreviewSettlement(p.Id,founder.RoleId) is {RejectionCode:null}).Select(p=>new{Star=s.Chart.Id,Planet=p.Id})).OrderBy(p=>p.Star==founder.StationaryStarId?0:1).ThenBy(p=>context.TravelTurns(founder.StationaryStarId!.Value,p.Star,founder.RoleId)??int.MaxValue).ThenBy(p=>p.Planet).FirstOrDefault();
                if(opportunities is not null){if(opportunities.Star==founder.StationaryStarId)await controls.Found(founder.Id,opportunities.Planet);else await controls.Course(founder.Id,opportunities.Star);}
            }
        }
        view=game.View(owner);context=RivalPlanningQueries.Create(content,game.State,owner);
        foreach(var scout in view.Vessels.Where(v=>!content.Vessels[v.RoleId].CombatOnly&&!content.Vessels[v.RoleId].CanFoundColony&&v.Transit is null&&v.PendingMove is null&&view.Scouting?.Vessels.FirstOrDefault(s=>s.Id==v.Id)?.Stage is null))
        {
            var next=view.Stars.Where(s=>s.Chart.Id!=scout.StationaryStarId&&view.Scouting?.Habitats.Any(h=>h.Star==s.Chart.Id)!=true).OrderBy(s=>s.LastObservedTurn??-1).ThenBy(s=>context.TravelTurns(scout.StationaryStarId!.Value,s.Chart.Id,scout.RoleId)??int.MaxValue).ThenBy(s=>s.Chart.Id).FirstOrDefault();
            if(next is not null)await controls.Course(scout.Id,next.Chart.Id);
        }
        view=game.View(owner);
        if(view.Research.TargetId is null&&content.Technologies.Values.Where(t=>Rules.TechnologyAvailable(content,view.RulesVersion,t.Id)&&!view.Research.KnownTechnologyIds.Contains(t.Id)&&t.PrerequisiteTechnologyIds.All(view.Research.KnownTechnologyIds.Contains)).OrderBy(t=>t.Cost).ThenBy(t=>t.Id,StringComparer.Ordinal).FirstOrDefault() is {} technology)await controls.Research(technology.Id);
        if(standard)
        {
            foreach(var stage in new[]{ConflictPlanningStage.Responses,ConflictPlanningStage.Production,ConflictPlanningStage.Grouping,ConflictPlanningStage.Grouping,ConflictPlanningStage.Orders})foreach(var command in ConflictPlanner.Plan(content,game.View(owner),stage))await Send(command);
            view=game.View(owner);if(view.Conflict?.Relations.FirstOrDefault(r=>r.Relation==RelationKind.Peace&&r.TruceEndTurn is null) is {} relation&&view.Conflict.Ships.Count(s=>s.Military&&!s.NewlyCompleted)>=2)
            {var action=new ConflictAction.DeclareWar(relation.Other);if(ConflictQueries.Preview(content,view,action).Allowed)await controls.Conflict(action);}
        }
        else
        {
            view=game.View(owner);
            foreach(var colony in view.Colonies.Where(c=>c.ProjectId is null&&c.Development?.Queue.IsEmpty!=false))
            {
                var building=content.Buildings.Values.Where(b=>!colony.CompletedBuildingIds.Contains(b.Id)&&b.PrerequisiteTechnologyIds.All(view.Research.KnownTechnologyIds.Contains)).OrderBy(b=>b.Cost).ThenBy(b=>b.Id,StringComparer.Ordinal).FirstOrDefault();
                if(building is not null)await controls.Production(colony.Id,new(ProductionKinds.Building,building.Id));
            }
        }
        foreach(var encounter in game.View(owner).Combat?.Encounters.Where(e=>game.View(owner).Combat!.Notices.Any(n=>n.Level==CombatNoticeLevel.Emergency&&n.Encounter==e.Id))??Enumerable.Empty<CombatEncounterView>())
        {var action=new CombatAction.RespondCombat(encounter.Id,encounter.Revision,CombatResponse.RemainWithBlockedReturn);if(!CombatQueries.Preview(content,game.View(owner),action).Allowed)action=action with{Response=CombatResponse.AcceptRevisedForce};await controls.Combat(action);}
        await controls.ScoutResponses();await controls.CrisisResponses();
    }

    private static Command ReadCommand(JsonElement element)
    {
        string? name=element.GetProperty("type").GetString();
        var extended=new[]{typeof(AcknowledgeCrisisCommand),typeof(SetQuarantineCommand),typeof(SetCureShareCommand),typeof(AcknowledgeAmebaCommand),typeof(SetScoutRetreatOrderCommand),typeof(SetAmebaRiskOrderCommand),typeof(SetAmebaApproachOrderCommand)}.SingleOrDefault(t=>t.FullName==name);
        return extended is null?EmpireConflictCampaignChecks.ReadJournalCommand(element):(Command)element.GetProperty("payload").Deserialize(extended)!;
    }
    private static string VerifyReplay(string save,string future)
    {
        var loaded=SaveGame.ReadFile(save);if(!loaded.Succeeded||loaded.Game!.State.RulesVersion!=12)throw new InvalidOperationException("Main checkpoint failed authoritative load.");
        using var proof=JsonDocument.Parse(File.ReadAllText(future));var data=proof.RootElement;
        if(data.GetProperty("mainSaveSha256").GetString()!=HashFile(save)||data.GetProperty("equalFutureBoundaries").GetInt32()!=3)throw new InvalidOperationException("Mismatched future source.");
        bool extended=false;var game=new GameSession(loaded.Game.Content,loaded.Game.State);int boundaries=0,commands=0;
        foreach(var boundary in data.GetProperty("hashes").EnumerateArray())
        {
            int before=game.State.Turn;
            foreach(var item in boundary.GetProperty("commands").EnumerateArray())
            {
                if(++commands>4096)throw new InvalidOperationException("Future command bound exceeded.");
                var element=item.GetProperty("command");Command command;
                try{command=EmpireConflictCampaignChecks.ReadJournalCommand(element);}catch(InvalidOperationException){extended=true;command=ReadCommand(element);}
                var outcome=game.Execute(command);if(outcome.Accepted!=item.GetProperty("accepted").GetBoolean())throw new InvalidOperationException("Future command acceptance differs.");
            }
            if(game.State.Turn!=before+1||Hash(Snapshot(game))!=boundary.GetProperty("save").GetString()||Hash(JsonSerializer.Serialize(game.State))!=boundary.GetProperty("state").GetString())throw new InvalidOperationException("Future full canonical boundary differs.");boundaries++;
        }
        if(boundaries!=3)throw new InvalidOperationException("Three complete future boundaries are required.");
        if(!extended)EmpireConflictCampaignChecks.VerifySaveProof(save,future); // unchanged existing probe authority
        return extended?"native-extended-core-replay":"existing-probe";
    }
}
