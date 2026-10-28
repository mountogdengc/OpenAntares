using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation;

/// <summary>Rule calculations shared by commands, validation, and turn resolution.</summary>
public static class Rules
{
    /// <summary>Version of the rules implementation. Saves record it, because formulas in code can't be embedded.</summary>
    public const int LegacyVersion = 1;
    public const int ExpansionVersion = 2;
    public const int SpeciesVersion = 3;
    public const int RivalVersion = 4;
    public const int DevelopmentVersion = 5;
    public const int CrisisVersion = 6;
    public const int ScoutVersion = 7;
    public const int FleetVersion = 8;
    public static bool HasFleets(int version) => version == FleetVersion;
    public static bool HasScouting(int version) => version is ScoutVersion or FleetVersion;
    public static bool HasCrises(int version) => version is CrisisVersion or ScoutVersion or FleetVersion;
    public const int CurrentVersion = FleetVersion;
    public static bool IsSupported(int version) => version is LegacyVersion or ExpansionVersion or SpeciesVersion or RivalVersion or DevelopmentVersion or CrisisVersion or ScoutVersion or FleetVersion;
    public static bool HasExpansion(int version) => version is ExpansionVersion or SpeciesVersion or RivalVersion or DevelopmentVersion or CrisisVersion or ScoutVersion or FleetVersion;
    public static bool HasSpecies(int version) => version is SpeciesVersion or RivalVersion or DevelopmentVersion or CrisisVersion or ScoutVersion or FleetVersion;
    public static bool HasRivals(int version) => version is RivalVersion or DevelopmentVersion or CrisisVersion or ScoutVersion or FleetVersion;

    public static bool HasDevelopment(int version) => version is DevelopmentVersion or CrisisVersion or ScoutVersion or FleetVersion;

    public static bool TechnologyAvailable(ContentSet content, int rulesVersion, string technologyId) =>
        IsSupported(rulesVersion) && content.Technologies.ContainsKey(technologyId) &&
        (HasFleets(rulesVersion) || content.FleetCommand?.RecoveryTechnologyId != technologyId);

    /// <summary>A colony's population capacity: the base capacity plus completed capacity buildings.</summary>
    public static int ColonyCapacity(ContentSet content, ColonyState colony)
    {
        long capacity = content.Rules.BaseColonyCapacity;
        foreach (string buildingId in colony.CompletedBuildingIds)
        {
            foreach (BuildingEffect effect in content.Buildings[buildingId].Effects)
            {
                if (effect.Kind == EffectKinds.Capacity)
                {
                    capacity += effect.Amount;
                }
            }
        }

        return checked((int)capacity);
    }

    public static int ColonyCapacity(ContentSet content, GameState state, ColonyState colony)
    {
        if (!HasSpecies(state.RulesVersion)) return ColonyCapacity(content, colony);
        long capacity = Species.SpeciesRules.BaseCapacity(content, state, colony.EmpireId, colony.HabitatMode);
        foreach (string id in colony.CompletedBuildingIds)
            foreach (var effect in content.Buildings[id].Effects)
                if (effect.Kind == EffectKinds.Capacity) capacity = checked(capacity + effect.Amount);
        return checked((int)capacity);
    }

    /// <summary>Whether the empire knows every listed prerequisite technology.</summary>
    public static bool PrerequisitesKnown(EmpireState empire, ImmutableArray<string> prerequisites) =>
        prerequisites.All(empire.KnownTechnologyIds.Contains);
}
