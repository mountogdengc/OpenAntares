using System;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class RivalGenerationTests
{
    private static NewGameSettings Settings(string size, ulong seed, int rules, string human, int count, string? rival) =>
        JsonSerializer.Deserialize<NewGameSettings>(JsonSerializer.Serialize(new { GalaxySizeId = size, Seed = seed, RulesVersion = rules, SpeciesId = human, RivalCount = count, RivalSpeciesId = rival }))!;

    [Theory]
    [InlineData("small")]
    [InlineData("medium")]
    [InlineData("large")]
    public void AllSpeciesPairsAndSizesHaveExactStarts(string size)
    {
        var content = RivalFixtures.Content(); string? facts = null;
        foreach (string human in new[] { "peldra", "harl", "sulan", "wenth" })
        foreach (string rival in new[] { "peldra", "harl", "sulan", "wenth" })
        {
            var settings = Settings(size, 42, 4, human, 1, rival);
            var result = GalaxyGenerator.CreateNewGame(content, settings); Assert.True(result.Succeeded, result.Error);
            var state = result.State!; Assert.Equal(2, state.Empires.Count); Assert.Equal(2, state.Colonies.Count); Assert.Equal(2, state.Vessels.Count);
            foreach (var empire in state.Empires)
            {
                var species = content.Species[empire.Controller == ControllerKind.Human ? human : rival];
                Assert.Equal(species.Id, empire.SpeciesId);
                Assert.Equal(empire.Controller == ControllerKind.Human ? null : "expansion_v1", empire.AiPolicyId);
                var colony = Assert.Single(state.Colonies, c => c.EmpireId == empire.Id);
                Assert.Equal(species.StartingPopulation, colony.Population); Assert.Equal(species.StartingWorkforce, colony.Workforce);
                Assert.Equal(species.BaseCapacity, Rules.ColonyCapacity(content, state, colony)); Assert.Equal("native", colony.HabitatMode);
                Assert.Equal(0, colony.GrowthProgress); Assert.Equal(0, colony.ProductionReserve); Assert.Null(colony.ProjectId); Assert.Empty(colony.CompletedBuildingIds);
                Assert.Equal(0, empire.ResearchReserve); Assert.Null(empire.ResearchTargetId); Assert.Empty(empire.KnownTechnologyIds); Assert.Empty(empire.Contacts);
                var home = state.FindPlanet(colony.PlanetId)!; Assert.Equal("shelf_sea", home.EnvironmentId); Assert.Equal(content.StartingColony.PlanetTypeId, home.PlanetTypeId);
                var scout = Assert.Single(state.Vessels, v => v.EmpireId == empire.Id); Assert.Equal(content.Expansion!.StartingVesselId, scout.RoleId); Assert.Equal(home.StarId, scout.StationaryStarId);
                Assert.Equal(0, scout.CarriedPopulation); Assert.Single(empire.Surveys);
            }
            Assert.NotEqual(state.FindPlanet(state.Colonies[0].PlanetId)!.StarId, state.FindPlanet(state.Colonies[1].PlanetId)!.StarId);
            string current = JsonSerializer.Serialize(new { state.Stars, state.Planets, state.Random, state.NextEntityId });
            if (facts is not null) Assert.Equal(facts, current); facts = current;
            Assert.Equal(JsonSerializer.Serialize(state), JsonSerializer.Serialize(GalaxyGenerator.CreateNewGame(content, settings).State));
            var view = EmpireViewProjector.Project(content, state, state.Empires[0].Id);
            var rivalStar = state.FindPlanet(state.Colonies[1].PlanetId)!.StarId;
            Assert.DoesNotContain(rivalStar, view.Galaxy.HomeMarkers); Assert.Null(view.Stars.Single(s => s.Chart.Id == rivalStar).Planets);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(999)]
    public void SoloMatchesRules3FactsIdsAndRandom(int seed)
    {
        var content = RivalFixtures.Content();
        var old = GalaxyGenerator.CreateNewGame(content, new("small", (ulong)seed, 3, "harl")).State!;
        var solo = GalaxyGenerator.CreateNewGame(content, Settings("small", (ulong)seed, 4, "harl", 0, null)); Assert.True(solo.Succeeded, solo.Error);
        var current = solo.State!; current.RulesVersion = 3;
        Assert.Equal(JsonSerializer.Serialize(old), JsonSerializer.Serialize(current));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(42, 0)]
    [InlineData(999, 0)]
    [InlineData(42, 2)]
    public void HomeDrawMatchesIndependentFarthestThirdReference(int seed, int count)
    {
        var content = RivalFixtures.Content();
        if (count == 2) content = content with { GalaxySizes = content.GalaxySizes.SetItem("small", content.GalaxySizes["small"] with { StarCount = 2 }) };
        var before = GalaxyGenerator.CreateNewGame(content, new("small", (ulong)seed, 3, "harl")).State!;
        var home = before.FindStar(before.FindPlanet(before.Colonies[0].PlanetId)!.StarId)!;
        Int128 Distance(StarState s) => ((Int128)s.X - home.X) * (s.X - (Int128)home.X) + ((Int128)s.Y - home.Y) * (s.Y - (Int128)home.Y);
        var eligible = before.Stars.Where(s => s.Id != home.Id && Distance(s) > 0).OrderByDescending(Distance).ThenBy(s => s.Id).ToArray();
        var pool = eligible.Take((eligible.Length + 2) / 3).OrderBy(s => s.Id).ToArray();
        var random = before.Random.Clone(); var chosen = pool[random.NextInt(pool.Length)];
        var result = GalaxyGenerator.CreateNewGame(content, Settings("small", (ulong)seed, 4, "harl", 1, "wenth")); Assert.True(result.Succeeded, result.Error);
        var state = result.State!; var ai = state.Empires.Single(e => e.Controller == ControllerKind.Ai); var colony = state.Colonies.Single(c => c.EmpireId == ai.Id);
        var planet = before.Planets.Where(p => p.StarId == chosen.Id).OrderBy(p => p.Orbit).ThenBy(p => p.Id).First();
        Assert.Equal(planet.Id, colony.PlanetId); Assert.Equal(before.NextEntityId, ai.Id.Value); Assert.Equal(before.NextEntityId + 1, colony.Id.Value);
        Assert.Equal(before.NextEntityId + 2, state.Vessels.Single(v => v.EmpireId == ai.Id).Id.Value); Assert.Equal(before.NextEntityId + 3, state.NextEntityId);
        Assert.Equal(random.State, state.Random.State); Assert.Equal(random.Increment, state.Random.Increment);
        planet.PlanetTypeId = content.StartingColony.PlanetTypeId; planet.EnvironmentId = "shelf_sea";
        Assert.Equal(JsonSerializer.Serialize(before.Planets), JsonSerializer.Serialize(state.Planets));
        Assert.Equal(JsonSerializer.Serialize(before.Stars), JsonSerializer.Serialize(state.Stars));
    }

    [Theory]
    [InlineData(4,-1,null)]
    [InlineData(4,2,"peldra")]
    [InlineData(4,0,"peldra")]
    [InlineData(4,1,null)]
    [InlineData(4,1,"missing")]
    [InlineData(3,1,"peldra")]
    [InlineData(2,1,"peldra")]
    [InlineData(1,1,"peldra")]
    public void BadCountSpeciesOrLegacyRivalPublishesNothing(int rules, int count, string? rival)
    {
        var result = GalaxyGenerator.CreateNewGame(RivalFixtures.Content(), Settings("small", 42, rules, "harl", count, rival));
        Assert.False(result.Succeeded); Assert.Null(result.State); Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public void SoloOneSystemSucceedsButRivalNeedsDistinctHomeAndPolicy()
    {
        var content = RivalFixtures.Content();
        content = content with { GalaxySizes = content.GalaxySizes.SetItem("small", content.GalaxySizes["small"] with { StarCount = 1 }) };
        Assert.True(GalaxyGenerator.CreateNewGame(content, Settings("small", 42, 4, "harl", 0, null)).Succeeded);
        var rival = GalaxyGenerator.CreateNewGame(content, Settings("small", 42, 4, "harl", 1, "peldra"));
        Assert.False(rival.Succeeded); Assert.Null(rival.State);
        Assert.False(GalaxyGenerator.CreateNewGame(content with { RivalAi = null }, Settings("small", 42, 4, "harl", 0, null)).Succeeded);
        content = content with { Galaxy = content.Galaxy with { MinStarDistance = 0 }, GalaxySizes = content.GalaxySizes.SetItem("small", content.GalaxySizes["small"] with { StarCount = 2, Width = 1, Height = 1 }) };
        Assert.False(GalaxyGenerator.CreateNewGame(content, Settings("small", 42, 4, "harl", 1, "peldra")).Succeeded);
    }
}
