using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace OpenAntares.Simulation.ShipDesigns;

public enum ShipComponentKind { Engine, Weapon, Armor }
public sealed record ShipRecipe(
    [property:JsonRequired] string FrameId,
    [property:JsonRequired] string EngineId,
    [property:JsonRequired] string? ArmorId,
    [property:JsonRequired] ImmutableArray<string> WeaponBankIds);
public sealed record ShipFrameDefinition(
    [property:JsonRequired] string Id,[property:JsonRequired] string Name,
    [property:JsonRequired] int Hull,[property:JsonRequired] int Space,
    [property:JsonRequired] long Cost,[property:JsonRequired] int TravelPercent,
    [property:JsonRequired] ImmutableArray<string> Prerequisites);
public sealed record ShipComponentDefinition(
    [property:JsonRequired] string Id,[property:JsonRequired] string Name,
    [property:JsonRequired] ShipComponentKind Kind,[property:JsonRequired] int Space,
    [property:JsonRequired] long Cost,[property:JsonRequired] int Value,
    [property:JsonRequired] ImmutableArray<string> Prerequisites);
public sealed record ShipDesignDefinition(
    [property:JsonRequired] string WarshipRoleId,
    [property:JsonRequired] ShipRecipe StartingRecipe,
    [property:JsonRequired] ImmutableSortedDictionary<string,ShipFrameDefinition> Frames,
    [property:JsonRequired] ImmutableSortedDictionary<string,ShipComponentDefinition> Components);
public sealed record DesignStats(long BuildCost,int MaxHull,int Attack,int Armor,int BaseSpeed,int SpaceUsed,int SpaceCapacity);
