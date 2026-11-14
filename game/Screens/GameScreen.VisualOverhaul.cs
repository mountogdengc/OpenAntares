using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Screens;

public partial class GameScreen
{
    private readonly EmpireStatusStrip _statusStrip = new();
    private readonly ColonyLedger _ledger = new() { Name = "ColonyLedger" };
    private readonly VBoxContainer _systemCard = new() { Name = "SystemCard" };
    private readonly HFlowContainer _workspaceTabs = new() { Name = "WorkspaceTabs" };
    private readonly WorkspaceFrame _windowFrame = new();
    private readonly Dictionary<Window, string> _visualWindows = new();
    private Window? _visualWindow;
    private bool _changingVisualWorkspace;
    private PlanetStyleCatalog? _contextPlanetStyles;
    private ShaderMaterial? _screenMaterial;
    private bool _screenEffects = true, _reducedEffects, _planetAnimation = true;
    public string PresentationPreferencesPath { get; set; } = "user://presentation.cfg";

    private void WireVisualWorkspaces()
    {
        AddNavigation(_workspaceTabs, Rules.HasFleets(View.RulesVersion) ? "Fleets" : "Ships", "FleetWorkspaceTab", () => { if (Rules.HasFleets(View.RulesVersion)) OpenFleetCommand(); else ShowWorkspace("Vessels"); });
        AddNavigation(_workspaceTabs, "Diplomacy", "DiplomacyWorkspaceTab", OpenDiplomacyWorkspace);
        foreach (var entry in new (Window Window, string Key)[] {
            (_fleetCommand,"Fleets"), (_scoutOverview,"Scouts"), (_shipDesigns,"Designs"),
            (_diplomacy,"Diplomacy"), (_officerDetails,"Officers"), (_assemblyRequest,"Assembly"),
            (_management,"Colony plans"), (_crisisOverview,"Colony crises") })
        {
            var window = entry.Window;
            _visualWindows.Add(window, entry.Key);
            window.ForceNative = false;
            window.CloseRequested += () => window.Hide();
            window.WindowInput += input =>
            {
                if (window == _visualWindow && FindChild("MainNavigation", true, false) is CommandBar bar && bar.TryShortcut(input)) window.SetInputAsHandled();
            };
            window.VisibilityChanged += () =>
            {
                if (_changingVisualWorkspace) return;
                if (window.Visible) OpenVisualWorkspace(window, entry.Key);
                else if (ReferenceEquals(_visualWindow, window))
                {
                    _visualWindow = null;
                    _windowFrame.Close();
                    // Existing report/overview return handlers get first opportunity to restore their context.
                    Callable.From(() => { if (IsInsideTree() && _visualWindow is null && _workspace == entry.Key) ReturnFromWorkspace(); }).CallDeferred();
                }
            };
        }
        UpdateVisualTabs(_workspace);
    }
    private void OpenVisualWorkspace(Window window, string key)
    {
        DismissReportForNavigation();
        _changingVisualWorkspace = true;
        try
        {
            if (_visualWindow is { } old && old != window)
            {
                if (old == _report) _reportOrigin = null;
                old.Hide();
            }
            _visualWindow = window;
            RecordWorkspaceChange(key);
            _workspace = key;
            UpdateGlobePresentation();
            _map.Hide(); _mainArea.GetNode<Control>("MapOverlays").Hide(); _workspaceScroll.Hide(); _consolePanel.Hide();
            _workspaceHeader.Show(); _workspaceTitle.Text = key;
            _windowFrame.Show();
            UpdateVisualTabs(key);
            _windowFrame.Open(window);
        }
        finally { _changingVisualWorkspace = false; }
    }
    private void CloseVisualWorkspace()
    {
        if (_visualWindow is null) return;
        _changingVisualWorkspace = true;
        try { if (_visualWindow == _report) _reportOrigin = null; _visualWindow.Hide(); _visualWindow = null; _windowFrame.Close(); }
        finally { _changingVisualWorkspace = false; }
    }
    private bool RestoreVisualWorkspace(string key)
    {
        switch (key)
        {
            case "Fleets": OpenFleetCommand(); return true;
            case "Scouts" when View.Scouting is { } scout: _scoutOverview.ShowScouting(_session.Content, scout); return true;
            case "Designs": _shipDesigns.ShowCatalogue(_session.Content, View); return true;
            case "Diplomacy": OpenDiplomacyWorkspace(); return true;
            default: return false;
        }
    }
    private void ApplyTouchTargets()
    {
        bool compact = Size.X < 1100;
        int size = GetThemeConstant(compact ? "compact_target" : "touch_target", "ConsoleMetrics");
        foreach (var button in PresentationNodes<Button>(this))
        {
            int target = button is WorkerFigure ? GetThemeConstant(compact ? "compact_worker_size" : "worker_size", "ConsoleMetrics") : size;
            button.CustomMinimumSize = new Vector2(button is WorkerFigure ? target : Math.Max(button.CustomMinimumSize.X, target), button.ThemeTypeVariation == "SystemPlanetKey" ? Math.Max(button.CustomMinimumSize.Y, target) : target);
        }
    }
    private void UpdateVisualTabs(string workspace)
    {
        bool fleet = workspace is "Fleets" or "Scouts" || workspace == "Vessels" && Rules.HasScouting(View.RulesVersion);
        bool diplomacy = workspace is "Diplomacy" or "Contacts";
        _workspaceTabs.Visible = fleet || diplomacy;
        foreach (var child in _workspaceTabs.GetChildren().OfType<Button>())
        {
            child.ThemeTypeVariation = "TabKey";
            child.Visible = child.Name.ToString() switch {
                "OpenScoutOverview" or "FleetWorkspaceTab" => fleet,
                "OpenContactsWorkspace" => diplomacy,
                "DiplomacyWorkspaceTab" => diplomacy && Rules.HasDiplomacy(View.RulesVersion),
                _ => false };
        }
    }
    private void RefreshVisualConsole(EmpireView view)
    {
        var pending = TurnGuidance.Build(view);
        int required = pending.Count(i => i.Severity == GuidanceSeverity.Required);
        _alertsEntry.Text = pending.Count > 0 ? "Decisions " + pending.Count : "Alerts";
        if (FindChild("MainNavigation", true, false) is CommandBar bar) bar.SetDecisionCount(pending.Count);
        _endTurn.TooltipText = string.Join("\n", pending.Select(i => i.Text));
        if (!view.Research.Ready) _endTurn.Text = required > 0 ? $"End Turn\n{required} required" : "End Turn\n" + (pending.Count > 0 ? $"{pending.Count} to consider" : "Check plans");
    }
    private void OpenDiplomacyWorkspace()
    {
        if (Rules.HasDiplomacy(View.RulesVersion)) _diplomacy.ShowEmpire(_session.Content, View);
        else if (Rules.HasRivals(View.RulesVersion)) ShowWorkspace("Contacts");
    }
    private void ConfigureEffects(MenuButton menu)
    {
        var settings = new ConfigFile();
        if (settings.Load(PresentationPreferencesPath) == Error.Ok)
        {
            _screenEffects = settings.GetValue("display", "screen_effects", true).AsBool();
            _reducedEffects = settings.GetValue("display", "reduced_effects", false).AsBool();
            _planetAnimation = settings.GetValue("display", "planet_animation", true).AsBool();
        }
        _screenMaterial = new ShaderMaterial { Shader = GD.Load<Shader>("res://game/theme/console_screen.gdshader") };
        _screenMaterial.SetShaderParameter("tint", GetThemeColor("ink", "ConsolePalette"));
        _screenMaterial.SetShaderParameter("strength", GetThemeConstant("screen_effect_strength", "ConsoleMetrics") / 1000.0f);
        var glass = new ColorRect { Name = "ConsoleGlass", MouseFilter = MouseFilterEnum.Ignore, Material = _screenMaterial };
        _map.AddChild(glass); glass.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ApplyEffects(menu);
    }
    private void ApplyEffects(MenuButton menu)
    {
        menu.GetPopup().SetItemChecked(menu.GetPopup().GetItemIndex(3), _screenEffects);
        menu.GetPopup().SetItemChecked(menu.GetPopup().GetItemIndex(4), _reducedEffects);
        _screenMaterial?.SetShaderParameter("enabled", _screenEffects && !_reducedEffects);
        menu.GetPopup().SetItemChecked(menu.GetPopup().GetItemIndex(6), _planetAnimation);
        _planetClock?.SetStatic(_reducedEffects || !_planetAnimation);
        UpdateGlobePresentation();
    }
    private void ToggleEffects(MenuButton menu, long id)
    {
        if (id == 3) _screenEffects = !_screenEffects;
        if (id == 4) _reducedEffects = !_reducedEffects;
        if (id == 6) _planetAnimation = !_planetAnimation;
        ApplyEffects(menu);
        var settings = new ConfigFile(); settings.Load(PresentationPreferencesPath);
        settings.SetValue("display", "screen_effects", _screenEffects); settings.SetValue("display", "reduced_effects", _reducedEffects);
        settings.SetValue("display", "planet_animation", _planetAnimation);
        if (settings.Save(PresentationPreferencesPath) != Error.Ok) ShowMessage("Display preference could not be saved. It remains active for this session.", true);
    }
}
