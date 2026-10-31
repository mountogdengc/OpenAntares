using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.ShipDesigns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictWithdrawalTests
{
    internal static void Upgrade(GameSession g,EmpireId owner)
    {
        var e=g.State.FindEmpire(owner)!;void Learn(string id){foreach(var prerequisite in g.Content.Technologies[id].PrerequisiteTechnologyIds)Learn(prerequisite);if(!e.KnownTechnologyIds.Contains(id))e.KnownTechnologyIds.Add(id);}Learn("vector_drives");e.KnownTechnologyIds.Sort(StringComparer.Ordinal);
        var data=e.ShipDesigns!;var recipe=data.Designs[0].Recipe with{EngineId="vector_drive"};var design=new ShipDesign(new(data.NextDesignId++),"Labelled fast escort",recipe,false);data.Designs.Add(design);foreach(var v in g.State.Vessels.Where(v=>v.EmpireId==owner&&v.Design is not null))v.Design=design.Id;
    }
    internal static void AtNeutralStar(GameSession g)
    {
        var e=g.State.Conflict!.Encounters[0];e.Star=g.State.Stars.First(s=>g.State.Colonies.All(c=>g.State.FindPlanet(c.PlanetId)!.StarId!=s.Id)).Id;
        foreach(var side in new[]{e.LowSide,e.HighSide})foreach(var id in side.Members.Concat(side.ProtectedCivilians))g.State.FindVessel(id)!.StationaryStarId=e.Star;ObservationUpdater.Refresh(g.State,g.State.Turn);ConflictObservations.Refresh(g.Content,g.State);
    }
    internal static (GameState Candidate,ConflictResolutionContext Context) Resolve(GameSession g)
    {var result=ConflictMatchingTests.Match(g);ConflictWithdrawal.Resolve(g.Content,g.State,result.Candidate,result.Context);ConflictExchange.Apply(g.Content,g.State,result.Candidate,result.Context);return result;}
    [Fact] public void BothDepartWithoutShotsButSpeedTieTakesOne()
    {
        var g=ConflictFixtures.WithEncounter();var e=g.State.Conflict!.Encounters[0];e.LowSide.WithdrawQueued=e.HighSide.WithdrawQueued=true;var both=Resolve(g);Assert.Empty(both.Context.Rounds);Assert.Equal(2,both.Context.Departures.Length);Assert.All(both.Context.Departures,d=>Assert.Null(d.Destination));
        g=ConflictFixtures.WithEncounter();e=g.State.Conflict!.Encounters[0];e.LowSide.WithdrawQueued=true;var tie=Resolve(g);var round=Assert.Single(tie.Context.Rounds);var shot=Assert.Single(round.Result.Shots);Assert.Equal(20,shot.Damage);Assert.Equal(e.HighSide.Members[0].Value,shot.Source.Id);Assert.Equal(80,tie.Candidate.FindVessel(e.LowSide.Members[0])!.Combat!.Hull);Assert.Equal(100,tie.Candidate.FindVessel(e.HighSide.Members[0])!.Combat!.Hull);Assert.Null(Assert.Single(tie.Context.Departures).Destination);Assert.Equal(ConflictPhase.Completed,tie.Candidate.Conflict!.Encounters[0].Phase);
    }
    [Fact] public void ExplicitOwnReturnUsesIndependentRoutesAndNoPositionMutationBeforeMovement()
    {
        var g=ConflictFixtures.WithEncounter();var e=g.State.Conflict!.Encounters[0];var star=g.State.Stars.First(s=>g.State.Colonies.All(c=>g.State.FindPlanet(c.PlanetId)!.StarId!=s.Id)).Id;e.Star=star;
        foreach(var side in new[]{e.LowSide,e.HighSide}){side.WithdrawQueued=true;side.Orders=ConflictOrders.Default with{ReturnColony=g.State.Colonies.First(c=>c.EmpireId==side.Owner).Id};foreach(var id in side.Members)g.State.FindVessel(id)!.StationaryStarId=star;}
        ObservationUpdater.Refresh(g.State,g.State.Turn);ConflictObservations.Refresh(g.Content,g.State);var result=Resolve(g);Assert.Equal(2,result.Context.Departures.Length);Assert.Equal(2,result.Context.Departures.Select(d=>d.Destination).Distinct().Count());Assert.All(result.Candidate.Vessels.Where(v=>v.Design is not null),v=>{Assert.Null(v.Transit);Assert.Equal(star,v.StationaryStarId);});Assert.Empty(result.Context.Rounds);
    }
    [Theory][InlineData("null")][InlineData("missing")][InlineData("colocated")][InlineData("foreign")]
    public void InvalidReturnFallsBackLocallyWithoutInventingAPort(string defect)
    {
        var g=ConflictFixtures.WithEncounter();var e=g.State.Conflict!.Encounters[0];e.LowSide.WithdrawQueued=true;var own=g.State.Colonies.First(c=>c.EmpireId==e.LowSide.Owner);e.LowSide.Orders=ConflictOrders.Default with{ReturnColony=defect switch{"missing"=>new ColonyId(g.State.NextEntityId-1),"colocated"=>own.Id,"foreign"=>g.State.Colonies.First(c=>c.EmpireId==e.HighSide.Owner).Id,_=>null}};
        int ids=g.State.NextEntityId;var result=Resolve(g);var departure=Assert.Single(result.Context.Departures);Assert.Null(departure.Destination);Assert.Equal(0,departure.TotalTurns);Assert.Equal(ids,result.Candidate.NextEntityId);Assert.All(result.Candidate.Vessels,v=>Assert.Null(v.Transit));
    }
    [Fact] public void StrictlyFasterLocalBreakIsCleanButInterceptionChangesLaunchSpeed()
    {
        var g=ConflictFixtures.WithEncounter();var e=g.State.Conflict!.Encounters[0];Upgrade(g,e.LowSide.Owner);e.LowSide.WithdrawQueued=true;var fast=Resolve(g);Assert.Empty(fast.Context.Rounds);Assert.Equal(400,Assert.Single(fast.Context.Departures).Speed);
        g=ConflictFixtures.WithEncounter();AtNeutralStar(g);e=g.State.Conflict!.Encounters[0];e.LowSide.WithdrawQueued=true;e.LowSide.Orders=ConflictOrders.Default with{ReturnColony=g.State.Colonies.First(c=>c.EmpireId==e.LowSide.Owner).Id};g.State.FindVessel(e.LowSide.Members[0])!.Combat!.Hull=60;
        var damaged=Resolve(g);Assert.Single(damaged.Context.Rounds);var route=Assert.Single(damaged.Context.Departures);Assert.NotNull(route.Destination);Assert.Equal(225,route.Speed);Assert.Equal(40,damaged.Candidate.FindVessel(e.LowSide.Members[0])!.Combat!.Hull);
    }
    [Theory][InlineData(StragglerPolicy.KeepTogether,80,true)][InlineData(StragglerPolicy.DetachCrippled,100,false)]
    public void CleanMainCohortIsExcludedFromStragglerInterception(StragglerPolicy policy,int flagshipHull,bool stragglerLives)
    {
        var g=ConflictFixtures.WithEncounter();var e=g.State.Conflict!.Encounters[0];var ship=ConflictFixtures.AddEscort(g,e.LowSide.Owner,e.Star,20);var fleet=g.State.FindEmpire(e.LowSide.Owner)!.Fleets!.Fleets.Single(f=>f.Id==e.LowSide.Fleet);fleet.Members.Add(ship.Id);ship.Fleet=fleet.Id;e.LowSide.Members.Add(ship.Id);Upgrade(g,e.LowSide.Owner);e.LowSide.WithdrawQueued=true;e.LowSide.Orders=ConflictOrders.Default with{Stragglers=policy};
        var result=Resolve(g);Assert.Equal(flagshipHull,result.Candidate.FindVessel(e.LowSide.Flagship)!.Combat!.Hull);Assert.Equal(stragglerLives,result.Candidate.FindVessel(ship.Id) is not null);Assert.Single(Assert.Single(result.Context.Rounds).Result.Shots);
    }
    [Fact] public void CrippledFlagshipStaysWithMainCohortAndCannotGrantDetachedNavigation()
    {
        var g=ConflictFixtures.WithEncounter();var e=g.State.Conflict!.Encounters[0];g.State.FindVessel(e.LowSide.Flagship)!.Combat!.Hull=20;var ship=ConflictFixtures.AddEscort(g,e.LowSide.Owner,e.Star,20);var fleet=g.State.FindEmpire(e.LowSide.Owner)!.Fleets!.Fleets.Single(f=>f.Id==e.LowSide.Fleet);fleet.Members.Add(ship.Id);ship.Fleet=fleet.Id;e.LowSide.Members.Add(ship.Id);e.LowSide.WithdrawQueued=true;e.LowSide.Orders=ConflictOrders.Default with{Stragglers=StragglerPolicy.DetachCrippled};
        var result=Resolve(g);var group=Assert.Single(result.Context.Departures);Assert.False(group.Main);Assert.Equal(new[]{ship.Id},group.Members);Assert.True(Assert.Single(result.Context.Destructions).Flagship);
    }
    [Fact] public void SlowProtectedCivilianContributesSpeedButNeverTakesInterceptionDamage()
    {
        var g=ConflictFixtures.WithMixedDefense();var e=g.State.Conflict!.Encounters[0];Upgrade(g,e.HighSide.Owner);e.HighSide.WithdrawQueued=true;var civilian=g.State.FindVessel(e.HighSide.Flagship)!;int hull=civilian.Combat!.Hull;int cargo=civilian.CarriedPopulation;
        var result=Resolve(g);Assert.Single(result.Context.Rounds);Assert.Equal(hull,result.Candidate.FindVessel(civilian.Id)!.Combat!.Hull);Assert.Equal(cargo,result.Candidate.FindVessel(civilian.Id)!.CarriedPopulation);Assert.Contains(civilian.Id,Assert.Single(result.Context.Departures).Members);Assert.All(result.Context.Rounds[0].Result.Shots,s=>Assert.NotEqual(civilian.Id.Value,s.Target.Id));
    }
    [Fact] public void QuarantinedReturnPortFallsBackToLocalBreakAndAttention()
    {
        var g=ConflictFixtures.WithEncounter();AtNeutralStar(g);var e=g.State.Conflict!.Encounters[0];e.LowSide.WithdrawQueued=true;var port=g.State.Colonies.First(c=>c.EmpireId==e.LowSide.Owner);e.LowSide.Orders=ConflictOrders.Default with{ReturnColony=port.Id};g.State.Crisis!.Enabled=true;port.Crisis!.Quarantined=true;Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        var result=Resolve(g);Assert.Null(Assert.Single(result.Context.Departures).Destination);Assert.Contains(result.Candidate.Conflict!.Owners.Single(o=>o.Owner==e.LowSide.Owner).Notices,n=>n.Level==ConflictNoticeLevel.Attention&&n.Key.StartsWith("local_break",StringComparison.Ordinal));
    }
    [Fact] public void PeaceSuppressesQueuedWithdrawalsAndEveryInterception()
    {
        var g=ConflictFixtures.WithEncounter();var e=g.State.Conflict!.Encounters[0];e.LowSide.WithdrawQueued=true;foreach(var actor in g.State.Empires.Select(x=>x.Id).ToArray())ConflictDeclarationTests.Issue(g,actor,new ConflictAction.ProposePeace(g.State.Empires.Single(x=>x.Id!=actor).Id));var result=Resolve(g);Assert.Empty(result.Context.Rounds);Assert.Empty(result.Context.Departures);Assert.All(result.Candidate.Vessels.Where(v=>v.Design is not null),v=>Assert.Equal(100,v.Combat!.Hull));
    }
    [Fact] public void NewlyAttackedDefenderKeepsItsAcceptedOrdinaryMoveAsDepartureAuthority()
    {
        var g=ConflictOrderTests.LocalForces();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var attack=ConflictOrderTests.Attack(g);var destination=g.State.Stars.First(s=>s.Id!=g.State.FindVessel(attack.Target)!.StationaryStarId).Id;
        var move=g.Execute(new OpenAntares.Simulation.Commands.SetMoveOrderCommand(b,g.State.Turn,attack.Target,destination));Assert.True(move.Accepted,move.Rejection?.Message);ConflictDeclarationTests.Issue(g,a,attack);var result=Resolve(g);var route=Assert.Single(result.Context.Departures);Assert.Equal(b,route.Owner);Assert.Equal(destination,route.Destination);Assert.Equal(CombatArrivalOrder.Hold,route.OnArrival);Assert.Single(Assert.Single(result.Context.Rounds).Result.Shots);
    }
    [Fact] public void ReturnGoalAlreadyAtCurrentSystemDoesNotInventDeparture()
    {
        var g=ConflictOrderTests.LocalForces();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var attack=ConflictOrderTests.Attack(g);var target=g.State.FindVessel(attack.Target)!;var data=g.State.FindEmpire(b)!.Fleets!;var fleet=new FleetRecord{Id=new(data.NextFleetId++),Name="Labelled arrived return",Members=new(){target.Id},Flagship=target.Id,Order=new(FleetOrderKind.Return,target.StationaryStarId)};data.Fleets.Add(fleet);target.Fleet=fleet.Id;
        ConflictDeclarationTests.Issue(g,a,attack);var result=Resolve(g);Assert.Empty(result.Context.Departures);Assert.Equal(2,Assert.Single(result.Context.Rounds).Result.Shots.Length);
    }
    [Fact] public void WithdrawalPreviewStaysConditionalAndDoesNotRevealForeignOrders()
    {
        var g=ConflictFixtures.WithEncounter();ConflictObservations.Refresh(g.Content,g.State);var a=g.State.Empires[0].Id;var e=g.View(a).Conflict!.Encounters[0];var action=new ConflictAction.Withdraw(e.Id,e.Revision);var preview=ConflictWithdrawal.Preview(g.Content,g.View(a),action);Assert.NotNull(preview);Assert.True(preview.Conditional);Assert.Null(preview.Destination);Assert.Single(preview.Cohorts);
        string before=System.Text.Json.JsonSerializer.Serialize(preview);var foreign=g.State.Conflict!.Encounters[0].HighSide;foreign.WithdrawQueued=true;foreign.Orders=foreign.Orders with{Doctrine=CombatDoctrine.Hold};g.State.FindVessel(foreign.Members[0])!.Combat!.Cohesion=1;Assert.Equal(before,System.Text.Json.JsonSerializer.Serialize(ConflictWithdrawal.Preview(g.Content,g.View(a),action)));
    }
}
