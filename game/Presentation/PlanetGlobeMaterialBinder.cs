using Godot;

namespace OpenAntares.Game.Presentation;

/// <summary>Single uniform contract for canvas and all spatial layers.</summary>
public static class PlanetGlobeMaterialBinder
{
    public static ShaderMaterial Create(string shader) => new() { Shader = GD.Load<Shader>(shader) };
    public static void Bind(ShaderMaterial material, PlanetVisual visual)
    {
        material.SetShaderParameter("terrain_seed", visual.TerrainSeed);
        material.SetShaderParameter("cloud_seed", visual.CloudSeed);
        material.SetShaderParameter("city_seed", visual.CitySeed);
        material.SetShaderParameter("surface_mode", (int)visual.SurfaceMode);
        material.SetShaderParameter("structural_variant", visual.Variant);
        material.SetShaderParameter("deep_water", visual.DeepWater);
        material.SetShaderParameter("shallow_water", visual.ShallowWater);
        material.SetShaderParameter("lowland", visual.Lowland);
        material.SetShaderParameter("highland", visual.Highland);
        material.SetShaderParameter("polar_color", visual.Polar);
        material.SetShaderParameter("cloud_color", visual.CloudColor);
        material.SetShaderParameter("atmosphere_color", visual.AtmosphereColor);
        material.SetShaderParameter("city_color", visual.CityColor);
        material.SetShaderParameter("water_enabled", visual.WaterEnabled);
        material.SetShaderParameter("all_water", visual.AllWater);
        material.SetShaderParameter("sea_threshold", visual.SeaThreshold);
        material.SetShaderParameter("ice_cap", visual.IceCap);
        material.SetShaderParameter("cloud_cover", visual.CloudCover);
        material.SetShaderParameter("atmosphere_density", visual.AtmosphereDensity);
        material.SetShaderParameter("spin_cycles", visual.SpinCycles);
        material.SetShaderParameter("cloud_cycles", visual.CloudCycles);
        material.SetShaderParameter("axial_tilt", visual.Tilt);
        material.SetShaderParameter("rotation_phase", visual.Phase);
        material.SetShaderParameter("city_level", visual.CityLevel);
        material.SetShaderParameter("city_threshold_sparse", visual.CityThresholdSparse);
        material.SetShaderParameter("city_threshold_dense", visual.CityThresholdDense);
        material.SetShaderParameter("sampling_grid", (float)visual.SamplingGrid);
    }
}
