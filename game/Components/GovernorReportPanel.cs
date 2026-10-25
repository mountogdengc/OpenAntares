using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class GovernorReportPanel:VBoxContainer
{
    public ColonyNotificationPreferences Preferences{get;set;}=ColonyNotificationPreferences.Defaults;
    public void ShowReports(ContentSet content,EmpireView view)
    {
        DevelopmentUi.Clear(this);Name="GovernorDigest";
        if(view.Development is not {} d)return;
        AddChild(new Label{Text="Governor digest",ThemeTypeVariation="HeaderLabel"});
        var reports=d.CompletedReports.Reverse().ToImmutableArray();if(d.PendingReport is {} pending)reports=reports.Insert(0,pending);
        if(reports.IsEmpty){AddChild(DevelopmentUi.Text("No governor actions have resolved yet."));return;}
        var picker=new OptionButton{Name="GovernorHistory"};AddChild(picker);foreach(var report in reports)picker.AddItem($"{(report==d.PendingReport?"Planning":"Resolved")} turn {report.Turn}");
        var summary=DevelopmentUi.Text("");summary.Name="GovernorReportSummary";AddChild(summary);var details=new VBoxContainer{Visible=false};var expand=new CheckButton{Name="ExpandGovernorDetails",Text="Show stored action details"};AddChild(expand);AddChild(details);
        void Render(){DevelopmentUi.Clear(details);var report=reports[picker.Selected];int actions=report.Entries.Count(e=>e.BeforeWorkforce.HasValue||e.BeforeTarget!=e.AfterTarget);int conditions=report.Entries.Sum(e=>e.Issues.Count(i=>i.Code!="plan_complete"));var level=Preferences.LevelFor("governor_actions");bool attention=level==NotificationLevel.Attention&&actions>0;summary.ThemeTypeVariation=attention?"WarningLabel":"SubtleLabel";summary.Text=level==NotificationLevel.Off?"Routine action notifications off. Stored details remain available.":$"{(attention?"Attention: ":"")}{actions} {(actions==1?"colony":"colonies")} with actions; {report.UnchangedCount} unchanged. {conditions} {(conditions==1?"condition":"conditions")} recorded in this report.";
            foreach(var entry in report.Entries.OrderBy(e=>e.Colony)){
                string name=view.Research.ColonyNames.TryGetValue(entry.Colony.ToString(),out var known)?known:entry.Colony.ToString();details.AddChild(DevelopmentUi.Text(name));
                if(entry.BeforeWorkforce is {} before&&entry.AfterWorkforce is {} after)details.AddChild(DevelopmentUi.Text($"Support/Production/Research: {before.Support}/{before.Production}/{before.Research} to {after.Support}/{after.Production}/{after.Research}. Support target first, remaining workers by configured weights."+(entry.WorkforceOverridden?" Overridden by a manual workforce order.":"")));
                if(entry.BeforeTarget!=entry.AfterTarget)details.AddChild(DevelopmentUi.Text($"Construction: {TargetName(entry.BeforeTarget)} to {TargetName(entry.AfterTarget)}. Explicit queue precedes assigned building preferences; active work is retained."+(entry.ConstructionOverridden?" Overridden by a manual construction order.":"")));
                foreach(var issue in entry.Issues)details.AddChild(DevelopmentUi.Text(IssueText(content,issue),issue.Code=="plan_complete"?"SubtleLabel":"WarningLabel"));
            }
            string TargetName(OpenAntares.Simulation.State.ProductionTarget? target)=>target is null?"bank production":target.Kind=="building"?content.Buildings[target.ContentId].Name:content.Vessels[target.ContentId].Name;
        }
        picker.ItemSelected+=_=>Render();expand.Toggled+=on=>details.Visible=on;Render();
    }
    internal static ImmutableArray<GovernorIssue> CurrentIssues(ContentSet content,EmpireView view,OwnedColonyView colony,GovernorPlan? plan=null)
    {
        var issues=ImmutableArray.CreateBuilder<GovernorIssue>();if(colony.Economy.SupportDeficit>0)issues.Add(new("support_shortage",null));if(colony.Economy.Production.BlockedReasonCode=="awaiting_embarkation")issues.Add(new("embarkation_wait",colony.ProjectId));
        plan??=GovernorPlanner.Plan(GovernorPlanningQueries.Create(content,view));if(plan.Succeeded)issues.AddRange(plan.Orders.FirstOrDefault(o=>o.Colony==colony.Id)?.Issues.Where(i=>i.Code!="plan_complete")??Enumerable.Empty<GovernorIssue>());
        return issues.Distinct().OrderBy(i=>i.Code,StringComparer.Ordinal).ThenBy(i=>i.ContentId,StringComparer.Ordinal).ToImmutableArray();
    }
    internal static string IssueText(ContentSet content,GovernorIssue issue)=>issue.Code switch{"support_shortage"=>"Support shortage: assign support workers or finish support infrastructure.","construction_locked"=>"Plan waiting for technology: "+(issue.ContentId is {} id&&content.Buildings.TryGetValue(id,out var b)?b.Name:"no eligible construction"),"embarkation_wait"=>"Awaiting embarkation: population, support workers or current full-capacity requirements are unmet.","plan_complete"=>"All eligible plan preferences are satisfied; production is banked.",_=>"Review colony planning."};
}
