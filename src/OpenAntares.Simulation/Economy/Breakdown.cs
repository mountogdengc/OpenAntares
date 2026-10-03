using System;
using System.Collections.Immutable;

namespace OpenAntares.Simulation.Economy;

/// <summary>
/// A calculated value with the contributions that explain it. <see cref="Total"/> always equals the
/// sum of the line amounts, because a quantity can only be built from its lines.
/// </summary>
public sealed record Quantity
{
    private Quantity(long total, ImmutableArray<BreakdownLine> lines)
    {
        Total = total;
        Lines = lines;
    }

    public long Total { get; }
    public ImmutableArray<BreakdownLine> Lines { get; }

    public static Quantity Zero { get; } = new(0, ImmutableArray<BreakdownLine>.Empty);

    public static Quantity From(params BreakdownLine[] lines) => From(lines.ToImmutableArray());

    public static Quantity From(ImmutableArray<BreakdownLine> lines)
    {
        long total = 0;
        foreach (BreakdownLine line in lines)
        {
            total = checked(total + line.Amount);
        }

        return new Quantity(total, lines);
    }
}

/// <summary>
/// One contribution to a <see cref="Quantity"/>. <see cref="Kind"/> is one of <see cref="BreakdownKinds"/>.
/// <see cref="SourceId"/> names the responsible planet type, building, technology, or colony, or is
/// empty for rule-level lines. Presentation localizes kinds and IDs; it never recomputes amounts.
/// </summary>
public sealed record BreakdownLine(string Kind, string SourceId, long? Count, long Amount, DivisionDetails? Division = null);

/// <summary>
/// Explains a floored division: <c>Numerator = Quotient * Divisor + Remainder</c>, with
/// <c>0 &lt;= Remainder &lt; Divisor</c>. The remainder is the fraction of one stored unit lost to
/// flooring. It is explanatory only and never contributes another amount.
/// </summary>
public sealed record DivisionDetails(Int128 Numerator, long Divisor, long Quotient, long Remainder)
{
    public static DivisionDetails Divide(Int128 numerator, long divisor)
    {
        if (divisor <= 0 || numerator < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(divisor), "Breakdown divisions use a nonnegative numerator and positive divisor.");
        }

        Int128 quotient = numerator / divisor;
        return new DivisionDetails(numerator, divisor, checked((long)quotient), checked((long)(numerator - quotient * divisor)));
    }
}

/// <summary>Stable breakdown line kinds.</summary>
public static class BreakdownKinds
{
    /// <summary>Assigned workers times the planet type's per-worker rate.</summary>
    public const string Workers = "workers";

    /// <summary>A completed building's flat bonus.</summary>
    public const string Building = "building";

    /// <summary>Population times the support each unit requires.</summary>
    public const string Population = "population";

    /// <summary>Output lost to a support shortage, including rounding. Growth lines of this kind pause growth.</summary>
    public const string Shortage = "shortage";

    public const string GrowthBase = "growth_base";
    public const string GrowthSurplus = "growth_surplus";

    /// <summary>The part of a bonus above its rule maximum.</summary>
    public const string Cap = "cap";

    /// <summary>Growth that cannot be used because the colony is at population capacity.</summary>
    public const string CapacityLimit = "capacity_limit";

    /// <summary>One colony's contribution to an empire total. The source is the colony ID.</summary>
    public const string Colony = "colony";

    /// <summary>Output added to a reserve this turn.</summary>
    public const string Earned = "earned";

    /// <summary>A completed target's cost, paid from a reserve.</summary>
    public const string CostPaid = "cost_paid";
}
