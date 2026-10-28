using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Fleets;

public static class FleetStateValidator
{
    public static void Validate(ContentSet content, GameState state, List<string> errors)
    {
        if (!Rules.HasFleets(state.RulesVersion))
        {
            if (state.Empires.Any(e => e.Fleets is not null) || state.Vessels.Any(v => v.Fleet is not null || v.NewlyCompleted || v.BuildOrigin is not null)) errors.Add("Old rules cannot contain fleet/officer metadata.");
            return;
        }
        if (content.FleetCommand is not { } d || state.GalaxySeed is not { } seed) { errors.Add("Fleet rules require embedded command content and a galaxy seed."); return; }
        foreach (var empire in state.Empires)
        {
            void Error(string text) => errors.Add($"{empire.Id}: fleet {text}");
            if (empire.Fleets is not { } f) { Error("state is missing."); continue; }
            if (f.Fleets is null || f.Requests is null || f.Officers is null || f.CandidateProfiles is null || f.ProfileCommissions is null || f.SurveyCredits is null || f.StudyCredits is null || f.HullCredits is null || f.PendingEvents is null || f.RecentEvents is null || f.Fleets.Any(x => x is null) || f.Requests.Any(x => x is null) || f.Officers.Any(x => x is null)) { Error("collections cannot be null."); continue; }
            if (f.NextFleetId < 1 || f.NextRequestId < 1 || f.NextOfficerId < 1 || f.NextEventId < 1 || f.NextFleetId <= f.Fleets.Select(x => x.Id.Value).DefaultIfEmpty().Max() || f.NextRequestId <= f.Requests.Select(x => x.Id.Value).DefaultIfEmpty().Max() || f.NextOfficerId <= f.Officers.Select(x => x.Id.Value).DefaultIfEmpty().Max()) Error("local counters are stale or invalid.");
            if (f.Fleets.Select(x => x.Id).Distinct().Count() != f.Fleets.Count || f.Requests.Select(x => x.Id).Distinct().Count() != f.Requests.Count || f.Officers.Select(x => x.Id).Distinct().Count() != f.Officers.Count) Error("local identifiers are duplicated.");
            var random = FleetInitializer.RecoveryRandom(seed, empire.Id);
            if (f.RecoveryAlgorithmVersion != 1 || f.RecoveryRandom is null || f.RecoveryRandom.Increment != random.Increment || f.RecoveryRollCount < 0 || f.RecoveryRollCount == 0 && f.RecoveryRandom.State != random.State) Error("recovery stream/version/count is invalid.");
            if (f.LifetimeCommissions != f.Officers.Count || f.LifetimeCommissions is < 1 || f.LifetimeCommissions > d.LifetimeCommissionLimit || f.Officers.Count(o => FleetQueries.IsActive(o.Status)) > d.ActiveOfficerLimit) Error("officer lifetime/active bound is invalid.");
            if (f.FutureDoctrine is { } doctrine && !Enum.IsDefined(doctrine)) Error("future doctrine is invalid.");
            if (f.Fleets.Count + f.Requests.Count(r => FleetQueries.IsActive(r.Status) && r.ReservedFleetSlot) > d.FleetLimit || f.Requests.Count(r => FleetQueries.IsActive(r.Status)) > d.RequestLimit) Error("active/reserved fleet or request limit exceeded.");
            if (f.SurveyCredits.Any(id => state.FindStar(id) is null) || f.StudyCredits.Any(id => id < 1) || f.HullCredits.Any(id => id.Value < 1)) Error("reward credit key is invalid.");
            if (!content.Species.TryGetValue(empire.SpeciesId ?? "", out var species)) { Error("species is missing."); continue; }
            var profiles = d.Profiles.Where(p => p.CultureId == species.CommandCultureId).ToDictionary(p => p.Id, StringComparer.Ordinal);
            if (f.ProfileCommissions.Any(p => !profiles.ContainsKey(p.Key) || p.Value < 1 || p.Value > d.LifetimeCommissionLimit) || f.ProfileCommissions.Values.Sum(v => (long)v) != f.LifetimeCommissions) Error("profile commission catalog is invalid.");
            if (f.CandidateProfiles.Distinct(StringComparer.Ordinal).Count() != f.CandidateProfiles.Count || f.CandidateProfiles.Any(id => id is null || !profiles.ContainsKey(id)) || species.CommandEmbodiment == CommandEmbodiment.ColonyBody && f.CandidateProfiles.Count != 0) Error("candidate catalog is invalid.");
            var memberIds = new HashSet<VesselId>();
            foreach (var fleet in f.Fleets)
            {
                if (fleet.Id.Value < 1 || !Text(fleet.Name, 80) || fleet.Members is null || fleet.Members.Count < 1 || fleet.Members.Count > d.MemberLimit || fleet.Members.Distinct().Count() != fleet.Members.Count || !fleet.Members.Contains(fleet.Flagship) || fleet.Order is null || !Enum.IsDefined(fleet.Order.Kind)) { Error("membership/name/flagship/order is invalid."); continue; }
                if ((fleet.Order.Kind is FleetOrderKind.Hold or FleetOrderKind.Explore) && fleet.Order.Destination is not null || (fleet.Order.Kind is FleetOrderKind.Assemble or FleetOrderKind.Return) && (fleet.Order.Destination is not { } target || state.FindStar(target) is null)) Error("order destination is invalid.");
                var members = fleet.Members.Select(id => state.FindVessel(id)).ToArray();
                foreach (var vessel in members)
                    if (vessel is null || vessel.EmpireId != empire.Id || vessel.Fleet != fleet.Id || !memberIds.Add(vessel.Id)) Error("member is missing, foreign, duplicated or linked to another fleet.");
                if (members.Any(v => v is null || v.EmpireId != empire.Id)) continue;
                if(fleet.Order.Kind==FleetOrderKind.Explore&&members.Any(v=>v!.Scout is null))Error("Explore contains a non-scout member.");
                if(empire.Ready&&members.Select(v=>v!.PendingMove).Distinct().Count()>1)Error("ready fleet has incompatible pending departures.");
                var first = members[0]!;
                if (members.Any(v => v!.StationaryStarId != first.StationaryStarId || v.Transit != first.Transit)) Error("members are not co-located in one transit cohort.");
                if (first.Transit is { } transit)
                {
                    if (fleet.Cohort is not { } cohort || cohort.Origin != transit.Origin || cohort.Destination != transit.Destination || cohort.TotalTurns != transit.TotalTurns || cohort.EffectiveSpeed < 1 || cohort.LaunchTurn < 0 || cohort.LaunchTurn > state.Turn) Error("committed cohort provenance is invalid.");
                }
                else if (fleet.Cohort is not null) Error("stationary fleet has committed transit provenance.");
                if (fleet.Officer is { } officer && !f.Officers.Any(o => o.Id == officer && o.Status == OfficerStatus.Commanding && o.Flagship == fleet.Flagship)) Error("commander assignment does not match the flagship.");
            }
            foreach (var vessel in state.Vessels.Where(v => v.EmpireId == empire.Id))
            {
                if (vessel.Fleet is { } fleet && !f.Fleets.Any(x => x.Id == fleet && x.Members.Contains(vessel.Id))) Error("vessel has an orphan fleet reference.");
                if (vessel.BuildOrigin is { } origin && (origin.Request.Value < 1 || origin.Slot < 1 || origin.QueueToken < 1 || origin.Colony.Value < 1 || !f.Requests.Any(r => r.Id == origin.Request))) Error("hull construction provenance is invalid.");
            }
            var participating = new HashSet<ColonyId>(); var rallies = new HashSet<FleetId>(); var reservedShips = new HashSet<VesselId>(); var queueBindings = new HashSet<(ColonyId, int)>();
            foreach (var request in f.Requests)
            {
                if (request.Id.Value < 1 || request.Revision < 1 || !Text(request.Name, 80) || !Enum.IsDefined(request.Priority) || !Enum.IsDefined(request.Status) || state.FindStar(request.Rally) is null || request.Colonies is null || request.PausedContributions is null || request.Slots is null || request.Colonies.Count < (request.Status == RequestStatus.SafetyPaused ? 0 : 1) || request.Colonies.Count > d.ParticipatingColonyLimit || request.Colonies.Distinct().Count() != request.Colonies.Count || request.Slots.Count < 1 || request.Slots.Count > d.MemberLimit || request.Slots.Any(s => s is null) || request.Slots.Select(s => s.Ordinal).Distinct().Count() != request.Slots.Count) { Error("request shape/priority/quota is invalid."); continue; }
                bool active = FleetQueries.IsActive(request.Status);
                if(request.NextSlotOrdinal<1||request.NextSlotOrdinal<=request.Slots.Max(s=>s.Ordinal))Error("request slot counter is stale.");
                if(request.Status!=RequestStatus.Complete&&request.CompletedTurn is not null)Error("unfinished request has a completion date.");
                if (request.PausedContributions.Distinct().Count() != request.PausedContributions.Count || request.PausedContributions.Any(id => !request.Colonies.Contains(id))) Error("paused contribution is invalid.");
                if (active)
                {
                    foreach (var colony in request.Colonies) if (state.FindColony(colony)?.EmpireId != empire.Id || !participating.Add(colony)) Error("participating colony is foreign, missing or already reserved.");
                    if (request.RallyFleet is { } rally && (!f.Fleets.Any(x => x.Id == rally) || !rallies.Add(rally) || request.ReservedFleetSlot)) Error("rally fleet reservation is invalid.");
                    if(request.RallyFleet is {} reserved&&f.Fleets.SingleOrDefault(x=>x.Id==reserved) is {} target&&target.Members.Count+request.Slots.Count(s=>s.Vessel is not {} id||!target.Members.Contains(id))>d.MemberLimit)Error("rally fleet lacks its full reserved quota capacity.");
                    if (request.Status == RequestStatus.Active && request.RallyFleet is null && !request.ReservedFleetSlot) Error("active request has no rally fleet capacity reservation.");
                    if (request.Status == RequestStatus.SafetyPaused && !Text(request.Attention, 1000)) Error("safety pause requires its observed reason.");
                    if (request.Coordinator is { } coordinator && !f.Officers.Any(o => o.Id == coordinator && o.Status == OfficerStatus.Coordinating && o.Request == request.Id)) Error("request coordinator is invalid.");
                }
                if (request.Status == RequestStatus.Complete && (request.CompletedTurn is not { } completed || completed < 0 || completed > state.Turn || request.Slots.Any(s => s.Status != SlotStatus.Arrived))) Error("completed request does not have its full arrived quota.");
                foreach (var slot in request.Slots)
                {
                    if (slot.Ordinal < 1 || !Enum.IsDefined(slot.Status) || slot.RoleId is null || !content.Vessels.TryGetValue(slot.RoleId, out var role) || !species.AllowedVesselIds.Contains(slot.RoleId)) { Error("request slot/role is invalid."); continue; }
                    if (role.EmbarkPopulation > 0 && !request.AuthorizeCargo) Error("request lacks explicit population cargo authorization.");
                    if (slot.Status == SlotStatus.BlockedSafety && (request.Status != RequestStatus.SafetyPaused || slot.ResumeStatus is null or SlotStatus.Missing or SlotStatus.BlockedSafety || !Enum.IsDefined(slot.ResumeStatus.Value))) Error("blocked-safety slot must preserve its previous progress.");
                    if (slot.Status != SlotStatus.BlockedSafety && slot.ResumeStatus is not null) Error("unblocked slot has stale safety progress.");
                    if (!active) continue; // Retained completed/cancelled facts are not live entity references.
                    var progress = slot.Status == SlotStatus.BlockedSafety ? slot.ResumeStatus : slot.Status;
                    if (slot.Vessel is { } id)
                    {
                        var vessel = state.FindVessel(id);
                        if (vessel?.EmpireId != empire.Id || vessel.RoleId != slot.RoleId || progress == SlotStatus.Missing || !reservedShips.Add(id)) Error("slot hull is foreign, missing, duplicated or incorrectly reserved.");
                        if(progress==SlotStatus.Arrived&&(request.RallyFleet is null||vessel?.Fleet!=request.RallyFleet||vessel.StationaryStarId!=request.Rally))Error("arrived slot is not at its actual rally fleet.");
                        if(progress==SlotStatus.Travelling&&vessel?.Transit is null||progress==SlotStatus.Built&&vessel?.Transit is not null)Error("slot transit progress is incoherent.");
                        if (slot.NewHull && vessel?.BuildOrigin is not { } origin) Error("new hull lacks original construction provenance.");
                        else if (slot.NewHull && (vessel!.BuildOrigin!.Request != request.Id || vessel.BuildOrigin.Slot != slot.Ordinal)) Error("new hull belongs to another original reservation.");
                    }
                    else if (progress is SlotStatus.Built or SlotStatus.Travelling or SlotStatus.Arrived) Error("built slot is missing its live hull.");
                    if (progress == SlotStatus.Queued && slot.QueueToken is { } token)
                    {
                        if (slot.Colony is not { } colony || !request.Colonies.Contains(colony) || !queueBindings.Add((colony, token)) || state.FindColony(colony)?.Development?.Queue.Any(q => q.Token == token && q.Target.Kind == ProductionKinds.Vessel && q.Target.ContentId == slot.RoleId) != true) Error("request queue binding is missing, duplicated or mismatched.");
                    }
                }
            }
            var bodies = new HashSet<ColonyId>(); var flagships = new HashSet<VesselId>(); var coordinators = new HashSet<RequestId>();
            foreach (var officer in f.Officers)
            {
                if (officer.Id.Value < 1 || !Text(officer.Name, 256) || !Text(officer.Pronouns, 80) || !Text(officer.Personality, 400) || officer.ProfileId is null || !profiles.TryGetValue(officer.ProfileId, out var p) || officer.SpeciesId != species.Id || officer.CultureId != species.CommandCultureId || officer.Specialty != p.Specialty || officer.Embodiment != species.CommandEmbodiment || !Enum.IsDefined(officer.Status) || !Enum.IsDefined(officer.Doctrine) || officer.Xp < 0 || officer.Xp > d.XpCap || officer.Rank != (officer.Xp >= d.RankThresholds[1] ? 2 : officer.Xp >= d.RankThresholds[0] ? 1 : 0)) { Error("officer identity/experience/unlocks is invalid."); continue; }
                bool live = FleetQueries.IsActive(officer.Status);
                if (officer.Embodiment == CommandEmbodiment.ColonyBody)
                {
                    if (officer.SourceBody is not { } source || live && state.FindColony(source)?.EmpireId != empire.Id || live && !bodies.Add(source) || officer.Status is OfficerStatus.Dead or OfficerStatus.Missing) Error("colony-body appointment/source identity is invalid.");
                }
                else if (officer.SourceBody is not null) Error("mortal officer has a colony-body source.");
                if (officer.Status is OfficerStatus.Posted or OfficerStatus.Commissioning or OfficerStatus.Coordinating)
                {
                    if (officer.PostedColony is not { } colony || state.FindColony(colony)?.EmpireId != empire.Id || officer.Flagship is not null || officer.RecoveryColony is not null) Error("posted officer location/assignment is invalid.");
                }
                else if (officer.PostedColony is not null) Error("officer has a conflicting posting.");
                if (officer.Status == OfficerStatus.Commanding)
                {
                    if (officer.Flagship is not { } id || state.FindVessel(id)?.EmpireId != empire.Id || !flagships.Add(id) || !f.Fleets.Any(x => x.Flagship == id && x.Officer == officer.Id) || officer.Request is not null) Error("flagship command assignment is invalid.");
                }
                else if (officer.Flagship is not null) Error("unavailable officer has a flagship assignment.");
                if (officer.Status == OfficerStatus.Coordinating)
                {
                    if (officer.Request is not { } id || !coordinators.Add(id) || !f.Requests.Any(r => r.Id == id && FleetQueries.IsActive(r.Status) && r.Coordinator == officer.Id && officer.PostedColony is { } col && r.Colonies.Contains(col))) Error("officer coordinates an invalid or second request.");
                }
                else if (officer.Request is not null) Error("officer has a conflicting request assignment.");
                if (officer.Status is OfficerStatus.Commissioning or OfficerStatus.Recovering)
                {
                    if (officer.AvailableAtTurn is not { } due || due <= state.Turn) Error("officer availability deadline is invalid.");
                }
                else if (officer.AvailableAtTurn is not null) Error("officer has a stale availability deadline.");
                if (officer.Status == OfficerStatus.Recovering && (officer.RecoveryColony is not { } recovery || state.FindColony(recovery)?.EmpireId != empire.Id)) Error("recovery destination is unavailable.");
                if (officer.Status != OfficerStatus.Recovering && officer.RecoveryColony is not null) Error("officer has a stale recovery destination.");
                if (officer.Highlights is null || officer.Details is null || officer.Career is null || officer.Highlights.Count > d.HighlightLimit || officer.Details.Count > d.DetailTurnLimit || officer.Highlights.Any(h => h.Event < 1 || h.Event >= f.NextEventId || h.Turn < 0 || h.Turn > state.Turn) || officer.Highlights.Select(h => h.Event).Distinct().Count() != officer.Highlights.Count || officer.Details.Any(t => t.Turn < 0 || t.Turn > state.Turn || t.Entries.IsDefault) || officer.Career.FirstSurveys < 0 || officer.Career.Studies < 0 || officer.Career.NewHulls < 0 || officer.Career.ShipsLost < 0 || officer.Career.FlagshipsLost < 0 || officer.Career.Recoveries < 0 || officer.Career.XpEarned < 0 || officer.Career.CompletedRequests < 0) Error("officer career/history is invalid.");
            }
            var events = f.PendingEvents.Concat(f.RecentEvents).ToArray();
            if (events.Any(x => x.Owner != empire.Id || x.Id < 1 || x.Id >= f.NextEventId || x.Turn < 0 || x.Turn > state.Turn || !Enum.IsDefined(x.Importance)) || events.Select(x => x.Id).Distinct().Count() != events.Length) Error("events/owner/event counter are invalid.");
        }
    }

    private static bool Text(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Length <= max && !value.Any(char.IsControl);
}
