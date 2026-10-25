using System.Linq;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisAiTests
{
    [Fact]
    public void AiQuarantinesWithoutHumanGovernorOrAcknowledgement()
    {
        var g=CrisisFixtures.TwoHumansAndAi();g.State.Turn=20;var ai=g.State.Empires[2];var c=g.State.Colonies[2];var location=CrisisQueries.Location(g.State,c);ai.Crisis!.NextEpisodeNumber=2;ai.Crisis.Episode=new(){Id=1,FamilyId="ash_bloom",Source=location,SourcePopulation=6,CureCost=1200};c.Crisis!.Case=new(){EpisodeId=1,Stage=CrisisStage.Warning,GraceRemaining=2,AttentionRevision=1,Origin=location};ObservationUpdater.Refresh(g.State,20);
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join("; ",result.Errors));c=result.State.FindColony(c.Id)!;ai=result.State.FindEmpire(ai.Id)!;
        Assert.True(c.Crisis!.Quarantined);Assert.Equal(1,c.Crisis.ReleaseEpisode);Assert.Equal(50,ai.Crisis!.Episode!.ResearchShare);Assert.Equal(1,c.Crisis.Case!.GraceRemaining);Assert.False(c.Development!.WorkforceEnabled);Assert.False(c.Development.ConstructionEnabled);Assert.Null(c.Development.Template);Assert.Empty(c.Development.Queue);Assert.Empty(ai.Development!.CompletedReports);Assert.Empty(ai.Crisis.PendingReceipts);
        Assert.Contains(ai.Crisis.CompletedDigests.SelectMany(d=>d.Entries),e=>e.Kind=="quarantine"&&e.Disposition=="automatic");
    }
    [Fact]
    public void ZeroColonyAiIdlesLegallyWithoutFreePopulation()
    {
        var g=CrisisFixtures.TwoHumansAndAi();var ai=g.State.Empires[2];var c=g.State.Colonies[2];CrisisLoss.Remove(g.State,c.Id,CrisisQueries.Location(g.State,c));
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join("; ",result.Errors));Assert.DoesNotContain(result.State.Colonies,x=>x.EmpireId==ai.Id);Assert.DoesNotContain(result.State.Vessels,v=>v.EmpireId==ai.Id);Assert.Equal(0,result.State.FindEmpire(ai.Id)!.ResearchReserve);
    }
}
