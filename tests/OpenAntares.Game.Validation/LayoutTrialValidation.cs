using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;

namespace OpenAntares.Game.Validation;

/// <summary>Headless control/layout checks. No framebuffer, native-input or usability claim.</summary>
public partial class LayoutTrialValidation : Node
{
    private readonly List<object> _checks = new();
    private readonly List<string> _savePaths = new();
    public override async void _Ready()
    {
        string output = OS.GetCmdlineUserArgs().Single(a => a.StartsWith("--validation-output=", StringComparison.Ordinal))[20..];
        string? error = null;
        try
        {
            var content = ContentFiles.LoadCore().Content ?? throw new InvalidOperationException("Content failed to load");
            double initialOrphans = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);
            foreach (int rules in new[] { Rules.CurrentVersion, Rules.LegacyVersion })
            {
                var generated = GalaxyGenerator.CreateNewGame(content, new("small", 42, rules, SpeciesId: Rules.HasSpecies(rules) ? "harl" : null, CrisesEnabled: false, AmebasEnabled: Rules.HasScouting(rules) ? false : null, SentriesEnabled: Rules.HasCombat(rules) ? false : null));
                var session = new GameSession(content, generated.State ?? throw new InvalidOperationException(generated.Error));
                var screen = new GameScreen(session, session.State.Empires.First().Id)
                {
                    NotificationPreferencesPath = Path.Combine(output, $"colony-preferences-{rules}.json"),
                    CrisisPreferencesPath = Path.Combine(output, $"crisis-preferences-{rules}.json"),
                    ScoutPreferencesPath = Path.Combine(output, $"scout-preferences-{rules}.json"),
                    FleetPreferencesPath = Path.Combine(output, $"fleet-preferences-{rules}.json"),
                    CombatPreferencesPath = Path.Combine(output, $"combat-preferences-{rules}.json"),
                };
                AddChild(screen);
                await Frames();
                string initial = SaveGame.Serialize(content, session.State);
                foreach (var size in new[] { new Vector2I(800, 600), new Vector2I(1152, 648), new Vector2I(1600, 900), new Vector2I(1920, 1080) })
                {
                    GetTree().Root.Size = size;
                    await Frames();
                    Check(screen.Size.DistanceTo(new Vector2(size.X, size.Y)) <= 2, $"rules{rules}/{size}: production viewport uses physical window coordinates");
                    var navigation = Named<Control>(screen, "MainNavigation");
                    var console = Named<Control>(screen, "EmpireConsole");
                    var map = All<GalaxyMap>(screen).Single();
                    Check(map.IsVisibleInTree(), $"rules{rules}/{size}: map is startup workspace");
                    Check(map.Size.X > screen.Size.X / 2 && map.Size.Y > screen.Size.Y / 2, $"rules{rules}/{size}: galaxy dominates screen");
                    Check(navigation.IsVisibleInTree() && Within(navigation, screen), $"rules{rules}/{size}: bottom navigation fits");
                    Check(navigation.GetChildren().OfType<Button>().Where(b => b.IsVisibleInTree()).All(b => Within(b, screen)), $"rules{rules}/{size}: every navigation action fits");
                    Check(console.IsVisibleInTree() && Within(console, screen), $"rules{rules}/{size}: right console fits");
                    Press(Named<Button>(screen, "OpenColonyWorkspace"));
                    await Frames();
                    Check(All<ColonyPanel>(screen).Single().IsVisibleInTree() && !map.IsVisibleInTree(), $"rules{rules}/{size}: colony workspace replaces map");
                    Check(Within(Named<Button>(screen, "ReturnToGalaxy"), screen), $"rules{rules}/{size}: return remains reachable");
                    Press(Named<Button>(screen, "ReturnToGalaxy"));
                    await Frames();
                    Check(map.IsVisibleInTree(), $"rules{rules}/{size}: return restores map");
                    Check(SaveGame.Serialize(content, session.State) == initial, $"rules{rules}/{size}: navigation and resize preserve exact campaign");
                }
                Press(Named<Button>(screen, "InspectSelectedSystem"));
                await Frames();
                Check(All<StarPanel>(screen).Single().IsVisibleInTree(), $"rules{rules}: system inspector is reachable");
                Press(Named<Button>(screen, "OpenResearch"));
                await Frames();
                var researchPicker = All<OptionButton>(All<ResearchBar>(screen).Single()).Single();
                Check(researchPicker.IsVisibleInTree() && researchPicker.GetPopup().Visible, $"rules{rules}: Research reveals console and opens picker");
                researchPicker.GetPopup().Hide();
                Press(Named<Button>(screen, "OpenColonyWorkspace"));
                await Frames();
                var colony = All<ColonyPanel>(screen).Single();
                var tools = Named<Button>(screen, "OpenColonyTools");
                Check(tools.IsVisibleInTree() == Rules.HasExpansion(rules), $"rules{rules}: unsupported colony tools are hidden");
                if (Rules.HasRivals(rules))
                {
                    Press(Named<Button>(screen, "OpenContactsWorkspace"));
                    await Frames();
                    Check(All<ContactsPanel>(screen).Single().IsVisibleInTree(), "Current campaigns retain Contacts alongside Diplomacy");
                    Press(Named<Button>(screen, "OpenColonyWorkspace"));
                    await Frames();
                }
                GetTree().Root.Size = new(800, 600);
                await Frames();
                var staleStep = All<Button>(colony).Single(b => b.TooltipText == "Move a worker from production to support");
                Press(Named<Button>(screen, "EndTurn"));
                var afterTurn = SaveGame.Serialize(content, session.State);
                // A previously laid-out order delivered before deferred refresh is stale.
                // It must reject visibly without publishing another state change.
                Press(staleStep);
                All<TurnReportDialog>(screen).Single().Hide();
                await Frames();
                var feedback = Named<Label>(screen, "CommandFeedback");
                Check(feedback.IsVisibleInTree() && feedback.Text.Length > 0, $"rules{rules}: rejected order remains visible on narrow colony page");
                Check(SaveGame.Serialize(content, session.State) == afterTurn, $"rules{rules}: stale visible order leaves campaign unchanged");
                int production = session.State.Colonies.Single().Workforce.Production;
                Press(All<Button>(colony).Single(b => b.TooltipText == "Move a worker from production to support"));
                await Frames();
                Check(session.State.Colonies.Single().Workforce.Production == production - 1, $"rules{rules}: workforce action still uses command handler");
                Press(All<Button>(colony).Single(b => b.TooltipText == "Move a worker from support to production"));
                await Frames();
                Select(Named<OptionButton>(colony, "ColonyProjectPicker"), "fabrication_hall");
                Select(All<OptionButton>(All<ResearchBar>(screen).Single()).Single(), "cultivation_methods");
                await Frames();
                Check(session.State.Colonies.Single().ProjectId == "fabrication_hall", $"rules{rules}: construction picker commits target");
                Press(Named<Button>(screen, "OpenGalaxyWorkspace"));
                await Frames();
                bool followUp = false;
                for (int turn = 0; turn < 30 && !followUp; turn++)
                {
                    Press(All<Button>(screen).Single(b => b.Text == "End Turn"));
                    await Frames();
                    var report = All<TurnReportDialog>(screen).Single();
                    var choose = All<Button>(report).FirstOrDefault(b => b.Text == "Choose next project");
                    if (choose is not null)
                    {
                        Press(choose);
                        await Frames(12);
                        var picker = Named<OptionButton>(colony, "ColonyProjectPicker");
                        Check(colony.IsVisibleInTree() && picker.GetPopup().Visible, $"rules{rules}: report reveals colony and opens next project picker");
                        picker.GetPopup().Hide();
                        followUp = true;
                    }
                    report.Hide();
                }
                Check(followUp, $"rules{rules}: construction completion follow-up exercised");
                var saved = SaveGame.Serialize(content, session.State);
                var read = SaveGame.Deserialize(saved);
                Check(read.Game is not null && SaveGame.Serialize(read.Game.Content, read.Game.State) == saved, $"rules{rules}: trial leaves save round trip intact");
                RemoveChild(screen);
                screen.QueueFree();
                await Frames();
            }
            await CheckGameMenuSaveLoad();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Frames();
            Check(Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount) <= initialOrphans, "Closing current, legacy and Main screens leaves no orphan controls");
        }
        catch (Exception exception) { error = exception.ToString(); GD.PrintErr(error); }
        finally
        {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(new { passed = error is null, error, method = "Headless Godot layout and in-process real control signals; no rendering, physical-input or usability acceptance", checks = _checks, retainedQaSavePaths = _savePaths }, new JsonSerializerOptions { WriteIndented = true }));
            GetTree().Quit(error is null ? 0 : 1);
        }
    }
    private async Task CheckGameMenuSaveLoad()
    {
        var main = new Main();
        AddChild(main);
        await Frames();
        var setup = All<NewGameScreen>(main).Single();
        setup.Configure("small", 42, speciesId: "harl", rivalCount: 0, crisesEnabled: false, amebasEnabled: false, sentriesEnabled: false);
        setup.Start();
        await Frames();
        var screen = All<GameScreen>(main).Single();
        var session = (GameSession)typeof(GameScreen).GetField("_session", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(screen)!;
        string before = SaveGame.Serialize(session.Content, session.State);
        string saveName = "qa-ui-layout-" + Guid.NewGuid().ToString("N");
        string path = SaveFiles.PathFor(saveName);
        Check(!File.Exists(path), "Game-menu QA save is unique");
        Named<MenuButton>(screen, "GameMenu").GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed, 0);
        await Frames();
        var save = All<SaveGameDialog>(screen).Single();
        Check(save.Visible, "Game menu opens real Save dialog");
        All<LineEdit>(save).Single().Text = saveName;
        Press(save.GetOkButton());
        await Frames();
        Check(!save.Visible && File.Exists(path), "Game-menu Save writes campaign and closes");
        _savePaths.Add(path);
        Named<MenuButton>(screen, "GameMenu").GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed, 1);
        await Frames();
        var load = All<LoadGameDialog>(main).Single();
        Check(load.Visible, "Game menu opens real Load dialog");
        var list = All<ItemList>(load).Single();
        int index = Enumerable.Range(0, list.ItemCount).Single(i => list.GetItemText(i).StartsWith(saveName + "    ", StringComparison.Ordinal));
        list.Select(index);
        list.EmitSignal(ItemList.SignalName.ItemSelected, index);
        Press(load.GetOkButton());
        await Frames();
        var restored = All<GameScreen>(main).Single();
        var restoredSession = (GameSession)typeof(GameScreen).GetField("_session", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(restored)!;
        Check(!ReferenceEquals(screen, restored) && SaveGame.Serialize(restoredSession.Content, restoredSession.State) == before, "Actual Main reload preserves campaign and restores trial shell");
        RemoveChild(main);
        main.QueueFree();
        await Frames();
    }
    private async Task Frames(int count = 6) { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private void Check(bool value, string name) { _checks.Add(new { name, passed = value }); if (!value) throw new InvalidOperationException(name); GD.Print("PASS: " + name); }
    private static bool Within(Control child, Control parent) { var c = child.GetGlobalRect(); var p = parent.GetGlobalRect().Grow(1); return p.Encloses(c); }
    private static IEnumerable<T> All<T>(Node node) where T : Node => node.GetChildren().SelectMany(c => (c is T match ? new[] { match } : Array.Empty<T>()).Concat(All<T>(c)));
    private static T Named<T>(Node node, string name) where T : Node => All<T>(node).SingleOrDefault(c => c.Name == name) ?? throw new InvalidOperationException("Missing trial control: " + name);
    private static void Press(Button button) { if (button.Disabled || !button.IsVisibleInTree()) throw new InvalidOperationException("Unreachable button: " + button.Name); button.EmitSignal(Button.SignalName.Pressed); }
    private static void Select(OptionButton picker, string id) { int index = Enumerable.Range(0, picker.ItemCount).Single(i => picker.GetItemMetadata(i).AsString() == id); if (picker.IsItemDisabled(index)) throw new InvalidOperationException("Disabled choice"); picker.Select(index); picker.EmitSignal(OptionButton.SignalName.ItemSelected, index); }
}
