using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Movement;
namespace OpenAntares.Simulation.Commands;
public static partial class CommandProcessor
{
    private static CommandOutcome? CheckVessel(GameState s,EmpireId issuer,VesselId id,out VesselState? v)
    {
        v=null;
        if(s.RulesVersion!=Rules.ExpansionVersion)return CommandOutcome.Reject(s,"expansion_unavailable","Legacy colony game - start a new game for exploration.");
        v=s.FindVessel(id);
        if(v is null || v.EmpireId!=issuer)return CommandOutcome.Reject(s,RejectionCodes.NotOwner,"Vessel is not controlled by this empire.");
        if(v.Transit is not null)return CommandOutcome.Reject(s,"vessel_in_transit","Travel is committed until arrival.");
        return null;
    }
    private static CommandOutcome SetMoveOrder(ContentSet content,GameState s,SetMoveOrderCommand c)
    {
        var rejection=CheckVessel(s,c.Issuer,c.Vessel,out var v);if(rejection is not null)return rejection;
        if(c.Destination is {} target)
        {
            var end=s.FindStar(target);if(end is null)return CommandOutcome.Reject(s,"unknown_star","Destination is not on the chart.");
            if(target==v!.StationaryStarId)return CommandOutcome.Reject(s,"already_there","Vessel is already at that system.");
            var start=s.FindStar(v.StationaryStarId!.Value)!;
            try {TravelCalculator.Duration(start.X,start.Y,end.X,end.Y,content.Vessels[v.RoleId].Speed);}
            catch(Exception ex) when(ex is OverflowException or ArgumentOutOfRangeException) {return CommandOutcome.Reject(s,"invalid_route","Route exceeds supported travel limits.");}
        }
        if(v!.PendingMove==c.Destination && (c.Destination is null || v.PendingFounding is null))return CommandOutcome.NoChange(s);
        return ApplyPlanningChange(s,c.Issuer,candidate=>{var unit=candidate.FindVessel(c.Vessel)!;unit.PendingMove=c.Destination;if(c.Destination.HasValue)unit.PendingFounding=null;});
    }
    private static CommandOutcome SetProductionTarget(ContentSet content,GameState s,EmpireState issuer,SetProductionTargetCommand c)
    {
        if(c.Target is null || c.Target.Kind==ProductionKinds.Building) return SetProject(content,s,issuer,new(c.Issuer,c.ExpectedTurn,c.Colony,c.Target?.ContentId));
        if(!TryGetOwnedColony(s,c.Issuer,c.Colony,out var colony,out var rejection))return rejection;
        if(c.Target.Kind!=ProductionKinds.Vessel)return CommandOutcome.Reject(s,"invalid_production_kind","Unknown production kind.");
        if(s.RulesVersion!=Rules.ExpansionVersion)return CommandOutcome.Reject(s,"expansion_unavailable","Legacy colony game - start a new game for exploration.");
        if(!content.Vessels.ContainsKey(c.Target.ContentId))return CommandOutcome.Reject(s,"unknown_vessel_role","Vessel role is not defined.");
        if(colony.ProjectId==c.Target.ContentId && colony.ProjectKind==c.Target.Kind)return CommandOutcome.NoChange(s);
        return ApplyPlanningChange(s,c.Issuer,candidate=>{var target=candidate.FindColony(c.Colony)!;target.ProjectId=c.Target.ContentId;target.ProjectKind=c.Target.Kind;});
    }
    private static CommandOutcome SetFoundingOrder(ContentSet content,GameState s,SetFoundingOrderCommand c)
    {
        var rejection=CheckVessel(s,c.Issuer,c.Vessel,out var v);if(rejection is not null)return rejection;
        if(c.Planet is {} target)
        {
            if(!s.FindEmpire(c.Issuer)!.Surveys.Any(o=>o.Planets.Any(p=>p.PlanetId==target)))return CommandOutcome.Reject(s,"target_not_known","Planet is not known to this empire.");
            var planet=s.FindPlanet(target);
            if(planet is null)return CommandOutcome.Reject(s,"target_not_known","Planet is not known to this empire.");
            var role=content.Vessels[v!.RoleId];
            if(!role.CanFoundColony || v.CarriedPopulation!=role.EmbarkPopulation || v.CarriedPopulation<1)return CommandOutcome.Reject(s,"cannot_found","This vessel cannot found a colony.");
            if(v.StationaryStarId!=planet.StarId)return CommandOutcome.Reject(s,"wrong_system","Settler must be stationed at the selected planet's system.");
            var type=content.PlanetTypes[planet.PlanetTypeId];
            if(!type.Colonizable || type.SupportPerWorker<=0 || content.Rules.BaseColonyCapacity<v.CarriedPopulation)return CommandOutcome.Reject(s,"not_colonizable","Planet cannot support this settlement.");
            if(s.Colonies.Any(colony=>colony.PlanetId==target))return CommandOutcome.Reject(s,"target_occupied","Planet is already colonized.");
            if(s.Vessels.Any(other=>other.Id!=v.Id && other.EmpireId==c.Issuer && other.PendingFounding==target))return CommandOutcome.Reject(s,"target_reserved","Another settler already has a founding order here.");
        }
        if(v!.PendingFounding==c.Planet && (c.Planet is null || v.PendingMove is null))return CommandOutcome.NoChange(s);
        return ApplyPlanningChange(s,c.Issuer,candidate=>{var unit=candidate.FindVessel(c.Vessel)!;unit.PendingFounding=c.Planet;if(c.Planet.HasValue)unit.PendingMove=null;});
    }
}
