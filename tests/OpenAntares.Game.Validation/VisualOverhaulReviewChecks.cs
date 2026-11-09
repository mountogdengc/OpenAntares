using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Fleets;

namespace OpenAntares.Game.Validation;

public partial class VisualOverhaulValidation
{
    private async Task PopulationForecast()
    {
        var screen=await Start();var session=Live(screen);var view=session.View(session.State.Empires.First(e=>e.Controller==OpenAntares.Simulation.State.ControllerKind.Human).Id);
        var colony=view.Colonies[0];var failures=new List<string>();
        void Review(bool passed,string name){_checks.Add(new{passed,name});if(!passed)failures.Add(name);}
        var strip=All<EmpireStatusStrip>(screen).Single();string unchanged=Snapshot(session);
        // Small presentation fixtures using the same published outcome records as a completed colony ship.
        var embark=colony.Economy with{Growth=colony.Economy.Growth with{PopulationBefore=colony.Population,PopulationAfter=colony.Population},EmbarkOutcome=new(colony.Population-2,colony.Workforce with{Support=colony.Workforce.Support-2},OpenAntares.Simulation.Economy.Quantity.Zero,OpenAntares.Simulation.Economy.Quantity.Zero)};
        strip.ShowEmpire(view with{Colonies=ImmutableArray.Create(colony with{Economy=embark})},session.Content);
        Review(Named<Label>("POPULATIONDelta").Text=="(-2)","R6: two embarking colonists appear as a population decrease, even without a birth");
        var growth=colony.Economy with{Growth=colony.Economy.Growth with{PopulationAfter=colony.Population+1},EmbarkOutcome=null};
        strip.ShowEmpire(view with{Colonies=ImmutableArray.Create(colony with{Economy=growth})},session.Content);
        Review(Named<Label>("POPULATIONDelta").Text=="(+1)","R6: a projected birth remains one population, not growth points");
        strip.ShowEmpire(view with{Colonies=ImmutableArray<OpenAntares.Simulation.Knowledge.OwnedColonyView>.Empty},session.Content);
        Review(Named<Label>("POPULATIONReadout").Text=="0"&&Named<Label>("POPULATIONDelta").Text=="(0)","R6: zero colonies show zero population and change");
        strip.ShowEmpire(view,session.Content);Review(Snapshot(session)==unchanged,"R6: readout fixtures issue no commands and alter no campaign state");await Capture("PopulationForecast",new(1920,1080));
        if(failures.Count>0)throw new InvalidOperationException(string.Join("; ",failures));
    }
    private async Task ReviewCorrections()
    {
        var failures=new List<string>();
        void Review(bool passed,string name){_checks.Add(new{passed,name});if(!passed)failures.Add(name);}
        var screen=await Start();var session=Live(screen);var originalMain=_main!;
        Check(!All<Control>(screen).Any(c=>c.Name=="FirstTurnHint"),"No automatic campaign-start banner");await Frames();
        var view=session.View(session.State.Empires.First(e=>e.Controller==OpenAntares.Simulation.State.ControllerKind.Human).Id);
        originalMain.Hide();
        foreach(int version in new[]{4,7})
        {
            // Explicit pre-session compatibility setup, not an earned campaign or a live rules upgrade.
            var generated=GalaxyGenerator.CreateNewGame(session.Content,new NewGameSettings("small",42,version,"harl",0,null,false,version==7?false:null,null));
            Check(generated.State is not null,"Labelled rules"+version+" UI setup is valid: "+generated.Error);
            var legacySession=new GameSession(session.Content,generated.State!);var owner=generated.State!.Empires.First(e=>e.Controller==OpenAntares.Simulation.State.ControllerKind.Human).Id;
            var legacy=new GameScreen(legacySession,owner);_main=legacy;AddChild(legacy);await Frames();string before=Snapshot(legacySession);
            if(version==4)
            {
                var parent=All<Button>(legacy).Single(b=>b.Name=="OpenDiplomacy");
                bool reachable=parent.IsVisibleInTree()&&!parent.Disabled;Review(reachable,"R1: old Contacts has a visible parent route");
                if(reachable){Press(parent);await Frames();Review(All<Control>(legacy).Single(c=>c.Name=="ContactsWorkspace").IsVisibleInTree(),"R1: old Contacts opens its actual overview");}
            }
            else
            {
                Press(Route("OpenFleetCommand"));await Frames();var scout=All<Button>(legacy).Single(b=>b.Name=="OpenScoutOverview");
                bool reachable=scout.IsVisibleInTree()&&!scout.Disabled;Review(reachable,"R1: rules7 Scouts remains visible in the ship-workspace fallback");
                if(reachable){Press(scout);await Frames();Review(All<ScoutOverviewDialog>(legacy).Single().Visible,"R1: legacy Scouts opens without fleet-command data");}
            }
            Review(Snapshot(legacySession)==before,"R1: legacy route inspection issues no orders");
            RemoveChild(legacy);legacy.QueueFree();await Frames();
        }
        _main=originalMain;originalMain.Show();await Frames();string unchanged=Snapshot(session);
        Press(Route("OpenFleetCommand"));await Frames();var fleet=All<FleetCommandDialog>(screen).Single();
        var own=view.Vessels[0];var ship=view.Fleets!.Ships.Single(s=>s.Id==own.Id);var combat=view.Combat!.Ships.Single(s=>s.Id==own.Id);var second=new VesselId(view.Vessels.Max(v=>v.Id.Value)+10000);
        var clone=own with{Id=second,Crisis=own.Crisis is {} health?health with{Id=second}:null};
        var projection=view with{Vessels=view.Vessels.Add(clone),Fleets=view.Fleets with{Ships=view.Fleets.Ships.Add(ship with{Id=second})},Combat=view.Combat with{Ships=view.Combat.Ships.Add(combat with{Id=second})}};
        fleet.ShowEmpire(session.Content,projection);await Frames();var list=All<FleetList>(fleet).Single();
        Review(list.ItemCount==2&&list.GetRoot().GetChildCount()==1,"R3: equivalent modern ships form one counted group with two exact leaves");
        list.Select(0);list.Select(1,false);list.NotifySelectionChanged();Review(list.GetSelectedItems().Length==2&&list.GetItemMetadata(0).AsInt32()!=list.GetItemMetadata(1).AsInt32(),"R3: stack leaves retain distinct vessel IDs");
        var reserved=projection with{Fleets=projection.Fleets! with{Ships=projection.Fleets.Ships.Select(s=>s.Id==second?s with{Request=new RequestId(20000)}:s).ToImmutableArray()}};
        fleet.ShowEmpire(session.Content,reserved);await Frames();Review(list.GetRoot().GetChildCount()==2,"R3: a different request commitment keeps ships separate");
        fleet.ShowEmpire(session.Content,view);await Frames();fleet.GrabFocus();list.GrabFocus();await Frames();
        Review(fleet.HasFocus()&&list.HasFocus(),"R2: engine keyboard focus is inside the embedded workspace");
        Input.ParseInputEvent(new InputEventKey{Pressed=true,Keycode=Key.C,PhysicalKeycode=Key.C,Unicode=99});await Frames(5);
        Input.ParseInputEvent(new InputEventKey{Pressed=false,Keycode=Key.C,PhysicalKeycode=Key.C});
        Review(All<ColonyLedger>(screen).Single().IsVisibleInTree(),"R2: engine-dispatched C navigates from a focused embedded workspace");
        Press(Route("OpenShipDesigns"));await Frames();var designs=All<ShipDesignDialog>(screen).Single();var search=All<LineEdit>(designs).Single(c=>c.Name=="DesignSearch");designs.GrabFocus();search.GrabFocus();await Frames();
        Input.ParseInputEvent(new InputEventKey{Pressed=true,Keycode=Key.C,PhysicalKeycode=Key.C,Unicode=99});await Frames();Input.ParseInputEvent(new InputEventKey{Pressed=false,Keycode=Key.C,PhysicalKeycode=Key.C});
        Review(designs.Visible,"R2: engine-dispatched typing in a text field does not navigate");
        Press(Route("OpenGalaxyWorkspace"));await Frames();Review(Snapshot(session)==unchanged,"R2/R3: inspection, grouping and hotkeys issue no orders");
        Press(Route("EndTurn"));await Frames(5);var report=All<TurnReportDialog>(screen).Single();Check(report.Visible,"One ordinary turn creates a real report for navigation correction");
        Press(report.GetOkButton());await Frames(5);Press(Route("OpenFleetCommand"));await Frames();Named<MenuButton>("GameMenu").GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed,2L);await Frames();
        Press(Route("OpenGalaxyWorkspace"));await Frames(5);Review(!fleet.Visible&&!report.Visible,"R4: report-to-Galaxy navigation leaves no untracked fleet window");
        await CloseWindows();Press(Route("OpenGalaxyWorkspace"));await Frames();Press(Route("OpenFleetCommand"));await Frames();Named<MenuButton>("GameMenu").GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed,2L);await Frames();Press(report.GetOkButton());await Frames(5);
        Review(fleet.Visible&&!report.Visible,"R4: ordinary report Continue restores its invoking fleet workspace");
        Press(Route("OpenGalaxyWorkspace"));await Frames();foreach(var size in Sizes)await Capture("ReviewedGalaxy",size);
        if(failures.Count>0)throw new InvalidOperationException("Review regressions: "+string.Join("; ",failures));
    }
}
