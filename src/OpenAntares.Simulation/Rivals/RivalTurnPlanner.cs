using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Rivals;

public sealed record RivalPlanningOutcome(GameState State, bool Succeeded);

/// <summary>Runs bounded policies on private state. The resolver owns final publication.</summary>
public static class RivalTurnPlanner
{
    internal const string FailureMessage = "The turn could not be completed.";

    public static RivalPlanningOutcome Prepare(ContentSet content, GameState snapshot) => Prepare(content,snapshot,ExpansionRivalPlanner.Plan);

    internal static RivalPlanningOutcome Prepare(ContentSet content, GameState snapshot,
        Func<Func<IRivalPlanningContext>,Func<Command,bool>,RivalPlanningResult> plan)
    {
        if (GameStateValidator.Validate(content,snapshot,allowAllHumansReady:true).Count != 0) return new(snapshot,false);
        if (!Rules.HasRivals(snapshot.RulesVersion)) return new(snapshot,true);
        var working = snapshot.Clone();
        try
        {
            foreach (var owner in snapshot.Empires.Where(e => e.Controller == ControllerKind.Ai).OrderBy(e => e.Id))
            {
                long submitted = 0;
                long budget = 2L*working.Colonies.Count(c => c.EmpireId == owner.Id)+working.Vessels.Count(v => v.EmpireId == owner.Id)+1;
                bool failed = false;
                var result = plan(() => RivalPlanningQueries.Create(content,working,owner.Id), command =>
                {
                    if (failed) return false;
                    if (submitted >= budget || command.Issuer != owner.Id || command.ExpectedTurn != snapshot.Turn ||
                        command is not (SetWorkforceCommand or SetProductionTargetCommand or SetResearchTargetCommand or SetMoveOrderCommand or SetFoundingOrderCommand))
                    { failed = true; return false; }
                    submitted++;
                    var outcome = CommandProcessor.Execute(content,working,command);
                    if (!outcome.Accepted) { failed = true; return false; }
                    working = outcome.State;
                    return true;
                });
                if (failed || !result.Succeeded || submitted != result.CommandCount) return new(snapshot,false);
            }
            return GameStateValidator.Validate(content,working,allowAllHumansReady:true).Count == 0 ? new(working,true) : new(snapshot,false);
        }
        catch (Exception error) when (error is OverflowException or ArgumentException or InvalidOperationException or KeyNotFoundException)
        { return new(snapshot,false); }
    }
}
