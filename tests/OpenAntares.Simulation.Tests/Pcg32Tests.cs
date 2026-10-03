using System;
using System.Linq;
using OpenAntares.Simulation.Random;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public class Pcg32Tests
{
    [Fact]
    public void MatchesReferenceImplementationOutput()
    {
        // pcg32-demo from pcg-random.org, seeded with initstate 42 and initseq 54.
        var rng = Pcg32.FromSeed(42, 54);

        uint[] values = Enumerable.Range(0, 6).Select(_ => rng.NextUInt32()).ToArray();

        Assert.Equal(new uint[] { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e }, values);
    }

    [Fact]
    public void SameSeedGivesSameSequence()
    {
        var first = Pcg32.FromSeed(7, 3);
        var second = Pcg32.FromSeed(7, 3);

        Assert.Equal(
            Enumerable.Range(0, 100).Select(_ => first.NextInt(1000)),
            Enumerable.Range(0, 100).Select(_ => second.NextInt(1000)));
    }

    [Fact]
    public void CloneContinuesTheSameSequenceIndependently()
    {
        var original = Pcg32.FromSeed(99, 1);
        original.NextUInt32();
        Pcg32 copy = original.Clone();

        uint fromOriginal = original.NextUInt32();
        uint fromCopy = copy.NextUInt32();

        Assert.Equal(fromOriginal, fromCopy);
        Assert.Equal(original.State, copy.State);
    }

    [Fact]
    public void NextIntStaysWithinBounds()
    {
        var rng = Pcg32.FromSeed(1, 1);

        for (int i = 0; i < 10_000; i++)
        {
            Assert.InRange(rng.NextInt(7), 0, 6);
            Assert.InRange(rng.NextInt(-3, 3), -3, 3);
        }
    }

    [Fact]
    public void NextIntCoversEveryValue()
    {
        var rng = Pcg32.FromSeed(5, 5);

        int[] seen = Enumerable.Range(0, 1000).Select(_ => rng.NextInt(5)).Distinct().OrderBy(v => v).ToArray();

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, seen);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NextIntRejectsNonPositiveBound(int bound)
    {
        var rng = Pcg32.FromSeed(1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(bound));
    }

    [Fact]
    public void NextIntRejectsInvertedRange()
    {
        var rng = Pcg32.FromSeed(1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(5, 4));
    }
}
