using UiNavigation = OpenAntares.Game.Validation.CombinedUiNavigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
namespace OpenAntares.Game.Validation;

internal static class ScoutUiControlChecks
{
    internal static ContentSet CompleteContent()
    {
        var files=DirAccess.GetFilesAt("res://content/core").Where(f=>f.EndsWith(".json",StringComparison.Ordinal)).OrderBy(f=>f,StringComparer.Ordinal).Select(f=>new ContentSource(f,Godot.FileAccess.GetFileAsString("res://content/core/"+f))).ToList();
        var loaded=ContentLoader.Load(files,requireCrises:true);if(loaded.Content?.SpaceAmeba is null)
        {
            using var fixture=JsonDocument.Parse(File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/scouting-content.json")));
            files.Add(new("approved-hazard-supplement","{\"space_ameba\":"+fixture.RootElement.GetProperty("space_ameba").GetRawText()+"}"));
        }
        return ContentLoader.Load(files,requireScouting:true).Content??throw new InvalidOperationException("Complete scout content failed.");
    }
    internal static LoadedGame LabelledWarning(bool cargo=false,int speed=300,int count=1,bool crisesEnabled=false,bool neutralOverride=false)
    {
        var c=CompleteContent();if(speed!=300)c=c with{Vessels=c.Vessels.SetItem("scout",c.Vessels["scout"] with{Speed=speed})};
        var generated=GalaxyGenerator.CreateNewGame(c,new("small",42,7,"harl",neutralOverride?1:0,neutralOverride?"peldra":null,CrisesEnabled:crisesEnabled,AmebasEnabled:true));if(!generated.Succeeded)throw new InvalidOperationException(generated.Error);var s=generated.State!;s.Turn=10;var owner=s.Empires[0];var h=s.Scouting!.Habitat!;var v=s.Vessels[0];v.StationaryStarId=h.Star;v.AmebaEncounter=new(){Number=1,HabitatId=h.Id,ArrivalTurn=10};owner.Scouting!.NextEncounterNumber=2;owner.Scouting.KnownHabitats.Add(new(h.Id,h.Star,10));
        if(neutralOverride){v.Scout!.Posture=ScoutPosture.AvoidEncounters;s.Vessels.Single(ship=>ship.EmpireId==s.Empires[1].Id).StationaryStarId=s.FindPlanet(s.Colonies.Single(col=>col.EmpireId==owner.Id).PlanetId)!.StarId;}
        if(cargo){v.RoleId="harl_settler";v.CarriedPopulation=c.Vessels[v.RoleId].EmbarkPopulation;v.Scout=null;}
        for(int i=1;i<count;i++){var extra=v.Clone();extra.Id=new(s.AllocateId());extra.AmebaEncounter!.Number=owner.Scouting.NextEncounterNumber++;s.Vessels.Add(extra);}
        ObservationUpdater.Refresh(s,10);ContactUpdater.Refresh(s.Clone(),s);var errors=GameStateValidator.Validate(c,s);if(errors.Count!=0)throw new InvalidOperationException(string.Join("; ",errors));return new(c,s);
    }
    public static async Task Run(Node root,Action<bool,string> check,string outputDirectory,string runId)
    {
        var records=new List<object>();void Check(bool value,string text){records.Add(new{passed=value,text});check(value,"Scout supplemental: "+text);}
        try
        {
            foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})
            {
                root.GetTree().Root.Size=size;var g=LabelledWarning();var owner=g.State.Empires[0].Id;var session=new GameSession(g.Content,g.State);var screen=Screen(session,owner,outputDirectory);root.AddChild(screen);await Frames(root);
                try
                {
                    var view=session.View(owner).Scouting!;Check(!view.Attention.IsEmpty,"Live warning is projected before acknowledgement");Check(view.Vessels[0].StudyPercent==75&&view.Vessels[0].CombinedRetentionNumerator==7125,"Study75%, late95%, combined7125/10000 from shared projection");
                    var overview=Descendants<AcceptDialog>(screen).FirstOrDefault(n=>n.Name=="ScoutOverview");Check(overview is not null,"Actual scout overview exists");if(overview is null)continue;
                    Check(overview.Visible,"Unresolved loaded warning opens the grouped overview by default");
                    var routeLabel=Descendants<Label>(overview).FirstOrDefault(n=>n.Name=="ScoutDepartureRoute");var destination=session.View(owner).Galaxy.Stars.Single(s=>s.Id==view.Vessels[0].EffectiveDestination);var origin=session.View(owner).Galaxy.Stars.Single(s=>s.Id==view.Vessels[0].Station);int duration=OpenAntares.Simulation.Movement.TravelCalculator.Duration(origin.X,origin.Y,destination.X,destination.Y,g.Content.Vessels[view.Vessels[0].RoleId].Speed);
                    Check(routeLabel is not null&&routeLabel.Text.Contains(destination.Name)&&routeLabel.Text.Contains($"Ordinary travel: {duration} turn")&&routeLabel.Text.Contains("Observed turn 10"),"Actual Warning discloses its effective destination, duration and own observation before acknowledgement");
                    var routeScroll=Descendants<ScrollContainer>(overview).First();routeScroll.EnsureControlVisible(routeLabel!);await Frames(root);Check(routeLabel!.GetGlobalRect().Intersects(routeScroll.GetGlobalRect()),"Actual Warning route consequences are reachable in the scroll at "+size.X);
                    Check(Descendants<Label>(screen).Any(n=>n.Name=="ScoutCriticalBadge"&&n.Visible),"Emergency badge remains visible");string before=Snapshot(session);Press(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));await Frames(root);Check(Snapshot(session)==before,"Actual End Turn cannot bypass exact acknowledgement");
                    Press(Named<Button>(overview,"ScoutStudy"));await Frames(root);var confirmation=Named<ConfirmationDialog>(overview,"ScoutConfirmation");Check(confirmation.Visible&&confirmation.DialogText.Contains("75%")&&confirmation.DialogText.Contains("7125/10000"),"Study confirmation shows exact two-stage retention");confirmation.Hide();await Frames(root);Check(Snapshot(session)==before,"Cancelling study preserves canonical state");
                    Press(Named<Button>(overview,"ScoutStudy"));await Frames(root);
                    var target=session.View(owner).Stars.First(star=>star.Chart.Id!=view.Vessels[0].Station&&star.Chart.Id!=view.Vessels[0].EffectiveDestination).Chart.Id;SelectDestination(screen,target);await Frames(root);Press(Named<Button>(screen,"SetVesselCourse"));await Frames(root);string moved=Snapshot(session);Press(confirmation.GetOkButton());await Frames(root);Check(Snapshot(session)==moved&&session.State.Vessels[0].AmebaEncounter!.RiskOrder is null,"Stale study confirmation rejects after an actual course change");
                    Press(Descendants<Button>(screen).Single(b=>b.Text=="Cancel move"));await Frames(root);Press(Named<Button>(overview,"ScoutStudy"));await Frames(root);Press(confirmation.GetOkButton());await Frames(root);Check(session.State.Vessels[0].AmebaEncounter!.RiskOrder?.Action==AmebaRiskAction.Study&&session.State.Empires[0].Scouting!.RiskRollCount==0,"Actual Study queues one-boundary risk without rolling");
                    Press(Named<Button>(overview,"ScoutAcknowledge"));await Frames(root);Check(session.State.Empires[0].Scouting!.Receipts.Count==1,"Actual grouped acknowledge saves exact current consequence");
                    Press(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));await Frames(root);Check(!overview.Visible&&Descendants<TurnReportDialog>(screen).Single().Visible,"Report owns the modal window before the next ameba prompt");foreach(var report in Descendants<TurnReportDialog>(screen))report.Hide();await Frames(root);Check(session.State.Turn==11&&session.State.Vessels[0].AmebaEncounter?.Stage==AmebaStage.Exposed,"Actual Ready resolves generated first draw55 into Exposed");Check(session.View(owner).Scouting!.History.SelectMany(d=>d.Entries).Any(e=>e.Kind=="study"&&e.ResearchAward==600),"Actual Study report retains earned600");Check(session.State.Empires[0].Scouting!.Receipts.Count==0,"Exposed requires fresh exact consent");
                    Press(Named<Button>(screen,"OpenScoutOverview"));await Frames(root);Press(Named<Button>(overview,"ScoutWithdraw"));await Frames(root);if(confirmation.Visible){Press(confirmation.GetOkButton());await Frames(root);}Press(Named<Button>(overview,"ScoutAcknowledge"));await Frames(root);Check(session.State.Vessels[0].PendingMove is not null,"Actual withdrawal uses ordinary pending travel");
                    string canonical=Snapshot(session);foreach(int mode in new[]{1,2,0}){Named<OptionButton>(overview,"ScoutDisplayLevel").Select(mode);Press(Named<Button>(overview,"ScoutApplyDisplay"));await Frames(root);Check(Snapshot(session)==canonical,"Local display mode "+mode+" preserves canonical state and consent");}
                    overview.PopupCenteredClamped(new(740,560));await Frames(root);await Frames(root);await Frames(root);Check(overview.Size.X<=size.X&&overview.Size.Y<=size.Y,"Grouped panel fits "+size.X+"x"+size.Y);var scroll=Descendants<ScrollContainer>(overview).First();var ack=Named<Button>(overview,"ScoutAcknowledge");File.AppendAllText(Path.Combine(outputDirectory,"response-scroll-geometry.jsonl"),JsonSerializer.Serialize(new{step="before-focus",ack=ack.GetGlobalRect().ToString(),scroll=scroll.GetGlobalRect().ToString(),offset=scroll.ScrollVertical})+"\n");ack.GrabFocus();File.AppendAllText(Path.Combine(outputDirectory,"response-scroll-geometry.jsonl"),JsonSerializer.Serialize(new{step="after-focus",ack=ack.GetGlobalRect().ToString(),scroll=scroll.GetGlobalRect().ToString(),offset=scroll.ScrollVertical})+"\n");await Frames(root);await Frames(root);await Frames(root);File.AppendAllText(Path.Combine(outputDirectory,"response-scroll-geometry.jsonl"),JsonSerializer.Serialize(new{size,ack=ack.GetGlobalRect().ToString(),scroll=scroll.GetGlobalRect().ToString(),offset=scroll.ScrollVertical,max=scroll.GetVScrollBar().MaxValue,page=scroll.GetVScrollBar().Page,focus=ack.HasFocus()})+"\n");bool initialReachable=ack.GetGlobalRect().Intersects(scroll.GetGlobalRect());if(!initialReachable){scroll.EnsureControlVisible(ack);await Frames(root);await Frames(root);await Frames(root);File.AppendAllText(Path.Combine(outputDirectory,"response-scroll-geometry.jsonl"),JsonSerializer.Serialize(new{step="after-settled-ensure",ack=ack.GetGlobalRect().ToString(),scroll=scroll.GetGlobalRect().ToString(),offset=scroll.ScrollVertical,focus=ack.HasFocus()})+"\n");}Check(initialReachable,"Focused response is reachable within scroll at "+size.X);
                    await Capture(root,overview,Path.Combine(outputDirectory,"scout-labelled-exposed-"+size.X+".png"));
                    SaveGame.WriteFile(Path.Combine(outputDirectory,"scout-labelled-exposed-"+size.X+".json"),session.Content,session.State);
                }
                finally{root.RemoveChild(screen);screen.QueueFree();await Frames(root);}
                var overridden=LabelledWarning(neutralOverride:true);screen=Screen(new GameSession(overridden.Content,overridden.State),overridden.State.Empires[0].Id,outputDirectory);root.AddChild(screen);await Frames(root);
                try
                {
                    var overview=Named<AcceptDialog>(screen,"ScoutOverview");var route=Named<Label>(overview,"ScoutDepartureRoute");Check(route.Text.Contains("Survival overrides neutral Avoid")&&route.Text.Contains("Ordinary travel:"),"Actual Warning explains survival overriding peaceful neutral caution with the route");var scroll=Descendants<ScrollContainer>(overview).First();scroll.EnsureControlVisible(route);await Frames(root);Check(route.GetGlobalRect().Intersects(scroll.GetGlobalRect()),"Neutral override explanation is reachable at "+size.X);await Capture(root,overview,Path.Combine(outputDirectory,"scout-neutral-override-"+size.X+".png"));
                }
                finally{root.RemoveChild(screen);screen.QueueFree();await Frames(root);}
                var cargo=LabelledWarning(cargo:true);var transport=new GameSession(cargo.Content,cargo.State);screen=Screen(transport,cargo.State.Empires[0].Id,outputDirectory);root.AddChild(screen);await Frames(root);
                try
                {
                    var overview=Named<AcceptDialog>(screen,"ScoutOverview");Check(Descendants<Label>(overview).Any(n=>n.Name=="ScoutResponseState"&&n.Text.Contains("response_required")),"Cargo with no route visibly requires a response instead of claiming departure");Press(Named<Button>(overview,"ScoutRemain"));await Frames(root);var confirm=Named<ConfirmationDialog>(overview,"ScoutConfirmation");Check(confirm.DialogText.Contains("cargo",StringComparison.OrdinalIgnoreCase)&&confirm.DialogText.Contains("100%"),"Remain confirmation explicitly loses vessel and cargo");Press(confirm.GetOkButton());await Frames(root);Press(Named<Button>(overview,"ScoutAcknowledge"));await Frames(root);Press(Descendants<Button>(screen).Single(b=>b.Text=="End Turn"));await Frames(root);Check(transport.State.Vessels.Count==0&&transport.State.Turn==11,"Actual cargo Remain/Acknowledge/Ready consumes exact expedition");Check(transport.View(cargo.State.Empires[0].Id).Scouting!.History.SelectMany(d=>d.Entries).Any(e=>e.Kind=="loss"&&e.Cargo>0),"Cargo loss stays readable without a live vessel");await Capture(root,Descendants<TurnReportDialog>(screen).Single(),Path.Combine(outputDirectory,"scout-labelled-cargo-loss-"+size.X+".png"));Check(!overview.Visible,"Reopening a report retains the queued emergency without a competing exclusive window");
                }
                finally{root.RemoveChild(screen);screen.QueueFree();await Frames(root);}
            }
        }
        finally{File.WriteAllText(Path.Combine(outputDirectory,"scout-supplement.json"),JsonSerializer.Serialize(new{runId,provenance="Labelled valid pre-session fixtures derived from complete generated core; in-process actual controls, no physical input claim",checks=records},new JsonSerializerOptions{WriteIndented=true}));}
    }
    internal static GameScreen Screen(GameSession s,EmpireId owner,string folder)
    {
        var screen=new GameScreen(s,owner){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres"),NotificationPreferencesPath=Path.Combine(folder,"scout-colony-prefs.json"),CrisisPreferencesPath=Path.Combine(folder,"scout-crisis-prefs.json")};typeof(GameScreen).GetProperty("ScoutPreferencesPath")?.SetValue(screen,Path.Combine(folder,"scout-prefs.json"));return screen;
    }
    internal static void SelectDestination(GameScreen screen,StarId target){var picker=Named<OptionButton>(screen,"DestinationPicker");int index=Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsInt32()==target.Value);picker.Select(index);picker.EmitSignal(OptionButton.SignalName.ItemSelected,(long)index);}
    internal static IEnumerable<T> Descendants<T>(Node node)where T:Node{foreach(Node child in node.GetChildren()){if(child is T value)yield return value;foreach(var nested in Descendants<T>(child))yield return nested;}}
    internal static T Named<T>(Node root,string name)where T:Node=>Descendants<T>(root).Single(n=>n.Name==name);
    internal static void Press(Button button){button=UiNavigation.Reveal(button);if(button.Disabled)throw new InvalidOperationException("Disabled control: "+button.Name);button.EmitSignal(BaseButton.SignalName.Pressed);}
    internal static async Task Frames(Node root){await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
    internal static string Snapshot(GameSession s)=>SaveGame.Serialize(s.Content,s.State);
    internal static async Task Capture(Node root,Window window,string file){window.Hide();window.ForceNative=false;window.PopupCenteredClamped(new(740,540));await Frames(root);await root.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=root.GetViewport().GetTexture().GetImage();image.SavePng(file);}
    internal static async Task<GameScreen> LoadGeneratedRules8(Node root,Control main,NewGameSettings settings,string folder,string label,Action<bool,string> check)
    {
        check(settings.RulesVersion==8&&settings.SentriesEnabled is null,"Historical graphical source explicitly selects rules8");
        var content=CompleteContent();var generated=GalaxyGenerator.CreateNewGame(content,settings);check(generated.Succeeded,"Explicit rules8 graphical source generates without reinterpretation");string canonical=SaveGame.Serialize(content,generated.State!);
        string name="qa-explicit8-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+label;string path=SaveFiles.PathFor(name);SaveGame.WriteFile(path,content,generated.State!);
        var setup=Descendants<NewGameScreen>(main).Single();Press(Descendants<Button>(setup).Single(b=>b.Text=="Load Game"));await Frames(root);var load=Descendants<LoadGameDialog>(main).Single();var list=Descendants<ItemList>(load).Single();int index=Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemText(i).StartsWith(name+"    ",StringComparison.Ordinal));list.Select(index);list.EmitSignal(ItemList.SignalName.ItemSelected,(long)index);Press(load.GetOkButton());await Frames(root);var screen=Descendants<GameScreen>(main).Single();
        var session=(GameSession)typeof(GameScreen).GetField("_session",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(screen)!;check(Snapshot(session)==canonical,"Actual Main Load preserves explicit8 source and embedded old roles");
        File.AppendAllText(Path.Combine(folder,"explicit-rules8-starts.jsonl"),JsonSerializer.Serialize(new{source="Explicit rules8 source replacing the former current8 Start path; ordinary Main Load, no live state injection",settings,path,sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant()})+"\n");return screen;
    }

}
