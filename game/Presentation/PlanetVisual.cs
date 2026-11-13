using Godot;

namespace OpenAntares.Game.Presentation;

public enum PlanetVisualKnowledge { Unknown, LegacyPlaceholder, Known }
public enum PlanetSurfaceMode { Terrestrial, Barren, Banded }

/// <summary>Reconstructed presentation data. Never serialized into a campaign.</summary>
public sealed record PlanetVisual(
    PlanetVisualKnowledge Knowledge, ulong SourceSeed, int Variant,
    Vector2I TerrainSeed, Vector2I CloudSeed, Vector2I CitySeed,
    PlanetSurfaceMode SurfaceMode, Color DeepWater, Color ShallowWater,
    Color Lowland, Color Highland, Color Polar, Color CloudColor,
    Color AtmosphereColor, Color CityColor, bool WaterEnabled, bool AllWater,
    float SeaThreshold, float IceCap, float CloudCover, float AtmosphereDensity,
    int SpinCycles, int CloudCycles, float Tilt, float Phase, float CityLevel,
    float CityThresholdSparse, float CityThresholdDense, int SamplingGrid);
