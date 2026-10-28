using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Fleets;
public static class OfficerBonuses
{
    public static int EffectiveSpeed(ContentSet content, GameState state, VesselId vessel)
    {
        var ship = state.FindVessel(vessel)!;
        int speed = content.Vessels[ship.RoleId].Speed;
        if (!Rules.HasFleets(state.RulesVersion) || ship.Fleet is not { } id || state.FindEmpire(ship.EmpireId)!.Fleets is not { } f) return speed;
        var fleet = f.Fleets.Single(x => x.Id == id);
        var officer = f.Officers.SingleOrDefault(o => o.Id == fleet.Officer && o.Flagship == fleet.Flagship && o.Status == OfficerStatus.Commanding);
        if (officer is not { Specialty: OfficerSpecialty.Navigator, Rank: > 0 }) return speed;
        return checked((int)((long)speed * (100 + content.FleetCommand!.NavigatorBonuses[officer.Rank - 1]) / 100));
    }
}
