using System.Text.Json;
using OpenAntares.Simulation.Galaxy;

namespace OpenAntares.Simulation.Content;

/// <summary>
/// Writes a <see cref="ContentSet"/> in the same JSON shape as the content files, so
/// <see cref="ContentLoader"/> can read and validate it again. Used to embed content in saves.
/// </summary>
public static class ContentWriter
{
    public static void Write(Utf8JsonWriter writer, ContentSet content)
    {
        writer.WriteStartObject();

        RulesParameters rules = content.Rules;
        writer.WriteStartObject("rules");
        writer.WriteNumber("support_per_population", rules.SupportPerPopulation);
        writer.WriteNumber("base_growth", rules.BaseGrowth);
        writer.WriteNumber("max_surplus_growth", rules.MaxSurplusGrowth);
        writer.WriteNumber("base_colony_capacity", rules.BaseColonyCapacity);
        writer.WriteEndObject();

        StartingColonyDefinition start = content.StartingColony;
        writer.WriteStartObject("starting_colony");
        writer.WriteString("planet_type", start.PlanetTypeId);
        writer.WriteNumber("population", start.Population);
        writer.WriteStartObject("workforce");
        writer.WriteNumber("support", start.Workforce.Support);
        writer.WriteNumber("production", start.Workforce.Production);
        writer.WriteNumber("research", start.Workforce.Research);
        writer.WriteEndObject();
        writer.WriteEndObject();

        GalaxyRules galaxy = content.Galaxy;
        writer.WriteStartObject("galaxy");
        writer.WriteNumber("min_star_distance", galaxy.MinStarDistance);
        writer.WriteNumber("min_planets_per_star", galaxy.MinPlanetsPerStar);
        writer.WriteNumber("max_planets_per_star", galaxy.MaxPlanetsPerStar);
        WriteStrings(writer, "star_names", galaxy.StarNames);
        writer.WriteStartObject("regions");
        foreach (GalaxyRegionDefinition region in galaxy.RegionDefinitions.Values)
        {
            writer.WriteStartObject(region.Id);
            writer.WriteString("name", region.Name);
            writer.WriteString("description", region.Description);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
        writer.WriteEndObject();

        writer.WriteStartArray("galaxy_sizes");
        foreach (GalaxySizeDefinition size in content.GalaxySizes.Values)
        {
            writer.WriteStartObject();
            writer.WriteString("id", size.Id);
            writer.WriteString("name", size.Name);
            writer.WriteNumber("star_count", size.StarCount);
            writer.WriteNumber("width", size.Width);
            writer.WriteNumber("height", size.Height);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartArray("planet_types");
        foreach (PlanetTypeDefinition planetType in content.PlanetTypes.Values)
        {
            writer.WriteStartObject();
            writer.WriteString("id", planetType.Id);
            writer.WriteString("name", planetType.Name);
            writer.WriteNumber("support_per_worker", planetType.SupportPerWorker);
            writer.WriteNumber("production_per_worker", planetType.ProductionPerWorker);
            writer.WriteNumber("research_per_worker", planetType.ResearchPerWorker);
            writer.WriteNumber("generation_weight", planetType.GenerationWeight);
            if (planetType.RegionGenerationWeights.Count > 0)
            {
                writer.WriteStartObject("region_generation_weights");
                foreach (var (regionId, weight) in planetType.RegionGenerationWeights)
                {
                    writer.WriteNumber(regionId, weight);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartArray("buildings");
        foreach (BuildingDefinition building in content.Buildings.Values)
        {
            writer.WriteStartObject();
            writer.WriteString("id", building.Id);
            writer.WriteString("name", building.Name);
            writer.WriteNumber("cost", building.Cost);
            WriteStrings(writer, "prerequisites", building.PrerequisiteTechnologyIds);
            writer.WriteStartArray("effects");
            foreach (BuildingEffect effect in building.Effects)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", effect.Kind);
                writer.WriteNumber("amount", effect.Amount);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartArray("technologies");
        foreach (TechnologyDefinition technology in content.Technologies.Values)
        {
            writer.WriteStartObject();
            writer.WriteString("id", technology.Id);
            writer.WriteString("name", technology.Name);
            writer.WriteNumber("cost", technology.Cost);
            WriteStrings(writer, "prerequisites", technology.PrerequisiteTechnologyIds);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    private static void WriteStrings(Utf8JsonWriter writer, string name, System.Collections.Generic.IEnumerable<string> values)
    {
        writer.WriteStartArray(name);
        foreach (string value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }
}
