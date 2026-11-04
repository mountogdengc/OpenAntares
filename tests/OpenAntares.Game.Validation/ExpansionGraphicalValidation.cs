using UiNavigation = OpenAntares.Game.Validation.CombinedUiNavigation;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
namespace OpenAntares.GameValidation;
public partial class ExpansionGraphicalValidation : GraphicalValidation
{
    public override async void _Ready()
    {
        string? error=null;
        try
        {
            _output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output=",StringComparison.Ordinal))[20..];Directory.CreateDirectory(_output);
            Check(DisplayServer.GetName()!="headless","Graphical display required");Check(GetTree().Root.Size==new Vector2I(1920,1080),"Default window 1920x1080");
            foreach(string size in new[]{"small","medium","large"})await GeneratedLoop(size);
            await SyntheticViews();await NondefaultContentInstructions();await LegacyFixtures();
        }
        catch(Exception ex){error=ex.ToString();GD.PrintErr(error);}
        finally
        {
            if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{runId=_runId,scenario="Expansion",method="Real controls/signals/handlers; generated campaigns and separately labelled synthetic fixtures. No physical OS input or player-understanding claim.",godot=Engine.GetVersionInfo()["string"].AsString(),display=DisplayServer.GetName(),passed=error is null,error,checks=_results,results=_results,retainedQaSavePaths=_savePaths},new JsonSerializerOptions{WriteIndented=true}));
            GetTree().Quit(error is null?0:1);
        }
    }
    private async Task GeneratedLoop(string size)
    {
        GetTree().Root.Size=new(1920,1080);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();
        var setup=One<NewGameScreen>(main);setup.Configure(size,42);Press(Button(setup,"Start"));await Frames();
        var frozenContent=FrozenExpansionContent();
        var frozenState=GalaxyGenerator.CreateNewGame(frozenContent,new(size,42,Rules.ExpansionVersion)).State!;
        string frozenName=$"qa-{_runId}-{size}-frozen-expansion";string frozenPath=SaveFiles.PathFor(frozenName);
        SaveGame.WriteFile(frozenPath,frozenContent,frozenState);_savePaths.Add(frozenPath);
        var screen=await LoadThroughDialog(main,frozenName);var session=Session(screen);var viewer=session.State.Empires[0].Id;
        Check(session.State.RulesVersion==2,$"{size}: frozen expansion campaign uses rules 2");Check(session.State.Vessels.Count==1&&session.State.Vessels[0].RoleId=="scout",$"{size}: one starting scout");Check(session.State.Empires[0].Surveys.Count==1,$"{size}: home-only survey");
        var view=session.View(viewer);var scout=view.Vessels[0];var home=view.Stars.Single(s=>s.Chart.Id==scout.StationaryStarId);
        var target=view.Stars.Where(s=>s.Chart.Id!=home.Chart.Id).OrderByDescending(s=>TravelCalculator.Duration(home.Chart.X,home.Chart.Y,s.Chart.X,s.Chart.Y,300)).ThenBy(s=>s.Chart.Id).First();
        int scoutTurns=TravelCalculator.Duration(home.Chart.X,home.Chart.Y,target.Chart.X,target.Chart.Y,300);
        Check(scoutTurns>1,$"{size}: generated route exercises saved transit");
        One<GalaxyMap>(screen).Select(target.Chart.Id);await Frames();
        var texts=Descendants<Label>(One<StarPanel>(screen)).Select(l=>l.Text).ToArray();Check(texts.Contains("Not surveyed")&&!texts.Any(t=>t.EndsWith("planets",StringComparison.Ordinal)||t=="1 planet"),$"{size}: unsurveyed inspector hides planet count");
        One<GalaxyMap>(screen).Select(home.Chart.Id);await Frames();Select(Descendants<OptionButton>(One<ColonyPanel>(screen)).Single(),"vessel|settler");await Frames();Check(session.State.Colonies[0].ProjectKind=="vessel",$"{size}: settler selection uses typed production");
        One<GalaxyMap>(screen).Select(target.Chart.Id);await Frames();await Reach(Button(One<VesselPanel>(screen),"Set course"));Press(Button(One<VesselPanel>(screen),"Set course"));await Frames();Check(session.State.Vessels[0].PendingMove==target.Chart.Id,$"{size}: pending scout route");
        string pending=await SaveThroughDialog(screen,$"{size}-pending-move");screen=await LoadThroughDialog(main,pending);session=Session(screen);Check(session.State.Vessels[0].PendingMove==target.Chart.Id,$"{size}: saved pending move retained");
        var replacement=view.Stars.First(s=>s.Chart.Id!=home.Chart.Id&&s.Chart.Id!=target.Chart.Id).Chart;
        OptionButton Destination()=>Descendants<OptionButton>(One<VesselPanel>(screen)).Single(p=>p.Name=="DestinationPicker");
        Check(Destination().GetSelectedMetadata().AsInt32()==target.Chart.Id.Value,$"{size}: reload initially previews queued route");
        SelectInt(Destination(),replacement.Id.Value);await Frames();
        Check(Destination().GetSelectedMetadata().AsInt32()==replacement.Id.Value,$"{size}: destination picker retains replacement preview");
        int replacementTurns=TravelCalculator.Duration(home.Chart.X,home.Chart.Y,replacement.X,replacement.Y,300);
        Check(Descendants<Label>(One<VesselPanel>(screen)).Any(l=>l.Text.Contains($"travel {replacementTurns} turn(s).",StringComparison.Ordinal)),$"{size}: replacement travel preview uses selected destination");
        Check(session.State.Vessels[0].PendingMove==target.Chart.Id,$"{size}: preview alone preserves queued route");
        Press(Button(One<VesselPanel>(screen),"Set course"));await Frames();Check(session.State.Vessels[0].PendingMove==replacement.Id,$"{size}: Set course replaces queued route");
        string replaced=await SaveThroughDialog(screen,$"{size}-replaced-move");screen=await LoadThroughDialog(main,replaced);session=Session(screen);
        Check(Destination().GetSelectedMetadata().AsInt32()==replacement.Id.Value,$"{size}: replaced route remains selected after reload");
        SelectInt(Destination(),target.Chart.Id.Value);await Frames();Press(Button(One<VesselPanel>(screen),"Set course"));await Frames();Check(session.State.Vessels[0].PendingMove==target.Chart.Id,$"{size}: queued route can be replaced again after reload");
        await EndTurn(screen);One<TurnReportDialog>(screen).Hide();Check(session.State.Vessels[0].Transit is not null,$"{size}: launch advances first transit step");Check(Descendants<Button>(One<VesselPanel>(screen)).Any(b=>b.Text=="Set course"&&b.Disabled),$"{size}: transit controls disabled");
        string transit=await SaveThroughDialog(screen,$"{size}-transit");screen=await LoadThroughDialog(main,transit);session=Session(screen);
        while(session.State.Turn<6)
        {
            var forecast=session.Forecast();await EndTurn(screen);
            Check(session.State.Colonies[0].ProductionReserve==forecast.Colonies[0].Production.Ending,$"{size}: forecast equals resolved production at turn {session.State.Turn}");
            if(session.State.Turn==6)Check(Descendants<Label>(One<TurnReportDialog>(screen)).Any(l=>l.Text.Contains("Reassign workers",StringComparison.Ordinal)),$"{size}: embark report warns about next-turn support");
            One<TurnReportDialog>(screen).Hide();
        }
        Check(session.State.Vessels.Count==2,$"{size}: one settler completed");Check(session.State.Colonies[0].Population==4&&session.State.Colonies[0].Workforce==new Workforce(0,2,2),$"{size}: two existing support workers embarked");
        One<GalaxyMap>(screen).Select(home.Chart.Id);await Frames();Press(Step(One<ColonyPanel>(screen),"Move a worker from production to support"));await Frames();Press(Step(One<ColonyPanel>(screen),"Move a worker from production to support"));await Frames();Check(session.Forecast().Colonies[0].SupportDeficit==0,$"{size}: real workforce controls repair source support");
        Select(Descendants<OptionButton>(One<ColonyPanel>(screen)).Single(),"fabrication_hall");await Frames();
        var settler=session.View(viewer).Vessels.Single(v=>v.RoleId=="settler");SelectInt(Descendants<OptionButton>(One<VesselPanel>(screen)).Single(p=>p.Name=="VesselPicker"),settler.Id.Value);await Frames();
        One<GalaxyMap>(screen).Select(target.Chart.Id);await Frames();Press(Button(One<VesselPanel>(screen),"Set course"));await Frames();
        int duration=TravelCalculator.Duration(home.Chart.X,home.Chart.Y,target.Chart.X,target.Chart.Y,200);int departure=session.State.Turn;
        for(int i=0;i<duration;i++){await EndTurn(screen);One<TurnReportDialog>(screen).Hide();}
        Check(session.State.Turn==departure+duration&&session.State.FindVessel(settler.Id)!.StationaryStarId==target.Chart.Id,$"{size}: settler arrives at exact integer duration");Check(session.State.Colonies.Count==1,$"{size}: arrival never founds automatically");
        One<GalaxyMap>(screen).Select(target.Chart.Id);await Frames();var planet=session.View(viewer).Stars.Single(s=>s.Chart.Id==target.Chart.Id).Planets!.Value.First(p=>p.Colonizable&&!p.OwnerId.HasValue);
        Press(Button(One<StarPanel>(screen),$"Select planet {planet.Orbit}"));await Frames();await Reach(Button(One<VesselPanel>(screen),"Found colony"));Press(Button(One<VesselPanel>(screen),"Found colony"));await Frames();Check(session.State.FindVessel(settler.Id)!.PendingFounding==planet.Id,$"{size}: explicit founding intent");
        string founding=await SaveThroughDialog(screen,$"{size}-pending-founding");screen=await LoadThroughDialog(main,founding);session=Session(screen);Check(session.State.FindVessel(settler.Id)!.PendingFounding==planet.Id,$"{size}: saved founding intent retained");
        var oldEconomy=session.Forecast();await EndTurn(screen);Check(session.State.Colonies.Count==2,$"{size}: founding creates second colony");Check(session.State.Vessels.All(v=>v.Id!=settler.Id),$"{size}: founding consumes correct settler");One<TurnReportDialog>(screen).Hide();
        var second=session.View(viewer).Colonies.Single(c=>c.PlanetId==planet.Id);Check(second.Population==2&&second.Workforce==new Workforce(2,0,0)&&second.GrowthProgress==0&&second.ProductionReserve==0,$"{size}: no founding-turn colony economy");
        var colonyList=Descendants<OptionButton>(One<ColonyList>(screen)).Single();SelectInt(colonyList,second.Id.Value);await Frames();Check(One<GalaxyMap>(screen).Selected==target.Chart.Id,$"{size}: exact colony selection selects correct system");
        Press(Step(One<ColonyPanel>(screen),"Move a worker from support to research"));await Frames();Select(Descendants<OptionButton>(One<ColonyPanel>(screen)).Single(),"vessel|scout");await Frames();
        Check(session.State.Colonies[0].ProjectId=="fabrication_hall"&&session.State.FindColony(second.Id)!.ProjectId=="scout",$"{size}: independent colony projects");
        var forecasts=session.Forecast();Check(forecasts.Empire(viewer).NetResearch.Total==forecasts.Colonies.Sum(c=>c.NetResearch.Total),$"{size}: empire research includes each colony once");Check(forecasts.Colony(second.Id).NetResearch.Total>0,$"{size}: settled colony contributes from next snapshot");
        string snapshot=Snapshot(session);foreach(var dimensions in new[]{new Vector2I(800,600),new Vector2I(1152,648),new Vector2I(1600,900),new Vector2I(1920,1080)}){GetTree().Root.Size=dimensions;await Frames();Check(Snapshot(session)==snapshot,$"{size}: resize {dimensions.X}x{dimensions.Y} preserves state");Check(One<GalaxyMap>(screen).Size.X>0,$"{size}: map remains reachable after resize");if(dimensions.X is 800 or 1920)await Capture($"{size}-two-colonies-{dimensions.X}");}
        string completed=await SaveThroughDialog(screen,$"{size}-two-colonies");var expected=new GameSession(session.Content,session.State.Clone());Check(expected.Execute(new SetReadyCommand(viewer,expected.State.Turn,true)).Accepted,"Predicted continuation resolves");screen=await LoadThroughDialog(main,completed);await EndTurn(screen);Check(Snapshot(Session(screen))==Snapshot(expected),$"{size}: UI reload continuation equals uninterrupted result");One<TurnReportDialog>(screen).Hide();
        RemoveChild(main);main.QueueFree();await Frames();
    }
    private static GameState SyntheticFixture()
    {
        var s=new GameState{RulesVersion=2,NextEntityId=12};s.Empires.Add(new EmpireState{Id=new(1),Controller=ControllerKind.Human});
        s.Stars.Add(new StarState{Id=new(2),Name="Fixture Home",X=100,Y=100});s.Stars.Add(new StarState{Id=new(10),Name="Fixture Unknown",X=500,Y=100});
        s.Planets.Add(new PlanetState{Id=new(3),StarId=new(2),Orbit=1,PlanetTypeId="forge_world"});s.Planets.Add(new PlanetState{Id=new(4),StarId=new(2),Orbit=2,PlanetTypeId="crystal_world"});s.Planets.Add(new PlanetState{Id=new(9),StarId=new(2),Orbit=3,PlanetTypeId="verdant_world"});s.Planets.Add(new PlanetState{Id=new(11),StarId=new(10),Orbit=1,PlanetTypeId="forge_world"});
        s.Colonies.Add(new ColonyState{Id=new(5),EmpireId=new(1),PlanetId=new(3),Population=2,Workforce=new(2,0,0)});s.Colonies.Add(new ColonyState{Id=new(6),EmpireId=new(1),PlanetId=new(4),Population=2,Workforce=new(2,0,0)});
        s.Vessels.Add(new VesselState{Id=new(7),EmpireId=new(1),RoleId="scout",StationaryStarId=new(2)});s.Vessels.Add(new VesselState{Id=new(8),EmpireId=new(1),RoleId="settler",StationaryStarId=new(2),CarriedPopulation=2});ObservationUpdater.Refresh(s,0);return s;
    }
    private async Task SyntheticViews()
    {
        var content=FrozenExpansionContent();var state=SyntheticFixture();Check(GameStateValidator.Validate(content,state).Count==0,"Labelled synthetic fixture is valid");
        var session=new GameSession(content,state);var screen=new GameScreen(session,new(1));AddChild(screen);await Frames();
        SelectInt(Descendants<OptionButton>(One<ColonyList>(screen)).Single(),6);await Frames();Press(Step(One<ColonyPanel>(screen),"Move a worker from support to research"));await Frames();
        Check(session.State.FindColony(new(6))!.Workforce==new Workforce(1,0,1)&&session.State.FindColony(new(5))!.Workforce==new Workforce(2,0,0),"Synthetic same-system selection commands the exact second colony");
        SelectInt(Descendants<OptionButton>(One<VesselPanel>(screen)).Single(p=>p.Name=="VesselPicker"),8);await Frames();Press(Button(One<StarPanel>(screen),"Select planet 3"));await Frames();
        Check(!Button(One<VesselPanel>(screen),"Found colony").Disabled,"Synthetic unoccupied world remains selected beside owned colonies");await Reach(Button(One<VesselPanel>(screen),"Found colony"));Press(Button(One<VesselPanel>(screen),"Found colony"));await Frames();Check(session.State.FindVessel(new(8))!.PendingFounding==new PlanetId(9),"Synthetic founding order targets exact same-system world");
        await Capture("synthetic-same-system-world");RemoveChild(screen);screen.QueueFree();await Frames();
        async Task<string> HiddenTree(GameState fixture)
        {
            var ui=new GameScreen(new GameSession(content,fixture),new(1));AddChild(ui);await Frames();One<GalaxyMap>(ui).Select(new(10));await Frames();
            var text=string.Join("\n",Descendants<Control>(ui).Where(c=>c.IsVisibleInTree()).Select(c=>c switch {Label l=>$"L:{l.Text}|{l.TooltipText}",OptionButton b=>$"O:{string.Join(",",Enumerable.Range(0,b.ItemCount).Select(i=>b.GetItemText(i)+":"+b.GetItemMetadata(i).ToString()))}",Button b=>$"B:{b.Text}|{b.Disabled}|{b.TooltipText}",_=>string.Empty}));
            RemoveChild(ui);ui.QueueFree();await Frames();return text;
        }
        var a=SyntheticFixture();var b=a.Clone();b.Planets.Single(p=>p.Id==new PlanetId(11)).PlanetTypeId="crystal_world";b.Empires.Add(new EmpireState{Id=new(12),Controller=ControllerKind.Ai});b.Planets.Add(new PlanetState{Id=new(13),StarId=new(10),Orbit=2,PlanetTypeId="barren_rock"});b.Colonies.Add(new ColonyState{Id=new(14),EmpireId=new(12),PlanetId=new(11),Population=2,Workforce=new(2,0,0)});b.Vessels.Add(new VesselState{Id=new(15),EmpireId=new(12),RoleId="settler",CarriedPopulation=2,StationaryStarId=new(10),PendingMove=new(2)});b.NextEntityId=16;ObservationUpdater.Refresh(b,0);
        Check(GameStateValidator.Validate(content,b).Count==0,"Changed hidden synthetic truth remains valid");Check(await HiddenTree(a)==await HiddenTree(b),"Hidden world counts/types/ownership/cargo/orders do not alter visible UI text actions or tooltips");
    }
    private async Task NondefaultContentInstructions()
    {
        var original=FrozenExpansionContent();
        var content=original with { Vessels=original.Vessels.SetItem("settler",original.Vessels["settler"] with { EmbarkPopulation=3,MinimumSourcePopulation=5 }) };
        var state=SyntheticFixture();state.Vessels.Single(v=>v.RoleId=="settler").CarriedPopulation=3;state.Colonies[0].ProjectKind="vessel";state.Colonies[0].ProjectId="settler";state.Colonies[0].ProductionReserve=2400;
        Check(GameStateValidator.Validate(content,state).Count==0,"Nondefault embedded-content fixture is valid");
        var screen=new GameScreen(new GameSession(content,state),new(1));AddChild(screen);await Frames();
        Check(Descendants<Label>(One<ColonyPanel>(screen)).Any(l=>l.Text=="Awaiting embarkation: requires population 5 and 3 support workers. Production is retained."),"Blocked embarkation instructions use embedded minimum and cargo");
        SelectInt(Descendants<OptionButton>(One<VesselPanel>(screen)).Single(p=>p.Name=="VesselPicker"),8);await Frames();
        Check(Descendants<Label>(One<VesselPanel>(screen)).Any(l=>l.Text.StartsWith("Founding transfers the cargo into population 3, all in support.",StringComparison.Ordinal)),"Founding preview uses actual owned cargo");
        await Capture("nondefault-content-instructions");RemoveChild(screen);screen.QueueFree();await Frames();
    }
    private async Task LegacyFixtures()
    {
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();var setup=One<NewGameScreen>(main);setup.Configure("small",42);setup.Start();await Frames();
        foreach(int schema in new[]{1,2})
        {
            string name=$"qa-{_runId}-expansion-legacy-v{schema}";string path=SaveFiles.PathFor(name);File.Copy(ProjectSettings.GlobalizePath($"res://tests/OpenAntares.Simulation.Tests/Fixtures/legacy-v{schema}-save.json"),path,false);_savePaths.Add(path);var expected=SaveGame.ReadFile(path).Game!;
            var screen=await LoadThroughDialog(main,name);Check(Snapshot(Session(screen))==SaveGame.Serialize(expected.Content,expected.State),$"Legacy schema {schema} UI load preserves original content/state");Check(Descendants<Label>(screen).Any(l=>l.Visible&&l.Text.StartsWith("Legacy colony game",StringComparison.Ordinal)),$"Legacy schema {schema} banner");Check(!One<VesselPanel>(screen).Visible,$"Legacy schema {schema} expansion controls absent");await Capture($"legacy-v{schema}-expansion-build");await EndTurn(screen);One<TurnReportDialog>(screen).Hide();Check(Session(screen).State.Turn==expected.State.Turn+1,$"Legacy schema {schema} continues old loop");
        }
        RemoveChild(main);main.QueueFree();await Frames();
    }
    private async Task Reach(Button button)
    {UiNavigation.Reveal(button);
        button.GrabFocus();await Frames();Node? n=button.GetParent();while(n is not null && n is not ScrollContainer)n=n.GetParent();
        if(n is ScrollContainer scroll)Check(scroll.GetGlobalRect().Intersects(button.GetGlobalRect()),$"Action reachable by focus/scroll: {button.Text}");
    }
    private static void SelectInt(OptionButton picker,int id)
    {UiNavigation.Reveal(picker);
        int index=Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsInt32()==id);picker.Select(index);picker.EmitSignal(OptionButton.SignalName.ItemSelected,index);
    }
    private static ContentSet FrozenExpansionContent() => SaveGame.ReadFile(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/legacy-rules2-v3-save.json")).Game!.Content;

}
