using System.Text.Json;
using OpenAntares.Simulation.Content;
namespace OpenAntares.Simulation.Saves;
internal static class SpeciesSaveData
{
    internal static void Write(Utf8JsonWriter writer, string field, string? value, int rulesVersion)
    {
        if(rulesVersion == Rules.SpeciesVersion) writer.WriteString(field,value);
    }
    // In old campaigns do not consume the field: the ordinary unknown-field check rejects injection.
    internal static string? Read(EntryReader reader, string field, int rulesVersion) => rulesVersion == Rules.SpeciesVersion ? reader.RequiredString(field) : null;
}
