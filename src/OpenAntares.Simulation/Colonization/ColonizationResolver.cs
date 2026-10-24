using System.Linq;
using System.Collections.Generic;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
namespace OpenAntares.Simulation.Colonization;

public static class ColonizationResolver
{
    public static void Apply(ContentSet content, GameState snapshot, GameState candidate, List<ExpansionEvent> events)
    {
        foreach (var before in snapshot.Vessels.Where(v => v.PendingFounding.HasValue).OrderBy(v => v.EmpireId).ThenBy(v => v.Id))
        {
            var target = before.PendingFounding!.Value; var vessel = candidate.FindVessel(before.Id)!; var planet = candidate.FindPlanet(target)!;
            if (candidate.Colonies.Any(c => c.PlanetId == target))
            {
                vessel.PendingFounding = null; events.Add(new("founding_failed", before.EmpireId, before.Id, planet.StarId, target, Reason: "target_occupied")); continue;
            }
            var colony = new ColonyState { Id = new(candidate.AllocateId()), PlanetId = target, EmpireId = before.EmpireId, Population = vessel.CarriedPopulation, Workforce = new(vessel.CarriedPopulation, 0, 0), HabitatMode = Rules.HasSpecies(snapshot.RulesVersion) ? (content.Vessels[before.RoleId].FoundsSealedHabitat ? Species.HabitatModes.Sealed : Species.HabitatModes.Native) : null };
            if (Rules.HasDevelopment(candidate.RulesVersion))
            {
                var owner = candidate.FindEmpire(colony.EmpireId)!;
                var inherited = owner.Controller == ControllerKind.Human ? owner.Development!.NewColonyPlan : null;
                colony.Development = new() { Template = inherited?.Template, WorkforceEnabled = inherited?.WorkforceEnabled ?? false, ConstructionEnabled = inherited?.ConstructionEnabled ?? false };
            }
            candidate.Colonies.Add(colony); candidate.Vessels.Remove(vessel);
            events.Add(new("colony_founded", before.EmpireId, before.Id, planet.StarId, target, colony.Id));
        }
    }
}
