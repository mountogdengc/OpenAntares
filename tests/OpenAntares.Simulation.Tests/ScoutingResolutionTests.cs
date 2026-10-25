using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ScoutingResolutionTests
{
    [Fact] public void CurrentOwnPresenceRefreshesStaticHabitatDateWhileLostObservationRetainsPriorDate()
    {
        var g=Risk(ScoutingFixtures.WithNextDraw(ScoutingFixtures.AtAmeba(),74),AmebaRiskAction.Study);var result=Ready(g);Assert.True(result.Accepted,result.Rejection?.Message);Assert.Equal(11,result.State.Empires[0].Scouting!.KnownHabitats.Single().ObservedTurn);
        g=Risk(ScoutingFixtures.AtAmeba(),AmebaRiskAction.Remain);result=Ready(g);Assert.True(result.Accepted,result.Rejection?.Message);Assert.Equal(10,result.State.Empires[0].Scouting!.KnownHabitats.Single().ObservedTurn);
    }
    internal static LoadedGame Risk(LoadedGame g,AmebaRiskAction action)
    {var v=g.State.Vessels[0];var r=CommandProcessor.Execute(g.Content,g.State,new SetAmebaRiskOrderCommand(v.EmpireId,g.State.Turn,v.Id,v.AmebaEncounter!.Number,v.AmebaEncounter.Revision,action));Assert.True(r.Accepted,r.Rejection?.Message);return ScoutingReadyTests.Acknowledge(g with{State=r.State});}
    internal static CommandOutcome Ready(LoadedGame g)=>CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,g.State.Turn,true));
    [Fact] public void WarningDepartureIsCertainAndUsesOrdinaryTwoTurnTravelWithoutRiskDraw()
    {
        var g=ScoutingReadyTests.Acknowledge(ScoutingFixtures.AtAmeba());var e=g.State.Empires[0].Scouting!;ulong random=e.RiskRandom!.State;var r=Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);var v=Assert.Single(r.State.Vessels);Assert.Null(v.AmebaEncounter);Assert.Null(v.StationaryStarId);Assert.Equal(2,v.Transit!.TotalTurns);Assert.Equal(1,v.Transit.RemainingTurns);Assert.Equal(random,r.State.Empires[0].Scouting!.RiskRandom!.State);Assert.Equal(e.RiskRollCount,r.State.Empires[0].Scouting!.RiskRollCount);Assert.Equal(11,r.State.Turn);
    }
    [Theory][InlineData(74,true,300)][InlineData(75,false,300)][InlineData(49,true,200)][InlineData(50,false,200)]
    public void StudyThresholdUsesExactlyOneOwnerDraw(int draw,bool survives,int speed)
    {
        var g=Risk(ScoutingFixtures.WithNextDraw(ScoutingFixtures.AtAmeba(scoutSpeed:speed),draw),AmebaRiskAction.Study);long rolls=g.State.Empires[0].Scouting!.RiskRollCount;var expected=g.State.Empires[0].Scouting!.RiskRandom!.Clone();Assert.Equal(draw,expected.NextInt(100));var r=Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);Assert.Equal(rolls+1,r.State.Empires[0].Scouting!.RiskRollCount);Assert.Equal(expected.State,r.State.Empires[0].Scouting!.RiskRandom!.State);Assert.Equal(survives?600:0,r.State.Empires[0].ResearchReserve);
        if(survives){var v=Assert.Single(r.State.Vessels);Assert.Equal(AmebaStage.Exposed,v.AmebaEncounter!.Stage);Assert.Null(v.AmebaEncounter.RiskOrder);Assert.Null(v.Transit);Assert.Single(r.State.Empires[0].Scouting!.SuccessfulStudies);Assert.Empty(r.State.Empires[0].Scouting!.Receipts);}else Assert.Empty(r.State.Vessels);
    }
    [Theory][InlineData(94,true)][InlineData(95,false)]
    public void ExposedEscapeHasOneDrawAndRetainsPriorResearchOnLoss(int draw,bool survives)
    {
        var g=ScoutingFixtures.WithNextDraw(ScoutingFixtures.AtAmeba(AmebaStage.Exposed),draw);g.State.Empires[0].ResearchReserve=600;g=ScoutingReadyTests.Acknowledge(g);long rolls=g.State.Empires[0].Scouting!.RiskRollCount;var r=Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);Assert.Equal(rolls+1,r.State.Empires[0].Scouting!.RiskRollCount);Assert.Equal(600,r.State.Empires[0].ResearchReserve);Assert.Equal(survives?1:0,r.State.Vessels.Count);if(survives)Assert.NotNull(r.State.Vessels[0].Transit);
    }
    [Fact] public void StudyRewardCannotCompleteCurrentTechnologyButEntersNextNormalInterval()
    {
        var g=ScoutingFixtures.WithNextDraw(ScoutingFixtures.AtAmeba(),74);g.State.Empires[0].ResearchReserve=1199;g.State.Empires[0].ResearchTargetId="cultivation_methods";g=Risk(g,AmebaRiskAction.Study);var r=Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);Assert.Equal(1799,r.State.Empires[0].ResearchReserve);Assert.DoesNotContain("cultivation_methods",r.State.Empires[0].KnownTechnologyIds);
        g=ScoutingFixtures.WithNextDraw(g with{State=r.State},94);g=ScoutingReadyTests.Acknowledge(g);r=Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);Assert.Contains("cultivation_methods",r.State.Empires[0].KnownTechnologyIds);Assert.Equal(599,r.State.Empires[0].ResearchReserve);
    }
    [Fact] public void StudyRewardDoesNotFundCurrentCureAndConsumedFlaggedCargoCannotExpose()
    {
        var g=ScoutingFixtures.AtAmeba();var plague=CrisisFixtures.Active();g.State.Empires[0].Crisis=plague.State.Empires[0].Crisis!.Clone();g.State.Colonies[0].Crisis=plague.State.Colonies[0].Crisis!.Clone();g.State.Empires[0].Crisis!.Episode!.CureProgress=1199;g.State.Empires[0].Crisis!.Episode!.ResearchShare=50;
        g=Risk(ScoutingFixtures.WithNextDraw(ScoutingFixtures.Valid(g),74),AmebaRiskAction.Study);var r=Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);Assert.Equal(1199,r.State.Empires[0].Crisis!.Episode!.CureProgress);Assert.Equal(600,r.State.Empires[0].ResearchReserve);
        g=ScoutingFixtures.AtAmeba();g.State.Empires[0].Crisis=plague.State.Empires[0].Crisis!.Clone();g.State.Colonies[0].Crisis=plague.State.Colonies[0].Crisis!.Clone();var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.Scout=null;v.Crisis!.CarrierEpisode=1;g=Risk(ScoutingFixtures.Valid(g),AmebaRiskAction.Remain);ulong random=g.State.Empires[0].Scouting!.RiskRandom!.State;r=Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);Assert.Empty(r.State.Vessels);Assert.Equal(random,r.State.Empires[0].Scouting!.RiskRandom!.State);Assert.DoesNotContain(r.Report!.CrisisEvents,e=>e.Kind=="exposure");
    }
    [Fact] public void ArrivalsReceiveIndependentFullWarningsAndDoNotEscapeTwice()
    {
        var g=ScoutingFixtures.Game();var s=g.State;var v=s.Vessels[0];v.PendingMove=new(8);var second=new VesselState{Id=new(s.AllocateId()),EmpireId=v.EmpireId,RoleId="scout",Transit=new(Fixtures.HomeStar,new(8),2,1),Scout=new(),Crisis=new()};s.Vessels.Add(second);g=ScoutingFixtures.Valid(g);
        var r=Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);Assert.Null(r.State.FindVessel(v.Id)!.AmebaEncounter);Assert.Equal(AmebaStage.Warning,r.State.FindVessel(second.Id)!.AmebaEncounter!.Stage);Assert.Equal(1,r.State.FindVessel(second.Id)!.AmebaEncounter!.ArrivalTurn);
        g=ScoutingReadyTests.Acknowledge(g with{State=r.State});r=Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);Assert.Equal(2,r.State.FindVessel(v.Id)!.AmebaEncounter!.ArrivalTurn);Assert.Equal(AmebaStage.Warning,r.State.FindVessel(v.Id)!.AmebaEncounter!.Stage);Assert.NotNull(r.State.FindVessel(second.Id)!.Transit);Assert.Equal(0,r.State.Empires[0].Scouting!.RiskRollCount);
    }
    [Fact] public void EveryFaultPhaseAndRewardOverflowRestoreEnclosingState()
    {
        var g=Risk(ScoutingFixtures.WithNextDraw(ScoutingFixtures.AtAmeba(),74),AmebaRiskAction.Study);string before=ScoutingFixtures.Bytes(g.State);
        foreach(var phase in Enum.GetValues<ScoutResolutionPhase>()){bool reached=false;var r=TurnResolver.ResolveScouting(g.Content,g.State,p=>{if(p==phase){reached=true;throw new InvalidOperationException("labelled phase fault");}});Assert.True(reached,phase.ToString());Assert.False(r.Succeeded);Assert.Same(g.State,r.State);Assert.Equal(before,ScoutingFixtures.Bytes(r.State));}
        g.State.Empires[0].ResearchReserve=long.MaxValue-100;var overflow=Ready(g);Assert.False(overflow.Accepted);Assert.Same(g.State,overflow.State);
    }
    [Theory][InlineData(0,false)][InlineData(100,true)]
    public void EndpointOddsStillConsumeOneDraw(int percent,bool survives)
    {
        var g=ScoutingFixtures.AtAmeba();var d=g.Content.SpaceAmeba!;g=g with{Content=g.Content with{SpaceAmeba=d with{Study=new(percent,0,percent,percent)}}};g=Risk(g,AmebaRiskAction.Study);var r=Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);Assert.Equal(1,r.State.Empires[0].Scouting!.RiskRollCount);Assert.Equal(survives?1:0,r.State.Vessels.Count);
    }
    [Fact] public void EachEmpireCanClaimOnceUsingOnlyItsOwnRiskStream()
    {
        var g=ScoutingReadyTests.MultiWarning();var s=g.State;var other=s.Empires[1];var v=new VesselState{Id=new(s.AllocateId()),EmpireId=other.Id,RoleId="scout",StationaryStarId=new(16),Scout=new(),Crisis=new(),AmebaEncounter=new(){Number=1,HabitatId=1}};s.Vessels.Add(v);other.Scouting!.NextEncounterNumber=2;other.Scouting.KnownHabitats.Add(new(1,new(16),0));ObservationUpdater.Refresh(s,0);ContactUpdater.Refresh(s.Clone(),s);g=ScoutingFixtures.Valid(g);ulong aiRandom=s.Empires[2].Scouting!.RiskRandom!.State;
        foreach(var owner in s.Empires.Where(e=>e.Controller==ControllerKind.Human))
        {
            var unit=g.State.Vessels.Single(vessel=>vessel.EmpireId==owner.Id);var risk=CommandProcessor.Execute(g.Content,g.State,new SetAmebaRiskOrderCommand(owner.Id,0,unit.Id,1,1,AmebaRiskAction.Study));Assert.True(risk.Accepted);g=g with{State=risk.State};var ack=CommandProcessor.Execute(g.Content,g.State,new AcknowledgeAmebaCommand(owner.Id,0,ScoutQueries.Project(g.Content,g.State,owner.Id).Attention));Assert.True(ack.Accepted);g=g with{State=ack.State};var ready=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(owner.Id,0,true));Assert.True(ready.Accepted,ready.Rejection?.Message);g=g with{State=ready.State};
        }
        Assert.Equal(1,g.State.Turn);foreach(var owner in g.State.Empires.Where(e=>e.Controller==ControllerKind.Human)){Assert.Single(owner.Scouting!.SuccessfulStudies);Assert.Equal(1,owner.Scouting.RiskRollCount);}Assert.Equal(aiRandom,g.State.Empires[2].Scouting!.RiskRandom!.State);
    }
    [Fact] public void SupposedDepartureBlockedDownstreamFailsInsteadOfGrantingStationarySafety()
    {
        var g=ScoutingFixtures.AtAmeba();var s=g.State;var v=s.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.Scout=null;v.PendingMove=Fixtures.HomeStar;g=ScoutingReadyTests.Acknowledge(ScoutingFixtures.Valid(g));s=g.State;var candidate=s.Clone();var context=AmebaResolutionContext.Capture(g.Content,s);AmebaResolver.ResolveExisting(g.Content,s,candidate,context);
        var closed=s.Clone();closed.Colonies[0].Crisis!.Quarantined=true;var health=CrisisResolutionContext.Capture(g.Content,closed);OpenAntares.Simulation.Movement.MovementResolver.Apply(g.Content,s,candidate,new(),health,context);Assert.Throws<InvalidOperationException>(()=>AmebaResolver.ValidateDepartures(s,candidate,context));Assert.Equal(AmebaStage.Warning,s.Vessels[0].AmebaEncounter!.Stage);
    }
}
