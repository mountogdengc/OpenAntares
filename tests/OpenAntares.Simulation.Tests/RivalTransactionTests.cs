using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class RivalTransactionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SamePlanetRaceKeepsIdPriorityAndLoserCargo(bool differentPlanets)
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("harl");var before=state.Clone();var human=state.Empires[0];var ai=state.Empires[1];
        var target=RivalPolicyTests.KnownWorld(state);var other=differentPlanets?RivalPolicyTests.KnownWorld(state):target;
        var playerShip=new VesselState{Id=new(state.AllocateId()),EmpireId=human.Id,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=target.StarId,PendingFounding=target.Id};state.Vessels.Add(playerShip);
        var rivalShip=new VesselState{Id=new(state.AllocateId()),EmpireId=ai.Id,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=target.StarId,PendingFounding=differentPlanets?other.Id:null};state.Vessels.Add(rivalShip);
        ObservationUpdater.Refresh(state,0);ContactUpdater.Refresh(before,state);Assert.Empty(GameStateValidator.Validate(content,state));
        var result=TurnResolver.Resolve(content,state);Assert.True(result.Succeeded,string.Join("; ",result.Errors));
        Assert.Equal(human.Id,result.State.Colonies.Single(c=>c.PlanetId==target.Id).EmpireId);Assert.Null(result.State.FindVessel(playerShip.Id));
        if(differentPlanets){Assert.Equal(ai.Id,result.State.Colonies.Single(c=>c.PlanetId==other.Id).EmpireId);Assert.Null(result.State.FindVessel(rivalShip.Id));}
        else{var loser=result.State.FindVessel(rivalShip.Id)!;Assert.Equal(1,loser.CarriedPopulation);Assert.Null(loser.PendingFounding);Assert.Equal(target.StarId,loser.StationaryStarId);}
    }

    [Fact]
    public void StaleOccupiedTargetRetargetsAfterObservation()
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("harl");var ai=state.Empires[1];var occupied=state.Planets[0];
        ai.Surveys.Add(new SystemObservation{StarId=occupied.StarId,LastObservedTurn=0,Planets={new(occupied.Id,1,occupied.PlanetTypeId,null,null){EnvironmentId=occupied.EnvironmentId}}});
        var alternative=new PlanetState{Id=new(state.AllocateId()),StarId=occupied.StarId,Orbit=2,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea"};state.Planets.Add(alternative);
        var founder=new VesselState{Id=new(state.AllocateId()),EmpireId=ai.Id,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=state.Stars[1].Id,PendingMove=occupied.StarId};state.Vessels.Add(founder);
        ObservationUpdater.Refresh(state,0);Assert.Empty(GameStateValidator.Validate(content,state));
        var travel=TurnResolver.Resolve(content,state);Assert.True(travel.Succeeded,string.Join("; ",travel.Errors));Assert.NotNull(travel.State.FindVessel(founder.Id)!.Transit);
        var arrival=TurnResolver.Resolve(content,travel.State);Assert.True(arrival.Succeeded,string.Join("; ",arrival.Errors));
        Assert.Equal(1,arrival.State.FindVessel(founder.Id)!.CarriedPopulation);Assert.Null(arrival.State.FindVessel(founder.Id)!.PendingFounding);Assert.Single(arrival.State.Colonies,c=>c.EmpireId==ai.Id);
        var retarget=TurnResolver.Resolve(content,arrival.State);Assert.True(retarget.Succeeded,string.Join("; ",retarget.Errors));
        Assert.Equal(ai.Id,retarget.State.Colonies.Single(c=>c.PlanetId==alternative.Id).EmpireId);Assert.Null(retarget.State.FindVessel(founder.Id));
    }
    [Theory]
    [InlineData("rejection")]
    [InlineData("budget")]
    [InlineData("ready")]
    [InlineData("issuer")]
    [InlineData("count")]
    [InlineData("exception")]
    public void PrivatePlanningFaultsDiscardEveryAcceptedChange(string fault)
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("harl");state.Empires[0].Ready=true;
        string before=JsonSerializer.Serialize(state);
        RivalPlanningResult Fault(Func<IRivalPlanningContext> current,Func<Command,bool> apply)
        {
            var view=current().View;
            if(fault=="exception")throw new OverflowException("private empire details");
            if(fault=="budget")
            {
                int accepted=0;
                for(int i=0;i<5;i++)if(apply(new SetResearchTargetCommand(view.Viewer,view.Turn,i%2==0?"cultivation_methods":"measurement_methods")))accepted++;
                return new(true,accepted);
            }
            if(fault=="ready")apply(new SetReadyCommand(view.Viewer,view.Turn,true));
            else if(fault=="issuer")apply(new SetResearchTargetCommand(state.Empires[0].Id,view.Turn,"cultivation_methods"));
            else if(fault=="count"){apply(new SetResearchTargetCommand(view.Viewer,view.Turn,"cultivation_methods"));return new(true,0);}
            else apply(new SetWorkforceCommand(view.Viewer,view.Turn,view.Colonies[0].Id,new(-1,6,1)));
            return new(true,1);
        }
        var planned=RivalTurnPlanner.Prepare(content,state,Fault);Assert.False(planned.Succeeded);Assert.Same(state,planned.State);
        var resolved=TurnResolver.Resolve(content,state,(c,s)=>RivalTurnPlanner.Prepare(c,s,Fault));
        Assert.False(resolved.Succeeded);Assert.Same(state,resolved.State);Assert.Null(resolved.Report);
        Assert.Equal(before,JsonSerializer.Serialize(state));Assert.Single(resolved.Errors);Assert.DoesNotContain("private",resolved.Errors[0]);
    }

    [Fact]
    public void InvalidCandidateIsNotPublishedAndOldAiNeverPlans()
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("peldra");
        var failed=TurnResolver.Resolve(content,state,(c,s)=>{var bad=s.Clone();bad.NextEntityId=0;return new(bad,true);});
        Assert.False(failed.Succeeded);Assert.Same(state,failed.State);Assert.Null(failed.Report);
        var old=CompatibilityFixtures.LoadPassiveAiRules3();
        var legacy=TurnResolver.Resolve(old.Content,old.State,(c,s)=>throw new InvalidOperationException("Old passive AI was activated"));
        Assert.True(legacy.Succeeded,string.Join("; ",legacy.Errors));
        Assert.Equal(CompatibilityFixtures.Rules3Trace(true)[0],CompatibilityFixtures.CanonicalWithoutSchema(old.Content,legacy.State));
    }

    [Fact]
    public void MultipleAiPlanInIdOrderWithoutSeeingEarlierPrivatePlans()
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("peldra");
        var star=new StarState{Id=new(state.AllocateId()),Name="Third home",X=900,Y=0};state.Stars.Add(star);
        var world=new PlanetState{Id=new(state.AllocateId()),StarId=star.Id,Orbit=1,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea"};state.Planets.Add(world);
        var third=new EmpireState{Id=new(state.AllocateId()),SpeciesId="harl",Controller=ControllerKind.Ai,AiPolicyId="expansion_v1"};state.Empires.Add(third);
        var profile=content.Species["harl"];state.Colonies.Add(new ColonyState{Id=new(state.AllocateId()),PlanetId=world.Id,EmpireId=third.Id,Population=profile.StartingPopulation,Workforce=profile.StartingWorkforce,HabitatMode="native"});
        state.Vessels.Add(new VesselState{Id=new(state.AllocateId()),EmpireId=third.Id,RoleId="scout",StationaryStarId=star.Id});ObservationUpdater.Refresh(state,0);
        string before=JsonSerializer.Serialize(state),thirdFacts=JsonSerializer.Serialize(RivalPlanningQueries.Create(content,state,third.Id).View);
        var order=new List<EmpireId>();
        var planned=RivalTurnPlanner.Prepare(content,state,(current,apply)=>{
            var context=current();order.Add(context.View.Viewer);
            if(context.View.Viewer==third.Id)Assert.Equal(thirdFacts,JsonSerializer.Serialize(context.View));
            return ExpansionRivalPlanner.Plan(current,apply);
        });
        Assert.True(planned.Succeeded);Assert.Equal(new[]{state.Empires[1].Id,third.Id},order);Assert.Equal(before,JsonSerializer.Serialize(state));
        var lateFailure=RivalTurnPlanner.Prepare(content,state,(current,apply)=>current().View.Viewer==third.Id?new(false,0):ExpansionRivalPlanner.Plan(current,apply));
        Assert.False(lateFailure.Succeeded);Assert.Same(state,lateFailure.State);Assert.Equal(before,JsonSerializer.Serialize(state));
        int calls=0;var resolved=TurnResolver.Resolve(content,state,(c,s)=>{calls++;return RivalTurnPlanner.Prepare(c,s);});
        Assert.True(resolved.Succeeded,string.Join("; ",resolved.Errors));Assert.Equal(1,calls);
    }

    [Fact]
    public void CompletedVesselWaitsUntilNextPlanning()
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("harl");var ai=state.Empires[1];var world=RivalPolicyTests.KnownWorld(state);
        state.Colonies.Single(c=>c.EmpireId==ai.Id).ProductionReserve=1600;
        var first=TurnResolver.Resolve(content,state);Assert.True(first.Succeeded,string.Join("; ",first.Errors));
        var ship=Assert.Single(first.State.Vessels,v=>v.EmpireId==ai.Id&&v.RoleId=="harl_settler");
        Assert.Null(ship.PendingMove);Assert.Null(ship.PendingFounding);Assert.Null(ship.Transit);Assert.DoesNotContain(first.State.Colonies,c=>c.PlanetId==world.Id);
        var second=TurnResolver.Resolve(content,first.State);Assert.True(second.Succeeded,string.Join("; ",second.Errors));
        var child=Assert.Single(second.State.Colonies,c=>c.PlanetId==world.Id);Assert.Equal(1,child.Population);Assert.Null(second.State.FindVessel(ship.Id));
        Assert.DoesNotContain(second.Report!.Economy.Colonies,c=>c.Colony==child.Id);
    }

    [Fact]
    public void GrowthAndExtensionCannotUnlockSameSnapshotEmbark()
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("wenth");var ai=state.Empires[1];RivalPolicyTests.KnownWorld(state);
        var parent=state.Colonies.Single(c=>c.EmpireId==ai.Id);parent.Population=5;parent.Workforce=new(3,1,1);parent.GrowthProgress=50;parent.ProductionReserve=2400;
        var first=TurnResolver.Resolve(content,state);Assert.True(first.Succeeded,string.Join("; ",first.Errors));
        Assert.Equal(6,first.State.FindColony(parent.Id)!.Population);Assert.DoesNotContain(first.State.Vessels,v=>v.RoleId=="daughter_seed");
        var second=TurnResolver.Resolve(content,first.State);Assert.True(second.Succeeded,string.Join("; ",second.Errors));Assert.Contains(second.State.Vessels,v=>v.RoleId=="daughter_seed");
        var extension=RivalFixtures.AtHome("wenth");var human=extension.Empires[0];human.SpeciesId="wenth";human.KnownTechnologyIds.Add("habitat_methods");
        var colony=extension.Colonies[0];colony.Population=6;colony.Workforce=new(4,1,1);colony.ProjectKind=ProductionKinds.Building;colony.ProjectId="habitat_extension";colony.ProductionReserve=4000;
        var built=TurnResolver.Resolve(content,extension);Assert.True(built.Succeeded,string.Join("; ",built.Errors));
        Assert.Equal(8,Rules.ColonyCapacity(content,built.State,built.State.FindColony(colony.Id)!));
        var queued=CommandProcessor.Execute(content,built.State,new SetProductionTargetCommand(human.Id,1,colony.Id,new(ProductionKinds.Vessel,"daughter_seed")));Assert.True(queued.Accepted);
        var forecast=OpenAntares.Simulation.Economy.EconomyCalculator.Calculate(content,queued.State).Colony(colony.Id);
        Assert.Null(forecast.VesselCompletionPlan);Assert.Contains(forecast.Production.BlockedRequirements,r=>r.Code=="full_colony"&&r.Actual==6&&r.Required==8);
    }
    [Fact]
    public void LastHumanReadyAndDirectResolvePlanExactlyOnce()
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("peldra");var human=state.Empires[0];var ai=state.Empires[1];
        var second=new EmpireState{Id=new(state.AllocateId()),Controller=ControllerKind.Human,SpeciesId="harl"};state.Empires.Add(second);
        var session=new GameSession(content,state);var nonFinal=session.Execute(new SetReadyCommand(human.Id,0,true));
        Assert.True(nonFinal.Accepted);Assert.Null(nonFinal.Report);Assert.Equal(0,session.State.Turn);Assert.Null(session.State.FindEmpire(ai.Id)!.ResearchTargetId);
        var original=session.State;var unchanged=session.Execute(new SetReadyCommand(human.Id,0,true));Assert.Same(original,unchanged.State);Assert.False(unchanged.Changed);
        var stale=session.Execute(new SetResearchTargetCommand(second.Id,-1,"cultivation_methods"));Assert.False(stale.Accepted);Assert.Same(original,session.State);
        session.Forecast();Assert.Same(original,session.State);Assert.Null(session.State.FindEmpire(ai.Id)!.ResearchTargetId);
        string path=Path.Combine(Path.GetTempPath(),"OpenAntares-rival-readiness-"+Guid.NewGuid()+".json");
        try{session.Save(path);Assert.True(session.Load(path).Succeeded);Assert.Null(session.State.FindEmpire(ai.Id)!.ResearchTargetId);Assert.Equal(0,session.State.Turn);}
        finally{File.Delete(path);}
        var resolved=session.Execute(new SetReadyCommand(second.Id,0,true));Assert.True(resolved.Accepted,resolved.Rejection?.Message);Assert.NotNull(resolved.Report);
        var colony=session.State.Colonies.Single(c=>c.EmpireId==ai.Id);
        Assert.Equal(1,session.State.Turn);Assert.Equal(20,colony.GrowthProgress);Assert.Equal(600,colony.ProductionReserve);
        Assert.Equal("cultivation_methods",session.State.FindEmpire(ai.Id)!.ResearchTargetId);Assert.All(session.State.Empires.Where(e=>e.Controller==ControllerKind.Human),e=>Assert.False(e.Ready));
        var direct=TurnResolver.Resolve(content,state);Assert.True(direct.Succeeded,string.Join("; ",direct.Errors));
        Assert.Equal(SaveGame.Serialize(session.Content,session.State),SaveGame.Serialize(content,direct.State));
    }

    [Theory]
    [InlineData("output")]
    [InlineData("id")]
    public void FailureAfterPlanningPreservesOriginalReferenceAndBytes(string failure)
    {
        var content=RivalFixtures.Content();var state=RivalFixtures.AtHome("peldra");var human=state.Empires[0];var ai=state.Empires[1];
        if(failure=="output")human.ResearchReserve=long.MaxValue;
        else{state.NextEntityId=int.MaxValue;var colony=state.Colonies[0];colony.ProjectKind=ProductionKinds.Vessel;colony.ProjectId="harl_settler";colony.ProductionReserve=1600;}
        Assert.Empty(GameStateValidator.Validate(content,state));string before=SaveGame.Serialize(content,state);var session=new GameSession(content,state);
        var result=session.Execute(new SetReadyCommand(human.Id,state.Turn,true));
        Assert.False(result.Accepted);Assert.Same(state,result.State);Assert.Same(state,session.State);Assert.Same(content,session.Content);Assert.Null(result.Report);
        Assert.Equal(before,SaveGame.Serialize(content,session.State));Assert.False(session.State.FindEmpire(human.Id)!.Ready);Assert.Null(session.State.FindEmpire(ai.Id)!.ResearchTargetId);
        Assert.DoesNotContain(ai.Id.ToString(),result.Rejection!.Message);Assert.DoesNotContain("overflow",result.Rejection.Message,StringComparison.OrdinalIgnoreCase);
    }
}
