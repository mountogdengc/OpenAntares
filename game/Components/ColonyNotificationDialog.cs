using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using OpenAntares.Game.Presentation;
namespace OpenAntares.Game.Components;
public partial class ColonyNotificationDialog:ConfirmationDialog
{
    public event Action<ColonyNotificationPreferences>? Applied;
    private readonly Dictionary<string,OptionButton> _pickers=new(StringComparer.Ordinal);private readonly Label _message=DevelopmentUi.Text("","WarningLabel");private string _path="";private ColonyNotificationPreferences _preferences=ColonyNotificationPreferences.Defaults;
    public override void _Ready(){Title="Colony notification preferences";OkButtonText="Apply preferences";DialogHideOnOk=false;var body=DevelopmentUi.DialogBody(this);body.AddChild(DevelopmentUi.Text("Local display settings. Stored reports and unresolved dashboard conditions remain available. Current conditions do not block End Turn."));foreach(string category in ColonyNotificationPreferences.Defaults.Levels.Keys){body.AddChild(new Label{Text=category.Replace('_',' ')});var picker=new OptionButton{Name="Notify_"+category};foreach(string level in Enum.GetNames<NotificationLevel>())picker.AddItem(level);_pickers.Add(category,picker);body.AddChild(picker);}body.AddChild(_message);Confirmed+=Apply;}
    public void Open(string path){_path=ProjectSettings.GlobalizePath(path);_preferences=ColonyNotificationPreferences.Load(_path);foreach(var pair in _pickers)pair.Value.Select((int)_preferences.LevelFor(pair.Key));_message.Text=string.Join("\n",_preferences.Diagnostics);PopupCenteredClamped(new(620,500));}
    private void Apply(){try{var candidate=_preferences;foreach(var pair in _pickers)candidate=candidate.WithLevel(pair.Key,(NotificationLevel)pair.Value.Selected);candidate.Save(_path);_preferences=candidate;Applied?.Invoke(candidate);Hide();}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or ArgumentException){_message.Text="Preferences were not saved: "+ex.Message;}}
}
