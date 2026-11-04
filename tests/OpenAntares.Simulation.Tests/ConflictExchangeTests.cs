using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictExchangeTests
{
    internal static ConflictBattleFrame Duel(int lowHull=100,int highHull=100)
    {
        var g=ConflictFixtures.WithEncounter();var e=g.State.Conflict!.Encounters[0];var low=new CombatShipFrame(e.LowSide.Members[0],"escort",lowHull,100,100,24,4,10,true,0);var high=new CombatShipFrame(e.HighSide.Members[0],"escort",highHull,100,100,24,4,10,true,0);
        return new(e.Id,e.Pair,e.Star,0,0,ConflictPhase.Pending,ImmutableArray.Create(low),ImmutableArray.Create(high),ConflictOrders.Default,ConflictOrders.Default){Definition=g.Content.Combat!};
    }
    [Fact] public void OperationalAndCrippledEscortsFireSimultaneously()
    {
        var ordinary=ConflictExchange.Calculate(Duel());Assert.Single(ordinary.Low);Assert.Equal(80,ordinary.Low[0].After);Assert.Equal(80,ordinary.High[0].After);Assert.Equal(2,ordinary.Shots.Length);Assert.All(ordinary.Shots,s=>{Assert.False(s.Source.Sentry);Assert.False(s.Target.Sentry);});
        var crippled=ConflictExchange.Calculate(Duel(20));Assert.Equal(0,crippled.Low[0].After);Assert.Equal(98,crippled.High[0].After);
    }
    [Fact] public void FrozenLowestTargetReceivesEveryShotWithoutSpillOrRetarget()
    {
        var f=Duel(highHull:1);var extra=f.High[0] with{VesselId=new(f.High[0].VesselId.Value+100),Hull=100};var own=f.Low[0] with{VesselId=new(f.Low[0].VesselId.Value+200)};
        f=f with{Low=f.Low.Add(own),High=f.High.Add(extra)};var result=ConflictExchange.Calculate(f);Assert.Equal(0,result.High[0].After);Assert.Equal(100,result.High[1].After);Assert.Equal(4,result.Shots.Length);
        Assert.Equal(0,ConflictExchange.Calculate(Duel(1,1)).Low[0].After);
        var reversed=ConflictExchange.Calculate(f with{Low=f.Low.Reverse().ToImmutableArray(),High=f.High.Reverse().ToImmutableArray()});Assert.Equal(result.Low.ToArray(),reversed.Low.ToArray());Assert.Equal(result.High.ToArray(),reversed.High.ToArray());Assert.Equal(result.Shots.ToArray(),reversed.Shots.ToArray());
    }
    [Fact] public void ArmorAndWideTotalsNeverCreateNegativeHullOrSkipAnArmedShot()
    {
        var f=Duel();f=f with{Low=ImmutableArray.Create(f.Low[0] with{Attack=1}),High=ImmutableArray.Create(f.High[0] with{Armor=int.MaxValue})};var result=ConflictExchange.Calculate(f);Assert.Equal(99,result.High[0].After);
        var low=Enumerable.Range(1,32).Select(i=>f.Low[0] with{VesselId=new(i),Attack=int.MaxValue}).ToImmutableArray();var high=f.High[0] with{VesselId=new(100),Armor=0};result=ConflictExchange.Calculate(f with{Low=low,High=ImmutableArray.Create(high)});Assert.Equal(0,result.High[0].After);Assert.Equal(100,result.High[0].ActualLoss);Assert.Equal(33,result.Shots.Length);
    }
    [Theory][InlineData(CombatDoctrine.Cautious,100,100,false)][InlineData(CombatDoctrine.Cautious,40,100,true)][InlineData(CombatDoctrine.Press,40,100,false)][InlineData(CombatDoctrine.Press,100,40,true)][InlineData(CombatDoctrine.HitAndRun,100,100,true)][InlineData(CombatDoctrine.Hold,40,0,false)]
    public void AppliedDamageAndDoctrineUseActualHullAndCohesion(CombatDoctrine doctrine,int hull,int cohesion,bool withdraw)
    {
        var g=ConflictFixtures.WithEncounter();var e=g.State.Conflict!.Encounters[0];e.LowSide.Orders=ConflictOrders.Default with{Doctrine=doctrine};var ship=g.State.FindVessel(e.LowSide.Members[0])!;ship.Combat!.Hull=hull;ship.Combat.Cohesion=cohesion;
        var result=ConflictMatchingTests.Match(g);int ids=g.State.NextEntityId;ulong random=g.State.Random.State;ConflictExchange.Apply(g.Content,g.State,result.Candidate,result.Context);
        var after=result.Candidate.Conflict!.Encounters[0];Assert.Equal(1,after.Round);Assert.Equal(g.State.Turn,after.LastResolvedTurn);Assert.Equal(withdraw,after.LowSide.WithdrawQueued);Assert.Equal(hull-20,result.Candidate.FindVessel(ship.Id)!.Combat!.Hull);Assert.Equal(ids,result.Candidate.NextEntityId);Assert.Equal(random,result.Candidate.Random.State);Assert.Throws<InvalidOperationException>(()=>ConflictExchange.Apply(g.Content,g.State,result.Candidate,result.Context));
    }
    [Fact] public void ProtectedCiviliansRemainUntouchedAndMutualDestructionClosesSafely()
    {
        var g=ConflictFixtures.WithMixedDefense();var e=g.State.Conflict!.Encounters[0];var civilian=g.State.FindVessel(e.HighSide.ProtectedCivilians[0])!;int hull=civilian.Combat!.Hull;int cargo=civilian.CarriedPopulation;var target=g.State.FindVessel(e.HighSide.Members[0])!;g.State.FindVessel(e.LowSide.Members[0])!.Combat!.Hull=1;target.Combat!.Hull=1;
        var result=ConflictMatchingTests.Match(g);ConflictExchange.Apply(g.Content,g.State,result.Candidate,result.Context);Assert.Equal(ConflictPhase.Completed,result.Candidate.Conflict!.Encounters[0].Phase);Assert.Empty(result.Candidate.Conflict.Encounters[0].LowSide.Members);Assert.Empty(result.Candidate.Conflict.Encounters[0].HighSide.Members);Assert.Equal(hull,result.Candidate.FindVessel(civilian.Id)!.Combat!.Hull);Assert.Equal(cargo,result.Candidate.FindVessel(civilian.Id)!.CarriedPopulation);
    }
    [Fact] public void HealthPauseAndAcceptedPeacePreventAnyFinalExchange()
    {
        var g=ConflictFixtures.WithEncounter();g.State.Conflict!.Encounters[0].Phase=ConflictPhase.Active;var candidate=g.State.Clone();var context=ConflictResolutionContext.Capture(g.Content,g.State);candidate.FindVessel(g.State.Conflict.Encounters[0].LowSide.Members[0])!.Combat!.Hull--;
        ConflictRelations.Prepare(g.Content,g.State,candidate,context);ConflictMatching.Resolve(g.Content,g.State,candidate,context);ConflictExchange.Apply(g.Content,g.State,candidate,context);Assert.Equal(0,candidate.Conflict!.Encounters[0].Round);
        g=ConflictFixtures.WithEncounter();foreach(var actor in g.State.Empires.Select(x=>x.Id).ToArray())ConflictDeclarationTests.Issue(g,actor,new ConflictAction.ProposePeace(g.State.Empires.Single(x=>x.Id!=actor).Id));var result=ConflictMatchingTests.Match(g);ConflictExchange.Apply(g.Content,g.State,result.Candidate,result.Context);Assert.Empty(result.Context.Battles);Assert.Equal(0,result.Candidate.Conflict!.Encounters[0].Round);Assert.All(result.Candidate.Vessels.Where(v=>v.Design is not null),v=>Assert.Equal(100,v.Combat!.Hull));
    }
    [Fact] public void ConditionalVolleyPreviewNeverReadsHiddenEnemyCohesionOrDoctrine()
    {
        var g=ConflictOrderTests.LocalForces();var a=g.State.Empires[0].Id;var action=ConflictOrderTests.Attack(g);var preview=ConflictQueries.PreviewVolley(g.Content,g.View(a),action);Assert.NotNull(preview);Assert.True(preview.Conditional);Assert.Equal(80,preview.Own[0].After);Assert.Equal(80,preview.Opposing[0].After);
        string before=System.Text.Json.JsonSerializer.Serialize(preview);g.State.FindVessel(action.Target)!.Combat!.Cohesion=1;g.State.Conflict!.Owners.Single(o=>o.Owner!=a).Preferences.Add(new(null,action.Target,ConflictOrders.Default with{Doctrine=CombatDoctrine.Hold},1));Assert.Equal(before,System.Text.Json.JsonSerializer.Serialize(ConflictQueries.PreviewVolley(g.Content,g.View(a),action)));
        Assert.DoesNotContain("Cohesion",before);Assert.DoesNotContain("Doctrine",before);Assert.Contains("conditional",preview.Explanation,StringComparison.OrdinalIgnoreCase);
    }
    [Fact] public void CohesionLossIsCohortLocalAndParticipatingFlagshipLossCountsOnce()
    {
        var frame=Duel(1);var reserve=frame.Low[0] with{VesselId=new(frame.Low[0].VesselId.Value+100),Hull=100,Flagship=false};frame=frame with{Low=frame.Low.Add(reserve)};var result=ConflictExchange.Calculate(frame);Assert.Equal(70,result.LowForecasts.Single(f=>f.Id==reserve.VesselId).CohesionAfter);Assert.Equal(100,result.HighForecasts[0].CohesionAfter);
        var protectedFlagship=ConflictExchange.Calculate(frame with{Low=frame.Low.Select(s=>s with{Flagship=false}).ToImmutableArray()});Assert.Equal(80,protectedFlagship.LowForecasts.Single(f=>f.Id==reserve.VesselId).CohesionAfter);
        var lowCohesion=ConflictExchange.Calculate(frame with{Low=frame.Low.Select(s=>s with{Cohesion=1}).ToImmutableArray()});Assert.Equal(result.Shots.ToArray(),lowCohesion.Shots.ToArray());
    }
    [Fact] public void PublicGroupDamageChangeInvalidatesTheOwnAttackToken()
    {
        var g=ConflictOrderTests.LocalForces(true);var a=g.State.Empires[0].Id;var action=ConflictOrderTests.Attack(g);var target=g.State.FindVessel(action.Target)!;var extra=ConflictFixtures.AddEscort(g,target.EmpireId,target.StationaryStarId!.Value);var fleet=g.State.FindEmpire(target.EmpireId)!.Fleets!.Fleets.Single(f=>f.Id==target.Fleet);extra.Fleet=fleet.Id;fleet.Members.Add(extra.Id);fleet.Members.Sort();ConflictObservations.Refresh(g.Content,g.State);
        string token=ConflictTokens.For(g.Content,g.View(a),action);extra.Combat!.Hull--;ConflictObservations.Refresh(g.Content,g.State);Assert.NotEqual(token,ConflictTokens.For(g.Content,g.View(a),action));
    }
    [Fact] public void OneHullCannotParticipateInTwoVolleysInOneContext()
    {
        var g=ConflictFixtures.WithEncounter();var result=ConflictMatchingTests.Match(g);ConflictExchange.Apply(g.Content,g.State,result.Candidate,result.Context);
        var duplicate=result.Candidate.Clone().Conflict!.Encounters[0];duplicate.Id=new(2);duplicate.LastResolvedTurn=null;duplicate.Round=0;duplicate.Rounds.Clear();duplicate.Phase=ConflictPhase.Pending;result.Candidate.Conflict!.Encounters.Add(duplicate);result.Candidate.Conflict.NextEncounterId=3;
        var frame=result.Context.Battles[0];frame=frame with{Encounter=duplicate.Id,Low=frame.Low.Select(s=>s with{Hull=80}).ToImmutableArray(),High=frame.High.Select(s=>s with{Hull=80}).ToImmutableArray()};string bytes=ConflictFixtures.Canonical(result.Candidate);
        Assert.Throws<InvalidOperationException>(()=>ConflictExchange.ApplyResult(g.Content,result.Candidate,result.Context,frame,ConflictExchange.Calculate(frame),false));Assert.Equal(bytes,ConflictFixtures.Canonical(result.Candidate));
    }
}
