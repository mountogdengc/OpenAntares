using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatMathTests
{
    static CombatShipFrame Escort(int id=1,int hull=100)=>new(new(id),"escort",hull,100,100,24,4,300,true,0);
    static SentryFrame Sentry(int id=1,int hull=80)=>new(new(id),hull,80,18,2,250);
    static CombatFrame Frame(ImmutableArray<CombatShipFrame> ships,ImmutableArray<SentryFrame> sentries)=>new(null,0,1,new(1),new(1),ships,sentries,new(CombatDoctrine.Cautious,new(1),StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair));
    [Fact] public void SimultaneousExchangeAndBoundaryStats()
    {
        var profile=new CombatProfile(100,24,4);
        Assert.Equal((24,315),(CombatMath.Stats(profile,51,300,5).Firepower,CombatMath.Stats(profile,51,300,5).Speed));
        Assert.Equal((12,236),(CombatMath.Stats(profile,50,300,5).Firepower,CombatMath.Stats(profile,50,300,5).Speed));
        Assert.Equal((12,236),(CombatMath.Stats(profile,26,300,5).Firepower,CombatMath.Stats(profile,26,300,5).Speed));
        Assert.Equal((6,157),(CombatMath.Stats(profile,25,300,5).Firepower,CombatMath.Stats(profile,25,300,5).Speed));
        Assert.Equal(CombatDamageBand.Damaged,CombatMath.Band(new(60,0,0),30));Assert.Equal(CombatDamageBand.Crippled,CombatMath.Band(new(60,0,0),15));
        var result=CombatExchange.Calculate(Frame(ImmutableArray.Create(Escort()),ImmutableArray.Create(Sentry())),false,ImmutableArray<VesselId>.Empty);
        Assert.Equal(86,Assert.Single(result.Ships).After);Assert.Equal(58,Assert.Single(result.Sentries).After);Assert.Equal(2,result.Shots.Length);
        var dying=CombatExchange.Calculate(Frame(ImmutableArray.Create(Escort()),ImmutableArray.Create(Sentry(hull:1))),false,ImmutableArray<VesselId>.Empty);
        Assert.Equal(99,Assert.Single(dying.Ships).After);Assert.Equal(0,Assert.Single(dying.Sentries).After);Assert.Equal(1,Assert.Single(dying.DestroyedSentries).Value);
    }
    [Fact] public void FrozenTargetsUseArmedPriorityStableIdsAndNeverRetargetOverkill()
    {
        var ships=ImmutableArray.Create(Escort(3),new CombatShipFrame(new(1),"scout",60,60,100,0,0,300,false,0),Escort(2));var sentries=ImmutableArray.Create(Sentry(2),Sentry(1,1));
        var frame=Frame(ships,sentries);var a=CombatExchange.Calculate(frame,false,ImmutableArray<VesselId>.Empty);var b=CombatExchange.Calculate(frame with {Ships=ships.Reverse().ToImmutableArray(),Sentries=sentries.Reverse().ToImmutableArray()},false,ImmutableArray<VesselId>.Empty);
        Assert.Equal(JsonSerializer.Serialize(a),JsonSerializer.Serialize(b));Assert.Equal(1,a.Sentries[0].ActualLoss);Assert.Equal(80,a.Sentries[1].After);Assert.Equal(60,a.Ships.Single(s=>s.Id.Value==1).After);
        Assert.All(a.Shots.Where(s=>s.Source.Sentry),shot=>Assert.Equal(new CombatActor(false,2),shot.Target));Assert.DoesNotContain(a.Shots,s=>s.Source==new CombatActor(false,1));
        var minimum=CombatExchange.Calculate(Frame(ImmutableArray.Create(Escort() with {Attack=1,Hull=1}),ImmutableArray.Create(Sentry() with {Armor=int.MaxValue})),false,ImmutableArray<VesselId>.Empty);Assert.Equal(1,minimum.Shots.Single(s=>!s.Source.Sentry).Damage);
    }
    [Fact] public void InterceptionShootsOnlyExposedShipsWithNoReturnFire()
    {
        var frame=Frame(ImmutableArray.Create(Escort(1),Escort(2)),ImmutableArray.Create(Sentry()));var a=CombatExchange.Calculate(frame,true,ImmutableArray.Create(new VesselId(2)));
        Assert.Single(a.Shots);Assert.Equal(100,a.Ships.Single(s=>s.Id.Value==1).After);Assert.Equal(86,a.Ships.Single(s=>s.Id.Value==2).After);Assert.Equal(80,Assert.Single(a.Sentries).After);
        Assert.Empty(CombatExchange.Calculate(frame,true,ImmutableArray<VesselId>.Empty).Shots);
    }
    [Fact] public void ExactRatiosCheckedStatsAndWideDamageSums()
    {
        Assert.Equal(CombatDamageBand.Operational,CombatMath.Band(new(int.MaxValue,0,0),int.MaxValue/2+1));Assert.Equal(CombatDamageBand.Damaged,CombatMath.Band(new(int.MaxValue,0,0),int.MaxValue/2));
        Assert.Throws<OverflowException>(()=>CombatMath.Stats(new(100,24,4),100,int.MaxValue,int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(()=>CombatMath.Stats(new(100,24,4),0,300,0));
        var ships=Enumerable.Range(1,32).Select(i=>Escort(i) with {Attack=int.MaxValue}).ToImmutableArray();var result=CombatExchange.Calculate(Frame(ships,ImmutableArray.Create(Sentry() with {Hull=int.MaxValue,MaxHull=int.MaxValue})),false,ImmutableArray<VesselId>.Empty);
        Assert.Equal(int.MaxValue,Assert.Single(result.Sentries).ActualLoss);Assert.Equal(0,result.Sentries[0].After);
    }
    [Fact] public void OwnViewPreviewIsPureAndMatchesFrozenExchange()
    {
        var g=CombatFixtures.Duel();string before=CombatFixtures.Bytes(g.State);var owner=g.State.Empires[0];var action=new CombatAction.AttackPocket(new(1),new(1),new(CombatDoctrine.Cautious,g.State.Colonies[0].Id,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair),false);
        var view=EmpireViewProjector.Project(g.Content,g.State,owner.Id);var preview=CombatQueries.Preview(g.Content,view,action);Assert.True(preview.Allowed,preview.RejectionCode);Assert.NotNull(preview.Frame);Assert.NotNull(preview.Exchange);
        Assert.Equal(72,Assert.Single(preview.Exchange.Ships).After);Assert.Equal(58,preview.Exchange.Sentries[0].After);Assert.Equal(80,preview.Exchange.Sentries[1].After);
        Assert.Equal(JsonSerializer.Serialize(CombatExchange.Calculate(preview.Frame,false,ImmutableArray<VesselId>.Empty)),JsonSerializer.Serialize(preview.Exchange));Assert.Equal(before,CombatFixtures.Bytes(g.State));
        Assert.Equal(preview.Token,CombatQueries.Preview(g.Content,view,action).Token);Assert.Equal(64,preview.Token.Length);
        // The pure query sees the supplied observation, never the hidden authoritative hazard.
        g.State.Combat!.Pockets[0].Sentries[0].Hull=1;Assert.Equal(preview.Token,CombatQueries.Preview(g.Content,EmpireViewProjector.Project(g.Content,g.State,owner.Id),action).Token);
        Assert.NotEqual(preview.Token,CombatQueries.Preview(g.Content,view,action with {Orders=action.Orders with {Doctrine=CombatDoctrine.Hold}}).Token);
    }
    [Fact] public void TokensBindExactResponseAndCanonicalPermittedInputs()
    {
        var g=CombatFixtures.Duel();var state=g.State;var owner=state.Empires[0];var orders=new CombatOrders(CombatDoctrine.Cautious,state.Colonies[0].Id,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair);
        state.Combat!.Encounters.Add(new(){Id=new(1),Owner=owner.Id,Fleet=new(1),Pocket=new(1),Members=new(){state.Vessels[0].Id},Orders=orders,Phase=CombatPhase.BlockedResponse});state.Combat.NextEncounterId=2;owner.Combat!.Responses.Add(new(new(1),1,"Revised force"));CombatFixtures.Valid(g);
        var view=EmpireViewProjector.Project(g.Content,state,owner.Id);var accept=new CombatAction.RespondCombat(new(1),1,CombatResponse.AcceptRevisedForce);var remain=accept with {Response=CombatResponse.RemainWithBlockedReturn};
        var first=CombatQueries.Preview(g.Content,view,accept);var second=CombatQueries.Preview(g.Content,view,remain);Assert.True(first.Allowed);Assert.True(second.Allowed);Assert.NotEqual(first.Token,second.Token);
        var reordered=view with {Combat=view.Combat! with {Ships=view.Combat.Ships.Reverse().ToImmutableArray(),Pockets=view.Combat.Pockets.Select(p=>p with {Sentries=p.Sentries.Reverse().ToImmutableArray()}).ToImmutableArray()},Vessels=view.Vessels.Reverse().ToImmutableArray()};
        Assert.Equal(first.Token,CombatQueries.Preview(g.Content,reordered,accept).Token);
    }
    [Fact] public void EmbeddedBandFactorsAndAllCulturesUseTheSameIntegerOrder()
    {
        var content=CombatFixtures.Game().Content;var definition=content.Combat! with {AttackPercent=ImmutableArray.Create(100,20,10),SpeedPercent=ImmutableArray.Create(100,67,40)};
        Assert.Equal(new CombatStats(4,4,211),CombatMath.Stats(new(100,24,4),50,300,5,definition));
        foreach(var species in new[]{"harl","peldra","sulan","wenth"})
        {
            var g=CombatFixtures.Game(species);var s=g.State;var ship=s.Vessels[0];ship.RoleId="escort";CombatInitializer.InitializeVessel(g.Content,9,ship);ship.Combat!.Hull=25;ship.Fleet=new(1);var f=s.Empires[0].Fleets!;var o=f.Officers[0];o.Status=OfficerStatus.Commanding;o.PostedColony=null;o.Flagship=ship.Id;o.Rank=1;o.Xp=10;f.Fleets.Add(new(){Id=new(1),Name="Fixture",Members=new(){ship.Id},Flagship=ship.Id,Officer=o.Id});f.NextFleetId=2;
            Assert.Equal(157,OfficerBonuses.EffectiveSpeed(g.Content,s,ship.Id));Assert.Equal(157,OfficerBonuses.EffectiveSpeed(g.Content,s.Clone(),ship.Id));
        }
    }
    [Fact] public void DamagedOwnSpeedAffectsRealFleetAndProjectedTravelOnlyUnderNine()
    {
        var g=CombatFixtures.Duel(50);var s=g.State;var ship=s.Vessels[0];var fleet=s.Empires[0].Fleets!.Fleets[0];var officer=s.Empires[0].Fleets!.Officers[0];officer.Status=OfficerStatus.Commanding;officer.PostedColony=null;officer.Flagship=ship.Id;officer.Xp=10;officer.Rank=1;fleet.Officer=officer.Id;
        Assert.Equal(236,OfficerBonuses.EffectiveSpeed(g.Content,s,ship.Id));var clone=s.Clone();clone.RulesVersion=8;Assert.Equal(315,OfficerBonuses.EffectiveSpeed(g.Content,clone,ship.Id));
        var target=s.FindPlanet(s.Colonies[0].PlanetId)!.StarId;var origin=s.FindStar(ship.StationaryStarId!.Value)!;var destination=s.FindStar(target)!;int expected=OpenAntares.Simulation.Movement.TravelCalculator.Duration(origin.X,origin.Y,destination.X,destination.Y,236);
        Assert.Equal(expected,FleetTravel.Duration(g.Content,s,ship.EmpireId,fleet.Id,target));var view=EmpireViewProjector.Project(g.Content,s,ship.EmpireId);Assert.Equal(expected,FleetTravel.PreviewDuration(g.Content,view,view.Vessels[0],target));
    }
}
