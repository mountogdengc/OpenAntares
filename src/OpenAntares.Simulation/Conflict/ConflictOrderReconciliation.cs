using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
internal static class ConflictOrderReconciliation
{
    internal static void AfterCommand(GameState before,GameState candidate,Command command)
    {
        if(!Rules.HasConflict(candidate.RulesVersion)||candidate.Conflict is null)return;
        var owner=candidate.Conflict.Owners.Single(o=>o.Owner==command.Issuer);var empire=candidate.FindEmpire(command.Issuer)!;
        owner.Preferences.RemoveAll(p=>candidate.FindVessel(p.Anchor) is not {} ship||ship.EmpireId!=owner.Owner||p.Fleet!=ship.Fleet);
        foreach(var blockade in owner.Blockades.ToArray())
        {
            var fleet=empire.Fleets!.Fleets.SingleOrDefault(f=>f.Id==blockade.Fleet);var original=before.FindEmpire(owner.Owner)!.Fleets!.Fleets.SingleOrDefault(f=>f.Id==blockade.Fleet);
            bool replaced=fleet is null||original is not null&&!fleet.Members.SequenceEqual(original.Members)||fleet?.Order.Kind!=FleetOrderKind.Hold||fleet?.Members.Any(id=>candidate.FindVessel(id) is not {} ship||ship.StationaryStarId!=blockade.Star||ship.Transit is not null||ship.PendingMove is not null||ship.Combat?.Repairing==true||empire.ShipDesigns!.Refits.Any(r=>r.Vessel==id)||empire.Fleets!.Requests.Any(r=>FleetQueries.IsActive(r.Status)&&r.Slots.Any(s=>s.Vessel==id)))==true;
            if(!replaced)continue;owner.Blockades.Remove(blockade);owner.Revision=checked(owner.Revision+1);
            ConflictRelations.RecordEvent(candidate,owner.Owner,blockade.Other,"blockade_lifted","Your accepted fleet, movement, repair, refit or assembly order lifted the blockade immediately.",blockade.Star);
        }
    }
}
