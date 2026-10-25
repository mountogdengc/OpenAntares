using System.Collections.Immutable;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Development;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class TemplatePresetTests
{
    [Fact] public void PresetsContainDraftsAndImportAllocatesNewLocalIds() {
        var g=ColonyFixtures.Game();var draft=ColonyFixtures.Snapshot(g.Content,"industrial").Definition;string text=TemplatePresetCodec.Serialize(ImmutableArray.Create(draft));Assert.Contains("OpenAntaresGovernorTemplates",text);Assert.DoesNotContain("custom_",text);
        var parsed=TemplatePresetCodec.Deserialize(text,g.Content);Assert.True(parsed.Succeeded,string.Join("; ",parsed.Errors));Assert.Single(parsed.Templates);
        var r=CommandProcessor.Execute(g.Content,g.State,new ImportGovernorTemplatesCommand(g.State.Empires[0].Id,0,parsed.Templates));Assert.True(r.Accepted);Assert.Equal("custom_1",Assert.Single(r.State.Empires[0].Development!.CustomTemplates).Id);
    }
    [Theory][InlineData("tooBig")][InlineData("65")][InlineData("33")][InlineData("refs")][InlineData("weights")][InlineData("json")][InlineData("duplicateField")][InlineData("duplicateBuilding")][InlineData("campaignIds")]
    public void InvalidPresetBatchReturnsNoPartialDrafts(string kind) {
        var g=ColonyFixtures.Game();string text=TemplatePresetCodec.Serialize(ImmutableArray.Create(ColonyFixtures.Snapshot(g.Content,"balanced").Definition));
        if(kind=="tooBig")text=new string('x',256*1024+1);
        else if(kind=="json")text="{";
        else if(kind=="duplicateField")text=text.Replace("\"version\": 1","\"version\": 1, \"version\": 1");
        else {var root=JsonNode.Parse(text)!;var list=root["templates"]!.AsArray();var item=list[0]!;if(kind=="65")for(int i=1;i<65;i++)list.Add(item.DeepClone());if(kind=="33"){var a=new JsonArray();for(int i=0;i<33;i++)a.Add("fabrication_hall");item["foundation_ids"]=a;}if(kind=="refs")item["development_ids"]=JsonNode.Parse("[\"missing\"]");if(kind=="weights"){item["production_weight"]=0;item["research_weight"]=0;}if(kind=="duplicateBuilding")item["development_ids"]=JsonNode.Parse("[\"fabrication_hall\"]");if(kind=="campaignIds")item["next_entity_id"]=500;text=root.ToJsonString();}
        var result=TemplatePresetCodec.Deserialize(text,g.Content);Assert.False(result.Succeeded);Assert.Empty(result.Templates);
    }
    [Fact] public void EmptyImportIsANoOp() {var g=ColonyFixtures.Game();var r=CommandProcessor.Execute(g.Content,g.State,new ImportGovernorTemplatesCommand(g.State.Empires[0].Id,0,ImmutableArray<GovernorTemplateDraft>.Empty));Assert.Same(g.State,r.State);}
}
