using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatIntegrationTests
{
    // Reachable stream-state and pre-session appointment supplements, not earned/generated claims.
    static LoadedGame Loss(int draw=49,string species="harl",bool researched=false,OfficerDoctrine doctrine=OfficerDoctrine.RecoverCommand,bool journal=true,bool amebas=false)
    {
        var g=CombatFixtures.Game(species);if(amebas){var made=OpenAntares.Simulation.Galaxy.GalaxyGenerator.CreateNewGame(g.Content,new("small",42,9,species,CrisesEnabled:false,AmebasEnabled:true,SentriesEnabled:true));Assert.True(made.Succeeded,made.Error);g=new(g.Content,made.State!);}var s=g.State;s.Turn=10;var e=s.Empires[0];var ship=s.Vessels[0];ship.RoleId="escort";ship.StationaryStarId=s.Combat!.Pockets[0].Star;CombatInitializer.InitializeVessel(g.Content,9,ship);ship.Combat!.Hull=1;ship.Fleet=new(1);var f=e.Fleets!;var officer=f.Officers[0];officer.Status=OfficerStatus.Commanding;officer.PostedColony=null;officer.Flagship=ship.Id;officer.Doctrine=doctrine;officer.Xp=10;officer.Rank=1;f.Fleets.Add(new(){Id=new(1),Name="Loss fixture",Members=new(){ship.Id},Flagship=ship.Id,Officer=officer.Id});f.NextFleetId=2;
        if(researched)Know(g,true);int count=0;while(f.RecoveryRandom!.Clone().NextInt(100)!=draw){Assert.True(++count<10000);f.RecoveryRandom.NextInt(100);f.RecoveryRollCount++;}
        ObservationUpdater.Refresh(s,s.Turn);var pocket=s.Combat.Pockets[0];e.Combat!.Observations.Add(new(pocket.Id,pocket.Star,s.Turn,false,pocket.Sentries.Select(a=>new ObservedSentry(a.Id,a.Profile,a.Hull,g.Content.Combat!.SentryPursuitSpeed)).ToImmutableArray()));CombatFixtures.Valid(g);
        if(journal){var command=CommandProcessor.Execute(g.Content,s,new SetScoutPostureCommand(e.Id,s.Turn,ship.Id,ScoutPosture.AvoidEncounters));Assert.True(command.Accepted,command.Rejection?.Message);g=new(g.Content,command.State);}
        return new(g.Content,CombatCommandTests.Commit(g,CombatCommandTests.Attack(g.State)));
    }
    static void Know(LoadedGame g,bool recovery)
    {
        var e=g.State.Empires[0];foreach(string id in new[]{"measurement_methods","industrial_coordination"}.Concat(recovery?new[]{g.Content.FleetCommand!.RecoveryTechnologyId}:Array.Empty<string>()))if(!e.KnownTechnologyIds.Contains(id))e.KnownTechnologyIds.Add(id);e.KnownTechnologyIds.Sort(StringComparer.Ordinal);
    }
    [Theory] [InlineData(49,false,true)] [InlineData(50,false,false)] [InlineData(69,true,true)] [InlineData(70,true,false)]
    public void FlagshipRecoveryUsesTheExistingExactDrawAndThreshold(int draw,bool researched,bool success)
    {
        var g=Loss(draw,researched:researched);var before=g.State.Empires[0].Fleets!;var random=before.RecoveryRandom!.Clone();Assert.Equal(draw,random.NextInt(100));var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var after=result.State.Empires[0].Fleets!;var officer=after.Officers[0];Assert.Equal(success?OfficerStatus.Recovering:OfficerStatus.Dead,officer.Status);Assert.Equal(researched?70:50,officer.RecoveryPercentAtLoss);Assert.Equal(before.RecoveryRollCount+1,after.RecoveryRollCount);Assert.Equal(random.State,after.RecoveryRandom!.State);Assert.Equal(10,officer.Xp);Assert.Equal(1,officer.Career.ShipsLost);Assert.Equal(1,officer.Career.FlagshipsLost);Assert.Equal(g.State.Random.State,result.State.Random.State);
    }
    [Fact] public void SuccessfulMortalRecoveryWaitsTwoFullSubsequentTurns()
    {
        var g=Loss();var first=TurnResolver.Resolve(g.Content,g.State);Assert.True(first.Succeeded,string.Join(";",first.Errors));var officer=first.State.Empires[0].Fleets!.Officers[0];Assert.Equal(11,officer.RecoveryStartedTurn);Assert.Equal(13,officer.AvailableAtTurn);var second=TurnResolver.Resolve(g.Content,first.State);Assert.True(second.Succeeded,string.Join(";",second.Errors));Assert.Equal(OfficerStatus.Recovering,second.State.Empires[0].Fleets!.Officers[0].Status);var third=TurnResolver.Resolve(g.Content,second.State);Assert.True(third.Succeeded,string.Join(";",third.Errors));Assert.Equal(OfficerStatus.Posted,third.State.Empires[0].Fleets!.Officers[0].Status);Assert.Equal(1,third.State.Empires[0].Fleets!.Officers[0].Career.Recoveries);
    }
    [Fact] public void SameBoundaryResearchCannotReplaceSnapshotRecoveryEquipment()
    {
        var g=Loss(60);Know(g,false);var e=g.State.Empires[0];e.ResearchTargetId=g.Content.FleetCommand!.RecoveryTechnologyId;e.ResearchReserve=g.Content.Technologies[e.ResearchTargetId].Cost;var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));Assert.Contains(e.ResearchTargetId,result.State.Empires[0].KnownTechnologyIds);var officer=result.State.Empires[0].Fleets!.Officers[0];Assert.Equal(50,officer.RecoveryPercentAtLoss);Assert.Equal(OfficerStatus.Dead,officer.Status);
    }
    [Fact] public void MortalLastWatchConsumesNoRecoveryDraw()
    {
        var g=Loss(doctrine:OfficerDoctrine.LastWatch);var f=g.State.Empires[0].Fleets!;var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var after=result.State.Empires[0].Fleets!;Assert.Equal(f.RecoveryRandom!.State,after.RecoveryRandom!.State);Assert.Equal(f.RecoveryRollCount,after.RecoveryRollCount);Assert.Equal(0,after.Officers[0].RecoveryPercentAtLoss);Assert.Equal(OfficerStatus.Dead,after.Officers[0].Status);
    }
    [Theory] [InlineData(false,OfficerDoctrine.RecoverCommand,2)] [InlineData(true,OfficerDoctrine.RecoverCommand,1)] [InlineData(false,OfficerDoctrine.LastWatch,4)] [InlineData(true,OfficerDoctrine.LastWatch,3)]
    public void WenthKeepsWholeBodyIdentityAndExistingLinkClock(bool researched,OfficerDoctrine doctrine,int delay)
    {
        var g=Loss(species:"wenth",researched:researched,doctrine:doctrine);var before=g.State.Empires[0].Fleets!;var body=before.Officers[0].SourceBody;var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var after=result.State.Empires[0].Fleets!;var officer=after.Officers[0];Assert.Equal(OfficerStatus.Recovering,officer.Status);Assert.Equal(body,officer.SourceBody);Assert.Equal(body,officer.RecoveryColony);Assert.Equal(delay,officer.AvailableAtTurn-officer.RecoveryStartedTurn);Assert.Equal(10,officer.Xp);Assert.Equal(before.RecoveryRandom!.State,after.RecoveryRandom!.State);Assert.Equal(before.RecoveryRollCount,after.RecoveryRollCount);
    }
    [Fact] public void FlagshipCargoAndRequestLossOnce()
    {
        var g=CombatWithdrawalTests.Queued(CombatWithdrawalTests.Battle(new[]{25,15},new[]{false,true},flagship:1,navigator:1),StragglerPolicy.DetachCrippled);var owner=g.State.Empires[0];int loop=0;while(owner.Fleets!.RecoveryRandom!.Clone().NextInt(100)!=49){Assert.True(++loop<10000);owner.Fleets.RecoveryRandom.NextInt(100);owner.Fleets.RecoveryRollCount++;}
        var cargo=g.State.Vessels[0].Id;var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));Assert.Empty(result.State.Empires[0].Fleets!.Fleets);Assert.Null(result.State.FindVessel(cargo)!.Fleet);var officer=result.State.Empires[0].Fleets!.Officers[0];Assert.Null(officer.Flagship);Assert.Equal(OfficerStatus.Recovering,officer.Status);Assert.Equal(1,officer.Career.ShipsLost);Assert.Equal(1,officer.Career.FlagshipsLost);Assert.Equal(10,officer.Xp);
    }
    [Fact] public void ReservedHealthLossBecomesMissingWithoutReplacementAndAdapterIsIdempotent()
    {
        var g=CombatFixtures.Game(enabled:false);var ship=g.State.Vessels[0];var draft=FleetPlanningTests.Draft(g,1) with {AcceptedIdleVessels=ImmutableArray.Create(ship.Id)};g=FleetRequestTests.Accept(g,draft);var snapshot=g.State;var candidate=snapshot.Clone();candidate.Vessels.RemoveAll(v=>v.Id==ship.Id);var context=CombatResolutionContext.Capture(g.Content,snapshot);
        CombatLossIntegration.Apply(g.Content,snapshot,candidate,context.FleetContext,context);var request=Assert.Single(candidate.Empires[0].Fleets!.Requests);Assert.Equal(SlotStatus.Missing,request.Slots[0].Status);Assert.Equal(ship.Id,request.Slots[0].FormerVessel);Assert.Null(request.Slots[0].Vessel);Assert.Null(request.Slots[0].QueueToken);string before=CombatFixtures.Bytes(candidate);CombatLossIntegration.Apply(g.Content,snapshot,candidate,context.FleetContext,context);Assert.Equal(before,CombatFixtures.Bytes(candidate));Assert.Empty(candidate.Colonies[0].Development!.Queue);Assert.Empty(candidate.Empires[0].Fleets!.HullCredits);
    }
    [Fact] public void HealthLossOfLastWarshipCannotResumeAnUnarmedAttack()
    {
        var g=CombatWithdrawalTests.Battle(new[]{100,100},new[]{true,false});var first=g.State.Clone();var firstContext=CombatResolutionContext.Capture(g.Content,g.State);CombatResolver.ResolveExchanges(g.Content,g.State,first,firstContext);first.Turn++;ObservationUpdater.Refresh(first,first.Turn);CombatTurnCoordinator.RefreshKnownObservations(g.Content,first,firstContext);
        // Explicit generic precombat-loss supplement; no claim that this core pocket overlaps an ameba.
        var context=CombatResolutionContext.Capture(g.Content,first);var candidate=first.Clone();candidate.Vessels.RemoveAll(v=>v.Id==first.Vessels[0].Id);CombatResolver.ResolveExchanges(g.Content,first,candidate,context);candidate.Turn++;ObservationUpdater.Refresh(candidate,candidate.Turn);CombatTurnCoordinator.RefreshKnownObservations(g.Content,candidate,context);g=CombatFixtures.Valid(new(g.Content,candidate));var e=Assert.Single(candidate.Combat!.Encounters);
        var accepted=CombatCommandTests.Commit(g,new CombatAction.RespondCombat(e.Id,e.Revision,CombatResponse.AcceptRevisedForce));Assert.Equal(CombatPhase.WithdrawalQueued,accepted.Combat!.Encounters[0].Phase);var changed=CombatCommandTests.Commit(g,new CombatAction.ChangeCombatOrders(e.Id,e.Revision,e.Orders with {Doctrine=CombatDoctrine.Hold},true));Assert.Equal(CombatPhase.WithdrawalQueued,changed.Combat!.Encounters[0].Phase);var result=TurnResolver.Resolve(g.Content,accepted);Assert.True(result.Succeeded,string.Join(";",result.Errors));Assert.Empty(result.State.Combat!.Encounters);Assert.Equal(first.Combat!.Pockets[0].Sentries[0].Hull,result.State.Combat.Pockets[0].Sentries[0].Hull);
    }
    [Fact] public void MixedAmebaAndCombatLossRetainsSeparateFactsAndOneOfficerRoll()
    {
        var g=Loss(amebas:true);var s=g.State;var owner=s.Empires[0];var home=s.FindPlanet(s.Colonies[0].PlanetId)!.StarId;var scout=new VesselState{Id=new(s.AllocateId()),EmpireId=owner.Id,RoleId="scout",StationaryStarId=home,Crisis=new()};ScoutInitializer.InitializeVessel(g.Content,9,scout);CombatInitializer.InitializeVessel(g.Content,9,scout);s.Vessels.Add(scout);var draft=FleetPlanningTests.Draft(g,1) with {AcceptedIdleVessels=ImmutableArray.Create(scout.Id)};g=FleetRequestTests.Accept(g,draft);s=g.State;owner=s.Empires[0];scout=s.FindVessel(scout.Id)!;var habitat=s.Scouting!.Habitat!;scout.StationaryStarId=habitat.Star;scout.AmebaEncounter=new(){Number=1,HabitatId=habitat.Id,ArrivalTurn=s.Turn,Stage=AmebaStage.Warning};owner.Scouting!.KnownHabitats.Add(new(habitat.Id,habitat.Star,s.Turn));owner.Scouting.NextEncounterNumber=2;ObservationUpdater.Refresh(s,s.Turn);CombatFixtures.Valid(g);
        var remain=CommandProcessor.Execute(g.Content,s,new SetAmebaRiskOrderCommand(owner.Id,s.Turn,scout.Id,1,1,AmebaRiskAction.Remain));Assert.True(remain.Accepted,remain.Rejection?.Message);var attention=ScoutQueries.Project(g.Content,remain.State,owner.Id).Attention;var acknowledged=CommandProcessor.Execute(g.Content,remain.State,new AcknowledgeAmebaCommand(owner.Id,s.Turn,attention));Assert.True(acknowledged.Accepted,acknowledged.Rejection?.Message);var source=acknowledged.State;long rolls=source.Empires[0].Fleets!.RecoveryRollCount;var expected=source.Empires[0].Fleets!.RecoveryRandom!.Clone();expected.NextInt(100);ulong risk=source.Empires[0].Scouting!.RiskRandom!.State;long riskRolls=source.Empires[0].Scouting!.RiskRollCount;
        var result=TurnResolver.Resolve(g.Content,source);Assert.True(result.Succeeded,string.Join(";",result.Errors));Assert.Empty(result.State.Vessels);var request=Assert.Single(result.State.Empires[0].Fleets!.Requests);Assert.Equal(SlotStatus.Missing,request.Slots[0].Status);Assert.Equal(scout.Id,request.Slots[0].FormerVessel);Assert.Null(request.Slots[0].Vessel);Assert.Empty(result.State.Colonies[0].Development!.Queue);Assert.Equal(rolls+1,result.State.Empires[0].Fleets!.RecoveryRollCount);Assert.Equal(expected.State,result.State.Empires[0].Fleets!.RecoveryRandom!.State);Assert.Equal(risk,result.State.Empires[0].Scouting!.RiskRandom!.State);Assert.Equal(riskRolls,result.State.Empires[0].Scouting!.RiskRollCount);Assert.Single(result.Report!.ScoutEvents,e=>e.Kind=="loss");Assert.Equal(1,result.State.Empires[0].Combat!.LifetimeShipsLost);Assert.Equal(1,result.State.Empires[0].Fleets!.Officers[0].Career.ShipsLost);
    }
    [Fact] public void DestroyedCarrierLosesEmbarkedPopulationWithoutSecondSourceChargeOrCure()
    {
        var g=CombatWithdrawalTests.Battle(new[]{1,1},new[]{false,true},flagship:0,crises:true);var s=g.State;var source=s.Colonies[0];var e=s.Empires[0].Crisis!;var d=g.Content.Plagues.Values.Single();e.NextEpisodeNumber=2;e.RequireAcknowledgement=false;e.Episode=new(){Id=1,FamilyId=d.Id,Source=CrisisQueries.Location(s,source),SourcePopulation=source.Population,CureCost=checked(d.CureBase+d.CurePerPopulation*source.Population)};s.Vessels[0].Crisis!.CarrierEpisode=1;CombatFixtures.Valid(g);int passengers=s.Vessels[0].CarriedPopulation;var first=TurnResolver.Resolve(g.Content,s);Assert.True(first.Succeeded,string.Join(";",first.Errors));var survivors=first.State.Combat!.Encounters[0];var confirmed=CombatCommandTests.Commit(new(g.Content,first.State),new CombatAction.WithdrawCombat(survivors.Id,survivors.Revision));var second=TurnResolver.Resolve(g.Content,confirmed);Assert.True(second.Succeeded,string.Join(";",second.Errors));Assert.Empty(second.State.Vessels);Assert.Equal(passengers,second.State.Empires[0].Combat!.LifetimeCargoLost);Assert.Equal(second.Report!.Economy.Colony(source.Id).Growth.PopulationAfter,second.State.FindColony(source.Id)!.Population);Assert.DoesNotContain(second.Report.CrisisEvents,ev=>ev.Kind=="cure_complete");
    }
}
