using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace OpenAntares.Simulation.Combat;

public sealed record CombatProfile(
    [property: JsonRequired] int MaxHull,
    [property: JsonRequired] int Attack,
    [property: JsonRequired] int Armor);

public sealed record CombatDefinition
{
    public required ImmutableSortedDictionary<string, CombatProfile> RoleProfiles { get; init; }
    public required string SentryRoleId { get; init; }
    public required CombatProfile SentryProfile { get; init; }
    public required int SentryPursuitSpeed { get; init; }
    public required int PocketLimit { get; init; }
    public required int SentriesPerPocket { get; init; }
    public required int HistoryLimit { get; init; }
    public required int RoundDetailLimit { get; init; }
    public required ImmutableArray<int> HullThresholds { get; init; }
    public required ImmutableArray<int> AttackPercent { get; init; }
    public required ImmutableArray<int> SpeedPercent { get; init; }
    public required int DestroyedCohesionLoss { get; init; }
    public required int CrippledCohesionLoss { get; init; }
    public required int FlagshipCohesionLoss { get; init; }
    public required int RetreatCohesionThreshold { get; init; }
    public required int RepairHullDivisor { get; init; }
    public required int RepairCohesion { get; init; }
}
