using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Species;
namespace OpenAntares.Simulation.State;

internal static class SpeciesStateValidator
{
    internal static void Validate(ContentSet content, GameState state, List<string> errors)
    {
        if (state.RulesVersion != Rules.SpeciesVersion)
        {
            if (state.Empires.Any(e => e.SpeciesId is not null || e.Surveys.Any(o => o.Planets.Any(p => p.EnvironmentId is not null))) || state.Planets.Any(p => p.EnvironmentId is not null) || state.Colonies.Any(c => c.HabitatMode is not null))
                errors.Add("Legacy campaigns cannot contain species/environment/habitat metadata.");
            return;
        }
        if (content.Species.IsEmpty || content.Environments.IsEmpty) errors.Add("Species rules require species/environment content.");
        foreach (var empire in state.Empires)
        {
            if (SpeciesRules.Definition(content,state,empire.Id) is null) errors.Add($"{empire.Id}: species_id is missing or undefined.");
            foreach (var observation in empire.Surveys)
                foreach (var observed in observation.Planets)
                    if (observed.EnvironmentId is not { } id || !content.Environments.ContainsKey(id) || state.FindPlanet(observed.PlanetId)?.EnvironmentId != id)
                        errors.Add($"{empire.Id}: observed environment_id is missing, undefined or false.");
        }
        foreach (var planet in state.Planets)
            if (planet.EnvironmentId is not { } id || !content.Environments.ContainsKey(id)) errors.Add($"{planet.Id}: environment_id is missing or undefined.");
        foreach (var colony in state.Colonies)
        {
            var environment = state.FindPlanet(colony.PlanetId)?.EnvironmentId;
            if (environment is null || colony.HabitatMode is not { } mode || !SpeciesRules.HabitatAllowed(content,state,colony.EmpireId,environment,mode)) errors.Add($"{colony.Id}: habitat_mode is missing or not available for species/environment.");
            if (colony.ProjectKind == ProductionKinds.Vessel && colony.ProjectId is { } role && !SpeciesRules.CanBuildVessel(content,state,colony.EmpireId,role)) errors.Add($"{colony.Id}: project not available for species.");
        }
        foreach (var vessel in state.Vessels)
        {
            if (!SpeciesRules.CanBuildVessel(content,state,vessel.EmpireId,vessel.RoleId)) errors.Add($"{vessel.Id}: role not available for species.");
            if (!content.Vessels.TryGetValue(vessel.RoleId,out var role) || !role.CanFoundColony) continue;
            string mode = role.FoundsSealedHabitat ? HabitatModes.Sealed : HabitatModes.Native;
            var profile = SpeciesRules.Definition(content,state,vessel.EmpireId);
            if (profile is null || (role.FoundsSealedHabitat && !profile.AllowsSealedHabitats) || vessel.CarriedPopulation > SpeciesRules.BaseCapacity(content,state,vessel.EmpireId,mode)) errors.Add($"{vessel.Id}: founding cargo/mode cannot fit species habitat.");
            if (vessel.PendingFounding is { } target && (state.FindPlanet(target)?.EnvironmentId is not { } environment || !SpeciesRules.HabitatAllowed(content,state,vessel.EmpireId,environment,mode))) errors.Add($"{vessel.Id}: founding habitat is not suitable.");
        }
    }
}
