using System.Text.Json;
using OpenAntares.Simulation.Development;
namespace OpenAntares.Simulation.Content;
public static partial class ContentWriter
{
    private static void WriteDevelopment(Utf8JsonWriter w,ContentSet content)
    {
        w.WriteStartArray("governor_templates");foreach(var t in content.GovernorTemplates.Values){w.WriteStartObject();w.WriteString("id",t.Id);GovernorTemplateJson.WriteFields(w,new(t.Name,t.FoundationIds,t.DevelopmentIds,t.SupportTarget,t.ProductionWeight,t.ResearchWeight));w.WriteEndObject();}w.WriteEndArray();
    }
}
