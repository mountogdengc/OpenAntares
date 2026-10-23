namespace OpenAntares.Simulation.State;

public sealed record ProductionTarget(string Kind, string ContentId);
public static class ProductionKinds
{
    public const string Building = "building";
    public const string Vessel = "vessel";
}
