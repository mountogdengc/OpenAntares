using System;
using System.Linq;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisEconomyTests
{
    [Fact]
    public void ExactFactorsApplyBeforeProductionCompletion()
    {
        var g=CrisisFixtures.Active(quarantine:true);var c=g.State.Colonies[0];c.Workforce=new(2,2,2);
        c.CompletedBuildingIds=new(){"analysis_lab","fabrication_hall"};c.ProjectId="cultivation_hub";c.Development!.Origin=OpenAntares.Simulation.Development.ProductionOrigin.Manual;c.ProductionReserve=700;c.GrowthProgress=90;
        var owner=g.State.Empires[0];owner.KnownTechnologyIds=g.Content.Technologies.Keys.ToList();owner.Crisis!.CivicLevel=1;owner.Crisis.CivicExpiresAt=g.State.Turn+5;
        // Verdant worker output400 +flat100, then Active .8 /quarantine .75 /civic .9 =270.
        var result=EconomyCalculator.Calculate(g.Content,g.State).Colony(c.Id);
        Assert.Equal(270,result.NetProduction.Total);Assert.Equal(270,result.NetResearch.Total);
        Assert.Equal(600,result.SupportProduced.Total);Assert.Equal(600,result.SupportRequired.Total);
        Assert.Equal(0,result.GrowthEarned.Total);Assert.Equal(90,result.Growth.ProgressAfter);Assert.Equal(6,result.Growth.PopulationAfter);
        Assert.Null(result.Production.CompletedId);Assert.Equal(970,result.Production.Ending);
    }
    [Fact]
    public void QuarantineHoldsCargoBeforeCostOrEmbark()
    {
        var g=CrisisFixtures.Active(quarantine:true);var c=g.State.Colonies[0];c.ProjectId="harl_settler";c.ProjectKind=ProductionKinds.Vessel;c.Development!.Origin=OpenAntares.Simulation.Development.ProductionOrigin.Manual;c.ProductionReserve=1800;
        var result=EconomyCalculator.Calculate(g.Content,g.State).Colony(c.Id);
        Assert.Null(result.VesselCompletionPlan);Assert.Null(result.EmbarkOutcome);
        Assert.Equal("quarantine",result.Production.BlockedReasonCode);Assert.Equal(1800,result.Production.Ending);
        Assert.Null(result.Production.CompletedId);
    }
    [Fact]
    public void ExistingReserveNeverFundsCure()
    {
        var g=CrisisFixtures.Active();g.State.Colonies[0].Workforce=new(2,2,2);var owner=g.State.Empires[0];owner.ResearchReserve=1000;owner.Crisis!.Episode!.ResearchShare=100;
        var result=EconomyCalculator.Calculate(g.Content,g.State).Empire(owner.Id);
        Assert.Equal(1000,result.Research.Ending);
        Assert.Equal(320,result.Cure!.Diverted);Assert.Equal(320,result.NetResearch.Total);
    }

    [Fact]
    public void ExactFactorsAndAggregateCureSpill()
    {
        Assert.Equal(486,CrisisEconomy.Scale(CrisisEconomy.Scale(CrisisEconomy.Scale(900,8000),7500),9000));
        Assert.Equal(378,CrisisEconomy.Scale(CrisisEconomy.Scale(CrisisEconomy.Scale(700,8000),7500),9000));
        var g=CrisisFixtures.Active();var owner=g.State.Empires[0];var e=owner.Crisis!.Episode!;
        e.CureCost=1200;e.CureProgress=900;e.ResearchShare=50;
        var allocation=CrisisEconomy.SplitResearch(g.Content,g.State,owner.Id,1000);
        Assert.Equal(500,allocation.Requested);Assert.Equal(300,allocation.Diverted);Assert.Equal(700,allocation.RemainingForTechnology);Assert.True(allocation.Completes);
        e.CureCost=2;e.CureProgress=0;
        Assert.Equal(1,CrisisEconomy.SplitResearch(g.Content,g.State,owner.Id,2).Diverted);
    }
    [Theory]
    [InlineData(0,0,7)] [InlineData(25,1,6)] [InlineData(50,3,4)] [InlineData(100,7,0)]
    public void ResearchShareFloorsOnce(int share,int diverted,int ordinary)
    {
        var g=CrisisFixtures.Active();var owner=g.State.Empires[0];owner.Crisis!.Episode!.ResearchShare=share;
        var allocation=CrisisEconomy.SplitResearch(g.Content,g.State,owner.Id,7);Assert.Equal(diverted,allocation.Diverted);Assert.Equal(ordinary,allocation.RemainingForTechnology);
    }
    [Theory][InlineData("harl")][InlineData("peldra")][InlineData("sulan")][InlineData("wenth")]
    public void FlatFoundationsPayAllFactorsWithoutProductionWorkers(string species)
    {
        var g=CrisisFixtures.Active(species:species,quarantine:true);var c=g.State.Colonies[0];c.CompletedBuildingIds=new(){"analysis_lab","fabrication_hall"};var e=g.State.Empires[0];e.KnownTechnologyIds=g.Content.Technologies.Keys.ToList();e.Crisis!.CivicLevel=1;e.Crisis.CivicExpiresAt=g.State.Turn+5;
        var result=EconomyCalculator.Calculate(g.Content,g.State).Colony(c.Id);
        Assert.Equal(54,result.NetProduction.Total);Assert.Equal(54,result.NetResearch.Total);
        Assert.Equal(3,result.NetProduction.Lines.Count(l=>l.Kind is "crisis_illness" or "crisis_quarantine" or "crisis_civic"));
        Assert.All(result.NetProduction.Lines.Where(l=>l.Kind.StartsWith("crisis_",StringComparison.Ordinal)),line=>Assert.NotNull(line.Division));
    }
    [Fact]
    public void SharedFullPipelineRoundsTinyResearchAtEmpireAggregate()
    {
        var g=CrisisFixtures.Active();var s=g.State;var c=s.Colonies[0];c.Workforce=new(2,2,2);
        var planet=s.FindPlanet(c.PlanetId)!;var type=g.Content.PlanetTypes[planet.PlanetTypeId];
        // Two research workers at1 each, Active factor .8 leaves1 per colony.
        var content=g.Content with{PlanetTypes=g.Content.PlanetTypes.SetItem(type.Id,type with{ResearchPerWorker=1})};
        var second=c.Clone();second.Id=new(s.AllocateId());second.PlanetId=new(7);second.Crisis=new();s.FindPlanet(second.PlanetId)!.PlanetTypeId=type.Id;s.Colonies.Add(second);
        c.Crisis!.Quarantined=false;s.Empires[0].Crisis!.Episode!.ResearchShare=50;
        // Second colony also Active: same episode and literal one-unit output.
        second.Crisis.Case=c.Crisis.Case!.Clone();
        var result=EconomyCalculator.Calculate(content,s).Empire(c.EmpireId);
        Assert.Equal(2,result.NetResearch.Total);Assert.Equal(1,result.Cure!.Diverted);Assert.Equal(1,result.Research.Ending);
    }
    [Fact]
    public void ZeroSupportAndGrowthCapacityPreserveExistingRules()
    {
        var g=CrisisFixtures.Active(quarantine:true);var c=g.State.Colonies[0];c.Workforce=new(0,3,3);c.GrowthProgress=65;
        var zero=EconomyCalculator.Calculate(g.Content,g.State).Colony(c.Id);Assert.Equal(0,zero.NetProduction.Total);Assert.Equal(65,zero.Growth.ProgressAfter);
        c.Population=10;c.Workforce=new(10,0,0);
        var capped=EconomyCalculator.Calculate(g.Content,g.State).Colony(c.Id);Assert.Equal(0,capped.Growth.ProgressAfter);Assert.Equal(10,capped.Growth.PopulationAfter);
    }
    [Fact]
    public void ScaleUsesWideIntermediatesAndRejectsInvalidInputs()
    {
        Assert.Equal(6917529027641081855L,CrisisEconomy.Scale(long.MaxValue,7500));
        Assert.Throws<ArgumentOutOfRangeException>(()=>CrisisEconomy.Scale(-1,8000));Assert.Throws<ArgumentOutOfRangeException>(()=>CrisisEconomy.Scale(1,10001));
        Assert.Equal(0,CrisisEconomy.Scale(1,8000));
    }
    [Fact]
    public void WarningImmunityOffAndExpiredCivicDoNotPayIllness()
    {
        var g=CrisisFixtures.Warning();var c=g.State.Colonies[0];c.Workforce=new(2,2,2);var e=g.State.Empires[0].Crisis!;e.CivicLevel=1;e.CivicExpiresAt=g.State.Turn;
        Assert.Equal(400,EconomyCalculator.Calculate(g.Content,g.State).Colony(c.Id).NetProduction.Total);
        c.Crisis!.Case=null;c.Crisis.ImmunityFamilyId="ash_bloom";c.Crisis.ImmuneUntilTurn=g.State.Turn+12;
        Assert.Equal(400,EconomyCalculator.Calculate(g.Content,g.State).Colony(c.Id).NetProduction.Total);
        g=CrisisFixtures.Game(enabled:false);g.State.Colonies[0].Workforce=new(2,2,2);Assert.Equal(400,EconomyCalculator.Calculate(g.Content,g.State).Colonies[0].NetProduction.Total);
    }

    [Fact]
    public void Literal900And700PipelineGives486And378()
    {
        var g=CrisisFixtures.Active(quarantine:true);var c=g.State.Colonies[0];c.Workforce=new(2,2,2);c.CompletedBuildingIds=new(){"analysis_lab","fabrication_hall"};
        var e=g.State.Empires[0];e.KnownTechnologyIds=g.Content.Technologies.Keys.ToList();e.Crisis!.CivicLevel=1;e.Crisis.CivicExpiresAt=g.State.Turn+5;
        var type=g.Content.PlanetTypes["verdant_world"];
        var content=g.Content with{PlanetTypes=g.Content.PlanetTypes.SetItem(type.Id,type with{ProductionPerWorker=400,ResearchPerWorker=300})};
        var result=EconomyCalculator.Calculate(content,g.State).Colony(c.Id);
        Assert.Equal(486,result.NetProduction.Total);Assert.Equal(378,result.NetResearch.Total);
    }
    [Fact]
    public void FullPipelinePreservesFinalCureSpillAndOldResearchReserve()
    {
        var g=CrisisFixtures.Active();var c=g.State.Colonies[0];c.Workforce=new(2,2,2);
        var e=g.State.Empires[0];e.ResearchReserve=1000;e.Crisis!.Episode!.ResearchShare=50;e.Crisis.Episode.CureProgress=900;
        var type=g.Content.PlanetTypes["verdant_world"];var content=g.Content with{PlanetTypes=g.Content.PlanetTypes.SetItem(type.Id,type with{ResearchPerWorker=625})};
        var result=EconomyCalculator.Calculate(content,g.State).Empire(e.Id);
        Assert.Equal(1000,result.NetResearch.Total);Assert.Equal(300,result.Cure!.Diverted);Assert.Equal(700,result.Cure.RemainingForTechnology);Assert.Equal(1700,result.Research.Ending);
        Assert.Equal(700,result.Research.Change.Total);
    }
}
