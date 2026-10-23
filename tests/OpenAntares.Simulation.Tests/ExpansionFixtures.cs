using System;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Tests;

internal static class ExpansionFixtures
{
    public static ContentSet Content() => Fixtures.PrototypeContent() with
    {
        PlanetTypes = Fixtures.PrototypeContent().PlanetTypes.ToImmutableSortedDictionary(p => p.Key, p => p.Value with { Colonizable = true }, StringComparer.Ordinal),
        Vessels = new[] { new VesselDefinition("scout", "Scout", 800, 300, 0, 0, false), new VesselDefinition("settler", "Settler", 2400, 200, 2, 4, true) }.ToImmutableSortedDictionary(v => v.Id, v => v, StringComparer.Ordinal),
        Expansion = new ExpansionDefinition("scout")
    };
    public static GameState AtHome()
    {
        var s = Fixtures.StartingState(); s.RulesVersion = 2;
        s.Stars[0].X = 0; s.Stars[0].Y = 0;
        s.Vessels.Add(new VesselState { Id = new(5), EmpireId = Fixtures.Empire, RoleId = "scout", StationaryStarId = Fixtures.HomeStar });
        s.Stars.Add(new StarState { Id = new(6), Name = "Remote", X = 300, Y = 0 });
        s.Planets.Add(new PlanetState { Id = new(7), StarId = new(6), Orbit = 1, PlanetTypeId = "forge_world" });
        s.Empires[0].Surveys.Add(new SystemObservation { StarId = Fixtures.HomeStar, LastObservedTurn = 0, Planets = { new PlanetObservation(Fixtures.HomePlanet, 1, "verdant_world", Fixtures.HomeColony, Fixtures.Empire) } });
        s.NextEntityId = 8; return s;
    }
}
