using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatCommandTests
{
    internal static CombatAction.AttackPocket Attack(GameState s,bool cargo=false,EmpireId? owner=null)
    {
        var e=s.FindEmpire(owner??s.Empires[0].Id)!;return new(e.Fleets!.Fleets[0].Id,s.Combat!.Pockets[0].Id,new(CombatDoctrine.Cautious,s.Colonies.First(c=>c.EmpireId==e.Id).Id,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair),cargo);
    }
    internal static CombatActionCommand Command(LoadedGame g,CombatAction action,EmpireId? owner=null)
    {
        var id=owner??g.State.Empires[0].Id;var preview=CombatQueries.Preview(g.Content,EmpireViewProjector.Project(g.Content,g.State,id),action);return new(id,g.State.Turn,action,preview.Token);
    }
    internal static GameState Commit(LoadedGame g,CombatAction action)
    {
        var result=CommandProcessor.Execute(g.Content,g.State,Command(g,action));Assert.True(result.Accepted,result.Rejection?.Message);return result.State;
    }
    static void Rejected(LoadedGame g,Command command,string? code=null)
    {
        string before=CombatFixtures.Bytes(g.State);var result=CommandProcessor.Execute(g.Content,g.State,command);Assert.False(result.Accepted);Assert.Same(g.State,result.State);Assert.Equal(before,CombatFixtures.Bytes(result.State));if(code is not null)Assert.Equal(code,result.Rejection!.Code);
    }
    [Fact] public void AttackRequiresLegalFleetAndExactCivilianConsent()
    {
        var g=CombatFixtures.Convoy();var action=Attack(g.State);Rejected(g,Command(g,action));
        var accepted=Commit(g,action with {AuthorizeCivilianRisk=true});var encounter=Assert.Single(accepted.Combat!.Encounters);Assert.Equal(CombatPhase.Pending,encounter.Phase);Assert.Equal(1,encounter.ConsentRevision);Assert.Equal(2,encounter.Members.Count);Assert.Equal(action.Orders,encounter.Orders);Assert.Equal(2,accepted.Combat.NextEncounterId);Assert.Empty(g.State.Combat!.Encounters);
        foreach(var mutate in new Action<GameState>[] {s=>s.Vessels[0].PendingMove=s.Stars[0].Id,s=>s.Vessels[0].Combat!.Hull=1,s=>s.Empires[0].Fleets!.Fleets[0].Order=new(FleetOrderKind.Explore),s=>s.Empires[0].Combat!.Observations.Clear()})
        {var clone=g.State.Clone();var command=Command(g,action with {AuthorizeCivilianRisk=true});mutate(clone);Rejected(new(g.Content,clone),command);}
        Rejected(g,Command(g,action with {AuthorizeCivilianRisk=true}) with {ExpectedTurn=1},RejectionCodes.StaleTurn);
        Rejected(g,Command(g,action with {AuthorizeCivilianRisk=true}) with {PreviewToken="old"},CombatRejectionCodes.StalePreview);
    }
    [Fact] public void IdenticalOrdersAreNoOpAndChangedOrdersBindNewRevision()
    {
        var initial=CombatFixtures.Duel();var s=Commit(initial,Attack(initial.State));var g=new LoadedGame(initial.Content,s);s.Empires[0].Ready=true;
        var existing=s.Combat!.Encounters[0];var action=new CombatAction.ChangeCombatOrders(existing.Id,existing.Revision,existing.Orders,false);var outcome=CommandProcessor.Execute(g.Content,s,Command(g,action));Assert.True(outcome.Accepted,outcome.Rejection?.Message);Assert.False(outcome.Changed);Assert.Same(s,outcome.State);Assert.True(outcome.State.Empires[0].Ready);
        var changed=Commit(g,action with {Orders=action.Orders with {Doctrine=CombatDoctrine.Press}});Assert.Equal(2,changed.Combat!.Encounters[0].Revision);Assert.Equal(2,changed.Combat.Encounters[0].ConsentRevision);Assert.False(changed.Empires[0].Ready);Rejected(new(g.Content,changed),Command(g,action),CombatRejectionCodes.StalePreview);
    }
    [Fact] public void TwoHumansCompeteWithoutForeignDisclosureAndCancellationReleases()
    {
        var c=CombatFixtures.Game().Content;var generated=GalaxyGenerator.CreateNewGame(c,new("small",42,9,"harl",1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:true));Assert.True(generated.Succeeded,generated.Error);var s=generated.State!;var pocket=s.Combat!.Pockets[0];
        foreach(var e in s.Empires)
        {
            e.Controller=ControllerKind.Human;e.AiPolicyId=null;var v=s.Vessels.Single(v=>v.EmpireId==e.Id);v.RoleId="escort";CombatInitializer.InitializeVessel(c,9,v);v.StationaryStarId=pocket.Star;
            if(e.Fleets!.Fleets.Count==0){v.Fleet=new(1);e.Fleets.Fleets.Add(new(){Id=new(1),Name="Fixture",Members=new(){v.Id},Flagship=v.Id});e.Fleets.NextFleetId=2;}
            e.Combat!.Observations.Add(new(pocket.Id,pocket.Star,0,false,pocket.Sentries.Select(a=>new ObservedSentry(a.Id,a.Profile,a.Hull,c.Combat!.SentryPursuitSpeed)).ToImmutableArray()));
        }
        var before=s.Clone();ObservationUpdater.Refresh(s,0);ContactUpdater.Refresh(before,s);var g=CombatFixtures.Valid(new(c,s));var first=Command(g,Attack(s),s.Empires[0].Id);var second=Command(g,Attack(s,owner:s.Empires[1].Id),s.Empires[1].Id);
        var a=CommandProcessor.Execute(c,s,first);Assert.True(a.Accepted,a.Rejection?.Message);a.State.Empires[0].Ready=true;Rejected(new(c,a.State),second,CombatRejectionCodes.Reserved);Assert.True(a.State.Empires[0].Ready);
        var e1=a.State.Combat!.Encounters[0];var cancellation=CommandProcessor.Execute(c,a.State,Command(new(c,a.State),new CombatAction.WithdrawCombat(e1.Id,e1.Revision)));Assert.True(cancellation.Accepted,cancellation.Rejection?.Message);Assert.Empty(cancellation.State.Combat!.Encounters);Assert.All(cancellation.State.Vessels.Where(v=>v.EmpireId==s.Empires[0].Id),v=>Assert.NotNull(v.PendingMove));
        var current=new LoadedGame(c,cancellation.State);var retry=CommandProcessor.Execute(c,current.State,Command(current,Attack(current.State,owner:s.Empires[1].Id),s.Empires[1].Id));Assert.True(retry.Accepted,retry.Rejection?.Message);Assert.Single(retry.State.Combat!.Encounters);
    }
    [Fact] public void CombatLocksActualMembersButAllowsRenameAndUnrelatedOrders()
    {
        var initial=CombatFixtures.Convoy();var s=Commit(initial,Attack(initial.State,true));var g=new LoadedGame(initial.Content,s);var owner=s.Empires[0].Id;var f=s.Empires[0].Fleets!.Fleets[0];var ship=s.Vessels[0];var port=s.FindPlanet(s.Colonies[0].PlanetId)!.StarId;
        foreach(var command in new Command[]{new SetMoveOrderCommand(owner,0,ship.Id,port),new SetFoundingOrderCommand(owner,0,s.Vessels[1].Id,null),new SetScoutPostureCommand(owner,0,ship.Id,OpenAntares.Simulation.Scouting.ScoutPosture.AvoidEncounters),new SetAmebaRiskOrderCommand(owner,0,ship.Id,1,1,OpenAntares.Simulation.Scouting.AmebaRiskAction.Study),new FleetCommand(owner,0,new FleetAction.Disband(f.Id)),new FleetCommand(owner,0,new FleetAction.Split(f.Id,"Split",ImmutableArray.Create(s.Vessels[1].Id))),new FleetCommand(owner,0,new FleetAction.SetFlagship(f.Id,s.Vessels[1].Id)),new FleetCommand(owner,0,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(f.Id),port)),new OfficerCommand(owner,0,new OfficerAction.AssignFlagship(s.Empires[0].Fleets!.Officers[0].Id,f.Id))})Rejected(g,command);
        var rename=CommandProcessor.Execute(g.Content,s,new FleetCommand(owner,0,new FleetAction.Rename(f.Id,"Still committed")));Assert.True(rename.Accepted,rename.Rejection?.Message);Assert.Equal(100,rename.State.Vessels[0].Combat!.Cohesion);
        Assert.True(CommandProcessor.Execute(g.Content,s,new SetResearchTargetCommand(owner,0,null)).Accepted);
        Assert.False(CombatQueries.Preview(g.Content,EmpireViewProjector.Project(g.Content,s,owner),new CombatAction.SetRepair(f.Members.ToImmutableArray(),true)).Allowed);
    }
    [Fact] public void PendingResponsesCannotBeSatisfiedByUnrelatedClicksOrStaleRevision()
    {
        var initial=CombatFixtures.Duel();var s=Commit(initial,Attack(initial.State));var e=s.Combat!.Encounters[0];e.Phase=CombatPhase.BlockedResponse;e.Revision=2;e.ConsentRevision=null;s.Empires[0].Combat!.Responses.Add(new(e.Id,2,"Revised force"));var g=CombatFixtures.Valid(new(initial.Content,s));
        Assert.NotEmpty(CombatReadyQueries.ValidateResponse(g.Content,s,s.Empires[0].Id));Rejected(g,new SetReadyCommand(s.Empires[0].Id,0,true),CombatRejectionCodes.ResponseRequired);Rejected(g,Command(g,new CombatAction.RespondCombat(e.Id,1,CombatResponse.AcceptRevisedForce)),CombatRejectionCodes.StalePreview);
        var current=Commit(g,new CombatAction.RespondCombat(e.Id,2,CombatResponse.AcceptRevisedForce));Assert.Empty(current.Empires[0].Combat!.Responses);Assert.Empty(CombatReadyQueries.ValidateResponse(g.Content,current,current.Empires[0].Id));Assert.Equal(2,current.Combat!.Encounters[0].ConsentRevision);
    }
    [Fact] public void ResponseKindsAuthorizeOnlyTheirSpecificRiskAndNeedBothWhenCombined()
    {
        var initial=CombatFixtures.Duel();var s=Commit(initial,Attack(initial.State));var e=s.Combat!.Encounters[0];e.Phase=CombatPhase.BlockedResponse;e.Revision=2;e.ConsentRevision=null;s.Empires[0].Combat!.Responses.Add(new(e.Id,2,"Both risks",CombatResponseNeed.RevisedForce|CombatResponseNeed.BlockedReturn));var g=CombatFixtures.Valid(new(initial.Content,s));
        var continued=Commit(g,new CombatAction.RespondCombat(e.Id,2,CombatResponse.RemainWithBlockedReturn));Assert.Equal(0,continued.Combat!.Encounters[0].ContinueRiskTurn);Assert.Null(continued.Combat.Encounters[0].ConsentRevision);Assert.Equal(CombatResponseNeed.RevisedForce,Assert.Single(continued.Empires[0].Combat!.Responses).Need);Assert.NotEmpty(CombatReadyQueries.ValidateResponse(g.Content,continued,continued.Empires[0].Id));
        var current=new LoadedGame(g.Content,continued);Rejected(current,Command(current,new CombatAction.RespondCombat(e.Id,2,CombatResponse.RemainWithBlockedReturn)));
        var accepted=Commit(current,new CombatAction.RespondCombat(e.Id,2,CombatResponse.AcceptRevisedForce));Assert.Empty(accepted.Empires[0].Combat!.Responses);Assert.Equal(2,accepted.Combat!.Encounters[0].ConsentRevision);
    }
    [Fact] public void ActiveRallyAuthorityMustBeReleasedBeforeAttack()
    {
        var g=CombatFixtures.Duel();var s=g.State;var owner=s.Empires[0].Id;
        var draft=new AssemblyDraft("Reinforcement fixture",s.Combat!.Pockets[0].Star,new(1),ImmutableArray.Create("escort"),ImmutableArray.Create(s.Colonies[0].Id),RequestPriority.Normal,ImmutableArray<VesselId>.Empty,false,null);
        var preview=AssemblyPlanner.Preview(FleetPlanningQueries.Create(g.Content,EmpireViewProjector.Project(g.Content,s,owner)),draft);Assert.True(preview.Allowed,string.Join(";",preview.Rejections));
        var request=CommandProcessor.Execute(g.Content,s,new AssemblyCommand(owner,0,new AssemblyAction.Accept(draft,preview.Fingerprint)));Assert.True(request.Accepted,request.Rejection?.Message);
        var bound=new LoadedGame(g.Content,request.State);Rejected(bound,Command(bound,Attack(bound.State)));
        var injected=bound.State.Clone();injected.Combat!.Encounters.Add(new(){Id=new(1),Owner=owner,Fleet=new(1),Pocket=new(1),Members=new(){injected.Vessels[0].Id},Orders=Attack(injected).Orders,ConsentRevision=1});injected.Combat.NextEncounterId=2;Assert.NotEmpty(GameStateValidator.Validate(g.Content,injected));
        var accepted=Commit(g,Attack(s));var committed=new LoadedGame(g.Content,accepted);Rejected(committed,new AssemblyCommand(owner,0,new AssemblyAction.Accept(draft,preview.Fingerprint)));
    }
    [Fact] public void OtherwiseLegalOfficerTransferIsLockedDuringCombat()
    {
        var g=CombatFixtures.Duel();var s=g.State;var owner=s.Empires[0];var main=owner.Fleets!.Fleets[0];var flagship=s.Vessels[0];var officer=owner.Fleets.Officers[0];officer.Status=OfficerStatus.Commanding;officer.PostedColony=null;officer.Flagship=flagship.Id;main.Officer=officer.Id;
        var destination=new VesselState{Id=new(s.AllocateId()),EmpireId=owner.Id,RoleId="scout",StationaryStarId=flagship.StationaryStarId,Fleet=new(2),Crisis=new()};CombatInitializer.InitializeVessel(g.Content,9,destination);OpenAntares.Simulation.Scouting.ScoutInitializer.InitializeVessel(g.Content,9,destination);s.Vessels.Add(destination);owner.Fleets.Fleets.Add(new(){Id=new(2),Name="Fixture second fleet",Members=new(){destination.Id},Flagship=destination.Id});owner.Fleets.NextFleetId=3;CombatFixtures.Valid(g);
        var command=new OfficerCommand(owner.Id,0,new OfficerAction.Transfer(officer.Id,flagship.Id,destination.Id));Assert.True(CommandProcessor.Execute(g.Content,s,command).Accepted);
        var committed=Commit(g,Attack(s));Rejected(new(g.Content,committed),command);Assert.Equal(flagship.Id,committed.Empires[0].Fleets!.Officers[0].Flagship);
    }
    [Fact] public void MalformedOrderIsRejectedWithoutExceptionOrPartialState()
    {
        var g=CombatFixtures.Duel();var bad=Attack(g.State) with {Orders=null!};Rejected(g,new CombatActionCommand(g.State.Empires[0].Id,0,bad,"invalid"));
    }
    [Fact] public void MalformedBulkSelectionIsRejectedWhileOtherShipStillMoves()
    {
        var initial=CombatFixtures.Duel();var s=Commit(initial,Attack(initial.State));var owner=s.Empires[0].Id;var port=s.FindPlanet(s.Colonies[0].PlanetId)!.StarId;
        var ship=new VesselState{Id=new(s.AllocateId()),EmpireId=owner,RoleId="scout",StationaryStarId=port,Crisis=new()};CombatInitializer.InitializeVessel(initial.Content,9,ship);OpenAntares.Simulation.Scouting.ScoutInitializer.InitializeVessel(initial.Content,9,ship);s.Vessels.Add(ship);var g=CombatFixtures.Valid(new(initial.Content,s));
        Rejected(g,new FleetCommand(owner,0,new FleetAction.MoveSelection(default,default,port)));
        var move=CommandProcessor.Execute(g.Content,s,new SetMoveOrderCommand(owner,0,ship.Id,s.Combat!.Pockets[0].Star));Assert.True(move.Accepted,move.Rejection?.Message);Assert.Equal(s.Combat.Pockets[0].Star,move.State.FindVessel(ship.Id)!.PendingMove);Assert.Single(move.State.Combat!.Encounters);
    }
    [Fact] public void RepairCannotSilentlyCompeteWithPersistentExploration()
    {
        var g=CombatFixtures.Game(enabled:false);var s=g.State;var ship=s.Vessels[0];ship.Combat!.Hull=30;ship.Fleet=new(1);var f=s.Empires[0].Fleets!;f.Fleets.Add(new(){Id=new(1),Name="Fixture exploring fleet",Members=new(){ship.Id},Flagship=ship.Id,Order=new(FleetOrderKind.Explore)});f.NextFleetId=2;CombatFixtures.Valid(g);
        var repair=new CombatAction.SetRepair(ImmutableArray.Create(ship.Id),true);Rejected(g,Command(g,repair));
        var injected=s.Clone();injected.Vessels[0].Combat!.Repairing=true;injected.Vessels[0].Combat!.ReturnColony=s.Colonies[0].Id;injected.Vessels[0].Combat!.OnArrival=CombatArrivalOrder.Repair;Assert.NotEmpty(GameStateValidator.Validate(g.Content,injected));
    }
    [Fact] public void RepairRejectsForeignOrTravellingShipsAndAuthorizesOnlyOwnPort()
    {
        var g=CombatFixtures.Game(enabled:false);var ship=g.State.Vessels[0];ship.Combat!.Hull=30;var repair=new CombatAction.SetRepair(ImmutableArray.Create(ship.Id),true);var s=Commit(g,repair);Assert.True(s.Vessels[0].Combat!.Repairing);Assert.Equal(g.State.Colonies[0].Id,s.Vessels[0].Combat!.ReturnColony);Assert.Equal(CombatArrivalOrder.Repair,s.Vessels[0].Combat!.OnArrival);
        Rejected(g,new CombatActionCommand(g.State.Empires[0].Id,0,new CombatAction.SetRepair(ImmutableArray.Create(new VesselId(999999)),true),"x"),RejectionCodes.NotOwner);
        var clone=g.State.Clone();ship=clone.Vessels[0];ship.Transit=new(ship.StationaryStarId!.Value,clone.Stars.First(st=>st.Id!=ship.StationaryStarId).Id,2,1);ship.StationaryStarId=null;Rejected(new(g.Content,clone),Command(g,repair));
    }
}
