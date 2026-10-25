using System;
using System.IO;
using System.Collections.Immutable;
using System.Collections.Generic;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Movement;
using OpenAntares.Game;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
namespace OpenAntares.GameValidation;

public partial class SpeciesGraphicalValidation : GraphicalValidation
{
    private readonly HashSet<string> _sulanModes = new(StringComparer.Ordinal);
    public override async void _Ready()
    {
        string? error=null;
        try
        {
            _output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output=",StringComparison.Ordinal))[20..];Directory.CreateDirectory(_output);
            Check(DisplayServer.GetName()!="headless","Species validation requires a graphical display");
            await DefaultSetup();
            await AlternativeContent();
            foreach(string species in new[]{"peldra","harl","sulan","wenth"})
                foreach(string size in new[]{"small","medium","large"}) await GeneratedLoop(species,size);
            await GeneratedLoop("sulan","small",forceHarbor:true);
            await SulanChoiceFixture();
            await ExtendedWenthFixture();
        }
        catch(Exception ex){error=ex.ToString();GD.PrintErr(error);}
        finally
        {
            if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{runId=_runId,scenario="Species",method="Actual controls/signals/commands; no physical OS input or human-comprehension claim.",passed=error is null,error,results=_results,retainedQaSavePaths=_savePaths},new JsonSerializerOptions{WriteIndented=true}));
            GetTree().Quit(error is null?0:1);
        }
    }
    private async Task DefaultSetup()
    {
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();
        var setup=One<NewGameScreen>(main);var picker=Descendants<OptionButton>(setup).FirstOrDefault(p=>p.Name=="SpeciesPicker");
        Check(picker is not null && picker.ItemCount==4,"New Game exposes four species choices");
        Check(picker!.GetSelectedMetadata().AsString()=="harl","Harl is visibly selected by default");
        Check(Descendants<Label>(setup).Any(l=>l.Name=="SpeciesSummary" && l.Text.Length>60),"Starting tradeoffs and flavor appear before Start");
        Press(Button(setup,"Start"));await Frames();var screen=One<GameScreen>(main);var session=Session(screen);
        Check(session.State.RulesVersion==7 && session.State.Crisis!.Enabled && session.State.Scouting!.Enabled && session.State.Empires[0].SpeciesId=="harl","Actual current Start launches a rules7 Standard plague/ameba Harl campaign");
        await Capture("default-harl-start");RemoveChild(main);main.QueueFree();await Frames();
    }
    private async Task AlternativeContent()
    {
        var core=ContentFiles.LoadCore().Content!;
        var alternative=core with {Species=core.Species.Remove("harl")};
        var setup=new NewGameScreen(alternative){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(setup);await Frames();
        var picker=Descendants<OptionButton>(setup).Single(p=>p.Name=="SpeciesPicker");
        Check(picker.ItemCount==3 && picker.GetSelectedMetadata().AsString()=="peldra","Valid content without Harl selects the ordinal first species");
        NewGameSettings? selected=null;setup.StartRequested+=settings=>selected=settings;setup.Configure("small",42);Press(Button(setup,"Start"));await Frames();
        Check(selected is {SpeciesId:"peldra",RulesVersion:7,CrisesEnabled:true},"Alternative-content Start passes its actual species identity and current Standard rules");
        RemoveChild(setup);setup.QueueFree();await Frames();
        var empty=new NewGameScreen(core with{Species=ImmutableSortedDictionary<string,SpeciesDefinition>.Empty}){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(empty);await Frames();
        Check(Button(empty,"Start").Disabled,"Missing species disables New Game");bool load=false;empty.LoadRequested+=()=>load=true;Press(Button(empty,"Load Game"));Check(load,"Missing species still permits Load Game");
        RemoveChild(empty);empty.QueueFree();await Frames();
        var edited=core with{Vessels=core.Vessels.SetItem("harbor_settler",core.Vessels["harbor_settler"] with{Name="Harbor Ark",EmbarkPopulation=3,MinimumSourcePopulation=5})};
        var state=GalaxyGenerator.CreateNewGame(edited,new("small",42,3,"sulan")).State!;
        var target=state.Planets.First(p=>edited.PlanetTypes[p.PlanetTypeId].Colonizable && state.Colonies.All(c=>c.PlanetId!=p.Id));
        var ship=new VesselState{Id=new(state.AllocateId()),EmpireId=state.Empires[0].Id,RoleId="harbor_settler",CarriedPopulation=3,StationaryStarId=target.StarId};state.Vessels.Add(ship);
        state.Colonies[0].ProjectKind="vessel";state.Colonies[0].ProjectId="harbor_settler";state.Colonies[0].ProductionReserve=3600;ObservationUpdater.Refresh(state,0);
        Check(GameStateValidator.Validate(edited,state).Count==0,"Labelled alternative-content UI fixture is structurally valid");
        var screen=new GameScreen(new GameSession(edited,state),state.Empires[0].Id){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(screen);await Frames();
        var projects=Descendants<OptionButton>(One<ColonyPanel>(screen)).Single();
        Check(Enumerable.Range(0,projects.ItemCount).Any(i=>projects.GetItemText(i).StartsWith("Harbor Ark",StringComparison.Ordinal) && projects.GetItemTooltip(i).Contains("population 5",StringComparison.Ordinal)),"Role picker uses edited names and requirements");
        Check(Descendants<Label>(One<ColonyPanel>(screen)).Any(l=>l.Text=="Support workers for cargo: 2 / 3."),"Blocked conditions use edited cargo and actual support workers");
        SelectNumber(Descendants<OptionButton>(One<VesselPanel>(screen)).Single(p=>p.Name=="VesselPicker"),ship.Id.Value);await Frames();One<GalaxyMap>(screen).Select(target.StarId);await Frames();Press(Button(One<StarPanel>(screen),$"Select planet {target.Orbit}"));await Frames();
        Check(Descendants<Label>(One<VesselPanel>(screen)).Any(l=>l.Text.Contains("3 population",StringComparison.Ordinal)),"Founding description uses edited carried population");
        Check(!Button(One<VesselPanel>(screen),"Found colony").Disabled,"Prepared expedition eligibility comes from its shared permitted preview");
        await Capture("alternative-content-harbor");RemoveChild(screen);screen.QueueFree();await Frames();
        foreach(string kind in new[]{"production","research"})
        {
            var profile=core.Species["harl"] with{BaseCapacity=100,StartingPopulation=100,StartingWorkforce=kind=="production"?new(20,80,0):new(20,0,80),ProductionBasisPoints=5000,ResearchBasisPoints=5000};
            var world=core.PlanetTypes["verdant_world"] with{SupportPerWorker=500,ProductionPerWorker=kind=="production"?153722867280912930:200,ResearchPerWorker=kind=="research"?153722867280912930:200};
            var reduced=core with{Species=core.Species.SetItem("harl",profile),PlanetTypes=core.PlanetTypes.SetItem("verdant_world",world)};
            var initial=GalaxyGenerator.CreateNewGame(reduced,new("small",42,3,"harl")).State!;
            Check(GameStateValidator.Validate(reduced,initial).Count==0,"Reduced "+kind+": alternative-content UI state validates");
            var ui=new GameScreen(new GameSession(reduced,initial),initial.Empires[0].Id){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(ui);await Frames();
            Check(Descendants<Control>(ui).Any(c=>c.TooltipText.Contains("80 workers after Harl multiplier",StringComparison.Ordinal)),"Reduced "+kind+": combined worker contribution has a truthful tooltip label");
            RemoveChild(ui);ui.QueueFree();await Frames();
        }
    }
    private static void SelectNumber(OptionButton picker,int id)
    {
        int index=Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsInt32()==id);picker.Select(index);picker.EmitSignal(OptionButton.SignalName.ItemSelected,index);
    }

    private async Task GeneratedLoop(string species,string size,bool forceHarbor=false)
    {
        string label=$"species-{species}-{size}"+(forceHarbor?"-harbor":string.Empty);
        GetTree().Root.Size=new(1920,1080);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();
        var setup=One<NewGameScreen>(main);setup.Configure(size,42);Select(Descendants<OptionButton>(setup).Single(p=>p.Name=="SpeciesPicker"),species);await Frames();
        if(size=="small" && !forceHarbor)
        {
            GetTree().Root.Size=new(800,600);await Frames();Check(InViewport(Button(setup,"Start")) && InViewport(Descendants<OptionButton>(setup).Single(p=>p.Name=="SpeciesPicker")),label+": setup controls fit the smaller window");
            await Capture(label+"-setup-800");GetTree().Root.Size=new(1920,1080);await Frames();
        }
        Press(Button(setup,"Start"));await Frames();
        var frozen=GalaxyGenerator.CreateNewGame(FrozenContent.Rules3(),new(size,42,3,species));
        string frozenName=$"qa-{_runId}-{label}-rules3";string frozenPath=SaveFiles.PathFor(frozenName);Check(!File.Exists(frozenPath),label+": frozen save is unique");SaveGame.WriteFile(frozenPath,FrozenContent.Rules3(),frozen.State!);_savePaths.Add(frozenPath);
        var screen=await LoadThroughDialog(main,frozenName);var session=Session(screen);var viewer=session.State.Empires[0].Id;var source=session.State.Colonies[0].Id;
        Check(session.State.RulesVersion==3 && session.State.Empires[0].SpeciesId==species,label+": actual selected species starts");
        var opening=session.View(viewer);var scout=opening.Vessels.Single();var home=opening.Stars.Single(s=>s.Chart.Id==scout.StationaryStarId);
        var target=opening.Stars.Where(s=>s.Chart.Id!=home.Chart.Id).OrderByDescending(s=>TravelCalculator.Duration(home.Chart.X,home.Chart.Y,s.Chart.X,s.Chart.Y,300)).ThenBy(s=>s.Chart.Id).First();
        One<GalaxyMap>(screen).Select(target.Chart.Id);await Frames();
        Check(Descendants<Label>(One<StarPanel>(screen)).Any(l=>l.Text=="Not surveyed"),label+": unknown planet/environment facts remain hidden");
        await Reach(Button(One<VesselPanel>(screen),"Set course"));Press(Button(One<VesselPanel>(screen),"Set course"));await Frames();
        string pending=await SaveThroughDialog(screen,label+"-pending-move");screen=await LoadThroughDialog(main,pending);session=Session(screen);
        Check(session.State.FindVessel(scout.Id)!.PendingMove==target.Chart.Id,label+": queued route survives UI reload");
        await EndTurn(screen);One<TurnReportDialog>(screen).Hide();
        if(session.State.FindVessel(scout.Id)!.Transit is not null)
        {
            Check(Button(One<VesselPanel>(screen),"Set course").Disabled,label+": committed transit controls disabled");
            string transit=await SaveThroughDialog(screen,label+"-transit");screen=await LoadThroughDialog(main,transit);session=Session(screen);
        }
        for(int guard=0;session.State.FindVessel(scout.Id)!.Transit is not null && guard<20;guard++){await EndTurn(screen);One<TurnReportDialog>(screen).Hide();}
        Check(session.State.FindVessel(scout.Id)!.StationaryStarId==target.Chart.Id,label+": scout arrives and surveys");
        var surveyed=session.View(viewer).Stars.Single(s=>s.Chart.Id==target.Chart.Id).Planets!.Value.Where(p=>p.Colonizable && !p.OwnerId.HasValue).ToArray();
        Check(surveyed.Length>0,label+": surveyed target offers a colonizable world");
        var planet=species=="sulan" && !forceHarbor ? surveyed.FirstOrDefault(p=>p.EnvironmentId=="shelf_sea") ?? surveyed[0] : surveyed[0];
        string role=species switch{"peldra"=>"peldra_settler","harl"=>"harl_settler","wenth"=>"daughter_seed",_=>!forceHarbor && planet.EnvironmentId=="shelf_sea"?"settler":"harbor_settler"};
        One<GalaxyMap>(screen).Select(home.Chart.Id);await Frames();
        var projects=Descendants<OptionButton>(One<ColonyPanel>(screen)).Single();await Reach(projects);Select(projects,"vessel|"+role);await Frames();
        Check(session.State.FindColony(source)!.ProjectId==role,label+": role selected through actual project control");
        if(species=="wenth" && session.Forecast().Colony(source).Production.BlockedReasonCode is not null)
        {
            Check(Descendants<Label>(One<ColonyPanel>(screen)).Any(l=>l.Text.StartsWith("Full colony required:",StringComparison.Ordinal)),label+": Wenth maturity is explained");
            string banked=await SaveThroughDialog(screen,label+"-immature-seed");screen=await LoadThroughDialog(main,banked);session=Session(screen);
            Check(session.State.FindColony(source)!.ProjectId==role,label+": banked immature target remains valid on load");
        }
        bool complete=false;
        for(int guard=0;guard<80 && !complete;guard++)
        {
            var forecast=session.Forecast().Colony(source);int idBefore=session.State.NextEntityId;
            await EndTurn(screen);
            Check(session.State.FindColony(source)!.ProductionReserve==forecast.Production.Ending,label+$": forecast production matches at turn{session.State.Turn}");
            complete=session.State.Vessels.Any(v=>v.RoleId==role);
            if(forecast.Production.BlockedReasonCode is not null)Check(!complete && session.State.NextEntityId==idBefore,label+": blocked project banks without ship or allocated ID");
            if(complete && forecast.EmbarkOutcome?.ReassignmentNeeded==true)Check(Descendants<Label>(One<TurnReportDialog>(screen)).Any(l=>l.Text.Contains("Reassign workers",StringComparison.Ordinal)),label+": report warns of post-embark support");
            One<TurnReportDialog>(screen).Hide();
        }
        Check(complete,label+": expedition completes within the core budget");
        var founder=session.View(viewer).Vessels.Single(v=>v.RoleId==role);int cargo=founder.CarriedPopulation;
        Check(founder.PendingMove is null && founder.PendingFounding is null && founder.Transit is null,label+": new vessel waits for another planning order");
        if(species=="wenth")Check(session.State.FindColony(source)!.Population==3 && cargo==3,label+": full six-lobe body becomes parent3 plus seed3");
        if(species=="peldra")Check(session.Forecast().Colony(source).SupportDeficit>0,label+": Peldra source shows its actual support tradeoff");
        One<GalaxyMap>(screen).Select(home.Chart.Id);await Frames();
        for(int guard=0;session.Forecast().Colony(source).SupportDeficit>0 && guard<10;guard++)
        {
            var colony=session.State.FindColony(source)!;string tip=colony.Workforce.Research>0?"Move a worker from research to support":"Move a worker from production to support";
            var step=Step(One<ColonyPanel>(screen),tip);await Reach(step);Press(step);await Frames();
        }
        Check(session.Forecast().Colony(source).SupportDeficit==0,label+": actual workforce controls repair source support");
        SelectNumber(Descendants<OptionButton>(One<VesselPanel>(screen)).Single(p=>p.Name=="VesselPicker"),founder.Id.Value);await Frames();One<GalaxyMap>(screen).Select(target.Chart.Id);await Frames();await Reach(Button(One<VesselPanel>(screen),"Set course"));Press(Button(One<VesselPanel>(screen),"Set course"));await Frames();
        int travel=TravelCalculator.Duration(home.Chart.X,home.Chart.Y,target.Chart.X,target.Chart.Y,session.Content.Vessels[role].Speed);
        for(int t=0;t<travel;t++){await EndTurn(screen);One<TurnReportDialog>(screen).Hide();}
        Check(session.State.FindVessel(founder.Id)!.StationaryStarId==target.Chart.Id && session.State.Colonies.Count==1,label+": exact travel arrives without automatic founding");
        One<GalaxyMap>(screen).Select(target.Chart.Id);await Frames();var world=Button(One<StarPanel>(screen),$"Select planet {planet.Orbit}");await Reach(world);Press(world);await Frames();
        var found=Button(One<VesselPanel>(screen),"Found colony");Check(!found.Disabled,label+": shared permitted eligibility enables founding");await Reach(found);Press(found);await Frames();
        string intent=await SaveThroughDialog(screen,label+"-founding");screen=await LoadThroughDialog(main,intent);session=Session(screen);
        Check(session.State.FindVessel(founder.Id)!.PendingFounding==planet.Id,label+": explicit founding intent survives reload");
        await EndTurn(screen);var report=One<TurnReportDialog>(screen);Check(session.State.Colonies.Count==2 && session.State.FindVessel(founder.Id) is null,label+": founding creates one colony and consumes exactly its vessel");report.Hide();
        var child=session.State.Colonies.Single(c=>c.PlanetId==planet.Id);Check(child.Population==cargo && child.Workforce==new Workforce(cargo,0,0) && child.GrowthProgress==0 && child.ProductionReserve==0,label+": daughter starts with conserved all-support cargo and no founding-turn economy");
        if(species=="sulan"){_sulanModes.Add(child.HabitatMode!);Check(child.HabitatMode==(role=="harbor_settler"?"sealed":"native"),label+": actual founded habitat mode");}
        if(species=="harl"){Check(child.Population==1,label+": Harl launches a one-unit daughter");for(int t=0;session.State.FindColony(child.Id)!.Population<2 && t<10;t++){await EndTurn(screen);One<TurnReportDialog>(screen).Hide();}Check(session.State.FindColony(child.Id)!.Population>=2,label+": Harl daughter recovers through real economic turns");}
        if(species=="wenth")Check(session.Forecast().Colony(child.Id).Growth.Capacity==6,label+": Wenth daughter has unbuilt capacity6");
        SelectNumber(Descendants<OptionButton>(One<ColonyList>(screen)).Single(),source.Value);await Frames();Select(Descendants<OptionButton>(One<ColonyPanel>(screen)).Single(),"vessel|scout");await Frames();
        SelectNumber(Descendants<OptionButton>(One<ColonyList>(screen)).Single(),child.Id.Value);await Frames();var researchStep=Step(One<ColonyPanel>(screen),"Move a worker from support to research");await Reach(researchStep);Press(researchStep);await Frames();Select(Descendants<OptionButton>(One<ColonyPanel>(screen)).Single(),"fabrication_hall");await Frames();
        Check(session.State.FindColony(source)!.ProjectId=="scout" && session.State.FindColony(child.Id)!.ProjectId=="fabrication_hall",label+": exact colony controls keep independent typed projects");
        var aggregate=session.Forecast();Check(aggregate.Empire(viewer).NetResearch.Total==aggregate.Colonies.Sum(c=>c.NetResearch.Total) && aggregate.Colony(child.Id).NetResearch.Total>0,label+": both owned colonies contribute once to shared research");
        string snapshot=Snapshot(session);
        foreach(var sizePixels in new[]{new Vector2I(800,600),new Vector2I(1152,648),new Vector2I(1280,720),new Vector2I(1600,900),new Vector2I(1920,1080)})
        {
            GetTree().Root.Size=sizePixels;await Frames();Check(Snapshot(session)==snapshot,label+$": resize{sizePixels.X}x{sizePixels.Y} preserves authoritative state");
            var picker=Descendants<OptionButton>(One<ColonyPanel>(screen)).Single();await Reach(picker);Check(InViewport(picker) && InViewport(Button(screen,"End Turn")),label+$": critical controls remain in bounds at{sizePixels.X}x{sizePixels.Y}");
            if(size=="small" && sizePixels.X==800 && !forceHarbor)await Capture(label+"-two-colonies-800");
        }
        await Capture(label+"-two-colonies-1920");
        var expected=new GameSession(session.Content,session.State.Clone());Check(expected.Execute(new SetReadyCommand(viewer,expected.State.Turn,true)).Accepted,label+": uninterrupted reference continuation resolves");
        string save=await SaveThroughDialog(screen,label+"-two-colonies");screen=await LoadThroughDialog(main,save);await EndTurn(screen);Check(Snapshot(Session(screen))==Snapshot(expected),label+": UI reload continuation equals uninterrupted play");One<TurnReportDialog>(screen).Hide();
        if(species=="wenth" && size=="large")await CheckInvalidAndLegacyLoad(main,screen);
        RemoveChild(main);main.QueueFree();await Frames();
    }
    private bool InViewport(Control control) => GetViewport().GetVisibleRect().Encloses(control.GetGlobalRect());
    private async Task Reach(Control control)
    {
        bool alreadyFocused=control.HasFocus();control.GrabFocus();await Frames();Node? parent=control.GetParent();while(parent is not null && parent is not ScrollContainer)parent=parent.GetParent();
        if(parent is ScrollContainer scroll)
        {
            if(control is Button {Disabled:true}) {scroll.EnsureControlVisible(control);await Frames();}
            if(!scroll.GetGlobalRect().Intersects(control.GetGlobalRect()))
            {
                _results.Add(new { name="Focus resize diagnostic",passed=false,alreadyFocused,focusAfter=control.HasFocus(),controlRect=control.GetGlobalRect().ToString(),scrollRect=scroll.GetGlobalRect().ToString(),scrollVertical=scroll.ScrollVertical,window=GetTree().Root.Size.ToString() });
                await Capture("focus-resize-failure");control.ReleaseFocus();control.GrabFocus();await Frames();
                _results.Add(new { name="Explicit refocus diagnostic",passed=scroll.GetGlobalRect().Intersects(control.GetGlobalRect()),controlRect=control.GetGlobalRect().ToString(),scrollVertical=scroll.ScrollVertical });
                Check(false,"Already-focused construction picker stays visible through resize");
            }
            if(alreadyFocused)Check(scroll.GetGlobalRect().Intersects(control.GetGlobalRect()),"Already-focused control remains visible after resize");
            Check(scroll.GetGlobalRect().Intersects(control.GetGlobalRect()),"Focus/scroll reaches "+control.GetType().Name);
        }
    }
    private static GameState HabitatFixture(bool reversed)
    {
        var state=new GameState{RulesVersion=3,NextEntityId=12};
        state.Empires.Add(new EmpireState{Id=new(1),Controller=ControllerKind.Human,SpeciesId="sulan"});
        state.Stars.Add(new StarState{Id=new(2),Name="Labelled Habitat Home",X=100,Y=100});
        state.Stars.Add(new StarState{Id=new(6),Name="Labelled Unknown",X=500,Y=100});
        state.Planets.Add(new PlanetState{Id=new(3),StarId=new(2),Orbit=1,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea"});
        state.Planets.Add(new PlanetState{Id=new(7),StarId=new(6),Orbit=1,PlanetTypeId="forge_world",EnvironmentId="dry"});
        state.Planets.Add(new PlanetState{Id=new(8),StarId=new(2),Orbit=2,PlanetTypeId="verdant_world",EnvironmentId=reversed?"shelf_sea":"dry"});
        state.Planets.Add(new PlanetState{Id=new(9),StarId=new(2),Orbit=3,PlanetTypeId="forge_world",EnvironmentId=reversed?"dry":"shelf_sea"});
        state.Colonies.Add(new ColonyState{Id=new(4),EmpireId=new(1),PlanetId=new(3),Population=6,Workforce=new(2,2,2),HabitatMode="native"});
        state.Vessels.Add(new VesselState{Id=new(5),EmpireId=new(1),RoleId="scout",StationaryStarId=new(2)});
        state.Vessels.Add(new VesselState{Id=new(10),EmpireId=new(1),RoleId="settler",CarriedPopulation=2,StationaryStarId=new(2)});
        state.Vessels.Add(new VesselState{Id=new(11),EmpireId=new(1),RoleId="harbor_settler",CarriedPopulation=2,StationaryStarId=new(2)});
        ObservationUpdater.Refresh(state,0);return state;
    }
    private async Task<(Control Main,GameScreen Screen)> LoadFixture(GameState state,string label)
    {
        var content=FrozenContent.Rules3();var errors=GameStateValidator.Validate(content,state);
        Check(errors.Count==0,label+": labelled initial fixture validates: "+string.Join(";",errors));
        string name=$"qa-{_runId}-labelled-{label}";string path=SaveFiles.PathFor(name);Check(!File.Exists(path),label+": QA path is new");
        SaveGame.WriteFile(path,content,state);_savePaths.Add(path);
        GetTree().Root.Size=new(1920,1080);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();
        Press(Button(One<NewGameScreen>(main),"Start"));await Frames();return(main,await LoadThroughDialog(main,name));
    }
    private async Task SulanChoiceFixture()
    {
        foreach(bool reversed in new[]{false,true})
        {
            string label="sulan-contrasting-"+(reversed?"b":"a");var loaded=await LoadFixture(HabitatFixture(reversed),label);var screen=loaded.Screen;var session=Session(screen);
            int shelf=reversed?8:9,dry=reversed?9:8;
            SelectNumber(Descendants<OptionButton>(One<VesselPanel>(screen)).Single(p=>p.Name=="VesselPicker"),10);await Frames();
            Press(Button(One<StarPanel>(screen),$"Select planet {session.State.FindPlanet(new(dry))!.Orbit}"));await Frames();
            Check(Button(One<VesselPanel>(screen),"Found colony").Disabled,label+": native expedition cannot found on dry ground");
            await Reach(Button(One<VesselPanel>(screen),"Found colony"));await Capture(label+"-native-blocked");
            Press(Button(One<StarPanel>(screen),$"Select planet {session.State.FindPlanet(new(shelf))!.Orbit}"));await Frames();
            Check(!Button(One<VesselPanel>(screen),"Found colony").Disabled,label+": native expedition can choose the shelf world");
            Press(Button(One<VesselPanel>(screen),"Found colony"));await Frames();await EndTurn(screen);One<TurnReportDialog>(screen).Hide();
            var native=session.State.Colonies.Single(c=>c.PlanetId==new PlanetId(shelf));
            Check(native.HabitatMode=="native" && session.Forecast().Colony(native.Id).Growth.Capacity==12 && native.GrowthProgress==0,label+": native daughter has capacity12 and waits for next snapshot");_sulanModes.Add("native");
            SelectNumber(Descendants<OptionButton>(One<VesselPanel>(screen)).Single(p=>p.Name=="VesselPicker"),11);await Frames();
            Press(Button(One<StarPanel>(screen),$"Select planet {session.State.FindPlanet(new(dry))!.Orbit}"));await Frames();
            Check(!Button(One<VesselPanel>(screen),"Found colony").Disabled,label+": harbor expedition can choose the dry world");
            Press(Button(One<VesselPanel>(screen),"Found colony"));await Frames();await EndTurn(screen);One<TurnReportDialog>(screen).Hide();
            var sealedColony=session.State.Colonies.Single(c=>c.PlanetId==new PlanetId(dry));
            Check(sealedColony.HabitatMode=="sealed" && session.Forecast().Colony(sealedColony.Id).Growth.Capacity==8 && sealedColony.GrowthProgress==0,label+": harbor daughter has capacity8 and waits for next snapshot");_sulanModes.Add("sealed");
            Check(session.State.FindPlanet(new(dry))!.EnvironmentId=="dry" && session.State.FindPlanet(new(shelf))!.EnvironmentId=="shelf_sea",label+": founding preserves physical environments");
            Check(session.State.Colonies.Count==3 && session.State.Colonies.Sum(c=>c.Population)==10 && session.State.Vessels.All(v=>v.CarriedPopulation==0),label+": both actual founding orders conserve the initial ten units");
            await Capture(label+"-both-daughters");RemoveChild(loaded.Main);loaded.Main.QueueFree();await Frames();
        }
        Check(_sulanModes.SetEquals(new[]{"native","sealed"}),"Both Sulan habitat modes executed");
        var content=FrozenContent.Rules3();var a=HabitatFixture(false);var b=a.Clone();
        b.Planets.Single(p=>p.Id==new PlanetId(7)).EnvironmentId="open_ocean";
        b.Empires.Add(new EmpireState{Id=new(12),Controller=ControllerKind.Ai,SpeciesId="wenth",ResearchReserve=long.MaxValue});
        b.Planets.Add(new PlanetState{Id=new(13),StarId=new(6),Orbit=2,PlanetTypeId="crystal_world",EnvironmentId="shelf_sea"});
        b.Colonies.Add(new ColonyState{Id=new(14),EmpireId=new(12),PlanetId=new(13),Population=3,Workforce=new(1,1,1),HabitatMode="native",ProductionReserve=long.MaxValue});
        b.Vessels.Add(new VesselState{Id=new(15),EmpireId=new(12),RoleId="daughter_seed",CarriedPopulation=3,StationaryStarId=new(6)});b.NextEntityId=16;ObservationUpdater.Refresh(b,0);
        Check(GameStateValidator.Validate(content,b).Count==0,"Changed hidden species fixture remains structurally valid");
        async Task<string> HiddenTree(GameState fixture)
        {
            var ui=new GameScreen(new GameSession(content,fixture),new(1)){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(ui);await Frames();One<GalaxyMap>(ui).Select(new(6));await Frames();
            string text=string.Join("\n",Descendants<Control>(ui).Where(c=>c.IsVisibleInTree()).Select(c=>c switch{Label l=>$"L:{l.Text}|{l.TooltipText}",OptionButton o=>$"O:{string.Join(",",Enumerable.Range(0,o.ItemCount).Select(i=>o.GetItemText(i)+":"+o.GetItemMetadata(i)+":"+o.GetItemTooltip(i)))}",Button button=>$"B:{button.Text}|{button.Disabled}|{button.TooltipText}",_=>string.Empty}));
            RemoveChild(ui);ui.QueueFree();await Frames();return text;
        }
        Check(await HiddenTree(a)==await HiddenTree(b),"Hidden environments/species/cargo/overflowing foreign economy do not change own visible text actions or tooltips");
    }
    private async Task ExtendedWenthFixture()
    {
        var state=HabitatFixture(false);state.Empires[0].SpeciesId="wenth";state.Empires[0].KnownTechnologyIds.Add("habitat_methods");
        state.Planets.RemoveAll(p=>p.Id==new PlanetId(9));state.Vessels.RemoveAll(v=>v.Id.Value>5);state.NextEntityId=9;
        var source=state.Colonies[0];source.Workforce=new(4,1,1);source.CompletedBuildingIds.Add("habitat_extension");source.ProjectKind="vessel";source.ProjectId="daughter_seed";source.ProductionReserve=2400;
        ObservationUpdater.Refresh(state,0);var loaded=await LoadFixture(state,"wenth-extended");var screen=loaded.Screen;var session=Session(screen);
        Check(session.Forecast().Colony(new(4)).Growth.Capacity==8,"Extended Wenth initial full-capacity threshold is eight");
        Check(Descendants<Label>(One<ColonyPanel>(screen)).Any(l=>l.Text.Contains("Full colony required: 6 / 8",StringComparison.Ordinal)),"Extended Wenth UI explains the actual maturity threshold");
        for(int guard=0;!session.State.Vessels.Any(v=>v.RoleId=="daughter_seed") && guard<10;guard++)
        {
            int population=session.State.Colonies[0].Population;int nextId=session.State.NextEntityId;var forecast=session.Forecast().Colony(new(4));
            await EndTurn(screen);One<TurnReportDialog>(screen).Hide();
            Check(session.State.Colonies[0].ProductionReserve==forecast.Production.Ending,"Extended Wenth bank follows actual snapshot forecast");
            if(population<8)Check(session.State.Vessels.Count==1 && session.State.NextEntityId==nextId,"Extended Wenth cannot complete on a growth-to-maturity snapshot");
        }
        var seed=session.State.Vessels.Single(v=>v.RoleId=="daughter_seed");
        Check(session.State.Colonies[0].Population==5 && seed.CarriedPopulation==3,"Extended Wenth conserves eight as parent5 plus seed3");
        SelectNumber(Descendants<OptionButton>(One<VesselPanel>(screen)).Single(p=>p.Name=="VesselPicker"),seed.Id.Value);await Frames();
        Press(Button(One<StarPanel>(screen),"Select planet 2"));await Frames();Check(!Button(One<VesselPanel>(screen),"Found colony").Disabled,"Extended Wenth seed can found on the surveyed dry world");
        Press(Button(One<VesselPanel>(screen),"Found colony"));await Frames();await EndTurn(screen);One<TurnReportDialog>(screen).Hide();
        var child=session.State.Colonies.Single(c=>c.PlanetId==new PlanetId(8));
        Check(child.Population==3 && child.Workforce==new Workforce(3,0,0) && child.CompletedBuildingIds.Count==0 && child.GrowthProgress==0,"Extended Wenth daughter starts with three all-support lobes and no inherited building or economy");
        Check(session.Forecast().Colony(child.Id).Growth.Capacity==6 && session.Forecast().Colony(new(4)).Growth.Capacity==8,"Extended Wenth daughter capacity6 remains independent of parent capacity8");
        SelectNumber(Descendants<OptionButton>(One<ColonyList>(screen)).Single(),child.Id.Value);await Frames();await Capture("wenth-extended-daughter");
        string save=await SaveThroughDialog(screen,"wenth-extended-daughter");screen=await LoadThroughDialog(loaded.Main,save);Check(Session(screen).Forecast().Colony(child.Id).Growth.Capacity==6,"Extended daughter capacity survives the real save/load dialogs");
        RemoveChild(loaded.Main);loaded.Main.QueueFree();await Frames();
    }

}
