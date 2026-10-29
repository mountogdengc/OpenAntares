using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAntares.Simulation.Content;

namespace OpenAntares.Simulation.Combat;

internal static class CombatContentReader
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    internal static CombatDefinition? Read(JsonElement element, string file, List<ContentError> errors)
    {
        try
        {
            Unique(element);
            var value = element.Deserialize<CombatDefinition>(Options);
            if (value is null) throw new JsonException("A combat definition object is required.");
            return value.RoleProfiles is null ? value : value with { RoleProfiles = value.RoleProfiles.ToImmutableSortedDictionary(StringComparer.Ordinal) };
        }
        catch (Exception error) when (error is JsonException or NotSupportedException or ArgumentException)
        {
            errors.Add(new(file, "combat", "combat", error.Message));
            return null;
        }
    }

    internal static void Validate(CombatDefinition value, IEnumerable<VesselDefinition> roles, string file, List<ContentError> errors)
    {
        void Error(string field, string message, string id = "combat") => errors.Add(new(file, id, "combat." + field, message));
        void Bound(string field, int number, int maximum, int minimum = 1)
        { if (number < minimum || number > maximum) Error(field, $"Expected {minimum}..{maximum}."); }
        void Profile(string id, CombatProfile? profile, string path)
        {
            if (profile is null) { Error(path, "Profile is required.", id); return; }
            if (profile.MaxHull < 1) Error(path + ".max_hull", "Hull must be positive.", id);
            if (profile.Attack < 0) Error(path + ".attack", "Attack cannot be negative.", id);
            if (profile.Armor < 0) Error(path + ".armor", "Armor cannot be negative.", id);
        }
        var vessels = roles.ToDictionary(v => v.Id, StringComparer.Ordinal);
        if (value.RoleProfiles is null || value.RoleProfiles.Count == 0) Error("role_profiles", "Every defined vessel role needs a profile.");
        else
        {
            foreach (var profile in value.RoleProfiles)
            {
                Profile(profile.Key, profile.Value, "role_profiles." + profile.Key);
                if (!vessels.ContainsKey(profile.Key)) Error("role_profiles." + profile.Key, "Profile references an undefined vessel role.", profile.Key);
            }
            foreach (var role in vessels.Values)
            {
                if (!value.RoleProfiles.TryGetValue(role.Id, out var profile)) { Error("role_profiles." + role.Id, "Missing vessel profile.", role.Id); continue; }
                if (role.CombatOnly && (profile is null || profile.Attack < 1 || role.CanFoundColony || role.EmbarkPopulation != 0 || role.RequiresFullColony || role.FoundsSealedHabitat))
                    Error("role_profiles." + role.Id, "Combat-only hulls must be armed and carry no settlement population or founding capability.", role.Id);
            }
        }
        if (string.IsNullOrWhiteSpace(value.SentryRoleId) || value.SentryRoleId.Length > 80 || vessels.ContainsKey(value.SentryRoleId)) Error("sentry_role_id", "Use a distinct bounded nonempty hazard role ID.");
        Profile(value.SentryRoleId ?? "sentry", value.SentryProfile, "sentry_profile");
        if (value.SentryProfile is { Attack: < 1 }) Error("sentry_profile.attack", "A sentry must be armed.");
        Bound("sentry_pursuit_speed", value.SentryPursuitSpeed, int.MaxValue);
        Bound("pocket_limit", value.PocketLimit, 1); Bound("sentries_per_pocket", value.SentriesPerPocket, 2);
        Bound("history_limit", value.HistoryLimit, 20); Bound("round_detail_limit", value.RoundDetailLimit, 20);
        Bound("destroyed_cohesion_loss", value.DestroyedCohesionLoss, 100, 0);
        Bound("crippled_cohesion_loss", value.CrippledCohesionLoss, 100, 0);
        Bound("flagship_cohesion_loss", value.FlagshipCohesionLoss, 100, 0);
        Bound("retreat_cohesion_threshold", value.RetreatCohesionThreshold, 100, 0);
        Bound("repair_hull_divisor", value.RepairHullDivisor, int.MaxValue); Bound("repair_cohesion", value.RepairCohesion, 100);
        if (value.HullThresholds.IsDefault || value.HullThresholds.Length != 2 || value.HullThresholds[0] is <= 0 or >= 100 || value.HullThresholds[1] <= 0 || value.HullThresholds[1] >= value.HullThresholds[0]) Error("hull_thresholds", "Provide two descending thresholds strictly between 0 and 100.");
        Factors("attack_percent", value.AttackPercent, 0); Factors("speed_percent", value.SpeedPercent, 1);
        void Factors(string field, ImmutableArray<int> factors, int minimum)
        {
            if (factors.IsDefault || factors.Length != 3 || factors.Any(v => v < minimum || v > 100) || factors[0] < factors[1] || factors[1] < factors[2]) Error(field, "Provide three bounded nonincreasing percent factors.");
        }
    }

    private static void Unique(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in element.EnumerateObject())
            {
                if (!names.Add(field.Name)) throw new JsonException("Duplicate combat field: " + field.Name);
                Unique(field.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) Unique(item);
    }
}
