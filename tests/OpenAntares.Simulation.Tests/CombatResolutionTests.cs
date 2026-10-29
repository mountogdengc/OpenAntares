using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatResolutionTests
{
    static LoadedGame Pending(int hull=100,int sentry=80,CombatDoctrine doctrine=CombatDoctrine.Cautious,bool convoy=false)
    {
        var g=convoy?CombatFixtures.Convoy():CombatFixtures.Duel(hull,sentry);g.State.Combat!.Pockets[0].Sentries[1].Hull=0;var known=g.State.Empires[0].Combat!.Observations[0];g.State.Empires[0].Combat!.Observations[0]=known with {Sentries=known.Sentries.Select((s,i)=>i==1?s with {Hull=0}:s).ToImmutableArray()};
        var attack=CombatCommandTests.Attack(g.State,convoy);return new(g.Content,CombatCommandTests.Commit(g,attack with {Orders=attack.Orders with {Doctrine=doctrine}}));
    }
    static (GameState State,CombatResolutionContext Context) Resolve(LoadedGame g)
    {var c=g.State.Clone();var context=CombatResolutionContext.Capture(g.Content,g.State);CombatResolver.ResolveExchanges(g.Content,g.State,c,context);return(c,context);}
    [Fact] public void OneRoundUpdatesPersistentState()
    {
        var g=Pending();string before=CombatFixtures.Bytes(g.State);var (candidate,context)=Resolve(g);Assert.Equal(86,candidate.Vessels[0].Combat!.Hull);Assert.Equal(58,candidate.Combat!.Pockets[0].Sentries[0].Hull);var e=Assert.Single(candidate.Combat.Encounters);Assert.Equal(1,e.Round);Assert.Equal(CombatPhase.Active,e.Phase);Assert.Single(context.Rounds);Assert.Equal(before,CombatFixtures.Bytes(g.State));
        string after=CombatFixtures.Bytes(candidate);Assert.Throws<InvalidOperationException>(()=>CombatResolver.ResolveExchanges(g.Content,g.State,candidate,context));Assert.Equal(after,CombatFixtures.Bytes(candidate));
        var second=CombatResolutionContext.Capture(g.Content,g.State);CombatResolver.ResolveExchanges(g.Content,g.State,candidate,second);Assert.Equal(after,CombatFixtures.Bytes(candidate));
    }
    [Theory] [InlineData(CombatDoctrine.Cautious,39,CombatPhase.WithdrawalQueued)] [InlineData(CombatDoctrine.Press,39,CombatPhase.Active)] [InlineData(CombatDoctrine.Hold,39,CombatPhase.Active)] [InlineData(CombatDoctrine.HitAndRun,100,CombatPhase.WithdrawalQueued)]
    public void DoctrineQueuesOnlyFollowingBoundary(CombatDoctrine doctrine,int hull,CombatPhase expected)
    {
        var g=Pending(hull,doctrine:doctrine);var (s,context)=Resolve(g);Assert.Equal(expected,Assert.Single(s.Combat!.Encounters).Phase);Assert.Equal(g.State.Vessels[0].StationaryStarId,s.Vessels[0].StationaryStarId);Assert.Empty(context.Departures);Assert.Null(s.Vessels[0].Transit);
    }
    [Fact] public void AbstractCohesionExampleKeepsDeathAndNewCrippleDisjoint()
    {
        // Formula-only frame for the approved abstract example; not a naturally reachable two-target volley.
        var g=Pending(convoy:true);var s=g.State;var first=s.Vessels[0];first.Combat!.Cohesion=80;var middle=s.Vessels[1];middle.Combat!.Cohesion=70;
        var third=middle.Clone();third.Id=new(s.AllocateId());third.Combat!.Cohesion=60;s.Vessels.Add(third);s.Empires[0].Fleets!.Fleets[0].Members.Add(third.Id);var e=s.Combat!.Encounters[0];e.Members.Add(third.Id);
        var frame=CombatQueries.Frame(g.Content,s,e.Id);var result=new CombatExchangeResult(ImmutableArray.Create(new CombatShipDamage(first.Id,100,100,0),new CombatShipDamage(middle.Id,100,0,100),new CombatShipDamage(third.Id,100,25,75)),ImmutableArray.Create(new SentryDamage(new(1),80,80,0)),ImmutableArray<CombatShot>.Empty,ImmutableArray.Create(middle.Id),ImmutableArray<SentryId>.Empty);
        var context=CombatResolutionContext.Capture(g.Content,s);CombatResolver.ApplyExchange(g.Content,s,e,frame,result,context);Assert.Equal(50,first.Combat.Cohesion);Assert.Equal(30,third.Combat.Cohesion);Assert.Null(s.FindVessel(middle.Id));Assert.Equal(CombatPhase.WithdrawalQueued,e.Phase);
    }
    [Theory] [InlineData(100,10,true,true)] [InlineData(1,1,true,false)] [InlineData(1,80,false,false)]
    public void TerminalOutcomesReleaseAndRetainExactAttrition(int hull,int sentry,bool cleared,bool survivor)
    {
        var g=Pending(hull,sentry);var own=g.State.Vessels[0].Id;int colonies=g.State.Colonies.Count;long research=g.State.Empires[0].ResearchReserve;int xp=g.State.Empires[0].Fleets!.Officers[0].Xp;
        var (s,context)=Resolve(g);Assert.Empty(s.Combat!.Encounters);Assert.Equal(cleared,s.Combat.Pockets[0].Cleared);Assert.Equal(survivor,s.FindVessel(own) is not null);Assert.Equal(colonies,s.Colonies.Count);Assert.Equal(research,s.Empires[0].ResearchReserve);Assert.Equal(xp,s.Empires[0].Fleets!.Officers[0].Xp);Assert.Single(s.Empires[0].Combat!.History);Assert.Equal(survivor?0:1,context.Destructions.Length);
    }
    [Fact] public void LastWarshipLossRequiresCivilianWithdrawalEvenUnderHold()
    {
        var g=Pending(doctrine:CombatDoctrine.Hold,convoy:true);g.State.Vessels[0].Combat!.Hull=1;var (s,context)=Resolve(g);Assert.Null(s.FindVessel(g.State.Vessels[0].Id));Assert.Equal(CombatPhase.WithdrawalQueued,Assert.Single(s.Combat!.Encounters).Phase);Assert.Contains(s.Empires[0].Combat!.PendingEvents,e=>e.Kind=="emergency");Assert.Equal(70,s.Vessels[0].Combat!.Cohesion);
    }
    [Fact] public void HealthLossCancelsPendingAndPausesActiveWithoutFiring()
    {
        foreach(bool active in new[]{false,true})
        {
            var g=Pending(convoy:true);if(active){var first=Resolve(g).State;first.Turn=1;ObservationUpdater.Refresh(first,1);g=new(g.Content,first);}
            var snapshot=g.State;var candidate=snapshot.Clone();var context=CombatResolutionContext.Capture(g.Content,snapshot);var victim=candidate.Vessels.Last();candidate.Vessels.Remove(victim);int enemy=candidate.Combat!.Pockets[0].Sentries[0].Hull;
            CombatResolver.ResolveExchanges(g.Content,snapshot,candidate,context);Assert.Equal(enemy,candidate.Combat.Pockets[0].Sentries[0].Hull);Assert.Empty(context.Rounds);
            if(active){Assert.Equal(CombatPhase.BlockedResponse,Assert.Single(candidate.Combat.Encounters).Phase);Assert.Single(candidate.Empires[0].Combat!.Responses);Assert.Null(candidate.Combat.Encounters[0].ConsentRevision);}else Assert.Empty(candidate.Combat.Encounters);
        }
    }
    [Fact] public void BoundedRoundHistoryAndCloneCannotResetCohesionOrCounters()
    {
        var g=Pending(doctrine:CombatDoctrine.Hold);var definition=g.Content.Combat! with {RoleProfiles=g.Content.Combat.RoleProfiles.SetItem("escort",new(int.MaxValue,1,0)),SentryProfile=new(int.MaxValue,1,0)};var content=g.Content with {Combat=definition};var state=g.State;state.Vessels[0].Combat!.Hull=int.MaxValue;state.Vessels[0].Combat!.Cohesion=60;
        foreach(var sentry in state.Combat!.Pockets[0].Sentries){sentry.Profile=definition.SentryProfile;if(sentry.Hull>0)sentry.Hull=int.MaxValue;}
        var known=state.Empires[0].Combat!.Observations[0];state.Empires[0].Combat!.Observations[0]=known with {Sentries=known.Sentries.Select(s=>s with {Profile=definition.SentryProfile,Hull=s.Hull>0?int.MaxValue:0}).ToImmutableArray()};
        ulong random=state.Random.State,recovery=state.Empires[0].Fleets!.RecoveryRandom!.State;
        for(int i=0;i<25;i++){var next=Resolve(new(content,state)).State;next.Turn++;ObservationUpdater.Refresh(next,next.Turn);state=next;}
        var e=Assert.Single(state.Combat!.Encounters);Assert.Equal(25,e.Round);Assert.Equal(20,e.Rounds.Count);Assert.Equal(6,e.Rounds[0].Round);Assert.Equal(60,state.Vessels[0].Combat!.Cohesion);Assert.Equal(random,state.Random.State);Assert.Equal(recovery,state.Empires[0].Fleets!.RecoveryRandom!.State);Assert.Empty(GameStateValidator.Validate(content,state));
        var clone=state.Clone();clone.Combat!.Encounters[0].Rounds.Clear();clone.Combat.Encounters[0].DestroyedMembers.Add(new(999));Assert.Equal(20,state.Combat.Encounters[0].Rounds.Count);Assert.Empty(state.Combat.Encounters[0].DestroyedMembers);
    }
    [Fact] public void InvalidNewCountersAndRoundRecordsAreRejected()
    {
        var g=Pending();var state=Resolve(g).State;
        foreach(var mutate in new Action<GameState>[] {s=>s.Combat!.Encounters[0].HullLost=-1,s=>s.Empires[0].Combat!.LifetimeHullLost=-1,s=>s.Combat!.Encounters[0].CargoLost=-1,s=>s.Combat!.Encounters[0].DestroyedMembers.Add(s.Vessels[0].Id),s=>s.Combat!.Encounters[0].Rounds.Add(s.Combat.Encounters[0].Rounds[0]),s=>s.Combat!.Encounters[0].Rounds[0]=s.Combat.Encounters[0].Rounds[0] with {Turn=1}})
        {var bad=state.Clone();mutate(bad);Assert.NotEmpty(GameStateValidator.Validate(g.Content,bad));}
    }
    [Fact] public void ThresholdWithdrawalCanBeExplicitlyOverriddenByPressOrHold()
    {
        var g=Pending(39);var s=Resolve(g).State;var e=s.Combat!.Encounters[0];var orders=e.Orders with {Doctrine=CombatDoctrine.Press};var current=new LoadedGame(g.Content,s);var changed=CombatCommandTests.Commit(current,new CombatAction.ChangeCombatOrders(e.Id,e.Revision,orders,false));Assert.Equal(CombatPhase.Active,changed.Combat!.Encounters[0].Phase);
    }
}
