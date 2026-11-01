using System;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Knowledge;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictReportTests
{
    [Fact]
    public void BlockadeIncomeAndReportsAgreeWithActualTurn()
    {
        var g=ConflictBlockadeTests.Activate(ConflictBlockadeTests.Economic());var target=g.State.Empires[1].Id;
        string saved=OpenAntares.Simulation.Saves.SaveGame.Serialize(g.Content,g.State);
        var outcome=ConflictAtomicTurnTests.Advance(g);var report=EmpireViewProjector.ProjectReport(g.Content,outcome.State,outcome.Report!,target);
        Assert.Equal(450,report.Colonies.Single().Economy.NetProduction.Total);Assert.Single(report.Conflict!.OwnPressure);
        var blockader=EmpireViewProjector.ProjectReport(g.Content,outcome.State,outcome.Report!,g.State.Empires[0].Id);
        Assert.Empty(blockader.Conflict!.OwnPressure);Assert.DoesNotContain("ProductionReserve",JsonSerializer.Serialize(blockader.Conflict));
        string root=new System.IO.DirectoryInfo(ContentLoaderTests.CoreContentDirectory()).Parent!.Parent!.FullName;
        string hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(saved))).ToLowerInvariant();
        string relative="reports/conflict-execution/task-19/blockade-fixtures/"+hash+".json";string path=System.IO.Path.Combine(root,relative);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        if(!System.IO.File.Exists(path))System.IO.File.WriteAllText(path,saved);else Assert.Equal(saved,System.IO.File.ReadAllText(path));
        System.IO.File.WriteAllText(System.IO.Path.Combine(root,"reports/conflict-execution/task-19/blockade-ui-supplement.json"),JsonSerializer.Serialize(new{save=relative,hash,method="Labelled pre-session 701 + neutral trade100 economic supplement; no earned-campaign claim"}));
    }
    [Fact]
    public void CompletedHistoryTrimsWithoutRemovingActiveBattleOrRequiredResponse()
    {
        var g=ConflictFixtures.WithEncounter();var original=g.State.Conflict!.Encounters.Single();g.State.Conflict.Encounters.Clear();
        for(int i=1;i<=21;i++){var ended=JsonSerializer.Deserialize<ConflictEncounterState>(JsonSerializer.Serialize(original))!;ended.Id=new(i);ended.Phase=ConflictPhase.Completed;ended.CompletedTurn=0;g.State.Conflict.Encounters.Add(ended);}
        var live=JsonSerializer.Deserialize<ConflictEncounterState>(JsonSerializer.Serialize(original))!;live.Id=new(22);g.State.Conflict.Encounters.Add(live);g.State.Conflict.NextEncounterId=23;
        var outcome=ConflictAtomicTurnTests.Advance(g);var a=original.Pair.Low;
        Assert.Equal(20,outcome.State.Conflict!.Owners.Single(o=>o.Owner==a).History.Count);
        Assert.Contains(outcome.State.Conflict.Encounters,e=>e.Id.Value==22&&e.Phase!=ConflictPhase.Completed);
        Assert.DoesNotContain(outcome.State.Conflict.Encounters,e=>e.Id.Value==1);
        var save=OpenAntares.Simulation.Saves.SaveGame.Deserialize(OpenAntares.Simulation.Saves.SaveGame.Serialize(g.Content,outcome.State));Assert.True(save.Succeeded,string.Join("; ",save.Errors));
    }
    [Fact]
    public void RevisedForceEmergencyUsesOnlyOwnEncounterAndRevision()
    {
        var g=ConflictFixtures.WithEncounter();var snapshot=g.State;var candidate=snapshot.Clone();candidate.Turn++;
        var encounter=candidate.Conflict!.Encounters.Single();encounter.LowSide.NeedsResponse=true;encounter.LowSide.ConsentRevision=7;
        ObservationUpdater.Refresh(candidate,candidate.Turn);ConflictObservations.Refresh(g.Content,candidate);
        var reports=ConflictReports.Finalize(g.Content,snapshot,candidate,ConflictResolutionContext.Capture(g.Content,snapshot));
        var own=candidate.Conflict.Owners.Single(o=>o.Owner==encounter.Pair.Low);
        Assert.Contains(own.Notices,n=>n.Level==ConflictNoticeLevel.Emergency&&n.Encounter==encounter.Id&&n.Revision==7);
        Assert.DoesNotContain(candidate.Conflict.Owners.Single(o=>o.Owner==encounter.Pair.High).Notices,n=>n.Level==ConflictNoticeLevel.Emergency);
        Assert.True(reports.Single(r=>r.Viewer==encounter.Pair.Low).Encounters.Single().OwnNeedsResponse);
        Assert.False(reports.Single(r=>r.Viewer==encounter.Pair.High).Encounters.Single().OwnNeedsResponse);
    }
    [Fact]
    public void RetainedReportSurvivesCurrentEventTrimmingAndFiltersEachOwner()
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;
        ConflictDeclarationTests.Issue(g,a,new ConflictAction.DeclareWar(b));var outcome=ConflictAtomicTurnTests.Advance(g);
        var report=outcome.Report!;var before=EmpireViewProjector.ProjectReport(g.Content,outcome.State,report,a);
        var value=before.GetType().GetProperty("Conflict")?.GetValue(before);
        Assert.NotNull(value);
        var conflict=JsonSerializer.SerializeToElement(value,value.GetType());
        Assert.Equal(a.Value,conflict.GetProperty("Viewer").GetProperty("Value").GetInt32());
        Assert.Contains("war_declared",conflict.GetRawText());
        foreach(var owner in outcome.State.Conflict!.Owners)owner.Events.Clear();
        var after=EmpireViewProjector.ProjectReport(g.Content,outcome.State,report,a);
        Assert.Equal(JsonSerializer.Serialize(value,value.GetType()),JsonSerializer.Serialize(after.Conflict));
        var other=JsonSerializer.SerializeToElement(EmpireViewProjector.ProjectReport(g.Content,outcome.State,report,b).Conflict);
        Assert.Equal(b.Value,other.GetProperty("Viewer").GetProperty("Value").GetInt32());
    }
    [Fact]
    public void LastVolleyUpdatesDatedObservationEvenWhenLastObserverDies()
    {
        var g=ConflictFixtures.WithEncounter();var encounter=g.State.Conflict!.Encounters.Single();var a=encounter.LowSide.Owner;var enemy=encounter.HighSide.Members.Single();
        var star=g.State.Stars.First(s=>s.Id!=encounter.Star&&!g.State.Colonies.Any(c=>g.State.FindPlanet(c.PlanetId)!.StarId==s.Id)).Id;
        encounter.Star=star;foreach(var id in encounter.LowSide.Members.Concat(encounter.HighSide.Members))g.State.FindVessel(id)!.StationaryStarId=star;
        g.State.FindVessel(encounter.LowSide.Members.Single())!.Combat!.Hull=1;
        ObservationUpdater.Refresh(g.State,g.State.Turn);ConflictObservations.Refresh(g.Content,g.State);
        var outcome=ConflictAtomicTurnTests.Advance(g);var survivor=outcome.State.FindVessel(enemy)!;
        var observed=outcome.State.Conflict!.Owners.Single(o=>o.Owner==a).Observations.Single(o=>o.Vessel==enemy);
        Assert.False(observed.Current);Assert.Equal(survivor.Combat!.Hull,observed.Hull);
        Assert.True(observed.Hull<observed.MaxHull,"Actual final volley damage remains known after observer loss.");
    }
}
