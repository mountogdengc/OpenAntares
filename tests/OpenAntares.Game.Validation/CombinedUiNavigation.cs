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
    /// <summary>Opens the system view for the selected star through its real button and returns one planet on the orbit diagram.</summary>
    internal static Button OrbitPlanet(Node root, int orbit)
    {
        var screen=root as GameScreen ?? All<GameScreen>(root).Single();
        var panel=All<StarPanel>(screen).Single();
        if(!panel.IsVisibleInTree())
        {
            var inspect=All<Button>(screen).Single(b=>b.Name=="InspectSelectedSystem"&&b.GetWindow()==screen.GetWindow());
            if(inspect.Disabled)throw new InvalidOperationException("System view is unreachable: no star selected");
            inspect.EmitSignal(BaseButton.SignalName.Pressed);
        }
        var planet=All<Button>(panel).Single(b=>b.Name=="OrbitPlanet_"+orbit);
        if(!planet.IsVisibleInTree())throw new InvalidOperationException("Orbit planet is unreachable: "+orbit);
        return planet;
    }
    /// <summary>Command-bar pages toggle closed when pressed while showing; true when this entry's window is already open.</summary>
    /// <summary>Returns the live control when a refresh replaced <paramref name="stale"/> while awaiting frames.</summary>
    internal static T Current<T>(T stale,Node root,string name,string? text=null) where T:Control
    {
        if(GodotObject.IsInstanceValid(stale))return stale;
        var live=All<T>(root).Where(c=>c.IsVisibleInTree()&&(name.StartsWith("@",StringComparison.Ordinal)
            // Unnamed controls get a fresh generated name on every rebuild; match those by their button text.
            ? text is not null&&c is Button b&&b.Text==text : c.Name==name)).ToArray();
        return live.Length==1?live[0]:throw new InvalidOperationException("Refreshed control could not be reacquired: "+name+(text is null?"":$" \"{text}\""));
    }
    /// <summary>Names the first hidden ancestor (or window) for failure messages.</summary>
    internal static string HiddenAt(Control c){for(Node? n=c;n is not null;n=n.GetParent())if(n is CanvasItem i&&!i.Visible||n is Window w&&!w.Visible)return n.Name+":"+n.GetType().Name;return c.IsVisibleInTree()?"visible":"window "+c.GetWindow().Name;}
    internal static bool AlreadyOpen(Control control)
    {
        if(control.Name!="OpenFleetCommand"&&control.Name!="OpenDiplomacy")return false;
        GameScreen? screen=null;for(Node? n=control;n is not null;n=n.GetParent())if(n is GameScreen found){screen=found;break;}
        if(screen is null)return false;
        return control.Name=="OpenFleetCommand"?All<FleetCommandDialog>(screen).Any(w=>w.Visible):All<DiplomacyDialog>(screen).Any(w=>w.Visible);
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
        // Decisions hides when nothing needs attention; Colonies -> Plans is the always-present route to the colony tools.
        // Command keys toggle their page closed when pressed twice, so start from the map.
        if(workspace=="OpenColonyOverview") { Navigate("OpenGalaxyWorkspace"); Navigate("OpenColonyWorkspace"); workspace="OpenColonyPlans"; }
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
            // Inspecting from Fleets clears the course choice; carry a destination already picked on the hidden panel.
            var course=All<OptionButton>(host!).SingleOrDefault(p=>p.Name=="DestinationPicker");
            int? destination=course is not null&&course.Selected>=0?course.GetSelectedMetadata().AsInt32():null;
            Navigate("OpenFleetCommand");
            var list=All<FleetList>(screen).Single();
            // Pre-fleet rules route Fleets straight to the ship page; there is no roster to pick from.
            if(!list.IsVisibleInTree()&&host!.IsVisibleInTree())goto shown;
            if(list.ItemCount==0)throw new InvalidOperationException("No owned vessel is available for inspection");
            int index=selected is {} id?Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemMetadata(i).AsInt32()==id):0;
            list.DeselectAll();list.Select(index);
            var inspector=All<Button>(screen).Single(b=>b.Name=="FleetInspectShip");
            if(inspector.Disabled||!inspector.IsVisibleInTree())throw new InvalidOperationException("Fleet inspector is unreachable");
            inspector.EmitSignal(BaseButton.SignalName.Pressed);
            // The game defers this refresh one frame; run it now so the ship page shows the reached controls.
            typeof(GameScreen).GetMethod("Refresh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance,Type.EmptyTypes)!.Invoke(screen,null);
            // Only for a course press: re-picking a destination moves the map and resets the planet choice.
            if(destination is {} star&&(originalName=="SetVesselCourse"||text=="Set course")&&All<OptionButton>(host!).SingleOrDefault(p=>p.Name=="DestinationPicker") is {} live)
                for(int i=0;i<live.ItemCount;i++)if(live.GetItemMetadata(i).AsInt32()==star){live.Select(i);live.EmitSignal(OptionButton.SignalName.ItemSelected,(long)i);break;}
            shown:;
        }
        if(workspace=="OpenContactsWorkspace") Navigate("OpenDiplomacy");
        if(workspace!="VesselInspector")Navigate(workspace);
        if(control.IsInsideTree() && control.IsVisibleInTree()) return control;
        // Workspace navigation may synchronously refresh a panel. Reacquire the
        // corresponding current control rather than dispatching to a detached one.
        var candidates=All<T>(host is not null&&GodotObject.IsInstanceValid(host)&&host.IsInsideTree()?host:screen).Where(c=>c.IsVisibleInTree() && c.GetType()==control.GetType());
        T? replacement = originalName.StartsWith("@",StringComparison.Ordinal)
            ? (candidates.Where(c=>(c is Button b ? b.Text : c is Label l ? l.Text : null)==text).ToArray() is var same && same.Length==1 ? same[0]
                // Tooltips change with enabled state (reason vs. hint), so only use them to tell same-text controls apart.
                : same.SingleOrDefault(c=>c.TooltipText==tooltip))
            : candidates.SingleOrDefault(c=>c.Name==originalName);
        return replacement ?? throw new InvalidOperationException($"Workspace did not reveal control: {originalName} \"{text}\" via {workspace} ({candidates.Count()} visible {typeof(T).Name} candidates: {string.Join(", ",candidates.Select(c=>c.Name+"="+((c as Button)?.Text??"")))}; host {(host is not null && GodotObject.IsInstanceValid(host) ? HiddenAt(host) : "disposed")})");
    }
}
