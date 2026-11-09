using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Presentation;

/// <summary>Resolve explicitly selected ships into the existing atomic move action. No detachment or consent.</summary>
public static class MapFleetOrders
{
    public static FleetAction.MoveSelection Selection(EmpireView view, IEnumerable<VesselId> selected, StarId destination)
    {
        var ids = selected.ToHashSet();
        var fleets = view.Fleets!.Ships.Where(s => ids.Contains(s.Id) && s.Fleet is not null).Select(s => s.Fleet!.Value).Distinct().OrderBy(f => f).ToImmutableArray();
        var individuals = view.Fleets.Ships.Where(s => ids.Contains(s.Id) && s.Fleet is null).Select(s => s.Id).OrderBy(id => id).ToImmutableArray();
        return new(individuals, fleets, destination);
    }
}
