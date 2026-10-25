using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class GovernorPlanningTests
{
    private static IGovernorPlanningContext Context(LoadedGame g)=>GovernorPlanningQueries.Create(g.Content,EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id));
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplacementPreviewMatchesAssignmentAndNextReadyWithoutMutatingSource(bool queued)
    {
        var g=ColonyFixtures.WithFoundations("wenth",6,"research");
        var c=g.State.Colonies[0];
        c.CompletedBuildingIds.Add("habitat_extension");
        c.CompletedBuildingIds.Sort(StringComparer.Ordinal);
        c.ProjectKind=ProductionKinds.Vessel;
        c.ProjectId="daughter_seed";
        c.ProductionReserve=123;
        c.Development!.Origin=ProductionOrigin.Manual;
        c.Development.ConstructionEnabled=true;
        if(queued){c.Development.Queue.Add(new(7,new(ProductionKinds.Building,"production_complex")));c.Development.NextQueueToken=8;}
        var owner=g.State.Empires[0];
        g.State.Empires.Add(new(){Id=new(g.State.AllocateId()),Controller=ControllerKind.Human,SpeciesId="harl",Development=new()});
        var view=EmpireViewProjector.Project(g.Content,g.State,owner.Id);
        string before=JsonSerializer.Serialize(g.State);
        var retained=GovernorPlanningQueries.Preview(g.Content,view,c.Id,c.Development.Template,true,true);
        Assert.Equal(new Workforce(3,1,2),retained!.Workforce);
        var preview=GovernorPlanningQueries.Preview(g.Content,view,c.Id,c.Development.Template,true,true,replaceActive:true);
        Assert.NotNull(preview);
        Assert.Equal(new Workforce(2,1,3),preview.Workforce);
        Assert.Equal(queued?"production_complex":"discovery_center",preview.Target!.ContentId);
        Assert.Equal(queued?7:(int?)null,preview.QueueToken);
        Assert.Equal(before,JsonSerializer.Serialize(g.State));
        var applied=CommandProcessor.Execute(g.Content,g.State,new AssignGovernorTemplateCommand(owner.Id,0,ImmutableArray.Create(c.Id),"research",true,true,true));
        Assert.True(applied.Accepted,applied.Rejection?.Message);
        Assert.Null(applied.State.FindColony(c.Id)!.ProjectId);
        Assert.Equal(123,applied.State.FindColony(c.Id)!.ProductionReserve);
        var ready=CommandProcessor.Execute(g.Content,applied.State,new SetReadyCommand(owner.Id,0,true));
        Assert.True(ready.Accepted,ready.Rejection?.Message);
        Assert.Null(ready.Report);
        var actual=ready.State.FindColony(c.Id)!;
        Assert.Equal(preview.Workforce,actual.Workforce);
        Assert.Equal(preview.Target.ContentId,actual.ProjectId);
        Assert.Equal(preview.QueueToken,actual.Development!.ActiveQueueToken);
        Assert.Equal(c.Development.Queue,actual.Development.Queue);
        Assert.Equal(123,actual.ProductionReserve);
    }
    [Theory]
    [InlineData("harl",6,"balanced",2,2,2,500,500,0)]
    [InlineData("harl",6,"industrial",2,3,1,700,300,0)]
    [InlineData("harl",6,"research",2,1,3,300,700,0)]
    [InlineData("harl",6,"growth",3,2,1,500,300,90)]
    [InlineData("harl",10,"growth",3,4,3,900,700,0)]
    [InlineData("peldra",6,"industrial",4,2,0,1300,100,0)]
    public void LiteralSupportedAllocationAndOutputs(string species,int n,string template,int support,int production,int research,long p,long r,long growth) {
        var g=ColonyFixtures.WithFoundations(species,n,template);var context=Context(g);var plan=GovernorPlanner.Plan(context);Assert.True(plan.Succeeded,plan.Error);var order=Assert.Single(plan.Orders);Assert.Equal(new Workforce(support,production,research),order.Workforce);
        var forecast=context.Forecast(order.Colony,order.Workforce,order.Target)!;Assert.NotNull(forecast);Assert.Equal(p,forecast.NetProduction.Total);Assert.Equal(r,forecast.NetResearch.Total);if(growth!=0||n==10)Assert.Equal(growth,forecast.GrowthEarned.Total);
    }
    [Fact] public void SpecialistFlatProductionIsNotSpeciesMultiplied() {var g=ColonyFixtures.WithFoundations("harl",6,"industrial");g.State.Colonies[0].CompletedBuildingIds.Add("production_complex");g.State.Colonies[0].CompletedBuildingIds.Sort(StringComparer.Ordinal);var ctx=Context(g);var order=Assert.Single(GovernorPlanner.Plan(ctx).Orders);var f=ctx.Forecast(order.Colony,order.Workforce,order.Target)!;Assert.Equal(1000,f.NetProduction.Total);Assert.Equal(300,f.NetResearch.Total);}
    [Fact] public void LockedFirstEntrySkipsToLegalWithoutDiscardingQueue() {
        var g=ColonyFixtures.Game();var c=g.State.Colonies[0];c.Development!.ConstructionEnabled=true;c.Development.Queue.Add(new(1,new("building","production_complex")));c.Development.Queue.Add(new(2,new("building","fabrication_hall")));c.Development.NextQueueToken=3;
        var plan=GovernorPlanner.Plan(Context(g));Assert.True(plan.Succeeded);var row=Assert.Single(plan.Orders);Assert.Equal("fabrication_hall",row.Target!.ContentId);Assert.Equal(2,row.QueueToken);Assert.Empty(row.Issues);Assert.Equal(2,c.Development.Queue.Count);
    }
    [Fact] public void EntirelyLockedQueueExplainsBankingAndExhaustedPlanExplainsCompletion() {
        var g=ColonyFixtures.Game();var d=g.State.Colonies[0].Development!;d.ConstructionEnabled=true;d.Queue.Add(new(1,new("building","production_complex")));d.NextQueueToken=2;
        var row=Assert.Single(GovernorPlanner.Plan(Context(g)).Orders);Assert.Null(row.Target);Assert.Contains(row.Issues,i=>i.Code=="construction_locked");
        d.Queue.Clear();row=Assert.Single(GovernorPlanner.Plan(Context(g)).Orders);Assert.Contains(row.Issues,i=>i.Code=="plan_complete");
    }
    [Fact] public void RetainsManualAndUnembarkableExplicitVesselTargets() {
        var g=ColonyFixtures.WithFoundations("wenth",6,"industrial");var c=g.State.Colonies[0];c.ProjectKind="vessel";c.ProjectId="wenth_seedship";c.Development!.Origin=ProductionOrigin.Manual;c.Development.ConstructionEnabled=true;
        // Resolve role through the species definition, not a guessed core identifier.
        c.ProjectId=g.Content.Species["wenth"].AllowedVesselIds.Single(id=>g.Content.Vessels[id].CanFoundColony);
        c.CompletedBuildingIds.Add("habitat_extension");c.CompletedBuildingIds.Sort(StringComparer.Ordinal);
        var row=Assert.Single(GovernorPlanner.Plan(Context(g)).Orders);Assert.Equal(c.ProjectId,row.Target!.ContentId);Assert.True(row.Workforce.Support>=3);Assert.Contains(row.Issues,i=>i.Code=="embarkation_wait");
    }
    [Theory][InlineData(true)][InlineData(false)] public void ZeroSupportRateHasExplicitFlatSupportBranch(bool covered) {
        var g=ColonyFixtures.WithFoundations("harl",6,"industrial");var planet=g.State.Planets.Single(p=>p.Id==g.State.Colonies[0].PlanetId);var type=g.Content.PlanetTypes[planet.PlanetTypeId];
        var content=g.Content with{PlanetTypes=g.Content.PlanetTypes.SetItem(type.Id,type with{SupportPerWorker=0}),Buildings=g.Content.Buildings.SetItem("cultivation_hub",g.Content.Buildings["cultivation_hub"] with{Effects=ImmutableArray.Create(new BuildingEffect("support",covered?600:200))})};g=g with{Content=content};
        var row=Assert.Single(GovernorPlanner.Plan(Context(g)).Orders);Assert.Equal(covered?0:6,row.Workforce.Support);Assert.Equal(!covered,row.Issues.Any(i=>i.Code=="support_shortage"));
    }
    [Fact] public void ProjectionsArePrivateAndPolicyConsumesNoIdsOrRandomness() {
        var g=ColonyFixtures.WithFoundations("harl",6,"industrial");string before=JsonSerializer.Serialize(g.State);var view=EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id);Assert.NotNull(view.Development);Assert.NotNull(view.Colonies[0].Development);
        var ctx=GovernorPlanningQueries.Create(g.Content,view);Assert.Null(ctx.View.Galaxy.Seed);var first=GovernorPlanner.Plan(ctx);Assert.Equal(JsonSerializer.Serialize(first),JsonSerializer.Serialize(GovernorPlanner.Plan(ctx)));Assert.Equal(before,JsonSerializer.Serialize(g.State));
    }
    [Theory][InlineData(1)][InlineData(2147483647)]
    public void AllocationIsBoundedAtPopulationExtremes(int n) {
        var g=ColonyFixtures.WithFoundations("harl",n,"industrial");g=g with{Content=g.Content with{Species=g.Content.Species.SetItem("harl",g.Content.Species["harl"] with{BaseCapacity=int.MaxValue})}};
        var result=GovernorPlanner.Plan(Context(g));Assert.True(result.Succeeded,result.Error);var w=Assert.Single(result.Orders).Workforce;Assert.Equal(n,(long)w.Support+w.Production+w.Research);Assert.False(w.HasNegativeCount);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void ResearchOnlyWeightsNeedProductionWorkerOnlyWithoutFlatProduction(bool factory) {
        var g=ColonyFixtures.WithFoundations("harl",6,"research");var c=g.State.Colonies[0];if(!factory)c.CompletedBuildingIds.Remove("fabrication_hall");c.ProjectId=factory?"production_complex":"fabrication_hall";c.Development!.Origin=ProductionOrigin.Manual;
        var snapshot=c.Development.Template! with{Id="custom_1",Definition=c.Development.Template!.Definition with{ProductionWeight=0,ResearchWeight=100}};c.Development.Template=snapshot;g.State.Empires[0].Development!.CustomTemplates.Add(snapshot);g.State.Empires[0].Development!.NextTemplateNumber=2;
        var row=Assert.Single(GovernorPlanner.Plan(Context(g)).Orders);Assert.Equal(factory?0:1,row.Workforce.Production);Assert.Equal(factory?4:3,row.Workforce.Research);
    }
    [Theory][InlineData("native",12)][InlineData("sealed",8)]
    public void SulanUsesCurrentHabitatCapacity(string habitat,int capacity) {
        var g=ColonyFixtures.WithFoundations("sulan",6,"growth");g.State.Colonies[0].HabitatMode=habitat;var ctx=Context(g);var row=Assert.Single(GovernorPlanner.Plan(ctx).Orders);Assert.Equal(capacity,ctx.Forecast(row.Colony,row.Workforce,row.Target)!.Growth.Capacity);
    }
    [Fact] public void FullWenthUsesCurrentExpandedCapacityAndReservesCargoWorkers() {
        var g=ColonyFixtures.WithFoundations("wenth",8,"industrial");var c=g.State.Colonies[0];c.CompletedBuildingIds.Add("habitat_extension");c.CompletedBuildingIds.Sort(StringComparer.Ordinal);c.ProjectKind="vessel";c.ProjectId=g.Content.Species["wenth"].AllowedVesselIds.Single(id=>g.Content.Vessels[id].CanFoundColony);c.Development!.Origin=ProductionOrigin.Manual;
        var ctx=Context(g);var row=Assert.Single(GovernorPlanner.Plan(ctx).Orders);Assert.Equal(new Workforce(3,4,1),row.Workforce);Assert.DoesNotContain(row.Issues,i=>i.Code=="embarkation_wait");Assert.Equal(8,ctx.Forecast(row.Colony,row.Workforce,row.Target)!.Growth.Capacity);
    }
    [Fact] public void HiddenOverflowingForeignEconomyCannotChangeOwnOrders() {
        var g=ColonyFixtures.WithFoundations("harl",6,"industrial");var first=JsonSerializer.Serialize(GovernorPlanner.Plan(Context(g)));
        var foreign=new EmpireState{Id=new(8),Controller=ControllerKind.Ai,SpeciesId="peldra",AiPolicyId="expansion_v1",ResearchReserve=long.MaxValue,Development=new()};g.State.Empires.Add(foreign);
        g.State.Colonies.Add(new(){Id=new(9),EmpireId=foreign.Id,PlanetId=new(7),Population=6,Workforce=new(0,0,6),ProductionReserve=long.MaxValue,HabitatMode="native",Development=new()});
        Assert.Equal(first,JsonSerializer.Serialize(GovernorPlanner.Plan(Context(g))));
        foreign.SpeciesId="wenth";g.State.Colonies[1].ProjectId="fabrication_hall";g.State.Planets.Single(p=>p.Id.Value==7).Appearance=new(1,7,ulong.MaxValue);
        Assert.Equal(first,JsonSerializer.Serialize(GovernorPlanner.Plan(Context(g))));
    }
    [Fact] public void OwnOverflowReturnsFailureWithoutPartialOrders() {
        var g=ColonyFixtures.WithFoundations("harl",6,"industrial");var view=EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id);view=view with{Colonies=ImmutableArray.Create(view.Colonies[0] with{ProductionReserve=long.MaxValue})};
        var result=GovernorPlanner.Plan(GovernorPlanningQueries.Create(g.Content,view));Assert.False(result.Succeeded);Assert.Empty(result.Orders);
    }
}
