namespace OpenAntares.Simulation.Content;

/// <summary>Embedded tuning for the bounded expansion policy. No difficulty bonuses.</summary>
public sealed record RivalAiDefinition(string AlgorithmId, int GrowthSupportWorkers, int ResurveyMinAge);
