using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Random;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Galaxy;

/// <summary>Choices made on the new-game screen.</summary>
public sealed record NewGameSettings(string GalaxySizeId, ulong Seed, int RulesVersion = Rules.CurrentVersion, string? SpeciesId = null,
    int RivalCount = 0, string? RivalSpeciesId = null, bool CrisesEnabled = true, bool? AmebasEnabled = null, bool? SentriesEnabled = null);

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
        if(Rules.HasDiplomacy(settings.RulesVersion)&&content.Diplomacy is null)return Fail("Diplomacy campaigns require explicit diplomacy content, including Solo.");
        if(Rules.HasShipDesigns(settings.RulesVersion)&&content.ShipDesigns is null)return Fail("Ship-design campaigns require explicit content, including Off.");
        if(Rules.HasCombat(settings.RulesVersion))
        {
            if(settings.SentriesEnabled is null||content.Combat is null)return Fail("Combat campaigns require explicit Sentries Standard / Off and combat content.");
        }
        else if(settings.SentriesEnabled is not null)return Fail("Old campaign rules cannot select Sentries settings.");
        if (Rules.HasFleets(settings.RulesVersion) && (content.FleetCommand is null || content.Species.Values.Any(sp => sp.CommandEmbodiment is null || sp.CommandCultureId is null)))
            return Fail("Fleet campaigns require explicit command content and species command identities.");
        if (Rules.HasScouting(settings.RulesVersion))
        {
            if(settings.AmebasEnabled is null||content.SpaceAmeba is null)return Fail("Scout campaigns require an explicit ameba choice and hazard content, including Off.");
        }
        else if(settings.AmebasEnabled is not null)return Fail("Old campaign rules cannot select ameba settings.");
        if (Rules.HasCrises(settings.RulesVersion) && content.Plagues.Count != 1) return Fail("Crisis campaigns require exactly one plague family, including Off.");
        if (Rules.HasDevelopment(settings.RulesVersion) && content.GovernorTemplates.IsEmpty) return Fail("Development campaigns require explicit governor defaults.");
        if (!Rules.IsSupported(settings.RulesVersion)) return Fail("Unsupported rules version.");
        if (settings.RivalCount is not (0 or 1)) return Fail("Choose either Solo or one rival.");
        if (settings.RivalCount == 0 && settings.RivalSpeciesId is not null) return Fail("Solo games cannot select a rival species.");
        if (!Rules.HasRivals(settings.RulesVersion) && (settings.RivalCount != 0 || settings.RivalSpeciesId is not null))
            return Fail("Old campaign rules cannot start active rivals.");
        if (Rules.HasRivals(settings.RulesVersion) && content.RivalAi is not { AlgorithmId: "expansion_v1", GrowthSupportWorkers: 0 or 1, ResurveyMinAge: > 0 })
            return Fail("Rival campaigns, including Solo, require supported rival policy content.");
        if (settings.RivalCount == 1 && (settings.RivalSpeciesId is not { } rivalSpecies || !content.Species.ContainsKey(rivalSpecies)))
            return Fail("Select a defined rival species.");
        if (Rules.HasExpansion(settings.RulesVersion) && content.Expansion is null) return Fail("Expansion games require expansion content.");
        if (!content.GalaxySizes.TryGetValue(settings.GalaxySizeId, out GalaxySizeDefinition? size))
        {
            return Fail($"Galaxy size '{settings.GalaxySizeId}' is not defined.");
        }

        SpeciesDefinition? species = null;
        if (Rules.HasSpecies(settings.RulesVersion) && (settings.SpeciesId is not { } speciesId || !content.Species.TryGetValue(speciesId, out species)))
            return Fail("A defined species must be selected for a species campaign.");

        var state = new GameState
        {
            RulesVersion = settings.RulesVersion,
            Turn = 0,
            Random = Pcg32.FromSeed(settings.Seed, RandomStream),
            GalaxySeed = settings.Seed,
            GalaxyShapeId = "spiral",
            GalaxyWidth = size.Width,
            GalaxyHeight = size.Height,
        };
        Pcg32 random = state.Random;
        GalaxyRules galaxy = content.Galaxy;

        var empire = new EmpireState { Id = new EmpireId(state.AllocateId()), Controller = ControllerKind.Human, SpeciesId = species?.Id };
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
        var coreTypes = WeightedTypes(content, GalaxyRegions.Core);
        var armTypes = WeightedTypes(content, GalaxyRegions.Arm);
        var rimTypes = WeightedTypes(content, GalaxyRegions.Rim);
        foreach (StarState star in state.Stars)
        {
            var (weightedTypes, totalWeight) = star.RegionId switch
            {
                GalaxyRegions.Core => coreTypes,
                GalaxyRegions.Rim => rimTypes,
                _ => armTypes,
            };
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

        if (species is not null)
        {
            EnvironmentGenerator.Apply(content,state,homePlanet.Id);
            var colony = state.Colonies[0];
            colony.Population = species.StartingPopulation;
            colony.Workforce = species.StartingWorkforce;
            colony.HabitatMode = Species.HabitatModes.Native;
        }
        if (Rules.HasExpansion(settings.RulesVersion))
        {
            state.Vessels.Add(new VesselState { Id = new(state.AllocateId()), EmpireId = empire.Id, RoleId = content.Expansion!.StartingVesselId, StationaryStarId = home.Id });
            if (settings.RivalCount == 1 && RivalHomeGenerator.Add(content, state, settings.RivalSpeciesId!) is { } error)
                return Fail(error);
            if (Rules.HasDevelopment(state.RulesVersion))
            {
                foreach (var owner in state.Empires) owner.Development = new();
                foreach (var colony in state.Colonies) colony.Development = new();
                foreach (var planet in state.Planets) planet.Appearance = Appearance.PlanetAppearance.Create(settings.Seed, planet.Id);
            }
            var beforeRefresh = Rules.HasRivals(state.RulesVersion) ? state.Clone() : null;
            Knowledge.ObservationUpdater.Refresh(state, 0);
            if (beforeRefresh is not null) Knowledge.ContactUpdater.Refresh(beforeRefresh, state);
        }

        Crises.CrisisInitializer.Initialize(content,state,settings.CrisesEnabled);
        if(Rules.HasScouting(state.RulesVersion))Scouting.ScoutInitializer.Initialize(content,state,settings.AmebasEnabled!.Value);
        if (Rules.HasFleets(state.RulesVersion)) Fleets.FleetInitializer.Initialize(content, state);
        ShipDesigns.ShipDesignInitializer.Initialize(content,state);
        if(Rules.HasCombat(state.RulesVersion)&&Combat.CombatInitializer.Initialize(content,state,settings.SentriesEnabled!.Value) is {} combatError)return Fail(combatError);
        if(Rules.HasDiplomacy(state.RulesVersion))Diplomacy.DiplomacyInitializer.Initialize(content,state);
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

    private static (List<(PlanetTypeDefinition Type, int Weight)> Types, int Total) WeightedTypes(
        ContentSet content, string regionId)
    {
        List<(PlanetTypeDefinition Type, int Weight)> types = content.PlanetTypes.Values
            .Select(type => (Type: type, Weight: type.WeightForRegion(regionId)))
            .Where(entry => entry.Weight > 0)
            .ToList();
        return (types, types.Sum(entry => entry.Weight));
    }

    private static PlanetTypeDefinition PickWeighted(
        Pcg32 random, List<(PlanetTypeDefinition Type, int Weight)> types, int totalWeight)
    {
        int roll = random.NextInt(totalWeight);
        foreach (var (type, weight) in types)
        {
            if (roll < weight)
            {
                return type;
            }

            roll -= weight;
        }

        return types[^1].Type;
    }

    private static long DistanceSquared(int x1, int y1, int x2, int y2)
    {
        long dx = (long)x1 - x2;
        long dy = (long)y1 - y2;
        return dx * dx + dy * dy;
    }

    private static NewGameResult Fail(string error) => new(null, error);
}
