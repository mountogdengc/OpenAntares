using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Random;

namespace OpenAntares.Simulation.Fleets;

public readonly record struct FleetId(int Value) : IComparable<FleetId>
{
    public int CompareTo(FleetId other) => Value.CompareTo(other.Value);
}
public readonly record struct RequestId(int Value) : IComparable<RequestId>
{
    public int CompareTo(RequestId other) => Value.CompareTo(other.Value);
}
public readonly record struct OfficerId(int Value) : IComparable<OfficerId>
{
    public int CompareTo(OfficerId other) => Value.CompareTo(other.Value);
}
public enum FleetOrderKind { Hold, Assemble, Explore, Return }
public enum RequestPriority { Normal, Urgent }
public enum RequestStatus { Active, Paused, SafetyPaused, Complete, Cancelled }
public enum SlotStatus { Queued, Built, Travelling, Arrived, Missing, BlockedSafety }
public enum OfficerStatus { Commissioning, Posted, Commanding, Coordinating, Recovering, Missing, Dead, Retired }
public enum FleetImportance { Summary, Attention }
public sealed record FleetOrder(FleetOrderKind Kind, StarId? Destination = null);
public sealed record FleetTransitProvenance(StarId Origin, StarId Destination, int TotalTurns, int LaunchTurn, int EffectiveSpeed);
public sealed record HullBuildOrigin(RequestId Request, int Slot, ColonyId Colony, int QueueToken);
public sealed record FleetEvent(EmpireId Owner, long Id, int Turn, string Kind, string Reason, FleetImportance Importance = FleetImportance.Summary, FleetId? Fleet = null, RequestId? Request = null, OfficerId? Officer = null, VesselId? Vessel = null);
public sealed record OfficerHighlight(long Event, int Turn, string Kind, string Reason);
public sealed record OfficerTurnDetail(int Turn, ImmutableArray<OfficerHighlight> Entries);
public sealed record OfficerCareerTotals(long FirstSurveys = 0, long Studies = 0, long NewHulls = 0, long ShipsLost = 0, long FlagshipsLost = 0, long Recoveries = 0, long XpEarned = 0);

public sealed class FleetRecord
{
    public FleetId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<VesselId> Members { get; set; } = new();
    public VesselId Flagship { get; set; }
    public OfficerId? Officer { get; set; }
    public FleetOrder Order { get; set; } = new(FleetOrderKind.Hold);
    public FleetTransitProvenance? Cohort { get; set; }
    public string? Attention { get; set; }
    public FleetRecord Clone() { var c = (FleetRecord)MemberwiseClone(); c.Members = new(Members); return c; }
}

