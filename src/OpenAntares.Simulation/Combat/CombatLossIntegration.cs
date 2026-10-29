using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
internal static class CombatLossIntegration
{
    internal static void Apply(ContentSet content,GameState snapshot,GameState candidate,FleetResolutionContext fleet,CombatResolutionContext combat)
    {
        if(!Rules.HasCombat(snapshot.RulesVersion))return;
        if(!ReferenceEquals(fleet,combat.FleetContext)||snapshot.Turn!=combat.FromTurn)throw new InvalidOperationException("Combat losses require their shared snapshot officer context.");
        if(combat.Destructions.Any(d=>candidate.FindVessel(d.Vessel) is not null))throw new InvalidOperationException("Consumed combat vessel is still live.");
        var missing=snapshot.Vessels.Where(v=>candidate.FindVessel(v.Id) is null&&!fleet.IntegratedLosses.Contains(v.Id)).OrderBy(v=>v.Id).ToArray();if(missing.Length==0)return;
        FleetResolver.NormalizeAfterLosses(content,snapshot,candidate,fleet);
        foreach(var vessel in missing)
        {
            fleet.IntegratedLosses.Add(vessel.Id);
            // Immutable scouting/study/health facts remain historical. The existing report finalizer
            // retains certified retired-vessel facts only while those histories still refer to them.
            candidate.FindEmpire(vessel.EmpireId)!.Scouting!.Receipts.RemoveAll(r=>r.Token.Vessel==vessel.Id);
        }
    }
}
