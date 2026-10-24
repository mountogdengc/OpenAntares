using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ColonyStateTests
{
    [Fact] public void ExplicitRules5FixtureIsValidAndManual() {var g=ColonyFixtures.Game();Assert.Empty(GameStateValidator.Validate(g.Content,g.State));Assert.False(g.State.Colonies[0].Development!.WorkforceEnabled);Assert.False(g.State.Colonies[0].Development!.ConstructionEnabled);}
    [Fact] public void CloneOwnsEveryMutableDevelopmentList() {
        var g=ColonyFixtures.Game();var e=g.State.Empires[0].Development!;var c=g.State.Colonies[0].Development!;e.CustomTemplates.Add(ColonyFixtures.Snapshot(g.Content,"balanced") with{Id="custom_1"});c.Queue.Add(new(1,new("building","fabrication_hall")));c.NextQueueToken=2;
        var clone=g.State.Clone();clone.Empires[0].Development!.CustomTemplates.Clear();clone.Empires[0].Development!.CompletedReports.Add(new(0,ImmutableArray<GovernorReportEntry>.Empty,0));clone.Colonies[0].Development!.Queue.Clear();
        Assert.Single(e.CustomTemplates);Assert.Empty(e.CompletedReports);Assert.Single(c.Queue);
    }
    [Theory][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)] public void LegacyCannotContainDevelopment(int version) {var g=ColonyFixtures.Game();g.State.RulesVersion=version;Assert.Contains(GameStateValidator.Validate(g.Content,g.State),e=>e.Contains("development",System.StringComparison.OrdinalIgnoreCase));}
    [Theory][InlineData("missing")][InlineData("weights")][InlineData("duplicate")][InlineData("queue33")][InlineData("token")][InlineData("appearance")][InlineData("reports")][InlineData("custom65")]
    public void InvalidDevelopmentRejects(string kind) {
        var g=ColonyFixtures.Game();var c=g.State.Colonies[0];var d=c.Development!;var e=g.State.Empires[0].Development!;
        if(kind=="missing")c.Development=null;
        if(kind=="weights")d.Template=ColonyFixtures.Snapshot(g.Content,"balanced") with{Definition=ColonyFixtures.Snapshot(g.Content,"balanced").Definition with{ProductionWeight=0,ResearchWeight=0}};
        if(kind=="duplicate"){d.Queue.Add(new(1,new("vessel","scout")));d.Queue.Add(new(1,new("vessel","scout")));d.NextQueueToken=2;}
        if(kind=="queue33"){for(int i=1;i<=33;i++)d.Queue.Add(new(i,new("vessel",g.Content.Expansion!.StartingVesselId)));d.NextQueueToken=34;}
        if(kind=="token"){d.Queue.Add(new(1,new("building","fabrication_hall")));d.NextQueueToken=1;}
        if(kind=="appearance")g.State.Planets[0].Appearance=new(2,8,0);
        if(kind=="reports")e.PendingReport=new(g.State.Turn+1,ImmutableArray<GovernorReportEntry>.Empty,0);
        if(kind=="custom65"){for(int i=1;i<=65;i++)e.CustomTemplates.Add(ColonyFixtures.Snapshot(g.Content,"balanced") with{Id="custom_"+i});e.NextTemplateNumber=66;}
        Assert.Contains(GameStateValidator.Validate(g.Content,g.State),x=>x.Contains("development",System.StringComparison.OrdinalIgnoreCase)||x.Contains("appearance",System.StringComparison.OrdinalIgnoreCase));
    }
    [Fact] public void MaximumBoundedLibraryAndQueueAreLegal() {
        var g=ColonyFixtures.Game();var e=g.State.Empires[0].Development!;for(int i=1;i<=64;i++)e.CustomTemplates.Add(ColonyFixtures.Snapshot(g.Content,"balanced") with{Id="custom_"+i});
        e.CustomTemplates=e.CustomTemplates.OrderBy(x=>x.Id,System.StringComparer.Ordinal).ToList();e.NextTemplateNumber=65;
        var c=g.State.Colonies[0].Development!;for(int i=1;i<=32;i++)c.Queue.Add(new(i,new("vessel",g.Content.Expansion!.StartingVesselId)));c.NextQueueToken=33;
        Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
    }
    [Fact] public void AiCannotOwnPlayerGovernorControls() {
        var g=ColonyFixtures.Game();var e=g.State.Empires[0];e.Controller=ControllerKind.Ai;e.AiPolicyId="expansion_v1";
        g.State.Colonies[0].Development!.Template=ColonyFixtures.Snapshot(g.Content,"balanced");
        Assert.Contains(GameStateValidator.Validate(g.Content,g.State),x=>x.Contains("AI cannot"));
    }
    [Theory][InlineData("null")][InlineData("{}")][InlineData("[]")]
    public void LegacyReaderRejectsInjectedFieldsEvenEmpty(string value) {
        var json=System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(CompatibilityFixtures.PathFor("legacy-rules4-v5-save.json")))!;
        json["state"]!["empires"]![0]!["development"]=System.Text.Json.Nodes.JsonNode.Parse(value);
        Assert.False(OpenAntares.Simulation.Saves.SaveGame.Deserialize(json.ToJsonString()).Succeeded);
    }
    [Fact] public void ReportOwnershipHistoryAndIssueCodesAreValidated() {
        var g=ColonyFixtures.Game();var d=g.State.Empires[0].Development!;
        d.PendingReport=new(g.State.Turn,ImmutableArray.Create(new GovernorReportEntry(new ColonyId(int.MaxValue),null,null,null,null,ImmutableArray.Create(new GovernorIssue("secret_foreign_fact",null)),false,false)),0);
        Assert.Contains(GameStateValidator.Validate(g.Content,g.State),e=>e.Contains("report"));
        d.PendingReport=null;for(int i=0;i<11;i++)d.CompletedReports.Add(new(i,ImmutableArray<GovernorReportEntry>.Empty,0));
        Assert.Contains(GameStateValidator.Validate(g.Content,g.State),e=>e.Contains("history"));
    }
}
