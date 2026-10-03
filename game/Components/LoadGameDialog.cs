using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using OpenAntares.Simulation.Saves;

namespace OpenAntares.Game.Components;

/// <summary>
/// Pick a save and load it. A save that can't be loaded shows its errors here and nothing else
/// changes; only a successful load is passed on.
/// </summary>
public partial class LoadGameDialog : ConfirmationDialog
{
    private readonly ItemList _saves = new() { CustomMinimumSize = new Vector2(460, 220) };
    private readonly Label _error = new()
    {
        ThemeTypeVariation = "WarningLabel",
        Visible = false,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        CustomMinimumSize = new Vector2(460, 0),
    };

    private IReadOnlyList<SaveFileInfo> _files = Array.Empty<SaveFileInfo>();

    /// <summary>Raised with the loaded game when a save loads successfully.</summary>
    public event Action<LoadedGame>? Loaded;

    public override void _Ready()
    {
        Title = "Load Game";
        OkButtonText = "Load";
        DialogHideOnOk = false;

        var padding = new MarginContainer { ThemeTypeVariation = "PanelPadding" };
        AddChild(padding);
        var column = new VBoxContainer();
        padding.AddChild(column);
        column.AddChild(_saves);
        column.AddChild(_error);

        _saves.ItemSelected += _ => GetOkButton().Disabled = false;
        _saves.ItemActivated += _ => LoadSelected();
        Confirmed += LoadSelected;
    }

    public void Open()
    {
        _files = SaveFiles.List();
        _saves.Clear();
        foreach (SaveFileInfo file in _files)
        {
            _saves.AddItem($"{file.Name}    {file.Modified.ToString("g", CultureInfo.CurrentCulture)}");
        }

        _error.Text = _files.Count == 0 ? "There are no saved games yet." : string.Empty;
        _error.Visible = _files.Count == 0;
        GetOkButton().Disabled = true;
        PopupCentered();
    }

    private void LoadSelected()
    {
        int[] selected = _saves.GetSelectedItems();
        if (selected.Length == 0)
        {
            return;
        }

        SaveLoadResult result = SaveGame.ReadFile(_files[selected[0]].Path);
        if (result.Game is null)
        {
            _error.Text = "This save can't be loaded:\n" + string.Join("\n", result.Errors);
            _error.Visible = true;
            return;
        }

        Hide();
        Loaded?.Invoke(result.Game);
    }
}
