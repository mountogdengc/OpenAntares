using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Development;
public interface IGovernorPlanningContext {ContentSet Content{get;} EmpireView View{get;} ColonyEconomy? Forecast(ColonyId colony,Workforce workforce,ProductionTarget? target);}
