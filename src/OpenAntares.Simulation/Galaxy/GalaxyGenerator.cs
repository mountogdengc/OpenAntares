using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Random;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Galaxy;

/// <summary>Choices made on the new-game screen.</summary>
public sealed record NewGameSettings(string GalaxySizeId, ulong Seed);

/// <summary>The outcome of creating a new game. <see cref="State"/> is set only on success.</summary>
public sealed record NewGameResult(GameState? State, string? Error)
{
    public bool Succeeded => State is not null;
}

/// <summary>
/// Creates a new game: a seeded galaxy of named stars with planets, and one human empire with its
/// starting colony. The same content and settings always produce the same game. All geometry uses
/// whole numbers; the seeded generator is stored in the new state and continues from there.
/// </summary>
public static class GalaxyGenerator
{
    /// <summary>Stream selector for new games, so the game seed alone determines the sequence.</summary>
    public const ulong RandomStream = 0x4F70656E416E7461UL;

    /// <summary>Random positions tried per star before giving up on the spacing rule.</summary>
    public const int PlacementAttemptsPerStar = 1000;

    public static NewGameResult CreateNewGame(ContentSet content, NewGameSettings settings)
    {
        if (!content.GalaxySizes.TryGetValue(settings.GalaxySizeId, out GalaxySizeDefinition? size))
        {
            return Fail($"Galaxy size '{settings.GalaxySizeId}' is not defined.");
        }

        var state = new GameState
        {
            Turn = 0,
            Random = Pcg32.FromSeed(settings.Seed, RandomStream),
            GalaxySeed = settings.Seed,
            GalaxyShapeId = "spiral",
            GalaxyWidth = size.Width,
            GalaxyHeight = size.Height,
        };
        Pcg32 random = state.Random;
        GalaxyRules galaxy = content.Galaxy;

        var empire = new EmpireState { Id = new EmpireId(state.AllocateId()), Controller = ControllerKind.Human };
        state.Empires.Add(empire);

        // Stars: random positions kept at least the minimum distance apart.
        List<(int X, int Y, string RegionId)>? positions = PlaceStars(random, settings.Seed, size, galaxy.MinStarDistance);
        if (positions is null)
        {
            return Fail($"Could not place {size.StarCount} stars at least {galaxy.MinStarDistance} apart on a "
                + $"{size.Width} x {size.Height} map. Reduce star_count or min_star_distance, or enlarge the map.");
        }

        string[] names = ShuffledNames(random, galaxy.StarNames.ToArray(), size.StarCount);
        for (int i = 0; i < positions.Count; i++)
        {
            state.Stars.Add(new StarState
            {
                Id = new StarId(state.AllocateId()),
                Name = names[i],
                X = positions[i].X,
                Y = positions[i].Y,
                RegionId = positions[i].RegionId,
            });
        }

        // Planets: a random count per star, each type picked by generation weight.
        List<PlanetTypeDefinition> weightedTypes = content.PlanetTypes.Values.Where(p => p.GenerationWeight > 0).ToList();
        int totalWeight = weightedTypes.Sum(p => p.GenerationWeight);
        foreach (StarState star in state.Stars)
        {
            int planetCount = random.NextInt(galaxy.MinPlanetsPerStar, galaxy.MaxPlanetsPerStar);
            for (int orbit = 1; orbit <= planetCount; orbit++)
            {
                state.Planets.Add(new PlanetState
                {
                    Id = new PlanetId(state.AllocateId()),
                    StarId = star.Id,
                    Orbit = orbit,
                    PlanetTypeId = PickWeighted(random, weightedTypes, totalWeight).Id,
                });
            }
        }

        // Home: one planet of a random star becomes the starting planet type and gets the colony.
        StarState home = state.Stars[random.NextInt(state.Stars.Count)];
        List<PlanetState> homePlanets = state.Planets.Where(p => p.StarId == home.Id).ToList();
        PlanetState homePlanet = homePlanets[random.NextInt(homePlanets.Count)];
        StartingColonyDefinition start = content.StartingColony;
        homePlanet.PlanetTypeId = start.PlanetTypeId;

        state.Colonies.Add(new ColonyState
        {
            Id = new ColonyId(state.AllocateId()),
            PlanetId = homePlanet.Id,
            EmpireId = empire.Id,
            Population = start.Population,
            Workforce = start.Workforce,
        });

        IReadOnlyList<string> errors = GameStateValidator.Validate(content, state);
        return errors.Count > 0
            ? Fail("Generated an invalid game: " + string.Join("; ", errors))
            : new NewGameResult(state, null);
    }

    private static List<(int X, int Y, string RegionId)>? PlaceStars(
        Pcg32 random, ulong seed, GalaxySizeDefinition size, int minDistance)
    {
        long minDistanceSquared = (long)minDistance * minDistance;
        var positions = new List<(int X, int Y, string RegionId)>(size.StarCount);
        int quota = size.StarCount >= 3 ? Math.Max(1, (3 * size.StarCount + 10) / 20) : 0;
        int rotation = SpiralShape.Rotation(seed);
        foreach (var (regionId, count) in new[]
        {
            (GalaxyRegions.Core, quota),
            (GalaxyRegions.Arm, size.StarCount - 2 * quota),
            (GalaxyRegions.Rim, quota),
        })
        {
            for (int star = 0; star < count; star++)
            {
                bool placed = false;
                for (int attempt = 0; attempt < PlacementAttemptsPerStar && !placed; attempt++)
                {
                    var sample = SpiralShape.Sample(random, regionId, rotation);
                    var (x, y) = SpiralShape.ToMap(sample.X, sample.Y, size.Width, size.Height);
                    if (positions.All(p => DistanceSquared(p.X, p.Y, x, y) >= minDistanceSquared))
                    {
                        positions.Add((x, y, regionId));
                        placed = true;
                    }
                }

                if (!placed)
                {
                    return null;
                }
            }
        }

        return positions;
    }

    /// <summary>The first <paramref name="count"/> names of a Fisher-Yates shuffle.</summary>
    private static string[] ShuffledNames(Pcg32 random, string[] names, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int j = random.NextInt(i, names.Length - 1);
            (names[i], names[j]) = (names[j], names[i]);
        }

        return names[..count];
    }

    private static PlanetTypeDefinition PickWeighted(Pcg32 random, List<PlanetTypeDefinition> types, int totalWeight)
    {
        int roll = random.NextInt(totalWeight);
        foreach (PlanetTypeDefinition type in types)
        {
            if (roll < type.GenerationWeight)
            {
                return type;
            }

            roll -= type.GenerationWeight;
        }

        return types[^1];
    }

    private static long DistanceSquared(int x1, int y1, int x2, int y2)
    {
        long dx = (long)x1 - x2;
        long dy = (long)y1 - y2;
        return dx * dx + dy * dy;
    }

    private static NewGameResult Fail(string error) => new(null, error);
}
