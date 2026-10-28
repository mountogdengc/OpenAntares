using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Knowledge;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetRequestTests
{
    [Fact]
    public void ExplicitContributionPauseRetainsPaidProjectQueueAndBindings()
    {
        var g=FleetEconomyTests.Organizer();var col=g.State.Colonies[0];col.ProductionReserve=700;var request=g.State.Empires[0].Fleets!.Requests[0];var token=col.Development!.ActiveQueueToken;var paused=Run(g,new AssemblyAction.SetContributionPaused(request.Id,request.Revision,col.Id,true));Assert.True(paused.Accepted,paused.Rejection?.Message);Assert.Equal(700,paused.State.Colonies[0].ProductionReserve);Assert.Equal(token,paused.State.Colonies[0].Development!.ActiveQueueToken);Assert.Equal(col.ProjectId,paused.State.Colonies[0].ProjectId);Assert.Equal(request.Slots[0].QueueToken,paused.State.Empires[0].Fleets!.Requests[0].Slots[0].QueueToken);Assert.Contains(col.Id,paused.State.Empires[0].Fleets!.Requests[0].PausedContributions);Assert.Equal(800,OfficerBonuses.RequestShipCost(g.Content,paused.State,col.Id,"scout"));
        g=new(g.Content,paused.State);request=g.State.Empires[0].Fleets!.Requests[0];var resumed=Run(g,new AssemblyAction.SetContributionPaused(request.Id,request.Revision,col.Id,false));Assert.True(resumed.Accepted);Assert.Empty(resumed.State.Empires[0].Fleets!.Requests[0].PausedContributions);Assert.Equal(760,OfficerBonuses.RequestShipCost(g.Content,resumed.State,col.Id,"scout"));
    }
    internal static CommandOutcome Run(Saves.LoadedGame g,AssemblyAction action)=>CommandProcessor.Execute(g.Content,g.State,new AssemblyCommand(g.State.Empires[0].Id,g.State.Turn,action));
    internal static Saves.LoadedGame Accept(Saves.LoadedGame g,AssemblyDraft? draft=null)
    {
        draft??=FleetPlanningTests.Draft(g,2);var preview=FleetPlanningTests.Preview(g,draft);Assert.True(preview.Allowed,string.Join(";",preview.Rejections));
        var accepted=Run(g,new AssemblyAction.Accept(draft,preview.Fingerprint));Assert.True(accepted.Accepted,accepted.Rejection?.Message);Assert.Empty(GameStateValidator.Validate(g.Content,accepted.State));return new(g.Content,accepted.State);
    }
    internal static AssemblyPreview Retained(Saves.LoadedGame g,AssemblyRequest request,StarId? rally=null)=>AssemblyPlanner.PreviewRetained(FleetPlanningQueries.Create(g.Content,EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id)),request.Id,rally);
    [Fact]
    public void PriorityEditPreservesActiveTokenReserveAndUnrelatedOrder()
    {
        var g=FleetFixtures.Game();var col=g.State.Colonies[0];var d=col.Development!;var buildings=g.Content.Buildings.Keys.Take(2).ToArray();
        d.Queue.AddRange(new[]{new ProductionQueueEntry(10,new(ProductionKinds.Vessel,"scout")),new(11,new(ProductionKinds.Building,buildings[0])),new(12,new(ProductionKinds.Building,buildings[1]))});d.NextQueueToken=13;d.ActiveQueueToken=10;d.Origin=ProductionOrigin.Queue;col.ProjectKind=ProductionKinds.Vessel;col.ProjectId="scout";col.ProductionReserve=900;
        g=Accept(g);var request=g.State.Empires[0].Fleets!.Requests[0];col=g.State.Colonies[0];Assert.Equal(new[]{10,11,12,13,14},col.Development!.Queue.Select(q=>q.Token));
        var urgent=Run(g,new AssemblyAction.SetPriority(request.Id,request.Revision,RequestPriority.Urgent));Assert.True(urgent.Accepted,urgent.Rejection?.Message);g=new(g.Content,urgent.State);request=g.State.Empires[0].Fleets!.Requests[0];col=g.State.Colonies[0];
        Assert.Equal(new[]{10,13,14,11,12},col.Development!.Queue.Select(q=>q.Token));Assert.Equal(10,col.Development.ActiveQueueToken);Assert.Equal(900,col.ProductionReserve);Assert.Equal(15,col.Development.NextQueueToken);
        g.State.Empires[0].Ready=true;var same=Run(g,new AssemblyAction.SetPriority(request.Id,request.Revision,RequestPriority.Urgent));Assert.True(same.Accepted);Assert.Same(g.State,same.State);Assert.True(same.State.Empires[0].Ready);
        var pause=Run(g,new AssemblyAction.Pause(request.Id,request.Revision));Assert.True(pause.Accepted);g=new(g.Content,pause.State);request=g.State.Empires[0].Fleets!.Requests[0];
        var normal=Run(g,new AssemblyAction.SetPriority(request.Id,request.Revision,RequestPriority.Normal));Assert.True(normal.Accepted);Assert.Equal(RequestStatus.Paused,normal.State.Empires[0].Fleets!.Requests[0].Status);Assert.Equal(new[]{10,11,12,13,14},normal.State.Colonies[0].Development!.Queue.Select(q=>q.Token));
    }
    [Fact]
    public void OffGovernorStoredTemplateNeverGainsTemplateAuthority()
    {
        var g=FleetFixtures.Nested();var col=g.State.Colonies[0];var definition=g.Content.GovernorTemplates.Values.First();col.Development!.Template=new(definition.Id,1,new(definition.Name,definition.FoundationIds,definition.DevelopmentIds,definition.SupportTarget,definition.ProductionWeight,definition.ResearchWeight));col.Development.Queue.Add(new(2,new(ProductionKinds.Vessel,"scout")));col.Development.NextQueueToken=3;
        var plan=GovernorPlanner.Plan(GovernorPlanningQueries.Create(g.Content,EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id)));Assert.True(plan.Succeeded);Assert.NotNull(plan.Orders.Single().Target);Assert.Equal("scout",plan.Orders.Single().Target!.ContentId);Assert.Equal(1,plan.Orders.Single().QueueToken);Assert.False(col.Development.ConstructionEnabled);
        var prepared=GovernorTurnCoordinator.Prepare(g.Content,g.State,g.State.Empires[0].Id);Assert.True(prepared.Succeeded);Assert.Equal("scout",prepared.State.Colonies[0].ProjectId);Assert.Equal(1,prepared.State.Colonies[0].Development!.ActiveQueueToken);Assert.False(prepared.State.Colonies[0].Development!.ConstructionEnabled);
        var vessel=g.State.Vessels.First(v=>v.EmpireId==g.State.Empires[0].Id).Clone();vessel.Id=new(g.State.NextEntityId++);vessel.Fleet=null;vessel.NewlyCompleted=true;vessel.BuildOrigin=new(new(1),1,col.Id,1);g.State.Vessels.Add(vessel);
        var slot=g.State.Empires[0].Fleets!.Requests[0].Slots[0];slot.QueueToken=null;slot.Vessel=vessel.Id;slot.Status=SlotStatus.Built;slot.NewHull=true;col.Development.Queue.RemoveAll(q=>q.Token==1);
        FleetFixtures.Valid(g);var ended=GovernorPlanner.Plan(GovernorPlanningQueries.Create(g.Content,EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id)));Assert.True(ended.Succeeded);Assert.Null(ended.Orders.Single().Target);Assert.False(col.Development.ConstructionEnabled);
    }
    [Fact]
    public void CargoNeedsExplicitAuthorization()
    {
        foreach(string species in new[]{"harl","peldra","sulan","wenth"})
        {
            var g=FleetFixtures.Game(species);string role=g.Content.Species[species].AllowedVesselIds.First(id=>g.Content.Vessels[id].EmbarkPopulation>0);var draft=FleetPlanningTests.Draft(g,1) with{Roles=ImmutableArray.Create(role)};
            var denied=Run(g,new AssemblyAction.Accept(draft,"unapproved"));Assert.False(denied.Accepted);Assert.Same(g.State,denied.State);
            var accepted=Accept(g,draft with{AuthorizeCargo=true});Assert.True(accepted.State.Empires[0].Fleets!.Requests[0].AuthorizeCargo);
        }
    }
    [Fact]
    public void SafetyPauseRetainsLivingBindingsAndRequiresLegalResume()
    {
        var g=FleetFixtures.Game(amebas:true);var habitat=g.State.Scouting!.Habitat!;var ship=g.State.Vessels[0];var home=ship.StationaryStarId!.Value;
        g=Accept(g,FleetPlanningTests.Draft(g,1) with{Rally=habitat.Star,AcceptedIdleVessels=ImmutableArray.Create(ship.Id)});var r=g.State.Empires[0].Fleets!.Requests[0];var slot=r.Slots[0];var binding=slot.Vessel;
        g.State.Empires[0].Scouting!.KnownHabitats.Add(new(habitat.Id,habitat.Star,0));RequestQueueAuthority.SafetyPause(g.State,g.State.Empires[0].Id,r.Id,"Known ameba at rally.");
        Assert.Equal(RequestStatus.SafetyPaused,r.Status);Assert.Equal(SlotStatus.BlockedSafety,slot.Status);Assert.Equal(SlotStatus.Built,slot.ResumeStatus);Assert.Equal(binding,slot.Vessel);Assert.False(Retained(g,r).Allowed);
        var deny=Run(g,new AssemblyAction.Resume(r.Id,r.Revision,"stale"));Assert.False(deny.Accepted);
        var legal=Retained(g,r,home);Assert.True(legal.Allowed,string.Join(";",legal.Rejections));var change=Run(g,new AssemblyAction.ChangeRally(r.Id,r.Revision,home,null,legal.Fingerprint));Assert.True(change.Accepted,change.Rejection?.Message);g=new(g.Content,change.State);r=g.State.Empires[0].Fleets!.Requests[0];
        Assert.Equal(RequestStatus.SafetyPaused,r.Status);var fresh=Retained(g,r);Assert.True(fresh.Allowed);var resumed=Run(g,new AssemblyAction.Resume(r.Id,r.Revision,fresh.Fingerprint));Assert.True(resumed.Accepted,resumed.Rejection?.Message);Assert.Equal(RequestStatus.Active,resumed.State.Empires[0].Fleets!.Requests[0].Status);Assert.Equal(binding,resumed.State.Empires[0].Fleets!.Requests[0].Slots[0].Vessel);
    }
    [Fact]
    public void MissingSlotBlocksCompletionUntilFiniteAmendment()
    {
        var g=FleetFixtures.Game();var ship=g.State.Vessels[0];g=Accept(g,FleetPlanningTests.Draft(g,1) with{AcceptedIdleVessels=ImmutableArray.Create(ship.Id)});var r=g.State.Empires[0].Fleets!.Requests[0];
        var detached=Run(g,new AssemblyAction.DetachSlot(r.Id,r.Revision,r.Slots[0].Ordinal));Assert.True(detached.Accepted,detached.Rejection?.Message);g=new(g.Content,detached.State);r=g.State.Empires[0].Fleets!.Requests[0];Assert.Equal(SlotStatus.Missing,r.Slots[0].Status);Assert.Empty(g.State.Colonies[0].Development!.Queue);
        var unchanged=Retained(g,r);Assert.True(unchanged.Allowed);var resumed=Run(g,new AssemblyAction.Resume(r.Id,r.Revision,unchanged.Fingerprint));Assert.True(resumed.Accepted);g=new(g.Content,resumed.State);r=g.State.Empires[0].Fleets!.Requests[0];Assert.Equal(SlotStatus.Missing,r.Slots[0].Status);Assert.Empty(g.State.Colonies[0].Development!.Queue);
        var draft=FleetPlanningTests.Draft(g,1);var preview=AssemblyPlanner.Preview(FleetPlanningQueries.Create(g.Content,EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id)),draft,r.Id);Assert.True(preview.Allowed,string.Join(";",preview.Rejections));
        var amended=Run(g,new AssemblyAction.Amend(r.Id,r.Revision,draft,preview.Fingerprint));Assert.True(amended.Accepted,amended.Rejection?.Message);Assert.Single(amended.State.Colonies[0].Development!.Queue);Assert.Equal(SlotStatus.Queued,amended.State.Empires[0].Fleets!.Requests[0].Slots[0].Status);Assert.NotNull(amended.State.FindVessel(ship.Id));
    }
    [Theory]
    [InlineData(RequestCancellation.KeepQueue)]
    [InlineData(RequestCancellation.RemoveUnstarted)]
    public void CancellationRetainsActiveWorkAndReleasesReservations(RequestCancellation choice)
    {
        var g=Accept(FleetFixtures.Game());var col=g.State.Colonies[0];var r=g.State.Empires[0].Fleets!.Requests[0];int first=r.Slots[0].QueueToken!.Value,second=r.Slots[1].QueueToken!.Value;
        col.ProjectKind=ProductionKinds.Vessel;col.ProjectId="scout";col.Development!.ActiveQueueToken=first;col.Development.Origin=ProductionOrigin.Queue;col.ProductionReserve=700;
        var cancelled=Run(g,new AssemblyAction.Cancel(r.Id,r.Revision,choice));Assert.True(cancelled.Accepted,cancelled.Rejection?.Message);col=cancelled.State.Colonies[0];r=cancelled.State.Empires[0].Fleets!.Requests[0];Assert.Equal(RequestStatus.Cancelled,r.Status);Assert.False(r.ReservedFleetSlot);Assert.Equal(700,col.ProductionReserve);Assert.Equal(first,col.Development!.ActiveQueueToken);Assert.Contains(col.Development.Queue,q=>q.Token==first);Assert.Equal(choice==RequestCancellation.KeepQueue,col.Development.Queue.Any(q=>q.Token==second));
    }
    [Fact]
    public void StaleOrOverflowAcceptanceIsAtomic()
    {
        var g=FleetFixtures.Game();var draft=FleetPlanningTests.Draft(g,1);var preview=FleetPlanningTests.Preview(g,draft);g.State.Colonies[0].Development!.NextQueueToken++;
        string before=FleetFixtures.Bytes(g.State);var stale=Run(g,new AssemblyAction.Accept(draft,preview.Fingerprint));Assert.False(stale.Accepted);Assert.Same(g.State,stale.State);Assert.Equal(before,FleetFixtures.Bytes(g.State));
        var d=g.State.Colonies[0].Development!;for(int i=0;i<32;i++)d.Queue.Add(new(d.NextQueueToken++,new(ProductionKinds.Vessel,"scout")));
        Assert.False(FleetPlanningTests.Preview(g,draft).Allowed);Assert.Empty(g.State.Empires[0].Fleets!.Requests);
    }
    [Fact]
    public void AmendmentRetainsPaidWorkAndNeverReusesRemovedSlotOrdinals()
    {
        var g=Accept(FleetFixtures.Game());var col=g.State.Colonies[0];var r=g.State.Empires[0].Fleets!.Requests[0];int first=r.Slots[0].QueueToken!.Value,second=r.Slots[1].QueueToken!.Value;
        col.ProjectKind=ProductionKinds.Vessel;col.ProjectId="scout";col.Development!.ActiveQueueToken=first;col.Development.Origin=ProductionOrigin.Queue;col.ProductionReserve=700;
        var draft=FleetPlanningTests.Draft(g,3);var preview=AssemblyPlanner.Preview(FleetPlanningQueries.Create(g.Content,EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id)),draft,r.Id);Assert.True(preview.Allowed,string.Join(";",preview.Rejections));Assert.Equal(2,preview.Slots.Count(s=>s.RetainedSlotOrdinal is not null));
        var grown=Run(g,new AssemblyAction.Amend(r.Id,r.Revision,draft,preview.Fingerprint));Assert.True(grown.Accepted,grown.Rejection?.Message);g=new(g.Content,grown.State);r=g.State.Empires[0].Fleets!.Requests[0];Assert.Equal(new[]{first,second,3},g.State.Colonies[0].Development!.Queue.Select(q=>q.Token));Assert.Equal(700,g.State.Colonies[0].ProductionReserve);Assert.Equal(4,r.NextSlotOrdinal);
        draft=FleetPlanningTests.Draft(g,1);preview=AssemblyPlanner.Preview(FleetPlanningQueries.Create(g.Content,EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id)),draft,r.Id);var reduced=Run(g,new AssemblyAction.Amend(r.Id,r.Revision,draft,preview.Fingerprint));Assert.True(reduced.Accepted,reduced.Rejection?.Message);g=new(g.Content,reduced.State);r=g.State.Empires[0].Fleets!.Requests[0];Assert.Equal(first,Assert.Single(g.State.Colonies[0].Development!.Queue).Token);Assert.Equal(4,r.NextSlotOrdinal);
        draft=FleetPlanningTests.Draft(g,2);preview=AssemblyPlanner.Preview(FleetPlanningQueries.Create(g.Content,EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id)),draft,r.Id);var regrown=Run(g,new AssemblyAction.Amend(r.Id,r.Revision,draft,preview.Fingerprint));Assert.True(regrown.Accepted,regrown.Rejection?.Message);Assert.Equal(new[]{1,4},regrown.State.Empires[0].Fleets!.Requests[0].Slots.Select(s=>s.Ordinal));Assert.Equal(700,regrown.State.Colonies[0].ProductionReserve);
        g=new(g.Content,regrown.State);r=g.State.Empires[0].Fleets!.Requests[0];g.State.Empires[0].Ready=true;preview=AssemblyPlanner.Preview(FleetPlanningQueries.Create(g.Content,EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id)),draft,r.Id);var same=Run(g,new AssemblyAction.Amend(r.Id,r.Revision,draft,preview.Fingerprint));Assert.True(same.Accepted,same.Rejection?.Message);Assert.Same(g.State,same.State);
    }
    [Fact]
    public void ManualProjectAndQueueChangesPauseOnlyTheAffectedContribution()
    {
        var g=FleetFixtures.Game();FleetOfficerTests.AddColony(g);g=Accept(g);var col=g.State.Colonies[0];var r=g.State.Empires[0].Fleets!.Requests[0];
        string building=g.Content.Buildings.Values.First(b=>b.PrerequisiteTechnologyIds.IsEmpty&&!col.CompletedBuildingIds.Contains(b.Id)).Id;
        var manual=CommandProcessor.Execute(g.Content,g.State,new SetProjectCommand(g.State.Empires[0].Id,0,col.Id,building));Assert.True(manual.Accepted,manual.Rejection?.Message);g=new(g.Content,manual.State);r=g.State.Empires[0].Fleets!.Requests[0];Assert.Equal(col.Id,Assert.Single(r.PausedContributions));Assert.Equal(RequestStatus.Active,r.Status);Assert.All(r.Slots,s=>Assert.NotNull(s.QueueToken));
        var queued=g.State.Colonies.First(c=>r.Slots.Any(s=>s.Colony==c.Id));var remove=CommandProcessor.Execute(g.Content,g.State,new SetColonyQueueCommand(g.State.Empires[0].Id,0,queued.Id,ImmutableArray<QueueEntryDraft>.Empty));Assert.True(remove.Accepted,remove.Rejection?.Message);Assert.Contains(remove.State.Empires[0].Fleets!.Requests[0].Slots,s=>s.Status==SlotStatus.Missing);Assert.Empty(GameStateValidator.Validate(g.Content,remove.State));
    }
    [Fact]
    public void DisbandingRallyPausesAndRetainsLivingBindings()
    {
        var g=FleetFixtures.Game();var ship=g.State.Vessels[0];var create=FleetCommandTests.Run(g,new FleetAction.Create("Rally",ImmutableArray.Create(ship.Id)));Assert.True(create.Accepted);g=new(g.Content,create.State);
        g=Accept(g,FleetPlanningTests.Draft(g,2) with{RallyFleet=new(1),AcceptedIdleVessels=ImmutableArray.Create(ship.Id)});var r=g.State.Empires[0].Fleets!.Requests[0];r.Slots[0].Status=SlotStatus.Arrived;FleetFixtures.Valid(g);int queued=r.Slots[1].QueueToken!.Value;
        var disband=FleetCommandTests.Run(g,new FleetAction.Disband(new(1)));Assert.True(disband.Accepted,disband.Rejection?.Message);r=disband.State.Empires[0].Fleets!.Requests[0];Assert.Equal(RequestStatus.Paused,r.Status);Assert.True(r.ReservedFleetSlot);Assert.Null(r.RallyFleet);Assert.Equal(ship.Id,r.Slots[0].Vessel);Assert.Equal(SlotStatus.Built,r.Slots[0].Status);Assert.Equal(queued,r.Slots[1].QueueToken);Assert.Empty(GameStateValidator.Validate(g.Content,disband.State));
    }
    [Fact]
    public void SafetyRallyChangePreservesCommittedTransit()
    {
        var g=FleetFixtures.Game();var ship=g.State.Vessels[0];var home=ship.StationaryStarId!.Value;var destination=g.State.Stars.First(s=>s.Id!=home).Id;
        g=Accept(g,FleetPlanningTests.Draft(g,1) with{Rally=destination,AcceptedIdleVessels=ImmutableArray.Create(ship.Id)});var r=g.State.Empires[0].Fleets!.Requests[0];var vessel=g.State.FindVessel(ship.Id)!;vessel.Transit=new(home,destination,4,3);vessel.StationaryStarId=null;r.Slots[0].Status=SlotStatus.Travelling;
        RequestQueueAuthority.SafetyPause(g.State,g.State.Empires[0].Id,r.Id,"Observed unsafe rally.");var committed=vessel.Transit;var preview=Retained(g,r,home);Assert.True(preview.Allowed,string.Join(";",preview.Rejections));var changed=Run(g,new AssemblyAction.ChangeRally(r.Id,r.Revision,home,null,preview.Fingerprint));Assert.True(changed.Accepted,changed.Rejection?.Message);Assert.Equal(committed,changed.State.FindVessel(ship.Id)!.Transit);Assert.Equal(SlotStatus.Travelling,changed.State.Empires[0].Fleets!.Requests[0].Slots[0].ResumeStatus);
    }
    [Fact]
    public void ColonyAndRallyReservationsAreExclusive()
    {
        var g=FleetFixtures.Game();var extra=FleetOfficerTests.AddColony(g);var created=FleetCommandTests.Run(g,new FleetAction.Create("Rally",ImmutableArray.Create(g.State.Vessels[0].Id)));Assert.True(created.Accepted);g=new(g.Content,created.State);
        var draft=FleetPlanningTests.Draft(g,1) with{Colonies=ImmutableArray.Create(g.State.Colonies[0].Id),RallyFleet=new(1)};g=Accept(g,draft);
        string before=FleetFixtures.Bytes(g.State);Assert.False(FleetPlanningTests.Preview(g,draft with{RallyFleet=null}).Allowed);Assert.False(FleetPlanningTests.Preview(g,draft with{Colonies=ImmutableArray.Create(extra.Id)}).Allowed);Assert.Equal(before,FleetFixtures.Bytes(g.State));
    }
    [Fact]
    public void AuthorizedCargoStillWaitsForQuarantineRelease()
    {
        var c=FleetContentTests.Core();var generated=Galaxy.GalaxyGenerator.CreateNewGame(c,new("small",42,8,"harl",CrisesEnabled:true,AmebasEnabled:false));Assert.True(generated.Succeeded,generated.Error);var g=new Saves.LoadedGame(c,generated.State!);
        var col=g.State.Colonies[0];col.Crisis!.Quarantined=true;FleetFixtures.Valid(g);
        var draft=FleetPlanningTests.Draft(g,1) with{Roles=ImmutableArray.Create("harl_settler"),AuthorizeCargo=true};Assert.False(FleetPlanningTests.Preview(g,draft).Allowed);
        draft=draft with{Rally=g.State.Stars.First(s=>s.Id!=draft.Rally).Id};var preview=FleetPlanningTests.Preview(g,draft);Assert.True(preview.Allowed,string.Join(";",preview.Rejections));Assert.Null(preview.Slots[0].ArrivalTurns);g=Accept(g,draft);
        var prepared=GovernorTurnCoordinator.Prepare(g.Content,g.State,g.State.Empires[0].Id);Assert.True(prepared.Succeeded);prepared.State.Colonies[0].ProductionReserve=1600;
        var forecast=Economy.EconomyCalculator.CalculateColony(g.Content,prepared.State,prepared.State.Colonies[0]);Assert.Equal("quarantine",forecast.Production.BlockedReasonCode);Assert.Null(forecast.VesselCompletionPlan);Assert.Null(forecast.Production.CompletedId);
    }
}
