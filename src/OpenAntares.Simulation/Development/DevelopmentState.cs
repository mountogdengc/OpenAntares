using System.Collections.Generic;
using System.Collections.Immutable;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Development;
public enum SupportTargetMode { Minimum, CappedGrowth }
public enum ProductionOrigin { None, Manual, Queue, Template }
public sealed record GovernorTemplateDraft(string Name,ImmutableArray<string> FoundationIds,ImmutableArray<string> DevelopmentIds,SupportTargetMode SupportTarget,int ProductionWeight,int ResearchWeight);
public sealed record GovernorTemplateSnapshot(string Id,int Revision,GovernorTemplateDraft Definition);
public sealed record ProductionQueueEntry(int Token,ProductionTarget Target);
public sealed record QueueEntryDraft(int? Token,ProductionTarget Target);
public sealed class ColonyDevelopmentState
{
    public GovernorTemplateSnapshot? Template {get;set;}
    public bool WorkforceEnabled {get;set;}
    public bool ConstructionEnabled {get;set;}
    public List<ProductionQueueEntry> Queue {get;set;}=new();
    public int NextQueueToken {get;set;}=1;
    public int? ActiveQueueToken {get;set;}
    public ProductionOrigin Origin {get;set;}
    public ColonyDevelopmentState Clone()=>new(){Template=Template,WorkforceEnabled=WorkforceEnabled,ConstructionEnabled=ConstructionEnabled,Queue=new(Queue),NextQueueToken=NextQueueToken,ActiveQueueToken=ActiveQueueToken,Origin=Origin};
}
public sealed record NewColonyPlan(GovernorTemplateSnapshot Template,bool WorkforceEnabled,bool ConstructionEnabled);
public sealed class EmpireDevelopmentState
{
    public List<GovernorTemplateSnapshot> CustomTemplates {get;set;}=new();
    public int NextTemplateNumber {get;set;}=1;
    public NewColonyPlan? NewColonyPlan {get;set;}
    public GovernorTurnReport? PendingReport {get;set;}
    public List<GovernorTurnReport> CompletedReports {get;set;}=new();
    public EmpireDevelopmentState Clone()=>new(){CustomTemplates=new(CustomTemplates),NextTemplateNumber=NextTemplateNumber,NewColonyPlan=NewColonyPlan,PendingReport=PendingReport,CompletedReports=new(CompletedReports)};
}
public sealed record GovernorIssue(string Code,string? ContentId);
public sealed record GovernorReportEntry(ColonyId Colony,Workforce? BeforeWorkforce,Workforce? AfterWorkforce,ProductionTarget? BeforeTarget,ProductionTarget? AfterTarget,ImmutableArray<GovernorIssue> Issues,bool WorkforceOverridden,bool ConstructionOverridden)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ShipDesigns.EquipmentFact? BeforeEquipment {get;init;}
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ShipDesigns.EquipmentFact? AfterEquipment {get;init;}
}
public sealed record GovernorTurnReport(int Turn,ImmutableArray<GovernorReportEntry> Entries,int UnchangedCount);
public sealed record ColonyDevelopmentView(GovernorTemplateSnapshot? Template,bool WorkforceEnabled,bool ConstructionEnabled,ImmutableArray<ProductionQueueEntry> Queue,int? ActiveQueueToken,ProductionOrigin Origin)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? NextQueueToken {get;init;}
}
public sealed record EmpireDevelopmentView(ImmutableArray<GovernorTemplateSnapshot> CustomTemplates,NewColonyPlan? NewColonyPlan,GovernorTurnReport? PendingReport,ImmutableArray<GovernorTurnReport> CompletedReports);
