using System.Collections.Generic;
namespace OpenAntares.Simulation.State;

public sealed record PlanetObservation(PlanetId PlanetId, int Orbit, string PlanetTypeId, ColonyId? ColonyId, EmpireId? OwnerId)
{
    public string? EnvironmentId { get; init; }
}
public sealed class SystemObservation
{
    public StarId StarId { get; set; }
    public int LastObservedTurn { get; set; }
    public List<PlanetObservation> Planets { get; set; } = new();
    public SystemObservation Clone() => new() { StarId = StarId, LastObservedTurn = LastObservedTurn, Planets = new(Planets) };
}
