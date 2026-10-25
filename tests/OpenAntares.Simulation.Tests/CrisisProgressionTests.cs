using System;
using System.Linq;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisProgressionTests
{
    [Fact]
    public void GraceAndSixIntervalCourse()
    {
        var g=CrisisFixtures.Warning();g.State.Colonies[0].Workforce=new(2,2,2);
        g=Step(g);Assert.Equal(21,g.State.Turn);Assert.Equal(1,g.State.Colonies[0].Crisis!.Case!.GraceRemaining);
        Assert.Equal(400,EconomyCalculator.Calculate(g.Content,g.State).Colonies[0].NetProduction.Total);
        g=Step(g);Assert.Equal(22,g.State.Turn);Assert.Equal(CrisisStage.Active,g.State.Colonies[0].Crisis!.Case!.Stage);Assert.Equal(20,g.State.Colonies[0].Crisis!.Case!.Severity);
        Assert.Equal(320,EconomyCalculator.Calculate(g.Content,g.State).Colonies[0].NetProduction.Total);
        int[] severities={20,40,60,80,100,100};int[] populations={6,6,6,5,4,4};
        for(int i=0;i<6;i++){Assert.Equal(severities[i],g.State.Colonies[0].Crisis!.Case!.Severity);g=Step(g);Assert.Equal(populations[i],g.State.Colonies[0].Population);}
        Assert.Null(g.State.Colonies[0].Crisis!.Case);Assert.Equal(40,g.State.Colonies[0].Crisis!.ImmuneUntilTurn);Assert.Null(g.State.Empires[0].Crisis!.Episode);
    }
    [Fact]
    public void EarlyQuarantineRecoversWithoutMortalityAndReleasesBoundCase()
    {
        var g=CrisisFixtures.Active(quarantine:true);g.State.Colonies[0].Crisis!.ReleaseEpisode=1;
        foreach(int severity in new[]{20,25,30,35,40,45}){Assert.Equal(severity,g.State.Colonies[0].Crisis!.Case!.Severity);g=Step(g);Assert.Equal(6,g.State.Colonies[0].Population);}
        Assert.Null(g.State.Colonies[0].Crisis!.Case);Assert.False(g.State.Colonies[0].Crisis!.Quarantined);Assert.Null(g.State.Colonies[0].Crisis!.ReleaseEpisode);
    }
    [Fact]
    public void NaturalCloseWaitsForDepartingCarrier()
    {
        var g=CrisisFixtures.Active(severity:100,age:5);var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.Crisis!.CarrierEpisode=1;v.PendingMove=new(6);
        g=Step(g);Assert.Null(g.State.Colonies[0].Crisis!.Case);Assert.NotNull(g.State.Empires[0].Crisis!.Episode);Assert.Equal(1,g.State.Vessels[0].Crisis!.CarrierEpisode);Assert.Equal(0,g.State.Empires[0].Crisis!.NextEligibleTurn);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void CurePrecedesN1Mortality(bool cure)
    {
        var g=CrisisFixtures.Active(severity:80,age:3,population:1);var c=g.State.Colonies[0];c.Workforce=new(0,0,1);c.CompletedBuildingIds=new(){"cultivation_hub"};g.State.Empires[0].KnownTechnologyIds=g.Content.Technologies.Keys.ToList();
        var e=g.State.Empires[0].Crisis!;e.Episode!.CureProgress=699;e.Episode.ResearchShare=cure?100:0;
        var economy=EconomyCalculator.Calculate(g.Content,g.State);Assert.Equal(160,economy.Colonies[0].NetResearch.Total);
        g=Step(g);
        if(cure){Assert.Equal(1,g.State.Colonies[0].Population);Assert.Null(g.State.Colonies[0].Crisis!.Case);Assert.Equal(159,g.State.Empires[0].ResearchReserve);}
        else{Assert.Empty(g.State.Colonies);Assert.Single(g.State.Empires[0].Crisis!.LostColonyLocations);Assert.Single(g.State.Empires[0].Crisis!.PendingReceipts);}
        Assert.Null(g.State.Empires[0].Crisis!.Episode);Assert.Equal(53,g.State.Empires[0].Crisis!.NextEligibleTurn);
    }
    [Fact]
    public void SourceCostAndCooldownRemainFrozen()
    {
        var g=CrisisFixtures.Active();g.State.Turn=20;ObservationUpdater.Refresh(g.State,20);g.State.Empires[0].Crisis!.Episode!.CureProgress=900;
        for(int i=0;i<6;i++)g=Step(g);
        Assert.Equal(26,g.State.Turn);Assert.Equal(56,g.State.Empires[0].Crisis!.NextEligibleTurn);Assert.Equal(38,g.State.Colonies[0].Crisis!.ImmuneUntilTurn);
        Assert.Contains(g.State.Empires[0].Crisis!.PendingDigests.SelectMany(d=>d.Entries),e=>e.Kind=="episode_closed"&&e.Reason.Contains("9 research"));
        while(g.State.Turn<38)g=Step(g);Assert.Null(g.State.Colonies[0].Crisis!.ImmunityFamilyId);
        ulong random=g.State.Empires[0].Crisis!.Random.State;
        while(g.State.Turn<59)g=Step(g);
        Assert.Equal(random,g.State.Empires[0].Crisis!.Random.State);Assert.Null(g.State.Empires[0].Crisis!.Episode);
        g=Step(g);Assert.NotNull(g.State.Empires[0].Crisis!.Episode);Assert.Equal(60,g.State.Turn);Assert.Equal(1000,g.State.Empires[0].Crisis!.Episode!.CureCost);
    }
    [Fact]
    public void ResolvedUrgentReceiptOutlivesTenTurnHistory()
    {
        var g=CrisisFixtures.Active(severity:80,age:3,population:1);g=Step(g);
        for(int i=0;i<14;i++)g=Step(g);
        Assert.Empty(g.State.Colonies);Assert.Equal(10,g.State.Empires[0].Crisis!.CompletedDigests.Count);
        var receipt=Assert.Single(CrisisQueries.PendingAttention(g.State,g.State.Empires[0].Id));Assert.Equal(23,receipt.Turn);
        Assert.All(g.State.Empires[0].Crisis!.CompletedDigests,d=>Assert.True(d.Turn>23));
    }
    [Fact]
    public void WaivedResolvedCasesDoNotAccumulatePendingReceipts()
    {
        var g=CrisisFixtures.Active(severity:80,age:3,population:1);g.State.Empires[0].Crisis!.RequireAcknowledgement=false;g=Step(g);
        Assert.Empty(g.State.Empires[0].Crisis!.PendingReceipts);Assert.Contains(g.State.Empires[0].Crisis!.PendingDigests.SelectMany(d=>d.Entries),e=>e.Disposition=="waived"&&e.PopulationLost==1);
    }
    [Fact]
    public void CaseRevisionCoalescesActivationAndCriticalThreshold()
    {
        var g=CrisisFixtures.Warning(grace:1);var d=g.Content.Plagues["ash_bloom"];g=g with{Content=g.Content with{Plagues=g.Content.Plagues.SetItem(d.Id,d with{MortalityThreshold=20})}};
        g=Step(g);Assert.Equal(2,g.State.Colonies[0].Crisis!.Case!.AttentionRevision);Assert.Equal(2,CrisisQueries.PendingAttention(g.State,g.State.Empires[0].Id)[0].Revision);
        var entries=g.State.Empires[0].Crisis!.PendingDigests.SelectMany(x=>x.Entries).Where(e=>e.Kind is "active" or "critical").ToArray();Assert.Equal(2,entries.Length);Assert.All(entries,e=>Assert.Equal(2,e.Revision));
    }
    [Fact]
    public void NewestReceiptPreservesEarlierUnacknowledgedFacts()
    {
        var g=CrisisFixtures.Warning();g=Step(g);g=Step(g);
        var receipt=Assert.Single(CrisisQueries.PendingAttention(g.State,g.State.Empires[0].Id));Assert.Contains("New plague exposure",receipt.Reason);Assert.Contains("Active",receipt.Reason);Assert.Equal(2,receipt.Revision);
    }
    [Fact]
    public void AiRecordsMaterialEventsWithoutInventingHumanAcknowledgement()
    {
        var g=CrisisFixtures.TwoHumansAndAi();var d=g.Content.Plagues["ash_bloom"];g=g with{Content=g.Content with{Plagues=g.Content.Plagues.SetItem(d.Id,d with{CheckChancePercent=100})}};g.State.Turn=19;ObservationUpdater.Refresh(g.State,19);
        g=Step(g);var ai=g.State.Empires.Single(e=>e.Controller==ControllerKind.Ai);
        Assert.Empty(ai.Crisis!.PendingReceipts);Assert.Empty(CrisisQueries.PendingAttention(g.State,ai.Id));
        var illness=g.State.Colonies.Single(c=>c.EmpireId==ai.Id).Crisis!.Case!;Assert.Equal(0,illness.HandledRevision);Assert.Equal(0,illness.WaivedRevision);
        Assert.Contains(ai.Crisis.PendingDigests.SelectMany(x=>x.Entries),e=>e.Kind=="exposure"&&e.Disposition=="recorded");
    }
    [Fact]
    public void NaturalRecoveryWinsBeforeSixthPossibleDeath()
    {
        var g=CrisisFixtures.Active(severity:100,age:5,population:1,quarantine:true);g=Step(g);Assert.Single(g.State.Colonies);Assert.Equal(1,g.State.Colonies[0].Population);Assert.True(g.State.Colonies[0].Crisis!.Quarantined);
    }
    [Theory][InlineData("revision")][InlineData("immunity")]
    public void HealthOverflowNeverMutatesInput(string kind)
    {
        var g=CrisisFixtures.Active(severity:60,age:2);if(kind=="revision")g.State.Colonies[0].Crisis!.Case!.AttentionRevision=long.MaxValue-1;
        else{g.State.Turn=int.MaxValue-2;var illness=g.State.Colonies[0].Crisis!.Case!;illness.ActiveAge=5;ObservationUpdater.Refresh(g.State,g.State.Turn);}
        string before=SaveGame.Serialize(g.Content,g.State);Assert.Throws<OverflowException>(()=>Step(g));Assert.Equal(before,SaveGame.Serialize(g.Content,g.State));
    }
    internal static LoadedGame Step(LoadedGame game)
    {
        Assert.Empty(GameStateValidator.Validate(game.Content,game.State));
        var candidate=game.State.Clone();var context=CrisisResolutionContext.Capture(game.Content,game.State);var economy=EconomyCalculator.Calculate(game.Content,game.State);
        // Labelled health-only helper: apply research, not ordinary growth/production/movement.
        foreach(var e in economy.Empires)candidate.FindEmpire(e.Empire)!.ResearchReserve=e.Research.Ending;
        CrisisProgression.Apply(game.Content,game.State,candidate,economy,context);CrisisOnset.Apply(game.Content,candidate,context);CrisisReports.CloseFinishedEpisodes(game.Content,candidate,context);CrisisReports.Finalize(game.State,candidate,context);
        candidate.Turn=context.ToTurn;ObservationUpdater.Refresh(candidate,candidate.Turn);
        Assert.Empty(GameStateValidator.Validate(game.Content,candidate));
        return new(game.Content,candidate);
    }
}
