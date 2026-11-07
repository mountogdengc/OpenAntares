using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class ColonyList : VBoxContainer
{
    public event Action<ColonyId>? ColonySelected;
    public event Action? CrisisRequested;
    public event Action? CrisisNotificationsRequested;
    public event Action? ManageRequested;
    public event Action? NotificationsRequested;
    public event Action? LatestReportRequested;
    private readonly System.Collections.Generic.HashSet<int> _dismissedSuggestions = new();
    public void ShowColonies(EmpireView view, ColonyId? selected, OpenAntares.Simulation.Content.ContentSet? content = null, OpenAntares.Game.Presentation.ColonyNotificationPreferences? preferences = null, EmpireTurnReportView? latestReport = null)
    {
        foreach (Node child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        Visible = Rules.HasExpansion(view.RulesVersion);
        if (!Visible) return;
        AddChild(new Label { Text = "Your colonies", ThemeTypeVariation = "HeaderLabel" });
        if(view.Crisis is {} crisis)
        {
            int endangered=crisis.Cases.Length,urgent=crisis.PendingReceipts.Length,infectious=crisis.Carriers.Count(v=>v.CarrierEpisode is not null);
            if(endangered>0||urgent>0||infectious>0){var critical=DevelopmentUi.Text($"Critical: {endangered} endangered {(endangered==1?"colony":"colonies")}; {infectious} infectious cargo vessels; {urgent} urgent facts awaiting acknowledgement. Open Colony crises to review.","WarningLabel");critical.Name="CrisisCriticalBadge";AddChild(critical);}
            DevelopmentUi.Button(this,"Colony crises",()=>CrisisRequested?.Invoke(),"OpenCrisisOverview");
            DevelopmentUi.Button(this,"Crisis notifications",()=>CrisisNotificationsRequested?.Invoke(),"OpenCrisisNotifications");
        }
        var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill }; AddChild(picker);
        foreach (var c in view.Colonies)
        {
            string name = view.Research.ColonyNames[c.Id.ToString()]; picker.AddItem($"{name} — {c.PopulationUnitLabel} {c.Population}"); picker.SetItemMetadata(picker.ItemCount - 1, c.Id.Value);
            if (c.Id == selected) picker.Select(picker.ItemCount - 1);
        }
        if (selected is null) picker.Select(-1);
        if (Rules.HasDevelopment(view.RulesVersion))
        {
            DevelopmentUi.Button(this,"Manage colonies",()=>ManageRequested?.Invoke(),"ManageColonies");
            DevelopmentUi.Button(this,"Colony notifications",()=>NotificationsRequested?.Invoke());
            int stage=view.Colonies.Length>=11?11:view.Colonies.Length>=6?6:0;
            if(stage>0&&!_dismissedSuggestions.Contains(stage)) {
                var suggestion=new VBoxContainer{Name="GovernorSizeSuggestion"};AddChild(suggestion);
                suggestion.AddChild(DevelopmentUi.Text(stage==11?"A larger empire can use bulk plans. Keep favorite colonies manual.":"You can delegate routine colony work whenever you choose."));
                DevelopmentUi.Button(suggestion,"Dismiss suggestion",()=>{_dismissedSuggestions.Add(stage);suggestion.Hide();},"DismissSuggestion");
            }
            if(content is not null) {
                var plan=OpenAntares.Simulation.Development.GovernorPlanner.Plan(OpenAntares.Simulation.Development.GovernorPlanningQueries.Create(content,view));
                int attention=view.Colonies.Count(c=>GovernorReportPanel.CurrentIssues(content,view,c,plan).Any(i=>(preferences??OpenAntares.Game.Presentation.ColonyNotificationPreferences.Defaults).LevelFor(i.Code)==OpenAntares.Game.Presentation.NotificationLevel.Attention));
                if(attention>0)AddChild(DevelopmentUi.Text($"Attention: {attention} {(attention==1?"colony has":"colonies have")} unresolved conditions. Inspect Manage colonies.","WarningLabel"));
                var prefs=preferences??OpenAntares.Game.Presentation.ColonyNotificationPreferences.Defaults;
                var recorded=view.Development!.PendingReport??view.Development.CompletedReports.LastOrDefault();
                int actions=recorded?.Entries.Count(e=>e.BeforeWorkforce.HasValue||e.BeforeTarget!=e.AfterTarget)??0;
                if(actions>0&&prefs.LevelFor("governor_actions")==OpenAntares.Game.Presentation.NotificationLevel.Attention)
                {
                    var badge=DevelopmentUi.Text($"Attention: {actions} {(actions==1?"colony":"colonies")} with recorded governor actions (turn {recorded!.Turn}). Inspect Manage colonies.","WarningLabel");
                    badge.Name="GovernorActionAttentionBadge";AddChild(badge);
                }
                if(latestReport is {} report&&report.ToTurn==view.Turn)
                {
                    int completions=report.Colonies.Count(c=>view.Colonies.Any(owned=>owned.Id==c.Id)&&c.Economy.Production.CompletedId is not null)+(report.Research.Research.CompletedId is null?0:1);
                    if(completions>0&&prefs.LevelFor("completions")==OpenAntares.Game.Presentation.NotificationLevel.Attention)
                    {
                        var badge=DevelopmentUi.Text($"Attention: {completions} {(completions==1?"completion":"completions")} in the latest turn report (this session).","WarningLabel");
                        badge.Name="CompletionAttentionBadge";AddChild(badge);
                    }
                    var button=DevelopmentUi.Button(this,"View latest turn report (this session)",()=>LatestReportRequested?.Invoke(),"ViewLatestTurnReport");
                    button.TooltipText="Economic turn reports are retained until the next resolution or loading a session. Governor history is saved separately.";
                }
            }
        }
        picker.ItemSelected += index => ColonySelected?.Invoke(new(picker.GetItemMetadata((int)index).AsInt32()));
    }
}
