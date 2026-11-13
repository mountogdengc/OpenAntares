using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Presentation;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Validation;

public partial class VisualOverhaulValidation
{
    private async Task SelectionConsole()
    {
        var sizingFailures = new System.Collections.Generic.List<string>();
        void Fit(Button key, string context)
        {
            var style=key.GetThemeStylebox("normal");
            float required=key.GetThemeFont("font").GetStringSize(key.Text,fontSize:key.GetThemeFontSize("font_size")).X+style.GetContentMargin(Side.Left)+style.GetContentMargin(Side.Right);
            if(key.Icon is not null)required+=key.GetThemeConstant("icon_max_width")+key.GetThemeConstant("h_separation");
            if(All<PlanetGlobe2D>(key).FirstOrDefault() is {} globe)required+=globe.Size.X+key.GetThemeConstant("key_gap","PlanetGlobeMetrics");
            bool fits=required<=key.Size.X+1;
            _checks.Add(new{passed=fits,name=context+": full action/planet text fits "+key.Name});if(!fits)sizingFailures.Add(context+": clipped "+key.Name);
        }
        var screen = await Start(); var game = Live(screen);
        var view = game.View(game.State.Empires.First(e => e.Controller == ControllerKind.Human).Id);
        var map = All<GalaxyMap>(screen).Single(); var home = view.Colonies[0].StarId;
        string before = Snapshot(game);
        var console = Named<PanelContainer>("EmpireConsole");
        Check(!All<TabContainer>(console).Any(), "Selection console has no System / Empire / Fleets tabs");
        Check(Named<Label>("ConsoleSelectionKind").Text == "SELECTED · STAR SYSTEM", "Home selection opens the system card");
        Check(!All<TurnGuidancePanel>(console).Any() && !All<Button>(console).Any(b => b.Name == "OpenColonyOverview"), "Empire guidance and colony overview live outside the selection console");
        Check(!Named<Button>("OpenConflictOrders").Visible, "Peaceful selected system hides console conflict orders");
        var planets = view.Stars.Single(s => s.Chart.Id == home).Planets!.Value;
        Check(All<Button>(screen).Count(b => b.Name.ToString().StartsWith("SystemPlanet_")) == planets.Length, "System card includes every known planet key");
        Check(!Named<Button>("ManageSelectedColony").Disabled && Named<PanelContainer>("PlanetCard").IsVisibleInTree(), "Owned home world has a colony card and enabled management");
        foreach (var size in new[] { new Vector2I(800,600), new Vector2I(1920,1080) })
        {
            await Capture("SystemCard", size);
            foreach(var key in All<Button>(screen).Where(b=>b.Name.ToString().StartsWith("SystemPlanet_")||b.Name=="ManageSelectedColony"||b.Name=="InspectSelectedSystem"))Fit(key,"System "+size);
            var output=All<Label>(Named<PanelContainer>("PlanetCard")).Where(l=>l.Text.Contains("production / turn")).ToArray();
            bool concise=output.Length>0&&output.All(l=>l.GetLineCount()<=2);
            _checks.Add(new{passed=concise,name="Colony output stays readable without word-by-word wrapping at "+size});if(!concise)sizingFailures.Add("Colony output wraps excessively at "+size);
        }
        var unowned = planets.First(p => p.OwnerId != view.Viewer);
        Press(Named<Button>("SystemPlanet_" + unowned.Id.Value)); await Frames();
        Check(Named<Button>("ManageSelectedColony").Disabled && Named<Button>("SystemPlanet_" + unowned.Id.Value).ButtonPressed, "Planet key changes the card and disables management for an unowned world");
        map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=map.OwnedMarkerBounds[home].GetCenter()});await Frames();
        Press(Named<Button>("OpenVesselWorkspace"));await Frames();
        Check((PlanetId?)typeof(GameScreen).GetField("_selectedPlanet",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(screen)==unowned.Id,"Opening Ship orders from the map preserves the explicitly chosen world for founding review");
        Press(Route("OpenGalaxyWorkspace"));await Frames();map.Select(home);await Frames();
        Press(Named<Button>("SystemPlanet_" + view.Colonies[0].PlanetId.Value)); await Frames();
        Press(Route("OpenAttentionConsole")); await Frames();
        var guidance = All<TurnGuidancePanel>(screen).Single();
        var items = TurnGuidance.Build(view);
        Check(Named<Control>("DecisionsWorkspace").IsVisibleInTree() && !console.IsVisibleInTree(), "Decisions key opens its own page");
        Check(!All<Button>(guidance).Any(b => b.Name == "ToggleOptionalGuidance") && All<Button>(guidance).Count(b => b.Name.ToString().StartsWith("Guidance_") && b.IsVisibleInTree()) == items.Count, "Decisions lists all required and optional items directly");
        Check(items.All(i => Named<Button>("EndTurn").TooltipText.Contains(i.Text)), "Decisions items match the End Turn guidance");
        Press(Named<Button>("DecisionsConflictOrders")); await Frames();
        Check(All<ConflictOrdersDialog>(screen).Single().Visible, "Empire conflict orders remain reachable on Decisions without a selected conflict");
        All<ConflictOrdersDialog>(screen).Single().Hide(); await Frames();
        Press(Route("OpenAttentionConsole")); await Frames();
        Check(map.IsVisibleInTree() && !Named<Control>("DecisionsWorkspace").Visible, "Repeated Decisions key returns to the map");
        var row = All<MapFleetRow>(screen).Single(r => r.Name.ToString().StartsWith("SystemShipRow_"));
        string shipName = "Scout " + view.Vessels[0].Id.Value;
        Check(row.Text.StartsWith(shipName + "  ·  "), "Single independent ship uses the shared numbered name in the system card");
        Press(row); await Frames();
        Check(map.Selected is null && Named<Label>("ConsoleSelectionKind").Text == "SELECTED · FLEET" && !Named<Control>("SystemContext").Visible, "System fleet row replaces the system card with a fleet card and clears the system ring");
        Check(Named<MapFleetRow>("MapShipRow_" + view.Vessels[0].Id.Value).Text.StartsWith(shipName + "  ·  "), "Fleet card uses the same ship name and status");
        Check(All<Label>(Named<PanelContainer>("FleetCardDetails")).Any(l => l.Text.Contains(shipName) && l.Text.Contains("Idle")), "Selected fleet card shows the ship status");
        foreach (var size in new[] { new Vector2I(800,600), new Vector2I(1920,1080) })
        {
            await Capture("FleetCard", size);
            Fit(Named<Button>("OpenVesselWorkspace"),"Fleet "+size);Fit(Named<Button>("FleetCardShowSystem"),"Fleet "+size);
        }
        var detail=typeof(GameScreen).GetMethod("ShipDetail",BindingFlags.NonPublic|BindingFlags.Instance)!;
        string cargo=(string)detail.Invoke(screen,new object[]{view,view.Vessels[0] with{CarriedPopulation=2}})!;
        Check(cargo.Contains("2 "+view.PopulationUnitLabel+" aboard")&&cargo.Contains("hull 60/60"),"Published ship details include carried population and actual hull; cargo is a labelled projection supplement");
        Press(Named<Button>("OpenVesselWorkspace")); await Frames();
        Check(Named<Control>("VesselsWorkspace").IsVisibleInTree() && Named<OptionButton>("VesselPicker").GetSelectedMetadata().AsInt32() == view.Vessels[0].Id.Value, "Ship orders opens the exact selected vessel");
        Press(Route("OpenGalaxyWorkspace")); await Frames();
        map._GuiInput(new InputEventMouseButton { Pressed=true, ButtonIndex=MouseButton.Left, Position=map.OwnedMarkerBounds[home].GetCenter() }); await Frames();
        Check(map.Selected is null && Named<Control>("MapFleetList").Visible, "Map fleet flag opens the fleet card without a system ring");
        Press(Named<MapFleetRow>("MapShipRow_" + view.Vessels[0].Id.Value)); await Frames();
        screen._UnhandledKeyInput(new InputEventKey { Pressed=true, Keycode=Key.Escape }); await Frames();
        Check(Named<Control>("MapFleetList").Visible && !All<PanelContainer>(screen).Any(p => p.Name == "FleetCardDetails"), "First Escape clears chosen ships and retains the fleet card");
        screen._UnhandledKeyInput(new InputEventKey { Pressed=true, Keycode=Key.Escape }); await Frames();
        Check(!Named<Control>("MapFleetList").Visible && map.Selected is null && Named<Label>("ConsoleSelectionKind").Text == "NOTHING SELECTED", "Second Escape clears the fleet card");
        map.Select(home); await Frames();
        screen._UnhandledKeyInput(new InputEventKey { Pressed=true, Keycode=Key.Escape }); await Frames();
        Check(map.Selected is null && Named<Label>("ConsoleSelectionKind").Text == "NOTHING SELECTED", "Escape clears a system-only selection in one press");
        Check(Snapshot(game) == before, "All selection, Decisions, inspection and Escape actions preserve the campaign and PRNG");

        // Requester-projection supplement: an own response at home, not an earned encounter.
        var encounter = new OwnConflictEncounterView(new(1), new(2), home, null, 7, 0, ConflictPhase.ResponseRequired,
            ImmutableArray.Create(view.Vessels[0].Id), ImmutableArray<VesselId>.Empty,
            new(CombatDoctrine.Cautious, view.Colonies[0].Id, StragglerPolicy.KeepTogether, CombatArrivalOrder.Repair), false, true, ImmutableArray<MilitaryObservation>.Empty);
        var projected = view with { Conflict = view.Conflict! with { Encounters = ImmutableArray.Create(encounter) } };
        var refresh = typeof(GameScreen).GetMethod("RefreshSelectionConsole", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var homeView = projected.Stars.Single(s => s.Chart.Id == home);
        refresh.Invoke(screen, new object?[] { projected, homeView, projected.Colonies[0] });
        Check(Named<Button>("OpenConflictOrders").Visible, "Selected system with a projected own encounter shows conflict orders");
        refresh.Invoke(screen, new object?[] { projected, projected.Stars.First(s => s.Chart.Id != home), null });
        Check(!Named<Button>("OpenConflictOrders").Visible, "A different selected system hides conflict orders even while another system has conflict");
        refresh.Invoke(screen, new object?[] { projected, null, null });
        Check(!Named<Button>("OpenConflictOrders").Visible, "No selection hides console conflict orders");
        Press(Route("OpenAttentionConsole")); await Frames(); guidance.ShowGuidance(projected); await Frames();
        Check(All<Button>(guidance).Count(b => b.IsVisibleInTree() && b.Name.ToString().StartsWith("Guidance_")) == TurnGuidance.Build(projected).Count && Named<Button>("Guidance_conflict").IsVisibleInTree(), "Decisions directly lists a required conflict response alongside optional guidance");
        foreach (var size in new[] { new Vector2I(800,600), new Vector2I(1920,1080) }) await Capture("Decisions", size);
        Check(Snapshot(game) == before, "Projection supplements never alter the campaign");

        // Pre-session roster supplement; normal fleet commands below create the two named fleets.
        var generated = GalaxyGenerator.CreateNewGame(game.Content, new("small",42,12,"harl",0,null,false,false,false));
        var state = generated.State!;
        for (int i=0; i<4; i++) { var copy=state.Vessels[0].Clone(); copy.Id=new(state.AllocateId()); state.Vessels.Add(copy); }
        var fixture = new GameSession(game.Content,state); var owner=state.Empires.First(e=>e.Controller==ControllerKind.Human).Id;
        void Apply(GameSession target, FleetAction action) { var v=target.View(owner); var preview=FleetQueries.PreviewAction(target.Content,v,action); Check(preview.Allowed,"Fixture/reference action legal"); Check(target.Execute(new FleetCommand(owner,v.Turn,action){Fingerprint=preview.Fingerprint}).Accepted,"Fixture/reference action accepted"); }
        Apply(fixture,new FleetAction.Create("First",state.Vessels.Take(2).Select(v=>v.Id).ToImmutableArray()));
        Apply(fixture,new FleetAction.Create("Second",ImmutableArray.Create(state.Vessels[2].Id)));
        var original = _main!; original.Hide();
        var fixtureScreen = new GameScreen(fixture,owner); _main=fixtureScreen; AddChild(fixtureScreen); await Frames();
        var fixtureView=fixture.View(owner); var fixtureMap=All<GalaxyMap>(fixtureScreen).Single(); home=fixtureView.Colonies[0].StarId;
        string fixtureBefore=Snapshot(fixture);
        var last=fixtureView.Vessels.Last();
        Press(Route("OpenFleetCommand"));await Frames();var exact=Named<FleetList>("FleetShipList");exact.DeselectAll();exact.Select(Enumerable.Range(0,exact.ItemCount).Single(i=>exact.GetItemMetadata(i).AsInt32()==last.Id.Value));
        Press(Named<Button>("FleetInspectShip"));await Frames();
        Press(Route("OpenGalaxyWorkspace"));await Frames();
        CombinedUiNavigation.Reveal(All<VesselPanel>(fixtureScreen).Single());await Frames();
        Check(Named<OptionButton>("VesselPicker").GetSelectedMetadata().AsInt32()==last.Id.Value,"Adapted shared inspector navigation preserves the exact vessel instead of falling through to the first ship");
        Press(Route("OpenGalaxyWorkspace"));await Frames();fixtureMap.Select(home);await Frames();
        CombinedUiNavigation.Reveal(Named<Button>("OpenColonyOverview"));await Frames();
        Check(Named<Control>("DecisionsWorkspace").IsVisibleInTree(),"Adapted shared navigation reveals moved Decisions controls through the command key");
        Press(Route("OpenGalaxyWorkspace"));await Frames();fixtureMap.Select(home);await Frames();
        Check(Snapshot(fixture)==fixtureBefore,"Adapted navigation preserves the campaign and PRNG");
        Check(All<MapFleetRow>(fixtureScreen).Count(r=>r.Name.ToString().StartsWith("SystemFleetRow_"))==2, "System card lists the two named fleets separately");
        var stack=All<MapFleetRow>(fixtureScreen).Single(r=>r.Name.ToString().StartsWith("SystemShipRow_"));
        Check(stack.Text.Contains("×2"), "System card stacks two independent ships of the same class and status");
        Press(stack); await Frames();
        Check(All<MapFleetRow>(fixtureScreen).Count(r=>r.Name.ToString().StartsWith("MapShipRow_"))==1, "Fleet card stacks equivalent independent ships using the shared row");
        Check(All<Label>(Named<PanelContainer>("FleetCardDetails")).Count(l=>l.Text.Contains(" · Idle"))==2, "Opening a system stack selects both underlying ships with individual details");
        Press(Named<Button>("FleetCardShowSystem")); await Frames();
        Check(fixtureMap.Selected==home && Named<Control>("SystemContext").Visible && !Named<Control>("MapFleetList").Visible, "System card action restores the same system and clears chosen ships");
        Press(All<MapFleetRow>(fixtureScreen).First(r=>r.Name.ToString().StartsWith("SystemFleetRow_"))); await Frames();
        var first=fixtureView.Fleets!.Fleets.Single(f=>f.Name=="First"); var second=fixtureView.Fleets.Fleets.Single(f=>f.Name=="Second");
        MapFleetRow FleetRow(FleetId id)=>Named<MapFleetRow>("MapFleetRow_"+id.Value);
        FleetRow(second.Id)._GuiInput(new InputEventMouseButton { Pressed=true,ButtonIndex=MouseButton.Left,ShiftPressed=true }); Press(FleetRow(second.Id)); await Frames();
        Check(FleetRow(first.Id).ButtonPressed && FleetRow(second.Id).ButtonPressed,"Shift-click retains both named fleets in the new card");
        FleetRow(first.Id)._GuiInput(new InputEventMouseButton { Pressed=true,ButtonIndex=MouseButton.Left }); Press(FleetRow(first.Id)); await Frames();
        Check(FleetRow(first.Id).ButtonPressed && !FleetRow(second.Id).ButtonPressed,"Plain click replaces the multi-selection");
        FleetRow(first.Id)._GuiInput(new InputEventMouseButton { Pressed=true,ButtonIndex=MouseButton.Right,Position=new(4,4) }); await Frames();
        var menu=Named<PopupMenu>("MapFleetContext"); Check(menu.Visible,"Right-click still exposes fleet split"); menu.EmitSignal(PopupMenu.SignalName.IdPressed,0L); menu.Hide(); await Frames();
        var split=Named<MapFleetSplitDialog>("MapFleetSplit"); var members=Named<ItemList>("MapSplitMembers");
        Check(Enumerable.Range(0,members.ItemCount).All(i=>members.GetItemText(i)==FleetNaming.Ship(fixture.Content,fixtureView,fixtureView.Vessels.Single(v=>v.Id.Value==members.GetItemMetadata(i).AsInt32()))),"Split dialog uses the same numbered ship names");
        members.Select(0); members.EmitSignal(ItemList.SignalName.MultiSelected,0L,true); await Frames();
        var expected=new GameSession(fixture.Content,fixture.State.Clone()); Apply(expected,new FleetAction.Split(first.Id,Named<LineEdit>("MapSplitName").Text,ImmutableArray.Create(first.Members[0])));
        Press(split.GetOkButton()); await Frames(); Check(Snapshot(fixture)==Snapshot(expected),"Split submits exactly the existing reviewed command"); split.Hide(); await Frames();
        var drag=FleetRow(first.Id)._GetDragData(Vector2.Zero); Check(FleetRow(second.Id)._CanDropData(Vector2.Zero,drag),"Existing merge remains available in the new fleet card");
        expected=new GameSession(fixture.Content,fixture.State.Clone()); Apply(expected,new FleetAction.Merge(second.Id,ImmutableArray.Create(first.Id))); FleetRow(second.Id)._DropData(Vector2.Zero,drag); await Frames();
        Check(Snapshot(fixture)==Snapshot(expected),"Drag merge submits exactly the existing command");
        Press(Route("OpenFleetCommand")); await Frames(); var fleetList=Named<FleetList>("FleetShipList"); var current=fixture.View(owner);
        Check(Enumerable.Range(0,fleetList.ItemCount).All(i=>fleetList.GetItemText(i).StartsWith(FleetNaming.Ship(fixture.Content,current,current.Vessels.Single(v=>v.Id.Value==fleetList.GetItemMetadata(i).AsInt32()))+" · ")),"Fleets workspace exact-member rows share numbered ship names");
        Press(Route("OpenGalaxyWorkspace")); await Frames();
        fixtureMap._GuiInput(new InputEventMouseButton { Pressed=true,ButtonIndex=MouseButton.Left,Position=fixtureMap.OwnedMarkerBounds[home].GetCenter() }); await Frames();
        var mapShips=Named<Control>("MapFleetList");
        var expansion=All<Button>(mapShips).FirstOrDefault(b=>b.Name.ToString().StartsWith("ExpandShipStack_"));
        if(expansion is not null&&expansion.Visible){Press(expansion);await Frames();Press(All<MapFleetRow>(mapShips).First(r=>r.Name.ToString().StartsWith("MapShipMember_")));}
        else Press(All<MapFleetRow>(mapShips).First(r=>r.Name.ToString().StartsWith("MapShipRow_")));
        await Frames();
        var selection=fixture.View(owner).Vessels.Single(v=>All<MapFleetRow>(fixtureScreen).Any(r=>(r.Name=="MapShipMember_"+v.Id.Value||r.Name=="MapShipRow_"+v.Id.Value)&&r.ButtonPressed));
        var destination=current.Stars.First(s=>s.Chart.Id!=home&&!current.Scouting!.Habitats.Any(h=>h.Star==s.Chart.Id)).Chart;
        var transform=typeof(GalaxyMap).GetMethod("BuildTransform",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(fixtureMap,null)!;
        var at=(Vector2)typeof(GalaxyMap).GetMethod("ToScreen",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(fixtureMap,new object[]{destination.X,destination.Y,transform})!;
        expected=new GameSession(fixture.Content,fixture.State.Clone()); Apply(expected,new FleetAction.MoveSelection(ImmutableArray.Create(selection.Id),ImmutableArray<FleetId>.Empty,destination.Id));
        fixtureMap._GuiInput(new InputEventMouseMotion { Position=at }); await Frames();
        fixtureMap._GuiInput(new InputEventMouseButton { Pressed=true,ButtonIndex=MouseButton.Left,Position=at }); await Frames();
        Check(Snapshot(fixture)==Snapshot(expected),"Destination click from the fleet card submits exactly one ordinary selected-ship move");
        fixtureMap._GuiInput(new InputEventMouseButton { Pressed=true,ButtonIndex=MouseButton.Right,Position=fixtureMap.Size/2 }); await Frames();
        Check(Named<Label>("ConsoleSelectionKind").Text=="NOTHING SELECTED", "Right-click clears fleet and system selection");
        RemoveChild(fixtureScreen); fixtureScreen.QueueFree(); _main=original; original.Show(); await Frames();
        if(sizingFailures.Count>0)throw new InvalidOperationException(string.Join("; ",sizingFailures));
    }
}
