using System;
using System.IO;
using Godot;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
namespace OpenAntares.Game.Components;
public partial class CrisisNotificationDialog:AcceptDialog
{
    private readonly OptionButton _critical=new(){Name="CrisisDisplayLevel"},_routine=new(){Name="CrisisRoutineLevel"};
    private readonly CheckBox _require=new(){Name="CrisisRequireAcknowledgement",Text="Require me to acknowledge urgent crisis facts before End Turn"};
    private readonly Label _status=DevelopmentUi.Text("","WarningLabel");private readonly ConfirmationDialog _confirm=new(){Title="Change saved crisis acknowledgement rule",OkButtonText="Apply saved rule"};
    private string _path="";private CrisisView? _view;private CrisisNotificationPreferences _preferences=CrisisNotificationPreferences.Defaults;
    public event Action<CrisisNotificationPreferences>? Applied;public event Action<Command>? CommandRequested;
    public override void _Ready()
    {
        Title="Crisis notifications";Name="CrisisNotificationDialog";OkButtonText="Close";var body=DevelopmentUi.DialogBody(this);
        body.AddChild(DevelopmentUi.Text("Local display settings change whether an urgent update opens this panel. Critical facts and an End Turn blocker remain visible in every mode. Routine cure and inspection notes can be quieted."));
        body.AddChild(_critical);foreach(var level in Enum.GetValues<CrisisDisplayLevel>())_critical.AddItem(level.ToString());
        body.AddChild(_routine);foreach(var level in Enum.GetValues<NotificationLevel>())_routine.AddItem(level.ToString());
        DevelopmentUi.Button(body,"Apply local display",ApplyLocal,"CrisisApplyDisplay");body.AddChild(_status);
        body.AddChild(new HSeparator());body.AddChild(DevelopmentUi.Text("Saved campaign rule. Turning this off explicitly waives current and future urgent revisions; it never records them as read. Turning it back on renews current material revisions."));body.AddChild(_require);
        DevelopmentUi.Button(body,"Review saved acknowledgement rule",()=>{if(_view is null)return;bool value=_require.ButtonPressed;if(value==_view.RequireAcknowledgement){_status.Text="The saved rule already has this value.";return;}_confirm.DialogText=value?"Require acknowledgement again. Current urgent revisions, including unresolved losses, will block your next End Turn until explicitly acknowledged.":"Continue without requiring acknowledgement. Current and future urgent revisions will be marked waived, not read. Critical facts remain visible.";_confirm.PopupCenteredClamped(new(570,310));},"CrisisReviewAcknowledgement");
        AddChild(_confirm);_confirm.Confirmed+=()=>{if(_view is {} v){CommandRequested?.Invoke(new SetCrisisAcknowledgementCommand(v.Viewer,v.Turn,_require.ButtonPressed));Hide();}};
    }
    public void Open(string path,CrisisView view){_path=ProjectSettings.GlobalizePath(path);_view=view;_preferences=CrisisNotificationPreferences.Load(_path);_critical.Select((int)_preferences.Critical);_routine.Select((int)_preferences.Routine);_require.SetPressedNoSignal(view.RequireAcknowledgement);_status.Text=string.Join("\n",_preferences.Diagnostics);PopupCenteredClamped(new(660,540));}
    private void ApplyLocal(){try{var candidate=new CrisisNotificationPreferences((CrisisDisplayLevel)_critical.Selected,(NotificationLevel)_routine.Selected,System.Collections.Immutable.ImmutableArray<string>.Empty);candidate.Save(_path);_preferences=candidate;Applied?.Invoke(candidate);_status.Text="Local display updated. Saved crisis acknowledgement is unchanged.";}catch(Exception e)when(e is IOException or UnauthorizedAccessException or ArgumentException){_status.Text="Crisis preferences were not saved: "+e.Message;}}
}
