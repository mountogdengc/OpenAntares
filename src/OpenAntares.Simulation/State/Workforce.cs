namespace OpenAntares.Simulation.State;

/// <summary>How a colony's population is assigned. Counts are whole workforce units.</summary>
public readonly record struct Workforce(int Support, int Production, int Research)
{
    public bool HasNegativeCount => Support < 0 || Production < 0 || Research < 0;

    /// <summary>Sum of all assignments. Throws <see cref="System.OverflowException"/> if it does not fit.</summary>
    public int Total => Support + Production + Research;
}
