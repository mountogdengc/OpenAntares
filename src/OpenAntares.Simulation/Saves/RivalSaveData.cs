using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Saves;

internal static class RivalSaveData
{
    internal static void WriteEmpire(Utf8JsonWriter writer, EmpireState empire, int rulesVersion)
    {
        if (!Rules.HasRivals(rulesVersion)) return;
        writer.WriteString("ai_policy_id", empire.AiPolicyId);
        writer.WriteStartArray("contacts");
        foreach (var contact in empire.Contacts)
        {
            writer.WriteStartObject();
            writer.WriteNumber("other_empire", contact.OtherEmpireId.Value);
            writer.WriteString("species_id", contact.SpeciesId);
            writer.WriteNumber("first_observed_turn", contact.FirstObservedTurn);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    internal static void ReadEmpire(EntryReader reader, EmpireState empire, int rulesVersion)
    {
        // Leave every new field unread for old rules, so unknown-field validation rejects injection.
        if (!Rules.HasRivals(rulesVersion)) return;
        empire.AiPolicyId = reader.RequiredNullableString("ai_policy_id");
        if (reader.RequiredArray("contacts") is not { } contacts) return;
        int index = 0;
        foreach (var element in contacts.EnumerateArray())
        {
            if (reader.Nested(element, $"contacts[{index}]") is { } contact)
            {
                empire.Contacts.Add(new(new(contact.RequiredInt("other_empire", 1, int.MaxValue)),
                    contact.RequiredString("species_id"), contact.RequiredInt("first_observed_turn", 0, int.MaxValue)));
                contact.RejectUnknownFields();
            }
            index++;
        }
    }
}
