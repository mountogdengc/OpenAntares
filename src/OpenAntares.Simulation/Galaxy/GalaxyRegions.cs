using System;
using System.Collections.Immutable;

namespace OpenAntares.Simulation.Galaxy;

/// <summary>Stable identifiers for the three regions of a generated spiral galaxy.</summary>
public static class GalaxyRegions
{
    public const string Core = "core";
    public const string Arm = "arm";
    public const string Rim = "rim";

    public static readonly ImmutableArray<string> Ids = [Core, Arm, Rim];

    public static readonly ImmutableSortedDictionary<string, GalaxyRegionDefinition> DefaultDefinitions =
        ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, new[]
        {
            new System.Collections.Generic.KeyValuePair<string, GalaxyRegionDefinition>(Core,
                new(Core, "Core", "The dense galactic center.")),
            new System.Collections.Generic.KeyValuePair<string, GalaxyRegionDefinition>(Arm,
                new(Arm, "Arm", "The star-rich spiral arms.")),
            new System.Collections.Generic.KeyValuePair<string, GalaxyRegionDefinition>(Rim,
                new(Rim, "Rim", "The sparse outer galaxy.")),
        });

    public static bool IsKnown(string id) => id is Core or Arm or Rim;
}

public sealed record GalaxyRegionDefinition(string Id, string Name, string Description);
