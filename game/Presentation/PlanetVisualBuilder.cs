using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Presentation;

/// <summary>Reads only published inspection data. It has no GameState or PRNG access.</summary>
public sealed class PlanetVisualBuilder
{
    private readonly PlanetVisualCatalog _catalog;
    public PlanetVisualBuilder(PlanetVisualCatalog catalog) => _catalog = catalog;

    public PlanetVisual Build(StarView star, KnownPlanetView? planet, OwnedColonyView? ownedColony)
    {
        if (planet is not null && (star.Planets is null || !star.Planets.Value.Any(p => p == planet)))
            throw new ArgumentException("Planet must be the supplied star's published observation.");
        if (ownedColony is not null && (planet is null || ownedColony.PlanetId != planet.Id ||
            ownedColony.StarId != star.Chart.Id || planet.OwnerId != ownedColony.EmpireId || planet.ColonyId != ownedColony.Id))
            throw new ArgumentException("Owned colony must match this observed planet, star and owner.");
        var appearance = planet?.Appearance;
        bool supported = _catalog.TryResolve(planet?.PlanetTypeId ?? "neutral", planet?.EnvironmentId, out var preset, out var environment);
        bool known = planet is not null && appearance is { Version: 1, Variant: >= 0 and < 8 } && supported;
        if (!known) _catalog.TryResolve("neutral", "neutral", out preset, out environment);
        ulong seed = known ? appearance!.SurfaceSeed : 0;
        bool airless = preset.Mode == PlanetSurfaceMode.Barren;
        // Fixed independent SplitMix64 salts are part of presentation generator v1.
        return new(planet is null ? PlanetVisualKnowledge.Unknown : known ? PlanetVisualKnowledge.Known : PlanetVisualKnowledge.LegacyPlaceholder,
            seed, known ? appearance!.Variant : 0,
            PackSeed(Derive(seed, 0x5445525241494e31UL)), PackSeed(Derive(seed, 0x434c4f5544533031UL)), PackSeed(Derive(seed, 0x4349544945533031UL)),
            preset.Mode, environment.DeepWater, environment.ShallowWater, preset.Lowland, preset.Highland,
            preset.Polar, environment.Cloud, preset.Atmosphere, _catalog.CityColor,
            known && !airless && environment.WaterEnabled, false,
            environment.SeaThreshold, preset.IceCap, known && !airless ? environment.CloudCover : 0,
            known && !airless ? preset.AtmosphereDensity : 0,
            32 + (int)(Derive(seed, 0x5350494e30303031UL) % 33),
            67 + (int)(Derive(seed, 0x434c4f5544524154UL) % 19),
            known ? ((int)((seed >> 12) & 255) - 127) / 127f * 0.32f : 0,
            known ? (seed & 65535) / 65536f : 0,
            known && star.Current && ownedColony is not null ? CityLevel(ownedColony.Population, _catalog.PopulationReference) : 0,
            _catalog.CityThresholdSparse, _catalog.CityThresholdDense, _catalog.SamplingGrid);
    }

    public static float CityLevel(int population) => CityLevel(population, 20);
    private static float CityLevel(int population, float reference) => MathF.Sqrt(Math.Clamp(population / reference, 0, 1));
    public static Vector2I PackSeed(ulong seed) => new(unchecked((int)(uint)seed), unchecked((int)(uint)(seed >> 32)));
    private static ulong Derive(ulong seed, ulong salt)
    {
        unchecked
        {
            ulong value = seed ^ salt;
            value = (value ^ (value >> 30)) * 0xbf58476d1ce4e5b9UL;
            value = (value ^ (value >> 27)) * 0x94d049bb133111ebUL;
            return value ^ (value >> 31);
        }
    }
}
