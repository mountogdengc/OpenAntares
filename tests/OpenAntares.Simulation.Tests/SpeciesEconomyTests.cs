using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class SpeciesEconomyTests
{
    [Theory]
    [InlineData("peldra",1200,1200,600,600,5)]
    [InlineData("harl",600,600,400,400,10)]
    [InlineData("sulan",600,600,400,400,10)]
    [InlineData("wenth",600,400,400,400,30)]
    public void ExactOpeningAndResolvedForecast(string id,long produced,long required,long production,long research,long growth)
    {
        var content=SpeciesFixtures.Content();var state=SpeciesFixtures.AtHome(id);string before=SpeciesFixtures.Snapshot(state);
        var forecast=EconomyCalculator.Calculate(content,state).Colonies[0];
        Assert.Equal(produced,forecast.SupportProduced.Total);Assert.Equal(required,forecast.SupportRequired.Total);
        Assert.Equal(production,forecast.NetProduction.Total);Assert.Equal(research,forecast.NetResearch.Total);Assert.Equal(growth,forecast.GrowthEarned.Total);
        var resolved=TurnResolver.Resolve(content,state);Assert.True(resolved.Succeeded,string.Join(";",resolved.Errors));
        Assert.Equal(JsonSerializer.Serialize(forecast),JsonSerializer.Serialize(resolved.Report!.Economy.Colonies[0]));
        Assert.Equal(before,SpeciesFixtures.Snapshot(state));
    }
    [Fact]
    public void PeldraShortageAndFlatBuildingsAreNotMultiplied()
    {
        var content=SpeciesFixtures.Content();var state=SpeciesFixtures.AtHome("peldra");var c=state.Colonies[0];
        c.Workforce=new(2,2,2);c.GrowthProgress=37;
        var result=EconomyCalculator.Calculate(content,state).Colonies[0];
        Assert.Equal(600,result.SupportProduced.Total);Assert.Equal(1200,result.SupportRequired.Total);
        Assert.Equal(1200,result.GrossProduction.Total);Assert.Equal(600,result.NetProduction.Total);Assert.Equal(0,result.GrowthEarned.Total);Assert.Equal(37,result.Growth.ProgressAfter);
        c.Workforce=new(4,1,1);c.CompletedBuildingIds.Add("cultivation_hub");
        result=EconomyCalculator.Calculate(content,state).Colonies[0];Assert.Equal(1400,result.SupportProduced.Total);Assert.Equal(15,result.GrowthEarned.Total);
        c.CompletedBuildingIds.Clear();c.CompletedBuildingIds.Add("fabrication_hall");
        result=EconomyCalculator.Calculate(content,state).Colonies[0];Assert.Equal(700,result.GrossProduction.Total);
    }
    [Fact]
    public void AggregateRoundingThenBuildingThenShortageHasBothRemainders()
    {
        var content=SpeciesFixtures.Content();var state=SpeciesFixtures.AtHome("harl");var c=state.Colonies[0];
        c.Workforce=new(2,3,1);c.CompletedBuildingIds.Add("fabrication_hall");
        content=content with { Species=content.Species.SetItem("harl",content.Species["harl"] with {ProductionBasisPoints=15000}), PlanetTypes=content.PlanetTypes.SetItem("verdant_world",content.PlanetTypes["verdant_world"] with {SupportPerWorker=200,ProductionPerWorker=101})};
        var result=EconomyCalculator.Calculate(content,state).Colonies[0];
        Assert.Equal(554,result.GrossProduction.Total);Assert.Equal(369,result.NetProduction.Total);
        Assert.Equal(369,result.NetProduction.Lines.Sum(l=>l.Amount));
        var adjustment=Assert.Single(result.GrossProduction.Lines.Where(l=>l.Kind=="species"));
        Assert.Equal("harl",adjustment.SourceId);Assert.Equal(151,adjustment.Amount);Assert.Equal(5000,adjustment.Division!.Remainder);
        var shortage=Assert.Single(result.NetProduction.Lines.Where(l=>l.Kind==BreakdownKinds.Shortage));Assert.Equal(200,shortage.Division!.Remainder);
    }
    [Theory][InlineData("harl",4,1,1,5,10,50)][InlineData("harl",2,2,2,15,10,70)]
    [InlineData("sulan",4,1,1,12,12,0)][InlineData("wenth",2,1,1,6,6,20)]
    [InlineData("legacy",4,1,1,8,10,0)]
    public void NoCommandGrowthTraceMatchesApprovedCapacityTurn(string id,int sw,int pw,int rw,int turns,int capacity,int discard)
    {
        var content=id=="legacy"?ExpansionFixtures.Content():SpeciesFixtures.Content();
        var state=id=="legacy"?ExpansionFixtures.AtHome():SpeciesFixtures.AtHome(id);state.Colonies[0].Workforce=new(sw,pw,rw);
        for(int t=1;t<=turns;t++)
        {
            var next=TurnResolver.Resolve(content,state);Assert.True(next.Succeeded,string.Join(";",next.Errors));state=next.State;
            if(t<turns)Assert.True(state.Colonies[0].Population<capacity);
            else {Assert.Equal(capacity,state.Colonies[0].Population);Assert.Equal(discard,next.Report!.Economy.Colonies[0].Growth.DiscardedGrowth);Assert.Equal(0,state.Colonies[0].GrowthProgress);}
            if(id=="harl"&&sw==2&&t<=10)Assert.Equal(t<10?6:7,state.Colonies[0].Population);
        }
    }
    [Fact]
    public void PostEmbarkSupportUsesSpeciesRequirement()
    {
        var content=SpeciesFixtures.Content();var state=SpeciesFixtures.AtHome("peldra");var c=state.Colonies[0];
        c.ProjectKind=ProductionKinds.Vessel;c.ProjectId="peldra_settler";c.ProductionReserve=2600;
        var forecast=EconomyCalculator.Calculate(content,state).Colonies[0];
        Assert.NotNull(forecast.EmbarkOutcome);Assert.Equal(4,forecast.EmbarkOutcome!.PopulationAfter);
        Assert.Equal(600,forecast.EmbarkOutcome.NextSupportProduced.Total);Assert.Equal(800,forecast.EmbarkOutcome.NextSupportRequired.Total);Assert.True(forecast.EmbarkOutcome.ReassignmentNeeded);
    }
    [Fact]
    public void ReserveOverflowRollsBackSpeciesTurn()
    {
        var state=SpeciesFixtures.AtHome("peldra");state.Colonies[0].ProductionReserve=long.MaxValue;string before=SpeciesFixtures.Snapshot(state);
        var next=TurnResolver.Resolve(SpeciesFixtures.Content(),state);Assert.False(next.Succeeded);Assert.Same(state,next.State);Assert.Equal(before,SpeciesFixtures.Snapshot(state));
    }
    [Fact]
    public void ValidLargeSupportGrowthIsCappedBeforeStoredTotalOverflow()
    {
        var root=JsonNode.Parse(SpeciesFixtures.ContentJson())!;
        var harl=root["species"]!.AsArray().Single(p=>p!["id"]!.GetValue<string>()=="harl")!.DeepClone();
        harl["support_per_population"]=1;harl["support_per_growth_point"]=1;harl["base_growth"]=1000;harl["max_surplus_growth"]=40;root["species"]=new JsonArray(harl);
        var verdant=root["planet_types"]!.AsArray().Single(p=>p!["id"]!.GetValue<string>()=="verdant_world")!;
        verdant["support_per_worker"]=(long.MaxValue-200)/12;
        var loaded=ContentLoader.Load(new[]{new ContentSource("large-valid-support.json",root.ToJsonString())},requireSpecies:true);Assert.True(loaded.Succeeded,string.Join(";",loaded.Errors));
        var state=SpeciesFixtures.AtHome("harl");var c=state.Colonies[0];c.Population=12;c.Workforce=new(12,0,0);c.CompletedBuildingIds.Add("cultivation_hub");c.CompletedBuildingIds.Add("habitat_extension");state.Empires[0].KnownTechnologyIds.Add("cultivation_methods");state.Empires[0].KnownTechnologyIds.Add("habitat_methods");
        Assert.Empty(GameStateValidator.Validate(loaded.Content!,state));
        var next=TurnResolver.Resolve(loaded.Content!,state);Assert.True(next.Succeeded,string.Join(";",next.Errors));
        var growth=next.Report!.Economy.Colonies[0].GrowthEarned;Assert.Equal(0,growth.Total);Assert.Equal(-1040,Assert.Single(growth.Lines.Where(l=>l.Kind==BreakdownKinds.CapacityLimit)).Amount);
    }

}
