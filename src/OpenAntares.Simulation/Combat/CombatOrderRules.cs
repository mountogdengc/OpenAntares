using System.Collections.Immutable;
using System.Linq;

namespace OpenAntares.Simulation.Combat;

internal static class CombatOrderRules
{
    internal static CombatPhase AfterChange(CombatEncounterView before, CombatOrders selected,
        bool civilianConsent, ImmutableArray<CombatShipFrame> ships, bool hasResponse, CombatDefinition definition)
    {
        if (before.Orders == selected && before.CivilianRiskAuthorized == civilianConsent && !hasResponse)
            return before.Phase;
        if (!ships.Any(ship => ship.Attack > 0)) return CombatPhase.WithdrawalQueued;
        if (before.Phase == CombatPhase.BlockedResponse)
            return before.Round == 0 ? CombatPhase.Pending : CombatPhase.Active;
        if (before.Phase == CombatPhase.WithdrawalQueued &&
            (selected.Doctrine == CombatDoctrine.Hold || selected.Doctrine == CombatDoctrine.Press &&
                ships.Min(ship => ship.Cohesion) > definition.RetreatCohesionThreshold))
            return CombatPhase.Active;
        return before.Phase;
    }

    internal static string Explain(CombatDoctrine doctrine, CombatDefinition definition) => doctrine switch
    {
        CombatDoctrine.Cautious => $"Cautious queues withdrawal after a friendly loss, a newly crippled ship, or cohesion at or below {definition.RetreatCohesionThreshold}. An unarmed force must withdraw.",
        CombatDoctrine.Press => $"Press queues withdrawal at cohesion {definition.RetreatCohesionThreshold} or below, or when no armed ship remains.",
        CombatDoctrine.HitAndRun => "Hit and run performs one normal volley, then queues withdrawal. Direct withdrawal may leave before that volley.",
        _ => "Hold despite losses is a manual opt-in: it ignores losses and cohesion retreat thresholds while an armed ship remains. An unarmed force must withdraw."
    };
}
