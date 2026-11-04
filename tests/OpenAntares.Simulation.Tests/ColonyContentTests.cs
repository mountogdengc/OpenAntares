using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ColonyContentTests
{
    [Fact] public void DefaultsAndSpecialistsMatchApprovedValues() {
        var c=ColonyFixtures.Content();Assert.Equal(4,c.GovernorTemplates.Count);
        Assert.Equal(new[]{"balanced","growth","industrial","research"},c.GovernorTemplates.Keys);
        foreach(var t in c.GovernorTemplates.Values) Assert.Equal(new[]{"fabrication_hall","cultivation_hub","analysis_lab"},t.FoundationIds);
        Assert.Equal(new[]{"production_complex","habitat_extension"},c.GovernorTemplates["industrial"].DevelopmentIds);
        Assert.Equal(3,c.GovernorTemplates["industrial"].ProductionWeight);Assert.Equal(3,c.GovernorTemplates["research"].ResearchWeight);
        Assert.Equal(SupportTargetMode.CappedGrowth,c.GovernorTemplates["growth"].SupportTarget);
        Assert.Equal(3200,c.Buildings["production_complex"].Cost);Assert.Equal(300,c.Buildings["production_complex"].Effects.Single().Amount);
        Assert.Equal(2400,c.Technologies["adaptive_habitats"].Cost);Assert.Equal(2,c.Buildings["habitat_network"].Effects.Single(e=>e.Kind=="capacity").Amount);
        Assert.Equal(12,Rules.CurrentVersion);
    }
    [Theory][InlineData("production_weight","101")][InlineData("production_weight","-1")][InlineData("name","\"\"")][InlineData("support_target","\"future\"")][InlineData("foundation_ids","[\"missing\"]")][InlineData("development_ids","[\"fabrication_hall\"]")]
    public void InvalidDefaultsHaveLocatedErrors(string field,string json) {
        var root=JsonNode.Parse(ColonyFixtures.Json())!;root["governor_templates"]![0]![field]=JsonNode.Parse(json);
        var r=ContentLoader.Load(new[]{new ContentSource("bad-template.json",root.ToJsonString())},requireDevelopment:true);
        Assert.Null(r.Content);Assert.Contains(r.Errors,e=>e.File=="bad-template.json"&&e.EntryId=="balanced");
    }
    [Fact] public void BuildingFreeBalancedAlternativeAndRenamedPrioritiesAreLegal() {
        var root=JsonNode.Parse(ColonyFixtures.Json())!;var defaults=root["governor_templates"]!.AsArray();var balanced=defaults[0]!.DeepClone(); defaults.Clear();defaults.Add(balanced);
        balanced["foundation_ids"]=new JsonArray();balanced["development_ids"]=new JsonArray();
        var r=ContentLoader.Load(new[]{new ContentSource("alternative.json",root.ToJsonString())},requireDevelopment:true);Assert.NotNull(r.Content);
    }
    [Theory][InlineData("name81")][InlineData("weights0")][InlineData("list33")][InlineData("duplicateId")][InlineData("missingDefaults")]
    public void TemplateBoundsAndRequiredDefinitionsReject(string kind) {
        var root=JsonNode.Parse(ColonyFixtures.Json())!;var a=root["governor_templates"]!.AsArray();var b=a[0]!;
        if(kind=="name81")b["name"]=new string('x',81);
        if(kind=="weights0"){b["production_weight"]=0;b["research_weight"]=0;}
        if(kind=="list33"){var list=new JsonArray();for(int i=0;i<33;i++)list.Add("fabrication_hall");b["foundation_ids"]=list;}
        if(kind=="duplicateId")a.Add(b.DeepClone());
        if(kind=="missingDefaults")root.AsObject().Remove("governor_templates");
        var r=ContentLoader.Load(new[]{new ContentSource("invalid.json",root.ToJsonString())},requireDevelopment:true);
        Assert.Null(r.Content);Assert.NotEmpty(r.Errors);
    }
    [Fact] public void RenamedBuildingReferencesAreDataDriven() {
        var json=ColonyFixtures.Json().Replace("fabrication_hall","modded_factory");
        var r=ContentLoader.Load(new[]{new ContentSource("mod.json",json)},requireDevelopment:true);
        Assert.NotNull(r.Content);Assert.Equal("modded_factory",r.Content!.GovernorTemplates["balanced"].FoundationIds[0]);
    }
    [Fact] public void CurrentCoreHasApprovedDevelopmentAndDefaults() {
        var result=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory());Assert.NotNull(result.Content);var c=result.Content!;Assert.True(Rules.HasDevelopment(Rules.CurrentVersion));Assert.Equal(7,c.Buildings.Count);Assert.Equal(6,c.Technologies.Values.Count(t=>Rules.TechnologyAvailable(c,Rules.DevelopmentVersion,t.Id)));Assert.Equal(4,c.GovernorTemplates.Count);
        Assert.Equal(3200,c.Buildings["production_complex"].Cost);Assert.Equal(new[]{"industrial_coordination"},c.Buildings["production_complex"].PrerequisiteTechnologyIds);Assert.Equal(300,c.Buildings["production_complex"].Effects.Single().Amount);
        Assert.Equal(3200,c.Buildings["discovery_center"].Cost);Assert.Equal(300,c.Buildings["discovery_center"].Effects.Single().Amount);Assert.Equal(2800,c.Buildings["habitat_network"].Cost);Assert.Equal(new[]{new BuildingEffect("support",200),new BuildingEffect("capacity",2)},c.Buildings["habitat_network"].Effects);Assert.Equal(System.Text.Json.JsonSerializer.Serialize(ColonyFixtures.Content().GovernorTemplates),System.Text.Json.JsonSerializer.Serialize(c.GovernorTemplates));Assert.Equal(new[]{"cultivation_methods","habitat_methods"},c.Technologies["adaptive_habitats"].PrerequisiteTechnologyIds);
        foreach(string id in new[]{"industrial_coordination","cooperative_science","adaptive_habitats"})Assert.Equal(2400,c.Technologies[id].Cost);
    }
    [Theory][MemberData(nameof(PlanetAppearanceTests.SpeciesPairs),MemberType=typeof(PlanetAppearanceTests))]
    public void CurrentSpeciesPairsStartManualWithDisabledAiGovernors(string size,string human,string rival) {
        var content=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;var result=OpenAntares.Simulation.Galaxy.GalaxyGenerator.CreateNewGame(content,new(size,42,8,SpeciesId:human,RivalCount:1,RivalSpeciesId:rival,CrisesEnabled:false,AmebasEnabled:true));Assert.True(result.Succeeded,result.Error);Assert.Equal(8,result.State!.RulesVersion);Assert.False(result.State.Crisis!.Enabled);
        foreach(var c in result.State.Colonies){Assert.NotNull(c.Development);Assert.False(c.Development!.WorkforceEnabled);Assert.False(c.Development.ConstructionEnabled);Assert.Empty(c.Development.Queue);Assert.Empty(c.CompletedBuildingIds);Assert.Null(c.ProjectId);}
        var ai=result.State.Empires.Single(e=>e.Controller==OpenAntares.Simulation.State.ControllerKind.Ai);Assert.Empty(ai.Development!.CustomTemplates);Assert.Empty(ai.Development.CompletedReports);Assert.Null(ai.Development.NewColonyPlan);
    }
    [Fact] public void WhitespaceDefaultNameIsRejectedAtContentLoad() {
        var root=JsonNode.Parse(ColonyFixtures.Json())!;root["governor_templates"]![0]!["name"]="   ";var result=ContentLoader.Load(new[]{new ContentSource("bad-name.json",root.ToJsonString())},requireDevelopment:true);Assert.False(result.Succeeded);Assert.Contains(result.Errors,e=>e.File=="bad-name.json"&&e.EntryId=="balanced"&&e.Field=="name");
    }
}

