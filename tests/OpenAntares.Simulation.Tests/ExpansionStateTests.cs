using System;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;

public class ExpansionStateTests
{
    [Fact]
    public void ExpansionFixtureIsValidAndCloneIsIndependent()
    {
        var s = ExpansionFixtures.AtHome(); Assert.Empty(GameStateValidator.Validate(ExpansionFixtures.Content(), s));
        var copy = s.Clone(); copy.Vessels[0].PendingMove = new(6); copy.Empires[0].Surveys[0].Planets.Clear();
        Assert.Null(s.Vessels[0].PendingMove); Assert.Single(s.Empires[0].Surveys[0].Planets);
    }
    [Theory][InlineData(0)][InlineData(3)] public void UnsupportedRulesReject(int version) { var s = ExpansionFixtures.AtHome(); s.RulesVersion = version; Assert.NotEmpty(GameStateValidator.Validate(ExpansionFixtures.Content(), s)); }
    [Fact] public void SharedIdsIncludeVessels() { var s = ExpansionFixtures.AtHome(); s.Vessels[0].Id = new(4); Assert.NotEmpty(GameStateValidator.Validate(ExpansionFixtures.Content(), s)); }
    [Theory]
    [InlineData("both-location")]
    [InlineData("cargo")]
    [InlineData("order")]
    [InlineData("future-knowledge")]
    [InlineData("bad-role")]
    public void InvalidExtensionRejects(string kind)
    {
        var s = ExpansionFixtures.AtHome(); var v = s.Vessels[0];
        switch (kind) { case "both-location": v.Transit = new(Fixtures.HomeStar, new(6), 2, 1); break; case "cargo": v.CarriedPopulation = 2; break; case "order": v.PendingMove = new(6); v.PendingFounding = new(7); break; case "future-knowledge": s.Empires[0].Surveys[0].LastObservedTurn = 1; break; case "bad-role": v.RoleId = "missing"; break; }
        Assert.NotEmpty(GameStateValidator.Validate(ExpansionFixtures.Content(), s));
    }
    [Fact]
    public void PublishedAllReadyRejectsButResolvingFlagIsNarrow()
    {
        var s = ExpansionFixtures.AtHome(); s.Empires[0].Ready = true;
        Assert.NotEmpty(GameStateValidator.Validate(ExpansionFixtures.Content(), s));
        Assert.Empty(GameStateValidator.Validate(ExpansionFixtures.Content(), s, allowAllHumansReady: true));
        s.Vessels[0].CarriedPopulation = 1; Assert.NotEmpty(GameStateValidator.Validate(ExpansionFixtures.Content(), s, true));
    }
    [Fact] public void LegacyStateCannotContainExpansionEntities() { var s = ExpansionFixtures.AtHome(); s.RulesVersion = 1; Assert.NotEmpty(GameStateValidator.Validate(ExpansionFixtures.Content(), s)); }
    [Fact] public void IdAllocationOverflowDoesNotWrap() { var s = ExpansionFixtures.AtHome(); s.NextEntityId = int.MaxValue; Assert.Throws<OverflowException>(() => s.AllocateId()); Assert.Equal(int.MaxValue, s.NextEntityId); }
}
