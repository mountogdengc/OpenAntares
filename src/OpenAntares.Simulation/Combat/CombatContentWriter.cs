using System.Text.Json;

namespace OpenAntares.Simulation.Combat;

internal static class CombatContentWriter
{
    internal static void Write(Utf8JsonWriter writer, CombatDefinition definition)
    {
        writer.WritePropertyName("combat");
        JsonSerializer.Serialize(writer, definition, CombatContentReader.Options);
    }
}
