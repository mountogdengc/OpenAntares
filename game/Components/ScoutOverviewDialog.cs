using System;
using System.Linq;
using Godot;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Scouting;
namespace OpenAntares.Game.Components;
public partial class ScoutOverviewDialog:AcceptDialog
{
    private VBoxContainer _body=null!;private ScoutView? _view;private VesselId? _selected;private Command? _pending;
    private readonly ConfirmationDialog _confirm=new(){Name="ScoutConfirmation",Title="Review ameba response",OkButtonText="Queue this response"};
    public string PreferencesPath {get;set;}="user://scout-notifications.json";
    public event Action<Command>? CommandIssued;public event Action<StarId>? StarRequested;public event Action<ScoutNotificationPreferences>? DisplayApplied;
    public override void _Ready()
    {
        Name="ScoutOverview";Title="Scouts and space ameba";OkButtonText="Close";_body=DevelopmentUi.DialogBody(this);AddChild(_confirm);
        _confirm.Confirmed+=()=>{var command=_pending;_pending=null;if(command is not null)CommandIssued?.Invoke(command);};
    }
    public void ShowScouting(ContentSet content,ScoutView view)
    {
        _view=view;DevelopmentUi.Clear(_body);
        _body.AddChild(DevelopmentUi.Text("Observe is the neutral default; Avoid encounters is opt-in. Scouts always try to withdraw from an ameba unless you choose one explicit risk response for this boundary."));
        _body.AddChild(DevelopmentUi.Text($"Turn {view.Turn}. Live encounters: {view.Attention.Length}. Review the exact current responses, then acknowledge this group before End Turn. Acknowledgement alone cannot replace a legal departure or risk response.",view.Attention.IsEmpty?"SubtleLabel":"WarningLabel"));
        var list=new ItemList{Name="ScoutVesselList",CustomMinimumSize=new(0,100)};_body.AddChild(list);
        if(!view.Vessels.Any(v=>v.Id==_selected))_selected=view.Vessels.FirstOrDefault()?.Id;
        foreach(var row in view.Vessels){list.AddItem($"{content.Vessels[row.RoleId].Name} {row.Id.Value}: {row.Stage?.ToString()??(row.Transit is not null?"Travelling":row.Posture?.ToString()??"Population expedition")}");if(row.Id==_selected)list.Select(list.ItemCount-1);}
        Callable.From(()=>{if(GodotObject.IsInstanceValid(list)&&list.IsInsideTree())list.EnsureCurrentIsVisible();}).CallDeferred();
        list.ItemSelected+=index=>{_selected=view.Vessels[(int)index].Id;ShowScouting(content,_view!);};
        var vessel=view.Vessels.FirstOrDefault(v=>v.Id==_selected);
        if(vessel is not null)
        {
            string location=vessel.Transit is {} t?$"Committed travel: {t.RemainingTurns} turn(s) remain; no redirection.":$"Stationed at {vessel.Station}.";
            _body.AddChild(DevelopmentUi.Text(location));
            if(vessel.Posture is {} posture)_body.AddChild(DevelopmentUi.Text($"Neutral posture: {posture}. Withdraw from ameba: enabled. Change neutral posture in the vessel inspector, including while travelling."));
            if(vessel.Stage is not null)
            {
                string odds=$"Vessel speed {vessel.Speed}; ameba pursuit {vessel.PursuitSpeed}. Study survival {vessel.StudyPercent}%; later escape {vessel.LateEscapePercent}%. Combined hull retention {(vessel.CombinedRetentionNumerator/100m):0.##}% ({vessel.CombinedRetentionNumerator}/10000). Study reward {Format.Points(vessel.ResearchReward)} research once per empire at this habitat, deposited after normal research and cure.";
                var detail=DevelopmentUi.Text((vessel.Stage==AmebaStage.Warning?$"Warning: depart during this planning turn {vessel.DepartureDeadline} for 100% survival. Ordinary travel time still applies. ":"Exposed: departure now risks the displayed late-escape roll. ")+odds,"WarningLabel");detail.Name="ScoutDetails";_body.AddChild(detail);
                _body.AddChild(DevelopmentUi.Text($"Encounter {vessel.EncounterNumber}, revision {vessel.Revision}; response {vessel.RiskAction?.ToString()??"departure"}; acknowledgement {(vessel.Acknowledged?"current":"required")}."));
                if(vessel.Blocker is {} blocker)_body.AddChild(DevelopmentUi.Text(blocker,"WarningLabel"));
                var study=vessel.Actions.OfType<SetAmebaRiskOrderCommand>().FirstOrDefault(c=>c.Action==AmebaRiskAction.Study);
                var studyButton=DevelopmentUi.Button(_body,"Study ameba…",()=>Confirm(study,$"{vessel.StudyPercent}% immediate survival, {100-vessel.StudyPercent}% loss of this scout. Success earns {Format.Points(vessel.ResearchReward)} research and publishes Exposed; later escape is {vessel.LateEscapePercent}%. Combined hull retention {vessel.CombinedRetentionNumerator}/10000. No cure or technology completes from this reward during the study boundary."),"ScoutStudy");studyButton.Disabled=study is null;
                var remain=vessel.Actions.OfType<SetAmebaRiskOrderCommand>().FirstOrDefault(c=>c.Action==AmebaRiskAction.Remain);
                var remainButton=DevelopmentUi.Button(_body,"Remain at risk…",()=>Confirm(remain,$"100% loss at the next boundary: this vessel and its cargo ({vessel.Cargo}) will be consumed. There is no refund, free replacement or research reward. This explicit response lasts one boundary."),"ScoutRemain");remainButton.Disabled=remain is null;
                Command? departure=vessel.Posture is not null&&vessel.EffectiveDestination is {} destination?new SetScoutRetreatOrderCommand(view.Owner,view.Turn,vessel.Id,destination):null;
                var withdraw=DevelopmentUi.Button(_body,"Queue departure",()=>{if(departure is null)return;if(vessel.Stage==AmebaStage.Exposed)Confirm(departure,$"Late escape: {vessel.LateEscapePercent}% survival; {100-vessel.LateEscapePercent}% vessel loss. Earned research stays with your empire. Travel uses the ordinary route to {vessel.EffectiveDestination}.");else CommandIssued?.Invoke(departure);},"ScoutWithdraw");withdraw.Disabled=departure is null;withdraw.TooltipText=departure is null?"Choose a legal manual destination in the vessel inspector, or an explicit risk response.":"Queue the current projected ordinary retreat; acknowledge the current group before End Turn.";
                var clear=DevelopmentUi.Button(_body,"Clear risk response",()=>CommandIssued?.Invoke(new SetAmebaRiskOrderCommand(view.Owner,view.Turn,vessel.Id,vessel.EncounterNumber!.Value,vessel.Revision!.Value,null)),"ScoutClearRisk");clear.Disabled=vessel.RiskAction is null;
            }
            else if(vessel.Blocker is {} wait)_body.AddChild(DevelopmentUi.Text(wait,"WarningLabel"));
        }
        else _body.AddChild(DevelopmentUi.Text("No live vessels. Prior outcomes remain in the history below; build a replacement through ordinary colony production."));
        var ack=DevelopmentUi.Button(_body,"Acknowledge all current responses",()=>CommandIssued?.Invoke(new AcknowledgeAmebaCommand(view.Owner,view.Turn,view.Attention)),"ScoutAcknowledge");ack.Disabled=view.Attention.IsEmpty;
        _body.AddChild(new HSeparator());_body.AddChild(DevelopmentUi.Text("Local Emergency display. Every level keeps the badge, exact End Turn gate and history; there is no risk waiver."));
        var level=new OptionButton{Name="ScoutDisplayLevel"};foreach(var mode in Enum.GetValues<ScoutDisplayLevel>())level.AddItem(mode.ToString());level.Select((int)ScoutNotificationPreferences.Load(ProjectSettings.GlobalizePath(PreferencesPath)).Level);_body.AddChild(level);
        DevelopmentUi.Button(_body,"Apply local display",()=>{try{var preferences=new ScoutNotificationPreferences((ScoutDisplayLevel)level.Selected);preferences.Save(ProjectSettings.GlobalizePath(PreferencesPath));DisplayApplied?.Invoke(preferences);}catch(Exception error)when(error is System.IO.IOException or UnauthorizedAccessException or ArgumentException){_body.AddChild(DevelopmentUi.Text("Display setting was not saved: "+error.Message,"WarningLabel"));}},"ScoutApplyDisplay");
        _body.AddChild(new HSeparator());_body.AddChild(DevelopmentUi.Text("Own order and outcome history", "HeaderLabel"));
        foreach(var digest in view.History.Reverse())foreach(var entry in digest.Entries)
        {
            _body.AddChild(DevelopmentUi.Text($"Turn {entry.Turn}, {content.Vessels[entry.RoleId].Name} {entry.Vessel.Value}: {entry.Kind}. {entry.Reason}"+(entry.ResearchAward>0?$" Earned {Format.Points(entry.ResearchAward)} research.":""),entry.Kind=="loss"?"WarningLabel":"SubtleLabel"));
            var star=entry.Star;DevelopmentUi.Button(_body,"Show recorded system",()=>StarRequested?.Invoke(star));
        }
        PopupCenteredClamped(new(740,560));
    }
    private void Confirm(Command? command,string consequence){if(command is null)return;_pending=command;_confirm.DialogText=consequence;_confirm.PopupCenteredClamped(new(640,380));}
}
