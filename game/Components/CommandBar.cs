using System;
using System.Collections.Generic;
using Godot;

namespace OpenAntares.Game.Components;

/// <summary>Route keys only. It cannot issue simulation commands.</summary>
public partial class CommandBar : HFlowContainer
{
    private readonly Dictionary<Key, Button> _shortcuts = new();
    private readonly Dictionary<Button, string> _labels = new();
    private int _decisionCount;
    public void Configure()
    {
        Name = "MainNavigation";
        foreach (var child in GetChildren())
        {
            if (child is not Button button || button.Name == "EndTurn") continue;
            _labels[button] = button.Text;
            button.ThemeTypeVariation = button is MenuButton ? "IconKey" : "CommandKey";
            string name = button.Name.ToString();
            var mapping = name switch
            {
                "OpenColonyWorkspace" => (Key.C, "population"),
                "OpenFleetCommand" => (Key.F, "escort"),
                "OpenResearch" => (Key.R, "research"),
                "OpenShipDesigns" => (Key.D, "production"),
                "OpenDiplomacy" => (Key.P, "alert"),
                "OpenGalaxyWorkspace" => (Key.G, "map"),
                _ => (Key.None, name == "GameMenu" ? "gear" : "alert")
            };
            button.Icon = GetThemeIcon(mapping.Item2, "Icons");
            button.ExpandIcon = true;
            if (mapping.Item1 != Key.None)
            {
                _shortcuts[mapping.Item1] = button;
                button.TooltipText = button.Text + " (" + mapping.Item1 + ")";
            }
            button.ClipText = true;
        }
        Resized += Compact;
        Compact();
    }
    private void Compact()
    {
        bool compact = GetViewportRect().Size.X < 1100;
        foreach (var node in GetChildren())
            if (node is Button button && button.Name != "EndTurn")
            {
                button.ThemeTypeVariation = button is MenuButton ? "IconKey" : compact ? "CompactCommandKey" : "CommandKey";
                var shortcut = System.Linq.Enumerable.FirstOrDefault(_shortcuts, pair => pair.Value == button).Key;
                if (button.Name == "OpenAttentionConsole") button.Text = compact ? _decisionCount.ToString() : "Decisions " + _decisionCount;
                else if (button is MenuButton) button.Text = compact ? "" : "Game";
                else if (shortcut != Key.None) button.Text = compact ? shortcut.ToString() : _labels[button] + "  " + shortcut;
                else if (_labels.TryGetValue(button, out var label)) button.Text = compact ? "Back" : label;
            }
    }
    public void SetDecisionCount(int count) { _decisionCount = count; Compact(); }
    public bool TryShortcut(InputEvent input)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false } key || key.CtrlPressed || key.AltPressed || key.MetaPressed) return false;
        var focus = GetViewport().GuiGetFocusOwner();
        for (Node? node = focus; node is not null; node = node.GetParent())
            if (node is LineEdit or TextEdit or SpinBox or Popup) return false;
        // Confirmations and popups own their input. Ordinary workspaces may still navigate.
        foreach (Window window in GetViewport().GetEmbeddedSubwindows())
        {
            if (!window.Visible) continue;
            if (window is Popup || window.Exclusive) return false;
            for (Node? node = window.GuiGetFocusOwner(); node is not null; node = node.GetParent())
                if (node is LineEdit or TextEdit or SpinBox) return false;
        }
        Key code = key.PhysicalKeycode == Key.None ? key.Keycode : key.PhysicalKeycode;
        if (_shortcuts.TryGetValue(code, out var button) && button.IsVisibleInTree() && !button.Disabled)
        {
            button.EmitSignal(BaseButton.SignalName.Pressed);
            return true;
        }
        return false;
    }
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (TryShortcut(input)) GetViewport().SetInputAsHandled();
    }
}
