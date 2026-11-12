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
    internal static void ExpandSetup(NewGameScreen setup)
    {
        var options=All<Control>(setup).Single(c=>c.Name=="AdvancedSetupOptions");
        if(options.Visible)return;
        var toggle=All<Button>(setup).Single(c=>c.Name=="AdvancedSetupToggle");
        if(toggle.Disabled||!toggle.IsVisibleInTree())throw new InvalidOperationException("Advanced setup entry is unreachable");
        toggle.EmitSignal(Button.SignalName.Pressed);
        if(!options.Visible)throw new InvalidOperationException("Advanced setup did not expand");
    }
    internal static T Reveal<T>(T control) where T:Control
    {
        for(Node? parent=control;parent is not null;parent=parent.GetParent())
            if(parent is NewGameScreen setup)
            {
                var options=All<Control>(setup).Single(c=>c.Name=="AdvancedSetupOptions");
                if(options.IsAncestorOf(control))ExpandSetup(setup);
                break;
            }
        if(control.Name=="OpenScoutOverview" || control.Name=="OpenContactsWorkspace")
        {
            GameScreen? owner=null;for(Node? n=control;n is not null;n=n.GetParent())if(n is GameScreen found){owner=found;break;}
            if(owner is not null&&!control.IsVisibleInTree())All<Button>(owner).Single(b=>b.Name==(control.Name=="OpenScoutOverview"?"OpenFleetCommand":"OpenDiplomacy")&&b.GetWindow()==owner.GetWindow()).EmitSignal(BaseButton.SignalName.Pressed);
        }
        if(control.IsVisibleInTree()) return control;
        GameScreen? screen=null; string? workspace=null; Control? host=null;
        string originalName=control.Name, tooltip=control.TooltipText;
        string? text=control is Button action ? action.Text : control is Label label ? label.Text : null;
        for(Node? node=control;node is not null;node=node.GetParent())
        {
            if(node.Name=="DecisionsWorkspace") {workspace="OpenAttentionConsole";host=(Control)node;}
            else if(node is ColonyPanel panel) {workspace="ManageSelectedColony";host=panel;}
            else if(node is ColonyList panelColonyList) {workspace="OpenColonyOverview";host=panelColonyList;}
            else if(node is VesselPanel panelVesselPanel) {workspace="VesselInspector";host=panelVesselPanel;}
            else if(node is StarPanel panelStarPanel) {workspace="InspectSelectedSystem";host=panelStarPanel;}
            else if(node is ContactsPanel panelContactsPanel) {workspace="OpenContactsWorkspace";host=panelContactsPanel;}
            else if(node is ResearchBar panelResearchBar) {workspace="OpenResearch";host=panelResearchBar;}
            else if(node.Name=="EmpireConsole") {workspace="OpenGalaxyWorkspace";host=(Control)node;}
            if(node is GameScreen owner) {screen=owner;break;}
        }
        if(screen is null || workspace is null) return control;
        void Navigate(string name)
        {
            var button=All<Button>(screen).Single(b=>b.Name==name&&b.GetWindow()==screen.GetWindow());
            if(button.Disabled || !button.IsVisibleInTree()) throw new InvalidOperationException("Workspace navigation unreachable: "+name);
            button.EmitSignal(Button.SignalName.Pressed);
        }
        if(workspace=="OpenColonyOverview") Navigate("OpenAttentionConsole");
        if(workspace is "InspectSelectedSystem" or "ManageSelectedColony")
        {
            Navigate("OpenGalaxyWorkspace");
            var system=All<Button>(screen).SingleOrDefault(b=>b.Name=="FleetCardShowSystem");
            if(system is not null&&system.IsVisibleInTree())system.EmitSignal(BaseButton.SignalName.Pressed);
        }
        if(workspace=="VesselInspector")
        {
            var picker=All<OptionButton>(host!).Single(p=>p.Name=="VesselPicker");
            int? selected=picker.Selected>=0?picker.GetSelectedMetadata().AsInt32():null;
            Navigate("OpenFleetCommand");
            var list=All<FleetList>(screen).Single();
            if(list.ItemCount==0)throw new InvalidOperationException("No owned vessel is available for inspection");
            int index=selected is {} id?Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemMetadata(i).AsInt32()==id):0;
            list.DeselectAll();list.Select(index);
            var inspector=All<Button>(screen).Single(b=>b.Name=="FleetInspectShip");
            if(inspector.Disabled||!inspector.IsVisibleInTree())throw new InvalidOperationException("Fleet inspector is unreachable");
            inspector.EmitSignal(BaseButton.SignalName.Pressed);
        }
        if(workspace=="OpenContactsWorkspace") Navigate("OpenDiplomacy");
        if(workspace!="VesselInspector")Navigate(workspace);
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
