using System;
using Godot;

namespace OpenAntares.Game.Screens;

public partial class MainMenuScreen : CenterContainer
{
    public event Action? ResumeRequested;
    public event Action? NewGameRequested;
    public event Action? LoadRequested;
    public override void _Ready()
    {
        Name = "MainMenu";
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var frame = new PanelContainer { ThemeTypeVariation = "WorkspaceFrame" }; AddChild(frame);
        var padding = new MarginContainer { ThemeTypeVariation = "PanelPadding" }; frame.AddChild(padding);
        var column = new VBoxContainer(); padding.AddChild(column);
        column.AddChild(new Label { Text = "OpenAntares", ThemeTypeVariation = "TitleLabel" });
        column.AddChild(new Label { Text = "Main menu", ThemeTypeVariation = "HeaderLabel" });
        void Key(string text, string name, Action action)
        {
            var button = new Button { Text = text, Name = name, ThemeTypeVariation = "CommandKey" };
            button.Pressed += action; column.AddChild(button);
        }
        Key("Resume campaign", "ResumeCampaign", () => ResumeRequested?.Invoke());
        Key("New game", "MainMenuNewGame", () => NewGameRequested?.Invoke());
        Key("Load game", "MainMenuLoadGame", () => LoadRequested?.Invoke());
    }
}
