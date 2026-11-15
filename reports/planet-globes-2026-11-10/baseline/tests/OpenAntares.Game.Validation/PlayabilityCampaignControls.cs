using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Validation;

internal sealed partial class PlayabilityCampaignControls
{
    private readonly PlayabilityCampaignObserver _observer;
    private readonly Func<GameScreen> _current;
    private GameScreen Screen=>_current();
    private EmpireView View=>_observer.Game.View(_observer.Owner);
    private static System.Collections.Generic.IEnumerable<T> All<T>(Node root) where T:Node=>PlayabilityCampaignObserver.Controls<T>(root);
    private static T Named<T>(Node root,string name) where T:Node=>All<T>(root).Single(n=>n.Name==name);
    public PlayabilityCampaignControls(PlayabilityCampaignObserver observer,Func<GameScreen> current){_observer=observer;_current=current;}
    private async Task Click(Button button)
    {
        button=CombinedUiNavigation.Reveal(button);
        if(button.Disabled||!button.IsVisibleInTree())throw new InvalidOperationException("Campaign action unavailable: "+button.Name);
        for(Node? node=button.GetParent();node is not null;node=node.GetParent())if(node is ScrollContainer scroll)scroll.EnsureControlVisible(button);
        button.GrabFocus();await _observer.Frames();button.EmitSignal(Button.SignalName.Pressed);await _observer.Frames();
    }
    private Task Click(Node root,string name)=>Click(Named<Button>(root,name));
    private async Task Pick(OptionButton picker,int index)
    {
        picker=CombinedUiNavigation.Reveal(picker);
        if(index<0||index>=picker.ItemCount||picker.Disabled||picker.IsItemDisabled(index))throw new InvalidOperationException("Campaign picker choice unavailable: "+picker.Name);
        picker.Select(index);picker.EmitSignal(OptionButton.SignalName.ItemSelected,(long)index);await _observer.Frames();
    }
    private Task PickId(OptionButton picker,int id)=>Pick(picker,Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsInt32()==id));
    private Task PickKey(OptionButton picker,string key)=>Pick(picker,Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsString()==key));
    public Task CloseWindows()=>_observer.Navigate("Close transient windows",async()=>
    {
        for(int i=0;i<3;i++){foreach(var window in All<Window>(Screen).Where(w=>w.Visible))window.Hide();await _observer.Frames(3);}
    });
    public Task SelectColony(ColonyId colony)=>_observer.Navigate("Select exact owned colony",async()=>
    {
        await Click(Screen,"OpenGalaxyWorkspace");await Click(Screen,"OpenAttentionConsole");await Click(Screen,"OpenColonyOverview");
        var list=All<ColonyList>(Screen).Single();await PickId(All<OptionButton>(list).Single(),colony.Value);
    });
    public Task SelectVessel(VesselId vessel)=>_observer.Navigate("Select exact owned vessel",async()=>
    {
        await Click(Screen,"OpenFleetCommand");
        var ships=Named<FleetList>(Screen,"FleetShipList");ships.DeselectAll();
        int index=Enumerable.Range(0,ships.ItemCount).Single(i=>ships.GetItemMetadata(i).AsInt32()==vessel.Value);ships.Select(index);
        await Click(Screen,"FleetInspectShip");await PickId(Named<OptionButton>(Screen,"VesselPicker"),vessel.Value);
    });
    private Task SelectStar(StarId star)=>_observer.Navigate("Select known system",async()=>
    {
        await Click(Screen,"OpenGalaxyWorkspace");
        var map=All<GalaxyMap>(Screen).Single();
        map.Deselect();await _observer.Frames();
        map.Select(star);await _observer.Frames();
    });
    private Task SelectPlanet(PlanetId planet,VesselId vessel)=>_observer.Navigate("Inspect exact known founding world",async()=>
    {
        var star=View.Stars.Single(s=>s.Planets?.Any(p=>p.Id==planet)==true);var world=star.Planets!.Value.Single(p=>p.Id==planet);
        await SelectStar(star.Chart.Id);await Click(Screen,"InspectSelectedSystem");
        await Click(All<Button>(All<StarPanel>(Screen).Single()).Single(b=>b.Text=="Select planet "+world.Orbit));
        await Click(Screen,"OpenGalaxyWorkspace");
        var map=All<GalaxyMap>(Screen).Single();
        map._GuiInput(new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=map.OwnedMarkerBounds[star.Chart.Id].GetCenter()});await _observer.Frames();
        await Click(Screen,"OpenVesselWorkspace");await PickId(Named<OptionButton>(Screen,"VesselPicker"),vessel.Value);
    });
    public async Task Workforce(ColonyId colony,Workforce target)
    {
        await CloseWindows();await SelectColony(colony);
        for(int i=0;i<256;i++)
        {
            var current=View.Colonies.Single(c=>c.Id==colony).Workforce;if(current==target)return;
            string control=current.Research>target.Research?"ResearchWorkforceLess":current.Production>target.Production?"ProductionWorkforceLess":current.Production<target.Production?"ProductionWorkforceMore":"ResearchWorkforceMore";
            await _observer.ExecuteUi("Workforce "+control,()=>Click(All<ColonyPanel>(Screen).Single(),control));
        }
        throw new InvalidOperationException("Workforce action bound exceeded.");
    }
    public async Task Production(ColonyId colony,ProductionTarget? target)
    {
        await CloseWindows();await SelectColony(colony);
        string key=target is null?"":target.Kind==ProductionKinds.Building?target.ContentId:target.Kind+"|"+target.ContentId;
        await _observer.ExecuteUi("Select production",()=>PickKey(Named<OptionButton>(All<ColonyPanel>(Screen).Single(),"ColonyProjectPicker"),key));
    }
    public async Task Research(string technology)
    {
        await CloseWindows();await _observer.Navigate("Open research choices",()=>Click(Screen,"OpenGalaxyWorkspace"));
        await _observer.ExecuteUi("Select research",()=>PickKey(Named<OptionButton>(Screen,"ResearchTargetPicker"),technology));
    }
    public async Task Course(VesselId vessel,StarId destination)
    {
        await CloseWindows();await SelectVessel(vessel);
        await _observer.Navigate("Preview ordinary route",()=>PickId(Named<OptionButton>(Screen,"DestinationPicker"),destination.Value));
        await _observer.ExecuteUi("Set vessel course",()=>Click(Screen,"SetVesselCourse"));
    }
    public async Task Found(VesselId vessel,PlanetId planet)
    {
        await CloseWindows();await SelectVessel(vessel);await SelectPlanet(planet,vessel);
        await _observer.ExecuteUi("Found exact selected world",()=>Click(All<Button>(All<VesselPanel>(Screen).Single()).Single(b=>b.Text=="Found colony")));
    }
    public async Task Ready()
    {
        await CloseWindows();await _observer.ExecuteUi("EndTurn",()=>Click(Screen,"EndTurn"));
    }
    private async Task Ships(FleetCommandDialog dialog,ImmutableArray<VesselId> ids)
    {
        Named<TabContainer>(dialog,"FleetTabs").CurrentTab=0;await _observer.Frames();var list=Named<FleetList>(dialog,"FleetShipList");list.DeselectAll();
        foreach(var id in ids){int index=Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemMetadata(i).AsInt32()==id.Value);list.Select(index,false);}
        if(list.ItemCount>0)list.NotifySelectionChanged();await _observer.Frames();
    }
    private async Task Groups(FleetCommandDialog dialog,ImmutableArray<FleetId> ids)
    {
        Named<TabContainer>(dialog,"FleetTabs").CurrentTab=1;await _observer.Frames();var list=Named<FleetRowList>(dialog,"FleetGroupList");list.DeselectAll();var rows=View.Fleets!.Fleets.OrderBy(f=>f.Id).ToArray();
        foreach(var id in ids){int index=Array.FindIndex(rows,f=>f.Id==id);if(index<0)throw new InvalidOperationException("Missing owned fleet row.");list.Select(index,false);}
        if(list.ItemCount>0)list.EmitSignal(ItemList.SignalName.MultiSelected,0L,true);await _observer.Frames();
    }
    public async Task Fleet(FleetAction action)
    {
        await CloseWindows();await _observer.Navigate("Open fleet command",()=>Click(Screen,"OpenFleetCommand"));var dialog=All<FleetCommandDialog>(Screen).Single();string button;
        await _observer.Navigate("Configure exact fleet selection",async()=>
        {
            switch(action)
            {
                case FleetAction.Create create:await Ships(dialog,create.Members);Named<LineEdit>(dialog,"FleetName").Text=create.Name;break;
                case FleetAction.DismissNewMarker dismiss:await Ships(dialog,dismiss.Vessels);break;
                case FleetAction.DetachSelection detach:await Ships(dialog,detach.Vessels);break;
                case FleetAction.Merge merge:await Groups(dialog,merge.Sources.Add(merge.Target));break;
                case FleetAction.MoveSelection move:
                    if(!move.Fleets.IsEmpty){await Groups(dialog,move.Fleets);await PickId(Named<OptionButton>(dialog,"FleetGroupDestination"),move.Destination?.Value??0);}
                    else{await Ships(dialog,move.Vessels);if(move.Destination is {} star)await PickId(Named<OptionButton>(dialog,"FleetDestination"),star.Value);}break;
                case FleetAction.SetOrder order:await Groups(dialog,order.Fleets);await Pick(Named<OptionButton>(dialog,"FleetStandingOrder"),(int)order.Order.Kind);if(order.Order.Destination is {} target)await PickId(Named<OptionButton>(dialog,"FleetGroupDestination"),target.Value);break;
                case FleetAction.Split split:
                    await Groups(dialog,ImmutableArray.Create(split.Source));Named<LineEdit>(dialog,"FleetGroupName").Text=split.Name;
                    var members=Named<FleetRowList>(dialog,"FleetMemberList");members.DeselectAll();var rows=View.Fleets!.Fleets.Single(f=>f.Id==split.Source).Members.OrderBy(id=>id).ToArray();
                    foreach(var id in split.Members){int index=Array.IndexOf(rows,id);if(index<0)throw new InvalidOperationException("Missing owned member row.");members.Select(index,false);}break;
                default:throw new InvalidOperationException("Unsupported ordinary fleet UI action: "+action.GetType().Name);
            }
        });
        button=action switch{FleetAction.Create=>"FleetCreate",FleetAction.DismissNewMarker=>"FleetDismissNew",FleetAction.DetachSelection=>"FleetDetach",FleetAction.Merge=>"FleetMerge",FleetAction.SetOrder=>"FleetSetStandingOrder",FleetAction.Split=>"FleetSplit",FleetAction.MoveSelection m when !m.Fleets.IsEmpty=>"FleetPreviewGroupMove",FleetAction.MoveSelection m when m.Destination is null=>"FleetCancelMoves",FleetAction.MoveSelection=>"FleetPreviewMove",_=>throw new InvalidOperationException()};
        await _observer.Navigate("Review fleet consequence",()=>Click(dialog,button));
        var confirm=Named<ConfirmationDialog>(dialog,"FleetActionConfirmation");
        await _observer.ExecuteUi("Apply reviewed fleet "+action.GetType().Name,()=>Click(confirm.GetOkButton()));
    }
}
