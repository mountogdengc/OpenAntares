using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.Turns;
namespace OpenAntares.GameValidation;

public partial class RivalGraphicalValidation : GraphicalValidation
{
    private readonly System.Collections.Generic.HashSet<string> _researchSpecies=new(StringComparer.Ordinal);
    public override async void _Ready()
    {
        string? error=null;
        try
        {
            _output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output=",StringComparison.Ordinal))[20..];Directory.CreateDirectory(_output);
            Check(DisplayServer.GetName()!="headless","Rival validation requires a graphical display");
            await SetupChecks();
            await InvalidDiskContentCanStillLoadCampaign();
            await ObservationUiChecks();
            await EncounterReportChecks();
            await HumanAndSoloChecks();
            await LabelledFixtures();
            await TwelveGeneratedRivalsExpandAndAreEncounteredWithin80Turns();
        }
        catch(Exception ex){error=ex.ToString();GD.PrintErr(error);}
        finally
        {
            if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{runId=_runId,scenario="Rival",method="Actual control signals and commands; no physical OS input or human-comprehension claim.",passed=error is null,error,results=_results,retainedQaSavePaths=_savePaths},new JsonSerializerOptions{WriteIndented=true}));
            GetTree().Quit(error is null?0:1);
        }
    }
    private static OptionButton Picker(Node screen,string name)=>Descendants<OptionButton>(screen).Single(p=>p.Name==name);
    private async Task InvalidDiskContentCanStillLoadCampaign()
    {
        var core=FrozenContent.Rules3();var state=GalaxyGenerator.CreateNewGame(core,new("small",42,3,"harl")).State!;
        string name=$"qa-{_runId}-invalid-disk-content-legacy";string path=SaveFiles.PathFor(name);Check(!File.Exists(path),"Invalid-content legacy QA name is unique");SaveGame.WriteFile(path,core,state);_savePaths.Add(path);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();
        GetTree().Root.Size=new(800,600);await Frames();
        var invalid=ContentLoader.Load(Enumerable.Range(0,80).Select(i=>new ContentSource($"invalid-{i}.json","{invalid json")));Check(invalid.Content is null && invalid.Errors.Count>=80,"Long invalid disk-content result comes from the real loader");
        typeof(Main).GetMethod("ShowContentErrors",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(main,new object[]{invalid});await Frames();
        Check(Descendants<Button>(main).Any(b=>b.Text=="Load Game" && b.IsVisibleInTree()),"Invalid disk content keeps Load Game available");
        Check(VisibleInWindow(Button(main,"Load Game")),"Long content-error recovery action stays inside the 800x600 viewport");
        var scroll=Descendants<ScrollContainer>(main).Single(s=>s.Name=="ContentErrorScroll");
        Check(scroll.GetVScrollBar().MaxValue>scroll.GetVScrollBar().Page,"Long content diagnostics scroll while recovery action stays visible");
        await Capture("long-content-error-recovery-800");
        Press(Button(main,"Load Game"));await Frames();var dialog=One<LoadGameDialog>(main);var list=One<ItemList>(dialog);int index=Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemText(i).StartsWith(name+"    ",StringComparison.Ordinal));list.Select(index);list.EmitSignal(ItemList.SignalName.ItemSelected,index);Press(dialog.GetOkButton());await Frames();
        Check(Snapshot(Session(One<GameScreen>(main)))==SaveGame.Serialize(core,state),"Invalid disk content still loads original embedded rules3 campaign");await Drop(main);
    }
    private async Task Drop(Node node){RemoveChild(node);node.QueueFree();await Frames();}
    private async Task SetupChecks()
    {
        var core=ContentFiles.LoadCore().Content!;
        var setup=new NewGameScreen(core){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(setup);await Frames();
        Check(Descendants<OptionButton>(setup).Any(p=>p.Name=="RivalCountPicker"),"Visible rival count selector exists");
        var count=Picker(setup,"RivalCountPicker");var human=Picker(setup,"SpeciesPicker");var rival=Picker(setup,"RivalSpeciesPicker");
        Check(count.GetSelectedMetadata().AsString()=="1" && human.GetSelectedMetadata().AsString()=="harl" && rival.GetSelectedMetadata().AsString()=="peldra","Visible defaults are One / Harl / Peldra");
        NewGameSettings? selected=null;setup.StartRequested+=s=>selected=s;setup.Configure("small",42);Press(Button(setup,"Start"));
        Check(selected is {RulesVersion:9,SentriesEnabled:true,RivalCount:1,SpeciesId:"harl",RivalSpeciesId:"peldra",CrisesEnabled:true},"Visible current defaults reach simulation unchanged");
        Select(human,"peldra");Check(rival.GetSelectedMetadata().AsString()=="peldra","Human selection does not swap rival; mirrors legal");Press(Button(setup,"Start"));Check(selected?.RivalSpeciesId=="peldra" && selected.SpeciesId=="peldra","Mirror settings reach simulation");
        Select(count,"0");Press(Button(setup,"Start"));Check(rival.Disabled && selected is {RivalCount:0,RivalSpeciesId:null},"Solo disables rival and passes null identity");
        Select(count,"1");Check(!rival.Disabled,"One enables the retained rival choice");
        foreach(var dimensions in new[]{new Vector2I(800,600),new(1152,648),new(1280,720),new(1600,900),new(1920,1080)})
        {
            GetTree().Root.Size=dimensions;await Frames();rival.GrabFocus();await Frames();
            Check(GetViewport().GuiGetFocusOwner()==rival && VisibleInWindow(rival) && VisibleInWindow(Button(setup,"Start")),"Setup controls/focus fit "+dimensions);
            await Capture("rival-setup-"+dimensions.X+"x"+dimensions.Y);
        }
        await Drop(setup);
        foreach(var entry in new[]{
            (Ids:new[]{"peldra","sulan","wenth"},Human:"peldra",Rival:"sulan"),
            (Ids:new[]{"harl","sulan","wenth"},Human:"harl",Rival:"sulan"),
            (Ids:new[]{"sulan","wenth"},Human:"sulan",Rival:"wenth"),
            (Ids:new[]{"peldra"},Human:"peldra",Rival:"peldra"),
            (Ids:new[]{"harl"},Human:"harl",Rival:"harl"),
            (Ids:new[]{"wenth"},Human:"wenth",Rival:"wenth"),
            (Ids:Array.Empty<string>(),Human:"",Rival:"")})
        {
            var ids=entry.Ids;
            var alternative=core with{Species=core.Species.Where(p=>ids.Contains(p.Key)).ToImmutableSortedDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal)};
            var ui=new NewGameScreen(alternative){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(ui);await Frames();
            if(ids.Length==0){Check(Button(ui,"Start").Disabled,"No species disables Start");bool loaded=false;ui.LoadRequested+=()=>loaded=true;Press(Button(ui,"Load Game"));Check(loaded,"No species retains Load");}
            else
            {
                Check(Picker(ui,"SpeciesPicker").GetSelectedMetadata().AsString()==entry.Human,"Alternative human defaults to "+entry.Human);
                Check(Picker(ui,"RivalSpeciesPicker").GetSelectedMetadata().AsString()==entry.Rival,"Alternative rival defaults to distinct choice or sole mirror: "+entry.Rival);
                NewGameSettings? chosen=null;ui.StartRequested+=settings=>chosen=settings;ui.Configure("small",42);Press(Button(ui,"Start"));
                Check(chosen?.SpeciesId==entry.Human && chosen.RivalSpeciesId==entry.Rival && chosen.RivalCount==1,"Actual alternative Start uses both initial identities");
                Select(Picker(ui,"SpeciesPicker"),entry.Rival);Press(Button(ui,"Start"));
                Check(chosen?.SpeciesId==entry.Rival && chosen.RivalSpeciesId==entry.Rival,"Later manual mirror remains legal without rival auto-swap");
            }
            await Drop(ui);
        }
        var renamed=core with{Species=ImmutableSortedDictionary<string,SpeciesDefinition>.Empty
            .Add("custom_zeta",core.Species["harl"] with{Id="custom_zeta",Name="Zeta"})
            .Add("custom_alpha",core.Species["peldra"] with{Id="custom_alpha",Name="Alpha"})};
        var renamedUi=new NewGameScreen(renamed){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(renamedUi);await Frames();
        Check(Picker(renamedUi,"SpeciesPicker").GetSelectedMetadata().AsString()=="custom_alpha" && Picker(renamedUi,"RivalSpeciesPicker").GetSelectedMetadata().AsString()=="custom_zeta","Renamed/reordered alternative definitions initialize in ordinal order with a distinct rival");
        await Drop(renamedUi);
        var invalid=new NewGameScreen(core with{RivalAi=null}){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(invalid);await Frames();
        Check(Button(invalid,"Start").Disabled,"Missing policy disables new rules-4 game");bool loadAvailable=false;invalid.LoadRequested+=()=>loadAvailable=true;Press(Button(invalid,"Load Game"));Check(loadAvailable,"Missing policy retains Load");await Drop(invalid);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();var current=One<NewGameScreen>(main);
        var cramped=core with{GalaxySizes=core.GalaxySizes.SetItem("small",core.GalaxySizes["small"] with{Width=1,Height=1})};
        typeof(Main).GetField("_content",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(main,cramped);
        current.Configure("small",42);Press(Button(current,"Start"));await Frames();
        Check(ReferenceEquals(current,One<NewGameScreen>(main)) && !Descendants<GameScreen>(main).Any(),"Failed generation retains setup without replacing a session");
        Check(Descendants<Label>(current).Any(l=>l.IsVisibleInTree() && l.ThemeTypeVariation=="WarningLabel" && l.Text.Length>10),"Failed generation shows an actionable error");await Drop(main);
    }
    private async Task ObservationUiChecks()
    {
        var core=FrozenContent.Rules4();var state=GalaxyGenerator.CreateNewGame(core,new("small",42,4,"harl",1,"peldra")).State!;
        var before=state.Clone();var human=state.Empires[0];var ai=state.Empires[1];var home=state.FindPlanet(state.Colonies.Single(c=>c.EmpireId==ai.Id).PlanetId)!.StarId;
        state.Vessels.Single(v=>v.EmpireId==human.Id).StationaryStarId=home;ObservationUpdater.Refresh(state,0);ContactUpdater.Refresh(before,state);
        Check(GameStateValidator.Validate(core,state).Count==0,"Labelled pre-session observation fixture validates");
        string? text=null;
        foreach(bool changed in new[]{false,true})
        {
            var fixture=state.Clone();if(changed){fixture.FindEmpire(ai.Id)!.ResearchReserve=long.MaxValue;fixture.Colonies.Single(c=>c.EmpireId==ai.Id).ProductionReserve=long.MaxValue;fixture.Vessels.Single(v=>v.EmpireId==ai.Id).PendingMove=fixture.Stars.First(s=>s.Id!=home).Id;}
            var screen=new GameScreen(new GameSession(core,fixture),human.Id){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(screen);await Frames();One<GalaxyMap>(screen).Select(home);await Frames();
            string rendered=string.Join("\n",Descendants<Label>(screen).Select(l=>l.Text).Concat(Descendants<Control>(screen).Select(c=>c.TooltipText)));
            Check(Descendants<Label>(One<ContactsPanel>(screen)).Any(l=>l.Text.Contains(core.Species["peldra"].CultureName,StringComparison.Ordinal)),"Contact UI shows observed culture identity");
            Check(Descendants<Label>(One<StarPanel>(screen)).Any(l=>l.Text.Contains("(turn 0)",StringComparison.Ordinal)),"Ownership UI dates its observation");
            if(text is null){text=rendered;await Capture("rival-observed-contact");}else Check(text==rendered,"Rendered labels and tooltips ignore hidden rival reserves and orders");
            await Drop(screen);
        }
    }
    private async Task EncounterReportChecks()
    {
        var content=FrozenContent.Rules4();var human=content.Species["harl"];var rival=content.Species["peldra"];
        var state=new GameState{RulesVersion=4,NextEntityId=15};
        state.Empires.Add(new EmpireState{Id=new(1),Controller=ControllerKind.Human,SpeciesId="harl"});
        state.Empires.Add(new EmpireState{Id=new(8),Controller=ControllerKind.Ai,SpeciesId="peldra",AiPolicyId="expansion_v1"});
        state.Stars.Add(new StarState{Id=new(2),Name="Report Home",X=0,Y=0});state.Stars.Add(new StarState{Id=new(6),Name="Observed System",X=300,Y=0});
        state.Planets.Add(new PlanetState{Id=new(3),StarId=new(2),Orbit=1,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea"});
        state.Planets.Add(new PlanetState{Id=new(7),StarId=new(6),Orbit=1,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea"});
        state.Planets.Add(new PlanetState{Id=new(11),StarId=new(6),Orbit=2,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea"});
        state.Planets.Add(new PlanetState{Id=new(13),StarId=new(6),Orbit=3,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea"});
        state.Colonies.Add(new ColonyState{Id=new(4),EmpireId=new(1),PlanetId=new(3),Population=human.StartingPopulation,Workforce=human.StartingWorkforce,HabitatMode="native"});
        state.Colonies.Add(new ColonyState{Id=new(9),EmpireId=new(8),PlanetId=new(7),Population=rival.StartingPopulation,Workforce=rival.StartingWorkforce,HabitatMode="native"});
        state.Colonies.Add(new ColonyState{Id=new(12),EmpireId=new(8),PlanetId=new(11),Population=2,Workforce=new(2,0,0),HabitatMode="native"});
        state.Colonies.Add(new ColonyState{Id=new(14),EmpireId=new(8),PlanetId=new(13),Population=2,Workforce=new(2,0,0),HabitatMode="native"});
        state.Vessels.Add(new VesselState{Id=new(5),EmpireId=new(1),RoleId="scout",StationaryStarId=new(2),PendingMove=new(6)});
        state.Vessels.Add(new VesselState{Id=new(10),EmpireId=new(8),RoleId="scout",StationaryStarId=new(6)});ObservationUpdater.Refresh(state,0);
        var loaded=await LoadLabelled(content,state,"encounter-report-details");var screen=loaded.Screen;await EndTurn(screen);
        var session=Session(screen);var dialog=One<TurnReportDialog>(screen);var labels=Descendants<Label>(dialog).Select(l=>l.Text).ToArray();
        foreach(int orbit in new[]{2,3})Check(labels.Any(l=>l.Contains("Peldra",StringComparison.Ordinal) && l.Contains("Estates",StringComparison.Ordinal) && l.Contains("Observed System",StringComparison.Ordinal) && l.Contains("orbit "+orbit,StringComparison.Ordinal) && l.Contains("turn 1",StringComparison.Ordinal)),"Actual encounter report distinguishes Peldra colony at observed orbit "+orbit+" with culture/date");
        Check(labels.Any(l=>l.StartsWith("First contact observed",StringComparison.Ordinal) && l.Contains("Peldra",StringComparison.Ordinal) && !l.Contains("orbit",StringComparison.Ordinal)),"First-contact report identifies species without inventing a planet");
        Check(!labels.Any(l=>l.Contains("just founded",StringComparison.OrdinalIgnoreCase)),"Encounter wording describes observation rather than unknown founding history");
        GetTree().Root.Size=new(800,600);await Frames();
        var reportScroll=Descendants<ScrollContainer>(dialog).Single();reportScroll.ScrollVertical=(int)reportScroll.GetVScrollBar().MaxValue;await Frames();
        Check(new[]{2,3}.All(orbit=>Descendants<Label>(dialog).Any(l=>l.Text.Contains("orbit "+orbit,StringComparison.Ordinal) && reportScroll.GetGlobalRect().Encloses(l.GetGlobalRect()))),"Report scrolling exposes both exact observed worlds at 800x600");
        await Capture("encounter-report-details-800");GetTree().Root.Size=new(1920,1080);await Frames();await Capture("encounter-report-details-1920");
        var reference=TurnResolver.Resolve(content,state);Check(reference.Succeeded,"Labelled report reference resolves normally");
        var changed=session.State.Clone();changed.Empires[1].SpeciesId="wenth";changed.Empires[1].ResearchReserve=long.MaxValue;
        foreach(var planet in changed.Planets.Where(p=>p.StarId==new StarId(6)))planet.Orbit+=10;
        dialog.ShowReport(content,EmpireViewProjector.ProjectReport(content,changed,reference.Report!,new(1)));await Frames();
        Check(labels.SequenceEqual(Descendants<Label>(dialog).Select(l=>l.Text)),"Report identity/orbit/text ignores hidden foreign mutations and retains dated facts");
        dialog.Hide();await Drop(loaded.Main);
    }
    private bool VisibleInWindow(Control control)=>new Rect2(Vector2.Zero,GetViewport().GetVisibleRect().Size).Encloses(control.GetGlobalRect());

    private async Task HumanAndSoloChecks()
    {
        foreach(string species in new[]{"harl","peldra","sulan","wenth"})foreach(int rivals in new[]{0,1})
        {
            var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();
            var setup=One<NewGameScreen>(main);setup.Configure("small",42,species,rivals,"peldra",false,false);Press(Button(setup,"Start"));await Frames();
            var screen=One<GameScreen>(main);var session=Session(screen);var human=session.State.Empires[0].Id;
            Check(session.State.RulesVersion==9 && !session.State.Crisis!.Enabled && session.State.Empires[0].SpeciesId==species && session.State.Empires.Count==rivals+1,$"{species}/rivals{rivals}: visible Off settings create correct empires");
            var expected=new GameSession(session.Content,session.State.Clone());Check(expected.Execute(new SetReadyCommand(human,0,true)).Accepted,"Reference turn resolves");
            string name=await SaveThroughDialog(screen,$"human-{species}-rivals{rivals}");screen=await LoadThroughDialog(main,name);await EndTurn(screen);
            Check(Snapshot(Session(screen))==Snapshot(expected),$"{species}/rivals{rivals}: UI save/load preserves future continuation");One<TurnReportDialog>(screen).Hide();await Drop(main);
        }
    }
    private async Task Reach(Control control)
    {
        control.GrabFocus();await Frames();Node? ancestor=control.GetParent();while(ancestor is not null && ancestor is not ScrollContainer)ancestor=ancestor.GetParent();
        if(ancestor is ScrollContainer scroll){scroll.EnsureControlVisible(control);await Frames();Check(scroll.GetGlobalRect().Intersects(control.GetGlobalRect()),"Rival loop focus/scroll reaches "+control.GetType().Name);}
    }
    private static void SelectNumber(OptionButton picker,int id)
    {
        int index=Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsInt32()==id);picker.Select(index);picker.EmitSignal(OptionButton.SignalName.ItemSelected,index);
    }
    private async Task SelectVessel(GameScreen screen,VesselId id)
    {SelectNumber(Picker(One<VesselPanel>(screen),"VesselPicker"),id.Value);await Frames();}
    private async Task Course(GameScreen screen,StarId destination)
    {
        One<GalaxyMap>(screen).Select(destination);await Frames();var button=Button(One<VesselPanel>(screen),"Set course");await Reach(button);Press(button);await Frames();
    }
    private async Task TwelveGeneratedRivalsExpandAndAreEncounteredWithin80Turns()
    {foreach(string species in new[]{"peldra","harl","sulan","wenth"})foreach(string size in new[]{"small","medium","large"})await GeneratedRivalLoop(species,size);Check(_researchSpecies.SetEquals(new[]{"peldra","harl","sulan","wenth"}),"Each rival species completed real research in a generated campaign");}
    private async Task GeneratedRivalLoop(string species,string size)
    {
        string label=$"rival-{species}-{size}";GetTree().Root.Size=new(1920,1080);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();
        var setup=One<NewGameScreen>(main);
        var screen=await OpenAntares.Game.Validation.ScoutUiControlChecks.LoadGeneratedRules8(this,main,new(size,42,8,"harl",1,species,CrisesEnabled:false,AmebasEnabled:false),_output,label,Check);GameSession session=Session(screen);EmpireId humanId=session.State.Empires[0].Id;EmpireId rivalId=session.State.Empires[1].Id;
        Check(session.State.RulesVersion==8 && !session.State.Crisis!.Enabled && session.State.GalaxySeed==42 && session.State.Empires[1].SpeciesId==species,label+": actual Main Load preserves explicit8 plague/ameba Off seed42 campaign");
        var home=session.View(humanId).Colonies.Single();var scout=session.View(humanId).Vessels.Single();
        Select(Descendants<OptionButton>(One<ColonyPanel>(screen)).Single(p=>p.Name=="ColonyProjectPicker"),"vessel|harl_settler");await Frames();
        Select(Descendants<OptionButton>(One<ResearchBar>(screen)).Single(),"cultivation_methods");await Frames();
        bool savedPending=false,savedTransit=false,savedFounding=false,firstContactReport=false,development=false;int resolvedTurns=0,secondAt=-1,contactAt=-1;
        while(resolvedTurns<80)
        {
            var view=session.View(humanId);
            var scoutNow=view.Vessels.Single(v=>v.Id==scout.Id);
            if(scoutNow.Transit is null && scoutNow.PendingMove is null)
            {
                var at=view.Stars.Single(s=>s.Chart.Id==scoutNow.StationaryStarId);
                var target=view.Stars.Where(s=>s.Chart.Id!=at.Chart.Id && s.Planets is null).OrderBy(s=>TravelCalculator.Duration(at.Chart.X,at.Chart.Y,s.Chart.X,s.Chart.Y,session.Content.Vessels["scout"].Speed)).ThenBy(s=>s.Chart.Id).FirstOrDefault()
                    ??view.Stars.Where(s=>s.Chart.Id!=at.Chart.Id && !s.Current).OrderBy(s=>s.LastObservedTurn).ThenBy(s=>s.Chart.Id).FirstOrDefault();
                if(resolvedTurns==0)target=view.Stars.Where(s=>s.Chart.Id!=at.Chart.Id).OrderByDescending(s=>TravelCalculator.Duration(at.Chart.X,at.Chart.Y,s.Chart.X,s.Chart.Y,session.Content.Vessels["scout"].Speed)).ThenBy(s=>s.Chart.Id).First();
                if(target is not null){await SelectVessel(screen,scout.Id);await Course(screen,target.Chart.Id);}
            }
            view=session.View(humanId);
            foreach(var founder in view.Vessels.Where(v=>v.CarriedPopulation>0 && v.Transit is null && v.PendingMove is null && v.PendingFounding is null))
            {
                var at=view.Stars.Single(s=>s.Chart.Id==founder.StationaryStarId);
                var candidates=view.Stars.Where(s=>s.Planets.HasValue).SelectMany(s=>s.Planets!.Value.Where(p=>p.Colonizable && p.SupportPerWorker>0 && p.OwnerId is null).Select(p=>new{Star=s,Planet=p}))
                    .OrderBy(t=>t.Star.Chart.Id==at.Chart.Id?0:TravelCalculator.Duration(at.Chart.X,at.Chart.Y,t.Star.Chart.X,t.Star.Chart.Y,session.Content.Vessels[founder.RoleId].Speed)).ThenBy(t=>t.Planet.Id).ToArray();
                var target=candidates.FirstOrDefault();if(target is null)continue;
                await SelectVessel(screen,founder.Id);One<GalaxyMap>(screen).Select(target.Star.Chart.Id);await Frames();
                if(target.Star.Chart.Id!=at.Chart.Id)await Course(screen,target.Star.Chart.Id);
                else
                {
                    Press(Button(One<StarPanel>(screen),$"Select planet {target.Planet.Orbit}"));await Frames();var button=Button(One<VesselPanel>(screen),"Found colony");
                    if(!button.Disabled){await Reach(button);Press(button);await Frames();if(!savedFounding){string name=await SaveThroughDialog(screen,label+"-founding");screen=await LoadThroughDialog(main,name);session=Session(screen);savedFounding=true;}}
                }
            }
            if(!savedPending && session.View(humanId).Vessels.Any(v=>v.PendingMove.HasValue))
            {string name=await SaveThroughDialog(screen,label+"-pending");screen=await LoadThroughDialog(main,name);session=Session(screen);savedPending=true;}
            if(!development && session.View(humanId).Vessels.Any(v=>v.CarriedPopulation>0))
            {
                One<GalaxyMap>(screen).Select(home.StarId);await Frames();Press(Button(One<StarPanel>(screen),$"Select planet {session.State.FindPlanet(home.PlanetId)!.Orbit}"));await Frames();
                Select(Descendants<OptionButton>(One<ColonyPanel>(screen)).Single(p=>p.Name=="ColonyProjectPicker"),"cultivation_hub");await Frames();development=true;
            }
            var reference=new GameSession(session.Content,session.State.Clone());Check(reference.Execute(new SetReadyCommand(humanId,reference.State.Turn,true)).Accepted,label+": uninterrupted turn is valid");
            await EndTurn(screen);resolvedTurns++;
            Check(Snapshot(session)==Snapshot(reference),label+": actual End Turn equals canonical reference");
            if(Descendants<Label>(One<TurnReportDialog>(screen)).Any(l=>l.Text.StartsWith("First contact observed",StringComparison.Ordinal)))firstContactReport=true;
            One<TurnReportDialog>(screen).Hide();
            if(!savedTransit && session.View(humanId).Vessels.Any(v=>v.Transit is not null))
            {string name=await SaveThroughDialog(screen,label+"-transit");screen=await LoadThroughDialog(main,name);session=Session(screen);savedTransit=true;}
            if(secondAt<0 && session.State.Colonies.Count(c=>c.EmpireId==rivalId)>=2)secondAt=resolvedTurns;
            if(contactAt<0 && session.View(humanId).Contacts.Any(c=>c.OtherEmpireId==rivalId))contactAt=resolvedTurns;
            if(session.State.FindEmpire(rivalId)!.KnownTechnologyIds.Count>0)_researchSpecies.Add(species);
            if(secondAt>=0 && contactAt>=0 && session.View(humanId).Colonies.Length>=2 && savedFounding && savedTransit && savedPending && firstContactReport && development && (size!="small" || _researchSpecies.Contains(species)))break;
        }
        Check(resolvedTurns<=80,label+": generated loop stays within core turn bound");
        Check(session.State.Colonies.Count(c=>c.EmpireId==rivalId)>=2,label+": rival founded a real second colony");
        Check(session.View(humanId).Contacts.Any(c=>c.OtherEmpireId==rivalId),label+": human observed the rival");
        Check(session.View(humanId).Colonies.Length>=2,label+": real human settlement");
        Check(savedPending && savedTransit && savedFounding,label+": pending/transit/founding saved order stages");
        Check(firstContactReport && development,label+": real development and observed contact report");
        _results.Add(new{name=label+": generated provenance",passed=true,seed=42,humanSpecies="harl",rivalSpecies=species,size,resolvedTurns,rivalSecondColonyTurn=secondAt,contactTurn=contactAt,rivalResearchCompletions=session.State.FindEmpire(rivalId)!.KnownTechnologyIds.Count,liveOutcomeInjections=0,operationBound="2*ownedColonies+ownedVessels+1 (unit enforced)",provenance="Unmodified generated campaign; human choices from own view through actual controls."});
        string completed=await SaveThroughDialog(screen,label+"-expanded");var future=new GameSession(session.Content,session.State.Clone());
        for(int i=0;i<3;i++)Check(future.Execute(new SetReadyCommand(humanId,future.State.Turn,true)).Accepted,label+": reference future resolves");
        screen=await LoadThroughDialog(main,completed);for(int i=0;i<3;i++){await EndTurn(screen);One<TurnReportDialog>(screen).Hide();}
        Check(Snapshot(Session(screen))==Snapshot(future),label+": three future turns after UI reload match uninterrupted campaign");
        foreach(var dimensions in new[]{new Vector2I(800,600),new(1152,648),new(1280,720),new(1600,900),new(1920,1080)})
        {GetTree().Root.Size=dimensions;await Frames();var picker=Picker(One<VesselPanel>(screen),"VesselPicker");await Reach(picker);Check(VisibleInWindow(Button(screen,"End Turn")) && VisibleInWindow(picker),label+": game controls fit "+dimensions);if(size=="small" && dimensions.X is 800 or 1920)await Capture(label+"-observed-"+dimensions.X);}
        await Drop(main);
    }
    private async Task<(Control Main,GameScreen Screen)> LoadLabelled(ContentSet content,GameState state,string label)
    {
        Check(GameStateValidator.Validate(content,state).Count==0,label+": labelled pre-session fixture validates");string name=$"qa-{_runId}-labelled-{label}";string path=SaveFiles.PathFor(name);Check(!File.Exists(path),label+": fixture path unique");SaveGame.WriteFile(path,content,state);_savePaths.Add(path);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();Press(Button(One<NewGameScreen>(main),"Start"));await Frames();return(main,await LoadThroughDialog(main,name));
    }
    private async Task LabelledFixtures()
    {
        var core=FrozenContent.Rules4();
        foreach(string mode in new[]{"native","sealed","extended","race","alternate"})
        {
            string species=mode is "native" or "sealed"?"sulan":mode=="extended"?"wenth":"harl";
            var content=core;
            if(mode=="alternate")
            {
                var renamed=core.Species["harl"] with{Id="renamed_species",Name="Fixture Pioneers",CultureName="Fixture Guild"};
                content=core with{Species=core.Species.Remove("harl").Add(renamed.Id,renamed),RivalAi=new("expansion_v1",0,9)};species=renamed.Id;
            }
            var state=GalaxyGenerator.CreateNewGame(content,new("small",42,4,species,1,species)).State!;var human=state.Empires[0];var ai=state.Empires[1];var source=state.Colonies.Single(c=>c.EmpireId==ai.Id);var star=state.FindPlanet(source.PlanetId)!.StarId;
            var target=new PlanetState{Id=new(state.AllocateId()),StarId=star,Orbit=5,PlanetTypeId="verdant_world",EnvironmentId=mode=="sealed"?"dry":"shelf_sea"};state.Planets.Add(target);
            if(mode is "native" or "sealed")state.Vessels.Add(new VesselState{Id=new(state.AllocateId()),EmpireId=ai.Id,RoleId=mode=="sealed"?"harbor_settler":"settler",CarriedPopulation=2,StationaryStarId=star});
            if(mode=="extended"){ai.KnownTechnologyIds.Add("habitat_methods");source.CompletedBuildingIds.Add("habitat_extension");source.Population=6;source.Workforce=new(4,1,1);source.ProjectKind="vessel";source.ProjectId="daughter_seed";source.ProductionReserve=2400;}
            if(mode=="race")
            {
                state.Vessels.Add(new VesselState{Id=new(state.AllocateId()),EmpireId=human.Id,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=star,PendingFounding=target.Id});
                state.Vessels.Add(new VesselState{Id=new(state.AllocateId()),EmpireId=ai.Id,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=star});
            }
            ObservationUpdater.Refresh(state,0);ContactUpdater.Refresh(state.Clone(),state);
            var loaded=await LoadLabelled(content,state,"rival-"+mode);var screen=loaded.Screen;var session=Session(screen);
            if(mode is "native" or "sealed")
            {await EndTurn(screen);One<TurnReportDialog>(screen).Hide();var child=session.State.Colonies.Single(c=>c.PlanetId==target.Id);Check(child.HabitatMode==mode && session.View(ai.Id).Colonies.Single(c=>c.Id==child.Id).Economy.Growth.Capacity==(mode=="sealed"?8:12),"Labelled rival Sulan explicitly founds "+mode);}
            if(mode=="extended")
            {
                bool complete=false;for(int i=0;i<20 && !complete;i++){int population=session.State.FindColony(source.Id)!.Population;await EndTurn(screen);One<TurnReportDialog>(screen).Hide();complete=session.State.Vessels.Any(v=>v.EmpireId==ai.Id && v.RoleId=="daughter_seed");if(population<8)Check(!complete,"Labelled AI Wenth waits for current capacity eight, including growth-to-eight snapshot");}
                Check(complete && session.State.FindColony(source.Id)!.Population==5,"Labelled AI extended Wenth transfers eight to parent5 plus seed3");await EndTurn(screen);One<TurnReportDialog>(screen).Hide();var child=session.State.Colonies.Single(c=>c.PlanetId==target.Id);Check(child.Population==3 && session.View(ai.Id).Colonies.Single(c=>c.Id==child.Id).Economy.Growth.Capacity==6,"Labelled AI Wenth daughter is unbuilt capacity six");
            }
            if(mode=="race")
            {await EndTurn(screen);Check(session.State.Colonies.Single(c=>c.PlanetId==target.Id).EmpireId==human.Id,"Labelled simultaneous race preserves lower EmpireId winner");Check(session.State.Vessels.Any(v=>v.EmpireId==ai.Id && v.CarriedPopulation==1),"Labelled race loser retains cargo");Check(Descendants<Label>(One<TurnReportDialog>(screen)).All(l=>!l.Text.Contains("Another expedition settled",StringComparison.Ordinal)),"Winner report does not disclose a hidden losing order");One<TurnReportDialog>(screen).Hide();}
            if(mode=="alternate")
            {
                string name=await SaveThroughDialog(screen,"rival-renamed-policy");var expected=new GameSession(content,session.State.Clone());for(int i=0;i<4;i++)Check(expected.Execute(new SetReadyCommand(human.Id,expected.State.Turn,true)).Accepted,"Renamed embedded policy reference resolves");screen=await LoadThroughDialog(loaded.Main,name);for(int i=0;i<4;i++){await EndTurn(screen);One<TurnReportDialog>(screen).Hide();}Check(Snapshot(Session(screen))==Snapshot(expected) && Session(screen).Content.RivalAi==new RivalAiDefinition("expansion_v1",0,9),"Renamed species and nondefault embedded policy continue exactly through UI load");
            }
            await Capture("labelled-rival-"+mode);await Drop(loaded.Main);
        }
    }
}
