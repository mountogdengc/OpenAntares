using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
namespace OpenAntares.Simulation.Fleets;
public sealed record OfficerCommand(EmpireId Issuer,int ExpectedTurn,OfficerAction Action):Command(Issuer,ExpectedTurn);
public abstract record OfficerAction
{
    public sealed record Commission(string? ProfileId,ColonyId Posting,ColonyId? SourceBody=null,OfficerSpecialty? Specialty=null):OfficerAction;
    public sealed record Dismiss(OfficerId Officer):OfficerAction;
    public sealed record Post(OfficerId Officer,ColonyId Colony):OfficerAction;
    public sealed record AssignFlagship(OfficerId Officer,FleetId Fleet):OfficerAction;
    public sealed record Transfer(OfficerId Officer,VesselId From,VesselId To):OfficerAction;
    public sealed record Remove(OfficerId Officer,ColonyId Colony):OfficerAction;
    public sealed record SetDoctrine(OfficerId Officer,OfficerDoctrine Doctrine):OfficerAction;
    public sealed record ApplyDoctrineToSelection(ImmutableArray<OfficerId> Officers,OfficerDoctrine Doctrine):OfficerAction;
    public sealed record SetFutureDoctrine(OfficerDoctrine? Doctrine):OfficerAction;
}
