using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Components;

/// <summary>Shared row roster; compatibility indices always address exact entities, never a stack representative.</summary>
public partial class FleetRowList : ScrollContainer
{
    [Signal] public delegate void MultiSelectedEventHandler(long index,bool selected);
    private sealed class Entry { public Variant Metadata;public string Text="";public MapFleetRow? Row;public FleetShipStack? Stack;public VesselId? Vessel; }
    private readonly VBoxContainer _body=new(){SizeFlagsHorizontal=SizeFlags.ExpandFill};
    private readonly List<Entry> _rows=new();private readonly List<FleetShipStack> _stacks=new();
    private readonly HashSet<int> _selected=new();private readonly HashSet<string> _expanded=new(StringComparer.Ordinal);
    public int ItemCount=>_rows.Count;
    public int GroupCount=>_stacks.Count+_rows.Count(r=>r.Row is not null);
    public FleetRowList(){HorizontalScrollMode=ScrollMode.Disabled;FollowFocus=true;FocusMode=FocusModeEnum.All;}
    public override void _Ready()=>AddChild(_body);
    public void Clear()
    {
        foreach(Node child in _body.GetChildren()){_body.RemoveChild(child);child.QueueFree();}
        _rows.Clear();_stacks.Clear();_selected.Clear();
    }
    public void ShowShips(ContentSet content,EmpireView view,OwnedVesselView[] ships)
    {
        Clear();var indices=new Dictionary<VesselId,int>();
        foreach(var ship in ships){indices.Add(ship.Id,_rows.Count);_rows.Add(new(){Metadata=ship.Id.Value,Vessel=ship.Id,Text=FleetNaming.Ship(content,view,ship)+" · "+FleetRoster.Status(view,new[]{ship},true)});}
        foreach(var group in ships.GroupBy(s=>FleetRoster.Key(view,s)))
        {
            var members=group.ToArray();var stack=new FleetShipStack();_body.AddChild(stack);_stacks.Add(stack);
            stack.Bind(content,view,members,new HashSet<VesselId>(),"FleetShipRow_",_expanded.Contains(group.Key),true);
            foreach(var ship in members)_rows[indices[ship.Id]].Stack=stack;
            stack.SelectionRequested+=(ids,additive)=>Choose(ids.Select(id=>indices[id]).ToArray(),additive);
            stack.ExpandedChanged+=expanded=>{if(expanded)_expanded.Add(group.Key);else _expanded.Remove(group.Key);};
        }
    }
    public void AddRow(string title,string status,Texture2D? icon)
    {
        int index=_rows.Count;var row=new MapFleetRow{Name="RosterRow_"+index,ThemeTypeVariation="CommandKey",ToggleMode=true,ClipText=true,SizeFlagsHorizontal=SizeFlags.ExpandFill};
        _body.AddChild(row);int target=GetThemeConstant(GetViewportRect().Size.X<1100?"compact_target":"touch_target","ConsoleMetrics");row.CustomMinimumSize=new(0,target);
        row.Describe(title,status,icon,"Click to select. Shift-click to add or remove.");_rows.Add(new(){Row=row});row.Pressed+=()=>Choose(new[]{index},row.AdditiveSelection);
    }
    public void SetItemMetadata(int index,Variant value)=>_rows[index].Metadata=value;
    public Variant GetItemMetadata(int index)=>_rows[index].Metadata;
    public string GetItemText(int index)=>_rows[index].Row?.Text??_rows[index].Text;
    public int[] GetSelectedItems()=>_selected.OrderBy(i=>i).ToArray();
    public void DeselectAll(){_selected.Clear();Sync();}
    public void Select(int index,bool single=true){if(single)_selected.Clear();_selected.Add(index);Sync();}
    public void RestoreShips(IReadOnlySet<VesselId> selected)
    {
        _selected.Clear();for(int i=0;i<_rows.Count;i++)if(_rows[i].Vessel is {} id&&selected.Contains(id))_selected.Add(i);Sync();
    }
    private void Choose(int[] indices,bool additive)
    {
        bool remove=additive&&indices.All(_selected.Contains);if(!additive)_selected.Clear();
        if(remove)_selected.ExceptWith(indices);else _selected.UnionWith(indices);Sync();NotifySelectionChanged();
    }
    private void Sync()
    {
        foreach(var row in _rows.Select((entry,index)=>(entry,index)))row.entry.Row?.SetPressedNoSignal(_selected.Contains(row.index));
        var ids=_selected.Where(i=>_rows[i].Vessel is not null).Select(i=>_rows[i].Vessel!.Value).ToHashSet();
        foreach(var stack in _stacks)stack.UpdateSelection(ids);
    }
    public void NotifySelectionChanged()=>EmitSignal(SignalName.MultiSelected,(long)_selected.FirstOrDefault(-1),_selected.Count>0);
    public void EnsureCurrentIsVisible()
    {
        if(_selected.FirstOrDefault(-1) is int index&&index>=0){var row=_rows[index];EnsureControlVisible(row.Row??row.Stack!.SelectionControl(row.Vessel!.Value));}
    }
}
