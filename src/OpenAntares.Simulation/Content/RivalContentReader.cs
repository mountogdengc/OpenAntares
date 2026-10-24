using System.Text.Json;

namespace OpenAntares.Simulation.Content;

public static partial class ContentLoader
{
    private sealed partial class Builder
    {
        private (RivalAiDefinition Value, string File)? _rivalAi;

        private RivalAiDefinition? ReadRivalAi(JsonElement element, string file)
        {
            if (EntryReader.Create(element, file, null, "rival_ai", _errors) is not { } reader) return null;
            string algorithm = reader.RequiredString("algorithm_id");
            if (algorithm != "expansion_v1") reader.Error("algorithm_id", "Unsupported rival policy algorithm.");
            int growth = reader.RequiredInt("growth_support_workers", 0, 1);
            int age = reader.RequiredInt("resurvey_min_age", 1, int.MaxValue);
            reader.RejectUnknownFields();
            return new(algorithm, growth, age);
        }

        private void CheckRivals()
        {
            if (requireRivals && _rivalAi is null)
                _errors.Add(new(AllFiles, null, "rival_ai", "Rival rules require an explicit embedded policy."));
        }
    }
}
