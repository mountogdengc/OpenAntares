using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Economy;

public sealed record EmbarkationRequirement(string Code, int Actual, int Required);
public sealed record EmbarkationEligibility(bool Eligible, ImmutableArray<EmbarkationRequirement> Unmet);
public static class EmbarkationRules
{
    /// <summary>Only the beginning snapshot can establish eligibility; births and completions cannot change it.</summary>
    public static EmbarkationEligibility Evaluate(ContentSet content, GameState snapshot, ColonyState colony, VesselDefinition role)
    {
        var unmet=ImmutableArray.CreateBuilder<EmbarkationRequirement>();
        if(role.EmbarkPopulation>0&&snapshot.FindPlanet(colony.PlanetId) is {} port&&Crises.CrisisQueries.PortClosed(snapshot,colony.EmpireId,port.StarId))unmet.Add(new("quarantine",0,1));
        if(colony.Population < role.MinimumSourcePopulation) unmet.Add(new("minimum_population",colony.Population,role.MinimumSourcePopulation));
        if(colony.Workforce.Support < role.EmbarkPopulation) unmet.Add(new("support_workers",colony.Workforce.Support,role.EmbarkPopulation));
        if(role.CanFoundColony && colony.Population-role.EmbarkPopulation < 1) unmet.Add(new("remaining_population",colony.Population-role.EmbarkPopulation,1));
        if(Rules.HasSpecies(snapshot.RulesVersion) && role.RequiresFullColony)
        {
            int capacity=Rules.ColonyCapacity(content,snapshot,colony);
            if(colony.Population != capacity) unmet.Add(new("full_colony",colony.Population,capacity));
        }
        return new(unmet.Count == 0,unmet.ToImmutable());
    }
}
