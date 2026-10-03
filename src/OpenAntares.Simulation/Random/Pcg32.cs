using System;

namespace OpenAntares.Simulation.Random;

/// <summary>
/// The simulation's pseudo-random number generator: PCG32 (XSH RR, 64-bit state), as published at
/// pcg-random.org. The algorithm is fixed here so results never depend on the .NET version or
/// platform. Its state is part of <see cref="State.GameState"/> and is saved with the game.
/// </summary>
public sealed class Pcg32
{
    private const ulong Multiplier = 6364136223846793005UL;

    /// <summary>Internal generator state.</summary>
    public ulong State { get; set; }

    /// <summary>Stream selector. Always odd.</summary>
    public ulong Increment { get; set; }

    /// <summary>Seeds a generator the same way as the reference <c>pcg32_srandom_r</c>.</summary>
    public static Pcg32 FromSeed(ulong seed, ulong sequence)
    {
        var rng = new Pcg32 { State = 0, Increment = unchecked((sequence << 1) | 1UL) };
        rng.NextUInt32();
        rng.State = unchecked(rng.State + seed);
        rng.NextUInt32();
        return rng;
    }

    public uint NextUInt32()
    {
        unchecked
        {
            ulong old = State;
            State = old * Multiplier + Increment;
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rotation = (int)(old >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }
    }

    /// <summary>Returns a uniformly distributed integer in [0, <paramref name="exclusiveMax"/>), without modulo bias.</summary>
    public int NextInt(int exclusiveMax)
    {
        if (exclusiveMax <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax), "Upper bound must be positive.");
        }

        uint bound = (uint)exclusiveMax;
        // Reject the few low values that would make some results more likely than others.
        uint threshold = unchecked(0u - bound) % bound;
        while (true)
        {
            uint value = NextUInt32();
            if (value >= threshold)
            {
                return (int)(value % bound);
            }
        }
    }

    /// <summary>Returns a uniformly distributed integer in [<paramref name="min"/>, <paramref name="max"/>].</summary>
    public int NextInt(int min, int max)
    {
        if (max < min)
        {
            throw new ArgumentOutOfRangeException(nameof(max), "Upper bound must not be below the lower bound.");
        }

        long span = (long)max - min + 1;
        if (span > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(max), "Range is too large.");
        }

        return min + NextInt((int)span);
    }

    public Pcg32 Clone() => new() { State = State, Increment = Increment };
}
