using System.Text.Json;
namespace OpenAntares.Simulation.Diplomacy;
internal static class DiplomacyContentWriter
{
    internal static void Write(Utf8JsonWriter writer,DiplomacyDefinition definition)
    {writer.WritePropertyName("diplomacy");JsonSerializer.Serialize(writer,definition,DiplomacyContentReader.Options);}
}
