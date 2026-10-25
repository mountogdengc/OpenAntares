using System;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisTurnTests
{
    [Fact]
    public void PublishedTurnAdvancesWarningExactlyOnce()
    {
        var g=CrisisFixtures.Warning();var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join("; ",result.Errors));Assert.Equal(21,result.State.Turn);Assert.Equal(1,result.State.Colonies[0].Crisis!.Case!.GraceRemaining);Assert.Equal(2,g.State.Colonies[0].Crisis!.Case!.GraceRemaining);
    }
    [Fact]
    public void AggregateOneUnitCureSavesN1()
    {
        var g=CrisisFixtures.Active(severity:80,age:3,population:1);var c=g.State.Colonies[0];c.Workforce=new(0,0,1);c.CompletedBuildingIds=new(){"cultivation_hub"};g.State.Empires[0].KnownTechnologyIds=g.Content.Technologies.Keys.ToList();var episode=g.State.Empires[0].Crisis!.Episode!;episode.CureProgress=699;episode.ResearchShare=100;
        var forecast=EconomyCalculator.Calculate(g.Content,g.State);var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join("; ",result.Errors));Assert.Equal(1,result.State.Colonies[0].Population);Assert.Null(result.State.Colonies[0].Crisis!.Case);Assert.Equal(159,result.State.Empires[0].ResearchReserve);Assert.Equal(160,result.Report!.Economy.Colonies[0].NetResearch.Total);Assert.Equal(forecast.Empires[0].Cure,result.Report.Economy.Empires[0].Cure);Assert.Equal(1,result.Report.CrisisEvents.Sum(x=>x.ResearchDiverted));
    }
    [Fact]
    public void NaturalClosureWaitsUntilContactEnds()
    {
        var g=CrisisFixtures.Active(severity:100,age:5);AddDestination(g);var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.PendingMove=new(6);
        var first=TurnResolver.Resolve(g.Content,g.State);Assert.True(first.Succeeded,string.Join("; ",first.Errors));Assert.Null(first.State.Colonies[0].Crisis!.Case);Assert.Equal(1,first.State.Vessels[0].Crisis!.CarrierEpisode);Assert.NotNull(first.State.Empires[0].Crisis!.Episode);
        var second=TurnResolver.Resolve(g.Content,first.State);Assert.True(second.Succeeded,string.Join("; ",second.Errors));Assert.Equal(2,second.State.Colonies[1].Crisis!.Case!.GraceRemaining);Assert.NotNull(second.State.Empires[0].Crisis!.Episode);
    }
    [Fact]
    public void OnsetFreezesPostEconomyPopulationWithoutAllocatingGlobalId()
    {
        var g=CrisisFixtures.Game();g.State.Turn=19;g.State.Colonies[0].GrowthProgress=95;ObservationUpdater.Refresh(g.State,19);int next=g.State.NextEntityId;ulong random=g.State.Random.State;
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join("; ",result.Errors));Assert.Equal(7,result.State.Colonies[0].Population);Assert.NotNull(result.State.Empires[0].Crisis!.Episode);Assert.Equal(7,result.State.Empires[0].Crisis!.Episode!.SourcePopulation);Assert.Equal(1300,result.State.Empires[0].Crisis!.Episode!.CureCost);Assert.Equal(next,result.State.NextEntityId);Assert.Equal(random,result.State.Random.State);
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)]
    public void LateFailureRestoresWholeOriginal(int phaseNumber)
    {
        var g=CrisisFixtures.Game();var c=g.State.Colonies[0];g.State.Turn=19;c.ProjectId="fabrication_hall";c.ProductionReserve=1200;c.Development!.Queue.Add(new(1,new("building","fabrication_hall")));c.Development.NextQueueToken=2;c.Development.ActiveQueueToken=1;c.Development.Origin=ProductionOrigin.Queue;ObservationUpdater.Refresh(g.State,19);
        string before=SaveGame.Serialize(g.Content,g.State);ulong random=g.State.Random.State;ulong crisisRandom=g.State.Empires[0].Crisis!.Random.State;
        var control=TurnResolver.Resolve(g.Content,g.State);Assert.True(control.Succeeded,string.Join("; ",control.Errors));Assert.NotEqual(before,SaveGame.Serialize(g.Content,control.State));
        bool reached=false;var phase=(CrisisResolutionPhase)phaseNumber;
        var result=TurnResolver.Resolve(g.Content,g.State,current=>{if(current==phase){reached=true;throw new InvalidOperationException("foreign hidden health detail");}});
        Assert.False(result.Succeeded);Assert.True(reached);Assert.Same(g.State,result.State);Assert.Equal(before,SaveGame.Serialize(g.Content,result.State));Assert.Equal(random,result.State.Random.State);Assert.Equal(crisisRandom,result.State.Empires[0].Crisis!.Random.State);Assert.Null(result.Report);Assert.Equal("The turn could not be completed.",Assert.Single(result.Errors));
    }
    [Fact]
    public void FinalReadyFailurePreservesEarlierHumanPlanning()
    {
        var g=CrisisFixtures.TwoHumansAndAi();g.State.Turn=19;g.State.Empires[0].Crisis!.NextEpisodeNumber=long.MaxValue-1;ObservationUpdater.Refresh(g.State,19);
        var first=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,19,true));Assert.True(first.Accepted,first.Rejection?.Message);Assert.True(first.State.Empires[0].Ready);Assert.NotNull(first.State.Empires[0].Development!.PendingReport);
        string before=SaveGame.Serialize(g.Content,first.State);var result=CommandProcessor.Execute(g.Content,first.State,new SetReadyCommand(first.State.Empires[1].Id,19,true));Assert.False(result.Accepted);Assert.Same(first.State,result.State);Assert.Equal(before,SaveGame.Serialize(g.Content,result.State));Assert.True(result.State.Empires[0].Ready);Assert.False(result.State.Empires[1].Ready);
    }
    [Fact]
    public void CurrentRulesNeverAllocateAnUnrepresentableSuccessor()
    {
        var g=CrisisFixtures.Game();g.State.NextEntityId=int.MaxValue;var copy=g.State.Clone();Assert.Throws<OverflowException>(()=>copy.AllocateId());Assert.Equal(int.MaxValue,g.State.NextEntityId);
    }
    [Fact]
    public void GlobalAllocationOverflowDoesNotPublishEmbarkation()
    {
        var g=CrisisFixtures.Game();g.State.NextEntityId=int.MaxValue;var c=g.State.Colonies[0];c.ProjectId="harl_settler";c.ProjectKind=ProductionKinds.Vessel;c.ProductionReserve=1600;c.Development!.Origin=ProductionOrigin.Manual;
        string before=SaveGame.Serialize(g.Content,g.State);var result=TurnResolver.Resolve(g.Content,g.State);Assert.False(result.Succeeded);Assert.Same(g.State,result.State);Assert.Equal(before,SaveGame.Serialize(g.Content,result.State));Assert.Single(result.State.Vessels);Assert.Equal(6,result.State.Colonies[0].Population);
    }
    [Fact]
    public void SourceEmbarkThenLossRetainsOneActualCargoPopulation()
    {
        var g=CrisisFixtures.Active(severity:80,age:3,population:3);AddDestination(g);var c=g.State.Colonies[0];c.Workforce=new(1,2,0);c.ProjectId="harl_settler";c.ProjectKind=ProductionKinds.Vessel;c.ProductionReserve=1600;c.Development!.Origin=ProductionOrigin.Manual;
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join("; ",result.Errors));Assert.Equal(1,result.State.FindColony(c.Id)!.Population);var cargo=Assert.Single(result.State.Vessels,v=>v.RoleId=="harl_settler");Assert.Equal(1,cargo.CarriedPopulation);Assert.Equal(1,cargo.Crisis!.CarrierEpisode);
        var moved=CommandProcessor.Execute(g.Content,result.State,new SetMoveOrderCommand(c.EmpireId,result.State.Turn,cargo.Id,new(6)));Assert.True(moved.Accepted);
        var loss=TurnResolver.Resolve(g.Content,moved.State);Assert.True(loss.Succeeded,string.Join("; ",loss.Errors));Assert.Null(loss.State.FindColony(c.Id));Assert.Equal(1,loss.State.FindVessel(cargo.Id)!.CarriedPopulation);Assert.Single(loss.Report!.CrisisEvents,e=>e.Kind=="colony_lost");
    }
    internal static void AddDestination(LoadedGame g)
    {
        g.State.Colonies.Add(new(){Id=new(g.State.AllocateId()),PlanetId=new(7),EmpireId=g.State.Empires[0].Id,Population=3,Workforce=new(3,0,0),HabitatMode="native",Development=new(),Crisis=new()});ObservationUpdater.Refresh(g.State,g.State.Turn);
    }
}
