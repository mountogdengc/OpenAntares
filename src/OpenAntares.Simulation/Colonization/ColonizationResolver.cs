using System.Linq;
using System.Collections.Generic;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
namespace OpenAntares.Simulation.Colonization;

public static class ColonizationResolver
{
    public static void Apply(ContentSet content, GameState snapshot, GameState candidate, List<ExpansionEvent> events,Crises.CrisisResolutionContext? context=null,Scouting.AmebaResolutionContext? scouting=null)
    {
        foreach (var before in snapshot.Vessels.Where(v => v.PendingFounding.HasValue).OrderBy(v => v.EmpireId).ThenBy(v => v.Id))
        {
            if(scouting?.ConsumedVessels.Contains(before.Id)==true)continue;
            var target = before.PendingFounding!.Value; var vessel = candidate.FindVessel(before.Id)??throw new System.InvalidOperationException("Unexpected missing founding vessel."); var planet = candidate.FindPlanet(target)!;
            if(scouting is not null&&snapshot.Scouting?.Habitat?.Star==planet.StarId)throw new System.InvalidOperationException("Ameba habitat cannot be colonized.");
            if(context is not null&&(Crises.CrisisQueries.IsHeld(snapshot,before.Id)||context.ClosedPorts.Contains((before.EmpireId,planet.StarId))))continue;
            if (candidate.Colonies.Any(c => c.PlanetId == target))
            {
                vessel.PendingFounding = null; events.Add(new("founding_failed", before.EmpireId, before.Id, planet.StarId, target, Reason: "target_occupied")); continue;
            }
            var colony = new ColonyState { Crisis=Rules.HasCrises(candidate.RulesVersion)?new():null, Id = new(candidate.AllocateId()), PlanetId = target, EmpireId = before.EmpireId, Population = vessel.CarriedPopulation, Workforce = new(vessel.CarriedPopulation, 0, 0), HabitatMode = Rules.HasSpecies(snapshot.RulesVersion) ? (content.Vessels[before.RoleId].FoundsSealedHabitat ? Species.HabitatModes.Sealed : Species.HabitatModes.Native) : null };
            if (Rules.HasDevelopment(candidate.RulesVersion))
            {
                var owner = candidate.FindEmpire(colony.EmpireId)!;
                var inherited = owner.Controller == ControllerKind.Human ? owner.Development!.NewColonyPlan : null;
                colony.Development = new() { Template = inherited?.Template, WorkforceEnabled = inherited?.WorkforceEnabled ?? false, ConstructionEnabled = inherited?.ConstructionEnabled ?? false };
            }
            candidate.Colonies.Add(colony);
            if(context is not null)Crises.CrisisTransport.TransferFounding(content,snapshot,candidate,context,vessel,colony);
            candidate.Vessels.Remove(vessel);
            events.Add(new("colony_founded", before.EmpireId, before.Id, planet.StarId, target, colony.Id));
        }
    }
}
