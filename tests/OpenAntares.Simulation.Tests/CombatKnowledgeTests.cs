using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Colonization;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatKnowledgeTests
{
    // Labelled pre-session boards isolate observation and founding policy, not generation/balance.
    static void Observe(LoadedGame g,EmpireId owner)
    {
        var p=g.State.Combat!.Pockets[0];var e=g.State.FindEmpire(owner)!;
        e.Combat!.Observations.RemoveAll(o=>o.Pocket==p.Id);
        e.Combat.Observations.Add(new(p.Id,p.Star,g.State.Turn,p.Cleared,p.Sentries.Select(a=>new ObservedSentry(a.Id,a.Profile,a.Hull,g.Content.Combat!.SentryPursuitSpeed)).ToImmutableArray()));
        ObservationUpdater.Refresh(g.State,g.State.Turn);
    }
    static LoadedGame Founder(bool rival=false)
    {
        var c=CombatFixtures.Game().Content;var s=GalaxyGenerator.CreateNewGame(c,new("small",42,9,"harl",rival?1:0,rival?"peldra":null,CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:true)).State!;
        var owner=s.Empires[rival?1:0];var ship=s.Vessels.Single(v=>v.EmpireId==owner.Id);ship.RoleId=rival?"peldra_settler":"harl_settler";ship.CarriedPopulation=c.Vessels[ship.RoleId].EmbarkPopulation;
        ship.Scout=null;OpenAntares.Simulation.Scouting.ScoutInitializer.InitializeVessel(c,9,ship);CombatInitializer.InitializeVessel(c,9,ship);ship.StationaryStarId=s.Combat!.Pockets[0].Star;
        var planet=s.Planets.First(p=>p.StarId==ship.StationaryStarId);planet.PlanetTypeId="verdant_world";planet.EnvironmentId="dry";
        var g=new LoadedGame(c,s);Observe(g,owner.Id);return CombatFixtures.Valid(g);
    }
    [Fact] public void PocketKnowledgeIsCurrentOnlyWithOwnPresence()
    {
        var g=CombatFixtures.Game();var owner=g.State.Empires[0].Id;var pocket=g.State.Combat!.Pockets[0];
        Assert.Empty(EmpireViewProjector.Project(g.Content,g.State,owner).Combat!.Pockets);
        var moved=CommandProcessor.Execute(g.Content,g.State,new SetMoveOrderCommand(owner,0,g.State.Vessels[0].Id,pocket.Star));Assert.True(moved.Accepted,moved.Rejection?.Message);
        var s=moved.State;
        for(int i=0;i<20&&s.Vessels[0].StationaryStarId!=pocket.Star;i++){var next=TurnResolver.Resolve(g.Content,s);Assert.True(next.Succeeded,string.Join(";",next.Errors));s=next.State;}
        Assert.Equal(pocket.Star,s.Vessels[0].StationaryStarId);var current=Assert.Single(EmpireViewProjector.Project(g.Content,s,owner).Combat!.Pockets);Assert.True(current.Current);Assert.Equal(s.Turn,current.ObservedTurn);Assert.Equal(2,current.Sentries.Length);Assert.All(current.Sentries,a=>Assert.Equal(80,a.Hull));Assert.Empty(s.Combat!.Encounters);Assert.Equal(60,s.Vessels[0].Combat!.Hull);
    }
    [Fact] public void StaleOwnFactsDoNotRefreshFromHiddenDamage()
    {
        var g=Founder(rival:true);var s=g.State;var observer=s.Empires[1];var ship=s.Vessels.Single(v=>v.EmpireId==observer.Id);ship.StationaryStarId=s.FindPlanet(s.Colonies.Single(c=>c.EmpireId==observer.Id).PlanetId)!.StarId;
        var before=EmpireViewProjector.Project(g.Content,s,observer.Id).Combat!;Assert.False(before.Pockets[0].Current);
        var changed=s.Clone();var p=changed.Combat!.Pockets[0];foreach(var a in p.Sentries)a.Hull=0;p.Cleared=true;
        CombatFixtures.Valid(new(g.Content,changed));
        var after=EmpireViewProjector.Project(g.Content,changed,observer.Id).Combat!;
        Assert.Equal(JsonSerializer.Serialize(before),JsonSerializer.Serialize(after));
        Assert.Null(RivalPlanningQueries.Create(g.Content,s,observer.Id).PreviewSettlement(s.Planets.First(p=>p.StarId==s.Combat!.Pockets[0].Star).Id,"peldra_settler"));
        Assert.Null(RivalPlanningQueries.Create(g.Content,changed,observer.Id).PreviewSettlement(s.Planets.First(p=>p.StarId==s.Combat!.Pockets[0].Star).Id,"peldra_settler"));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void GuardedFoundingUsesSharedObservedRuleForBothControllers(bool rival)
    {
        var g=Founder(rival);var owner=g.State.Empires[rival?1:0].Id;var ship=g.State.Vessels.Single(v=>v.EmpireId==owner);var target=g.State.Planets.First(p=>p.StarId==ship.StationaryStarId).Id;
        Assert.Equal("sentry_pocket_guarded",FoundingRules.Preview(g.Content,g.State,owner,ship.Id,target).RejectionCode);
        string before=CombatFixtures.Bytes(g.State);var result=CommandProcessor.Execute(g.Content,g.State,new SetFoundingOrderCommand(owner,0,ship.Id,target));Assert.False(result.Accepted);Assert.Equal("sentry_pocket_guarded",result.Rejection!.Code);Assert.Equal(before,CombatFixtures.Bytes(result.State));
        if(rival)Assert.Null(RivalPlanningQueries.Create(g.Content,g.State,owner).PreviewSettlement(target,ship.RoleId));
    }
    [Fact] public void UnknownTargetChecksKnowledgeBeforeHiddenHazards()
    {
        var g=Founder();var owner=g.State.Empires[0];var target=g.State.Planets.First(p=>p.StarId==g.State.Combat!.Pockets[0].Star).Id;owner.Surveys.RemoveAll(o=>o.StarId==g.State.Combat!.Pockets[0].Star);owner.Combat!.Observations.Clear();
        Assert.Equal("target_not_known",FoundingRules.Preview(g.Content,g.State,owner.Id,g.State.Vessels[0].Id,target).RejectionCode);Assert.Equal("target_not_known",FoundingRules.Preview(g.Content,g.State,owner.Id,g.State.Vessels[0].Id,new(999999)).RejectionCode);
    }
    [Fact] public void VictoryOpensNextPlanningWithoutAutomaticFoundingOrClaim()
    {
        var g=CombatFixtures.Convoy();var s=g.State;var pocket=s.Combat!.Pockets[0];foreach(var a in pocket.Sentries)a.Hull=1;
        var target=s.Planets.First(p=>p.StarId==pocket.Star);target.PlanetTypeId="verdant_world";target.EnvironmentId="dry";Observe(g,s.Empires[0].Id);
        var founder=s.Vessels.Single(v=>v.CarriedPopulation>0);Assert.Equal("sentry_pocket_guarded",FoundingRules.Preview(g.Content,s,founder.EmpireId,founder.Id,target.Id).RejectionCode);
        var attack=CombatCommandTests.Attack(s,true);s=CombatCommandTests.Commit(g,attack with{Orders=attack.Orders with{Doctrine=CombatDoctrine.Hold}});
        for(int i=0;i<2;i++){var next=TurnResolver.Resolve(g.Content,s);Assert.True(next.Succeeded,string.Join(";",next.Errors));s=next.State;}
        Assert.True(s.Combat!.Pockets[0].Cleared);Assert.DoesNotContain(s.Colonies,c=>c.PlanetId==target.Id);Assert.Null(s.FindVessel(founder.Id)!.PendingFounding);
        var command=CommandProcessor.Execute(g.Content,s,new SetFoundingOrderCommand(founder.EmpireId,s.Turn,founder.Id,target.Id));Assert.True(command.Accepted,command.Rejection?.Message);Assert.DoesNotContain(command.State.Colonies,c=>c.PlanetId==target.Id);
        var founded=TurnResolver.Resolve(g.Content,command.State);Assert.True(founded.Succeeded,string.Join(";",founded.Errors));Assert.Contains(founded.State.Colonies,c=>c.PlanetId==target.Id);
    }
    [Fact] public void RivalPlansOnlyFromItsOwnGuardedObservationAndNeverRequestsWarships()
    {
        var g=Founder(rival:true);var owner=g.State.Empires[1].Id;var target=g.State.Planets.First(p=>p.StarId==g.State.Combat!.Pockets[0].Star).Id;var context=RivalPlanningQueries.Create(g.Content,g.State,owner);Assert.Null(context.PreviewSettlement(target,"peldra_settler"));
        var prepared=RivalTurnPlanner.Prepare(g.Content,g.State);Assert.True(prepared.Succeeded);Assert.Empty(prepared.State.Combat!.Encounters);Assert.DoesNotContain(prepared.State.Colonies.Where(c=>c.EmpireId==owner),c=>c.ProjectId is {} id&&g.Content.Vessels.TryGetValue(id,out var role)&&role.CombatOnly);
        var cleared=g.State.Clone();foreach(var a in cleared.Combat!.Pockets[0].Sentries)a.Hull=0;cleared.Combat.Pockets[0].Cleared=true;
        Assert.Null(RivalPlanningQueries.Create(g.Content,cleared,owner).PreviewSettlement(target,"peldra_settler"));
        var refreshed=new LoadedGame(g.Content,cleared);Observe(refreshed,owner);Assert.NotNull(RivalPlanningQueries.Create(g.Content,cleared,owner).PreviewSettlement(target,"peldra_settler"));
    }
    [Fact] public void ActualUnseenOpponentReservationAndVictoryLeaveOwnFactsAndTokensUnchanged()
    {
        var g=Founder(rival:true);var s=g.State;var observer=s.Empires[1];observer.Controller=ControllerKind.Human;observer.AiPolicyId=null;
        var pocket=s.Combat!.Pockets[0];foreach(var a in pocket.Sentries)a.Hull=1;Observe(g,observer.Id);
        s.Vessels.Single(v=>v.EmpireId==observer.Id).StationaryStarId=s.FindPlanet(s.Colonies.Single(c=>c.EmpireId==observer.Id).PlanetId)!.StarId;
        var attacker=s.Vessels.Single(v=>v.EmpireId==s.Empires[0].Id);attacker.RoleId="escort";attacker.StationaryStarId=pocket.Star;CombatInitializer.InitializeVessel(g.Content,9,attacker);attacker.Fleet=new(1);
        s.Empires[0].Fleets!.Fleets.Add(new(){Id=new(1),Name="Hidden attacker",Members=new(){attacker.Id},Flagship=attacker.Id});s.Empires[0].Fleets!.NextFleetId=2;Observe(g,s.Empires[0].Id);CombatFixtures.Valid(g);
        var old=EmpireViewProjector.Project(g.Content,s,observer.Id);var unavailable=CombatCommandTests.Attack(s,owner:observer.Id);string token=CombatQueries.Preview(g.Content,old,unavailable).Token;
        var attack=CombatCommandTests.Attack(s);s=CombatCommandTests.Commit(g,attack with{Orders=attack.Orders with{Doctrine=CombatDoctrine.Hold}});
        Assert.Equal(JsonSerializer.Serialize(old.Combat),JsonSerializer.Serialize(EmpireViewProjector.Project(g.Content,s,observer.Id).Combat));
        for(int i=0;i<2;i++){var next=TurnResolver.Resolve(g.Content,s);Assert.True(next.Succeeded,string.Join(";",next.Errors));s=next.State;}
        Assert.True(s.Combat!.Pockets[0].Cleared);var after=EmpireViewProjector.Project(g.Content,s,observer.Id);Assert.Equal(JsonSerializer.Serialize(old.Combat),JsonSerializer.Serialize(after.Combat));Assert.Equal(token,CombatQueries.Preview(g.Content,after,unavailable).Token);Assert.False(after.Combat!.Pockets[0].Cleared);
        var fleet=observer.Fleets!.Fleets[0].Id;var travel=CommandProcessor.Execute(g.Content,s,new OpenAntares.Simulation.Fleets.FleetCommand(observer.Id,s.Turn,new OpenAntares.Simulation.Fleets.FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(fleet),pocket.Star)));Assert.True(travel.Accepted,travel.Rejection?.Message);s=travel.State;
        for(int i=0;i<20&&!s.Vessels.Any(v=>v.EmpireId==observer.Id&&v.StationaryStarId==pocket.Star);i++){var next=TurnResolver.Resolve(g.Content,s);Assert.True(next.Succeeded,string.Join(";",next.Errors));s=next.State;}
        Assert.True(EmpireViewProjector.Project(g.Content,s,observer.Id).Combat!.Pockets[0].Cleared);
    }
}
