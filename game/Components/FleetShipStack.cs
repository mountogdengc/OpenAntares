using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Components;

/// <summary>A counted row with lazy exact-ID member choices, shared by map cards and workspaces.</summary>
public partial class FleetShipStack : VBoxContainer
{
    public event Action<ImmutableArray<VesselId>,bool>? SelectionRequested;
    public event Action<VesselId,Vector2>? ContextRequested;
    public event Action<bool>? ExpandedChanged;
    private readonly MapFleetRow _main=new(){ThemeTypeVariation="CommandKey",ToggleMode=true,ClipText=true,SizeFlagsHorizontal=SizeFlags.ExpandFill};
    private readonly Button _expand=new(){ThemeTypeVariation="CompactCommandKey"};
    private readonly VBoxContainer _members=new();
    private readonly Dictionary<VesselId,MapFleetRow> _leaves=new();
    private ContentSet _content=null!;private EmpireView _view=null!;private OwnedVesselView[] _ships=Array.Empty<OwnedVesselView>();
    private HashSet<VesselId> _selected=new();private string _prefix="";private bool _location,_expanded;
    public MapFleetRow MainRow=>_main;
    public override void _Ready()
    {
        SizeFlagsHorizontal=SizeFlags.ExpandFill;
        var header=new HBoxContainer();AddChild(header);header.AddChild(_main);header.AddChild(_expand);AddChild(_members);
        _main.Pressed+=()=>SelectionRequested?.Invoke(_ships.Select(s=>s.Id).ToImmutableArray(),_main.AdditiveSelection);
        _main.ContextRequested+=position=>{if(_ships.Length==1)ContextRequested?.Invoke(_ships[0].Id,position);else SetExpanded(true);};
        _expand.Pressed+=()=>SetExpanded(!_expanded);
    }
    public void Bind(ContentSet content,EmpireView view,OwnedVesselView[] ships,IReadOnlySet<VesselId> selected,string prefix,bool expanded,bool location=false)
    {
        _content=content;_view=view;_ships=ships;_prefix=prefix;_location=location;
        Name="FleetShipStack_"+ships[0].Id.Value;_main.Name=prefix+ships[0].Id.Value;_main.SystemId=ships[0].StationaryStarId?.Value??0;
        _expand.Name="ExpandShipStack_"+ships[0].Id.Value;_expand.Visible=ships.Length>1;
        int target=GetThemeConstant(GetViewportRect().Size.X<1100?"compact_target":"touch_target","ConsoleMetrics");
        _main.CustomMinimumSize=new(0,target);_expand.CustomMinimumSize=new(target,target);
        UpdateSelection(selected);SetExpanded(expanded,false);
    }
    private Texture2D Icon(string role)=>GetThemeIcon(HasThemeIcon("role_"+role,"Icons")?"role_"+role:"escort","Icons");
    public void UpdateSelection(IReadOnlySet<VesselId> selected)
    {
        _selected=selected.ToHashSet();int count=_ships.Count(s=>_selected.Contains(s.Id));
        string status=FleetRoster.Status(_view,_ships,_location);
        if(count>0&&count<_ships.Length)status+=" · "+count+"/"+_ships.Length+" selected";
        _main.Describe(FleetRoster.Title(_content,_view,_ships),status,Icon(_ships[0].RoleId),"Click selects this row. Shift-click adds or removes it. Expand to choose individual ships; right-click an individual for its actions.");
        _main.SetPressedNoSignal(count==_ships.Length);
        foreach(var leaf in _leaves)leaf.Value.SetPressedNoSignal(_selected.Contains(leaf.Key));
    }
    private void SetExpanded(bool expanded,bool notify=true)
    {
        _expanded=expanded&&_ships.Length>1;_expand.Text=_expanded?"−":"+";
        _expand.TooltipText=_expanded?"Hide individual ships":"Show individual ships";
        if(_expanded&&_leaves.Count==0)
            foreach(var ship in _ships)
            {
                var leaf=new MapFleetRow{Name=_prefix.Replace("Row_","Member_")+ship.Id.Value,SystemId=ship.StationaryStarId?.Value??0,ThemeTypeVariation="CommandKey",ToggleMode=true,ClipText=true,SizeFlagsHorizontal=SizeFlags.ExpandFill};
                int target=GetThemeConstant(GetViewportRect().Size.X<1100?"compact_target":"touch_target","ConsoleMetrics");leaf.CustomMinimumSize=new(0,target);
                leaf.Describe(FleetNaming.Ship(_content,_view,ship),FleetRoster.Status(_view,new[]{ship},_location),Icon(ship.RoleId),"Click selects this ship. Shift-click adds or removes it. Right-click for this ship's actions.");
                leaf.Pressed+=()=>SelectionRequested?.Invoke(ImmutableArray.Create(ship.Id),leaf.AdditiveSelection);
                leaf.ContextRequested+=position=>ContextRequested?.Invoke(ship.Id,position);
                leaf.SetPressedNoSignal(_selected.Contains(ship.Id));_members.AddChild(leaf);_leaves.Add(ship.Id,leaf);
            }
        _members.Visible=_expanded;if(notify)ExpandedChanged?.Invoke(_expanded);
    }
    public Control SelectionControl(VesselId id)=>_expanded&&_leaves.TryGetValue(id,out var leaf)?leaf:_main;
}
