using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Fleets;

public static class FleetQueries
{
    public static FleetActionPreview PreviewAction(ContentSet content, Knowledge.EmpireView view, FleetAction action)
    {
        string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { view.Turn, view.Viewer, view.Fleets, view.Scouting, view.Vessels, view.ForeignVessels, view.Research.KnownTechnologyIds, Action = System.Text.Json.JsonSerializer.Serialize(action, action.GetType()) })));
        try
        {
            FleetCommandProcessor.ValidateAction(content, view, action);
            var routes=ImmutableArray<FleetRouteConsequence>.Empty;
            if(action is FleetAction.MoveSelection {Destination:{} target} move)
            {
                var ids=move.Vessels.Concat(move.Fleets.SelectMany(id=>view.Fleets!.Fleets.Single(f=>f.Id==id).Members)).Distinct().OrderBy(id=>id);
                routes=ids.Select(id=>{var ship=view.Vessels.Single(v=>v.Id==id);return new FleetRouteConsequence(id,view.Fleets!.Ships.Single(v=>v.Id==id).Fleet,ship.StationaryStarId!.Value,target,FleetTravel.PreviewDuration(content,view,ship,target));}).ToImmutableArray();
            }
            return new(true, null, action is FleetAction.DetachSelection ? "Selected ships become independent; affected request slots pause without replacement." : "Apply the displayed fleet selection atomically."+(view.Conflict?.Blockades.IsEmpty==false?" Changing a blockading fleet movement, membership or activity lifts its blockade immediately.":""), fingerprint){Routes=routes,Detachments=action is FleetAction.DetachSelection detach?detach.Vessels.Distinct().OrderBy(id=>id).ToImmutableArray():ImmutableArray<VesselId>.Empty};
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or OverflowException) { return new(false, e.Message, "", fingerprint); }
    }
    public static FleetEmpireView Project(ContentSet content, GameState state, EmpireId owner)
    {
        var e = state.FindEmpire(owner) ?? throw new ArgumentException("Empire not found.", nameof(owner));
        if (!Rules.HasFleets(state.RulesVersion) || e.Fleets is not { } f)
            return new(owner, state.Turn, ImmutableArray<FleetShipView>.Empty, ImmutableArray<FleetView>.Empty, ImmutableArray<AssemblyRequestView>.Empty, ImmutableArray<OfficerView>.Empty, ImmutableArray<OfficerCandidateView>.Empty, ImmutableArray<FleetEvent>.Empty);
        var reservations = f.Requests.Where(r => IsActive(r.Status)).SelectMany(r => r.Slots.Where(s => s.Vessel is not null && s.Status != SlotStatus.Missing).Select(s => (Vessel: s.Vessel!.Value, Request: r.Id))).ToDictionary(x => x.Vessel, x => x.Request);
        var ships = state.Vessels.Where(v => v.EmpireId == owner).OrderBy(v => v.Id).Select(v => new FleetShipView(v.Id, v.RoleId, v.StationaryStarId, v.Transit, v.PendingMove, v.Fleet, reservations.TryGetValue(v.Id, out var request) ? request : null, v.NewlyCompleted, v.Scout?.ApproachAcceptance){Design=v.Design}).ToImmutableArray();
        var fleets = f.Fleets.OrderBy(x => x.Id).Select(x => new FleetView(x.Id, x.Name, x.Members.OrderBy(v => v).ToImmutableArray(), x.Flagship, x.Officer, x.Order, x.Cohort, x.Attention)).ToImmutableArray();
        var requests = f.Requests.OrderBy(x => x.Id).Select(x => new AssemblyRequestView(x.Id, x.Revision, x.Name, x.Rally, x.RallyFleet, x.ReservedFleetSlot, x.Priority, x.Status, x.Colonies.ToImmutableArray(), x.PausedContributions.ToImmutableArray(), x.Slots.Select(s => new AssemblySlotView(s.Ordinal, s.RoleId, s.Colony, s.QueueToken, s.Vessel, s.Status, s.ResumeStatus, s.NewHull, s.BlockedReason){Design=s.Design}).ToImmutableArray(), x.AuthorizeCargo, x.Coordinator, x.Attention){NextSlotOrdinal=x.NextSlotOrdinal}).ToImmutableArray();
        var officers = f.Officers.OrderBy(x => x.Id).Select(x => new OfficerView(x.Id, x.Name, x.Pronouns, x.Personality, x.ProfileId, x.CultureId, x.Embodiment, x.Specialty, x.Doctrine, x.Status, x.Xp, x.Rank, x.SourceBody, x.PostedColony, x.Flagship, x.Request, x.RecoveryColony, x.AvailableAtTurn, x.Career, x.Highlights.ToImmutableArray(), x.Details.ToImmutableArray(), x.FinalSummary)).ToImmutableArray();
        var candidates = f.CandidateProfiles.Select(id => content.FleetCommand!.Profiles.Single(p => p.Id == id)).Select(p => new OfficerCandidateView(p.Id, CandidateName(f, p), p.Pronouns, p.Specialty, p.Personality)).ToImmutableArray();
        return new(owner, state.Turn, ships, fleets, requests, officers, candidates, f.RecentEvents.Concat(f.PendingEvents).OrderBy(x => x.Id).ToImmutableArray()){FutureDoctrine=f.FutureDoctrine};
    }

    internal static bool RallyUnsafe(ContentSet content,GameState state,EmpireId owner,AssemblyRequest request)
    {
        var e=state.FindEmpire(owner)!;var f=e.Fleets!;
        var ships=request.Slots.Where(s=>s.Vessel is not null).Select(s=>s.Vessel!.Value)
            .Concat(f.Fleets.SingleOrDefault(fleet=>fleet.Id==request.RallyFleet)?.Members??Enumerable.Empty<VesselId>())
            .Distinct().Select(state.FindVessel).Where(v=>v is not null).ToArray();
        if(e.Scouting!.KnownHabitats.Any(h=>h.Star==request.Rally))return true;
        if(ships.Any(v=>v!.Scout?.Posture==Scouting.ScoutPosture.AvoidEncounters)&&Scouting.ScoutQueries.NeutralExcluded(Knowledge.EmpireViewProjector.ProjectBase(content,state,owner),request.Rally))return true;
        return (request.Slots.Any(s=>content.Vessels[s.RoleId].EmbarkPopulation>0)||ships.Any(v=>v!.CarriedPopulation>0))&&Crises.CrisisQueries.PortClosed(state,owner,request.Rally);
    }
    public static bool IsActive(RequestStatus status) => status is RequestStatus.Active or RequestStatus.Paused or RequestStatus.SafetyPaused;
    public static bool IsActive(OfficerStatus status) => status is not (OfficerStatus.Dead or OfficerStatus.Retired);
    internal static string CandidateName(FleetEmpireState state, OfficerProfileDefinition profile)
    {
        int used = state.ProfileCommissions.TryGetValue(profile.Id, out int count) ? count : 0;
        return used == 0 ? profile.Name : profile.Name + " " + checked(used + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
