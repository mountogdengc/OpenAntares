using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class RefitCommandTests
{
    [Fact]
    public void StartRefitRequiresSameFrameFullConditionAndIdlePort()
    {
        var session=Setup();var s=session.State;var e=s.Empires[0];var v=s.Vessels[0];var col=s.Colonies[0];col.ProductionReserve=300;
        var template=session.Content.GovernorTemplates.Keys.First();Do(session,new AssignGovernorTemplateCommand(e.Id,0,ImmutableArray.Create(col.Id),template,true,true));Do(session,new SetColonyQueueCommand(e.Id,0,col.Id,ImmutableArray.Create(new QueueEntryDraft(null,new(ProductionKinds.Vessel,"scout")))));
        var before=session.State.Clone();var preview=Preview(session,new RefitAction.Start(v.Id,col.Id,new(2)));Assert.True(preview.Allowed,preview.RejectionCode);Assert.Equal(700,preview.Price);Assert.Equal(300,preview.Before!.BaseSpeed);Assert.Equal(400,preview.After!.BaseSpeed);Assert.NotEmpty(preview.Consequences);
        Apply(session,new RefitAction.Start(v.Id,col.Id,new(2)));var job=Assert.Single(session.State.Empires[0].ShipDesigns!.Refits);Assert.Equal(new RefitId(1),job.Id);Assert.Equal(2,session.State.Empires[0].ShipDesigns!.NextRefitId);Assert.Equal(700,job.Price);Assert.Equal(new DesignId(1),session.State.Vessels[0].Design);
        var after=session.State.Colonies[0];Assert.Equal(300,after.ProductionReserve);Assert.Equal(ProductionKinds.Refit,after.ProjectKind);Assert.Equal("1",after.ProjectId);Assert.False(after.Development!.ConstructionEnabled);Assert.True(after.Development.WorkforceEnabled);Assert.Equal(before.Colonies[0].Development!.Queue,after.Development.Queue);Assert.Equal(before.Colonies[0].Development!.Template,after.Development.Template);Assert.Equal(before.Colonies[0].Workforce,after.Workforce);Assert.Equal(before.NextEntityId,session.State.NextEntityId);Assert.Equal(before.Random.State,session.State.Random.State);Assert.Empty(GameStateValidator.Validate(session.Content,session.State));
    }
    [Theory]
    [InlineData("cross_frame")][InlineData("hull")][InlineData("cohesion")][InlineData("cargo")][InlineData("foreign_ship")][InlineData("foreign_port")][InlineData("project")][InlineData("same_equipment")][InlineData("archived")][InlineData("locked")][InlineData("transit")][InlineData("pending_move")][InlineData("pending_founding")][InlineData("ameba")][InlineData("return")][InlineData("repair")][InlineData("active_request")]
    public void InvalidRefitInputsRejectWithoutMutation(string fault)
    {
        var session=Setup();var s=session.State;var v=s.Vessels[0];var col=s.Colonies[0];DesignId target=new(2);VesselId ship=v.Id;ColonyId port=col.Id;var e=s.Empires[0];
        switch(fault)
        {
            case "cross_frame":target=ShipDesignFixtures.SaveDesign(session,"Frigate",new("frigate_frame","conventional_drive",null,ImmutableArray.Create("pulse_bank")));break;
            case "hull":v.Combat!.Hull=99;break;case "cohesion":v.Combat!.Cohesion=99;break;case "cargo":v.CarriedPopulation=1;break;
            case "foreign_ship":ship=new(999);break;case "foreign_port":port=new(999);break;
            case "project":col.ProjectKind=ProductionKinds.Building;col.ProjectId=session.Content.Buildings.Values.First(b=>b.PrerequisiteTechnologyIds.IsEmpty).Id;break;
            case "same_equipment":target=ShipDesignFixtures.SaveDesign(session,"Same equipment",session.Content.ShipDesigns!.StartingRecipe);break;
            case "archived":Do(session,new ShipDesignCommand(e.Id,0,e.ShipDesigns!.CatalogueRevision,new ShipDesignAction.Archive(target)));break;
            case "locked":e.KnownTechnologyIds.Remove("vector_drives");break;
            case "transit":v.Transit=new(v.StationaryStarId!.Value,s.Stars.First(st=>st.Id!=v.StationaryStarId).Id,2,2);v.StationaryStarId=null;break;
            case "pending_move":v.PendingMove=s.Stars.First(st=>st.Id!=v.StationaryStarId).Id;break;
            case "pending_founding":v.PendingFounding=s.Planets[0].Id;break;
            case "ameba":v.AmebaEncounter=new(){Number=1,HabitatId=1,Stage=AmebaStage.Warning};break;
            case "return":v.Combat!.ReturnColony=col.Id;v.Combat.OnArrival=CombatArrivalOrder.Hold;break;
            case "repair":v.Combat!.ReturnColony=col.Id;v.Combat.OnArrival=CombatArrivalOrder.Repair;v.Combat.Repairing=true;break;
            case "active_request":e.Fleets!.Requests.Add(new(){Id=new(1),Slots=new(){new(){Vessel=v.Id,RoleId=v.RoleId,Design=v.Design,Ordinal=1,Status=SlotStatus.Built}},Status=RequestStatus.Active});break;
        }
        var action=new RefitAction.Start(ship,port,target);string before=Bytes(session);var p=Preview(session,action);Assert.False(p.Allowed);var result=session.Execute(new RefitCommand(session.State.Empires[0].Id,session.State.Turn,action,p.Token));Assert.False(result.Accepted);Assert.Equal(before,Bytes(session));
    }
    [Fact]
    public void IdenticalRefitIsANoOpAndCancelPreservesBankedReserve()
    {
        var session=Setup();var action=new RefitAction.Start(session.State.Vessels[0].Id,session.State.Colonies[0].Id,new(2));Apply(session,action);session.State.Empires[0].Ready=true;session.State.Colonies[0].ProductionReserve=500;
        string before=Bytes(session);var p=Preview(session,action);Assert.True(p.Allowed,p.RejectionCode);var result=session.Execute(new RefitCommand(session.State.Empires[0].Id,0,action,p.Token));Assert.True(result.Accepted,result.Rejection?.Message);Assert.False(result.Changed);Assert.Equal(before,Bytes(session));
        var job=session.State.Empires[0].ShipDesigns!.Refits.Single();Apply(session,new RefitAction.Cancel(job.Id,job.Revision));Assert.Empty(session.State.Empires[0].ShipDesigns!.Refits);Assert.Equal(500,session.State.Colonies[0].ProductionReserve);Assert.Equal(new DesignId(1),session.State.Vessels[0].Design);Assert.Null(session.State.Colonies[0].ProjectId);Assert.False(session.State.Colonies[0].Development!.ConstructionEnabled);
    }
    [Theory]
    [InlineData("condition")][InlineData("source")][InlineData("target")][InlineData("port")][InlineData("turn")][InlineData("reserve")][InlineData("cost")]
    public void RelevantChangesRejectOldPreview(string change)
    {
        var session=Setup();var action=new RefitAction.Start(session.State.Vessels[0].Id,session.State.Colonies[0].Id,new(2));var p=Preview(session,action);Assert.True(p.Allowed,p.RejectionCode);
        switch(change){case "condition":session.State.Vessels[0].Combat!.Hull=99;break;case "source":session.State.Vessels[0].Design=new(2);break;case "target":session.State.Empires[0].ShipDesigns!.Designs[1]=session.State.Empires[0].ShipDesigns!.Designs[1] with{Archived=true};break;case "port":session.State.Colonies[0].ProjectId="factory";break;case "turn":session.State.Turn++;break;case "reserve":session.State.Colonies[0].ProductionReserve++;break;case "cost":var c=session.Content;var d=c.ShipDesigns!;session=new(c with{ShipDesigns=d with{Components=d.Components.SetItem("vector_drive",d.Components["vector_drive"] with{Cost=1200})}},session.State);break;}
        string before=Bytes(session);var r=session.Execute(new RefitCommand(session.State.Empires[0].Id,session.State.Turn,action,p.Token));Assert.False(r.Accepted);Assert.Equal(before,Bytes(session));
    }
    [Fact]
    public void PausedJobRequiresFreshResumeAndKeepsFrozenPrice()
    {
        var session=Setup();Apply(session,new RefitAction.Start(session.State.Vessels[0].Id,session.State.Colonies[0].Id,new(2)));var job=session.State.Empires[0].ShipDesigns!.Refits.Single();job.Status=RefitStatus.Paused;job.Reason="Labelled paused supplement";job.Revision++;
        var e=session.State.Empires[0];Do(session,new ShipDesignCommand(e.Id,0,e.ShipDesigns!.CatalogueRevision,new ShipDesignAction.Archive(new(2))));job=session.State.Empires[0].ShipDesigns!.Refits.Single();Apply(session,new RefitAction.Resume(job.Id,job.Revision));job=session.State.Empires[0].ShipDesigns!.Refits.Single();Assert.Equal(RefitStatus.Active,job.Status);Assert.Null(job.Reason);Assert.Equal(700,job.Price);
    }
    [Fact]
    public void RefittingFlagshipKeepsOfficerButLocksAffectedCommands()
    {
        var session=Setup();var s=session.State;var e=s.Empires[0];var ship=s.Vessels[0];var other=ship.Clone();other.Id=new(s.AllocateId());s.Vessels.Add(other);
        Fleet(session,new FleetAction.Create("Refit fleet",ImmutableArray.Create(ship.Id,other.Id),ship.Id));var fleet=session.State.Empires[0].Fleets!.Fleets.Single();var officer=session.State.Empires[0].Fleets!.Officers[0];var assigned=FleetOfficerTests.Run(new LoadedGame(session.Content,session.State),new OfficerAction.AssignFlagship(officer.Id,fleet.Id));Assert.True(assigned.Accepted,assigned.Rejection?.Message);session=new(session.Content,assigned.State);
        Apply(session,new RefitAction.Start(ship.Id,s.Colonies[0].Id,new(2)));string career=JsonSerializer.Serialize(session.State.Empires[0].Fleets!.Officers);var destination=session.State.Stars.First(st=>st.Id!=ship.StationaryStarId).Id;
        foreach(var action in new FleetAction[]{new FleetAction.SetOrder(ImmutableArray.Create(fleet.Id),new(FleetOrderKind.Return,destination)),new FleetAction.SetFlagship(fleet.Id,other.Id),new FleetAction.Disband(fleet.Id),new FleetAction.MoveSelection(ImmutableArray.Create(ship.Id,other.Id),ImmutableArray<FleetId>.Empty,destination),new FleetAction.DetachSelection(ImmutableArray.Create(ship.Id))})
        {Assert.False(FleetQueries.PreviewAction(session.Content,session.View(e.Id),action).Allowed);RejectSame(session,new FleetCommand(e.Id,0,action));}
        RejectSame(session,new OfficerCommand(e.Id,0,new OfficerAction.Remove(officer.Id,s.Colonies[0].Id)));RejectSame(session,new SetMoveOrderCommand(e.Id,0,ship.Id,destination));Assert.Equal(career,JsonSerializer.Serialize(session.State.Empires[0].Fleets!.Officers));
        Fleet(session,new FleetAction.Split(fleet.Id,"Unrelated",ImmutableArray.Create(other.Id),other.Id));Assert.Equal(fleet.Id,session.State.FindVessel(ship.Id)!.Fleet);Assert.Equal(officer.Id,session.State.Empires[0].Fleets!.Fleets.Single(f=>f.Id==fleet.Id).Officer);Assert.Equal(career,JsonSerializer.Serialize(session.State.Empires[0].Fleets!.Officers));
    }
    [Fact]
    public void OrdinaryShipyardRepairAndScoutOrdersCannotCancelRefit()
    {
        var session=Setup();var e=session.State.Empires[0];var v=session.State.Vessels[0];var c=session.State.Colonies[0];Apply(session,new RefitAction.Start(v.Id,c.Id,new(2)));
        foreach(var command in new Command[]{new SetProjectCommand(e.Id,0,c.Id,null),new SetProductionTargetCommand(e.Id,0,c.Id,ShipDesignTargets.Build(new(1))),new SetColonyQueueCommand(e.Id,0,c.Id,ImmutableArray<QueueEntryDraft>.Empty),new SetColonyAutomationCommand(e.Id,0,c.Id,false,true),new SetScoutPostureCommand(e.Id,0,v.Id,ScoutPosture.AvoidEncounters),new CombatActionCommand(e.Id,0,new CombatAction.SetRepair(ImmutableArray.Create(v.Id),true),string.Empty)})RejectSame(session,command);
        Assert.Single(session.State.Empires[0].ShipDesigns!.Refits);Assert.Equal(new DesignId(1),session.State.Vessels[0].Design);
    }
    [Fact]
    public void HiddenRivalRecipeDoesNotChangeOwnPreview()
    {
        var core=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;var game=GalaxyGenerator.CreateNewGame(core,new("small",42,10,"harl",1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));Assert.True(game.Succeeded,game.Error);var s=game.State!;var e=s.Empires[0];e.KnownTechnologyIds=new(){"industrial_coordination","vector_drives"};var ship=s.Vessels.Single(v=>v.EmpireId==e.Id);ship.RoleId="escort";ship.Design=new(1);ship.Combat=new(){Hull=100};var session=new GameSession(core,s);ShipDesignFixtures.SaveDesign(session,"Fast",core.ShipDesigns!.StartingRecipe with{EngineId="vector_drive"});var action=new RefitAction.Start(ship.Id,s.Colonies[0].Id,new(2));var before=Preview(session,action);Assert.True(before.Allowed,before.RejectionCode);string view=JsonSerializer.Serialize(session.View(e.Id));
        var rival=session.State.Empires[1];rival.ShipDesigns!.Designs.Add(new(new(2),"Secret",core.ShipDesigns.StartingRecipe,false));rival.ShipDesigns.NextDesignId=3;rival.ShipDesigns.CatalogueRevision++;rival.ResearchReserve+=33;session.State.Colonies.Single(c=>c.EmpireId==rival.Id).ProductionReserve+=999;
        Assert.Equal(JsonSerializer.Serialize(before),JsonSerializer.Serialize(Preview(session,action)));Assert.Equal(view,JsonSerializer.Serialize(session.View(e.Id)));
    }
    [Fact]
    public void AlternativeDeclaredWarshipCanStartRefit()
    {
        var original=Setup();var c=original.Content;var role=c.Vessels["escort"] with{Id="guardian"};c=c with{Vessels=c.Vessels.Remove("escort").Add(role.Id,role),ShipDesigns=c.ShipDesigns! with{WarshipRoleId=role.Id},Combat=c.Combat! with{RoleProfiles=c.Combat.RoleProfiles.Remove("escort").Add(role.Id,c.Combat.RoleProfiles["escort"])}};original.State.Vessels[0].RoleId=role.Id;
        var session=new GameSession(c,original.State);Apply(session,new RefitAction.Start(session.State.Vessels[0].Id,session.State.Colonies[0].Id,new(2)));Assert.Single(session.State.Empires[0].ShipDesigns!.Refits);Assert.Empty(GameStateValidator.Validate(c,session.State));
    }
    [Theory]
    [InlineData("job")][InlineData("event")]
    public void RefitCounterOverflowRejectsWithoutAllocating(string counter)
    {
        var session=Setup();var e=session.State.Empires[0];if(counter=="job")e.ShipDesigns!.NextRefitId=int.MaxValue;else e.ShipDesigns!.NextEventId=long.MaxValue;e.Ready=true;
        var action=new RefitAction.Start(session.State.Vessels[0].Id,session.State.Colonies[0].Id,new(2));var p=Preview(session,action);Assert.True(p.Allowed,p.RejectionCode);RejectSame(session,new RefitCommand(e.Id,0,action,p.Token));
    }
    [Fact]
    public void DifferentRefitRequiresExplicitCancel()
    {
        var session=Setup();var gun=ShipDesignFixtures.SaveDesign(session,"Gun",new("escort_frame","conventional_drive",null,ImmutableArray.Create("pulse_bank","pulse_bank")));var ship=session.State.Vessels[0];var port=session.State.Colonies[0];Apply(session,new RefitAction.Start(ship.Id,port.Id,new(2)));
        var action=new RefitAction.Start(ship.Id,port.Id,gun);Assert.False(Preview(session,action).Allowed);RejectSame(session,new RefitCommand(session.State.Empires[0].Id,0,action,string.Empty));Assert.Equal(new DesignId(2),session.State.Empires[0].ShipDesigns!.Refits.Single().TargetDesign);
    }
    [Theory]
    [InlineData(CombatPhase.Pending)][InlineData(CombatPhase.Active)][InlineData(CombatPhase.WithdrawalQueued)]
    public void CombatMembershipInPermittedFactsBlocksRefit(CombatPhase phase)
    {
        // Labelled immutable-query supplement isolating the activity gate from spatial eligibility.
        var session=Setup();var ship=session.State.Vessels[0];var port=session.State.Colonies[0];var view=session.View(ship.EmpireId);var orders=new CombatOrders(CombatDoctrine.Cautious,port.Id,StragglerPolicy.KeepTogether,CombatArrivalOrder.Hold);
        view=view with{Combat=view.Combat! with{Encounters=ImmutableArray.Create(new CombatEncounterView(new(1),new(1),new(1),1,ImmutableArray.Create(ship.Id),orders,phase,0,1,false))}};
        var p=RefitQueries.Preview(session.Content,view,new RefitAction.Start(ship.Id,port.Id,new(2)));Assert.False(p.Allowed);Assert.Equal("refit_activity",p.RejectionCode);
    }
    [Fact]
    public void WenthCommandBodyAndFleetHoldRemainExplicit()
    {
        var session=ShipDesignFixtures.Session(ImmutableArray.Create("industrial_coordination","vector_drives"),species:"wenth");var v=session.State.Vessels[0];v.RoleId="escort";v.Design=new(1);CombatInitializer.InitializeVessel(session.Content,session.State,v);ShipDesignFixtures.SaveDesign(session,"Fast",session.Content.ShipDesigns!.StartingRecipe with{EngineId="vector_drive"});
        Fleet(session,new FleetAction.Create("Body command",ImmutableArray.Create(v.Id),v.Id));var f=session.State.Empires[0].Fleets!.Fleets.Single();var officer=session.State.Empires[0].Fleets!.Officers[0];var result=FleetOfficerTests.Run(new LoadedGame(session.Content,session.State),new OfficerAction.AssignFlagship(officer.Id,f.Id));Assert.True(result.Accepted,result.Rejection?.Message);session=new(session.Content,result.State);
        var action=new RefitAction.Start(v.Id,session.State.Colonies[0].Id,new(2));var view=session.View(v.EmpireId);var group=view.Fleets!.Fleets.Single();var notHeld=view with{Fleets=view.Fleets with{Fleets=ImmutableArray.Create(group with{Order=new(FleetOrderKind.Explore)})}};Assert.False(RefitQueries.Preview(session.Content,notHeld,action).Allowed);
        var body=session.State.Empires[0].Fleets!.Officers[0].SourceBody;Apply(session,action);var after=session.State.Empires[0].Fleets!.Officers[0];Assert.Equal(body,after.SourceBody);Assert.Equal(OfficerStatus.Commanding,after.Status);Assert.Equal(v.Id,after.Flagship);Assert.Empty(GameStateValidator.Validate(session.Content,session.State));
    }
    internal static GameSession Setup()
    {
        var session=ShipDesignMathTests.Equipped("standard");ShipDesignFixtures.SaveDesign(session,"Fast",session.Content.ShipDesigns!.StartingRecipe with{EngineId="vector_drive"});return session;
    }
    internal static RefitPreview Preview(GameSession s,RefitAction a)=>RefitQueries.Preview(s.Content,s.View(s.State.Empires[0].Id),a);
    internal static void Apply(GameSession s,RefitAction a){var p=Preview(s,a);Assert.True(p.Allowed,p.RejectionCode);Do(s,new RefitCommand(s.State.Empires[0].Id,s.State.Turn,a,p.Token));}
    private static string Bytes(GameSession s)=>JsonSerializer.Serialize(s.State);
    private static void Do(GameSession s,Command c){var r=s.Execute(c);Assert.True(r.Accepted,r.Rejection?.Message);}
    private static void RejectSame(GameSession s,Command c){string before=Bytes(s);Assert.False(s.Execute(c).Accepted);Assert.Equal(before,Bytes(s));}
    private static void Fleet(GameSession s,FleetAction a){var p=FleetQueries.PreviewAction(s.Content,s.View(s.State.Empires[0].Id),a);Assert.True(p.Allowed,p.Rejection);Do(s,new FleetCommand(s.State.Empires[0].Id,s.State.Turn,a){Fingerprint=p.Fingerprint});}
}
