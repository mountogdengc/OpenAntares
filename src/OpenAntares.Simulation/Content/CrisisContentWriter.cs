using System.Text.Json;
namespace OpenAntares.Simulation.Content;
public static partial class ContentWriter
{
    private static void WriteCrises(Utf8JsonWriter w,ContentSet content)
    {
        w.WriteStartArray("plagues");
        foreach(var d in content.Plagues.Values)
        {
            w.WriteStartObject();w.WriteString("id",d.Id);w.WriteString("name",d.Name);
            w.WriteNumber("first_check_turn",d.FirstCheckTurn);
            w.WriteNumber("check_interval",d.CheckInterval);
            w.WriteNumber("check_chance_percent",d.CheckChancePercent);
            w.WriteNumber("cooldown_turns",d.CooldownTurns);
            w.WriteNumber("grace_turns",d.GraceTurns);
            w.WriteNumber("recovery_intervals",d.RecoveryIntervals);
            w.WriteNumber("initial_severity",d.InitialSeverity);
            w.WriteNumber("severity_cap",d.SeverityCap);
            w.WriteNumber("open_severity_step",d.OpenSeverityStep);
            w.WriteNumber("quarantined_severity_step",d.QuarantinedSeverityStep);
            w.WriteNumber("mortality_threshold",d.MortalityThreshold);
            w.WriteNumber("mortality_units",d.MortalityUnits);
            w.WriteNumber("illness_factor_bp",d.IllnessFactorBp);
            w.WriteNumber("quarantine_factor_bp",d.QuarantineFactorBp);
            w.WriteNumber("cure_base",d.CureBase);
            w.WriteNumber("cure_per_population",d.CurePerPopulation);
            w.WriteNumber("immunity_turns",d.ImmunityTurns);
            w.WriteNumber("purge_numerator",d.PurgeNumerator);
            w.WriteNumber("purge_denominator",d.PurgeDenominator);
            w.WriteNumber("civic_factor_loss_bp",d.CivicFactorLossBp);
            w.WriteNumber("civic_max_level",d.CivicMaxLevel);
            w.WriteNumber("civic_turns",d.CivicTurns);
            w.WriteEndObject();
        }
        w.WriteEndArray();
    }
}
