using System;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.Turns;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Rivals;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictAtomicTurnTests
{
    internal static CommandOutcome Advance(GameSession g)
    {
        CommandOutcome? result=null;foreach(var owner in g.State.Empires.Where(e=>e.Controller==ControllerKind.Human).Select(e=>e.Id).ToArray()){result=g.Execute(new SetReadyCommand(owner,g.State.Turn,true));Assert.True(result.Accepted,result.Rejection?.Message);}return result!;
    }
    [Fact] public void PublicFinalReadyResolvesOneFrozenVolleyExactlyOnce()
    {
        var g=ConflictFixtures.WithEncounter();var members=g.State.Conflict!.Encounters[0].LowSide.Members.Concat(g.State.Conflict.Encounters[0].HighSide.Members).ToArray();var result=Advance(g);Assert.NotNull(result.Report);Assert.Equal(1,g.State.Turn);Assert.All(members,id=>Assert.Equal(80,g.State.FindVessel(id)!.Combat!.Hull));Assert.Equal(1,g.State.Conflict!.Encounters[0].Round);
    }
    [Fact] public void EveryInjectedPhaseFailureReturnsTheWholeOriginal()
    {
        foreach(var phase in Enum.GetValues<ConflictResolutionPhase>())
        {
            var g=ConflictFixtures.WithEncounter();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;Assert.True(g.Execute(new SetReadyCommand(a,0,true)).Accepted);string original=JsonSerializer.Serialize(g.State),random=JsonSerializer.Serialize(g.State.Random);bool reached=false;
            var failed=CommandProcessor.ExecuteConflict(g.Content,g.State,new SetReadyCommand(b,0,true),p=>{if(p==phase){reached=true;throw new InvalidOperationException("Injected "+phase);}});
            Assert.True(reached,phase.ToString());Assert.False(failed.Accepted);Assert.Null(failed.Report);Assert.Equal(original,JsonSerializer.Serialize(failed.State));Assert.Equal(original,JsonSerializer.Serialize(g.State));Assert.Equal(random,JsonSerializer.Serialize(failed.State.Random));
            var success=CommandProcessor.ExecuteConflict(g.Content,g.State,new SetReadyCommand(b,0,true),_=>{});Assert.True(success.Accepted,success.Rejection?.Message);Assert.Equal(1,success.State.Turn);Assert.Equal(1,success.State.Conflict!.Encounters[0].Round);
        }
    }
    [Fact] public void DeclarationPublishesWithoutDeclarationTurnShot()
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;ConflictDeclarationTests.Issue(g,a,new ConflictAction.DeclareWar(b));Advance(g);Assert.Equal(RelationKind.War,g.State.Conflict!.Pairs[0].Relation);Assert.Equal(1,g.State.Conflict.Pairs[0].WarTurn);Assert.Empty(g.State.Conflict.Encounters);Assert.All(g.State.Empires,e=>Assert.False(e.Ready));
    }
    [Fact] public void PublishedBlockadeAndAcceptedPeaceUseActualPublicBoundaryIncome()
    {
        var g=ConflictBlockadeTests.Activate(ConflictBlockadeTests.Economic());var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var colony=g.State.Colonies.First(c=>c.EmpireId==b).Id;
        Assert.Equal(450,Advance(g).Report!.Economy.Colony(colony).NetProduction.Total);foreach(var owner in new[]{a,b})ConflictDeclarationTests.Issue(g,owner,new ConflictAction.ProposePeace(owner==a?b:a));var result=Advance(g);Assert.Equal(801,result.Report!.Economy.Colony(colony).NetProduction.Total);var pair=g.State.Conflict!.Pairs.Single(p=>p.Pair.Contains(a)&&p.Pair.Contains(b));Assert.Equal(RelationKind.Peace,pair.Relation);Assert.Equal((long)g.State.Turn+10,pair.TruceEndTurn);
    }
    [Fact] public void PeaceHasNoFinalVolleyAndPreservesHullAndCohesion()
    {
        var g=ConflictFixtures.WithEncounter();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;foreach(var owner in new[]{a,b})ConflictDeclarationTests.Issue(g,owner,new ConflictAction.ProposePeace(owner==a?b:a));Advance(g);Assert.Equal(ConflictPhase.Completed,g.State.Conflict!.Encounters[0].Phase);Assert.Equal(0,g.State.Conflict.Encounters[0].Round);Assert.All(g.State.Vessels.Where(v=>v.Design is not null),v=>{Assert.Equal(100,v.Combat!.Hull);Assert.Equal(100,v.Combat.Cohesion);});
    }
    [Fact] public void CompletingRefitUnderActualAttackIsCancelledWithoutPayment()
    {
        var g=ConflictAftermathTests.CompletingRefit();var attack=ConflictOrderTests.Attack(g);var target=g.State.FindVessel(attack.Target)!;var port=g.State.Colonies.First(c=>c.EmpireId==target.EmpireId);long bank=port.ProductionReserve;ConflictDeclarationTests.Issue(g,g.State.Empires[0].Id,attack);var result=Advance(g);Assert.Equal(new DesignId(1),g.State.FindVessel(target.Id)!.Design);Assert.Equal(80,g.State.FindVessel(target.Id)!.Combat!.Hull);Assert.Equal(bank+result.Report!.Economy.Colony(port.Id).NetProduction.Total,g.State.FindColony(port.Id)!.ProductionReserve);Assert.Empty(g.State.FindEmpire(target.EmpireId)!.ShipDesigns!.Refits);Assert.DoesNotContain(result.Report.ShipDesignEvents!.Value,e=>e.Paid!=0);Assert.Single(g.State.FindColony(port.Id)!.Development!.Queue);
    }
    [Fact] public void ActualBlockadeActivationWaitsOneFullPublication()
    {
        var g=ConflictBlockadeTests.Economic();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var colony=g.State.Colonies.First(c=>c.EmpireId==b).Id;var fleet=g.State.FindEmpire(a)!.Fleets!.Fleets[0];ConflictDeclarationTests.Issue(g,a,new ConflictAction.Blockade(fleet.Id,b));
        Assert.Equal(801,Advance(g).Report!.Economy.Colony(colony).NetProduction.Total);Assert.Equal(450,g.View(b).Colonies.Single().Economy.NetProduction.Total);Assert.Equal(450,Advance(g).Report!.Economy.Colony(colony).NetProduction.Total);
    }
    [Fact] public void BothSidesWithdrawWithoutAnExtraOrdinaryVolley()
    {
        var g=ConflictFixtures.WithEncounter();Advance(g);foreach(var owner in g.State.Empires.Select(e=>e.Id).ToArray()){var e=g.View(owner).Conflict!.Encounters[0];ConflictDeclarationTests.Issue(g,owner,new ConflictAction.Withdraw(e.Id,e.Revision));}Advance(g);Assert.Equal(ConflictPhase.Completed,g.State.Conflict!.Encounters[0].Phase);Assert.Equal(1,g.State.Conflict.Encounters[0].Round);Assert.All(g.State.Vessels.Where(v=>v.Design is not null),v=>Assert.Equal(80,v.Combat!.Hull));
    }
    [Fact] public void FailureAfterActualOfficerRecoveryDrawDoesNotConsumeTheStream()
    {
        var g=ConflictFixtures.WithEncounter();var encounter=g.State.Conflict!.Encounters[0];g.State.Conflict.Encounters.Clear();var a=encounter.LowSide.Owner;var b=encounter.HighSide.Owner;var officer=g.State.FindEmpire(a)!.Fleets!.Officers[0];Assert.True(g.Execute(new OfficerCommand(a,0,new OfficerAction.AssignFlagship(officer.Id,encounter.LowSide.Fleet!.Value))).Accepted);g.State.Conflict!.Encounters.Add(encounter);g.State.FindVessel(encounter.LowSide.Flagship)!.Combat!.Hull=1;ConflictObservations.Refresh(g.Content,g.State);Assert.True(g.Execute(new SetReadyCommand(a,0,true)).Accepted);string original=JsonSerializer.Serialize(g.State);long rolls=g.State.FindEmpire(a)!.Fleets!.RecoveryRollCount;
        var failed=CommandProcessor.ExecuteConflict(g.Content,g.State,new SetReadyCommand(b,0,true),p=>{if(p==ConflictResolutionPhase.Reports)throw new InvalidOperationException("After actual recovery");});Assert.False(failed.Accepted);Assert.Equal(original,JsonSerializer.Serialize(failed.State));var success=CommandProcessor.ExecuteConflict(g.Content,g.State,new SetReadyCommand(b,0,true),_=>{});Assert.True(success.Accepted,success.Rejection?.Message);Assert.Equal(rolls+1,success.State.FindEmpire(a)!.Fleets!.RecoveryRollCount);Assert.Equal(original,JsonSerializer.Serialize(g.State));
    }
    [Fact] public void RivalsCannotAnswerAnotherRivalsUnpublishedOfferInTheSameBoundary()
    {
        var g=ConflictAiTests.WarRival();var b=g.State.Empires[1].Id;var hub=g.State.Colonies.First(c=>c.EmpireId==b);var star=g.State.FindPlanet(hub.PlanetId)!.StarId;var third=ConflictFixtures.AddNeutralObserver(g,star);g.State.FindEmpire(third)!.Controller=ControllerKind.Ai;g.State.FindEmpire(third)!.AiPolicyId="limited_conflict_v1";
        var template=g.State.Colonies.First(c=>c.EmpireId==g.State.Empires[0].Id);var planet=g.State.FindPlanet(template.PlanetId)!.Clone();planet.Id=new(g.State.AllocateId());planet.StarId=star;planet.Orbit=g.State.Planets.Where(p=>p.StarId==star).Max(p=>p.Orbit)+1;g.State.Planets.Add(planet);var colony=template.Clone();colony.Id=new(g.State.AllocateId());colony.EmpireId=third;colony.PlanetId=planet.Id;colony.ProductionReserve=5000;g.State.Colonies.Add(colony);hub.ProductionReserve=5000;var key=DiplomacyPair.For(b,third);foreach(var party in g.State.Diplomacy!.Pairs.Single(p=>p.Pair==key).Parties)party.Attitude.Streak=5;ObservationUpdater.Refresh(g.State,0);ConflictObservations.Refresh(g.Content,g.State);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        var result=RivalTurnPlanner.Prepare(g.Content,g.State);Assert.True(result.Succeeded);var offer=Assert.IsType<DiplomacyOffer>(result.State.Diplomacy!.Pairs.Single(p=>p.Pair==key).Pending);Assert.Equal(1,offer.CreatedTurn);Assert.Null(offer.Answer);
        Advance(g);Assert.Equal(1,g.State.Turn);Assert.Null(g.State.Diplomacy!.Pairs.Single(p=>p.Pair==key).Pending!.Answer);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
    }
}
