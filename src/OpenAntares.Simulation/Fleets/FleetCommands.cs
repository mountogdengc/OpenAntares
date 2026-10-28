using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
namespace OpenAntares.Simulation.Fleets;

public sealed record FleetCommand(EmpireId Issuer, int ExpectedTurn, FleetAction Action) : Command(Issuer, ExpectedTurn)
{
    public string? Fingerprint {get;init;}
}
public abstract record FleetAction
{
    public sealed record Create(string Name, ImmutableArray<VesselId> Members, VesselId? Flagship = null) : FleetAction;
    public sealed record Rename(FleetId Fleet, string Name) : FleetAction;
    public sealed record Disband(FleetId Fleet) : FleetAction;
    public sealed record Merge(FleetId Target, ImmutableArray<FleetId> Sources) : FleetAction;
    public sealed record Split(FleetId Source, string Name, ImmutableArray<VesselId> Members, VesselId? Flagship = null) : FleetAction;
    public sealed record SetFlagship(FleetId Fleet, VesselId Flagship) : FleetAction;
    public sealed record SetOrder(ImmutableArray<FleetId> Fleets, FleetOrder Order) : FleetAction;
    public sealed record MoveSelection(ImmutableArray<VesselId> Vessels, ImmutableArray<FleetId> Fleets, StarId? Destination) : FleetAction;
    public sealed record DetachSelection(ImmutableArray<VesselId> Vessels) : FleetAction;
    public sealed record DismissNewMarker(ImmutableArray<VesselId> Vessels) : FleetAction;
}
