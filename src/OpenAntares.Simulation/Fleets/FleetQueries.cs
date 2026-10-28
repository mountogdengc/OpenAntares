using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Fleets;

public static class FleetQueries
{
    public static FleetEmpireView Project(ContentSet content, GameState state, EmpireId owner)
    {
        var e = state.FindEmpire(owner) ?? throw new ArgumentException("Empire not found.", nameof(owner));
        if (!Rules.HasFleets(state.RulesVersion) || e.Fleets is not { } f)
            return new(owner, state.Turn, ImmutableArray<FleetShipView>.Empty, ImmutableArray<FleetView>.Empty, ImmutableArray<AssemblyRequestView>.Empty, ImmutableArray<OfficerView>.Empty, ImmutableArray<OfficerCandidateView>.Empty, ImmutableArray<FleetEvent>.Empty);
        var reservations = f.Requests.Where(r => IsActive(r.Status)).SelectMany(r => r.Slots.Where(s => s.Vessel is not null && s.Status != SlotStatus.Missing).Select(s => (Vessel: s.Vessel!.Value, Request: r.Id))).ToDictionary(x => x.Vessel, x => x.Request);
        var ships = state.Vessels.Where(v => v.EmpireId == owner).OrderBy(v => v.Id).Select(v => new FleetShipView(v.Id, v.RoleId, v.StationaryStarId, v.Transit, v.PendingMove, v.Fleet, reservations.TryGetValue(v.Id, out var request) ? request : null, v.NewlyCompleted)).ToImmutableArray();
        var fleets = f.Fleets.OrderBy(x => x.Id).Select(x => new FleetView(x.Id, x.Name, x.Members.OrderBy(v => v).ToImmutableArray(), x.Flagship, x.Officer, x.Order, x.Cohort, x.Attention)).ToImmutableArray();
        var requests = f.Requests.OrderBy(x => x.Id).Select(x => new AssemblyRequestView(x.Id, x.Revision, x.Name, x.Rally, x.RallyFleet, x.ReservedFleetSlot, x.Priority, x.Status, x.Colonies.ToImmutableArray(), x.PausedContributions.ToImmutableArray(), x.Slots.Select(s => new AssemblySlotView(s.Ordinal, s.RoleId, s.Colony, s.QueueToken, s.Vessel, s.Status, s.ResumeStatus, s.NewHull, s.BlockedReason)).ToImmutableArray(), x.AuthorizeCargo, x.Coordinator, x.Attention)).ToImmutableArray();
        var officers = f.Officers.OrderBy(x => x.Id).Select(x => new OfficerView(x.Id, x.Name, x.Pronouns, x.Personality, x.ProfileId, x.CultureId, x.Embodiment, x.Specialty, x.Doctrine, x.Status, x.Xp, x.Rank, x.SourceBody, x.PostedColony, x.Flagship, x.Request, x.RecoveryColony, x.AvailableAtTurn, x.Career, x.Highlights.ToImmutableArray(), x.Details.ToImmutableArray(), x.FinalSummary)).ToImmutableArray();
        var candidates = f.CandidateProfiles.Select(id => content.FleetCommand!.Profiles.Single(p => p.Id == id)).Select(p => new OfficerCandidateView(p.Id, CandidateName(f, p), p.Pronouns, p.Specialty, p.Personality)).ToImmutableArray();
        return new(owner, state.Turn, ships, fleets, requests, officers, candidates, f.RecentEvents.Concat(f.PendingEvents).OrderBy(x => x.Id).ToImmutableArray());
    }

    public static bool IsActive(RequestStatus status) => status is RequestStatus.Active or RequestStatus.Paused or RequestStatus.SafetyPaused;
    public static bool IsActive(OfficerStatus status) => status is not (OfficerStatus.Dead or OfficerStatus.Retired);
    internal static string CandidateName(FleetEmpireState state, OfficerProfileDefinition profile)
    {
        int used = state.ProfileCommissions.TryGetValue(profile.Id, out int count) ? count : 0;
        return used == 0 ? profile.Name : profile.Name + " " + checked(used + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
