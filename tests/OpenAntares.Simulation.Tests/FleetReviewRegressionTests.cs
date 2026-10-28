using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetReviewRegressionTests
{
    // Explicit pre-session interaction supplements, not generated discovery/economy/luck evidence.
    [Theory][InlineData(false,false)][InlineData(true,false)][InlineData(false,true)][InlineData(true,true)]
    public void NeutralSafetyPreparesNamedAndReservedWithdrawalOrCancellation(bool reserved,bool cancellation)
    {
        var g=FleetReadyTests.Multi();var s=g.State;var owner=s.Empires[0];var ship=s.Vessels.First(v=>v.EmpireId==owner.Id);var home=ship.StationaryStarId!.Value;var fallback=new StarId(14);
        if(reserved)g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,1) with{AcceptedIdleVessels=ImmutableArray.Create(ship.Id)});
        else {var made=FleetCommandTests.Run(g,new FleetAction.Create("Avoid command",ImmutableArray.Create(ship.Id)));Assert.True(made.Accepted);g=new(g.Content,made.State);}
        s=g.State;owner=s.Empires[0];ship=s.FindVessel(ship.Id)!;ship.Scout!.Posture=ScoutPosture.AvoidEncounters;
        if(cancellation)s.Colonies.Add(new(){Id=new(s.AllocateId()),EmpireId=s.Empires[1].Id,PlanetId=new(15),Population=1,Workforce=new(1,0,0),HabitatMode="native",Crisis=new(),Development=new()});else s.Vessels.Add(new(){Id=new(s.AllocateId()),EmpireId=s.Empires[1].Id,RoleId="scout",StationaryStarId=home,Crisis=new(),Scout=new()});owner.Surveys.Add(ObservationUpdater.Observe(s,fallback,0));owner.Surveys.Sort((a,b)=>a.StarId.CompareTo(b.StarId));if(cancellation)ship.PendingMove=fallback;ObservationUpdater.Refresh(s,0);ContactUpdater.Refresh(s.Clone(),s);FleetFixtures.Valid(g);
        var proposed=ScoutQueries.PreviewPreparation(g.Content,s,owner.Id);Assert.Contains(proposed.Commands,cmd=>cmd is SetMoveOrderCommand move&&move.Vessel==ship.Id&&move.Destination==(cancellation?null:fallback));
        string original=FleetFixtures.Bytes(s);var manual=CommandProcessor.Execute(g.Content,s,new SetMoveOrderCommand(owner.Id,0,ship.Id,cancellation?null:fallback));Assert.False(manual.Accepted);Assert.Equal("fleet_detachment_required",manual.Rejection!.Code);
        var first=CommandProcessor.Execute(g.Content,s,new SetReadyCommand(owner.Id,0,true));Assert.True(first.Accepted,first.Rejection?.Message);Assert.Equal(cancellation?null:fallback,first.State.FindVessel(ship.Id)!.PendingMove);Assert.Equal(original,FleetFixtures.Bytes(s));
        if(reserved){var request=first.State.Empires[0].Fleets!.Requests.Single();Assert.Equal(ship.Id,request.Slots.Single().Vessel);Assert.NotEqual(SlotStatus.Missing,request.Slots.Single().Status);}
        var last=CommandProcessor.Execute(g.Content,first.State,new SetReadyCommand(s.Empires[1].Id,0,true));Assert.True(last.Accepted,last.Rejection?.Message);var direct=TurnResolver.Resolve(g.Content,s);Assert.True(direct.Succeeded,string.Join(";",direct.Errors));Assert.Equal(FleetFixtures.Bytes(last.State),FleetFixtures.Bytes(direct.State));
    }
    [Theory][InlineData(RequestCancellation.KeepQueue,0)][InlineData(RequestCancellation.RemoveUnstarted,0)][InlineData(RequestCancellation.KeepQueue,1)][InlineData(RequestCancellation.RemoveUnstarted,1)][InlineData(RequestCancellation.KeepQueue,2)][InlineData(RequestCancellation.RemoveUnstarted,2)]
    public void CancelSafetyPausedRetainsPaidWorkLivingHullsAndTransit(RequestCancellation choice,int stage)
    {
        var g=FleetFixtures.Game();var target=FleetOfficerTests.AddColony(g);var rally=g.State.FindPlanet(target.PlanetId)!.StarId;var ship=g.State.Vessels[0];var draft=FleetPlanningTests.Draft(g,3) with{Rally=rally,Coordinator=new(1),AcceptedIdleVessels=stage==0?ImmutableArray<VesselId>.Empty:ImmutableArray.Create(ship.Id)};g=FleetRequestTests.Accept(g,draft);var r=g.State.Empires[0].Fleets!.Requests[0];
        if(stage==2){ship=g.State.FindVessel(ship.Id)!;var origin=g.State.FindStar(ship.StationaryStarId!.Value)!;var end=g.State.FindStar(rally)!;int duration=TravelCalculator.Duration(origin.X,origin.Y,end.X,end.Y,g.Content.Vessels[ship.RoleId].Speed);ship.StationaryStarId=null;ship.Transit=new(origin.Id,rally,duration,duration);r.Slots.Single(s=>s.Vessel==ship.Id).Status=SlotStatus.Travelling;}
        var queued=r.Slots.First(s=>s.QueueToken is not null);var source=g.State.FindColony(queued.Colony!.Value)!;source.ProjectId="scout";source.ProjectKind=ProductionKinds.Vessel;source.Development!.ActiveQueueToken=queued.QueueToken;source.Development.Origin=ProductionOrigin.Queue;source.ProductionReserve=700;int extra=source.Development.NextQueueToken++;source.Development.Queue.Add(new(extra,new(ProductionKinds.Building,"fabrication_hall")));
        RequestQueueAuthority.SafetyPause(g.State,g.State.Empires[0].Id,r.Id,"Observed unsafe rally.");FleetFixtures.Valid(g);string before=FleetFixtures.Bytes(g.State);string hulls=System.Text.Json.JsonSerializer.Serialize(g.State.Vessels);var retained=source.Development.Queue.ToArray();var cancelled=FleetRequestTests.Run(g,new AssemblyAction.Cancel(r.Id,r.Revision,choice));Assert.True(cancelled.Accepted,cancelled.Rejection?.Message);Assert.Equal(before,FleetFixtures.Bytes(g.State));
        var final=cancelled.State.Empires[0].Fleets!.Requests[0];Assert.Equal(RequestStatus.Cancelled,final.Status);Assert.All(final.Slots,s=>{Assert.NotEqual(SlotStatus.BlockedSafety,s.Status);Assert.Null(s.ResumeStatus);});Assert.Equal(hulls,System.Text.Json.JsonSerializer.Serialize(cancelled.State.Vessels));var paid=cancelled.State.FindColony(source.Id)!;Assert.Equal(700,paid.ProductionReserve);Assert.Equal(queued.QueueToken,paid.Development!.ActiveQueueToken);Assert.Equal("scout",paid.ProjectId);Assert.Contains(paid.Development.Queue,q=>q.Token==extra);if(choice==RequestCancellation.KeepQueue)Assert.Equal(retained,paid.Development.Queue);
        Assert.Equal(OfficerStatus.Posted,cancelled.State.Empires[0].Fleets!.Officers[0].Status);var loaded=SaveGame.Deserialize(SaveGame.Serialize(g.Content,cancelled.State));Assert.True(loaded.Succeeded,string.Join(";",loaded.Errors));Assert.Equal(FleetFixtures.Bytes(cancelled.State),FleetFixtures.Bytes(loaded.Game!.State));
    }
    [Theory][InlineData(false,false)][InlineData(true,false)][InlineData(false,true)][InlineData(true,true)]
    public void PartialArrivalRallyRequiresExplicitFleetReleaseThenRetainsQuota(bool safety,bool amend)
    {
        var g=FleetFixtures.Game();var target=FleetOfficerTests.AddColony(g);var destination=g.State.FindPlanet(target.PlanetId)!.StarId;var ship=g.State.Vessels[0];var made=FleetCommandTests.Run(g,new FleetAction.Create("Original rally",ImmutableArray.Create(ship.Id)));Assert.True(made.Accepted);g=new(g.Content,made.State);g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,2) with{RallyFleet=new(1),AcceptedIdleVessels=ImmutableArray.Create(ship.Id)});var r=g.State.Empires[0].Fleets!.Requests[0];r.Slots.Single(s=>s.Vessel==ship.Id).Status=SlotStatus.Arrived;if(safety)RequestQueueAuthority.SafetyPause(g.State,g.State.Empires[0].Id,r.Id,"Observed unsafe rally.");FleetFixtures.Valid(g);
        var view=EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id);var changed=AssemblyPlanner.DraftFor(view.Fleets!.Requests[0]) with{Rally=destination,RallyFleet=null};var preview=amend?AssemblyPlanner.Preview(FleetPlanningQueries.Create(g.Content,view),changed,r.Id):FleetRequestTests.Retained(g,r,destination);Assert.False(preview.Allowed);Assert.Contains(preview.Rejections,message=>message.Contains("release",StringComparison.OrdinalIgnoreCase));
        var disband=FleetCommandTests.Run(g,new FleetAction.Disband(new(1)));Assert.True(disband.Accepted,disband.Rejection?.Message);g=new(g.Content,disband.State);r=g.State.Empires[0].Fleets!.Requests[0];var slot=r.Slots.Single(s=>s.Vessel==ship.Id);Assert.NotEqual(SlotStatus.Missing,slot.Status);Assert.Null(g.State.FindVessel(ship.Id)!.Fleet);view=EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id);changed=AssemblyPlanner.DraftFor(view.Fleets!.Requests[0]) with{Rally=destination,RallyFleet=null};preview=amend?AssemblyPlanner.Preview(FleetPlanningQueries.Create(g.Content,view),changed,r.Id):FleetRequestTests.Retained(g,r,destination);Assert.True(preview.Allowed,string.Join(";",preview.Rejections));var outcome=FleetRequestTests.Run(g,amend?new AssemblyAction.Amend(r.Id,r.Revision,changed,preview.Fingerprint):new AssemblyAction.ChangeRally(r.Id,r.Revision,destination,null,preview.Fingerprint));Assert.True(outcome.Accepted,outcome.Rejection?.Message);g=new(g.Content,outcome.State);r=g.State.Empires[0].Fleets!.Requests[0];var resumed=FleetRequestTests.Retained(g,r);outcome=FleetRequestTests.Run(g,new AssemblyAction.Resume(r.Id,r.Revision,resumed.Fingerprint));Assert.True(outcome.Accepted,outcome.Rejection?.Message);g=new(g.Content,outcome.State);
        var loaded=SaveGame.Deserialize(SaveGame.Serialize(g.Content,g.State));Assert.True(loaded.Succeeded);var session=new GameSession(loaded.Game!.Content,loaded.Game.State);for(int i=0;i<20&&session.State.Empires[0].Fleets!.Requests[0].Status!=RequestStatus.Complete;i++)Assert.True(session.Execute(new SetReadyCommand(session.State.Empires[0].Id,session.State.Turn,true)).Accepted);Assert.Equal(RequestStatus.Complete,session.State.Empires[0].Fleets!.Requests[0].Status);Assert.Contains(session.State.Empires[0].Fleets!.Requests[0].Slots,s=>s.Vessel==ship.Id&&s.Status==SlotStatus.Arrived);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void NullQueuedBindingRejectsValidationAndLoadWithoutReplacingSession(bool blocked)
    {
        var g=FleetRequestTests.Accept(FleetFixtures.Game(),FleetPlanningTests.Draft(FleetFixtures.Game(),1));if(blocked)RequestQueueAuthority.SafetyPause(g.State,g.State.Empires[0].Id,g.State.Empires[0].Fleets!.Requests[0].Id,"Observed unsafe rally.");string valid=SaveGame.Serialize(g.Content,g.State);var root=JsonNode.Parse(valid)!;root["state"]!["empires"]![0]!["fleet_command"]!["requests"]![0]!["slots"]![0]!["queue_token"]=null;
        var invalid=g.State.Clone();invalid.Empires[0].Fleets!.Requests[0].Slots[0].QueueToken=null;Assert.NotEmpty(GameStateValidator.Validate(g.Content,invalid));var session=new GameSession(g.Content,g.State);var original=session.State;string path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"openantares-nullqueue-"+Guid.NewGuid()+".json");try{System.IO.File.WriteAllText(path,root.ToJsonString());var load=session.Load(path);Assert.False(load.Succeeded);Assert.Same(original,session.State);Assert.Equal(valid,SaveGame.Serialize(session.Content,session.State));}finally{if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
    }
}
