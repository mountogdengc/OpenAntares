using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Tests;

internal static class RivalFixtures
{
    internal static ContentSet Content() => ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content ?? throw new InvalidOperationException("Invalid core rival content.");

    internal static GameState AtHome(string speciesId)
    {
        var content = Content();
        var state = SpeciesFixtures.AtHome("harl");
        state.RulesVersion = 4;
        var home = state.Planets.Single(p => p.StarId != state.Stars[0].Id);
        home.PlanetTypeId = content.StartingColony.PlanetTypeId;
        home.EnvironmentId = "shelf_sea";
        var species = content.Species[speciesId];
        var ai = new EmpireState { Id = new(state.AllocateId()), Controller = ControllerKind.Ai, SpeciesId = speciesId, AiPolicyId = "expansion_v1" };
        state.Empires.Add(ai);
        state.Colonies.Add(new ColonyState { Id = new(state.AllocateId()), PlanetId = home.Id, EmpireId = ai.Id,
            Population = species.StartingPopulation, Workforce = species.StartingWorkforce, HabitatMode = "native" });
        state.Vessels.Add(new VesselState { Id = new(state.AllocateId()), EmpireId = ai.Id, RoleId = content.Expansion!.StartingVesselId, StationaryStarId = home.StarId });
        ObservationUpdater.Refresh(state, state.Turn);
        return state;
    }
}
