using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetResolutionTests
{
    [Fact]
    public void SameRoleWithDifferentActiveTokenCannotClaimOriginalReservation()
    {
        var g=FleetFixtures.Nested();var col=g.State.Colonies[0];col.Development!.Queue.Insert(0,new(2,new(ProductionKinds.Vessel,"scout")));col.Development.NextQueueToken=3;col.Development.ActiveQueueToken=2;col.Development.Origin=ProductionOrigin.Queue;col.ProjectKind=ProductionKinds.Vessel;col.ProjectId="scout";col.ProductionReserve=800;FleetFixtures.Valid(g);
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var built=result.State.Vessels.Single(v=>v.NewlyCompleted);Assert.Null(built.BuildOrigin);var slot=result.State.Empires[0].Fleets!.Requests[0].Slots[0];Assert.Equal(SlotStatus.Queued,slot.Status);Assert.Equal(1,slot.QueueToken);Assert.Null(slot.Vessel);Assert.Equal(1,Assert.Single(result.State.Colonies[0].Development!.Queue).Token);
    }
    [Fact]
    public void LossNormalizationRetainsLowestSurvivorAndCapturedLaunchProvenance()
    {
        var g=FleetFixtures.Game();var first=g.State.Vessels[0];var second=first.Clone();second.Id=new(g.State.AllocateId());g.State.Vessels.Add(second);var made=FleetCommandTests.Run(g,new FleetAction.Create("Retained",ImmutableArray.Create(first.Id,second.Id)));Assert.True(made.Accepted);g=new(g.Content,made.State);
        var home=g.State.FindVessel(first.Id)!.StationaryStarId!.Value;var away=g.State.Stars.First(s=>s.Id!=home);g.State.FindStar(home)!.X=0;g.State.FindStar(home)!.Y=0;away.X=601;away.Y=0;
        foreach(var v in g.State.Vessels)v.PendingMove=away.Id;
        var context=FleetResolutionContext.Capture(g.Content,g.State);var candidate=g.State.Clone();candidate.Vessels.Remove(candidate.FindVessel(first.Id)!);
        FleetResolver.NormalizeAfterLosses(g.Content,g.State,candidate,context);var retained=Assert.Single(candidate.Empires[0].Fleets!.Fleets);Assert.Equal(new FleetId(1),retained.Id);Assert.Equal(second.Id,retained.Flagship);Assert.Equal(2,candidate.Empires[0].Fleets!.NextFleetId);
        var events=new System.Collections.Generic.List<ExpansionEvent>();var scouting=AmebaResolutionContext.Capture(g.Content,g.State);scouting.ConsumedVessels.Add(first.Id);Movement.MovementResolver.Apply(g.Content,g.State,candidate,events,scouting:scouting,fleetContext:context);
        Assert.Equal(context.Launches[second.Id],retained.Cohort);Assert.Equal(context.Launches[second.Id].TotalTurns-1,candidate.FindVessel(second.Id)!.Transit!.RemainingTurns);FleetFixtures.Valid(new(g.Content,candidate));
    }
    [Fact]
    public void EveryPhaseFailureRollsBackTheEnclosingOriginalIncludingReadyHuman()
    {
        foreach(bool ready in new[]{false,true})
        {
            var g=FleetReadyTests.Explore(FleetReadyTests.Multi());if(ready){var result=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,0,true));Assert.True(result.Accepted);g=new(g.Content,result.State);}
            string bytes=FleetFixtures.Bytes(g.State);
            foreach(var phase in Enum.GetValues<FleetResolutionPhase>())
            {
                bool reached=false;var result=TurnResolver.ResolveFleet(g.Content,g.State,p=>{if(p==phase){reached=true;throw new InvalidOperationException("Injected fleet phase failure");}});
                Assert.True(reached,phase.ToString());Assert.False(result.Succeeded);Assert.Same(g.State,result.State);Assert.Null(result.Report);Assert.Equal(bytes,FleetFixtures.Bytes(g.State));
            }
        }
    }
    [Fact]
    public void PartialArrivalsAndMissingQuotaDoNotCompleteOrReplace()
    {
        var g=FleetFixtures.Game();var first=g.State.Vessels[0];var second=first.Clone();second.Id=new(g.State.AllocateId());g.State.Vessels.Add(second);
        var home=first.StationaryStarId!.Value;var away=g.State.Stars.First(s=>s.Id!=home).Id;
        g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,2) with{AcceptedIdleVessels=ImmutableArray.Create(first.Id,second.Id)});
        var r=g.State.Empires[0].Fleets!.Requests[0];var detached=FleetRequestTests.Run(g,new AssemblyAction.DetachSlot(r.Id,r.Revision,r.Slots.Single(s=>s.Vessel==second.Id).Ordinal));Assert.True(detached.Accepted,detached.Rejection?.Message);g=new(g.Content,detached.State);
        r=g.State.Empires[0].Fleets!.Requests[0];var resume=FleetRequestTests.Run(g,new AssemblyAction.Resume(r.Id,r.Revision,FleetRequestTests.Retained(g,r).Fingerprint));Assert.True(resume.Accepted,resume.Rejection?.Message);g=new(g.Content,resume.State);
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));r=result.State.Empires[0].Fleets!.Requests[0];Assert.Equal(RequestStatus.Active,r.Status);Assert.Equal(SlotStatus.Arrived,r.Slots.Single(s=>s.Vessel==first.Id).Status);Assert.Contains(r.Slots,s=>s.Status==SlotStatus.Missing);Assert.Null(r.CompletedTurn);Assert.Empty(result.State.Colonies[0].Development!.Queue);
        // Committed travel retains its stored timing despite changed current technology/rank.
        var live=result.State.FindVessel(first.Id)!;live.StationaryStarId=null;live.Transit=new(home,away,3,2);r.Slots.Single(s=>s.Vessel==first.Id).Status=SlotStatus.Travelling;
        var fleet=result.State.Empires[0].Fleets!.Fleets.Single();fleet.Cohort=new(home,away,3,0,300);
        var later=TurnResolver.Resolve(g.Content,result.State);Assert.True(later.Succeeded,string.Join(";",later.Errors));Assert.Equal(1,later.State.FindVessel(first.Id)!.Transit!.RemainingTurns);Assert.Equal(fleet.Cohort,later.State.Empires[0].Fleets!.Fleets.Single().Cohort);
    }
    [Fact]
    public void FullQuotaCompletesOnceAndLaterDangerDoesNotReopenIt()
    {
        var g=FleetFixtures.Game(amebas:true);var ship=g.State.Vessels[0];g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,1) with{AcceptedIdleVessels=ImmutableArray.Create(ship.Id)});
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var r=Assert.Single(result.State.Empires[0].Fleets!.Requests);Assert.Equal(RequestStatus.Complete,r.Status);Assert.Equal(1,r.CompletedTurn);Assert.Equal(FleetOrderKind.Hold,Assert.Single(result.State.Empires[0].Fleets!.Fleets).Order.Kind);
        var habitat=result.State.Scouting!.Habitat!;result.State.Empires[0].Scouting!.KnownHabitats.Add(new(habitat.Id,habitat.Star,1));var later=TurnResolver.Resolve(g.Content,result.State);Assert.True(later.Succeeded,string.Join(";",later.Errors));Assert.Equal(RequestStatus.Complete,later.State.Empires[0].Fleets!.Requests[0].Status);Assert.Equal(1,later.State.Empires[0].Fleets!.Requests[0].CompletedTurn);
    }
    [Fact]
    public void DestroyedFlagshipKeepsSurvivorIdentityAndFrozenLaunchSpeed()
    {
        var g=FleetFixtures.Game(amebas:true);var s=g.State;var habitat=s.Scouting!.Habitat!;var first=s.Vessels[0];var second=first.Clone();second.Id=new(s.AllocateId());s.Vessels.Add(second);
        var made=FleetCommandTests.Run(g,new FleetAction.Create("Survivors",ImmutableArray.Create(first.Id,second.Id)));Assert.True(made.Accepted);g=new(g.Content,made.State);s=g.State;
        var f=s.Empires[0].Fleets!;var fleet=f.Fleets[0];var destination=s.Stars.First(star=>star.Id!=habitat.Star).Id;
        foreach(var v in s.Vessels){v.StationaryStarId=habitat.Star;v.PendingMove=destination;v.AmebaEncounter=new(){Number=s.Empires[0].Scouting!.NextEncounterNumber++,HabitatId=habitat.Id,ArrivalTurn=0,Stage=AmebaStage.Warning};}
        s.Empires[0].Scouting!.KnownHabitats.Add(new(habitat.Id,habitat.Star,0));ObservationUpdater.Refresh(s,0);
        // Explicit remain causes certain loss; the other member takes its legal departure.
        var remain=CommandProcessor.Execute(g.Content,s,new SetAmebaRiskOrderCommand(s.Empires[0].Id,0,first.Id,s.FindVessel(first.Id)!.AmebaEncounter!.Number,1,AmebaRiskAction.Remain));Assert.True(remain.Accepted,remain.Rejection?.Message);g=new(g.Content,remain.State);
        var ack=CommandProcessor.Execute(g.Content,g.State,new AcknowledgeAmebaCommand(s.Empires[0].Id,0,ScoutQueries.Project(g.Content,g.State,s.Empires[0].Id).Attention));Assert.True(ack.Accepted,ack.Rejection?.Message);g=new(g.Content,ack.State);
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));Assert.Null(result.State.FindVessel(first.Id));Assert.NotNull(result.State.FindVessel(second.Id));Assert.Equal(0,result.State.Empires[0].Fleets!.RecoveryRollCount);
        // Safety separated the departing member before the loss; the emptied original retires.
        Assert.Empty(result.State.Empires[0].Fleets!.Fleets);Assert.Null(result.State.FindVessel(second.Id)!.Fleet);Assert.Equal(2,result.State.Empires[0].Fleets!.NextFleetId);
    }
    [Fact]
    public void LostColonyRetiresUnavailableAppointmentAndPreservesFiniteWorkFacts()
    {
        var g=FleetEconomyTests.Organizer(request:true,crises:true);var s=g.State;var col=s.Colonies[0];col.Population=1;col.Workforce=new(1,0,0);col.ProductionReserve=0;var location=CrisisFixtures.Location(s,col);var d=g.Content.Plagues.Values.Single();var e=s.Empires[0];
        e.Crisis!.RequireAcknowledgement=false;e.Crisis.NextEpisodeNumber=2;e.Crisis.Episode=new(){Id=1,FamilyId=d.Id,Source=location,SourcePopulation=1,CureCost=d.CureBase+d.CurePerPopulation};col.Crisis!.Case=new(){EpisodeId=1,Stage=Crises.CrisisStage.Active,Severity=d.MortalityThreshold,Origin=location,AttentionRevision=1,HandledRevision=1};FleetFixtures.Valid(g);
        var result=TurnResolver.Resolve(g.Content,s);Assert.True(result.Succeeded,string.Join(";",result.Errors));Assert.Null(result.State.FindColony(col.Id));Assert.All(result.State.Empires[0].Fleets!.Officers,o=>Assert.Equal(OfficerStatus.Retired,o.Status));var r=result.State.Empires[0].Fleets!.Requests[0];Assert.Equal(RequestStatus.SafetyPaused,r.Status);Assert.Empty(r.Colonies);Assert.Equal(SlotStatus.Missing,r.Slots[0].Status);Assert.Null(r.Coordinator);Assert.True(r.CoordinatorXpForfeited);Assert.Equal(0,result.State.Empires[0].Fleets!.RecoveryRollCount);
    }
    [Fact]
    public void CompletedHullWaitsUntilNextBoundaryAndBindsExactToken()
    {
        var g=FleetFixtures.Game();var rally=g.State.Stars.First(s=>s.Id!=g.State.Vessels[0].StationaryStarId).Id;
        g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,1) with{Rally=rally});g.State.Colonies[0].ProductionReserve=800;
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));
        var r=result.State.Empires[0].Fleets!.Requests[0];var slot=Assert.Single(r.Slots);Assert.Equal(SlotStatus.Built,slot.Status);
        var ship=result.State.FindVessel(slot.Vessel!.Value)!;Assert.Equal(new HullBuildOrigin(r.Id,slot.Ordinal,g.State.Colonies[0].Id,1),ship.BuildOrigin);Assert.True(ship.NewlyCompleted);Assert.Null(ship.Transit);Assert.Null(ship.PendingMove);Assert.Null(ship.Fleet);Assert.Empty(result.State.Colonies[0].Development!.Queue);
        var next=TurnResolver.Resolve(g.Content,result.State);Assert.True(next.Succeeded,string.Join(";",next.Errors));Assert.True(next.State.FindVessel(ship.Id)!.Transit is not null||next.State.FindVessel(ship.Id)!.StationaryStarId==rally);
    }
    [Fact]
    public void UnknownRallyArrivalSafetyPausesBeforeCompletion()
    {
        var g=FleetFixtures.Game(amebas:true);var ship=g.State.Vessels[0];var habitat=g.State.Scouting!.Habitat!;
        g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,1) with{Rally=habitat.Star,AcceptedIdleVessels=ImmutableArray.Create(ship.Id)});
        ship=g.State.FindVessel(ship.Id)!;ship.Transit=new(ship.StationaryStarId!.Value,habitat.Star,1,1);ship.StationaryStarId=null;g.State.Empires[0].Fleets!.Requests[0].Slots[0].Status=SlotStatus.Travelling;FleetFixtures.Valid(g);
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var r=result.State.Empires[0].Fleets!.Requests[0];Assert.Equal(RequestStatus.SafetyPaused,r.Status);Assert.Equal(SlotStatus.BlockedSafety,r.Slots[0].Status);Assert.Equal(SlotStatus.Built,r.Slots[0].ResumeStatus);Assert.Null(r.CompletedTurn);Assert.Null(result.State.FindVessel(ship.Id)!.Fleet);Assert.NotNull(result.State.FindVessel(ship.Id)!.AmebaEncounter);
    }
    [Fact]
    public void SafetySplitAtSixtyFourFleetsKeepsFlagshipCohort()
    {
        var g=FleetFixtures.Game();var s=g.State;var f=s.Empires[0].Fleets!;var first=s.Vessels[0];
        for(int i=1;i<=64;i++){var v=i==1?first:first.Clone();if(i!=1){v.Id=new(s.AllocateId());s.Vessels.Add(v);}v.Fleet=new(i);f.Fleets.Add(new(){Id=new(i),Name=$"Fleet {i}",Members=new(){v.Id},Flagship=v.Id});}f.NextFleetId=65;
        var extra=first.Clone();extra.Id=new(s.AllocateId());s.Vessels.Add(extra);f.Fleets[0].Members.Add(extra.Id);extra.PendingMove=s.Stars.First(star=>star.Id!=first.StationaryStarId).Id;
        var prepared=FleetTurnCoordinator.NormalizeSafety(g.Content,s,s.Empires[0].Id);Assert.True(prepared.Succeeded);FleetFixtures.Valid(new(g.Content,prepared.State));Assert.Equal(64,prepared.State.Empires[0].Fleets!.Fleets.Count);Assert.Equal(65,prepared.State.Empires[0].Fleets!.NextFleetId);Assert.Null(prepared.State.FindVessel(extra.Id)!.Fleet);Assert.Equal(first.Id,prepared.State.Empires[0].Fleets!.Fleets[0].Flagship);
    }
    [Theory]
    [InlineData("harl",false)]
    [InlineData("wenth",false)]
    [InlineData("harl",true)]
    [InlineData("wenth",true)]
    public void FounderPostsOfficerWithoutLossOrChangingWenthSource(string species,bool survivor)
    {
        var g=FleetFixtures.Game(species);var s=g.State;var ship=s.Vessels[0];ship.RoleId=g.Content.Vessels.Values.First(v=>v.CanFoundColony&&g.Content.Species[species].AllowedVesselIds.Contains(v.Id)).Id;ship.CarriedPopulation=g.Content.Vessels[ship.RoleId].EmbarkPopulation;ship.Scout=null;
        var target=s.Planets.First(p=>!s.Colonies.Any(c=>c.PlanetId==p.Id)&&g.Content.PlanetTypes[p.PlanetTypeId].Colonizable&&g.Content.PlanetTypes[p.PlanetTypeId].SupportPerWorker>0&&Species.SpeciesRules.HabitatAllowed(g.Content,s,s.Empires[0].Id,p.EnvironmentId!,g.Content.Vessels[ship.RoleId].FoundsSealedHabitat?Species.HabitatModes.Sealed:Species.HabitatModes.Native));
        // Board at home before moving this labelled supplement to its surveyed destination.
        var members=ImmutableArray.Create(ship.Id);if(survivor){var other=ship.Clone();other.Id=new(s.AllocateId());s.Vessels.Add(other);members=members.Add(other.Id);}
        var made=FleetCommandTests.Run(g,new FleetAction.Create("Colony command",members));Assert.True(made.Accepted,made.Rejection?.Message);g=new(g.Content,made.State);
        var aboard=FleetOfficerTests.Run(g,new OfficerAction.AssignFlagship(new(1),new(1)));Assert.True(aboard.Accepted,aboard.Rejection?.Message);g=new(g.Content,aboard.State);foreach(var id in members)g.State.FindVessel(id)!.StationaryStarId=target.StarId;ObservationUpdater.Refresh(g.State,0);var source=g.State.Empires[0].Fleets!.Officers[0].SourceBody;
        var founding=CommandProcessor.Execute(g.Content,g.State,new SetFoundingOrderCommand(g.State.Empires[0].Id,0,ship.Id,target.Id));Assert.True(founding.Accepted,founding.Rejection?.Message);
        var result=TurnResolver.Resolve(g.Content,founding.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var officer=result.State.Empires[0].Fleets!.Officers[0];Assert.Equal(OfficerStatus.Posted,officer.Status);Assert.Equal(result.State.Colonies.Single(c=>c.PlanetId==target.Id).Id,officer.PostedColony);Assert.Equal(source,officer.SourceBody);Assert.Equal(0,officer.Xp);Assert.Equal(0,result.State.Empires[0].Fleets!.RecoveryRollCount);Assert.Null(result.State.FindVessel(ship.Id));
        if(survivor){var fleet=Assert.Single(result.State.Empires[0].Fleets!.Fleets);Assert.Equal(members[1],fleet.Flagship);Assert.Null(fleet.Officer);Assert.Equal(FleetOrderKind.Hold,fleet.Order.Kind);}else Assert.Empty(result.State.Empires[0].Fleets!.Fleets);
    }
}
