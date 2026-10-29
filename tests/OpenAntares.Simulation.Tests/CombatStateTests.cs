using System;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatStateTests
{
    [Fact] public void ConstructedHullStartsAtFullHullAndCohesionWithoutFleetAllocation()
    {
        var g=CombatFixtures.Game(enabled:false);g.State.Colonies[0].ProductionReserve=2400;var session=new GameSession(g.Content,g.State);var owner=g.State.Empires[0].Id;
        Assert.True(session.Execute(new OpenAntares.Simulation.Commands.SetProductionTargetCommand(owner,0,g.State.Colonies[0].Id,new(ProductionKinds.Vessel,"escort"))).Accepted);
        var result=session.Execute(new OpenAntares.Simulation.Commands.SetReadyCommand(owner,0,true));Assert.True(result.Accepted,result.Rejection?.Message);
        var ship=Assert.Single(session.State.Vessels,v=>v.RoleId=="escort");Assert.NotNull(ship.Combat);Assert.Equal(100,ship.Combat.Hull);Assert.Equal(100,ship.Combat.Cohesion);Assert.Null(ship.Fleet);
        CombatFixtures.Valid(CombatFixtures.Convoy());
    }
    [Fact] public void ReadyStateWithMissingCombatFieldsRejectsBeforeConsentProjection()
    {
        var g=CombatFixtures.Duel();foreach(var mutate in new Action<GameState>[] {s=>s.Combat=null,s=>s.Empires[0].Combat=null,s=>s.Vessels[0].Combat=null})
        {var state=g.State.Clone();state.Empires[0].Ready=true;mutate(state);Assert.NotEmpty(GameStateValidator.Validate(g.Content,state,allowAllHumansReady:true));}
    }
    [Fact] public void CloneDoesNotShareCombatListsOrMutableActors()
    {
        var g=CombatFixtures.Duel();var s=g.State;var v=s.Vessels[0];var p=s.Combat!.Pockets[0];
        s.Combat.Encounters.Add(new(){Id=new(1),Owner=s.Empires[0].Id,Fleet=new(1),Pocket=p.Id,Members=new(){v.Id},Orders=new(CombatDoctrine.Cautious,s.Colonies[0].Id,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair)});s.Combat.NextEncounterId=2;
        CombatFixtures.Valid(g);string before=CombatFixtures.Bytes(s);var clone=s.Clone();Assert.NotNull(clone.Combat);Assert.NotNull(clone.Vessels[0].Combat);Assert.NotNull(clone.Empires[0].Combat);
        clone.Vessels[0].Combat!.Hull=1;clone.Combat!.Pockets[0].Sentries[0].Hull=1;clone.Combat.Encounters[0].Members.Clear();clone.Empires[0].Combat!.Observations.Clear();clone.Empires[0].Combat!.History.Add(new(new(1),new(1),0,"fixture",ImmutableArray<VesselId>.Empty,0));Assert.Equal(before,CombatFixtures.Bytes(s));
    }
    [Fact] public void RejectsMissingStateReferencesBoundsAndConflicts()
    {
        var g=CombatFixtures.Duel();var p=g.State.Combat!.Pockets[0];var s=g.State;var e=s.Empires[0];var v=s.Vessels[0];
        s.Combat.Encounters.Add(new(){Id=new(1),Owner=e.Id,Fleet=new(1),Pocket=p.Id,Members=new(){v.Id},Orders=new(CombatDoctrine.Cautious,s.Colonies[0].Id,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair)});s.Combat.NextEncounterId=2;CombatFixtures.Valid(g);
        foreach(var change in new Action<GameState>[] {
            a=>a.Combat=null,a=>a.Empires[0].Combat=null,a=>a.Vessels[0].Combat=null,a=>a.Vessels[0].Combat!.Hull=0,a=>a.Vessels[0].Combat!.Hull=101,a=>a.Vessels[0].Combat!.Cohesion=101,
            a=>a.Combat!.Pockets[0].Star=new(999999),a=>a.Combat!.Pockets[0].Sentries[0].Hull=81,a=>a.Combat!.Pockets[0].Cleared=true,a=>a.Combat!.NextEncounterId=1,
            a=>a.Combat!.Encounters[0].Members.Add(new(999999)),a=>a.Combat!.Encounters[0].Owner=new(999999),a=>a.Combat!.Encounters[0].Orders=a.Combat.Encounters[0].Orders with {ReturnColony=new(999999)},
            a=>a.Combat!.Encounters[0].Phase=(CombatPhase)999,a=>a.Combat!.Encounters[0].Round=2,a=>a.Combat!.Encounters[0].Revision=long.MaxValue,
            a=>a.Combat!.Encounters.Add(a.Combat.Encounters[0].Clone()),a=>a.Empires[0].Combat!.NextEventId=long.MaxValue,a=>a.Empires[0].Combat!.Observations.Add(a.Empires[0].Combat!.Observations[0]),
            a=>{a.Vessels[0].Combat!.Repairing=true;a.Vessels[0].Combat!.ReturnColony=a.Colonies[0].Id;},
            a=>a.Combat!.Encounters[0].Phase=CombatPhase.BlockedResponse,
            a=>{a.Vessels[0].Combat!.ReturnColony=new(999999);a.Vessels[0].Combat!.OnArrival=CombatArrivalOrder.Repair;a.Vessels[0].Combat!.Repairing=true;}
        }){var copy=s.Clone();change(copy);Assert.NotEmpty(GameStateValidator.Validate(g.Content,copy));}
        var legacy=CombatFixtures.Game(enabled:false).State;legacy.RulesVersion=8;Assert.NotEmpty(GameStateValidator.Validate(g.Content,legacy));
    }
}
