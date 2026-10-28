using System.Collections.Immutable;

namespace OpenAntares.Simulation.Fleets;

public sealed record FleetShipView(VesselId Id, string RoleId, StarId? Station, State.TransitState? Transit, StarId? PendingMove, FleetId? Fleet, RequestId? Request, bool NewlyCompleted, Scouting.AmebaApproach? ApproachAcceptance=null);
public sealed record FleetView(FleetId Id, string Name, ImmutableArray<VesselId> Members, VesselId Flagship, OfficerId? Officer, FleetOrder Order, FleetTransitProvenance? Cohort, string? Attention);
public sealed record AssemblySlotView(int Ordinal, string RoleId, ColonyId? Colony, int? QueueToken, VesselId? Vessel, SlotStatus Status, SlotStatus? ResumeStatus, bool NewHull, string? BlockedReason);
public sealed record AssemblyRequestView(RequestId Id, long Revision, string Name, StarId Rally, FleetId? RallyFleet, bool ReservedFleetSlot, RequestPriority Priority, RequestStatus Status, ImmutableArray<ColonyId> Colonies, ImmutableArray<ColonyId> PausedContributions, ImmutableArray<AssemblySlotView> Slots, bool AuthorizeCargo, OfficerId? Coordinator, string? Attention);
public sealed record OfficerView(OfficerId Id, string Name, string Pronouns, string Personality, string ProfileId, string CultureId, CommandEmbodiment Embodiment, OfficerSpecialty Specialty, OfficerDoctrine Doctrine, OfficerStatus Status, int Xp, int Rank, ColonyId? SourceBody, ColonyId? PostedColony, VesselId? Flagship, RequestId? Request, ColonyId? RecoveryColony, int? AvailableAtTurn, OfficerCareerTotals Career, ImmutableArray<OfficerHighlight> Highlights, ImmutableArray<OfficerTurnDetail> Details, string? FinalSummary);
public sealed record OfficerCandidateView(string ProfileId, string Name, string Pronouns, OfficerSpecialty Specialty, string Personality);
public sealed record FleetEmpireView(EmpireId Owner, int Turn, ImmutableArray<FleetShipView> Ships, ImmutableArray<FleetView> Fleets, ImmutableArray<AssemblyRequestView> Requests, ImmutableArray<OfficerView> Officers, ImmutableArray<OfficerCandidateView> Candidates, ImmutableArray<FleetEvent> Events);
public sealed record FleetRouteConsequence(VesselId Vessel,FleetId? Fleet,StarId Origin,StarId Destination,int Turns);
public sealed record FleetActionPreview(bool Allowed, string? Rejection, string Consequences, string Fingerprint)
{
    public ImmutableArray<FleetRouteConsequence> Routes { get; init; }=ImmutableArray<FleetRouteConsequence>.Empty;
    public ImmutableArray<VesselId> Detachments { get; init; }=ImmutableArray<VesselId>.Empty;
}
