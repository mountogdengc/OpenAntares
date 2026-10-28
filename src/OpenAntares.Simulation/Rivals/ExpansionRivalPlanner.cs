using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Rivals;

public static class ExpansionRivalPlanner
{
    public static RivalPlanningResult Plan(Func<IRivalPlanningContext> current, Func<Command, bool> apply)
    {
        var initial = current(); long count = 0;
        long budget = 2L * initial.View.Colonies.Length + (Rules.HasScouting(initial.View.RulesVersion)?2L:1L)*initial.View.Vessels.Length + 1;
        bool Emit(Command command)
        {
            if (count >= budget || count == int.MaxValue) return false;
            count++;
            return apply(command);
        }
        bool LegalResearch(string id) => Rules.TechnologyAvailable(initial.Content, initial.View.RulesVersion, id) && initial.Content.Technologies.TryGetValue(id, out var definition)
            && !initial.View.Research.KnownTechnologyIds.Contains(id)
            && definition.PrerequisiteTechnologyIds.All(initial.View.Research.KnownTechnologyIds.Contains);
        string? research = initial.View.Research.TargetId is { } selected && LegalResearch(selected) ? selected :
            initial.Content.Technologies.Values.Where(t => LegalResearch(t.Id)).OrderBy(t => t.Cost).ThenBy(t => t.Id, StringComparer.Ordinal).FirstOrDefault()?.Id;
        if (research != initial.View.Research.TargetId && !Emit(new SetResearchTargetCommand(initial.View.Viewer, initial.View.Turn, research)))
            return new(false, (int)count);
        foreach (var goal in RivalColonyPlanner.Plan(current()))
        {
            var context = current(); var colony = context.View.Colonies.Single(c => c.Id == goal.Colony);
            if ((colony.ProjectId != goal.Target?.ContentId || colony.ProjectKind != (goal.Target?.Kind ?? ProductionKinds.Building)) &&
                !Emit(new SetProductionTargetCommand(context.View.Viewer, context.View.Turn, goal.Colony, goal.Target))) return new(false, (int)count);
            context = current(); colony = context.View.Colonies.Single(c => c.Id == goal.Colony);
            if (colony.Workforce != goal.Workforce && !Emit(new SetWorkforceCommand(context.View.Viewer, context.View.Turn, goal.Colony, goal.Workforce))) return new(false, (int)count);
        }
        return new(RivalVesselPlanner.Plan(current, Emit), (int)count);
    }
}
