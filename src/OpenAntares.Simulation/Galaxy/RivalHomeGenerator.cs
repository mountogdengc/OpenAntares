using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Galaxy;

/// <summary>Adds one rival after the unchanged base generation sequence; never rerolls a map.</summary>
internal static class RivalHomeGenerator
{
    internal static string? Add(ContentSet content, GameState state, string rivalSpeciesId)
    {
        var humanHome = state.FindStar(state.FindPlanet(state.Colonies[0].PlanetId)!.StarId)!;
        Int128 Distance(StarState star)
        {
            Int128 dx = (Int128)star.X - humanHome.X, dy = (Int128)star.Y - humanHome.Y;
            return dx * dx + dy * dy;
        }
        var eligible = state.Stars.Where(s => s.Id != humanHome.Id && Distance(s) > 0)
            .OrderByDescending(Distance).ThenBy(s => s.Id).ToArray();
        if (eligible.Length == 0) return "A rival needs a distinct, non-collocated home system. Choose Solo or a larger map.";
        var pool = eligible.Take((eligible.Length + 2) / 3).OrderBy(s => s.Id).ToArray();
        // Exactly one bounded invocation even when the retained pool has just one star.
        var home = pool[state.Random.NextInt(pool.Length)];
        var planet = state.Planets.Where(p => p.StarId == home.Id).OrderBy(p => p.Orbit).ThenBy(p => p.Id).FirstOrDefault();
        if (planet is null) return "The selected rival home has no planet. Choose Solo or correct the galaxy content.";
        planet.PlanetTypeId = content.StartingColony.PlanetTypeId;
        planet.EnvironmentId = "shelf_sea";
        var species = content.Species[rivalSpeciesId];
        var empire = new EmpireState { Id = new(state.AllocateId()), Controller = ControllerKind.Ai,
            SpeciesId = species.Id, AiPolicyId = content.RivalAi!.AlgorithmId };
        state.Empires.Add(empire);
        state.Colonies.Add(new ColonyState { Id = new(state.AllocateId()), PlanetId = planet.Id, EmpireId = empire.Id,
            Population = species.StartingPopulation, Workforce = species.StartingWorkforce, HabitatMode = Species.HabitatModes.Native });
        state.Vessels.Add(new VesselState { Id = new(state.AllocateId()), EmpireId = empire.Id,
            RoleId = content.Expansion!.StartingVesselId, StationaryStarId = home.Id });
        return null;
    }
}
