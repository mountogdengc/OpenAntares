using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class ColonyList : VBoxContainer
{
    public event Action<ColonyId>? ColonySelected;
    public event Action? ManageRequested;
    public event Action? NotificationsRequested;
    private readonly System.Collections.Generic.HashSet<int> _dismissedSuggestions = new();
    public void ShowColonies(EmpireView view, ColonyId? selected, OpenAntares.Simulation.Content.ContentSet? content = null, OpenAntares.Game.Presentation.ColonyNotificationPreferences? preferences = null)
    {
        foreach (Node child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        Visible = Rules.HasExpansion(view.RulesVersion);
        if (!Visible) return;
        AddChild(new Label { Text = "Your colonies", ThemeTypeVariation = "HeaderLabel" });
        var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill }; AddChild(picker);
        foreach (var c in view.Colonies)
        {
            string name = view.Research.ColonyNames[c.Id.ToString()]; picker.AddItem($"{name} — {c.PopulationUnitLabel} {c.Population}"); picker.SetItemMetadata(picker.ItemCount - 1, c.Id.Value);
            if (c.Id == selected) picker.Select(picker.ItemCount - 1);
        }
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
            }
        }
        picker.ItemSelected += index => ColonySelected?.Invoke(new(picker.GetItemMetadata((int)index).AsInt32()));
    }
}
