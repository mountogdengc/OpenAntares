using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;

public sealed class ScoutingReviewRegressionTests
{
    private static LoadedGame CargoWarning()
    {
        var g=ScoutingReadyTests.MultiWarning();var v=g.State.Vessels[0];
        v.RoleId="harl_settler";v.CarriedPopulation=1;v.Scout=null;v.PendingMove=Fixtures.HomeStar;
        return ScoutingFixtures.Valid(g);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void QuarantineCancelsCargoDepartureAndReconcilesItsExactConsentOnce(bool acknowledged)
    {
        var g=CargoWarning();var owner=g.State.Empires[0].Id;
        var previous=Assert.Single(ScoutQueries.Project(g.Content,g.State,owner).Attention);Assert.Equal("depart",previous.Action);
        if(acknowledged)g=ScoutingReadyTests.Acknowledge(g);
        string before=SaveGame.Serialize(g.Content,g.State);
        var preview=CrisisQueries.PreviewQuarantine(g.Content,g.State,owner,ImmutableArray.Create(g.State.Colonies[0].Id),true,false);Assert.True(preview.Allowed);
        var result=CommandProcessor.Execute(g.Content,g.State,new SetQuarantineCommand(owner,g.State.Turn,preview));
        Assert.True(result.Accepted,result.Rejection?.Message);Assert.Equal(before,SaveGame.Serialize(g.Content,g.State));
        Assert.True(result.State.Colonies[0].Crisis!.Quarantined);Assert.Null(result.State.Vessels[0].PendingMove);Assert.Empty(result.State.Empires[0].Scouting!.Receipts);
        var current=Assert.Single(ScoutQueries.Project(g.Content,result.State,owner).Attention);Assert.Equal("response_required",current.Action);Assert.Equal(previous.Revision+1,current.Revision);
        Assert.Empty(GameStateValidator.Validate(g.Content,result.State));
        var stale=CommandProcessor.Execute(g.Content,result.State,new AcknowledgeAmebaCommand(owner,g.State.Turn,ImmutableArray.Create(previous)));Assert.False(stale.Accepted);Assert.Same(result.State,stale.State);
    }
    [Fact] public void PurgingLastColonyReconcilesAnAcknowledgedAutomaticFallback()
    {
        var crisis=CrisisFixtures.Active(population:1);var g=ScoutingFixtures.AtAmeba();var s=g.State;s.Turn=crisis.State.Turn;
        s.Vessels[0].AmebaEncounter!.ArrivalTurn=s.Turn;s.Empires[0].Scouting!.KnownHabitats[0]=new(1,new(8),s.Turn);
        s.Empires[0].Crisis=crisis.State.Empires[0].Crisis!.Clone();s.Colonies[0].Crisis=crisis.State.Colonies[0].Crisis!.Clone();s.Colonies[0].Population=1;s.Colonies[0].Workforce=new(1,0,0);
        s.FindStar(new(6))!.X=301;s.FindStar(new(6))!.Y=1;s.Empires[0].Surveys.Add(ObservationUpdater.Observe(s,new(6),s.Turn));s.Empires[0].Surveys.Sort((a,b)=>a.StarId.CompareTo(b.StarId));
        ObservationUpdater.Refresh(s,s.Turn);g=ScoutingReadyTests.Acknowledge(ScoutingFixtures.Valid(g));var owner=s.Empires[0].Id;
        var preview=CrisisQueries.PreviewPurge(g.Content,g.State,owner,s.Colonies[0].Id);Assert.True(preview.Allowed);Assert.True(preview.ColonyLost);
        var result=CommandProcessor.Execute(g.Content,g.State,new PurgeColonyCommand(owner,s.Turn,preview));Assert.True(result.Accepted,result.Rejection?.Message);
        Assert.Empty(result.State.Colonies);Assert.Empty(result.State.Empires[0].Scouting!.Receipts);Assert.Equal(2,result.State.Vessels[0].AmebaEncounter!.Revision);Assert.Empty(GameStateValidator.Validate(g.Content,result.State));
    }
    [Fact] public void DelegatedQuarantineReconcilesButReadyRollsBackTheWholeChangedResponse()
    {
        var g=CargoWarning();var active=CrisisFixtures.Active();var s=g.State;s.Turn=active.State.Turn;
        s.Vessels[0].AmebaEncounter!.ArrivalTurn=s.Turn;s.Empires[0].Scouting!.KnownHabitats[0]=new(1,new(16),s.Turn);
        s.Empires[0].Crisis=active.State.Empires[0].Crisis!.Clone();s.Empires[0].Crisis!.Authority=CrisisAuthority.Contain;s.Colonies[0].Crisis=active.State.Colonies[0].Crisis!.Clone();
        ObservationUpdater.Refresh(s,s.Turn);g=ScoutingReadyTests.Acknowledge(ScoutingFixtures.Valid(g));var owner=s.Empires[0].Id;string before=SaveGame.Serialize(g.Content,g.State);
        var prepared=CrisisTurnCoordinator.Prepare(g.Content,g.State,owner);Assert.True(prepared.Succeeded);Assert.True(prepared.State.Colonies[0].Crisis!.Quarantined);Assert.Empty(prepared.State.Empires[0].Scouting!.Receipts);Assert.Equal(2,prepared.State.Vessels[0].AmebaEncounter!.Revision);
        var ready=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(owner,s.Turn,true));Assert.False(ready.Accepted);Assert.Same(g.State,ready.State);Assert.Equal(before,SaveGame.Serialize(g.Content,ready.State));Assert.False(ready.State.Colonies[0].Crisis!.Quarantined);Assert.Equal(0,ready.State.Empires[0].Scouting!.RiskRollCount);Assert.Equal(AmebaStage.Warning,ready.State.Vessels[0].AmebaEncounter!.Stage);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void AcceptedOffCargoSaveKeepsItsWaitingOrderRegardlessOfUnrelatedScoutCount(bool addScout)
    {
        var g=ScoutingFixtures.TwoHumansAndAi();var s=g.State;s.Scouting!.Enabled=false;s.Scouting.PlacementRandom=null;s.Scouting.Habitat=null;s.Scouting.SelectionCount=0;
        foreach(var empire in s.Empires)empire.Scouting!.RiskRandom=null;
        var cargo=s.Vessels[0];cargo.RoleId="harl_settler";cargo.CarriedPopulation=1;cargo.Scout=null;cargo.StationaryStarId=new(14);cargo.PendingMove=Fixtures.HomeStar;s.Colonies[0].Crisis!.Quarantined=true;
        if(addScout)s.Vessels.Add(new(){Id=new(s.AllocateId()),EmpireId=cargo.EmpireId,RoleId="scout",StationaryStarId=Fixtures.HomeStar,Scout=new(),Crisis=new()});
        ObservationUpdater.Refresh(s,s.Turn);g=ScoutingFixtures.Valid(g);var loaded=SaveGame.Deserialize(SaveGame.Serialize(g.Content,g.State));Assert.True(loaded.Succeeded);g=loaded.Game!;var owner=cargo.EmpireId;
        Assert.Empty(ScoutQueries.PreviewPreparation(g.Content,g.State,owner).Commands);
        var ready=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(owner,s.Turn,true));Assert.True(ready.Accepted,ready.Rejection?.Message);Assert.Equal(Fixtures.HomeStar,ready.State.FindVessel(cargo.Id)!.PendingMove);Assert.Equal(0,ready.State.Turn);
    }
    [Fact] public void RetreatRouteDisclosesOnlyOwnChartDurationFreshnessAndNeutralOverride()
    {
        var g=ScoutingReadyTests.MultiWarning();var s=g.State;var owner=s.Empires[0];var v=s.Vessels[0];v.Scout!.Posture=ScoutPosture.AvoidEncounters;
        s.Vessels.Add(new(){Id=new(s.AllocateId()),EmpireId=s.Empires[2].Id,RoleId="scout",StationaryStarId=Fixtures.HomeStar,Scout=new(),Crisis=new()});ObservationUpdater.Refresh(s,s.Turn);ContactUpdater.Refresh(s.Clone(),s);g=ScoutingFixtures.Valid(g);
        var row=ScoutQueries.Project(g.Content,s,owner.Id).Vessels.Single(r=>r.Id==v.Id);Assert.NotNull(row.DepartureRoute);Assert.Equal(Fixtures.HomeStar,row.DepartureRoute.Destination);Assert.Equal(s.FindStar(Fixtures.HomeStar)!.Name,row.DepartureRoute.Name);Assert.True(row.DepartureRoute.Duration>1);Assert.Equal(s.Turn,row.DepartureRoute.LastObservedTurn);Assert.True(row.DepartureRoute.SurvivalOverridesNeutralCaution);
        Assert.Contains("overrides neutral Avoid",Assert.Single(ScoutQueries.PreviewPreparation(g.Content,s,owner.Id).Outcomes.Where(e=>e.Kind=="retreat_planned")).Reason);
    }
}
