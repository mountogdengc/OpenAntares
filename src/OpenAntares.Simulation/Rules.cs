using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation;

/// <summary>Rule calculations shared by commands, validation, and turn resolution.</summary>
public static class Rules
{
    /// <summary>Version of the rules implementation. Saves record it, because formulas in code can't be embedded.</summary>
    public const int CurrentVersion = 1;

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

    /// <summary>Whether the empire knows every listed prerequisite technology.</summary>
    public static bool PrerequisitesKnown(EmpireState empire, ImmutableArray<string> prerequisites) =>
        prerequisites.All(empire.KnownTechnologyIds.Contains);
}
