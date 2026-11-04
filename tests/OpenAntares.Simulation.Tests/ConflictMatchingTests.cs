using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictMatchingTests
{
    internal static (GameState Candidate,ConflictResolutionContext Context) Match(GameSession g)
    {var candidate=g.State.Clone();var context=ConflictResolutionContext.Capture(g.Content,g.State);ConflictRelations.Prepare(g.Content,g.State,candidate,context);ConflictMatching.Resolve(g.Content,g.State,candidate,context);return(candidate,context);}
    [Fact] public void MixedDefenseAndSentryMatchingConsumeEachArmedHullOnce()
    {
        var g=ConflictOrderTests.LocalForces(true);var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var action=ConflictOrderTests.Attack(g);var civilian=g.State.FindEmpire(b)!.Fleets!.Fleets.Single(f=>f.Id==g.State.FindVessel(action.Target)!.Fleet).Flagship;
        ConflictDeclarationTests.Issue(g,a,action);var result=Match(g);var battle=Assert.Single(result.Candidate.Conflict!.Encounters);Assert.Contains(action.Target,battle.HighSide.Members);Assert.DoesNotContain(civilian,battle.LowSide.Members.Concat(battle.HighSide.Members));Assert.Contains(civilian,battle.HighSide.ProtectedCivilians);Assert.Empty(g.State.Conflict!.Encounters);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void CrossedAttacksCollapseRegardlessOfSubmissionOrder(bool reverse)
    {
        var g=ConflictOrderTests.LocalForces();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var first=ConflictOrderTests.Attack(g);var target=g.State.FindVessel(first.Target)!;var data=g.State.FindEmpire(b)!.Fleets!;
        var fleet=new FleetRecord{Id=new(data.NextFleetId++),Name="Labelled second attacker",Members=new(){target.Id},Flagship=target.Id};data.Fleets.Add(fleet);target.Fleet=fleet.Id;
        var returnPlanet=g.State.FindPlanet(g.State.Colonies.First(c=>c.EmpireId==a).PlanetId)!;var ownPort=g.State.Colonies.First(c=>c.EmpireId==b);
        // Move both forces to a neutral system so each has an explicit different return port.
        var star=g.State.Stars.First(s=>s.Id!=returnPlanet.StarId&&s.Id!=g.State.FindPlanet(ownPort.PlanetId)!.StarId).Id;g.State.FindVessel(g.State.FindEmpire(a)!.Fleets!.Fleets[0].Members[0])!.StationaryStarId=target.StationaryStarId=star;ObservationUpdater.Refresh(g.State,g.State.Turn);ConflictObservations.Refresh(g.Content,g.State);
        var second=new ConflictAction.Attack(fleet.Id,g.State.FindEmpire(a)!.Fleets!.Fleets[0].Flagship,ConflictOrders.Default with{ReturnColony=ownPort.Id});
        if(reverse){ConflictDeclarationTests.Issue(g,b,second);ConflictDeclarationTests.Issue(g,a,first);}else{ConflictDeclarationTests.Issue(g,a,first);ConflictDeclarationTests.Issue(g,b,second);}
        var result=Match(g);Assert.Single(result.Candidate.Conflict!.Encounters);Assert.Empty(result.Candidate.Conflict.Owners.SelectMany(o=>o.Attacks));
    }
    [Fact] public void HealthLossCancelsPendingMatchWithoutSubstitutingTarget()
    {
        var g=ConflictOrderTests.LocalForces();var a=g.State.Empires[0].Id;var action=ConflictOrderTests.Attack(g);ConflictDeclarationTests.Issue(g,a,action);var next=g.State.Clone();next.FindVessel(action.Target)!.Combat!.Hull--;
        var context=ConflictResolutionContext.Capture(g.Content,g.State);ConflictRelations.Prepare(g.Content,g.State,next,context);ConflictMatching.Resolve(g.Content,g.State,next,context);Assert.Empty(next.Conflict!.Encounters);Assert.Empty(next.Conflict.Owners.SelectMany(o=>o.Attacks));Assert.Equal(99,next.FindVessel(action.Target)!.Combat!.Hull);
    }
    [Fact] public void ActiveForceLossPausesOnceAndRequiresOnlyOwnRevisedForceResponse()
    {
        var g=ConflictFixtures.WithEncounter();var battle=g.State.Conflict!.Encounters[0];battle.Phase=ConflictPhase.Active;var next=g.State.Clone();next.FindVessel(battle.LowSide.Members[0])!.Combat!.Hull--;
        var context=ConflictResolutionContext.Capture(g.Content,g.State);ConflictRelations.Prepare(g.Content,g.State,next,context);ConflictMatching.Resolve(g.Content,g.State,next,context);var after=next.Conflict!.Encounters[0];Assert.True(after.LowSide.NeedsResponse);Assert.False(after.HighSide.NeedsResponse);Assert.Equal(0,after.Round);Assert.Throws<InvalidOperationException>(()=>ConflictMatching.Resolve(g.Content,g.State,next,context));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void ThreeForcesCompeteByLowestVesselRatherThanSubmissionOrder(bool reverse)
    {
        var g=ConflictOrderTests.LocalForces();var a=g.State.Empires[0].Id;var first=ConflictOrderTests.Attack(g);var ship=ConflictFixtures.AddEscort(g,a,g.State.FindVessel(first.Target)!.StationaryStarId!.Value);var data=g.State.FindEmpire(a)!.Fleets!;var fleet=new FleetRecord{Id=new(data.NextFleetId++),Name="Labelled competing force",Members=new(){ship.Id},Flagship=ship.Id};data.Fleets.Add(fleet);ship.Fleet=fleet.Id;var second=first with{Fleet=fleet.Id};
        foreach(var action in reverse?new[]{second,first}:new[]{first,second})ConflictDeclarationTests.Issue(g,a,action);
        int ids=g.State.NextEntityId;ulong random=g.State.Random.State;var result=Match(g);var battle=Assert.Single(result.Candidate.Conflict!.Encounters);Assert.Equal(first.Fleet,battle.LowSide.Fleet);Assert.Equal(2,result.Context.UsedVessels.Count);Assert.Single(result.Candidate.Conflict.Owners.Single(o=>o.Owner==a).Events,e=>e.Kind=="attack_unavailable");Assert.Equal(ids,result.Candidate.NextEntityId);Assert.Equal(random,result.Candidate.Random.State);
    }
    [Fact] public void DefenderMaySplitAfterPrivateAttackButCannotShieldItsAnchor()
    {
        var g=ConflictOrderTests.LocalForces(true);var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var attack=ConflictOrderTests.Attack(g);var group=g.State.FindVessel(attack.Target)!.Fleet!.Value;ConflictDeclarationTests.Issue(g,a,attack);
        var command=new FleetCommand(b,g.State.Turn,new FleetAction.Split(group,"Labelled split defender",ImmutableArray.Create(attack.Target),attack.Target));var result=g.Execute(command);Assert.True(result.Accepted,result.Rejection?.Message);var actual=g.State.FindVessel(attack.Target)!.Fleet;var match=Assert.Single(Match(g).Candidate.Conflict!.Encounters);Assert.Equal(actual,match.HighSide.Fleet);Assert.Contains(attack.Target,match.HighSide.Members);
    }
    internal static GameSession WithPendingSentry()
    {
        var g=ConflictOrderTests.LocalForces();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var attack=ConflictOrderTests.Attack(g);var target=g.State.FindVessel(attack.Target)!;var star=g.State.Stars.First(s=>g.State.Colonies.All(c=>g.State.FindPlanet(c.PlanetId)!.StarId!=s.Id)).Id;
        target.StationaryStarId=star;foreach(var id in g.State.FindEmpire(a)!.Fleets!.Fleets[0].Members)g.State.FindVessel(id)!.StationaryStarId=star;
        var data=g.State.FindEmpire(b)!.Fleets!;var fleet=new FleetRecord{Id=new(data.NextFleetId++),Name="Labelled sentry defender",Members=new(){target.Id},Flagship=target.Id};data.Fleets.Add(fleet);target.Fleet=fleet.Id;
        var combat=g.State.Combat!;combat.SentriesEnabled=true;combat.NextPocketId=2;combat.NextEncounterId=2;var pocket=new PocketState{Id=new(1),Star=star,NextSentryId=g.Content.Combat!.SentriesPerPocket+1};for(int i=1;i<pocket.NextSentryId;i++)pocket.Sentries.Add(new(){Id=new(i),Profile=g.Content.Combat.SentryProfile,Hull=g.Content.Combat.SentryProfile.MaxHull});combat.Pockets.Add(pocket);
        var orders=new CombatOrders(CombatDoctrine.Cautious,g.State.Colonies.First(c=>c.EmpireId==b).Id,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair);combat.Encounters.Add(new(){Id=new(1),Owner=b,Fleet=fleet.Id,Pocket=pocket.Id,Members=new(){target.Id},Orders=orders,Phase=CombatPhase.Pending,ConsentRevision=1});g.State.FindEmpire(b)!.Combat!.LifetimeEncounters=1;
        ObservationUpdater.Refresh(g.State,g.State.Turn);ConflictObservations.Refresh(g.Content,g.State);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));return g;
    }
    [Fact] public void PendingSentryReservationYieldsWithoutASecondBattle()
    {
        var g=WithPendingSentry();var a=g.State.Empires[0].Id;ConflictDeclarationTests.Issue(g,a,ConflictOrderTests.Attack(g));var result=Match(g);Assert.Single(result.Candidate.Conflict!.Encounters);Assert.Empty(result.Candidate.Combat!.Encounters);Assert.Equal(2,result.Context.UsedVessels.Count);Assert.Equal(2,result.Candidate.Combat.NextEncounterId);Assert.All(result.Candidate.Vessels.Where(v=>result.Context.UsedVessels.Contains(v.Id)),v=>Assert.Equal(100,v.Combat!.Hull));
    }
    [Fact] public void ActiveSentryForceCannotBeEngagedAgain()
    {
        var g=WithPendingSentry();var a=g.State.Empires[0].Id;g.State.Combat!.Encounters[0].Phase=CombatPhase.Active;ConflictDeclarationTests.Issue(g,a,ConflictOrderTests.Attack(g));var result=Match(g);Assert.Empty(result.Candidate.Conflict!.Encounters);Assert.Single(result.Candidate.Combat!.Encounters);Assert.Empty(result.Context.Battles);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void IndependentPairsAtTheSameStarMatchWithoutSharingDamageForces(bool reverse)
    {
        var g=ConflictOrderTests.LocalForces();var a=g.State.Empires[0].Id;var attack=ConflictOrderTests.Attack(g);var star=g.State.FindVessel(attack.Target)!.StationaryStarId!.Value;var c=ConflictFixtures.AddNeutralObserver(g,star);var d=ConflictFixtures.AddNeutralObserver(g,star);
        ConflictDeclarationTests.Issue(g,c,new ConflictAction.DeclareWar(d));g=ConflictDeclarationTests.Publish(g);
        var template=g.State.Colonies.First(x=>x.EmpireId==a);var world=g.State.FindPlanet(template.PlanetId)!;
        ColonyId Port(EmpireId owner)
        {
            var planet=g.State.Planets.First(p=>p.StarId!=star&&p.PlanetTypeId==world.PlanetTypeId&&p.EnvironmentId==world.EnvironmentId&&g.State.Colonies.All(x=>x.PlanetId!=p.Id));var colony=template.Clone();colony.Id=new(g.State.AllocateId());colony.EmpireId=owner;colony.PlanetId=planet.Id;g.State.Colonies.Add(colony);return colony.Id;
        }
        var cPort=Port(c);Port(d);var own=ConflictFixtures.AddEscort(g,c,star);var target=ConflictFixtures.AddEscort(g,d,star);var data=g.State.FindEmpire(c)!.Fleets!;var fleet=new FleetRecord{Id=new(data.NextFleetId++),Name="Labelled independent war",Members=new(){own.Id},Flagship=own.Id};data.Fleets.Add(fleet);own.Fleet=fleet.Id;
        ObservationUpdater.Refresh(g.State,g.State.Turn);ConflictObservations.Refresh(g.Content,g.State);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        var second=new ConflictAction.Attack(fleet.Id,target.Id,ConflictOrders.Default with{ReturnColony=cPort});if(reverse){ConflictDeclarationTests.Issue(g,c,second);ConflictDeclarationTests.Issue(g,a,attack);}else{ConflictDeclarationTests.Issue(g,a,attack);ConflictDeclarationTests.Issue(g,c,second);}
        var result=Match(g);Assert.Equal(2,result.Candidate.Conflict!.Encounters.Count);Assert.Equal(4,result.Context.UsedVessels.Count);Assert.Equal(2,result.Context.Battles.Length);Assert.Empty(GameStateValidator.Validate(g.Content,result.Candidate));
    }
    [Fact] public void MatchedDefenderCannotReceiveItsPreviouslyCapturedRepairTick()
    {
        var g=ConflictOrderTests.LocalForces();var a=g.State.Empires[0].Id;var attack=ConflictOrderTests.Attack(g);var target=g.State.FindVessel(attack.Target)!;target.Combat!.Cohesion=80;target.Combat.Repairing=true;target.Combat.ReturnColony=g.State.Colonies.First(c=>c.EmpireId==target.EmpireId).Id;target.Combat.OnArrival=CombatArrivalOrder.Repair;
        ConflictDeclarationTests.Issue(g,a,attack);var combat=CombatResolutionContext.Capture(g.Content,g.State);Assert.Contains(target.Id,combat.RepairCapture);var result=Match(g);combat.ReserveEmpireParticipants(result.Context);Assert.DoesNotContain(target.Id,combat.RepairCapture);Assert.Empty(combat.Consumed);Assert.Contains(target.Id,combat.EmpireParticipants);
    }
}
