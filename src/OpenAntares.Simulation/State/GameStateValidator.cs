using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;

namespace OpenAntares.Simulation.State;

/// <summary>
/// Checks the world invariants a published game state must satisfy. Used after every turn
/// resolution and when loading a save. Returns readable errors naming the entity and field.
/// </summary>
public static class GameStateValidator
{
    public static IReadOnlyList<string> Validate(ContentSet content, GameState state)
    {
        var errors = new List<string>();

        if (state.RulesVersion != Rules.CurrentVersion)
        {
            errors.Add($"Rules version {state.RulesVersion} is not supported (expected {Rules.CurrentVersion}).");
        }

        if (state.Turn < 0)
        {
            errors.Add($"Turn {state.Turn} is negative.");
        }

        if ((state.Random.Increment & 1UL) == 0)
        {
            errors.Add("Random generator increment must be odd.");
        }

        ValidateIds(state, errors);
        ValidateGalaxy(state, errors);
        ValidateEmpires(content, state, errors);
        ValidatePlanets(content, state, errors);
        ValidateColonies(content, state, errors);

        return errors;
    }

    private static void ValidateGalaxy(GameState state, List<string> errors)
    {
        bool any = state.GalaxySeed.HasValue || state.GalaxyShapeId is not null
            || state.GalaxyWidth.HasValue || state.GalaxyHeight.HasValue;
        bool all = state.GalaxySeed.HasValue && state.GalaxyShapeId is not null
            && state.GalaxyWidth.HasValue && state.GalaxyHeight.HasValue;
        if (any && !all)
        {
            errors.Add("Galaxy metadata must include seed, shape, width, and height together.");
            return;
        }

        if (!any)
        {
            foreach (StarState star in state.Stars.Where(s => s.RegionId is not null))
            {
                errors.Add($"{star.Id}: legacy galaxy cannot have region '{star.RegionId}'.");
            }
            return;
        }

        if (state.GalaxyShapeId != "spiral")
        {
            errors.Add($"Galaxy shape '{state.GalaxyShapeId}' is not supported.");
        }
        if (state.GalaxyWidth <= 0 || state.GalaxyHeight <= 0)
        {
            errors.Add("Galaxy width and height must be positive.");
            return;
        }

        foreach (StarState star in state.Stars)
        {
            if (star.RegionId is null || !GalaxyRegions.IsKnown(star.RegionId))
            {
                errors.Add($"{star.Id}: region '{star.RegionId}' is not defined.");
            }
            if (star.X < 0 || star.X >= state.GalaxyWidth || star.Y < 0 || star.Y >= state.GalaxyHeight)
            {
                errors.Add($"{star.Id}: coordinates ({star.X}, {star.Y}) are outside the galaxy bounds.");
            }
        }
    }

    private static void ValidateIds(GameState state, List<string> errors)
    {
        CheckSortedUnique("Empires", state.Empires.Select(e => e.Id.Value).ToList(), errors);
        CheckSortedUnique("Stars", state.Stars.Select(s => s.Id.Value).ToList(), errors);
        CheckSortedUnique("Planets", state.Planets.Select(p => p.Id.Value).ToList(), errors);
        CheckSortedUnique("Colonies", state.Colonies.Select(c => c.Id.Value).ToList(), errors);

        // All entity kinds share one ID counter, so an ID is never reused across kinds.
        var allIds = state.Empires.Select(e => e.Id.Value)
            .Concat(state.Stars.Select(s => s.Id.Value))
            .Concat(state.Planets.Select(p => p.Id.Value))
            .Concat(state.Colonies.Select(c => c.Id.Value))
            .ToList();
        foreach (int duplicate in allIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).OrderBy(id => id))
        {
            errors.Add($"Entity ID {duplicate} is used by more than one entity.");
        }

