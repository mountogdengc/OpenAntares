using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Screens;

public partial class GameScreen
{
    private StarId? _fleetListStar,_hoverDestination;
    private readonly System.Collections.Generic.HashSet<string> _expandedFleetStacks=new(StringComparer.Ordinal);
    private readonly VBoxContainer _mapFleetList=new(){Name="MapFleetList",Visible=false};
    private readonly PopupMenu _mapFleetContext=new(){Name="MapFleetContext"};
    private readonly MapFleetSplitDialog _mapSplit=new();
    private readonly Control _reportDock=new(){Name="TurnReportDock",Visible=false,SizeFlagsVertical=SizeFlags.ExpandFill};
    private Control? _normalConsole;
    private FleetId? _contextFleet;private VesselId? _contextShip;

    private void WireFleetPanel(VBoxContainer console)
    {
        console.AddChild(_mapFleetList);AddChild(_mapFleetContext);AddChild(_mapSplit);
        _mapFleetContext.IdPressed+=id=>
        {
            if(id==0&&_contextFleet is {} fleet)_mapSplit.Open(_session.Content,View,fleet);
            else if(id==1&&_contextShip is {} ship)
            {
                var before=View.Fleets!.Fleets.Select(f=>f.Id).ToHashSet();
                ApplyMapFleetAction(new FleetAction.Create(_session.Content.Vessels[View.Vessels.Single(v=>v.Id==ship).RoleId].Name+" fleet",ImmutableArray.Create(ship)));
                if(View.Fleets.Fleets.FirstOrDefault(f=>!before.Contains(f.Id)) is {} created)SelectFleetRow(created.Id);
            }
        };
        _mapSplit.CommandIssued+=command=>
        {
            var before=View.Fleets!.Fleets.Select(f=>f.Id).ToHashSet();Execute(command);
            if(View.Fleets.Fleets.FirstOrDefault(f=>!before.Contains(f.Id)) is {} created)SelectFleetRow(created.Id);
        };
        _map.SelectionCleared+=ClearGalaxySelection;
        _map.StarClicked+=_=>{if(_mapFleetSelection.Count>0)SetMapCourse();else{_fleetListStar=null;Refresh();}};
        _map.HoverStarChanged+=star=>
        {
            _hoverDestination=star;var view=View;
            ReconcileMapFleetSelection(view);_map.ShowFleetPresentation(view,_mapFleetSelection,star??_courseDestination);
        };
        _report.DockTo(_reportDock);
        _report.VisibilityChanged+=()=>
        {
            _reportDock.Visible=_report.Visible;
            if(_normalConsole is not null)_normalConsole.Visible=!_report.Visible;
            UpdateWorkspaceLayout();
        };
    }
    private void RefreshFleetList(EmpireView view)
    {
        foreach(Node child in _mapFleetList.GetChildren()){_mapFleetList.RemoveChild(child);child.QueueFree();}
        _mapFleetList.Visible=_fleetListStar is not null;
        if(_fleetListStar is not {} star)return;
        string starName=view.Stars.Single(s=>s.Chart.Id==star).Chart.Name;
        _mapFleetList.AddChild(new Label{Text="FLEETS AT "+starName.ToUpperInvariant(),ThemeTypeVariation="ConsoleHeading",AutowrapMode=TextServer.AutowrapMode.WordSmart});
        var ships=view.Vessels.Where(v=>v.StationaryStarId==star).OrderBy(v=>v.Id).ToArray();
        if(view.Fleets is {} fleets)
            foreach(var fleet in fleets.Fleets.Where(f=>f.Members.Any(id=>ships.Any(v=>v.Id==id))).OrderBy(f=>f.Id))
            {
                var members=ships.Where(v=>fleet.Members.Contains(v.Id)).ToArray();
                var row=new MapFleetRow{Fleet=fleet.Id,SystemId=star.Value,Name="MapFleetRow_"+fleet.Id.Value,ThemeTypeVariation="CommandKey",ToggleMode=true,ClipText=true,SizeFlagsHorizontal=SizeFlags.ExpandFill};
                row.Describe(FleetNaming.Fleet(fleet),FleetNaming.Status(view,members,fleet),RoleIcon((members.FirstOrDefault(v=>v.Id==fleet.Flagship)??members[0]).RoleId),"Click to select. Shift-click to add or remove. Right-click to split. Drag onto another fleet to merge.");
                row.SetPressedNoSignal(fleet.Members.All(_mapFleetSelection.Contains));row.Pressed+=()=>SelectFleetRow(fleet.Id,row.AdditiveSelection);
                row.ContextRequested+=position=>OpenFleetContext(fleet.Id,null,position);
                row.MergeRejection=(source,target)=>FleetQueries.PreviewAction(_session.Content,View,new FleetAction.Merge(target,ImmutableArray.Create(source))).Rejection;
                row.MergeRequested+=(source,target)=>{ApplyMapFleetAction(new FleetAction.Merge(target,ImmutableArray.Create(source)));SelectFleetRow(target);};
                _mapFleetList.AddChild(row);
            }
        foreach(var group in ships.Where(v=>view.Fleets?.Ships.Single(s=>s.Id==v.Id).Fleet is null).GroupBy(v=>FleetRoster.Key(view,v)))
        {
            var stack=new FleetShipStack();_mapFleetList.AddChild(stack);
            stack.Bind(_session.Content,view,group.ToArray(),_mapFleetSelection,"MapShipRow_",_expandedFleetStacks.Contains(group.Key));
            stack.SelectionRequested+=SelectMapShips;stack.ContextRequested+=(ship,position)=>OpenFleetContext(null,ship,position);
            stack.ExpandedChanged+=expanded=>{if(expanded)_expandedFleetStacks.Add(group.Key);else _expandedFleetStacks.Remove(group.Key);};
        }
        if(ships.Length==0)_mapFleetList.AddChild(new Label{Text="No fleets remain here.",ThemeTypeVariation="SubtleLabel"});
        AddFleetCardDetails(view,star,starName,ships);
    }
    private void SelectFleetRow(FleetId fleet,bool additive=false)
    {
        if(View.Fleets?.Fleets.FirstOrDefault(f=>f.Id==fleet) is not {} group)return;
        SelectMapShips(group.Members,additive);
    }
    private void SelectMapShips(ImmutableArray<VesselId> members,bool additive)
    {
        bool remove=additive&&members.All(_mapFleetSelection.Contains);
        if(!additive)_mapFleetSelection.Clear();
        if(remove)_mapFleetSelection.ExceptWith(members);else _mapFleetSelection.UnionWith(members);
        _selectedVessel=_mapFleetSelection.Count>0?_mapFleetSelection.OrderBy(id=>id).First():null;
        _courseDestination=null;_hoverDestination=null;Refresh();
    }
    private void OpenFleetContext(FleetId? fleet,VesselId? ship,Vector2 position)
    {
        _contextFleet=fleet;_contextShip=ship;_mapFleetContext.Clear();
        if(fleet is not null)_mapFleetContext.AddItem("Split fleet…",0);
        else if(View.Fleets is not null)_mapFleetContext.AddItem("Form fleet",1);
        else return;
        _mapFleetContext.Popup(new Rect2I((Vector2I)position,Vector2I.Zero));
    }
    private void ApplyMapFleetAction(FleetAction action)
    {
        var view=View;var preview=FleetQueries.PreviewAction(_session.Content,view,action);
        if(!preview.Allowed){ShowMessage(preview.Rejection??"This fleet action is unavailable.",true);return;}
        Execute(new FleetCommand(view.Viewer,view.Turn,action){Fingerprint=preview.Fingerprint});
    }
    private void ClearGalaxySelection()
    {
        ClearMapFleetSelection();_fleetListStar=null;_hoverDestination=null;_selectedVessel=null;_selectedPlanet=null;_selectedColony=null;_requireVesselChoice=true;_requireColonyChoice=true;
        foreach(Node child in _starPanel.GetChildren()){_starPanel.RemoveChild(child);child.QueueFree();}
        Refresh();
    }
}
