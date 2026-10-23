using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using Xunit;
namespace OpenAntares.Simulation.Tests;

public class ExpansionGenerationTests
{
    [Theory]
    [InlineData("small")]
    [InlineData("medium")]
    [InlineData("large")]
    public void ExpansionGenerationStartsOneScoutAndHomeSurvey(string size)
    {
        var c = ExpansionFixtures.Content(); var a = GalaxyGenerator.CreateNewGame(c, new(size, 42, 1)).State!; var result = GalaxyGenerator.CreateNewGame(c, new(size, 42, 2)); Assert.True(result.Succeeded, result.Error); var b = result.State!;
        Assert.Equal(2, b.RulesVersion); Assert.Equal(JsonSerializer.Serialize(a.Stars), JsonSerializer.Serialize(b.Stars)); Assert.Equal(JsonSerializer.Serialize(a.Planets), JsonSerializer.Serialize(b.Planets)); Assert.Equal(a.Random.State, b.Random.State); Assert.Equal(a.Random.Increment, b.Random.Increment);
        var scout = Assert.Single(b.Vessels); Assert.Equal("scout", scout.RoleId); Assert.Equal(0, scout.CarriedPopulation); Assert.Equal(a.NextEntityId, scout.Id.Value); Assert.Equal(a.NextEntityId + 1, b.NextEntityId);
        var survey = Assert.Single(b.Empires[0].Surveys); Assert.Equal(scout.StationaryStarId, survey.StarId); Assert.Equal(0, survey.LastObservedTurn);
        Assert.Equal(1, EmpireViewProjector.Project(c, b, b.Empires[0].Id).Stars.Count(s => s.Planets.HasValue));
    }
    [Fact]
    public void ExpansionRequiresCompleteContentAndSupportedRules()
    {
        Assert.False(GalaxyGenerator.CreateNewGame(Fixtures.PrototypeContent(), new("small", 42, 2)).Succeeded);
        Assert.False(GalaxyGenerator.CreateNewGame(ExpansionFixtures.Content(), new("small", 42, 3)).Succeeded);
    }
}
