using Godot;

namespace OpenAntares.Game;

/// <summary>
/// Root of the entry scene. Builds the interface in code; styling comes from the project theme.
/// </summary>
public partial class Main : Control
{
    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var title = new Label
        {
            Text = "OpenAntares",
            ThemeTypeVariation = "TitleLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        center.AddChild(title);
    }
}
