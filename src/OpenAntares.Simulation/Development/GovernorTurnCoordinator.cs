using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Development;
public sealed record GovernorPreparationResult(GameState State,bool Succeeded);
public static class GovernorTurnCoordinator
{
    public static GovernorPreparationResult Prepare(ContentSet content,GameState state,EmpireId owner)=>Prepare(content,state,owner,GovernorPlanner.Plan);
    internal static GovernorPreparationResult Prepare(ContentSet content,GameState state,EmpireId owner,Func<IGovernorPlanningContext,GovernorPlan> plan)
    {
        if(!Rules.HasDevelopment(state.RulesVersion))return new(state,true);
        if(state.FindEmpire(owner)?.Controller!=ControllerKind.Human||GameStateValidator.Validate(content,state,allowAllHumansReady:true).Count!=0)return new(state,false);
        try {
            var view=EmpireViewProjector.Project(content,state,owner);var result=plan(GovernorPlanningQueries.Create(content,view));
            int count=view.Colonies.Length;
            if(!result.Succeeded||result.Orders.IsDefault||result.Orders.Length!=count||result.Orders.Select(o=>o.Colony).Distinct().Count()!=count)return new(state,false);
            var working=state.Clone();long changed=0,budget=2L*count;
            var oldReport=working.FindEmpire(owner)!.Development!.PendingReport;
            var entries=(oldReport?.Entries??ImmutableArray<GovernorReportEntry>.Empty).ToDictionary(e=>e.Colony);
            foreach(var order in result.Orders.OrderBy(o=>o.Colony)) {
                var before=working.FindColony(order.Colony);if(before is null||before.EmpireId!=owner)return new(state,false);
                var oldWorkforce=before.Workforce;ProductionTarget? oldTarget=before.ProjectId is {} id?new(before.ProjectKind,id):null;
                bool targetChanged=oldTarget!=order.Target,workforceChanged=oldWorkforce!=order.Workforce;
                if(targetChanged&&!Apply(new SetProductionTargetCommand(owner,state.Turn,before.Id,order.Target),order.QueueToken,order.Origin))return new(state,false);
                if(workforceChanged&&!Apply(new SetWorkforceCommand(owner,state.Turn,before.Id,order.Workforce),null,ProductionOrigin.None))return new(state,false);
                entries.TryGetValue(order.Colony,out var previous);
                if(previous is not null||targetChanged||workforceChanged||order.Issues.Length>0) {
                    var row=previous??new(order.Colony,null,null,null,null,ImmutableArray<GovernorIssue>.Empty,false,false);
                    if(workforceChanged)row=row with{BeforeWorkforce=row.BeforeWorkforce??oldWorkforce,AfterWorkforce=order.Workforce};
                    if(targetChanged)row=row with{BeforeTarget=row.BeforeTarget==row.AfterTarget?oldTarget:row.BeforeTarget,AfterTarget=order.Target};
                    entries[order.Colony]=row with{Issues=order.Issues.Distinct().OrderBy(i=>i.Code,StringComparer.Ordinal).ThenBy(i=>i.ContentId,StringComparer.Ordinal).ToImmutableArray()};
                }
            }
            var sorted=entries.Values.OrderBy(e=>e.Colony).ToImmutableArray();
            int actions=sorted.Count(e=>e.BeforeWorkforce.HasValue||e.BeforeTarget!=e.AfterTarget);
            working.FindEmpire(owner)!.Development!.PendingReport=new(state.Turn,sorted,count-actions);
            return GameStateValidator.Validate(content,working,allowAllHumansReady:true).Count==0?new(working,true):new(state,false);
            bool Apply(Command command,int? token,ProductionOrigin origin) {
                var outcome=CommandProcessor.ExecuteGovernor(content,working,command,token,origin);
                if(!outcome.Accepted)return false;
                if(outcome.Changed&&++changed>budget)return false;
                working=outcome.State;return true;
            }
        }catch(Exception error) when(error is OverflowException or ArgumentException or InvalidOperationException or KeyNotFoundException){return new(state,false);}
    }
}
