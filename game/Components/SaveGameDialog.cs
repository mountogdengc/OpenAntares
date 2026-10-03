using System;
using System.IO;
using Godot;
using OpenAntares.Simulation;

namespace OpenAntares.Game.Components;

/// <summary>Name a save (or pick an existing one to overwrite) and write the session to it.</summary>
public partial class SaveGameDialog : ConfirmationDialog
{
    private readonly LineEdit _name = new() { PlaceholderText = "Save name" };
    private readonly ItemList _existing = new() { CustomMinimumSize = new Vector2(420, 180) };
    private readonly Label _error = new() { ThemeTypeVariation = "WarningLabel", Visible = false };
    private GameSession? _session;

    /// <summary>Raised after a successful save, with the save's name.</summary>
    public event Action<string>? Saved;

    public override void _Ready()
    {
        Title = "Save Game";
        OkButtonText = "Save";
        DialogHideOnOk = false;

        var padding = new MarginContainer { ThemeTypeVariation = "PanelPadding" };
        AddChild(padding);
        var column = new VBoxContainer();
        padding.AddChild(column);
        column.AddChild(_name);
        column.AddChild(new Label { Text = "Existing saves", ThemeTypeVariation = "SubtleLabel" });
        column.AddChild(_existing);
        column.AddChild(_error);

        _name.TextChanged += _ => UpdateButton();
        _name.TextSubmitted += _ => SaveNow();
        _existing.ItemSelected += index => { _name.Text = _existing.GetItemText((int)index); UpdateButton(); };
        Confirmed += SaveNow;
    }

    public void Open(GameSession session, string suggestedName)
    {
        _session = session;
        _name.Text = suggestedName;
        _error.Visible = false;
        _existing.Clear();
        foreach (SaveFileInfo save in SaveFiles.List())
        {
            _existing.AddItem(save.Name);
        }

        UpdateButton();
        PopupCentered();
        _name.GrabFocus();
        _name.SelectAll();
    }

    private void UpdateButton() =>
        GetOkButton().Text = SaveFiles.Exists(_name.Text) ? "Overwrite" : "Save";

    private void SaveNow()
    {
        if (_session is null)
        {
            return;
        }

        string name = SaveFiles.Sanitize(_name.Text);
        try
        {
            _session.Save(SaveFiles.PathFor(name));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _error.Text = "The game could not be saved: " + exception.Message;
            _error.Visible = true;
            return;
        }

        Hide();
        Saved?.Invoke(name);
    }
}
