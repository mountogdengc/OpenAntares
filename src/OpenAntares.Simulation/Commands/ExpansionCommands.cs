using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Movement;
namespace OpenAntares.Simulation.Commands;

public static partial class CommandProcessor
{
    private static CommandOutcome? CheckVessel(GameState s, EmpireId issuer, VesselId id, out VesselState? v)
    {
        v = null;
        if (!Rules.HasExpansion(s.RulesVersion)) return CommandOutcome.Reject(s, "expansion_unavailable", "Legacy colony game - start a new game for exploration.");
        v = s.FindVessel(id);
        if (v is null || v.EmpireId != issuer) return CommandOutcome.Reject(s, RejectionCodes.NotOwner, "Vessel is not controlled by this empire.");
        if (v.Transit is not null) return CommandOutcome.Reject(s, "vessel_in_transit", "Travel is committed until arrival.");
        return null;
    }
    internal static CommandOutcome SetFleetMoveOrder(ContentSet content,GameState s,SetMoveOrderCommand c) => SetMoveOrder(content,s,c,group:true);
    private static CommandOutcome SetMoveOrder(ContentSet content, GameState s, SetMoveOrderCommand c,Scouting.AmebaApproach? acceptance=null,bool retreat=false,bool group=false)
    {
        var rejection = CheckVessel(s, c.Issuer, c.Vessel, out var v); if (rejection is not null) return rejection;
        if(Rules.HasFleets(s.RulesVersion)&&!group&&!retreat&&(v!.Fleet is not null||s.FindEmpire(c.Issuer)!.Fleets!.Requests.Any(r=>Fleets.FleetQueries.IsActive(r.Status)&&r.Slots.Any(slot=>slot.Vessel==v.Id)))&&v.PendingMove!=c.Destination)return CommandOutcome.Reject(s,"fleet_detachment_required","Explicitly detach this ship from its fleet/request first.");
        if (c.Destination is { } target)
        {
            var end = s.FindStar(target); if (end is null) return CommandOutcome.Reject(s, "unknown_star", "Destination is not on the chart.");
            if (target == v!.StationaryStarId) return CommandOutcome.Reject(s, "already_there", "Vessel is already at that system.");
            if(v.CarriedPopulation>0&&(Crises.CrisisQueries.IsHeld(s,v.Id)||Crises.CrisisQueries.PortClosed(s,c.Issuer,v.StationaryStarId!.Value)||Crises.CrisisQueries.PortClosed(s,c.Issuer,target)))return CommandOutcome.Reject(s,"population_port_closed","Own population transport is held or quarantined. Cancel orders or release the port when appropriate.");
            var start = s.FindStar(v.StationaryStarId!.Value)!;
            try { TravelCalculator.Duration(start.X, start.Y, end.X, end.Y, content.Vessels[v.RoleId].Speed); }
            catch (Exception ex) when (ex is OverflowException or ArgumentOutOfRangeException) { return CommandOutcome.Reject(s, "invalid_route", "Route exceeds supported travel limits."); }
            var scoutRejection=CheckScoutDestination(content,s,v,target,acceptance,retreat);if(scoutRejection is not null)return scoutRejection;
        }
        if (v!.PendingMove == c.Destination && (c.Destination is null || v.PendingFounding is null&&v.AmebaEncounter?.RiskOrder is null) && (acceptance is null||v.Scout?.ApproachAcceptance==acceptance)) return CommandOutcome.NoChange(s);
        return ApplyPlanningChange(s, c.Issuer, candidate => { var unit = candidate.FindVessel(c.Vessel)!; unit.PendingMove = c.Destination;if(unit.Scout is {} scout)scout.ApproachAcceptance=acceptance;if (c.Destination.HasValue){unit.PendingFounding = null;if(unit.AmebaEncounter is {} encounter)encounter.RiskOrder=null;} });
    }
    private static CommandOutcome SetProductionTarget(ContentSet content, GameState s, EmpireState issuer, SetProductionTargetCommand c)
    {
        if (c.Target is null || c.Target.Kind == ProductionKinds.Building) return SetProject(content, s, issuer, new(c.Issuer, c.ExpectedTurn, c.Colony, c.Target?.ContentId));
        if (!TryGetOwnedColony(s, c.Issuer, c.Colony, out var colony, out var rejection)) return rejection;
        if (c.Target.Kind != ProductionKinds.Vessel) return CommandOutcome.Reject(s, "invalid_production_kind", "Unknown production kind.");
        if (!Rules.HasExpansion(s.RulesVersion)) return CommandOutcome.Reject(s, "expansion_unavailable", "Legacy colony game - start a new game for exploration.");
        if (!content.Vessels.ContainsKey(c.Target.ContentId)) return CommandOutcome.Reject(s, "unknown_vessel_role", "Vessel role is not defined.");
        if (!Species.SpeciesRules.CanBuildVessel(content,s,c.Issuer,c.Target.ContentId)) return CommandOutcome.Reject(s, "project_not_available_for_species", "This expedition is not available to the selected species.");
        if (colony.ProjectId == c.Target.ContentId && colony.ProjectKind == c.Target.Kind) return CommandOutcome.NoChange(s);
        return ApplyPlanningChange(s, c.Issuer, candidate => { var target = candidate.FindColony(c.Colony)!; target.ProjectId = c.Target.ContentId; target.ProjectKind = c.Target.Kind; });
    }
    private static CommandOutcome SetFoundingOrder(ContentSet content, GameState s, SetFoundingOrderCommand c)
    {
        var rejection = CheckVessel(s, c.Issuer, c.Vessel, out var v); if (rejection is not null) return rejection;
        if(Rules.HasFleets(s.RulesVersion)&&c.Planet is not null&&s.FindEmpire(c.Issuer)!.Fleets!.Requests.Any(r=>Fleets.FleetQueries.IsActive(r.Status)&&r.Slots.Any(slot=>slot.Vessel==v!.Id)))return CommandOutcome.Reject(s,"fleet_detachment_required","Explicitly detach this request ship before founding.");
        if(Rules.HasFleets(s.RulesVersion)&&c.Planet is not null&&v!.Fleet is {} group&&s.FindEmpire(c.Issuer)!.Fleets!.Fleets.Single(f=>f.Id==group).Members.Any(id=>s.FindVessel(id)!.PendingMove is not null))return CommandOutcome.Reject(s,"fleet_detachment_required","Cancel incompatible fleet departures before founding.");
        if (c.Planet is { } target)
        {
            var preview = Colonization.FoundingRules.Preview(content,s,c.Issuer,c.Vessel,target);
            if (preview.RejectionCode is { } code) return CommandOutcome.Reject(s,code,Colonization.FoundingRules.Message(code));
        }
        if (v!.PendingFounding == c.Planet && (c.Planet is null || v.PendingMove is null)) return CommandOutcome.NoChange(s);
        return ApplyPlanningChange(s, c.Issuer, candidate => { var unit = candidate.FindVessel(c.Vessel)!; unit.PendingFounding = c.Planet; if (c.Planet.HasValue) unit.PendingMove = null; });
    }
}
