using System.Text.Json;
namespace OpenAntares.Simulation.Content;
public static partial class ContentWriter
{
    private static void WriteSpecies(Utf8JsonWriter writer, ContentSet content)
    {
        writer.WriteStartArray("environments");
        foreach(var environment in content.Environments.Values)
        {
            writer.WriteStartObject();writer.WriteString("id",environment.Id);writer.WriteString("name",environment.Name);writer.WriteString("description",environment.Description);writer.WriteEndObject();
        }
        writer.WriteEndArray();writer.WriteStartArray("species");
        foreach(var sp in content.Species.Values)
        {
            writer.WriteStartObject();writer.WriteString("id",sp.Id);writer.WriteString("name",sp.Name);writer.WriteString("description",sp.Description);
            writer.WriteString("culture_name",sp.CultureName);writer.WriteString("population_unit_label",sp.PopulationUnitLabel);
            writer.WriteNumber("starting_population",sp.StartingPopulation);writer.WriteStartObject("starting_workforce");
            writer.WriteNumber("support",sp.StartingWorkforce.Support);writer.WriteNumber("production",sp.StartingWorkforce.Production);writer.WriteNumber("research",sp.StartingWorkforce.Research);writer.WriteEndObject();
            writer.WriteNumber("base_capacity",sp.BaseCapacity);if(sp.SealedBaseCapacity is { } capacity)writer.WriteNumber("sealed_base_capacity",capacity);else writer.WriteNull("sealed_base_capacity");
            writer.WriteNumber("support_per_population",sp.SupportPerPopulation);writer.WriteNumber("production_basis_points",sp.ProductionBasisPoints);writer.WriteNumber("research_basis_points",sp.ResearchBasisPoints);
            writer.WriteNumber("base_growth",sp.BaseGrowth);writer.WriteNumber("max_surplus_growth",sp.MaxSurplusGrowth);writer.WriteNumber("support_per_growth_point",sp.SupportPerGrowthPoint);
            WriteStrings(writer,"natural_environment_ids",sp.NaturalEnvironmentIds);writer.WriteBoolean("allows_sealed_habitats",sp.AllowsSealedHabitats);WriteStrings(writer,"allowed_vessel_ids",sp.AllowedVesselIds);writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}
