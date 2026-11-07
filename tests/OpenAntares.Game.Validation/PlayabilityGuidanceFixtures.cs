using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Validation;

internal static partial class PlayabilityGuidanceChecks
{
    // Labelled pre-session UI supplements, never counted as earned campaign saves or stages.
    private static LoadedGame Generated(ContentSet content,int rules=12)
    {
        var generated=GalaxyGenerator.CreateNewGame(content,new("small",42,rules,"harl",1,"peldra",true,true,true));
        if(!generated.Succeeded)throw new InvalidOperationException(generated.Error);
        return new(content,generated.State!);
    }
    private static LoadedGame Valid(LoadedGame fixture)
    {
        var errors=GameStateValidator.Validate(fixture.Content,fixture.State);
        if(errors.Count>0)throw new InvalidOperationException("Invalid labelled guidance supplement: "+string.Join("; ",errors));
        return fixture;
    }
    private static LoadedGame CrisisSupplement(ContentSet content)
    {
        var fixture=Generated(content);var state=fixture.State;state.Turn=22;
        var owner=state.Empires[0];var colony=state.Colonies.First(c=>c.EmpireId==owner.Id);
        var definition=content.Plagues.Values.Single();var location=CrisisQueries.Location(state,colony);
        owner.Crisis!.NextEpisodeNumber=2;
        owner.Crisis.Episode=new(){Id=1,FamilyId=definition.Id,Source=location,SourcePopulation=colony.Population,CureCost=checked(definition.CureBase+definition.CurePerPopulation*colony.Population)};
        colony.Crisis!.Case=new(){EpisodeId=1,Stage=CrisisStage.Active,Severity=20,AttentionRevision=3,HandledRevision=2,Origin=location};
        ObservationUpdater.Refresh(state,state.Turn);
        return Valid(fixture);
    }
    private static LoadedGame ScoutSupplement(ContentSet content)
    {
        var fixture=Generated(content);var state=fixture.State;var owner=state.Empires[0];
        var habitat=state.Scouting!.Habitat??throw new InvalidOperationException("Approved seed must have its generated ameba habitat.");
        var vessel=state.Vessels.First(v=>v.EmpireId==owner.Id);
        vessel.StationaryStarId=habitat.Star;vessel.AmebaEncounter=new(){Number=1,HabitatId=habitat.Id,ArrivalTurn=state.Turn,Stage=AmebaStage.Warning};
        owner.Scouting!.NextEncounterNumber=2;owner.Scouting.KnownHabitats.Add(new(habitat.Id,habitat.Star,state.Turn));
        ObservationUpdater.Refresh(state,state.Turn);CombatObservations.Refresh(content,state,state.Turn);
        return Valid(fixture);
    }
    private static LoadedGame CombatForecastSupplement(ContentSet content)
    {
        var generated=GalaxyGenerator.CreateNewGame(content,new("small",42,9,"harl",1,"peldra",false,false,true));
        if(!generated.Succeeded)throw new InvalidOperationException(generated.Error);
        var state=generated.State!;CrisisInitializer.Initialize(content,state,true);
        var owner=state.Empires[0];var pocket=state.Combat!.Pockets[0];var first=state.Vessels.First(v=>v.EmpireId==owner.Id);
        state.Vessels.RemoveAll(v=>v.EmpireId==owner.Id);first.RoleId="escort";first.StationaryStarId=pocket.Star;first.Fleet=new(1);first.Scout=null;
        ScoutInitializer.InitializeVessel(content,9,first);CombatInitializer.InitializeVessel(content,9,first);state.Vessels.Add(first);
        var civilian=new VesselState{Id=new(state.AllocateId()),EmpireId=owner.Id,RoleId="harl_settler",CarriedPopulation=content.Vessels["harl_settler"].EmbarkPopulation,StationaryStarId=pocket.Star,Fleet=new(1),Crisis=new()};
        ScoutInitializer.InitializeVessel(content,9,civilian);CombatInitializer.InitializeVessel(content,9,civilian);state.Vessels.Add(civilian);state.Vessels.Sort((a,b)=>a.Id.Value.CompareTo(b.Id.Value));
        owner.Fleets!.Fleets.Add(new(){Id=new(1),Name="Labelled forecast convoy",Members=new(){first.Id,civilian.Id},Flagship=first.Id});owner.Fleets.NextFleetId=2;
        for(int i=0;i<pocket.Sentries.Count;i++){pocket.Sentries[i].Profile=content.Combat!.SentryProfile;pocket.Sentries[i].Hull=i==0?content.Combat.SentryProfile.MaxHull:0;}
        ObservationUpdater.Refresh(state,state.Turn);
        owner.Combat!.Observations.Clear();owner.Combat.Observations.Add(new(pocket.Id,pocket.Star,state.Turn,false,pocket.Sentries.Select(s=>new ObservedSentry(s.Id,s.Profile,s.Hull,content.Combat!.SentryPursuitSpeed)).ToImmutableArray()));
        Valid(new(content,state));var game=new GameSession(content,state);var id=owner.Id;var port=state.Colonies.First(c=>c.EmpireId==id).Id;
        void Send(CombatAction action)
        {
            var preview=CombatQueries.Preview(content,game.View(id),action);
            if(!preview.Allowed)throw new InvalidOperationException("Labelled combat preparation: "+preview.RejectionCode);
            var result=game.Execute(new CombatActionCommand(id,game.State.Turn,action,preview.Token));
            if(!result.Accepted)throw new InvalidOperationException(result.Rejection?.Message);
        }
        Send(new CombatAction.AttackPocket(new(1),pocket.Id,new(CombatDoctrine.Hold,port,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair),true));
        var firstRound=game.Execute(new SetReadyCommand(id,game.State.Turn,true));if(!firstRound.Accepted)throw new InvalidOperationException(firstRound.Rejection?.Message);
        var encounter=game.State.Combat!.Encounters.Single(e=>e.Owner==id);
        Send(new CombatAction.ChangeCombatOrders(encounter.Id,encounter.Revision,encounter.Orders,encounter.CivilianRiskAuthorized));
        encounter=game.State.Combat.Encounters.Single(e=>e.Owner==id);Send(new CombatAction.WithdrawCombat(encounter.Id,encounter.Revision));
        state=game.State;owner=state.FindEmpire(id)!;var colony=state.Colonies.First(c=>c.EmpireId==id);var plague=content.Plagues.Values.Single();var location=CrisisQueries.Location(state,colony);
        owner.Crisis!.RequireAcknowledgement=false;owner.Crisis.Authority=CrisisAuthority.Contain;owner.Crisis.NextEpisodeNumber=2;
        owner.Crisis.Episode=new(){Id=1,FamilyId=plague.Id,Source=location,SourcePopulation=colony.Population,CureCost=checked(plague.CureBase+plague.CurePerPopulation*colony.Population)};
        colony.Crisis!.Case=new(){EpisodeId=1,Stage=CrisisStage.Warning,GraceRemaining=plague.GraceTurns,AttentionRevision=1,Origin=location};
        return Valid(new(content,state));
    }

