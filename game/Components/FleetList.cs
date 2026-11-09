using System.Collections.Generic;
using System.Linq;
using Godot;

namespace OpenAntares.Game.Components;

/// <summary>Godot Tree draws only visible rows; all selected leaves retain their exact vessel IDs.</summary>
public partial class FleetList : Tree
{
    private readonly List<TreeItem> _rows = new();
    private readonly Dictionary<string, TreeItem> _groups = new();
    private readonly Dictionary<TreeItem, string> _titles = new();
    private readonly HashSet<string> _collapsed = new(System.StringComparer.Ordinal);
    private TreeItem? _root, _group;
    public int ItemCount => _rows.Count;
    public FleetList() { HideRoot = true; SelectMode = SelectModeEnum.Multi; }
    public override void _Ready() { MultiSelected += (item, column, selected) => { if (!_groups.Values.Contains(item)) return; foreach (TreeItem leaf in _rows.Where(row => row.GetParent() == item)) { if(selected) leaf.Select(0); else leaf.Deselect(0); } }; ItemCollapsed += item => { string key = item.GetMetadata(0).AsString(); if (item.Collapsed) _collapsed.Add(key); else _collapsed.Remove(key); }; }
    public new void Clear()
    {
        base.Clear(); _rows.Clear(); _groups.Clear(); _titles.Clear(); _root = CreateItem(); _group = _root;
    }
    public void BeginGroup(string key, string title)
    {
        if (!_groups.TryGetValue(key, out _group))
        {
            _group = CreateItem(_root); _groups.Add(key, _group);
            _titles[_group] = title;
            _group.SetText(0, title); _group.SetMetadata(0, key); _group.SetSelectable(0, true); _group.Collapsed = _collapsed.Contains(key);
        }
    }
    public void AddItem(string text) { var leaf = CreateItem(_group ?? _root); leaf.SetText(0, text); _rows.Add(leaf); if (_group is not null && _titles.TryGetValue(_group,out var title)) _group.SetText(0,title+"  ×"+_group.GetChildCount()); }
    public void SetItemMetadata(int index, Variant metadata) => _rows[index].SetMetadata(0, metadata);
    public void SetItemTooltip(int index, string text) => _rows[index].SetTooltipText(0, text);
    public string GetItemText(int index) => _rows[index].GetText(0);
    public Variant GetItemMetadata(int index) => _rows[index].GetMetadata(0);
    public void NotifySelectionChanged() => EmitSignal(Tree.SignalName.MultiSelected, _rows.FirstOrDefault(r => r.IsSelected(0)) ?? _root!, 0L, true);
    public int[] GetSelectedItems() => _rows.Select((item, index) => (item, index)).Where(x => x.item.IsSelected(0)).Select(x => x.index).ToArray();
    public void Select(int index, bool single = true) { if (single) DeselectAll(); _rows[index].Select(0); }
    public void EnsureCurrentIsVisible() { if (GetSelectedItems().FirstOrDefault(-1) is int index && index >= 0) ScrollToItem(_rows[index]); }
}