public sealed class AssemblySlot
{
    public int Ordinal { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public ColonyId? Colony { get; set; }
    public int? QueueToken { get; set; }
    public VesselId? Vessel { get; set; }
    public VesselId? FormerVessel { get; set; }
    public SlotStatus Status { get; set; }
    public SlotStatus? ResumeStatus { get; set; }
    public bool NewHull { get; set; }
    public string? BlockedReason { get; set; }
    public AssemblySlot Clone() => (AssemblySlot)MemberwiseClone();
}

public sealed class AssemblyRequest
{
    public int NextSlotOrdinal { get; set; } = 1;
    public RequestId Id { get; set; }
    public long Revision { get; set; } = 1;
    public string Name { get; set; } = string.Empty;
    public StarId Rally { get; set; }
    public FleetId? RallyFleet { get; set; }
    public bool ReservedFleetSlot { get; set; }
    public RequestPriority Priority { get; set; }
    public RequestStatus Status { get; set; }
    public List<ColonyId> Colonies { get; set; } = new();
    public List<ColonyId> PausedContributions { get; set; } = new();
    public List<AssemblySlot> Slots { get; set; } = new();
    public bool AuthorizeCargo { get; set; }
    public OfficerId? Coordinator { get; set; }
    public OfficerId? InitialXpRecipient { get; set; }
    public bool CoordinatorXpForfeited { get; set; }
    public bool HasAllocated { get; set; }
    public int? CompletedTurn { get; set; }
    public string? Attention { get; set; }
    public AssemblyRequest Clone()
    {
        var c = (AssemblyRequest)MemberwiseClone(); c.Colonies = new(Colonies); c.PausedContributions = new(PausedContributions); c.Slots = Slots.Select(s => s.Clone()).ToList(); return c;
    }
}

public sealed class OfficerRecord
{
    public OfficerId Id { get; set; }
    public string ProfileId { get; set; } = string.Empty;
    public string SpeciesId { get; set; } = string.Empty;
    public string CultureId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Pronouns { get; set; } = string.Empty;
    public string Personality { get; set; } = string.Empty;
    public CommandEmbodiment Embodiment { get; set; }
    public OfficerSpecialty Specialty { get; set; }
    public OfficerDoctrine Doctrine { get; set; }
    public OfficerStatus Status { get; set; }
    public int Xp { get; set; }
    public int Rank { get; set; }
    public ColonyId? SourceBody { get; set; }
    public ColonyId? PostedColony { get; set; }
    public VesselId? Flagship { get; set; }
    public RequestId? Request { get; set; }
    public ColonyId? RecoveryColony { get; set; }
    public int? AvailableAtTurn { get; set; }
    public int? RecoveryStartedTurn { get; set; }
    public int? RecoveryPercentAtLoss { get; set; }
    public string? FinalSummary { get; set; }
    public OfficerCareerTotals Career { get; set; } = new();
    public List<OfficerHighlight> Highlights { get; set; } = new();
    public List<OfficerTurnDetail> Details { get; set; } = new();
    public OfficerRecord Clone() { var c = (OfficerRecord)MemberwiseClone(); c.Highlights = new(Highlights); c.Details = new(Details); return c; }
}

public sealed class FleetEmpireState
{
    public int NextFleetId { get; set; } = 1;
    public int NextRequestId { get; set; } = 1;
    public int NextOfficerId { get; set; } = 1;
    public long NextEventId { get; set; } = 1;
    public int LifetimeCommissions { get; set; }
    public int RecoveryAlgorithmVersion { get; set; } = 1;
    public Pcg32? RecoveryRandom { get; set; }
    public long RecoveryRollCount { get; set; }
    public OfficerDoctrine? FutureDoctrine { get; set; }
    public List<FleetRecord> Fleets { get; set; } = new();
    public List<AssemblyRequest> Requests { get; set; } = new();
    public List<OfficerRecord> Officers { get; set; } = new();
    public List<string> CandidateProfiles { get; set; } = new();
    public SortedDictionary<string, int> ProfileCommissions { get; set; } = new(StringComparer.Ordinal);
    public SortedSet<StarId> SurveyCredits { get; set; } = new();
    public SortedSet<int> StudyCredits { get; set; } = new();
    public SortedSet<VesselId> HullCredits { get; set; } = new();
    public List<FleetEvent> PendingEvents { get; set; } = new();
    public List<FleetEvent> RecentEvents { get; set; } = new();
    public FleetEmpireState Clone()
    {
        var c = (FleetEmpireState)MemberwiseClone();
        c.RecoveryRandom = RecoveryRandom?.Clone();
        c.Fleets = Fleets.Select(f => f.Clone()).ToList(); c.Requests = Requests.Select(r => r.Clone()).ToList(); c.Officers = Officers.Select(o => o.Clone()).ToList();
        c.CandidateProfiles = new(CandidateProfiles); c.ProfileCommissions = new(ProfileCommissions, StringComparer.Ordinal);
        c.SurveyCredits = new(SurveyCredits); c.StudyCredits = new(StudyCredits); c.HullCredits = new(HullCredits);
        c.PendingEvents = new(PendingEvents); c.RecentEvents = new(RecentEvents);
        return c;
    }
}
