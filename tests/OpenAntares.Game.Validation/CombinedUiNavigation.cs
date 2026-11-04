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
    internal static T Reveal<T>(T control) where T:Control
    {
        if(control.IsVisibleInTree()) return control;
        GameScreen? screen=null; string? workspace=null; Control? host=null;
        string originalName=control.Name, tooltip=control.TooltipText;
        string? text=control is Button action ? action.Text : control is Label label ? label.Text : null;
        for(Node? node=control;node is not null;node=node.GetParent())
        {
            if(node is ColonyPanel panel) {workspace="OpenColonyWorkspace";host=panel;}
            else if(node is ColonyList panelColonyList) {workspace="OpenColonyOverview";host=panelColonyList;}
            else if(node is VesselPanel panelVesselPanel) {workspace="OpenVesselWorkspace";host=panelVesselPanel;}
            else if(node is StarPanel panelStarPanel) {workspace="InspectSelectedSystem";host=panelStarPanel;}
            else if(node is ContactsPanel panelContactsPanel) {workspace="OpenContactsWorkspace";host=panelContactsPanel;}
            else if(node is ResearchBar panelResearchBar) {workspace="OpenGalaxyWorkspace";host=panelResearchBar;}
            else if(node.Name=="EmpireConsole") {workspace="OpenGalaxyWorkspace";host=(Control)node;}
            if(node is GameScreen owner) {screen=owner;break;}
        }
        if(screen is null || workspace is null) return control;
        void Navigate(string name)
        {
            var button=All<Button>(screen).Single(b=>b.Name==name);
            if(button.Disabled || !button.IsVisibleInTree()) throw new InvalidOperationException("Workspace navigation unreachable: "+name);
            button.EmitSignal(Button.SignalName.Pressed);
        }
        if(workspace is "OpenColonyOverview" or "OpenVesselWorkspace" or "InspectSelectedSystem") Navigate("OpenGalaxyWorkspace");
        Navigate(workspace);
        if(control.IsInsideTree() && control.IsVisibleInTree()) return control;
        // Workspace navigation may synchronously refresh a panel. Reacquire the
        // corresponding current control rather than dispatching to a detached one.
        var candidates=All<T>(host ?? screen).Where(c=>c.IsVisibleInTree() && c.GetType()==control.GetType());
        T? replacement = originalName.StartsWith("@",StringComparison.Ordinal)
            ? candidates.SingleOrDefault(c=>c.TooltipText==tooltip && (c is Button b ? b.Text : c is Label l ? l.Text : null)==text)
            : candidates.SingleOrDefault(c=>c.Name==originalName);
        return replacement ?? throw new InvalidOperationException("Workspace did not reveal control: "+originalName);
    }
}
