using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Fleets;
public interface IFleetPlanningContext
{
    ContentSet Content { get; }
    EmpireView View { get; }
    ColonyEconomy? Forecast(ColonyId colony,Workforce workforce,ProductionTarget? target);
}
