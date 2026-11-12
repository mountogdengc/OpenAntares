using System.Collections.Generic;
using System.Linq;
using OpenAntares.Game.Components;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Presentation;

/// <summary>One naming and status vocabulary for ships and fleets in every list. Display only.</summary>
public static class FleetNaming
{
    /// <summary>Design name for designed warships, otherwise the role name. No ship number.</summary>
    public static string ShipClass(ContentSet content, EmpireView view, OwnedVesselView vessel) =>
        vessel.Loadout?.Design is { } id ? ShipDesignUi.DesignName(content, view, id) : content.Vessels[vessel.RoleId].Name;

    public static string Ship(ContentSet content, EmpireView view, OwnedVesselView vessel) =>
        ShipClass(content, view, vessel) + " " + vessel.Id.Value;

    public static string Ship(ContentSet content, EmpireView view, FleetShipView ship) =>
        (ship.Design is { } id ? ShipDesignUi.DesignName(content, view, id) : content.Vessels[ship.RoleId].Name) + " " + ship.Id.Value;

    public static string Fleet(FleetView fleet) => fleet.Name + " ×" + fleet.Members.Length;

    public static string Star(EmpireView view, StarId id) =>
        view.Galaxy.Stars.FirstOrDefault(s => s.Id == id)?.Name ?? "an unavailable system";

    public static string Turns(int turns) => turns == 1 ? "1 turn" : turns + " turns";

    /// <summary>Short status for one ship or one group of ships, from requester-visible facts only.</summary>
    public static string Status(EmpireView view, IReadOnlyList<OwnedVesselView> ships, FleetView? fleet)
    {
        if (ships.Count == 0) return "Away";
        if (ships.FirstOrDefault(v => v.Transit is not null)?.Transit is { } transit)
            return "To " + Star(view, transit.Destination) + " · " + Turns(ships.Max(v => v.Transit?.RemainingTurns ?? 0));
        var pending = ships.Select(v => v.PendingMove).Distinct().ToArray();
        if (pending.Length == 1 && pending[0] is { } destination) return "Course set · " + Star(view, destination);
        if (pending.Any(p => p is not null)) return "Courses set";
        if (ships.Any(v => v.PendingFounding is not null)) return "Founding queued";
        if (fleet is not null && fleet.Order.Kind != FleetOrderKind.Hold) return fleet.Order.Kind.ToString();
        return "Idle";
    }
}
