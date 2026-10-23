using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class ColonyList : VBoxContainer
{
    public event Action<ColonyId>? ColonySelected;
    public void ShowColonies(EmpireView view, ColonyId? selected)
    {
        foreach (Node child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        Visible = view.RulesVersion == Rules.ExpansionVersion;
        if (!Visible) return;
        AddChild(new Label { Text = "Your colonies", ThemeTypeVariation = "HeaderLabel" });
        var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill }; AddChild(picker);
        foreach (var c in view.Colonies)
        {
            string name = view.Research.ColonyNames[c.Id.ToString()]; picker.AddItem($"{name} — population {c.Population}"); picker.SetItemMetadata(picker.ItemCount - 1, c.Id.Value);
            if (c.Id == selected) picker.Select(picker.ItemCount - 1);
        }
        picker.ItemSelected += index => ColonySelected?.Invoke(new(picker.GetItemMetadata((int)index).AsInt32()));
    }
}
