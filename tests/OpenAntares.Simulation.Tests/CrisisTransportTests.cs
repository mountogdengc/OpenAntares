using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisTransportTests
{
    [Fact]
    public void ArrivalAtClosedPortGetsFullExtraInterval()
    {
        var g=CrisisFixtures.CargoArrival(quarantined:true);var v=g.State.Vessels.Single();
        var first=Step(g);g=first.Game;Assert.Equal(23,g.State.Turn);Assert.Equal(new StarId(6),g.State.Vessels[0].StationaryStarId);Assert.Equal(24,g.State.Vessels[0].Crisis!.InspectionDueTurn);Assert.Equal(1,g.State.Vessels[0].Crisis!.CarrierEpisode);
        Assert.Empty(first.Context.Exposures);Assert.Null(g.State.Colonies[1].Crisis!.Case);AssertRejectedMove(g,v.Id);
        var second=Step(g);g=second.Game;Assert.Null(g.State.Vessels[0].Crisis!.CarrierEpisode);Assert.Null(g.State.Vessels[0].Crisis!.InspectionDueTurn);Assert.Contains(v.Id,second.Context.SterileVessels);AssertRejectedMove(g,v.Id);
    }
    [Fact]
    public void CuredFlagCannotReappear()
    {
        var g=CrisisFixtures.Active();AddDestination(g);var c=g.State.Colonies[0];c.Workforce=new(2,2,2);var e=g.State.Empires[0].Crisis!;e.Episode!.CureProgress=1199;e.Episode.ResearchShare=100;
        var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.PendingMove=new(6);v.Crisis!.CarrierEpisode=1;
        var first=Step(g);g=first.Game;Assert.Contains((c.EmpireId,1L),first.Context.CuredEpisodes);Assert.Null(g.State.Vessels[0].Crisis!.CarrierEpisode);Assert.Equal(1,g.State.Vessels[0].Transit!.RemainingTurns);
        g=Step(g).Game;Assert.Null(g.State.Vessels[0].Crisis!.CarrierEpisode);Assert.All(g.State.Colonies,x=>Assert.Null(x.Crisis!.Case));Assert.Null(g.State.Empires[0].Crisis!.Episode);
    }
    [Fact]
    public void RecoveredSourceStillExportsAlreadyTaggedDeparture()
    {
        var g=CrisisFixtures.Active(severity:100,age:5);AddDestination(g);var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.PendingMove=new(6);
        g=Step(g).Game;Assert.Null(g.State.Colonies[0].Crisis!.Case);Assert.Equal(1,g.State.Vessels[0].Crisis!.CarrierEpisode);Assert.NotNull(g.State.Empires[0].Crisis!.Episode);
        g=Step(g).Game;var illness=g.State.Colonies[1].Crisis!.Case!;Assert.Equal(CrisisStage.Warning,illness.Stage);Assert.Equal(2,illness.GraceRemaining);Assert.Equal(v.Id,illness.SourceVessel);
    }
    [Fact]
    public void FoundingTransfersWarningBeforeVesselRemoval()
    {
        var g=CrisisFixtures.CargoArrival(found:true);var v=g.State.Vessels[0];g=Step(g).Game;
        var order=CommandProcessor.Execute(g.Content,g.State,new SetFoundingOrderCommand(v.EmpireId,g.State.Turn,v.Id,new(7)));Assert.True(order.Accepted,order.Rejection?.Message);
        g=Step(new(g.Content,order.State)).Game;Assert.Empty(g.State.Vessels);var founded=g.State.Colonies.Single(c=>c.PlanetId==new PlanetId(7));Assert.Equal(1,founded.Population);Assert.Equal(new Workforce(1,0,0),founded.Workforce);Assert.NotNull(founded.Development);Assert.Equal(2,founded.Crisis!.Case!.GraceRemaining);Assert.Equal(v.Id,founded.Crisis.Case.SourceVessel);
        Assert.True(SaveGame.Deserialize(SaveGame.Serialize(g.Content,g.State)).Succeeded);
    }
    [Fact]
    public void DeduplicatedContactsUseLowestSourceVessel()
    {
        var g=CrisisFixtures.CargoArrival();var first=g.State.Vessels[0];var second=first.Clone();second.Id=new(g.State.AllocateId());g.State.Vessels.Add(second);
        var result=Step(g);var illness=Assert.IsType<CrisisCaseState>(result.Game.State.Colonies[1].Crisis!.Case);Assert.Equal(first.Id,illness.SourceVessel);Assert.Equal(2,illness.GraceRemaining);Assert.Single(result.Context.Events,x=>x.Kind=="exposure");
        Assert.All(result.Game.State.Vessels,v=>Assert.Equal(1,v.CarriedPopulation));
    }
    [Theory][InlineData(2,false)][InlineData(1,true)]
    public void ArrivalContactsUsePostHealthActiveSourcesAfterFullWarning(int grace,bool exposes)
    {
        var g=CrisisFixtures.Warning(grace);var healthy=AddHealthyAtHome(g);var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.StationaryStarId=null;v.Transit=new(new(6),Fixtures.HomeStar,2,1);
        var result=Step(g);Assert.Equal(exposes,result.Game.State.FindColony(healthy.Id)!.Crisis!.Case is not null);
        Assert.Equal(exposes, result.Game.State.Vessels[0].Crisis!.CarrierEpisode is not null);
        if(exposes)Assert.Equal(2,result.Game.State.FindColony(healthy.Id)!.Crisis!.Case!.GraceRemaining);
    }
    [Fact]
    public void AutoReleaseCannotOpenPortMidBoundary()
    {
        var g=CrisisFixtures.CargoArrival(quarantined:true);var source=g.State.Colonies[0];var target=g.State.Colonies[1];
        target.Crisis!.Case=source.Crisis!.Case!.Clone();target.Crisis.Case.ActiveAge=5;target.Crisis.Case.Severity=100;target.Crisis.ReleaseEpisode=1;
        var result=Step(g);target=result.Game.State.FindColony(target.Id)!;Assert.False(target.Crisis!.Quarantined);Assert.Null(target.Crisis.Case);Assert.Equal(24,result.Game.State.Vessels[0].Crisis!.InspectionDueTurn);Assert.Empty(result.Context.Exposures);
    }
    [Fact]
    public void InspectionCreatesOneSterileBoundaryThenOpenSourceCanRecontaminate()
    {
        var g=CrisisFixtures.Active();var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.Crisis!.CarrierEpisode=1;
        var order=CommandProcessor.Execute(g.Content,g.State,new DecontaminateVesselCommand(v.EmpireId,g.State.Turn,v.Id,System.Collections.Immutable.ImmutableArray.Create(CrisisQueries.OrderFact(v))));Assert.True(order.Accepted);
        var first=Step(new(g.Content,order.State));g=first.Game;Assert.Null(g.State.Vessels[0].Crisis!.CarrierEpisode);Assert.Contains(v.Id,first.Context.SterileVessels);
        g=Step(g).Game;Assert.Equal(1,g.State.Vessels[0].Crisis!.CarrierEpisode);
    }
    [Fact]
    public void CureClearsFlagWithoutDeletingUnexpiredInspection()
    {
        var g=CrisisFixtures.CargoArrival(quarantined:true);var first=Step(g);g=first.Game;
        // Labelled pure progression check: capture a separate future hold, not a running-session injection.
        var snapshot=g.State.Clone();var original=snapshot.Empires[0].Crisis!;
        original.Episode!.ResearchShare=100;original.Episode.CureProgress=1199;snapshot.Colonies[0].Workforce=new(2,2,2);
        var candidate=snapshot.Clone();var context=CrisisResolutionContext.Capture(g.Content,snapshot);
        var economy=EconomyCalculator.Calculate(g.Content,snapshot);CrisisProgression.Apply(g.Content,snapshot,candidate,economy,context);
        Assert.Null(candidate.Vessels[0].Crisis!.CarrierEpisode);Assert.Equal(24,candidate.Vessels[0].Crisis!.InspectionDueTurn);
    }
    [Fact]
    public void LostSourceGivesNoNewArrivalContact()
    {
        var g=CrisisFixtures.Active(severity:80,age:3,population:1);var healthy=AddHealthyAtHome(g);var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.StationaryStarId=null;v.Transit=new(new(6),Fixtures.HomeStar,2,1);
        g=Step(g).Game;Assert.Null(g.State.FindColony(Fixtures.HomeColony));Assert.Null(g.State.FindColony(healthy.Id)!.Crisis!.Case);Assert.Null(g.State.Vessels[0].Crisis!.CarrierEpisode);
    }
    [Fact]
    public void SourceCanDieAfterRealEmbarkWithoutDestroyingCargo()
    {
        var g=CrisisFixtures.Active(severity:80,age:3,population:3);AddDestination(g);var c=g.State.Colonies[0];c.Workforce=new(1,2,0);c.ProductionReserve=1600;c.ProjectKind=ProductionKinds.Vessel;c.ProjectId="harl_settler";c.Development!.Origin=ProductionOrigin.Manual;
        g=Step(g).Game;Assert.Equal(1,g.State.FindColony(c.Id)!.Population);var born=g.State.Vessels.Single(v=>v.RoleId=="harl_settler");Assert.Equal(1,born.Crisis!.CarrierEpisode);Assert.Equal(1,born.CarriedPopulation);
        var move=CommandProcessor.Execute(g.Content,g.State,new SetMoveOrderCommand(c.EmpireId,g.State.Turn,born.Id,new(6)));Assert.True(move.Accepted);
        g=Step(new(g.Content,move.State)).Game;Assert.Null(g.State.FindColony(c.Id));Assert.Equal(1,g.State.FindVessel(born.Id)!.CarriedPopulation);Assert.Equal(1,g.State.FindVessel(born.Id)!.Crisis!.CarrierEpisode);Assert.NotNull(g.State.Empires[0].Crisis!.Episode);
        g=Step(g).Game;Assert.Equal(2,g.State.Colonies[0].Crisis!.Case!.GraceRemaining);
    }
    [Fact]
    public void ImmuneDestinationDoesNotReceiveExposure()
    {
        var g=CrisisFixtures.CargoArrival();var target=g.State.Colonies[1];target.Crisis!.ImmunityFamilyId="ash_bloom";target.Crisis.ImmuneUntilTurn=30;
        g=Step(g).Game;Assert.Null(g.State.FindColony(target.Id)!.Crisis!.Case);Assert.Equal(1,g.State.Vessels[0].Crisis!.CarrierEpisode);
    }
    [Fact]
    public void ScoutHasNoTransmissionAndOffKeepsOriginalTravelAndCargo()
    {
        var g=CrisisFixtures.Active();AddDestination(g);g.State.Vessels[0].PendingMove=new(6);g=Step(g).Game;Assert.Null(g.State.Vessels[0].Crisis!.CarrierEpisode);
        var off=CrisisFixtures.Game(enabled:false);var v=off.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.PendingMove=new(6);
        off=Step(off).Game;Assert.Equal(1,off.State.Vessels[0].Transit!.RemainingTurns);Assert.Equal(1,off.State.Vessels[0].CarriedPopulation);Assert.Null(off.State.Vessels[0].Crisis!.CarrierEpisode);
    }
    [Fact]
    public void ForeignActivePortDoesNotConferOwnCarrierFlag()
    {
        var g=CrisisFixtures.TwoHumansAndAi();var foreign=g.State.Empires[1];var c=g.State.Colonies[1];var location=CrisisQueries.Location(g.State,c);foreign.Crisis!.NextEpisodeNumber=2;foreign.Crisis.Episode=new(){Id=1,FamilyId="ash_bloom",Source=location,SourcePopulation=6,CureCost=1200};c.Crisis!.Case=new(){EpisodeId=1,Stage=CrisisStage.Active,Severity=20,AttentionRevision=2,HandledRevision=2,Origin=location};
        var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.StationaryStarId=null;v.Transit=new(Fixtures.HomeStar,new(6),2,1);
        g=Step(g).Game;Assert.Null(g.State.Vessels[0].Crisis!.CarrierEpisode);Assert.Null(g.State.Empires[0].Crisis!.Episode);
    }
    [Fact]
    public void OneQuarantinedWorldClosesWholeOwnSystemWithoutPenalizingOtherWorld()
    {
        var g=CrisisFixtures.Active();var healthy=AddHealthyAtHome(g);healthy.Crisis!.Quarantined=true;g.State.Colonies[0].Workforce=new(2,2,2);
        var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=1;v.StationaryStarId=null;v.Transit=new(new(6),Fixtures.HomeStar,2,1);v.Crisis!.CarrierEpisode=1;
        Assert.Equal(320,EconomyCalculator.Calculate(g.Content,g.State).Colony(Fixtures.HomeColony).NetProduction.Total);
        var result=Step(g);Assert.Equal(24,result.Game.State.Vessels[0].Crisis!.InspectionDueTurn);Assert.Null(result.Game.State.FindColony(healthy.Id)!.Crisis!.Case);Assert.Empty(result.Context.Exposures);
    }
    [Fact]
    public void LostQuarantinePortStillInspectsArrivalAndCanFinishAtEmptySystem()
    {
        var g=CrisisFixtures.CargoArrival(quarantined:true);var target=g.State.Colonies[1];target.Population=1;target.Workforce=new(1,0,0);target.Crisis!.Case=g.State.Colonies[0].Crisis!.Case!.Clone();target.Crisis.Case.Severity=80;target.Crisis.Case.ActiveAge=3;
        g=Step(g).Game;Assert.Null(g.State.FindColony(target.Id));Assert.Equal(24,g.State.Vessels[0].Crisis!.InspectionDueTurn);Assert.Equal(1,g.State.Vessels[0].Crisis!.CarrierEpisode);
        g=Step(g).Game;Assert.Null(g.State.Vessels[0].Crisis!.InspectionDueTurn);Assert.Null(g.State.Vessels[0].Crisis!.CarrierEpisode);
    }
    [Fact]
    public void SameBoundaryCureProducesHealthyFoundedColony()
    {
        var g=CrisisFixtures.CargoArrival(found:true);var v=g.State.Vessels[0];v.Transit=null;v.StationaryStarId=new(6);v.PendingFounding=new(7);g.State.Colonies[0].Workforce=new(2,2,2);var e=g.State.Empires[0].Crisis!;e.Episode!.CureProgress=1199;e.Episode.ResearchShare=100;ObservationUpdater.Refresh(g.State,g.State.Turn);
        g=Step(g).Game;Assert.Empty(g.State.Vessels);Assert.Equal(2,g.State.Colonies.Count);Assert.All(g.State.Colonies,c=>Assert.Null(c.Crisis!.Case));Assert.Null(g.State.Empires[0].Crisis!.Episode);
    }
    [Theory][InlineData("source_vessel")][InlineData("retired_colony")]
    public void TransportProvenanceCannotBorrowAnotherLiveEntityKind(string kind)
    {
        var g=CrisisFixtures.Active();Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        if(kind=="source_vessel")g.State.Colonies[0].Crisis!.Case!.SourceVessel=new(Fixtures.HomeStar.Value);
        else{var falseLocation=CrisisQueries.Location(g.State,g.State.Colonies[0]) with{Colony=new(g.State.Vessels[0].Id.Value)};g.State.Empires[0].Crisis!.LostColonyLocations.Add(falseLocation);g.State.Colonies[0].Crisis!.Case!.Origin=falseLocation;}
        Assert.NotEmpty(GameStateValidator.Validate(g.Content,g.State));
    }
    private static ColonyState AddDestination(LoadedGame g)
    {
        var c=new ColonyState{Id=new(g.State.AllocateId()),PlanetId=new(7),EmpireId=g.State.Empires[0].Id,Population=3,Workforce=new(3,0,0),HabitatMode="native",Development=new(),Crisis=new()};g.State.Colonies.Add(c);ObservationUpdater.Refresh(g.State,g.State.Turn);return c;
    }
    private static ColonyState AddHealthyAtHome(LoadedGame g)
    {
        var p=new PlanetState{Id=new(g.State.AllocateId()),StarId=Fixtures.HomeStar,Orbit=2,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea",Appearance=new(1,2,8)};g.State.Planets.Add(p);
        var c=new ColonyState{Id=new(g.State.AllocateId()),PlanetId=p.Id,EmpireId=g.State.Empires[0].Id,Population=3,Workforce=new(3,0,0),HabitatMode="native",Development=new(),Crisis=new()};g.State.Colonies.Add(c);ObservationUpdater.Refresh(g.State,g.State.Turn);return c;
    }
    private static void AssertRejectedMove(LoadedGame g,VesselId id){var result=CommandProcessor.Execute(g.Content,g.State,new SetMoveOrderCommand(g.State.Empires[0].Id,g.State.Turn,id,Fixtures.HomeStar));Assert.False(result.Accepted);Assert.Same(g.State,result.State);}
    // Labelled phase integration only: no human/AI preparation or Ready gate. T8 owns the real transaction.
    private static (LoadedGame Game,CrisisResolutionContext Context) Step(LoadedGame g)
    {
        Assert.Empty(GameStateValidator.Validate(g.Content,g.State));var before=g.State;var candidate=before.Clone();var ctx=CrisisResolutionContext.Capture(g.Content,before);var economy=EconomyCalculator.Calculate(g.Content,before);var events=new List<ExpansionEvent>();
        foreach(var output in economy.Colonies)
        {
            var c=candidate.FindColony(output.Colony)!;c.Population=output.Growth.PopulationAfter;c.Workforce=output.Growth.WorkforceAfter;c.GrowthProgress=output.Growth.ProgressAfter;c.ProductionReserve=output.Production.Ending;
            if(output.VesselCompletionPlan is {} plan)
            {
                var v=new VesselState{Id=new(candidate.AllocateId()),EmpireId=c.EmpireId,RoleId=plan.RoleId,CarriedPopulation=plan.Cargo,StationaryStarId=plan.Star};candidate.Vessels.Add(v);CrisisTransport.TagCompletion(g.Content,before,candidate,ctx,c.Id,v);
                if(output.EmbarkOutcome is {} embark){c.Population=embark.PopulationAfter;c.Workforce=embark.WorkforceAfter;}c.ProjectId=null;c.ProjectKind=ProductionKinds.Building;
            }
        }
        foreach(var output in economy.Empires)candidate.FindEmpire(output.Empire)!.ResearchReserve=output.Research.Ending;
        CrisisProgression.Apply(g.Content,before,candidate,economy,ctx);CrisisTransport.PrepareDepartures(g.Content,before,candidate,ctx);
        MovementResolver.Apply(g.Content,before,candidate,events,ctx);CrisisTransport.HandleArrivals(g.Content,before,candidate,ctx);
        OpenAntares.Simulation.Colonization.ColonizationResolver.Apply(g.Content,before,candidate,events,ctx);CrisisTransport.ApplyExposures(g.Content,before,candidate,ctx);CrisisOnset.Apply(g.Content,candidate,ctx);CrisisReports.CloseFinishedEpisodes(g.Content,candidate,ctx);CrisisReports.Finalize(before,candidate,ctx);
        DevelopmentCompletion.Apply(g.Content,candidate,new(before.Turn,ctx.ToTurn,economy));candidate.Turn=ctx.ToTurn;ObservationUpdater.Refresh(candidate,candidate.Turn);ContactUpdater.Refresh(before,candidate);Assert.Empty(GameStateValidator.Validate(g.Content,candidate));
        return (new(g.Content,candidate),ctx);
    }
}
