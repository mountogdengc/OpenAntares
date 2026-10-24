using System.Text.Json;

namespace OpenAntares.Simulation.Content;

public static partial class ContentWriter
{
    private static void WriteRivals(Utf8JsonWriter writer, RivalAiDefinition policy)
    {
        writer.WriteStartObject("rival_ai");
        writer.WriteString("algorithm_id", policy.AlgorithmId);
        writer.WriteNumber("growth_support_workers", policy.GrowthSupportWorkers);
        writer.WriteNumber("resurvey_min_age", policy.ResurveyMinAge);
        writer.WriteEndObject();
    }
}
