using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;
using static OpenAntares.Simulation.Tests.Fixtures;

namespace OpenAntares.Simulation.Tests;

public class GalaxyGeneratorTests
{
    private readonly ContentSet _content = PrototypeContent();

    [Theory]
    [InlineData("small", 1UL)]
    [InlineData("medium", 2UL)]
    [InlineData("large", 3UL)]
    [InlineData("large", ulong.MaxValue)]
    public void GeneratedGameFollowsTheGalaxyRules(string sizeId, ulong seed)
    {
        GalaxySizeDefinition size = _content.GalaxySizes[sizeId];
        GalaxyRules rules = _content.Galaxy;

        GameState state = Generate(sizeId, seed);

        Assert.Empty(GameStateValidator.Validate(_content, state));
        Assert.Equal(0, state.Turn);
        Assert.Equal(size.StarCount, state.Stars.Count);
        Assert.All(state.Stars, star =>
        {
            Assert.InRange(star.X, 0, size.Width - 1);
            Assert.InRange(star.Y, 0, size.Height - 1);
            Assert.Contains(star.Name, rules.StarNames);
        });
        Assert.Equal(state.Stars.Count, state.Stars.Select(s => s.Name).Distinct().Count());

        long minSquared = (long)rules.MinStarDistance * rules.MinStarDistance;
        foreach (StarState a in state.Stars)
        {
            foreach (StarState b in state.Stars.Where(b => b.Id.Value > a.Id.Value))
            {
                long dx = a.X - b.X;
                long dy = a.Y - b.Y;
                Assert.True(dx * dx + dy * dy >= minSquared, $"{a.Name} and {b.Name} are too close.");
            }
        }

        foreach (StarState star in state.Stars)
        {
            int[] orbits = state.Planets.Where(p => p.StarId == star.Id).Select(p => p.Orbit).ToArray();
            Assert.InRange(orbits.Length, rules.MinPlanetsPerStar, rules.MaxPlanetsPerStar);
            Assert.Equal(Enumerable.Range(1, orbits.Length), orbits);
        }
    }

    [Fact]
    public void StartsOneHumanEmpireWithTheStartingColony()
    {
        GameState state = Generate("small", 42);

        EmpireState empire = Assert.Single(state.Empires);
        Assert.Equal(ControllerKind.Human, empire.Controller);
        Assert.False(empire.Ready);
        Assert.Empty(empire.KnownTechnologyIds);
        Assert.Equal(0, empire.ResearchReserve);

        ColonyState colony = Assert.Single(state.Colonies);
        StartingColonyDefinition start = _content.StartingColony;
        Assert.Equal(empire.Id, colony.EmpireId);
        Assert.Equal(start.Population, colony.Population);
        Assert.Equal(start.Workforce, colony.Workforce);
        Assert.Equal(start.PlanetTypeId, state.FindPlanet(colony.PlanetId)!.PlanetTypeId);
    }

    [Fact]
    public void SameSeedGivesTheSameGalaxy()
    {
        Assert.Equal(Snapshot(Generate("medium", 1234)), Snapshot(Generate("medium", 1234)));
    }

    [Fact]
    public void DifferentSeedsGiveDifferentGalaxies()
    {
        Assert.NotEqual(Snapshot(Generate("medium", 1)), Snapshot(Generate("medium", 2)));
    }

    [Fact]
    public void PlanetTypesWithZeroWeightAreNeverGenerated()
    {
        var neverBarren = _content.PlanetTypes["barren_rock"] with { GenerationWeight = 0 };
        ContentSet content = _content with { PlanetTypes = _content.PlanetTypes.SetItem(neverBarren.Id, neverBarren) };

        for (ulong seed = 0; seed < 20; seed++)
        {
            GameState state = GalaxyGenerator.CreateNewGame(content, new NewGameSettings("large", seed)).State!;
            Assert.DoesNotContain(state.Planets, p => p.PlanetTypeId == "barren_rock");
        }
    }

    [Fact]
    public void EveryWeightedPlanetTypeAppearsAcrossSeeds()
    {
        var seen = Enumerable.Range(0, 10)
            .SelectMany(seed => Generate("large", (ulong)seed).Planets.Select(p => p.PlanetTypeId))
            .ToHashSet();

        Assert.Subset(seen, _content.PlanetTypes.Keys.ToHashSet());
        Assert.Equal(_content.PlanetTypes.Count, seen.Count);
    }

    [Fact]
    public void UnknownGalaxySizeIsReported()
    {
        NewGameResult result = GalaxyGenerator.CreateNewGame(_content, new NewGameSettings("gigantic", 1));

        Assert.False(result.Succeeded);
        Assert.Contains("'gigantic' is not defined", result.Error);
    }

    [Fact]
    public void ImpossibleSpacingIsReported()
    {
        var cramped = new GalaxySizeDefinition("cramped", "Cramped", 12, 100, 100);
        ContentSet content = _content with { GalaxySizes = _content.GalaxySizes.Add(cramped.Id, cramped) };

        NewGameResult result = GalaxyGenerator.CreateNewGame(content, new NewGameSettings("cramped", 1));

        Assert.False(result.Succeeded);
        Assert.Contains("Could not place 12 stars at least 120 apart", result.Error);
    }

    [Fact]
    public void GeneratedGameCanBePlayedSavedAndReloaded()
    {
        GameState state = Generate("small", 7);
        EmpireId empire = state.Empires[0].Id;
        ColonyId colony = state.Colonies[0].Id;
        state = CommandProcessor.Execute(_content, state, new SetProjectCommand(empire, 0, colony, "fabrication_hall")).State;
        CommandOutcome outcome = CommandProcessor.Execute(_content, state, new SetReadyCommand(empire, 0, true));
        Assert.True(outcome.Accepted, outcome.Rejection?.Message);

        SaveLoadResult loaded = SaveGame.Deserialize(SaveGame.Serialize(_content, outcome.State));

        Assert.True(loaded.Succeeded, string.Join("\n", loaded.Errors));
        Assert.Equal(Snapshot(outcome.State), Snapshot(loaded.Game!.State));
    }

    [Fact]
    public void CoreContentGeneratesEverySize()
    {
        ContentSet core = ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;

        foreach (string sizeId in core.GalaxySizes.Keys)
        {
            NewGameResult result = GalaxyGenerator.CreateNewGame(core, new NewGameSettings(sizeId, 99));
            Assert.True(result.Succeeded, result.Error);
        }
    }

    private GameState Generate(string sizeId, ulong seed)
    {
        NewGameResult result = GalaxyGenerator.CreateNewGame(_content, new NewGameSettings(sizeId, seed));
        Assert.True(result.Succeeded, result.Error);
        return result.State!;
    }
}
