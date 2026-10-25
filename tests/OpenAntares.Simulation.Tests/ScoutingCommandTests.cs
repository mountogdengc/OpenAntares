using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ScoutingCommandTests
{
    private static CommandOutcome Run(LoadedGame g,Command c)=>CommandProcessor.Execute(g.Content,g.State,c);
    private static void Reject(LoadedGame g,Command c,string code){var r=Run(g,c);Assert.False(r.Accepted);Assert.False(r.Changed);Assert.Same(g.State,r.State);Assert.Equal(code,r.Rejection!.Code);}
    [Fact] public void StudyReplacesMoveBindsNewRevisionAndReservesOnlyThisEmpire()
    {
        var g=ScoutingFixtures.WithSecondScout();var owner=g.State.Empires[0].Id;var v=g.State.Vessels[0];v.PendingMove=Fixtures.HomeStar;string before=ScoutingFixtures.Bytes(g.State);
        var r=Run(g,new SetAmebaRiskOrderCommand(owner,10,v.Id,1,1,AmebaRiskAction.Study));Assert.True(r.Accepted);Assert.True(r.Changed);Assert.Equal(before,ScoutingFixtures.Bytes(g.State));
        var after=r.State.FindVessel(v.Id)!;Assert.Null(after.PendingMove);Assert.Equal(2,after.AmebaEncounter!.Revision);Assert.Equal(2,after.AmebaEncounter.RiskOrder!.Revision);Assert.Equal(0,r.State.Empires[0].Scouting!.RiskRollCount);
        g=g with{State=r.State};Reject(g,new SetAmebaRiskOrderCommand(owner,10,g.State.Vessels[1].Id,2,1,AmebaRiskAction.Study),"ameba_study_reserved");
        r=Run(g,new SetAmebaRiskOrderCommand(owner,10,v.Id,1,2,null));Assert.True(r.Accepted);g=g with{State=r.State};Assert.True(Run(g,new SetAmebaRiskOrderCommand(owner,10,g.State.Vessels[1].Id,2,1,AmebaRiskAction.Study)).Accepted);
    }
    [Fact] public void GroupAcknowledgementMustMatchEveryCurrentTokenExactly()
    {
        var g=ScoutingFixtures.WithSecondScout();var owner=g.State.Empires[0].Id;var tokens=ScoutQueries.Project(g.Content,g.State,owner).Attention;Assert.Equal(2,tokens.Length);
        Reject(g,new AcknowledgeAmebaCommand(owner,10,tokens.RemoveAt(0)),"ameba_encounter_stale");
        Reject(g,new AcknowledgeAmebaCommand(owner,10,tokens.Add(tokens[0])),"ameba_encounter_stale");
        Reject(g,new AcknowledgeAmebaCommand(owner,10,tokens.SetItem(0,tokens[0] with{Vessel=new(999)})),"ameba_encounter_stale");
        Reject(g,new AcknowledgeAmebaCommand(owner,10,tokens.SetItem(0,tokens[0] with{Percent=0})),"ameba_encounter_stale");
        var r=Run(g,new AcknowledgeAmebaCommand(owner,10,tokens));Assert.True(r.Changed);Assert.Equal(2,r.State.Empires[0].Scouting!.Receipts.Count);g=g with{State=r.State};var no=Run(g,new AcknowledgeAmebaCommand(owner,10,tokens));Assert.True(no.Accepted);Assert.False(no.Changed);Assert.Same(g.State,no.State);
    }
    [Fact] public void PostureIsOptInCancelsExcludedPendingApproachAndNoOpsKeepReady()
    {
        var g=ScoutingFixtures.Game();var v=g.State.Vessels[0];var owner=g.State.Empires[0];owner.Ready=true;
        var no=Run(g,new SetScoutPostureCommand(owner.Id,0,v.Id,ScoutPosture.Observe));Assert.True(no.Accepted);Assert.Same(g.State,no.State);Assert.True(no.State.Empires[0].Ready);
        owner.Ready=false;owner.Scouting!.KnownHabitats.Add(new(1,new(8),0));v.PendingMove=new(8);v.Scout!.ApproachAcceptance=new(new(8),1);
        var r=Run(g,new SetScoutPostureCommand(owner.Id,0,v.Id,ScoutPosture.AvoidEncounters));Assert.True(r.Changed);Assert.Null(r.State.Vessels[0].PendingMove);Assert.Null(r.State.Vessels[0].Scout!.ApproachAcceptance);Assert.Equal(ScoutPosture.AvoidEncounters,r.State.Vessels[0].Scout!.Posture);
    }
    [Fact] public void OrdinaryUnknownApproachIsLegalKnownApproachRequiresExplicitObserveAcceptance()
    {
        var g=ScoutingFixtures.Game();var owner=g.State.Empires[0].Id;var v=g.State.Vessels[0];Assert.True(Run(g,new SetMoveOrderCommand(owner,0,v.Id,new(8))).Accepted);
        g.State.Empires[0].Scouting!.KnownHabitats.Add(new(1,new(8),0));Reject(g,new SetMoveOrderCommand(owner,0,v.Id,new(8)),"ameba_approach_confirmation_required");
        var r=Run(g,new SetAmebaApproachOrderCommand(owner,0,v.Id,new(8),1));Assert.True(r.Changed);Assert.Equal(new StarId(8),r.State.Vessels[0].PendingMove);Assert.NotNull(r.State.Vessels[0].Scout!.ApproachAcceptance);
        g=g with{State=r.State};r=Run(g,new SetMoveOrderCommand(owner,0,v.Id,null));Assert.Null(r.State.Vessels[0].Scout!.ApproachAcceptance);
        g.State.Vessels[0].Scout!.Posture=ScoutPosture.AvoidEncounters;Reject(g,new SetAmebaApproachOrderCommand(owner,0,v.Id,new(8),1),"scout_destination_excluded");
    }
    [Fact] public void CargoCanRemainButCannotStudyApproachOrFoundAtKnownHabitat()
    {
        var g=ScoutingFixtures.AtAmeba();var v=g.State.Vessels[0];var owner=g.State.Empires[0].Id;v.RoleId="harl_settler";v.CarriedPopulation=1;v.Scout=null;
        Reject(g,new SetAmebaRiskOrderCommand(owner,10,v.Id,1,1,AmebaRiskAction.Study),"scout_not_eligible");Reject(g,new SetFoundingOrderCommand(owner,10,v.Id,new(9)),"ameba_habitat_guarded");
        var r=Run(g,new SetAmebaRiskOrderCommand(owner,10,v.Id,1,1,AmebaRiskAction.Remain));Assert.True(r.Changed);Assert.Equal("remain",ScoutQueries.Project(g.Content,r.State,owner).Attention[0].Action);Assert.Equal(0,ScoutQueries.Project(g.Content,r.State,owner).Attention[0].Percent);
        g=g with{State=r.State};r=Run(g,new SetMoveOrderCommand(owner,10,v.Id,Fixtures.HomeStar));Assert.True(r.Accepted);Assert.Null(r.State.Vessels[0].AmebaEncounter!.RiskOrder);
    }
    [Fact] public void ExposedOrRewardedStudyIsRejectedWithoutSpendingOrRolling()
    {
        var g=ScoutingFixtures.AtAmeba(AmebaStage.Exposed);Reject(g,new SetAmebaRiskOrderCommand(g.State.Empires[0].Id,10,g.State.Vessels[0].Id,1,1,AmebaRiskAction.Study),"ameba_study_already_rewarded");
    }
    [Fact] public void StaleForeignTransitAndMissingTargetsFailBeforeHazardChecks()
    {
        var g=ScoutingFixtures.AtAmeba();var owner=g.State.Empires[0].Id;var v=g.State.Vessels[0];
        Reject(g,new SetScoutPostureCommand(owner,9,v.Id,ScoutPosture.AvoidEncounters),RejectionCodes.StaleTurn);Reject(g,new SetAmebaRiskOrderCommand(owner,10,new(999),1,1,AmebaRiskAction.Remain),RejectionCodes.NotOwner);
        Reject(g,new SetAmebaApproachOrderCommand(owner,10,v.Id,new(999),1),"unknown_star");Reject(g,new SetAmebaRiskOrderCommand(owner,10,v.Id,1,999,AmebaRiskAction.Remain),"ameba_encounter_stale");
        v.AmebaEncounter=null;v.StationaryStarId=null;v.Transit=new(new(8),Fixtures.HomeStar,2,1);Reject(g,new SetAmebaRiskOrderCommand(owner,10,v.Id,1,1,AmebaRiskAction.Remain),"vessel_in_transit");
        Assert.True(Run(g,new SetScoutPostureCommand(owner,10,v.Id,ScoutPosture.AvoidEncounters)).Accepted);
    }
    [Fact] public void AvoidRejectsKnownForeignColonyButObserveAllowsColocation()
    {
        var g=ScoutingFixtures.TwoHumansAndAi();var owner=g.State.Empires[0];var v=g.State.Vessels[0];owner.Surveys.Add(ObservationUpdater.Observe(g.State,new(6),0));owner.Surveys.Sort((a,b)=>a.StarId.CompareTo(b.StarId));
        Assert.True(Run(g,new SetMoveOrderCommand(owner.Id,0,v.Id,new(6))).Accepted);v.Scout!.Posture=ScoutPosture.AvoidEncounters;Reject(g,new SetMoveOrderCommand(owner.Id,0,v.Id,new(6)),"scout_destination_excluded");
    }
}
