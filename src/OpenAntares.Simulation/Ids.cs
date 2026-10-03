using System;

namespace OpenAntares.Simulation;

/// <summary>Stable identifier of an empire. Allocated from <see cref="State.GameState.NextEntityId"/>.</summary>
public readonly record struct EmpireId(int Value) : IComparable<EmpireId>
{
    public int CompareTo(EmpireId other) => Value.CompareTo(other.Value);
    public override string ToString() => $"empire:{Value}";
}

/// <summary>Stable identifier of a planet.</summary>
public readonly record struct PlanetId(int Value) : IComparable<PlanetId>
{
    public int CompareTo(PlanetId other) => Value.CompareTo(other.Value);
    public override string ToString() => $"planet:{Value}";
}

/// <summary>Stable identifier of a colony.</summary>
public readonly record struct ColonyId(int Value) : IComparable<ColonyId>
{
    public int CompareTo(ColonyId other) => Value.CompareTo(other.Value);
    public override string ToString() => $"colony:{Value}";
}
