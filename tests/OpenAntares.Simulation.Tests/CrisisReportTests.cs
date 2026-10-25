using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisReportTests
{
    [Fact]
    public void CompletionThenColonyLossStillReportsExactlyOnce()
    {
        var g=CompletionLossFixture();var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join("; ",result.Errors));Assert.Empty(result.State.Colonies);
        var report=EmpireViewProjector.ProjectReport(g.Content,result.State,result.Report!,g.State.Empires[0].Id);var colony=Assert.Single(report.Colonies);
        Assert.Equal("Aldrane 1",colony.Name);Assert.Equal(Fixtures.HomeStar,colony.Star);Assert.Equal("analysis_lab",colony.Economy.Production.CompletedId);Assert.Equal(80,colony.Economy.NetProduction.Total);Assert.Equal(80,colony.Economy.Production.Ending);Assert.Single(report.CrisisEvents,e=>e.Kind=="colony_lost"&&e.PopulationLost==1);
        Assert.Equal(0,result.State.Empires[0].ResearchReserve);
        var saved=SaveGame.Deserialize(SaveGame.Serialize(g.Content,result.State));Assert.True(saved.Succeeded,string.Join("; ",saved.Errors));Assert.Single(saved.Game!.State.Empires[0].Development!.CompletedReports[0].Entries);Assert.Single(saved.Game.State.Empires[0].Crisis!.LostColonyLocations);
    }
    [Fact]
    public void OwnLossClearsKnownGhostColonyWithoutInventingNewSurvey()
    {
        var g=CrisisFixtures.Active(severity:80,age:3,population:1);g.State.Vessels.Clear();
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join("; ",result.Errors));Assert.Empty(result.State.Colonies);
        var view=EmpireViewProjector.Project(g.Content,result.State,g.State.Empires[0].Id);var home=view.Stars.Single(s=>s.Chart.Id==Fixtures.HomeStar);Assert.False(home.Current);Assert.Equal(22,home.LastObservedTurn);Assert.Null(home.Planets!.Value.Single().ColonyId);Assert.Null(home.Planets.Value.Single().OwnerId);
    }
    [Fact]
    public void RetiredMetadataIsPrunedOnlyAfterEveryRetainedReferenceEnds()
    {
        var g=CrisisFixtures.Active(severity:80,age:3,population:1);g.State.Vessels.Clear();var loss=TurnResolver.Resolve(g.Content,g.State);Assert.True(loss.Succeeded,string.Join("; ",loss.Errors));
        var owner=g.State.Empires[0].Id;var ack=CommandProcessor.Execute(g.Content,loss.State,new AcknowledgeCrisisCommand(owner,loss.State.Turn,CrisisQueries.PendingAttention(loss.State,owner)));Assert.True(ack.Accepted,ack.Rejection?.Message);
        var state=ack.State;Assert.Single(state.Empires[0].Crisis!.LostColonyLocations);
        for(int i=0;i<12;i++){var next=TurnResolver.Resolve(g.Content,state);Assert.True(next.Succeeded,string.Join("; ",next.Errors));state=next.State;}
        Assert.Empty(state.Empires[0].Crisis!.PendingReceipts);Assert.Empty(state.Empires[0].Crisis!.LostColonyLocations);Assert.Equal(10,state.Empires[0].Crisis!.CompletedDigests.Count);
    }
    [Fact]
    public void ZeroColonyEmpireMayRecoverWithSurvivingFounderAndNewId()
    {
        var g=CrisisFixtures.Active(severity:80,age:3,population:1);var vessel=g.State.Vessels[0];vessel.RoleId="harl_settler";vessel.CarriedPopulation=1;vessel.StationaryStarId=new(6);ObservationUpdater.Refresh(g.State,g.State.Turn);
        var loss=TurnResolver.Resolve(g.Content,g.State);Assert.True(loss.Succeeded,string.Join("; ",loss.Errors));Assert.Empty(loss.State.Colonies);Assert.Single(loss.State.Vessels);
        var owner=g.State.Empires[0].Id;var ack=CommandProcessor.Execute(g.Content,loss.State,new AcknowledgeCrisisCommand(owner,loss.State.Turn,CrisisQueries.PendingAttention(loss.State,owner)));Assert.True(ack.Accepted);
        var order=CommandProcessor.Execute(g.Content,ack.State,new SetFoundingOrderCommand(owner,ack.State.Turn,vessel.Id,new(7)));Assert.True(order.Accepted,order.Rejection?.Message);
        var ready=CommandProcessor.Execute(g.Content,order.State,new SetReadyCommand(owner,order.State.Turn,true));Assert.True(ready.Accepted,ready.Rejection?.Message);var colony=Assert.Single(ready.State.Colonies);Assert.NotEqual(Fixtures.HomeColony,colony.Id);Assert.Equal(1,colony.Population);Assert.Empty(ready.State.Vessels);
    }
    [Fact]
    public void InvalidForeignCrisisStateHasGenericPublicFailure()
    {
        var g=CrisisFixtures.TwoHumansAndAi();g.State.Empires[1].Crisis!.Random.Increment=2;
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.False(result.Succeeded);Assert.Same(g.State,result.State);Assert.Equal("The turn could not be completed.",Assert.Single(result.Errors));
    }
    [Fact]
    public void OwnReportOmitsForeignEventsAndIgnoresCurrentForeignHealth()
    {
        var g=CrisisFixtures.TwoHumansAndAi();g.State.Turn=19;ObservationUpdater.Refresh(g.State,19);var d=g.Content.Plagues["ash_bloom"];g=g with{Content=g.Content with{Plagues=g.Content.Plagues.SetItem(d.Id,d with{CheckChancePercent=100})}};
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join("; ",result.Errors));Assert.Equal(3,result.Report!.CrisisEvents.Length);
        var own=EmpireViewProjector.ProjectReport(g.Content,result.State,result.Report,g.State.Empires[0].Id);Assert.Single(own.CrisisEvents);Assert.Single(own.Colonies);string before=ReportSnapshot(own);
        var foreign=result.State.Empires[1].Crisis!;foreign.Episode!.CureProgress=1199;foreign.RequireAcknowledgement=false;foreign.Random.NextUInt32();
        Assert.Equal(before,ReportSnapshot(EmpireViewProjector.ProjectReport(g.Content,result.State,result.Report,g.State.Empires[0].Id)));
    }
    [Fact]
    public void StaleForeignSurveyRetainsOnlyCertifiedRetiredMetadataAfterHistoryExpires()
    {
        var g=CrisisFixtures.Active(severity:80,age:3,population:1);g.State.Vessels.Clear();var second=new EmpireState{Id=new(g.State.AllocateId()),Controller=ControllerKind.Human,SpeciesId="harl",Development=new()};second.Crisis=new(){Random=CrisisInitializer.CreateRandom(42,second.Id)};second.Surveys.Add(ObservationUpdater.Observe(g.State,Fixtures.HomeStar,g.State.Turn));g.State.Empires.Add(second);
        Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        var visited=new System.Collections.Generic.List<CrisisResolutionPhase>();var loss=TurnResolver.Resolve(g.Content,g.State,phase=>visited.Add(phase));Assert.True(loss.Succeeded,string.Join("; ",loss.Errors)+"; phases: "+string.Join(",",visited));Assert.Empty(loss.State.Colonies);var owner=g.State.Empires[0].Id;
        var ack=CommandProcessor.Execute(g.Content,loss.State,new AcknowledgeCrisisCommand(owner,loss.State.Turn,CrisisQueries.PendingAttention(loss.State,owner)));Assert.True(ack.Accepted);var state=ack.State;
        for(int i=0;i<12;i++){Assert.Empty(GameStateValidator.Validate(g.Content,state));visited.Clear();var next=TurnResolver.Resolve(g.Content,state,phase=>visited.Add(phase));Assert.True(next.Succeeded,string.Join("; ",next.Errors)+"; from turn "+state.Turn+"; phases: "+string.Join(",",visited));state=next.State;}
        Assert.Single(state.Empires[0].Crisis!.LostColonyLocations);Assert.Empty(state.Empires[0].Crisis!.PendingReceipts);Assert.Empty(state.Empires[1].Crisis!.LostColonyLocations);Assert.Equal(Fixtures.HomeColony,state.Empires[1].Surveys[0].Planets[0].ColonyId);Assert.Equal(22,state.Empires[1].Surveys[0].LastObservedTurn);
        Assert.True(SaveGame.Deserialize(SaveGame.Serialize(g.Content,state)).Succeeded);
    }
    internal static LoadedGame CompletionLossFixture()
    {
        var g=CrisisFixtures.Active(severity:80,age:3,population:1);var c=g.State.Colonies[0];c.CompletedBuildingIds=new(){"fabrication_hall"};g.State.Empires[0].KnownTechnologyIds=g.Content.Technologies.Keys.ToList();c.ProjectId="analysis_lab";c.ProductionReserve=1200;
        c.Development!.Queue.Add(new(1,new("building","analysis_lab")));c.Development.NextQueueToken=2;c.Development.ActiveQueueToken=1;c.Development.Origin=ProductionOrigin.Queue;
        g.State.Empires[0].Development!.PendingReport=new(g.State.Turn,ImmutableArray.Create(new GovernorReportEntry(c.Id,null,null,null,new("building","analysis_lab"),ImmutableArray<GovernorIssue>.Empty,false,false)),0);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));return g;
    }
    private static string ReportSnapshot(EmpireTurnReportView report)=>JsonSerializer.Serialize(new{report.FromTurn,report.ToTurn,report.Colonies,report.Research,report.Events,report.EncounterEvents,report.CrisisEvents,StarNames=report.StarNames.OrderBy(x=>x.Key).Select(x=>new{Star=x.Key.Value,Name=x.Value})});
}
