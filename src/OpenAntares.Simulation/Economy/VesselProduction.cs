using System.Collections.Generic;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Economy;

public sealed record VesselCompletionPlan(string RoleId, int Cargo, ColonyId Source, StarId Star);
public sealed record EmbarkOutcome(int PopulationAfter, Workforce WorkforceAfter, Quantity NextSupportProduced, Quantity NextSupportRequired)
{
    public bool ReassignmentNeeded => NextSupportProduced.Total < NextSupportRequired.Total;
}
internal static class VesselProduction
{
    internal static ColonyEconomy Calculate(ContentSet content, GameState state, ColonyState colony, PlanetState planet, PlanetTypeDefinition type, List<BuildingDefinition> buildings, ColonyEconomy result)
    {
        if (state.RulesVersion != Rules.ExpansionVersion || colony.ProjectKind != ProductionKinds.Vessel || colony.ProjectId is not { } id || !content.Vessels.TryGetValue(id, out var role)) return result;
        bool eligible = colony.Population >= role.MinimumSourcePopulation && colony.Workforce.Support >= role.EmbarkPopulation;
        var reserve = EconomyCalculator.Reserve(colony.ProductionReserve, result.NetProduction.Total, id, role.Cost, eligible) with { TargetKind = ProductionKinds.Vessel, BlockedReasonCode = eligible ? null : "awaiting_embarkation" };
        if (!eligible) reserve = reserve with { Estimate = new TurnEstimate(null) };
        result = result with { Production = reserve };
        if (reserve.CompletedId is null) return result;
        var plan = new VesselCompletionPlan(id, role.EmbarkPopulation, colony.Id, planet.StarId);
        EmbarkOutcome? embark = null;
        if (role.EmbarkPopulation > 0)
        {
            int population = result.Growth.PopulationAfter - role.EmbarkPopulation;
            var workers = result.Growth.WorkforceAfter with { Support = result.Growth.WorkforceAfter.Support - role.EmbarkPopulation };
            embark = new(population, workers, EconomyCalculator.Output(type.Id, workers.Support, type.SupportPerWorker, buildings, EffectKinds.Support), Quantity.From(new BreakdownLine(BreakdownKinds.Population, string.Empty, population, checked(population * content.Rules.SupportPerPopulation))));
        }
        return result with { VesselCompletionPlan = plan, EmbarkOutcome = embark };
    }
}
