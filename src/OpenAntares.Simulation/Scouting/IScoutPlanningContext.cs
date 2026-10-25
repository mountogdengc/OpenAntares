using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Scouting;
public interface IScoutPlanningContext
{
    ContentSet Content { get; }
    EmpireView View { get; }
    ScoutPreparationPreview Preparation { get; }
    int? TravelTurns(StarId origin,StarId destination,string roleId);
}
public sealed record ScoutPlan(ImmutableArray<Command> Commands,ImmutableArray<ScoutOrderEvent> Outcomes);
public sealed record ScoutPreparationResult(GameState State,bool Succeeded,ScoutPlan Plan);
