using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Fleets;
public static class FleetPlanningQueries
{
    internal static long RequestShipCost(IFleetPlanningContext context,AssemblyDraft draft,ColonyId colony,string role,RequestId? replacing)
        =>OfficerBonuses.RequestShipCost(context.Content,context.View,draft,colony,role,replacing);
    public static IFleetPlanningContext Create(ContentSet content,EmpireView view)=>new Context(content,view with{Galaxy=view.Galaxy with{Seed=null}});
    private sealed record Context(ContentSet Content,EmpireView View):IFleetPlanningContext
    {
        private readonly Development.IGovernorPlanningContext _own=Development.GovernorPlanningQueries.Create(Content,View);
        public ColonyEconomy? Forecast(ColonyId colony,Workforce workforce,ProductionTarget? target)=>_own.Forecast(colony,workforce,target);
    }
}
