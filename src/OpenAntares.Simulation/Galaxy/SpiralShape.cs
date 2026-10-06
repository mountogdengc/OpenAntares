using System;
using OpenAntares.Simulation.Random;

namespace OpenAntares.Simulation.Galaxy;

/// <summary>Integer-only samples from a three-arm spiral in a 1024-unit square.</summary>
public static class SpiralShape
{
    private static readonly int[] QuarterSine =
        [0, 100, 200, 297, 392, 483, 569, 650, 724, 792, 851, 903, 946, 981, 1004, 1019, 1024];

    public static int Rotation(ulong seed) => (int)((seed ^ (seed >> 32)) & 63UL);

    public static (int X, int Y) Sample(Pcg32 random, string regionId) => Sample(random, regionId, 0);

    public static (int X, int Y) Sample(Pcg32 random, string regionId, int rotation)
    {
        int radius;
        int angle;
        int scatterX = 0;
        int scatterY = 0;
        switch (regionId)
        {
            case GalaxyRegions.Core:
                radius = random.NextInt(0, 190);
                angle = random.NextInt(64);
                break;
            case GalaxyRegions.Arm:
                radius = random.NextInt(120, 465);
                int phase = random.NextInt(3) switch { 0 => 0, 1 => 21, _ => 43 };
                angle = phase + radius * 96 / 465 + random.NextInt(-2, 2);
                scatterX = random.NextInt(-28, 28);
                scatterY = random.NextInt(-28, 28);
                break;
            case GalaxyRegions.Rim:
                radius = random.NextInt(380, 495);
                angle = random.NextInt(64);
                break;
            default:
                throw new ArgumentException($"Unknown galaxy region '{regionId}'.", nameof(regionId));
        }

        angle = (angle + rotation) & 63;
        int x = 512 + (int)((long)radius * Sin1024((angle + 16) & 63) / 1024) + scatterX;
        int y = 512 + (int)((long)radius * Sin1024(angle) * 860 / (1024 * 1024)) + scatterY;
        return (Math.Clamp(x, 8, 1016), Math.Clamp(y, 8, 1016));
    }

    public static (int X, int Y) ToMap(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Galaxy dimensions must be positive.");
        }
        return ((int)((long)x * (width - 1) / 1024), (int)((long)y * (height - 1) / 1024));
    }

    private static int Sin1024(int angle)
    {
        angle &= 63;
        if (angle <= 16) return QuarterSine[angle];
        if (angle <= 32) return QuarterSine[32 - angle];
        if (angle <= 48) return -QuarterSine[angle - 32];
        return -QuarterSine[64 - angle];
    }
}
