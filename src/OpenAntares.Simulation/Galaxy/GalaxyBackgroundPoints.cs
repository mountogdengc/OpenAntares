using System;
using System.Collections.Generic;
using OpenAntares.Simulation.Random;

namespace OpenAntares.Simulation.Galaxy;

/// <summary>Reproducible presentation-only points; never consumes or changes game-state random.</summary>
public static class GalaxyBackgroundPoints
{
    private const ulong BackgroundStream = 0x47616C6178794267UL;

    public static IReadOnlyList<(int X, int Y)> Generate(ulong seed, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Galaxy dimensions must be positive.");
        }

        var random = Pcg32.FromSeed(seed, BackgroundStream);
        int rotation = SpiralShape.Rotation(seed);
        var points = new List<(int X, int Y)>(1200);
        Add(GalaxyRegions.Core, 180);
        Add(GalaxyRegions.Arm, 850);
        Add(GalaxyRegions.Rim, 170);
        return points;

        void Add(string regionId, int count)
        {
            for (int index = 0; index < count; index++)
            {
                var sample = SpiralShape.SampleBackground(random, regionId, rotation);
                points.Add(SpiralShape.ToMap(sample.X, sample.Y, width, height));
            }
        }
    }
}
