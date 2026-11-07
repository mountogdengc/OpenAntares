using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Saves;

namespace OpenAntares.Game.Validation;

internal static class PlayabilityMenuChecks
{
    public static async Task Run(Node host, GameScreen screen, GameSession game, string output, Action<bool,string> check)
    {
        var owner = game.State.Empires.First(e => e.Controller == OpenAntares.Simulation.State.ControllerKind.Human).Id;
        var view = game.View(owner);
        string before = SaveGame.Serialize(game.Content,game.State);
        int issued = 0;
        check(Descendants<Button>(screen).Single(b=>b.Name=="OpenConflictOrders").TooltipText.Contains("revised force response",StringComparison.Ordinal),"Clipped conflict entry retains its full force-response explanation in the tooltip");
        var research = Descendants<ResearchBar>(screen).Single();
        var detail = Descendants<Label>(research).SingleOrDefault(n=>n.Name=="ResearchChoiceDetails");
        check(detail is not null, "ResearchAndProductionDetailsMatchExistingChoices: visible research choice explanation exists");
        check(detail!.Text.Contains("No target",StringComparison.Ordinal) && detail.Text.Contains("saved",StringComparison.OrdinalIgnoreCase), "No research target visibly explains banking without making it mandatory");
        var queue = new ConstructionQueueEditor(); host.AddChild(queue); queue.CommandIssued += _=>issued++;
        var fleet = Descendants<FleetCommandDialog>(screen).Single(); fleet.CommandIssued += _=>issued++;
        var officers = Descendants<OfficerDetailDialog>(screen).Single(); officers.CommandIssued += _=>issued++;
        var diplomacy = Descendants<DiplomacyDialog>(screen).Single(); diplomacy.CommandIssued += _=>issued++;
        var designs = Descendants<ShipDesignDialog>(screen).Single(); designs.ActionRequested += _=>issued++;
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1152,648),new Vector2I(1600,900),new Vector2I(1920,1080)})
        {
            host.GetTree().Root.Size=size; await Frames(host,4);
            queue.Open(game.Content,view,view.Colonies[0]); await Frames(host);
            var production = Descendants<Label>(queue).SingleOrDefault(n=>n.Name=="QueueChoiceDetails");
            check(production is not null && production.Text.Contains("Cost",StringComparison.Ordinal), $"{size}: selected queue choice has a visible authoritative cost explanation");
            var choices = Descendants<OptionButton>(queue).Single(n=>n.Name=="QueueTargetPicker");
            // Actual queue choice browsing must distinguish whole population capacity from fixed-point economy output.
            int habitat=Enumerable.Range(0,choices.ItemCount).Single(i=>choices.GetItemMetadata(i).AsString()=="building|habitat_extension");
            choices.Select(habitat);choices.EmitSignal(OptionButton.SignalName.ItemSelected,(long)habitat);await Frames(host);
            check(production!.Text.Contains("capacity +2",StringComparison.Ordinal)&&!production.Text.Contains("0.02",StringComparison.Ordinal),$"{size}: Habitat Extension queue details show two whole population capacity units");
            int fabrication=Enumerable.Range(0,choices.ItemCount).Single(i=>choices.GetItemMetadata(i).AsString()=="building|fabrication_hall");
            choices.Select(fabrication);choices.EmitSignal(OptionButton.SignalName.ItemSelected,(long)fabrication);
            check(production.Text.Contains("production +1",StringComparison.Ordinal),$"{size}: Fabrication Hall still formats100 stored production as one point");
            choices.Select(0);choices.EmitSignal(OptionButton.SignalName.ItemSelected,0L);
            var researchSupplement=new ResearchBar{Size=new(260,500)};host.AddChild(researchSupplement);
            foreach(string targetId in new[]{"habitat_methods","adaptive_habitats"})
            {
                researchSupplement.ShowEmpire(game.Content,view.Research with{TargetId=targetId});await Frames(host);
                string unlocks=Descendants<Label>(researchSupplement).Single(l=>l.Name=="ResearchChoiceDetails").Text;
                check(unlocks.Contains("capacity +2",StringComparison.Ordinal)&&!unlocks.Contains("0.02",StringComparison.Ordinal),$"{size}: labelled research projection {targetId} retains whole capacity units in its visible unlock description");
            }
            host.RemoveChild(researchSupplement);researchSupplement.QueueFree();await Frames(host);
            string firstChoice=choices.GetItemText(0);
            Descendants<Button>(queue).Single(b=>b.Name=="AddQueueEntry").EmitSignal(Button.SignalName.Pressed);
            if(choices.ItemCount>1){choices.Select(1);choices.EmitSignal(OptionButton.SignalName.ItemSelected,1L);}
            var entries=Descendants<ItemList>(queue).Single(n=>n.Name=="QueueEntries");
            entries.Select(0);entries.EmitSignal(ItemList.SignalName.ItemSelected,0L);
            check(production!.Text.Contains(firstChoice,StringComparison.Ordinal), $"{size}: inspecting a queued entry explains that exact entry rather than another picker choice");
            queue.EmitSignal(Window.SignalName.CloseRequested); queue.Hide();
            fleet.ShowEmpire(game.Content,view); await Frames(host); CheckDialog(fleet,size,check); fleet.Hide();
            officers.ShowOfficers(game.Content,view); await Frames(host); CheckDialog(officers,size,check); officers.Hide();
            diplomacy.ShowEmpire(game.Content,view); await Frames(host); CheckDialog(diplomacy,size,check);
            check(Descendants<Label>(diplomacy).Any(l=>l.Text.Contains("No other civilization",StringComparison.Ordinal)), $"{size}: no-contact diplomacy explains discovery without invented partners"); diplomacy.Hide();
            designs.ShowCatalogue(game.Content,view); await Frames(host); CheckDialog(designs,size,check); designs.Hide();
            var popup=Descendants<MenuButton>(screen).Single(n=>n.Name=="GameMenu").GetPopup();
            check(popup.ItemCount==3 && Enumerable.Range(0,3).All(i=>popup.GetItemId(i)==i), $"{size}: Game actions retain IDs 0/1/2 with no unsupported settings");
            check(issued==0 && before==SaveGame.Serialize(game.Content,game.State), $"{size}: opening, browsing and closing menus emits no command or campaign/PRNG change");
        }
        host.RemoveChild(queue); queue.QueueFree();
        var picker=Descendants<OptionButton>(research).Single();
        int target=Enumerable.Range(1,picker.ItemCount-1).First(i=>!picker.IsItemDisabled(i));
        string id=picker.GetItemMetadata(target).AsString();
        int researchCommands=0;research.CommandIssued+=_=>researchCommands++;
        picker.Select(target);picker.EmitSignal(OptionButton.SignalName.ItemSelected,(long)target);await Frames(host);
        check(researchCommands==1 && game.View(owner).Research.TargetId==id, "Explicit research quick selection submits its existing command exactly once");
        check(Descendants<Label>(research).Single(n=>n.Name=="ResearchChoiceDetails").Text.Contains(game.Content.Technologies[id].Name,StringComparison.Ordinal), "Visible research explanation follows the actual accepted target");
    }
    private static void CheckDialog(AcceptDialog dialog, Vector2I size, Action<bool,string> check)
    {
        check(dialog.Visible && dialog.Size.X<=size.X && dialog.Size.Y<=size.Y, $"{size}: {dialog.Title} fits the physical viewport");
        check(dialog.GetOkButton().Text=="Close", $"{size}: {dialog.Title} has an explicit Close action");
    }
    private static async Task Frames(Node host,int count=3){for(int i=0;i<count;i++)await host.ToSignal(host.GetTree(),SceneTree.SignalName.ProcessFrame);}
    private static IEnumerable<T> Descendants<T>(Node root) where T:Node
    {foreach(Node child in root.GetChildren()){if(child is T typed)yield return typed;foreach(var nested in Descendants<T>(child))yield return nested;}}
}
