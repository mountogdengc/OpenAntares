using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatReportTests
{
    [Fact] public void PublishedReportCarriesActualOwnedEventsRoundsAndPersistentStatus()
    {
        var g=CombatTurnTests.Pending(doctrine:CombatDoctrine.Hold);var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded);Assert.Single(result.Report!.CombatEvents);Assert.Single(result.Report.CombatRounds);Assert.Empty(result.State.Empires[0].Combat!.PendingEvents);Assert.Single(result.State.Empires[0].Combat!.RecentEvents);
        string before=CombatFixtures.Bytes(result.State);var own=EmpireViewProjector.ProjectReport(g.Content,result.State,result.Report!,g.State.Empires[0].Id);Assert.Single(own.CombatEvents);Assert.Single(own.CombatRounds);Assert.Equal(before,CombatFixtures.Bytes(result.State));
        var fresh=CombatResolutionContext.Capture(g.Content,result.State);var candidate=result.State.Clone();CombatReports.Finalize(g.Content,result.State,candidate,fresh);Assert.Equal(CombatFixtures.Bytes(result.State),CombatFixtures.Bytes(candidate));
    }
    [Fact] public void LastWarshipLossRequiresSpecificResponseAndWithdrawalRemainsMandatory()
    {
        var g=CombatFixtures.Convoy();g.State.Vessels[0].Combat!.Hull=1;g=new(g.Content,CombatCommandTests.Commit(g,CombatCommandTests.Attack(g.State,true)));g=CombatSaveTests.Next(g);var e=Assert.Single(g.State.Combat!.Encounters);Assert.Equal(CombatPhase.WithdrawalQueued,e.Phase);Assert.Single(g.State.Empires[0].Combat!.Responses);Assert.NotEmpty(CombatReadyQueries.ValidateResponse(g.Content,g.State,g.State.Empires[0].Id));
        var accepted=CombatCommandTests.Commit(g,new CombatAction.RespondCombat(e.Id,e.Revision,CombatResponse.AcceptRevisedForce));Assert.Empty(accepted.FindEmpire(e.Owner)!.Combat!.Responses);Assert.Equal(CombatPhase.WithdrawalQueued,accepted.Combat!.Encounters[0].Phase);Assert.Empty(CombatReadyQueries.ValidateResponse(g.Content,accepted,e.Owner));
        var withdrawn=CombatCommandTests.Commit(g,new CombatAction.WithdrawCombat(e.Id,e.Revision));Assert.Empty(withdrawn.FindEmpire(e.Owner)!.Combat!.Responses);Assert.True(withdrawn.Combat!.Encounters[0].Revision>e.Revision);
    }
    [Fact] public void TwentyOneOrdinaryCancellationsTrimSummariesWithoutExpiringLiveResponse()
    {
        var g=CombatFixtures.Convoy();var owner=g.State.Empires[0].Id;var fleet=g.State.Empires[0].Fleets!.Fleets[0].Id;
        for(int i=0;i<21;i++)
        {
            g=new(g.Content,CombatCommandTests.Commit(g,CombatCommandTests.Attack(g.State,true)));var e=g.State.Combat!.Encounters[0];g=new(g.Content,CombatCommandTests.Commit(g,new CombatAction.WithdrawCombat(e.Id,e.Revision)));
            var hold=CommandProcessor.Execute(g.Content,g.State,new FleetCommand(owner,g.State.Turn,new FleetAction.SetOrder(ImmutableArray.Create(fleet),new(FleetOrderKind.Hold))));Assert.True(hold.Accepted,hold.Rejection?.Message);g=new(g.Content,hold.State);
        }
        Assert.Equal(20,g.State.Empires[0].Combat!.History.Count);Assert.Equal(21,g.State.Empires[0].Combat!.LifetimeEncounters);Assert.Equal(22,g.State.Combat!.NextEncounterId);
        g.State.Vessels[0].Combat!.Hull=1;g=new(g.Content,CombatCommandTests.Commit(g,CombatCommandTests.Attack(g.State,true)));g=CombatSaveTests.Next(g);Assert.Equal(20,g.State.Empires[0].Combat!.History.Count);Assert.Single(g.State.Empires[0].Combat!.Responses);var loaded=SaveGame.Deserialize(SaveGame.Serialize(g.Content,g.State));Assert.True(loaded.Succeeded,string.Join(";",loaded.Errors));Assert.NotEmpty(CombatReadyQueries.ValidateResponse(loaded.Game!.Content,loaded.Game.State,owner));Assert.Equal(22,loaded.Game.State.Empires[0].Combat!.LifetimeEncounters);
    }
    [Fact] public void BlockedRepairReportsOnceEvenAfterItsBoundedEventIsTrimmed()
    {
        // Labelled31-ship pre-session supplement isolates bounded reporting/idempotence.
        var g=CombatFixtures.Game(enabled:false);OpenAntares.Simulation.Crises.CrisisInitializer.Initialize(g.Content,g.State,true);var first=g.State.Vessels[0];first.RoleId="harl_settler";first.CarriedPopulation=1;first.Scout=null;first.Combat!.Hull=46;first.Combat.Cohesion=30;first.Combat.ReturnColony=g.State.Colonies[0].Id;first.Combat.OnArrival=CombatArrivalOrder.Repair;first.Combat.Repairing=true;
        for(int i=1;i<31;i++){var added=first.Clone();added.Id=new(g.State.AllocateId());g.State.Vessels.Add(added);}CombatFixtures.Valid(g);
        var candidate=g.State.Clone();candidate.Colonies[0].Crisis!.Quarantined=true;var context=CombatResolutionContext.Capture(g.Content,g.State);CombatRepair.Apply(g.Content,g.State,candidate,context);Assert.All(candidate.Vessels,v=>Assert.Equal(46,v.Combat!.Hull));Assert.Equal(20,candidate.Empires[0].Combat!.PendingEvents.Count);Assert.All(candidate.Empires[0].Combat!.PendingEvents,e=>Assert.Equal("repair_blocked",e.Kind));Assert.DoesNotContain(candidate.Empires[0].Combat!.PendingEvents,e=>e.Vessel==first.Id);
        string before=CombatFixtures.Bytes(candidate);CombatRepair.Apply(g.Content,g.State,candidate,CombatResolutionContext.Capture(g.Content,g.State));Assert.Equal(before,CombatFixtures.Bytes(candidate));Assert.Contains(CombatQueries.Project(g.Content,candidate,first.EmpireId)!.Notices,n=>n.Vessel==first.Id&&n.Level==CombatNoticeLevel.Attention);
    }
    [Fact] public void ReportFilteringNeverIncludesAnotherEmpiresCombatFacts()
    {
        var c=CombatFixtures.Game().Content;var s=OpenAntares.Simulation.Galaxy.GalaxyGenerator.CreateNewGame(c,new("small",42,9,"harl",1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:true)).State!;var owner=s.Empires[0];var other=s.Empires[1];other.Controller=ControllerKind.Human;other.AiPolicyId=null;var ship=s.Vessels.Single(v=>v.EmpireId==owner.Id);ship.RoleId="escort";ship.StationaryStarId=s.Combat!.Pockets[0].Star;CombatInitializer.InitializeVessel(c,9,ship);ship.Fleet=new(1);owner.Fleets!.Fleets.Add(new(){Id=new(1),Name="Private battle",Members=new(){ship.Id},Flagship=ship.Id});owner.Fleets.NextFleetId=2;ObservationUpdater.Refresh(s,0);CombatObservations.Refresh(c,s,0);var g=CombatFixtures.Valid(new(c,s));s=CombatCommandTests.Commit(g,CombatCommandTests.Attack(s));var result=TurnResolver.Resolve(c,s);Assert.True(result.Succeeded);
        var foreign=EmpireViewProjector.ProjectReport(c,result.State,result.Report!,other.Id);Assert.Empty(foreign.CombatEvents);Assert.Empty(foreign.CombatRounds);Assert.Empty(foreign.Combat!.History);Assert.Empty(foreign.Combat.Pockets);Assert.Equal(0,foreign.Combat.Totals.Encounters);var own=EmpireViewProjector.ProjectReport(c,result.State,result.Report!,owner.Id);Assert.Single(own.CombatEvents);Assert.Single(own.CombatRounds);
    }
    [Fact] public void DoctrineTriggerAndActualDetachedDeparturesHaveReportFacts()
    {
        var cautious=CombatTurnTests.Pending(39);var first=TurnResolver.Resolve(cautious.Content,cautious.State);Assert.True(first.Succeeded);Assert.Contains(first.Report!.CombatEvents,e=>e.Event.Kind=="withdrawal_queued"&&e.Event.Reason.Contains("Cautious",StringComparison.Ordinal));
        var g=CombatWithdrawalTests.Queued(CombatWithdrawalTests.Battle(new[]{100,25},navigator:1),StragglerPolicy.DetachCrippled);var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded);var property=typeof(TurnReport).GetProperty("CombatDepartures");Assert.NotNull(property);var departures=(System.Collections.IEnumerable)property!.GetValue(result.Report)!;Assert.Equal(2,departures.Cast<object>().Count());var own=EmpireViewProjector.ProjectReport(g.Content,result.State,result.Report!,g.State.Empires[0].Id);var projected=typeof(EmpireTurnReportView).GetProperty("CombatDepartures");Assert.NotNull(projected);Assert.Equal(2,((System.Collections.IEnumerable)projected!.GetValue(own)!).Cast<object>().Count());
    }
    [Fact] public void NewKnownPortClosureCanBeAnsweredAlongsideAnExistingForceResponse()
    {
        var g=CombatWithdrawalTests.Battle(new[]{1,100},new[]{true,false},crises:true);g=CombatSaveTests.Next(g);var e=g.State.Combat!.Encounters[0];Assert.Equal(CombatResponseNeed.RevisedForce,g.State.Empires[0].Combat!.Responses[0].Need);
        var quarantine=OpenAntares.Simulation.Crises.CrisisQueries.PreviewQuarantine(g.Content,g.State,e.Owner,ImmutableArray.Create(e.Orders.ReturnColony),true,false);Assert.True(quarantine.Allowed);var closed=CommandProcessor.Execute(g.Content,g.State,new SetQuarantineCommand(e.Owner,g.State.Turn,quarantine));Assert.True(closed.Accepted,closed.Rejection?.Message);g=new(g.Content,closed.State);
        var continued=CombatCommandTests.Commit(g,new CombatAction.RespondCombat(e.Id,e.Revision,CombatResponse.RemainWithBlockedReturn));Assert.Single(continued.FindEmpire(e.Owner)!.Combat!.Responses);Assert.Null(continued.Combat!.Encounters[0].ConsentRevision);var accepted=CombatCommandTests.Commit(new(g.Content,continued),new CombatAction.RespondCombat(e.Id,e.Revision,CombatResponse.AcceptRevisedForce));Assert.Empty(accepted.FindEmpire(e.Owner)!.Combat!.Responses);Assert.Equal(CombatPhase.Active,accepted.Combat!.Encounters[0].Phase);Assert.Equal(accepted.Turn,accepted.Combat.Encounters[0].ContinueRiskTurn);Assert.Empty(CombatReadyQueries.ValidateResponse(g.Content,accepted,e.Owner));Assert.False(CombatQueries.Preview(g.Content,EmpireViewProjector.Project(g.Content,accepted,e.Owner),new CombatAction.WithdrawCombat(e.Id,e.Revision)).Allowed);
    }
}
