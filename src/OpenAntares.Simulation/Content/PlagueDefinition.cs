namespace OpenAntares.Simulation.Content;

/// <summary>Validated data parameters for the supported plague family. Money is stored hundredths.</summary>
public sealed record PlagueDefinition(
    string Id, string Name,
    int FirstCheckTurn,
    int CheckInterval,
    int CheckChancePercent,
    int CooldownTurns,
    int GraceTurns,
    int RecoveryIntervals,
    int InitialSeverity,
    int SeverityCap,
    int OpenSeverityStep,
    int QuarantinedSeverityStep,
    int MortalityThreshold,
    int MortalityUnits,
    int IllnessFactorBp,
    int QuarantineFactorBp,
    long CureBase,
    long CurePerPopulation,
    int ImmunityTurns,
    int PurgeNumerator,
    int PurgeDenominator,
    int CivicFactorLossBp,
    int CivicMaxLevel,
    int CivicTurns);
