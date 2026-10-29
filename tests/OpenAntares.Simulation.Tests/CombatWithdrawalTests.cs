using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatWithdrawalTests
{
    // Labelled pre-session supplements with ordinary first volleys, not generated/earned fleet claims.
    static LoadedGame Battle(int[] hulls,bool[]? armed=null,int flagship=0,int navigator=0,bool two=false,Func<CombatDefinition,CombatDefinition>? configure=null,bool crises=false)
    {
        var g=CombatFixtures.Game();var c=configure is null?g.Content:g.Content with {Combat=configure(g.Content.Combat!)};var s=g.State;CrisisInitializer.Initialize(c,s,crises);var e=s.Empires[0];var pocket=s.Combat!.Pockets[0];
        for(int i=0;i<pocket.Sentries.Count;i++){pocket.Sentries[i].Profile=c.Combat!.SentryProfile;pocket.Sentries[i].Hull=i==0||two?c.Combat.SentryProfile.MaxHull:0;}
        var original=s.Vessels[0];s.Vessels.Clear();var members=new System.Collections.Generic.List<VesselId>();
        for(int i=0;i<hulls.Length;i++)
        {
            bool weapon=armed is null||armed[i];var v=i==0?original:new VesselState{Id=new(s.AllocateId()),EmpireId=e.Id,Crisis=new()};v.RoleId=weapon?"escort":"harl_settler";v.CarriedPopulation=weapon?0:c.Vessels[v.RoleId].EmbarkPopulation;v.StationaryStarId=pocket.Star;v.Fleet=new(1);v.Scout=null;ScoutInitializer.InitializeVessel(c,9,v);CombatInitializer.InitializeVessel(c,9,v);v.Combat!.Hull=hulls[i];s.Vessels.Add(v);members.Add(v.Id);
        }
        var fleet=new FleetRecord{Id=new(1),Name="Withdrawal fixture",Members=members,Flagship=members[flagship]};e.Fleets!.Fleets.Add(fleet);e.Fleets.NextFleetId=2;
        if(navigator>0){var officer=e.Fleets.Officers[0];officer.Status=OfficerStatus.Commanding;officer.PostedColony=null;officer.Flagship=fleet.Flagship;officer.Rank=navigator;officer.Xp=navigator==1?10:30;fleet.Officer=officer.Id;}
        g=new(c,s);Refresh(g);CombatFixtures.Valid(g);var attack=CombatCommandTests.Attack(s); // Civilian authority selected below.
        attack=attack with {AuthorizeCivilianRisk=armed?.Any(a=>!a)==true,Orders=attack.Orders with {Doctrine=CombatDoctrine.Hold}};
        return new(c,CombatCommandTests.Commit(g,attack));
    }
    static void Refresh(LoadedGame g)
    {
        ObservationUpdater.Refresh(g.State,g.State.Turn);var p=g.State.Combat!.Pockets[0];var owner=g.State.Empires[0];Assert.Contains(g.State.Vessels,v=>v.EmpireId==owner.Id&&v.StationaryStarId==p.Star);
        owner.Combat!.Observations.Clear();owner.Combat.Observations.Add(new(p.Id,p.Star,g.State.Turn,p.Cleared,p.Sentries.Select(s=>new ObservedSentry(s.Id,s.Profile,s.Hull,g.Content.Combat!.SentryPursuitSpeed)).ToImmutableArray()));
    }
    static LoadedGame Queued(LoadedGame g,StragglerPolicy policy)
    {
        var context=CombatResolutionContext.Capture(g.Content,g.State);var s=g.State.Clone();CombatResolver.ResolveExchanges(g.Content,g.State,s,context);s.Turn++;g=new(g.Content,s);Refresh(g);var e=Assert.Single(s.Combat!.Encounters);
        s=CombatCommandTests.Commit(g,new CombatAction.ChangeCombatOrders(e.Id,e.Revision,e.Orders with {Stragglers=policy},e.CivilianRiskAuthorized));g=new(g.Content,s);e=s.Combat!.Encounters[0];return new(g.Content,CombatCommandTests.Commit(g,new CombatAction.WithdrawCombat(e.Id,e.Revision)));
    }
    static (GameState State,CombatResolutionContext Context) Depart(LoadedGame g,Action<GameState>? health=null)
    {
        var c=g.State.Clone();var context=CombatResolutionContext.Capture(g.Content,g.State);health?.Invoke(c);CombatResolver.ResolveExchanges(g.Content,g.State,c,context);CombatWithdrawal.Resolve(g.Content,g.State,c,context);return(c,context);
    }
    [Theory] [InlineData(StragglerPolicy.KeepTogether,1,72,25)] [InlineData(StragglerPolicy.DetachCrippled,2,86,11)]
    public void FrozenGroupsAndSingleInterception(StragglerPolicy policy,int groups,int flagshipHull,int stragglerHull)
    {
        var g=Queued(Battle(new[]{100,25}),policy);var e=g.State.Combat!.Encounters[0];var frame=CombatQueries.Frame(g.Content,g.State,e.Id);var proposed=CombatWithdrawal.Preview(g.Content,frame);Assert.Equal(groups,proposed.Length);
        var (s,context)=Depart(g);Assert.Equal(groups,context.Departures.Values.Distinct().Count());Assert.Equal(flagshipHull,s.Vessels[0].Combat!.Hull);Assert.Equal(stragglerHull,s.Vessels[1].Combat!.Hull);Assert.Empty(s.Combat!.Encounters);var receipt=Assert.Single(context.Rounds);Assert.True(receipt.Interception);Assert.Single(receipt.Result.Shots);Assert.All(receipt.Result.Shots,shot=>Assert.True(shot.Source.Sentry));
        if(policy==StragglerPolicy.DetachCrippled){Assert.Null(s.Vessels[1].Fleet);Assert.Contains(context.Departures.Values,d=>d.Speed==300);Assert.Contains(context.Departures.Values,d=>d.Speed==150);}else Assert.All(context.Departures.Values,d=>Assert.Equal(150,d.Speed));
    }
    [Fact] public void DetachedShipsLoseNavigatorAndCleanGroupKeepsCohesion()
    {
        var g=Queued(Battle(new[]{100,1},navigator:1),StragglerPolicy.DetachCrippled);var preview=CombatWithdrawal.Preview(g.Content,CombatQueries.Frame(g.Content,g.State,g.State.Combat!.Encounters[0].Id));Assert.Contains(preview,d=>d.Speed==315);Assert.Contains(preview,d=>d.Speed==150);
        var (s,context)=Depart(g);Assert.Single(s.Vessels);Assert.Equal(100,s.Vessels[0].Combat!.Cohesion);Assert.Equal(86,s.Vessels[0].Combat!.Hull);Assert.Single(context.Departures);Assert.Equal(315,context.Departures.Values.Single().Speed);
    }
    [Fact] public void NewlyCrippledNonFlagshipStaysInFrozenMainGroup()
    {
        var g=Queued(Battle(new[]{53,100},flagship:1,configure:d=>d with {SentryProfile=new(1000,18,2)}),StragglerPolicy.DetachCrippled);Assert.Equal(39,g.State.Vessels[0].Combat!.Hull);
        var (s,context)=Depart(g);Assert.Equal(25,s.Vessels[0].Combat!.Hull);Assert.All(s.Vessels,v=>Assert.Equal(new FleetId(1),v.Fleet));var departure=context.Departures.Values.Distinct().Single();Assert.Equal(2,departure.Members.Length);Assert.Equal(150,departure.Speed);Assert.Single(context.Rounds.Single().Result.Shots);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void FlagshipReplacementComesOnlyFromMainOrMainDissolves(bool otherMain)
    {
        var g=Queued(Battle(otherMain?new[]{25,15,100}:new[]{25,15},otherMain?new[]{false,true,true}:new[]{false,true},flagship:1),StragglerPolicy.DetachCrippled);var detached=g.State.Vessels[0].Id;var flagship=g.State.Vessels[1].Id;var (s,context)=Depart(g);Assert.Null(s.FindVessel(flagship));Assert.Null(s.FindVessel(detached)!.Fleet);Assert.Contains(detached,context.Departures.Keys);
        var fleets=s.Empires[0].Fleets!.Fleets;if(otherMain){var main=Assert.Single(fleets);Assert.Equal(g.State.Vessels[2].Id,main.Flagship);Assert.DoesNotContain(detached,main.Members);}else Assert.Empty(fleets);
    }
    [Fact] public void AllExposedCohortsShareOneSentryVolley()
    {
        var g=Queued(Battle(new[]{53,25,100},flagship:2,two:true),StragglerPolicy.DetachCrippled);var (s,context)=Depart(g);var result=Assert.Single(context.Rounds).Result;Assert.Equal(2,result.Shots.Length);Assert.All(result.Shots,shot=>Assert.Equal(new CombatActor(false,g.State.Vessels[0].Id.Value),shot.Target));Assert.Equal(100,s.Vessels[2].Combat!.Hull);Assert.Equal(3,context.Departures.Values.Distinct().Count());
    }
    [Fact] public void EqualityIsExposedAndPostDamageSpeedIsRecomputedOnce()
    {
        var g=Queued(Battle(new[]{75,100},configure:d=>d with {SentryProfile=new(1000,80,2),SentryPursuitSpeed=300}),StragglerPolicy.KeepTogether);var own=Assert.Single(g.State.Vessels);Assert.Equal(100,own.Combat!.Hull);var (s,context)=Depart(g);Assert.Equal(24,Assert.Single(s.Vessels).Combat!.Hull);Assert.Equal(150,context.Departures.Values.Single().Speed);Assert.Single(context.Rounds);Assert.Single(context.Rounds[0].Result.Shots);
    }
    [Fact] public void PortRaceDefendsOnceThenRequiresExplicitCurrentResponse()
    {
        var g=Queued(Battle(new[]{100,100},new[]{true,false},crises:true),StragglerPolicy.KeepTogether);var (s,context)=Depart(g,c=>c.Colonies[0].Crisis!.Quarantined=true);Assert.Empty(context.Departures);var e=Assert.Single(s.Combat!.Encounters);Assert.Equal(CombatPhase.BlockedResponse,e.Phase);Assert.Single(context.Rounds);Assert.False(context.Rounds[0].Interception);Assert.Contains(s.Empires[0].Combat!.PendingEvents,x=>x.Kind=="emergency");Assert.NotEmpty(CombatReadyQueries.ValidateResponse(g.Content,s,s.Empires[0].Id));
        var current=new LoadedGame(g.Content,s);var continued=CombatCommandTests.Commit(current,new CombatAction.RespondCombat(e.Id,e.Revision,CombatResponse.RemainWithBlockedReturn));Assert.Empty(CombatReadyQueries.ValidateResponse(g.Content,continued,continued.Empires[0].Id));Assert.Equal(s.Turn,continued.Combat!.Encounters[0].ContinueRiskTurn);
    }
    [Fact] public void KnownClosedPortOffersSpecificResponseWithoutWaitingForRace()
    {
        var g=Queued(Battle(new[]{100,100},new[]{true,false},crises:true),StragglerPolicy.KeepTogether);g.State.Colonies[0].Crisis!.Quarantined=true;Assert.Contains(CombatRejectionCodes.PortClosed,CombatReadyQueries.ValidateResponse(g.Content,g.State,g.State.Empires[0].Id));var e=g.State.Combat!.Encounters[0];var s=CombatCommandTests.Commit(g,new CombatAction.RespondCombat(e.Id,e.Revision,CombatResponse.RemainWithBlockedReturn));Assert.Empty(CombatReadyQueries.ValidateResponse(g.Content,s,s.Empires[0].Id));
    }
    [Fact] public void MissingReturnColonyBlocksWithoutGuessingAnotherDestination()
    {
        var g=Queued(Battle(new[]{100,100},new[]{true,false},crises:true),StragglerPolicy.KeepTogether);var (s,context)=Depart(g,c=>{var colony=c.Colonies[0];CrisisLoss.Remove(c,colony.Id,CrisisQueries.Location(c,colony));});Assert.Empty(context.Departures);var e=Assert.Single(s.Combat!.Encounters);Assert.Equal(CombatPhase.BlockedResponse,e.Phase);Assert.Empty(GameStateValidator.Validate(g.Content,s));
        var current=new LoadedGame(g.Content,s);var continued=CombatCommandTests.Commit(current,new CombatAction.RespondCombat(e.Id,e.Revision,CombatResponse.RemainWithBlockedReturn));Assert.Empty(CombatReadyQueries.ValidateResponse(g.Content,continued,continued.Empires[0].Id));
    }
    [Fact] public void PreVolleyCancellationHonorsTheDisplayedDetachPolicy()
    {
        var g=Battle(new[]{100,25},navigator:1);var e=g.State.Combat!.Encounters[0];var changed=CombatCommandTests.Commit(g,new CombatAction.ChangeCombatOrders(e.Id,e.Revision,e.Orders with {Stragglers=StragglerPolicy.DetachCrippled},false));g=new(g.Content,changed);e=changed.Combat!.Encounters[0];
        var action=new CombatAction.WithdrawCombat(e.Id,e.Revision);var preview=CombatQueries.Preview(g.Content,EmpireViewProjector.Project(g.Content,changed,changed.Empires[0].Id),action);Assert.True(preview.Allowed);Assert.Empty(preview.Exposed);Assert.Empty(preview.Exchange!.Shots);Assert.Equal(2,preview.Departures.Length);
        var s=CombatCommandTests.Commit(g,action);Assert.Null(s.Vessels[1].Fleet);Assert.Single(s.Empires[0].Fleets!.Fleets[0].Members);Assert.Equal(100,s.Vessels[0].Combat!.Hull);Assert.Equal(25,s.Vessels[1].Combat!.Hull);Assert.All(s.Vessels,v=>Assert.NotNull(v.PendingMove));
    }
    [Fact] public void ContinuedRiskNeverAuthorizesAnIllegalDeparture()
    {
        var g=Queued(Battle(new[]{100,100},new[]{true,false},crises:true),StragglerPolicy.KeepTogether);g.State.Colonies[0].Crisis!.Quarantined=true;var e=g.State.Combat!.Encounters[0];var state=CombatCommandTests.Commit(g,new CombatAction.RespondCombat(e.Id,e.Revision,CombatResponse.RemainWithBlockedReturn));e=state.Combat!.Encounters[0];var action=new CombatAction.WithdrawCombat(e.Id,e.Revision);var preview=CombatQueries.Preview(g.Content,EmpireViewProjector.Project(g.Content,state,state.Empires[0].Id),action);Assert.False(preview.Allowed);
    }
    [Fact] public void IdenticalContinuedRiskResponsePreservesReadiness()
    {
        var g=Queued(Battle(new[]{100,100},new[]{true,false},crises:true),StragglerPolicy.KeepTogether);g.State.Colonies[0].Crisis!.Quarantined=true;var e=g.State.Combat!.Encounters[0];var action=new CombatAction.RespondCombat(e.Id,e.Revision,CombatResponse.RemainWithBlockedReturn);var state=CombatCommandTests.Commit(g,action);state.Empires[0].Ready=true;var current=new LoadedGame(g.Content,state);var result=CommandProcessor.Execute(g.Content,state,CombatCommandTests.Command(current,action));Assert.True(result.Accepted,result.Rejection?.Message);Assert.False(result.Changed);Assert.Same(state,result.State);Assert.True(result.State.Empires[0].Ready);
    }
    [Fact] public void PendingCancellationTravelsWithoutInterceptionOrDamage()
    {
        var g=Battle(new[]{100,25});var e=g.State.Combat!.Encounters[0];var s=CombatCommandTests.Commit(g,new CombatAction.WithdrawCombat(e.Id,e.Revision));Assert.Empty(s.Combat!.Encounters);Assert.Equal(100,s.Vessels[0].Combat!.Hull);Assert.Equal(25,s.Vessels[1].Combat!.Hull);Assert.All(s.Vessels,v=>Assert.NotNull(v.PendingMove));
    }
}
