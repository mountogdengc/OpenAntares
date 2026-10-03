using System;
using System.Globalization;
using System.Linq;
using Godot;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;

namespace OpenAntares.Game.Screens;

/// <summary>Choose a galaxy size and seed, then start a game.</summary>
public partial class NewGameScreen : CenterContainer
{
    private readonly GalaxySizeDefinition[] _sizes;
    private readonly OptionButton _sizePicker = new();
    private readonly LineEdit _seedField = new();
    private readonly Label _error = new() { ThemeTypeVariation = "WarningLabel", Visible = false };

    public NewGameScreen(ContentSet content)
    {
        _sizes = content.GalaxySizes.Values
            .OrderBy(size => size.StarCount)
            .ThenBy(size => size.Id, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>Raised when the player starts a game with valid settings.</summary>
    public event Action<NewGameSettings>? StartRequested;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var panel = new PanelContainer();
        AddChild(panel);

        var margin = new MarginContainer { ThemeTypeVariation = "PanelPadding" };
        panel.AddChild(margin);

        var column = new VBoxContainer();
        margin.AddChild(column);

        column.AddChild(new Label
        {
            Text = "OpenAntares",
            ThemeTypeVariation = "TitleLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        column.AddChild(new Label
        {
            Text = "New Game",
            ThemeTypeVariation = "HeaderLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        var form = new GridContainer { Columns = 2 };
        column.AddChild(form);

        form.AddChild(new Label { Text = "Galaxy size" });
        foreach (GalaxySizeDefinition size in _sizes)
        {
            _sizePicker.AddItem($"{size.Name} ({size.StarCount} stars)");
        }

        _sizePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        form.AddChild(_sizePicker);

        form.AddChild(new Label { Text = "Seed" });
        var seedRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _seedField.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _seedField.TooltipText = "The same seed and size always produce the same galaxy.";
        seedRow.AddChild(_seedField);
        var randomButton = new Button { Text = "Random" };
        randomButton.Pressed += RandomizeSeed;
        seedRow.AddChild(randomButton);
        form.AddChild(seedRow);

        column.AddChild(_error);

        var startButton = new Button { Text = "Start" };
        startButton.Pressed += Start;
        column.AddChild(startButton);

        RandomizeSeed();
        startButton.GrabFocus();
    }

    /// <summary>Sets the form's values; used by development launch options.</summary>
    public void Configure(string sizeId, ulong seed)
    {
        int index = Array.FindIndex(_sizes, size => size.Id == sizeId);
        if (index >= 0)
        {
            _sizePicker.Select(index);
        }

        _seedField.Text = seed.ToString(CultureInfo.InvariantCulture);
    }

    public void Start()
    {
        if (_sizes.Length == 0)
        {
            ShowError("No galaxy sizes are defined in the content files.");
            return;
        }

        if (!ulong.TryParse(_seedField.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed))
        {
            ShowError($"The seed must be a whole number from 0 to {ulong.MaxValue}.");
            return;
        }

        _error.Visible = false;
        StartRequested?.Invoke(new NewGameSettings(_sizes[_sizePicker.Selected].Id, seed));
    }

    public void ShowError(string message)
    {
        _error.Text = message;
        _error.Visible = true;
    }

    private void RandomizeSeed()
    {
        // Presentation may use any randomness; the simulation only ever sees the chosen seed.
        _seedField.Text = (GD.Randi() % 1_000_000).ToString(CultureInfo.InvariantCulture);
    }
}