    public static async Task EngineRejections(Node host,ContentSet content,Func<LoadedGame,string,Task<GameScreen>> mount,Action<bool,string> check)
    {
        foreach(var scenario in new[]{"crisis","scouting","combat-forecast"})
        {
            var fixture=scenario=="crisis"?CrisisSupplement(content):scenario=="scouting"?ScoutSupplement(content):CombatForecastSupplement(content);
            var screen=await mount(fixture,"guidance-"+scenario);var game=(GameSession)typeof(GameScreen).GetField("_session",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(screen)!;
            foreach(var window in Descendants<Window>(screen))window.Hide();await Frames(host);
            var owner=game.State.Empires.First(e=>e.Controller==ControllerKind.Human).Id;
            string before=SaveGame.Serialize(game.Content,game.State);
            var reference=new GameSession(game.Content,game.State.Clone());var expected=reference.Execute(new SetReadyCommand(owner,game.State.Turn,true));
            check(!expected.Accepted&&expected.Rejection is not null,scenario+": fixture actually has an authoritative Ready rejection");
            if(scenario=="combat-forecast")
            {
                var model=typeof(GameScreen).Assembly.GetType("OpenAntares.Game.Presentation.TurnGuidance")!;
                check(Build(model,game.View(owner)).All(i=>Property(i,"Severity")!="Required"),"Forecast-only combat rejection has no fabricated projected requirement");
                check(Descendants<Label>(screen).Any(l=>l.Text==EmptyCopy),"Forecast-only case retains exact honest empty-state guidance");
                check(expected.Rejection!.Code==CombatRejectionCodes.PortClosed,"Forecast actually observes automatic containment closing the return port");
            }
            var end=Descendants<Button>(screen).Single(b=>b.Name=="EndTurn");
            check(!end.Disabled,scenario+": existing engine activation remains available for forecast checks");
            end.EmitSignal(Button.SignalName.Pressed);await Frames(host,5);
            check(before==SaveGame.Serialize(game.Content,game.State),"ForecastOnlyRejectionRemainsAuthoritative: "+scenario+" rejects atomically without retry, acknowledgement or random draw");
            var feedback=Descendants<Label>(screen).Single(l=>l.Name=="CommandFeedback");
            check(feedback.Visible&&feedback.Text==expected.Rejection!.Message,scenario+": exact unchanged engine rejection is visible");
            var dismiss=Descendants<Button>(screen).Single(b=>b.Name=="DismissCommandFeedback");
            check(dismiss.HasFocus(),scenario+": rejection focuses its safe dismiss action");
            foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1152,648),new Vector2I(1600,900),new Vector2I(1920,1080)})
            {
                host.GetTree().Root.Size=size;await Frames(host,4);
                check(new Rect2(Vector2.Zero,size).Encloses(dismiss.GetGlobalRect())&&new Rect2(Vector2.Zero,size).Encloses(end.GetGlobalRect()),scenario+" "+size+": safe dismiss and End Turn remain within the physical viewport");
                check(feedback.Text==expected.Rejection!.Message&&before==SaveGame.Serialize(game.Content,game.State),scenario+" "+size+": rejection resizing changes neither exact explanation nor campaign state");
            }
            var review=Descendants<Button>(screen).Single(b=>b.Name=="ReviewRejectedResponse");
            check(review.Visible,scenario+": known rejection has an explicit existing-overview link");
            review.EmitSignal(Button.SignalName.Pressed);await Frames(host);
            check(Descendants<Window>(screen).Any(w=>w.Visible)&&before==SaveGame.Serialize(game.Content,game.State),scenario+": reviewing the rejection opens an overview without answering it");
            foreach(var window in Descendants<Window>(screen))window.Hide();await Frames(host);
            dismiss.EmitSignal(Button.SignalName.Pressed);await Frames(host);
            check(before==SaveGame.Serialize(game.Content,game.State),scenario+": dismissing explanation issues no response or retry");
            if(scenario=="scouting")
            {
                var currentView=game.View(owner);var ack=new AcknowledgeAmebaCommand(owner,game.State.Turn,currentView.Scouting!.Attention);
                var acknowledged=new GameSession(game.Content,game.State.Clone());check(acknowledged.Execute(ack).Accepted,"Scouting acknowledgement reference is an ordinary legal command");
                Descendants<Button>(screen).Single(b=>b.Name=="OpenScoutOverview").EmitSignal(Button.SignalName.Pressed);await Frames(host);
                var overview=Descendants<ScoutOverviewDialog>(screen).Single();Descendants<Button>(overview).Single(b=>b.Name=="ScoutAcknowledge").EmitSignal(Button.SignalName.Pressed);await Frames(host);
                check(SaveGame.Serialize(game.Content,game.State)==SaveGame.Serialize(acknowledged.Content,acknowledged.State),"Only explicit acknowledgement records the exact projected scouting group");
                overview.GetOkButton().EmitSignal(Button.SignalName.Pressed);await Frames(host);
                long rolls=game.State.FindEmpire(owner)!.Scouting!.RiskRollCount;
                var departure=new GameSession(game.Content,game.State.Clone());var ready=departure.Execute(new SetReadyCommand(owner,game.State.Turn,true));
                check(ready.Accepted,"Scouting preparation has a legal ordinary departure after explicit consent");
                end.EmitSignal(Button.SignalName.Pressed);await Frames(host,5);
                check(SaveGame.Serialize(game.Content,game.State)==SaveGame.Serialize(departure.Content,departure.State)&&game.State.FindEmpire(owner)!.Scouting!.RiskRollCount==rolls,"Actual Ready materializes only the engine's prepared safe route with no warning-departure random draw");
            }
        }
        var waiting=Generated(content);waiting.State.Empires[1].Controller=ControllerKind.Human;waiting.State.Empires[1].AiPolicyId=null;Valid(waiting);
        var current=await mount(waiting,"guidance-waiting");var session=(GameSession)typeof(GameScreen).GetField("_session",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(current)!;
        var viewer=session.State.Empires[0].Id;var comparison=new GameSession(session.Content,session.State.Clone());
        var button=Descendants<Button>(current).Single(b=>b.Name=="EndTurn");
        comparison.Execute(new SetReadyCommand(viewer,0,true));button.EmitSignal(Button.SignalName.Pressed);await Frames(host,5);
        check(session.State.Turn==0&&session.View(viewer).Research.Ready&&button.Text.Contains("cancel",StringComparison.OrdinalIgnoreCase)&&SaveGame.Serialize(session.Content,session.State)==SaveGame.Serialize(comparison.Content,comparison.State),"WaitingCancelAndRevisionRemainAuthoritative: first human Ready waits and matches ordinary engine preparation");
        comparison.Execute(new SetReadyCommand(viewer,0,false));button.EmitSignal(Button.SignalName.Pressed);await Frames(host,5);
        check(!session.View(viewer).Research.Ready&&session.State.Turn==0&&SaveGame.Serialize(session.Content,session.State)==SaveGame.Serialize(comparison.Content,comparison.State),"Cancel withdraws only readiness through its original command and publishes no boundary");
    }
}
