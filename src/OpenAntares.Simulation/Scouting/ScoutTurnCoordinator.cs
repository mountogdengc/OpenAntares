using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Scouting;
public static class ScoutTurnCoordinator
{
    public static ScoutPreparationResult Prepare(ContentSet c,GameState s,EmpireId owner)=>Prepare(c,s,owner,ScoutPlanner.Plan);
    internal static ScoutPreparationResult Prepare(ContentSet c,GameState s,EmpireId owner,Func<IScoutPlanningContext,bool,ScoutPlan> planner)
    {
        var empty=new ScoutPlan(ImmutableArray<Command>.Empty,ImmutableArray<ScoutOrderEvent>.Empty);
        ScoutPreparationResult Failed()=>new(s,false,empty);
        if(!Rules.HasScouting(s.RulesVersion))return new(s,true,empty);
        if(s.FindEmpire(owner) is not {} empire||!ScoutReadyQueries.HasConsent(c,s,owner)||GameStateValidator.Validate(c,s,true).Count!=0)return Failed();
        try
        {
            var context=ScoutPlanningQueries.Create(c,s,owner);var plan=planner(context,empire.Controller==ControllerKind.Ai);
            if(plan.Commands.IsDefault||plan.Outcomes.IsDefault||!plan.Commands.SequenceEqual(context.Preparation.Commands)||!plan.Outcomes.SequenceEqual(context.Preparation.Outcomes))return Failed();
            long budget=s.Vessels.Count(v=>v.EmpireId==owner&&v.Scout is not null);
            if(plan.Commands.Length>budget||plan.Commands.Select(cmd=>cmd switch{SetMoveOrderCommand move=>move.Vessel,SetScoutRetreatOrderCommand retreat=>retreat.Vessel,_=>default}).Distinct().Count()!=plan.Commands.Length)return Failed();
            var working=s;
            foreach(var command in plan.Commands)
            {
                if(command.Issuer!=owner||command.ExpectedTurn!=s.Turn||command is not(SetMoveOrderCommand or SetScoutRetreatOrderCommand))return Failed();
                var result=Rules.HasFleets(working.RulesVersion)&&command is SetMoveOrderCommand move?CommandProcessor.ExecuteScoutPreparationMove(c,working,move,plan):CommandProcessor.Execute(c,working,command);if(!result.Accepted)return Failed();working=result.State;
            }
            if(ScoutReports.NeedsPreparationRecord(working,owner,plan))
            {
                if(ReferenceEquals(working,s))working=s.Clone();
                ScoutReports.RecordPreparation(working,owner,plan);
            }
            if(ScoutReadyQueries.ValidateResponse(c,working,owner).Length!=0||GameStateValidator.Validate(c,working,true).Count!=0)return Failed();
            return new(working,true,plan);
        }
        catch(Exception error)when(error is OverflowException or ArgumentException or InvalidOperationException or KeyNotFoundException){return Failed();}
    }
}
