using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Simulation.State;

internal static class RivalStateValidator
{
    internal static void Validate(ContentSet content, GameState state, List<string> errors)
    {
        if (!Rules.HasRivals(state.RulesVersion))
        {
            if (state.Empires.Any(e => e.AiPolicyId is not null || e.Contacts.Count != 0))
                errors.Add("Old campaigns cannot contain rival policy or contact metadata.");
            return;
        }
        if (content.RivalAi is not { AlgorithmId: "expansion_v1", GrowthSupportWorkers: 0 or 1, ResurveyMinAge: > 0 })
            errors.Add("Rival rules require supported embedded policy content.");
        foreach (var empire in state.Empires)
        {
            if (empire.Controller == ControllerKind.Human ? empire.AiPolicyId is not null : empire.AiPolicyId != "expansion_v1")
                errors.Add($"{empire.Id}: invalid ai_policy_id for controller.");
            int previous = 0;
            foreach (var contact in empire.Contacts)
            {
                var other = state.FindEmpire(contact.OtherEmpireId);
                if (contact.OtherEmpireId.Value <= previous || other is null || other.Id == empire.Id ||
                    !content.Species.ContainsKey(contact.SpeciesId) || other.SpeciesId != contact.SpeciesId ||
                    contact.FirstObservedTurn < 0 || contact.FirstObservedTurn > state.Turn)
                    errors.Add($"{empire.Id}: invalid, unsorted or duplicate contact.");
                previous = contact.OtherEmpireId.Value;
            }
            var seenOwners = empire.Surveys.Where(o => ObservationUpdater.IsCurrent(state, empire.Id, o.StarId))
                .SelectMany(o => o.Planets).Where(p => p.OwnerId.HasValue).Select(p => p.OwnerId!.Value)
                .Concat(state.Vessels.Where(v => v.EmpireId != empire.Id && v.StationaryStarId is { } star &&
                    ObservationUpdater.IsCurrent(state, empire.Id, star)).Select(v => v.EmpireId));
            foreach (var other in seenOwners.Where(id => id != empire.Id).Distinct().OrderBy(id => id))
                if (!empire.Contacts.Any(c => c.OtherEmpireId == other))
                    errors.Add($"{empire.Id}: current foreign observations require a contact.");
        }
    }
}
