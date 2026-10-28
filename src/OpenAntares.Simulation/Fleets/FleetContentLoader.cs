using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAntares.Simulation.Fleets;

namespace OpenAntares.Simulation.Content;

public static partial class ContentLoader
{
    internal static readonly JsonSerializerOptions FleetJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false) }
    };

    private sealed partial class Builder
    {
        private (FleetCommandDefinition Value, string File)? _fleetCommand;

        private FleetCommandDefinition? ReadFleetCommand(JsonElement element, string file)
        {
            try
            {
                var result = element.Deserialize<FleetCommandDefinition>(FleetJsonOptions);
                if (result is null) _errors.Add(new(file, null, "fleet_command", "Expected a command definition object."));
                return result;
            }
            catch (JsonException error)
            {
                _errors.Add(new(file, null, "fleet_command." + (error.Path ?? "").TrimStart('$', '.'), error.Message));
                return null;
            }
        }

        private void CheckFleets()
        {
            if (_fleetCommand is not { } entry)
            {
                if (requireFleets || _species.Values.Any(s => s.Value.CommandEmbodiment is not null || s.Value.CommandCultureId is not null))
                    _errors.Add(new(AllFiles, null, "fleet_command", "Fleet rules require explicit embedded command content and species capabilities."));
                return;
            }
            var d = entry.Value;
            void Error(string field, string message) => _errors.Add(new(entry.File, null, "fleet_command." + field, message));
            void Bound(string field, int value, int max) { if (value < 1 || value > max) Error(field, $"Expected1..{max}."); }
            Bound("member_limit", d.MemberLimit, 32); Bound("fleet_limit", d.FleetLimit, 64);
            Bound("request_limit", d.RequestLimit, 32); Bound("participating_colony_limit", d.ParticipatingColonyLimit, 32);
            Bound("active_officer_limit", d.ActiveOfficerLimit, 4); Bound("lifetime_commission_limit", d.LifetimeCommissionLimit, 64);
            Bound("commission_turns", d.CommissionTurns, 1000); Bound("xp_cap", d.XpCap, 1_000_000);
            Bound("highlight_limit", d.HighlightLimit, 32); Bound("detail_turn_limit", d.DetailTurnLimit, 10);
            Bound("mortal_recovery_turns", d.MortalRecoveryTurns, 1000); Bound("link_recovery_turns", d.LinkRecoveryTurns, 1000);
            Bound("last_watch_link_turns", d.LastWatchLinkTurns, 1000); Bound("researched_link_reduction", d.ResearchedLinkReduction, 1000);
            Bound("first_survey_xp", d.FirstSurveyXp, 1000); Bound("assembly_hull_xp", d.AssemblyHullXp, 1000); Bound("study_xp", d.StudyXp, 1000);
            if (d.MortalRecoveryPercent is < 0 or > 100) Error("mortal_recovery_percent", "Expected0..100.");
            if (d.ResearchedRecoveryPercent < d.MortalRecoveryPercent || d.ResearchedRecoveryPercent > 100) Error("researched_recovery_percent", "Equipment cannot reduce recovery or exceed100%.");
            if (d.ActiveOfficerLimit > d.LifetimeCommissionLimit) Error("active_officer_limit", "Cannot exceed the lifetime commission bound.");
            CheckPair("rank_thresholds", d.RankThresholds, d.XpCap, true);
            CheckPair("navigator_bonuses", d.NavigatorBonuses, 100, false);
            CheckPair("survey_research_bonuses", d.SurveyResearchBonuses, 1_000_000, false);
            CheckPair("organizer_discounts", d.OrganizerDiscounts, 99, false);
            if (string.IsNullOrEmpty(d.RecoveryTechnologyId) || !_technologies.ContainsKey(d.RecoveryTechnologyId)) Error("recovery_technology_id", "Unknown recovery technology.");
            if (d.Cultures.IsDefaultOrEmpty || d.Cultures.Length > 64 || d.Profiles.IsDefaultOrEmpty || d.Profiles.Length > 256)
            { Error("profiles", "Provide1..64 cultures and1..256 profiles."); return; }
            var cultures = new Dictionary<string, CommandCultureDefinition>(StringComparer.Ordinal);
            var profiles = new Dictionary<string, OfficerProfileDefinition>(StringComparer.Ordinal);
            foreach (var c in d.Cultures)
            {
                if (c is null || !Text(c.Id, 80) || !Text(c.Name, 80)) { Error("cultures", "Culture ID/name must contain1..80 visible characters."); continue; }
                if (!cultures.TryAdd(c.Id, c)) Error("cultures", "Duplicate culture ID.");
                if (c.DefaultDoctrine != OfficerDoctrine.RecoverCommand) Error("cultures", "Last watch requires manual choice, never a cultural default.");
                if (c.ProfileIds.IsDefaultOrEmpty || c.ProfileIds.Length > 256 || c.ProfileIds.Any(id => !Text(id, 80)) || c.ProfileIds.Distinct(StringComparer.Ordinal).Count() != c.ProfileIds.Length) Error("cultures", "Provide unique bounded profile references.");
            }
            foreach (var profile in d.Profiles)
            {
                if (profile is null || !Text(profile.Id, 80) || !Text(profile.Name, 80) || !Text(profile.Pronouns, 80) || !Text(profile.Personality, 400)) { Error("profiles", "Profile identity/text is missing or exceeds its bound."); continue; }
                if (!profiles.TryAdd(profile.Id, profile)) Error("profiles", "Duplicate profile ID.");
                if (profile.CultureId is null || !cultures.ContainsKey(profile.CultureId)) Error("profiles", "Unknown profile culture.");
                if (!Enum.IsDefined(profile.Specialty)) Error("profiles", "Unknown specialty.");
            }
            foreach (var c in cultures.Values)
            {
                if (c.ProfileIds.IsDefault) continue;
                foreach (string id in c.ProfileIds)
                    if (id is null || !profiles.TryGetValue(id, out var profile) || profile.CultureId != c.Id) Error("cultures", "Unknown profile or wrong culture reference.");
                foreach (var specialty in Enum.GetValues<OfficerSpecialty>())
                    if (!c.ProfileIds.Any(id => id is not null && profiles.TryGetValue(id, out var profile) && profile.CultureId == c.Id && profile.Specialty == specialty)) Error("cultures", "Each culture needs all three fixed specialties.");
            }
            foreach (var profile in profiles.Values)
                if (!cultures.Values.Any(c => !c.ProfileIds.IsDefault && c.ProfileIds.Contains(profile.Id))) Error("profiles", "Profile is not in its culture's candidate catalog.");
            foreach (var species in _species.Values)
            {
                var sp = species.Value;
                if (sp.CommandEmbodiment is null || sp.CommandCultureId is null || !cultures.TryGetValue(sp.CommandCultureId, out var c) || c.Name != sp.CultureName)
                    Error("species", "Every species must declare its embodiment and matching command culture; no species-ID fallback.");
            }

            void CheckPair(string field, ImmutableArray<int> values, int max, bool positive)
            {
                if (values.IsDefault || values.Length != 2 || values.Any(v => v < (positive ? 1 : 0) || v > max) || values[0] >= values[1]) Error(field, "Provide two ordered bounded rank values.");
            }
        }

        private static bool Text(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Length <= max && !value.Any(char.IsControl);
    }
}
