using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatRepairTests
{
    // Labelled pre-session return/repair supplements, not earned damage or construction claims.
    static LoadedGame Returning(CombatArrivalOrder arrival=CombatArrivalOrder.Repair,int count=1)
    {
        var g=CombatFixtures.Game(enabled:false);var s=g.State;s.Turn=11;var owner=s.Empires[0];var port=s.Colonies[0];var home=s.FindPlanet(port.PlanetId)!.StarId;var origin=s.Stars.First(st=>st.Id!=home).Id;var first=s.Vessels[0];s.Vessels.Clear();
        for(int i=0;i<count;i++)
        {
            var ship=i==0?first:new VesselState{Id=new(s.AllocateId()),EmpireId=owner.Id,Crisis=new()};ship.RoleId="escort";ship.StationaryStarId=null;ship.Transit=new(origin,home,2,1);CombatInitializer.InitializeVessel(g.Content,9,ship);ScoutInitializer.InitializeVessel(g.Content,9,ship);ship.Combat!.Hull=46;ship.Combat.Cohesion=30;ship.Combat.HullLost=54;ship.Combat.ReturnColony=port.Id;ship.Combat.OnArrival=arrival;s.Vessels.Add(ship);
        }
        ObservationUpdater.Refresh(s,s.Turn);return CombatFixtures.Valid(g);
    }
    static LoadedGame Tick(LoadedGame g,Action<GameState>? health=null)
    {
        var snapshot=g.State;var candidate=snapshot.Clone();var context=CombatResolutionContext.Capture(g.Content,snapshot);health?.Invoke(candidate);MovementResolver.Apply(g.Content,snapshot,candidate,new(),fleetContext:context.FleetContext);CombatRepair.Apply(g.Content,snapshot,candidate,context);CombatRepair.ObserveArrivals(candidate,context);candidate.Turn++;ObservationUpdater.Refresh(candidate,candidate.Turn);return new(g.Content,candidate);
    }
    [Fact] public void ArrivalThenFourFullRepairTicks()
    {
        var g=Returning();g=Tick(g);Assert.Equal(12,g.State.Turn);var ship=Assert.Single(g.State.Vessels);Assert.Equal((46,30),(ship.Combat!.Hull,ship.Combat.Cohesion));Assert.True(ship.Combat.Repairing);Assert.Equal(0,ship.Combat.HullRepaired);
        foreach(var expected in new[]{(71,50),(96,70),(100,90),(100,100)}){g=Tick(g);ship=g.State.Vessels[0];Assert.Equal(expected,(ship.Combat!.Hull,ship.Combat.Cohesion));}
        Assert.Equal(16,g.State.Turn);Assert.False(ship.Combat!.Repairing);Assert.Null(ship.Combat.ReturnColony);Assert.Null(ship.Combat.OnArrival);Assert.Equal(54,ship.Combat.HullRepaired);Assert.Equal(54,g.State.Empires[0].Combat!.LifetimeHullRepaired);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
    }
    [Fact] public void ArrivalHoldAndManualStopNeverGrantRepair()
    {
        var hold=Returning(CombatArrivalOrder.Hold);hold=Tick(hold);hold=Tick(hold);Assert.Equal((46,30),(hold.State.Vessels[0].Combat!.Hull,hold.State.Vessels[0].Combat!.Cohesion));Assert.False(hold.State.Vessels[0].Combat!.Repairing);Assert.Null(hold.State.Vessels[0].Combat!.ReturnColony);
        var repair=Tick(Returning());var stop=new CombatAction.SetRepair(ImmutableArray.Create(repair.State.Vessels[0].Id),false);var stopped=CombatCommandTests.Commit(repair,stop);var after=Tick(new(repair.Content,stopped));Assert.Equal(46,after.State.Vessels[0].Combat!.Hull);Assert.False(after.State.Vessels[0].Combat!.Repairing);
    }
    [Fact] public void ThirtyReturnsPreserveEconomyPopulationAndOfficerRecords()
    {
        var g=Returning(count:30);string economy=JsonSerializer.Serialize(g.State.Colonies);string officers=JsonSerializer.Serialize(g.State.Empires[0].Fleets);long reserve=g.State.Empires[0].ResearchReserve;
        for(int i=0;i<5;i++)g=Tick(g);Assert.Equal(30,g.State.Vessels.Count);Assert.All(g.State.Vessels,v=>{Assert.Equal(100,v.Combat!.Hull);Assert.Equal(100,v.Combat.Cohesion);Assert.False(v.Combat.Repairing);});Assert.Equal(economy,JsonSerializer.Serialize(g.State.Colonies));Assert.Equal(officers,JsonSerializer.Serialize(g.State.Empires[0].Fleets));Assert.Equal(reserve,g.State.Empires[0].ResearchReserve);
    }
    [Fact] public void ManualMovementAndPersistentHoldOverrideRepairBeforeTick()
    {
        var g=Tick(Returning());var ship=g.State.Vessels[0];var owner=ship.EmpireId;var target=g.State.Stars.First(st=>st.Id!=ship.StationaryStarId).Id;var move=CommandProcessor.Execute(g.Content,g.State,new SetMoveOrderCommand(owner,g.State.Turn,ship.Id,target));Assert.True(move.Accepted,move.Rejection?.Message);Assert.False(move.State.Vessels[0].Combat!.Repairing);Assert.Null(move.State.Vessels[0].Combat!.ReturnColony);
        ship.Fleet=new(1);g.State.Empires[0].Fleets!.Fleets.Add(new(){Id=new(1),Name="Repair fixture",Members=new(){ship.Id},Flagship=ship.Id});g.State.Empires[0].Fleets!.NextFleetId=2;var hold=CommandProcessor.Execute(g.Content,g.State,new FleetCommand(owner,g.State.Turn,new FleetAction.SetOrder(ImmutableArray.Create(new FleetId(1)),new(FleetOrderKind.Hold))));Assert.True(hold.Accepted,hold.Rejection?.Message);Assert.True(hold.Changed);Assert.False(hold.State.Vessels[0].Combat!.Repairing);
    }
    [Fact] public void CapturedEligibilityExcludesArrivalNewCompletionAndDuplicateTick()
    {
        var g=Tick(Returning());var snapshot=g.State;var candidate=snapshot.Clone();var context=CombatResolutionContext.Capture(g.Content,snapshot);Assert.Contains(snapshot.Vessels[0].Id,context.RepairCapture);
        // Candidate-only supplement proves no same-boundary eligibility, regardless of its initial damage.
        var added=candidate.Vessels[0].Clone();added.Id=new(candidate.AllocateId());added.NewlyCompleted=true;candidate.Vessels.Add(added);CombatRepair.Apply(g.Content,snapshot,candidate,context);Assert.Equal(46,added.Combat!.Hull);Assert.Equal(71,candidate.Vessels[0].Combat!.Hull);var before=CombatFixtures.Bytes(candidate);Assert.Throws<InvalidOperationException>(()=>CombatRepair.Apply(g.Content,snapshot,candidate,context));Assert.Equal(before,CombatFixtures.Bytes(candidate));
        var again=CombatResolutionContext.Capture(g.Content,snapshot);CombatRepair.Apply(g.Content,snapshot,candidate,again);Assert.Equal(before,CombatFixtures.Bytes(candidate));
    }
    [Fact] public void RepairChecksPortEachTickAndNeverCuresCargo()
    {
        var g=Tick(Returning());var s=g.State;CrisisInitializer.Initialize(g.Content,s,true);var ship=s.Vessels[0];ship.RoleId="harl_settler";ship.CarriedPopulation=g.Content.Vessels[ship.RoleId].EmbarkPopulation;ship.Scout=null;var col=s.Colonies[0];var d=g.Content.Plagues.Values.Single();var e=s.Empires[0].Crisis!;e.NextEpisodeNumber=2;e.Episode=new(){Id=1,FamilyId=d.Id,Source=CrisisQueries.Location(s,col),SourcePopulation=col.Population,CureCost=checked(d.CureBase+d.CurePerPopulation*col.Population)};ship.Crisis!.CarrierEpisode=1;CombatFixtures.Valid(g);
        string health=JsonSerializer.Serialize(ship.Crisis);var blocked=Tick(g,c=>c.Colonies[0].Crisis!.Quarantined=true);Assert.Equal(46,blocked.State.Vessels[0].Combat!.Hull);Assert.Equal(health,JsonSerializer.Serialize(blocked.State.Vessels[0].Crisis));var repaired=Tick(g);Assert.Equal(71,repaired.State.Vessels[0].Combat!.Hull);Assert.Equal(health,JsonSerializer.Serialize(repaired.State.Vessels[0].Crisis));
    }
    [Fact] public void CeilQuarterRepairUsesEmbeddedNonHundredHullProfile()
    {
        var initial=Returning();var definition=initial.Content.Combat! with {RoleProfiles=initial.Content.Combat.RoleProfiles.SetItem("escort",new(101,24,4))};var g=new LoadedGame(initial.Content with {Combat=definition},initial.State);g.State.Vessels[0].Combat!.HullLost=55;g=Tick(g);g=Tick(g);Assert.Equal(72,g.State.Vessels[0].Combat!.Hull);Assert.Equal(26,g.State.Vessels[0].Combat!.HullRepaired);
    }
    [Fact] public void CompletedConstructionProvenanceNeverReopensOrResendsAfterRepair()
    {
        var g=Tick(Returning());var s=g.State;var ship=s.Vessels[0];var f=s.Empires[0].Fleets!;var colony=s.Colonies[0];var star=s.FindPlanet(colony.PlanetId)!.StarId;ship.Fleet=new(1);ship.NewlyCompleted=true;ship.BuildOrigin=new(new(1),1,colony.Id,1);f.Fleets.Add(new(){Id=new(1),Name="Completed fixture",Members=new(){ship.Id},Flagship=ship.Id});f.NextFleetId=2;f.Requests.Add(new(){Id=new(1),NextSlotOrdinal=2,Name="Completed quota",Status=RequestStatus.Complete,CompletedTurn=s.Turn,Rally=star,RallyFleet=new(1),Colonies=new(){colony.Id},Slots=new(){new(){Ordinal=1,RoleId=ship.RoleId,Colony=colony.Id,Vessel=ship.Id,NewHull=true,Status=SlotStatus.Arrived}},HasAllocated=true});f.NextRequestId=2;f.HullCredits.Add(ship.Id);CombatFixtures.Valid(g);string before=JsonSerializer.Serialize(f);for(int i=0;i<4;i++)g=Tick(g);Assert.Equal(before,JsonSerializer.Serialize(g.State.Empires[0].Fleets));Assert.Null(g.State.Vessels[0].PendingMove);Assert.Null(g.State.Vessels[0].Transit);Assert.Equal(RequestStatus.Complete,g.State.Empires[0].Fleets!.Requests[0].Status);
    }
    [Fact] public void LostDestinationCancelsFiniteAuthorityWithoutRetargeting()
    {
        var g=Returning();var context=CombatResolutionContext.Capture(g.Content,g.State);var candidate=g.State.Clone();var col=candidate.Colonies[0];CrisisLoss.Remove(candidate,col.Id,CrisisQueries.Location(candidate,col));CombatRepair.Apply(g.Content,g.State,candidate,context);Assert.Null(candidate.Vessels[0].Combat!.ReturnColony);Assert.Null(candidate.Vessels[0].Combat!.OnArrival);Assert.False(candidate.Vessels[0].Combat!.Repairing);Assert.NotNull(candidate.Vessels[0].Transit);
    }
}
