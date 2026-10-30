using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Fleets;
namespace OpenAntares.Simulation.ShipDesigns;
internal static class RefitEligibility
{
    internal static string? Reason(ContentSet content,GameState state,EmpireId owner,RefitJob job)
    {
        var ship=state.FindVessel(job.Vessel);var port=state.FindColony(job.Port);
        if(ship is null||ship.EmpireId!=owner)return "refit_ship_missing";
        if(port?.EmpireId!=owner)return "refit_port_unavailable";
        if(ship.Design!=job.SourceDesign||ship.RoleId!=content.ShipDesigns?.WarshipRoleId)return "refit_equipment_changed";
        var profile=VesselLoadouts.CombatProfile(content,state,ship);
        if(ship.Combat is not {} condition||condition.Hull!=profile.MaxHull||condition.Cohesion!=100||ship.CarriedPopulation!=0)return "refit_condition";
        if(ship.Transit is not null||ship.StationaryStarId!=state.FindPlanet(port.PlanetId)?.StarId||ship.PendingMove is not null||ship.PendingFounding is not null)return "refit_location";
        if(condition.ReturnColony is not null||condition.Repairing||ship.AmebaEncounter is not null||state.Combat?.Encounters.Any(e=>e.Members.Contains(ship.Id))==true)return "refit_activity";
        var fleet=state.FindEmpire(owner)?.Fleets;var group=fleet?.Fleets.SingleOrDefault(f=>f.Id==ship.Fleet);
        if(group is not null&&(group.Order.Kind!=FleetOrderKind.Hold||group.Cohort is not null))return "refit_fleet_order";
        if(fleet?.Requests.Any(r=>FleetQueries.IsActive(r.Status)&&(r.Slots.Any(s=>s.Vessel==ship.Id&&s.Status!=SlotStatus.Missing)||group is not null&&r.RallyFleet==group.Id))==true)return "refit_request";
        if(ShipDesignTargets.Job(state,port)?.Id!=job.Id)return "refit_job_changed";
        return null;
    }
}
