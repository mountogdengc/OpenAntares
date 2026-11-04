using System;
using System.Linq;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
namespace OpenAntares.Game.Validation;

/// <summary>Navigate the production shell through real controls before exercising its workspaces.</summary>
internal static class CombinedUiNavigation
{
    private static System.Collections.Generic.IEnumerable<T> All<T>(Node root) where T:Node
    {
        foreach(Node child in root.GetChildren()) { if(child is T value) yield return value; foreach(var nested in All<T>(child)) yield return nested; }
    }
    internal static void GameMenu(GameScreen screen, int id)
    {
        var menu=All<MenuButton>(screen).Single(b=>b.Name=="GameMenu");
        if(!menu.IsVisibleInTree() || menu.Disabled) throw new InvalidOperationException("Game menu is unreachable");
        menu.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed,(long)id);
    }
    internal static void Reveal(Control control)
    {
        if(control.IsVisibleInTree()) return;
        GameScreen? screen=null; string? workspace=null;
        for(Node? node=control;node is not null;node=node.GetParent())
        {
            if(node is ColonyPanel) workspace="OpenColonyWorkspace";
            else if(node is ColonyList) workspace="OpenColonyOverview";
            else if(node is VesselPanel) workspace="OpenVesselWorkspace";
            else if(node is StarPanel) workspace="InspectSelectedSystem";
            else if(node is ContactsPanel) workspace="OpenContactsWorkspace";
            else if(node is ResearchBar || node.Name=="EmpireConsole") workspace="OpenGalaxyWorkspace";
            if(node is GameScreen owner) {screen=owner;break;}
        }
        if(screen is null || workspace is null) return;
        void Navigate(string name)
        {
            var button=All<Button>(screen).Single(b=>b.Name==name);
            if(button.Disabled || !button.IsVisibleInTree()) throw new InvalidOperationException("Workspace navigation unreachable: "+name);
            button.EmitSignal(Button.SignalName.Pressed);
        }
        if(workspace is "OpenColonyOverview" or "OpenVesselWorkspace" or "InspectSelectedSystem") Navigate("OpenGalaxyWorkspace");
        Navigate(workspace);
        if(!control.IsVisibleInTree()) throw new InvalidOperationException("Workspace did not reveal control: "+control.Name);
    }
}