        foreach (int id in allIds.Where(id => id <= 0 || id >= state.NextEntityId).OrderBy(id => id))
        {
            errors.Add($"Entity ID {id} is outside the allocated range 1..{state.NextEntityId - 1}.");
        }
    }

    private static void ValidateEmpires(ContentSet content, GameState state, List<string> errors)
    {
        var humans = state.Empires.Where(e => e.Controller == ControllerKind.Human).ToList();
        if (humans.Count == 0)
        {
            errors.Add("The game must have at least one human-controlled empire.");
        }
        else if (humans.All(e => e.Ready))
        {
            errors.Add("Every human-controlled empire is ready, but the turn has not resolved.");
        }

        foreach (EmpireState empire in state.Empires)
        {
            if (!Enum.IsDefined(empire.Controller))
            {
                errors.Add($"{empire.Id}: controller kind {(int)empire.Controller} is not defined.");
            }

            if (empire.ResearchReserve < 0)
            {
                errors.Add($"{empire.Id}: research reserve {empire.ResearchReserve} is negative.");
            }

            CheckContentIdSet($"{empire.Id}: known technologies", empire.KnownTechnologyIds, content.Technologies.ContainsKey, errors);

            if (empire.ResearchTargetId is { } target)
            {
                if (!content.Technologies.ContainsKey(target))
                {
                    errors.Add($"{empire.Id}: research target '{target}' is not defined.");
                }
                else if (empire.KnownTechnologyIds.Contains(target))
                {
                    errors.Add($"{empire.Id}: research target '{target}' is already known.");
                }
            }
        }
    }

    private static void ValidatePlanets(ContentSet content, GameState state, List<string> errors)
    {
        foreach (StarState star in state.Stars.Where(s => s.Name.Length == 0))
        {
            errors.Add($"{star.Id}: name is empty.");
        }

        foreach (var group in state.Planets.GroupBy(p => (p.StarId, p.Orbit)).Where(g => g.Count() > 1).OrderBy(g => g.Key.StarId).ThenBy(g => g.Key.Orbit))
        {
            errors.Add($"{group.Key.StarId} has more than one planet in orbit {group.Key.Orbit}.");
        }

        foreach (PlanetState planet in state.Planets)
        {
            if (state.FindStar(planet.StarId) is null)
            {
                errors.Add($"{planet.Id}: star {planet.StarId} does not exist.");
            }

            if (planet.Orbit < 1)
            {
                errors.Add($"{planet.Id}: orbit {planet.Orbit} is below 1.");
            }

            if (!content.PlanetTypes.ContainsKey(planet.PlanetTypeId))
            {
                errors.Add($"{planet.Id}: planet type '{planet.PlanetTypeId}' is not defined.");
            }
        }
    }

    private static void ValidateColonies(ContentSet content, GameState state, List<string> errors)
    {
        foreach (var group in state.Colonies.GroupBy(c => c.PlanetId).Where(g => g.Count() > 1).OrderBy(g => g.Key))
        {
            errors.Add($"{group.Key} has more than one colony.");
        }

        foreach (ColonyState colony in state.Colonies)
        {
            string name = colony.Id.ToString();

            if (state.FindPlanet(colony.PlanetId) is null)
            {
                errors.Add($"{name}: planet {colony.PlanetId} does not exist.");
            }

            if (state.FindEmpire(colony.EmpireId) is null)
            {
                errors.Add($"{name}: owning empire {colony.EmpireId} does not exist.");
            }

            if (colony.Population < 1)
            {
                errors.Add($"{name}: population {colony.Population} is below 1.");
            }

            Workforce workforce = colony.Workforce;
            if (workforce.HasNegativeCount)
            {
                errors.Add($"{name}: workforce has a negative count.");
            }
            else if ((long)workforce.Support + workforce.Production + workforce.Research != colony.Population)
            {
                errors.Add($"{name}: workforce does not sum to population {colony.Population}.");
            }

            if (colony.GrowthProgress is < 0 or > 99)
            {
                errors.Add($"{name}: growth progress {colony.GrowthProgress} is outside 0..99.");
            }

            if (colony.ProductionReserve < 0)
            {
                errors.Add($"{name}: production reserve {colony.ProductionReserve} is negative.");
            }

            bool buildingsValid = CheckContentIdSet($"{name}: completed buildings", colony.CompletedBuildingIds, content.Buildings.ContainsKey, errors);

            if (colony.ProjectId is { } project)
            {
                if (!content.Buildings.ContainsKey(project))
                {
                    errors.Add($"{name}: project '{project}' is not defined.");
                }
                else if (colony.CompletedBuildingIds.Contains(project))
                {
                    errors.Add($"{name}: project '{project}' is already completed.");
                }
            }

            if (buildingsValid)
            {
                int capacity;
                try
                {
                    capacity = Rules.ColonyCapacity(content, colony);
                }
                catch (OverflowException)
                {
                    errors.Add($"{name}: population capacity does not fit a 32-bit integer.");
                    continue;
                }

                if (colony.Population > capacity)
                {
                    errors.Add($"{name}: population {colony.Population} exceeds capacity {capacity}.");
                }
                else if (colony.Population == capacity && colony.GrowthProgress != 0)
                {
                    errors.Add($"{name}: growth progress must be 0 at capacity.");
                }
            }
        }
    }

    /// <summary>Checks a list of content IDs is defined, unique, and sorted ordinally. Returns whether all were defined.</summary>
    private static bool CheckContentIdSet(string label, List<string> ids, Func<string, bool> isDefined, List<string> errors)
    {
        bool allDefined = true;
        foreach (string id in ids.Where(id => !isDefined(id)))
        {
            errors.Add($"{label}: '{id}' is not defined.");
            allDefined = false;
        }

        for (int i = 1; i < ids.Count; i++)
        {
            if (string.CompareOrdinal(ids[i - 1], ids[i]) >= 0)
            {
                errors.Add($"{label}: IDs must be unique and sorted ordinally.");
                break;
            }
        }

        return allDefined;
    }

    private static void CheckSortedUnique(string label, List<int> ids, List<string> errors)
    {
        for (int i = 1; i < ids.Count; i++)
        {
            if (ids[i - 1] >= ids[i])
            {
                errors.Add($"{label}: IDs must be unique and sorted ascending.");
                return;
            }
        }
    }
}
