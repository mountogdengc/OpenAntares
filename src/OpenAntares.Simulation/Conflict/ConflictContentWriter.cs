using System.Text.Json;
namespace OpenAntares.Simulation.Conflict;
internal static class ConflictContentWriter
{
    internal static void Write(Utf8JsonWriter writer,ConflictDefinition definition)
    {writer.WritePropertyName("conflict");JsonSerializer.Serialize(writer,definition,ConflictContentReader.Options);}
}
