using OpenAntares.Simulation.Crises;
namespace OpenAntares.Simulation.State;

public sealed record TransitState(StarId Origin, StarId Destination, int TotalTurns, int RemainingTurns);
public sealed class VesselState
{
    public VesselCrisisState? Crisis { get; set; }
    public VesselId Id { get; set; }
    public EmpireId EmpireId { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public StarId? StationaryStarId { get; set; }
    public TransitState? Transit { get; set; }
    public StarId? PendingMove { get; set; }
    public PlanetId? PendingFounding { get; set; }
    public int CarriedPopulation { get; set; }
    public VesselState Clone() { var clone=(VesselState)MemberwiseClone();clone.Crisis=Crisis?.Clone();return clone; }
}
