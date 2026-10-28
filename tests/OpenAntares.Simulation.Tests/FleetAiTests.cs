using System;
using System.Linq;
using System.Collections.Generic;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetAiTests
{
    [Theory]
    [InlineData(0,2)]
    [InlineData(10,1)]
    [InlineData(30,1)]
    public void RivalUsesTheSameSnapshotNavigatorTravel(int xp,int duration)
    {
        // Labelled distance/rank supplement; generation and earned-XP proofs are separate.
        var g=FleetFixtures.Game(rivals:1);var s=g.State;var ai=s.Empires.Single(e=>e.Controller==ControllerKind.Ai);var group=Assert.Single(ai.Fleets!.Fleets);var ship=s.FindVessel(group.Flagship)!;var home=ship.StationaryStarId!.Value;var target=s.Stars.First(star=>!s.Colonies.Any(c=>s.FindPlanet(c.PlanetId)!.StarId==star.Id)).Id;
        int index=0;foreach(var star in s.Stars){if(star.Id==home){star.X=0;star.Y=0;}else if(star.Id==target){star.X=301;star.Y=0;}else{star.X=700+index;star.Y=100+index;index++;}}var officer=ai.Fleets.Officers[0];officer.Xp=xp;officer.Rank=xp>=30?2:xp>=10?1:0;FleetFixtures.Valid(g);
        var result=TurnResolver.Resolve(g.Content,s);Assert.True(result.Succeeded,string.Join(";",result.Errors));var actual=result.State.FindVessel(ship.Id)!;if(duration==1)Assert.Equal(target,actual.StationaryStarId);else{Assert.Equal(duration,actual.Transit!.TotalTurns);Assert.Equal(duration-1,actual.Transit.RemainingTurns);}Assert.Equal(0,result.State.FindEmpire(ai.Id)!.Fleets!.RecoveryRollCount);
    }
    [Fact]
    public void LegacyRivalFuturesRemainExact()
    {
        // Reuse the genuine pre-M8 archive and its command transcripts, never generate expectations
        // with the new planner. This includes the archived rival-enabled generated/UI sources.
        new FleetCompatibilityTests().GenuineRules7SourcesAndFuturesAreFrozen();
    }
    [Fact]
    public void SingleExistingPlannerCommandsStartingNavigatorFleet()
    {
        var g=FleetFixtures.Game(rivals:1);var ai=g.State.Empires.Single(e=>e.Controller==ControllerKind.Ai);var group=Assert.Single(ai.Fleets!.Fleets);Assert.Equal(OfficerStatus.Commanding,ai.Fleets.Officers[0].Status);string before=FleetFixtures.Bytes(g.State);int calls=0;var commands=new List<Command>();
        var planned=RivalTurnPlanner.Prepare(g.Content,g.State,(current,emit)=>{calls++;return ExpansionRivalPlanner.Plan(current,cmd=>{commands.Add(cmd);return emit(cmd);});});Assert.True(planned.Succeeded);Assert.Equal(1,calls);Assert.Equal(before,FleetFixtures.Bytes(g.State));Assert.Single(commands.OfType<SetMoveOrderCommand>());Assert.NotNull(planned.State.FindVessel(group.Flagship)!.PendingMove);Assert.Equal(g.State.NextEntityId,planned.State.NextEntityId);Assert.Single(planned.State.Empires.Single(e=>e.Id==ai.Id).Fleets!.Fleets);Assert.Empty(planned.State.Empires.Single(e=>e.Id==ai.Id).Fleets!.Requests);
        var ready=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,0,true));Assert.True(ready.Accepted,ready.Rejection?.Message);Assert.Equal(1,ready.State.Turn);Assert.Equal(FleetFixtures.Bytes(TurnResolver.Resolve(g.Content,g.State).State),FleetFixtures.Bytes(ready.State));
        g=new(g.Content,ready.State);for(int i=0;i<12;i++){g=FleetSaveTests.Consent(g);g=FleetSaveTests.Advance(g);}ai=g.State.Empires.Single(e=>e.Controller==ControllerKind.Ai);Assert.Single(ai.Fleets!.Officers);Assert.Single(ai.Fleets.Fleets);Assert.Empty(ai.Fleets.Requests);Assert.True(ai.Fleets.Officers[0].Xp>0);Assert.All(g.State.Vessels.Where(v=>v.EmpireId==ai.Id&&v.Id!=group.Flagship),v=>Assert.Null(v.Fleet));Assert.All(g.State.Vessels.Where(v=>v.EmpireId==ai.Id),v=>Assert.Null(v.AmebaEncounter?.RiskOrder));
    }
    [Fact]
    public void PairedHiddenWorldsProduceSameOwnAiOrders()
    {
        var g=FleetFixtures.Game(rivals:1);var other=g.State.Clone();var ai=other.Empires.Single(e=>e.Controller==ControllerKind.Ai);var unknown=other.Planets.First(p=>!ai.Surveys.Any(s=>s.StarId==p.StarId));unknown.PlanetTypeId=unknown.PlanetTypeId=="gas_giant"?"verdant_world":"gas_giant";
        var a=new List<Command>();var b=new List<Command>();Assert.True(ExpansionRivalPlanner.Plan(()=>RivalPlanningQueries.Create(g.Content,g.State,ai.Id),cmd=>{a.Add(cmd);return true;}).Succeeded);Assert.True(ExpansionRivalPlanner.Plan(()=>RivalPlanningQueries.Create(g.Content,other,ai.Id),cmd=>{b.Add(cmd);return true;}).Succeeded);Assert.Equal(a,b);Assert.Equal(g.State.Random.State,other.Random.State);
    }
}
