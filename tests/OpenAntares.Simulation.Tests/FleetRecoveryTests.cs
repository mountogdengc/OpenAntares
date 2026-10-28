using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetRecoveryTests
{
    // Labelled reachable stream-state supplements, never generated rare-event campaign evidence.
    internal static Saves.LoadedGame Loss(int draw=49,string species="harl",bool researched=false,OfficerDoctrine doctrine=OfficerDoctrine.RecoverCommand,bool cargo=false,bool crises=false)
    {
        var g=FleetFixtures.Game(species,amebas:true);if(crises){var madeGame=Galaxy.GalaxyGenerator.CreateNewGame(g.Content,new("small",42,8,species,CrisesEnabled:true,AmebasEnabled:true));Assert.True(madeGame.Succeeded);g=new(g.Content,madeGame.State!);}var v=g.State.Vessels[0];if(cargo){v.RoleId="harl_settler";v.CarriedPopulation=1;v.Scout=null;}
        var made=FleetCommandTests.Run(g,new FleetAction.Create("Flagship",ImmutableArray.Create(v.Id)));Assert.True(made.Accepted);g=new(g.Content,made.State);var assigned=FleetOfficerTests.Run(g,new OfficerAction.AssignFlagship(new(1),new(1)));Assert.True(assigned.Accepted);g=new(g.Content,assigned.State);
        var s=g.State;s.Turn=10;var e=s.Empires[0];var f=e.Fleets!;var officer=f.Officers[0];officer.Doctrine=doctrine;officer.Xp=10;officer.Rank=1;
        if(researched){foreach(var tech in new[]{"measurement_methods","industrial_coordination","command_recovery"})if(!e.KnownTechnologyIds.Contains(tech))e.KnownTechnologyIds.Add(tech);e.KnownTechnologyIds.Sort(StringComparer.Ordinal);}
        int bound=0;while(f.RecoveryRandom!.Clone().NextInt(100)!=draw){Assert.True(++bound<10000);f.RecoveryRandom.NextInt(100);f.RecoveryRollCount++;}
        var habitat=s.Scouting!.Habitat!;v=s.FindVessel(v.Id)!;v.StationaryStarId=habitat.Star;v.AmebaEncounter=new(){Number=1,HabitatId=habitat.Id,ArrivalTurn=10,Stage=AmebaStage.Warning};e.Scouting!.NextEncounterNumber=2;e.Scouting.KnownHabitats.Add(new(habitat.Id,habitat.Star,10));ObservationUpdater.Refresh(s,10);FleetFixtures.Valid(g);
        var remain=CommandProcessor.Execute(g.Content,s,new SetAmebaRiskOrderCommand(e.Id,10,v.Id,1,1,AmebaRiskAction.Remain));Assert.True(remain.Accepted,remain.Rejection?.Message);g=new(g.Content,remain.State);var ack=CommandProcessor.Execute(g.Content,g.State,new AcknowledgeAmebaCommand(e.Id,10,ScoutQueries.Project(g.Content,g.State,e.Id).Attention));Assert.True(ack.Accepted,ack.Rejection?.Message);return FleetFixtures.Valid(new(g.Content,ack.State));
    }
    [Theory]
    [InlineData(49,false,true)]
    [InlineData(50,false,false)]
    [InlineData(69,true,true)]
    [InlineData(70,true,false)]
    public void MortalDraw49Succeeds50FailsAnd69Succeeds70Fails(int draw,bool researched,bool success)
    {
        var g=Loss(draw,researched:researched,cargo:true);var f=g.State.Empires[0].Fleets!;var expected=f.RecoveryRandom!.Clone();Assert.Equal(draw,expected.NextInt(100));long rolls=f.RecoveryRollCount;
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var after=result.State.Empires[0].Fleets!;var officer=after.Officers[0];Assert.Equal(success?OfficerStatus.Recovering:OfficerStatus.Dead,officer.Status);Assert.Equal(rolls+1,after.RecoveryRollCount);Assert.Equal(expected.State,after.RecoveryRandom!.State);Assert.Equal(expected.Increment,after.RecoveryRandom.Increment);Assert.Empty(result.State.Vessels);Assert.Equal(g.State.Random.State,result.State.Random.State);Assert.Equal(g.State.Empires[0].Scouting!.RiskRandom!.State,result.State.Empires[0].Scouting!.RiskRandom!.State);Assert.Equal(researched?70:50,officer.RecoveryPercentAtLoss);Assert.Equal(1,officer.Career.FlagshipsLost);
    }
    [Fact]
    public void LossAtTenReturnsAtThirteen()
    {
        var g=Loss();var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var officer=result.State.Empires[0].Fleets!.Officers[0];Assert.Equal(11,result.State.Turn);Assert.Equal(13,officer.AvailableAtTurn);Assert.Equal(11,officer.RecoveryStartedTurn);Assert.Null(officer.Flagship);
        var middle=TurnResolver.Resolve(g.Content,result.State);Assert.True(middle.Succeeded);Assert.Equal(OfficerStatus.Recovering,middle.State.Empires[0].Fleets!.Officers[0].Status);
        var returned=TurnResolver.Resolve(g.Content,middle.State);Assert.True(returned.Succeeded);officer=returned.State.Empires[0].Fleets!.Officers[0];Assert.Equal(13,returned.State.Turn);Assert.Equal(OfficerStatus.Posted,officer.Status);Assert.Equal(returned.State.Colonies.OrderBy(c=>c.Id).First().Id,officer.PostedColony);Assert.Null(officer.AvailableAtTurn);Assert.Equal(1,officer.Career.Recoveries);
    }
    internal static void FatalPlague(Saves.LoadedGame g,ColonyId id)
    {
        var s=g.State;var col=s.FindColony(id)!;col.Population=1;col.Workforce=new(1,0,0);col.ProductionReserve=0;var e=s.FindEmpire(col.EmpireId)!;var d=g.Content.Plagues.Values.Single();var location=CrisisFixtures.Location(s,col);
        e.Crisis!.RequireAcknowledgement=false;e.Crisis.Episode??=new(){Id=e.Crisis.NextEpisodeNumber++,FamilyId=d.Id,Source=location,SourcePopulation=1,CureCost=d.CureBase+d.CurePerPopulation};col.Crisis!.ImmuneUntilTurn=0;col.Crisis.ImmunityFamilyId=null;col.Crisis.Case=new(){EpisodeId=e.Crisis.Episode.Id,Stage=Crises.CrisisStage.Active,Severity=d.MortalityThreshold,AttentionRevision=1,HandledRevision=1,Origin=e.Crisis.Episode.Source};FleetFixtures.Valid(g);
    }
    [Fact]
    public void MissingRecoveryStartsOnlyWithOwnColonyAndRestartsIfDestinationLost()
    {
        var g=Loss(crises:true);var home=g.State.Colonies[0];var planet=home.PlanetId;var star=g.State.FindPlanet(planet)!.StarId;
        var settler=new VesselState{Id=new(g.State.AllocateId()),EmpireId=home.EmpireId,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=star,Crisis=new()};g.State.Vessels.Add(settler);var secondSettler=settler.Clone();secondSettler.Id=new(g.State.AllocateId());g.State.Vessels.Add(secondSettler);FatalPlague(g,home.Id);FleetFixtures.Valid(g);
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var f=result.State.Empires[0].Fleets!;var o=f.Officers[0];Assert.Equal(OfficerStatus.Missing,o.Status);Assert.Null(o.AvailableAtTurn);Assert.Null(o.RecoveryColony);long rolls=f.RecoveryRollCount;
        var waiting=TurnResolver.Resolve(g.Content,result.State);Assert.True(waiting.Succeeded,string.Join(";",waiting.Errors));Assert.Equal(OfficerStatus.Missing,waiting.State.Empires[0].Fleets!.Officers[0].Status);
        var founded=CommandProcessor.Execute(g.Content,waiting.State,new SetFoundingOrderCommand(home.EmpireId,waiting.State.Turn,settler.Id,planet));Assert.True(founded.Accepted,founded.Rejection?.Message);result=TurnResolver.Resolve(g.Content,founded.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));o=result.State.Empires[0].Fleets!.Officers[0];Assert.Equal(OfficerStatus.Recovering,o.Status);Assert.Equal(result.State.Turn+2,o.AvailableAtTurn);Assert.Equal(rolls,result.State.Empires[0].Fleets!.RecoveryRollCount);
        var resumed=new Saves.LoadedGame(g.Content,result.State);FatalPlague(resumed,result.State.Colonies[0].Id);result=TurnResolver.Resolve(g.Content,resumed.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));o=result.State.Empires[0].Fleets!.Officers[0];Assert.Equal(OfficerStatus.Missing,o.Status);Assert.Null(o.AvailableAtTurn);Assert.Null(o.RecoveryColony);Assert.Equal(rolls,result.State.Empires[0].Fleets!.RecoveryRollCount);
        founded=CommandProcessor.Execute(g.Content,result.State,new SetFoundingOrderCommand(home.EmpireId,result.State.Turn,secondSettler.Id,planet));Assert.True(founded.Accepted,founded.Rejection?.Message);result=TurnResolver.Resolve(g.Content,founded.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));Assert.Equal(result.State.Turn+2,result.State.Empires[0].Fleets!.Officers[0].AvailableAtTurn);
        for(int i=0;i<2;i++){result=TurnResolver.Resolve(g.Content,result.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));}Assert.Equal(OfficerStatus.Posted,result.State.Empires[0].Fleets!.Officers[0].Status);Assert.Equal(rolls,result.State.Empires[0].Fleets!.RecoveryRollCount);
    }
    [Fact]
    public void NonFlagshipLossKeepsCommanderAndDrawsOnlyTheOrdinaryHullRisk()
    {
        var g=Loss();var s=g.State;var owner=s.Empires[0];var f=owner.Fleets!;var flagship=s.Vessels[0];var home=s.FindPlanet(s.Colonies[0].PlanetId)!.StarId;flagship.PendingMove=home;flagship.AmebaEncounter!.RiskOrder=null;flagship.AmebaEncounter.Revision++;
        var member=flagship.Clone();member.Id=new(s.AllocateId());member.AmebaEncounter!.Number=owner.Scouting!.NextEncounterNumber++;member.AmebaEncounter.ArrivalTurn=9;member.AmebaEncounter.Stage=AmebaStage.Exposed;s.Vessels.Add(member);f.Fleets[0].Members.Add(member.Id);owner.Scouting.SuccessfulStudies.Add(new(1,10,member.Id));f.StudyCredits.Add(1);
        owner.Scouting.RiskRandom!.NextInt(100);owner.Scouting.RiskRollCount++;while(owner.Scouting.RiskRandom.Clone().NextInt(100)<95){owner.Scouting.RiskRandom.NextInt(100);owner.Scouting.RiskRollCount++;}var expectedRisk=owner.Scouting.RiskRandom.Clone();Assert.InRange(expectedRisk.NextInt(100),95,99);long officerRolls=f.RecoveryRollCount,hullRolls=owner.Scouting.RiskRollCount;owner.Scouting.Receipts.Clear();FleetFixtures.Valid(g);
        var ack=CommandProcessor.Execute(g.Content,s,new AcknowledgeAmebaCommand(owner.Id,10,ScoutQueries.Project(g.Content,s,owner.Id).Attention));Assert.True(ack.Accepted,ack.Rejection?.Message);var result=TurnResolver.Resolve(g.Content,ack.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));Assert.Null(result.State.FindVessel(member.Id));Assert.NotNull(result.State.FindVessel(flagship.Id));var after=result.State.Empires[0].Fleets!;Assert.Equal(OfficerStatus.Commanding,after.Officers[0].Status);Assert.Equal(flagship.Id,Assert.Single(after.Fleets).Flagship);Assert.Equal(officerRolls,after.RecoveryRollCount);Assert.Equal(1,after.Officers[0].Career.ShipsLost);Assert.Equal(0,after.Officers[0].Career.FlagshipsLost);Assert.Equal(hullRolls+1,result.State.Empires[0].Scouting!.RiskRollCount);Assert.Equal(expectedRisk.State,result.State.Empires[0].Scouting!.RiskRandom!.State);
    }
    [Fact]
    public void EquipmentCompletedOnTheLossBoundaryAppliesOnlyNextSnapshot()
    {
        var g=Loss(69);var e=g.State.Empires[0];foreach(var tech in new[]{"measurement_methods","industrial_coordination"})e.KnownTechnologyIds.Add(tech);e.KnownTechnologyIds.Sort(StringComparer.Ordinal);e.ResearchTargetId="command_recovery";e.ResearchReserve=2400;FleetFixtures.Valid(g);
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));Assert.Contains("command_recovery",result.State.Empires[0].KnownTechnologyIds);Assert.Equal(OfficerStatus.Dead,result.State.Empires[0].Fleets!.Officers[0].Status);Assert.Equal(50,result.State.Empires[0].Fleets!.Officers[0].RecoveryPercentAtLoss);
    }
    [Fact]
    public void EveryPhaseIncludingRecoveryAndValidationRollsBackAllStreams()
    {
        var g=Loss();string bytes=FleetFixtures.Bytes(g.State);
        foreach(var phase in Enum.GetValues<FleetResolutionPhase>())
        {
            bool reached=false;var failed=TurnResolver.ResolveFleet(g.Content,g.State,p=>{if(p==phase){reached=true;throw new InvalidOperationException("Injected officer rollback");}});Assert.True(reached,phase.ToString());Assert.False(failed.Succeeded);Assert.Null(failed.Report);Assert.Same(g.State,failed.State);Assert.Equal(bytes,FleetFixtures.Bytes(g.State));
        }
    }
    [Fact]
    public void TwoActualFlagshipLossesDrawInOfficerOrderRatherThanFleetOrVesselOrder()
    {
        var g=Loss();var s=g.State;var e=s.Empires[0];var f=e.Fleets!;var first=s.Vessels[0];var second=first.Clone();second.Id=new(s.AllocateId());second.Fleet=new(2);second.AmebaEncounter!.Number=e.Scouting!.NextEncounterNumber++;s.Vessels.Add(second);
        var officer=f.Officers[0];var other=officer.Clone();other.Id=new(2);other.Flagship=first.Id;officer.Flagship=second.Id;f.Officers.Add(other);f.LifetimeCommissions=2;f.NextOfficerId=3;f.ProfileCommissions[officer.ProfileId]=2;f.Fleets[0].Officer=other.Id;f.Fleets.Add(new(){Id=new(2),Name="Second designated flagship",Members=new(){second.Id},Flagship=second.Id,Officer=officer.Id});f.NextFleetId=3;
        for(int i=0;;i++){Assert.True(i<10000);var peek=f.RecoveryRandom!.Clone();if(peek.NextInt(100)<50&&peek.NextInt(100)>=50)break;f.RecoveryRandom.NextInt(100);f.RecoveryRollCount++;}var expected=f.RecoveryRandom!.Clone();Assert.InRange(expected.NextInt(100),0,49);Assert.InRange(expected.NextInt(100),50,99);long initial=f.RecoveryRollCount;e.Scouting.Receipts.Clear();FleetFixtures.Valid(g);
        var ack=CommandProcessor.Execute(g.Content,s,new AcknowledgeAmebaCommand(e.Id,10,ScoutQueries.Project(g.Content,s,e.Id).Attention));Assert.True(ack.Accepted,ack.Rejection?.Message);var result=TurnResolver.Resolve(g.Content,ack.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var after=result.State.Empires[0].Fleets!;Assert.Equal(OfficerStatus.Recovering,after.Officers.Single(o=>o.Id==new OfficerId(1)).Status);Assert.Equal(OfficerStatus.Dead,after.Officers.Single(o=>o.Id==new OfficerId(2)).Status);Assert.Equal(initial+2,after.RecoveryRollCount);Assert.Equal(expected.State,after.RecoveryRandom!.State);
    }
    [Fact]
    public void PurePreviewsInvalidAndNoOpCommandsDoNotDraw()
    {
        var g=Loss();string before=FleetFixtures.Bytes(g.State);for(int i=0;i<25;i++){ScoutQueries.Project(g.Content,g.State,g.State.Empires[0].Id);FleetQueries.Project(g.Content,g.State,g.State.Empires[0].Id);}
        var invalid=FleetOfficerTests.Run(g,new OfficerAction.SetDoctrine(new(999),OfficerDoctrine.LastWatch));Assert.False(invalid.Accepted);Assert.Same(g.State,invalid.State);var same=FleetOfficerTests.Run(g,new OfficerAction.SetDoctrine(new(1),OfficerDoctrine.RecoverCommand));Assert.True(same.Accepted);Assert.Same(g.State,same.State);Assert.Equal(before,FleetFixtures.Bytes(g.State));
    }
    [Fact]
    public void CommittedTransitDoesNotConsumeOfficerRandomness()
    {
        var g=Loss();var s=g.State;var v=s.Vessels[0];var origin=v.StationaryStarId!.Value;var home=s.FindPlanet(s.Colonies[0].PlanetId)!.StarId;v.StationaryStarId=null;v.PendingMove=null;v.AmebaEncounter=null;v.Transit=new(origin,home,3,2);s.Empires[0].Scouting!.Receipts.Clear();var f=s.Empires[0].Fleets!;f.Fleets[0].Cohort=new(origin,home,3,9,315);long rolls=f.RecoveryRollCount;ulong random=f.RecoveryRandom!.State;FleetFixtures.Valid(g);
        var result=TurnResolver.Resolve(g.Content,s);Assert.True(result.Succeeded,string.Join(";",result.Errors));Assert.Equal(1,result.State.FindVessel(v.Id)!.Transit!.RemainingTurns);Assert.Equal(rolls,result.State.Empires[0].Fleets!.RecoveryRollCount);Assert.Equal(random,result.State.Empires[0].Fleets!.RecoveryRandom!.State);Assert.Equal(OfficerStatus.Commanding,result.State.Empires[0].Fleets!.Officers[0].Status);
    }
    [Theory]
    [InlineData(false,OfficerDoctrine.RecoverCommand,2)]
    [InlineData(true,OfficerDoctrine.RecoverCommand,1)]
    [InlineData(false,OfficerDoctrine.LastWatch,4)]
    [InlineData(true,OfficerDoctrine.LastWatch,3)]
    public void WenthLinkTwoOneAndLastWatchFourThreeWithoutDraw(bool researched,OfficerDoctrine doctrine,int delay)
    {
        var g=Loss(species:"wenth",researched:researched,doctrine:doctrine);var before=g.State.Empires[0].Fleets!;var source=before.Officers[0].SourceBody;long rolls=before.RecoveryRollCount;ulong random=before.RecoveryRandom!.State;
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var o=result.State.Empires[0].Fleets!.Officers[0];Assert.Equal(11+delay,o.AvailableAtTurn);Assert.Equal(OfficerStatus.Recovering,o.Status);Assert.Equal(source,o.SourceBody);Assert.Equal(10,o.Xp);Assert.Equal(rolls,result.State.Empires[0].Fleets!.RecoveryRollCount);Assert.Equal(random,result.State.Empires[0].Fleets!.RecoveryRandom!.State);
        for(int i=0;i<delay;i++){result=TurnResolver.Resolve(g.Content,result.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));}
        o=result.State.Empires[0].Fleets!.Officers[0];Assert.Equal(OfficerStatus.Posted,o.Status);Assert.Equal(source,o.PostedColony);Assert.DoesNotContain(o.Highlights,h=>h.Reason.Contains("limb killed",StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public void LastWatchAndFailedRecoveryArePermanentAndNeverDrawAgain()
    {
        foreach(var g in new[]{Loss(doctrine:OfficerDoctrine.LastWatch),Loss(50)})
        {
            long before=g.State.Empires[0].Fleets!.RecoveryRollCount;var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var officer=result.State.Empires[0].Fleets!.Officers[0];Assert.Equal(OfficerStatus.Dead,officer.Status);Assert.NotNull(officer.FinalSummary);long expected=before+(g.State.Empires[0].Fleets!.Officers[0].Doctrine==OfficerDoctrine.LastWatch?0:1);Assert.Equal(expected,result.State.Empires[0].Fleets!.RecoveryRollCount);
            for(int i=0;i<4;i++){result=TurnResolver.Resolve(g.Content,result.State);Assert.True(result.Succeeded);Assert.Equal(OfficerStatus.Dead,result.State.Empires[0].Fleets!.Officers[0].Status);Assert.Equal(expected,result.State.Empires[0].Fleets!.RecoveryRollCount);}
        }
    }
}
