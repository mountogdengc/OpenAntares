using System.Text.Json;
namespace OpenAntares.Simulation.ShipDesigns;
internal static class ShipDesignContentWriter
{
    internal static void Write(Utf8JsonWriter writer,ShipDesignDefinition definition)
    {
        writer.WritePropertyName("ship_designs");
        JsonSerializer.Serialize(writer,definition,ShipDesignContentReader.Options);
    }
}
