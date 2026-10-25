namespace OpenAntares.Simulation.Scouting;
public static class ScoutPlanner
{
    public static ScoutPlan Plan(IScoutPlanningContext context,bool ai)=>new(context.Preparation.Commands,context.Preparation.Outcomes);
}
