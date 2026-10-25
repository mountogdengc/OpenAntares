using OpenAntares.Simulation.Colonization;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Rivals;

/// <summary>Only public definitions, own facts and scoped hypothetical calculations reach a policy.</summary>
public interface IRivalPlanningContext
{
    EmpireView View { get; }
    ContentSet Content { get; }
    RivalAiDefinition Policy { get; }
    Scouting.ScoutPreparationPreview? ScoutPreparation => null;
    ColonyEconomy? Forecast(ColonyId colony, Workforce workforce, ProductionTarget? target, string? completedBuildingId = null);
    FoundingPreview? PreviewSettlement(PlanetId target, string roleId);
    int? TravelTurns(StarId origin, StarId destination, string roleId);
}
