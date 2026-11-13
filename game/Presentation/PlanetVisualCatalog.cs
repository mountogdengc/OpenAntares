using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using Godot;

namespace OpenAntares.Game.Presentation;

public sealed record PlanetGlobePreset(PlanetSurfaceMode Mode, Color Lowland, Color Highland,
    Color Polar, Color Atmosphere, float IceCap, float AtmosphereDensity);
public sealed record PlanetGlobeEnvironment(Color DeepWater, Color ShallowWater, Color Cloud,
    bool WaterEnabled, float SeaThreshold, float CloudCover);

/// <summary>Visual presets live outside ContentSet and saved content snapshots.</summary>
public sealed class PlanetVisualCatalog
{
    private readonly ImmutableDictionary<string, PlanetGlobePreset> _types;
    private readonly ImmutableDictionary<string, PlanetGlobeEnvironment> _environments;
    public float PopulationReference { get; }
    public Color CityColor { get; }
    public float CityThresholdSparse { get; }
    public float CityThresholdDense { get; }
    public int SamplingGrid { get; }

    private PlanetVisualCatalog(JsonElement root)
    {
        if (root.GetProperty("format").GetString() != "OpenAntaresPlanetGlobes" || root.GetProperty("version").GetInt32() != 1)
            throw new ArgumentException("Unsupported globe presentation catalog.");
        PopulationReference = Number(root, "population_reference", 1, 10000);
        CityColor = ColorValue(root, "city_color");
        CityThresholdSparse = Number(root, "city_threshold_sparse", 0, 1);
        CityThresholdDense = Number(root, "city_threshold_dense", 0, CityThresholdSparse);
        SamplingGrid = root.GetProperty("sampling_grid").GetInt32();
        if (SamplingGrid is < 32 or > 512) throw new ArgumentException("Globe sampling grid must be 32..512.");
        var types = ImmutableDictionary.CreateBuilder<string, PlanetGlobePreset>(StringComparer.Ordinal);
        foreach (var entry in root.GetProperty("types").EnumerateObject())
        {
            var item = entry.Value;
            PlanetSurfaceMode mode = item.GetProperty("mode").GetString() switch {
                "terrestrial" => PlanetSurfaceMode.Terrestrial,
                "barren" => PlanetSurfaceMode.Barren,
                "banded" => PlanetSurfaceMode.Banded,
                _ => throw new ArgumentException("Unsupported globe surface mode: " + entry.Name)
            };
            types.Add(entry.Name, new(mode, ColorValue(item, "lowland"), ColorValue(item, "highland"),
                ColorValue(item, "polar"), ColorValue(item, "atmosphere"), Number(item, "ice_cap", 0, 1),
                Number(item, "atmosphere_density", 0, 1)));
        }
        if (!types.ContainsKey("neutral")) throw new ArgumentException("Globe catalog requires a neutral preset.");
        _types = types.ToImmutable();
        var environments = ImmutableDictionary.CreateBuilder<string, PlanetGlobeEnvironment>(StringComparer.Ordinal);
        foreach (var entry in root.GetProperty("environments").EnumerateObject())
        {
            var item = entry.Value;
            environments.Add(entry.Name, new(ColorValue(item, "deep_water"), ColorValue(item, "shallow_water"),
                ColorValue(item, "cloud"), item.GetProperty("water_enabled").GetBoolean(),
                Number(item, "sea_threshold", 0, 0.85f), Number(item, "cloud_cover", 0, 1)));
        }
        if (!environments.ContainsKey("neutral")) throw new ArgumentException("Globe catalog requires a neutral environment.");
        _environments = environments.ToImmutable();
    }

    public static PlanetVisualCatalog Load(string path = "res://art/planets/planet_styles.json") =>
        Parse(Godot.FileAccess.GetFileAsString(path));

    public static PlanetVisualCatalog Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return new(document.RootElement.GetProperty("globes"));
    }

    public bool TryResolve(string type, string? environment, out PlanetGlobePreset preset, out PlanetGlobeEnvironment climate)
    {
        bool valid = _types.ContainsKey(type) && environment is not null && _environments.ContainsKey(environment);
        preset = _types[valid ? type : "neutral"];
        climate = _environments[valid ? environment! : "neutral"];
        return valid;
    }

    private static float Number(JsonElement item, string key, float minimum, float maximum)
    {
        float value = item.GetProperty(key).GetSingle();
        if (!float.IsFinite(value) || value < minimum || value > maximum)
            throw new ArgumentException("Globe setting outside its range: " + key);
        return value;
    }

    private static Color ColorValue(JsonElement item, string key)
    {
        string? value = item.GetProperty(key).GetString();
        if (value is null || !Regex.IsMatch(value, "^[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Globe color requires six hexadecimal digits: " + key);
        return Color.FromHtml(value);
    }
}
