using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ColonyCommandTests
{
    private static CommandOutcome Run(LoadedGame g,Func<EmpireId,int,Command> c)=>CommandProcessor.Execute(g.Content,g.State,c(g.State.Empires[0].Id,g.State.Turn));
    private static GovernorTemplateDraft Draft(LoadedGame g)=>ColonyFixtures.Snapshot(g.Content,"industrial").Definition with{Name="My industry"};
    [Fact] public void CopiesAreIndependentAndAssignmentIsASnapshot() {
        var g=ColonyFixtures.Game();var first=Run(g,(e,t)=>new UpsertGovernorTemplateCommand(e,t,null,Draft(g)));Assert.True(first.Accepted);g=g with{State=first.State};
        var assigned=Run(g,(e,t)=>new AssignGovernorTemplateCommand(e,t,ImmutableArray.Create(g.State.Colonies[0].Id),"custom_1",true,true));Assert.True(assigned.Accepted);g=g with{State=assigned.State};
        var edited=Run(g,(e,t)=>new UpsertGovernorTemplateCommand(e,t,"custom_1",Draft(g) with{Name="Updated"}));Assert.True(edited.Accepted);
        Assert.Equal(1,edited.State.Colonies[0].Development!.Template!.Revision);Assert.Equal(2,edited.State.Empires[0].Development!.CustomTemplates.Single().Revision);
        Assert.Equal("My industry",edited.State.Colonies[0].Development!.Template!.Definition.Name);Assert.Equal(1,g.State.Empires[0].Development!.CustomTemplates.Single().Revision);
    }
    [Fact] public void MixedOwnershipBulkAssignmentChangesNothing() {var g=ColonyFixtures.Game();string before=JsonSerializer.Serialize(g.State);var r=Run(g,(e,t)=>new AssignGovernorTemplateCommand(e,t,ImmutableArray.Create(g.State.Colonies[0].Id,new ColonyId(int.MaxValue)),"industrial",true,true));Assert.False(r.Accepted);Assert.Same(g.State,r.State);Assert.Equal(before,JsonSerializer.Serialize(r.State));}
    [Theory][InlineData(false)][InlineData(true)] public void ReferencedTemplateCannotBeDeleted(bool inherited) {
        var g=ColonyFixtures.Game();g=g with{State=Run(g,(e,t)=>new UpsertGovernorTemplateCommand(e,t,null,Draft(g))).State};
        g=g with{State=inherited?Run(g,(e,t)=>new SetNewColonyPlanCommand(e,t,"custom_1",true,true)).State:Run(g,(e,t)=>new AssignGovernorTemplateCommand(e,t,ImmutableArray.Create(g.State.Colonies[0].Id),"custom_1",true,true)).State};
        var r=Run(g,(e,t)=>new DeleteGovernorTemplateCommand(e,t,"custom_1"));Assert.False(r.Accepted);Assert.Same(g.State,r.State);
    }
    [Fact] public void ManualOrdersPauseOnlyTheirChannel() {
        var g=ColonyFixtures.WithFoundations("harl",6,"industrial");var c=g.State.Colonies[0];c.Development!.ConstructionEnabled=true;
        var noOp=Run(g,(e,t)=>new SetWorkforceCommand(e,t,c.Id,c.Workforce));Assert.Same(g.State,noOp.State);Assert.True(c.Development.WorkforceEnabled);
        var r=Run(g,(e,t)=>new SetWorkforceCommand(e,t,c.Id,new(2,3,1)));Assert.True(r.Accepted);Assert.False(r.State.Colonies[0].Development!.WorkforceEnabled);Assert.True(r.State.Colonies[0].Development!.ConstructionEnabled);
        var p=Run(g,(e,t)=>new SetProjectCommand(e,t,c.Id,"production_complex"));Assert.True(p.Accepted);Assert.False(p.State.Colonies[0].Development!.ConstructionEnabled);Assert.True(p.State.Colonies[0].Development!.WorkforceEnabled);Assert.Equal(ProductionOrigin.Manual,p.State.Colonies[0].Development!.Origin);
    }
    [Fact] public void QueueEditCannotSilentlyReplaceActiveWork() {
        var g=ColonyFixtures.Game();var c=g.State.Colonies[0];c.ProjectId="fabrication_hall";c.ProductionReserve=333;c.Development!.Origin=ProductionOrigin.Queue;c.Development.Queue.Add(new(1,new("building","fabrication_hall")));c.Development.ActiveQueueToken=1;c.Development.NextQueueToken=2;
        var r=Run(g,(e,t)=>new SetColonyQueueCommand(e,t,c.Id,ImmutableArray<QueueEntryDraft>.Empty));Assert.False(r.Accepted);Assert.Same(g.State,r.State);
        r=Run(g,(e,t)=>new SetColonyQueueCommand(e,t,c.Id,ImmutableArray<QueueEntryDraft>.Empty,true));Assert.True(r.Accepted);Assert.Null(r.State.Colonies[0].ProjectId);Assert.Equal(333,r.State.Colonies[0].ProductionReserve);
    }
    [Fact] public void StructurallyEqualCommandsAreNoOps() {
        var g=ColonyFixtures.Game();g=g with{State=Run(g,(e,t)=>new UpsertGovernorTemplateCommand(e,t,null,Draft(g))).State};g.State.Empires[0].Ready=true;
        var r=Run(g,(e,t)=>new UpsertGovernorTemplateCommand(e,t,"custom_1",Draft(g) with{FoundationIds=Draft(g).FoundationIds.ToArray().ToImmutableArray()}));Assert.Same(g.State,r.State);Assert.True(r.State.Empires[0].Ready);
    }
    [Theory][InlineData("noTemplate")][InlineData("duplicateColony")][InlineData("foreignToken")][InlineData("duplicateBuilding")][InlineData("counter")][InlineData("oldRules")]
    public void InvalidBatchesAreAtomic(string kind) {
        var g=ColonyFixtures.Game();var c=g.State.Colonies[0];if(kind=="counter")c.Development!.NextQueueToken=int.MaxValue;if(kind=="oldRules")g.State.RulesVersion=4;
        Command command=kind switch {
            "noTemplate"=>new SetColonyAutomationCommand(c.EmpireId,0,c.Id,true,false),
            "duplicateColony"=>new AssignGovernorTemplateCommand(c.EmpireId,0,ImmutableArray.Create(c.Id,c.Id),"balanced",true,true),
            "foreignToken"=>new SetColonyQueueCommand(c.EmpireId,0,c.Id,ImmutableArray.Create(new QueueEntryDraft(55,new("building","fabrication_hall")))),
            "duplicateBuilding"=>new SetColonyQueueCommand(c.EmpireId,0,c.Id,ImmutableArray.Create(new QueueEntryDraft(null,new("building","fabrication_hall")),new QueueEntryDraft(null,new("building","fabrication_hall")))),
            _=>new SetColonyQueueCommand(c.EmpireId,0,c.Id,ImmutableArray.Create(new QueueEntryDraft(null,new("building","fabrication_hall"))))};
        string before=JsonSerializer.Serialize(g.State);var r=CommandProcessor.Execute(g.Content,g.State,command);Assert.False(r.Accepted);Assert.Same(g.State,r.State);Assert.Equal(before,JsonSerializer.Serialize(r.State));
    }
    [Fact] public void QueueOnlyConstructionAndRepeatedVesselsAreLegal() {
        var g=ColonyFixtures.Game();var c=g.State.Colonies[0];string role=g.Content.Expansion!.StartingVesselId;
        var r=Run(g,(e,t)=>new SetColonyQueueCommand(e,t,c.Id,ImmutableArray.Create(new QueueEntryDraft(null,new("vessel",role)),new QueueEntryDraft(null,new("vessel",role)),new QueueEntryDraft(null,new("building","production_complex")))));Assert.True(r.Accepted);g=g with{State=r.State};
        r=Run(g,(e,t)=>new SetColonyAutomationCommand(e,t,c.Id,false,true));Assert.True(r.Accepted);Assert.True(r.State.Colonies[0].Development!.ConstructionEnabled);Assert.Null(r.State.Colonies[0].Development!.Template);
    }
    [Theory][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)]
    public void AllDevelopmentCommandsRejectLegacyCampaigns(int version) {
        var g=ColonyFixtures.Game();g.State.RulesVersion=version;var e=g.State.Empires[0].Id;var c=g.State.Colonies[0].Id;
        Command[] commands={new UpsertGovernorTemplateCommand(e,0,null,Draft(g)),new DeleteGovernorTemplateCommand(e,0,"custom_1"),new AssignGovernorTemplateCommand(e,0,ImmutableArray.Create(c),"balanced",true,true),new SetColonyAutomationCommand(e,0,c,false,true),new SetColonyQueueCommand(e,0,c,ImmutableArray<QueueEntryDraft>.Empty),new SetNewColonyPlanCommand(e,0,"balanced",true,true),new ImportGovernorTemplatesCommand(e,0,ImmutableArray.Create(Draft(g)))};
        foreach(var cmd in commands){var r=CommandProcessor.Execute(g.Content,g.State,cmd);Assert.False(r.Accepted);Assert.Same(g.State,r.State);}
    }
    [Fact] public void ResumeRetainsManuallyPinnedWorkAndAssignedRevision() {
        var g=ColonyFixtures.WithFoundations("harl",6,"industrial");var c=g.State.Colonies[0];c.ProjectId="habitat_extension";c.Development!.Origin=ProductionOrigin.Manual;
        var r=Run(g,(e,t)=>new SetColonyAutomationCommand(e,t,c.Id,true,true));Assert.True(r.Accepted);Assert.Equal("habitat_extension",r.State.Colonies[0].ProjectId);Assert.Equal("industrial",r.State.Colonies[0].Development!.Template!.Id);
    }
    [Fact] public void FullLibraryAndExhaustedCountersRejectWholeImport() {
        var g=ColonyFixtures.Game();var library=g.State.Empires[0].Development!;
        for(int i=1;i<=63;i++)library.CustomTemplates.Add(new("custom_"+i,1,Draft(g)));
        library.CustomTemplates=library.CustomTemplates.OrderBy(t=>t.Id,StringComparer.Ordinal).ToList();library.NextTemplateNumber=64;
        var r=Run(g,(e,t)=>new ImportGovernorTemplatesCommand(e,t,ImmutableArray.Create(Draft(g),Draft(g))));Assert.False(r.Accepted);Assert.Same(g.State,r.State);Assert.Equal(63,library.CustomTemplates.Count);
        library.NextTemplateNumber=int.MaxValue;r=Run(g,(e,t)=>new UpsertGovernorTemplateCommand(e,t,null,Draft(g)));Assert.False(r.Accepted);Assert.Same(g.State,r.State);
    }
    [Fact] public void ManualInterventionMarksOnlyPendingGovernorAction() {
        var g=ColonyFixtures.WithFoundations("harl",6,"industrial");var c=g.State.Colonies[0];
        g.State.Empires[0].Development!.PendingReport=new(0,ImmutableArray.Create(new GovernorReportEntry(c.Id,new(6,0,0),new(2,3,1),null,new("building","production_complex"),ImmutableArray<GovernorIssue>.Empty,false,false)),0);
        var r=Run(g,(e,t)=>new SetWorkforceCommand(e,t,c.Id,new(2,2,2)));Assert.True(r.Accepted);var row=Assert.Single(r.State.Empires[0].Development!.PendingReport!.Entries);Assert.True(row.WorkforceOverridden);Assert.False(row.ConstructionOverridden);
    }
}
