using System.Linq;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisStateTests
{
    [Fact]
    public void IndependentStreamMatchesLiteralReferenceVector()
    {
        var rng=CrisisInitializer.CreateRandom(42,new(1));
        Assert.Equal(11158500043467840445UL,rng.State);Assert.Equal(11572142791786627073UL,rng.Increment);
        Assert.Equal(new uint[]{1215802219,315086694,2652593244,2676242757},Enumerable.Range(0,4).Select(_=>rng.NextUInt32()).ToArray());
        Assert.Equal(77,CrisisInitializer.CreateRandom(42,new(46)).NextInt(100));
        Assert.False(Rules.HasCrises(5));Assert.True(Rules.HasCrises(6));
    }
    [Fact]
    public void NewHumanIsManualAndRequiresAcknowledgement()
    {
        var g=CrisisFixtures.Game();Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        var crisis=g.State.Empires[0].Crisis!;
        Assert.Equal(CrisisAuthority.Manual,crisis.Authority);Assert.True(crisis.RequireAcknowledgement);Assert.Null(crisis.Episode);
        Assert.Equal(1,crisis.NextEpisodeNumber);Assert.Empty(crisis.PendingReceipts);
    }
    [Fact]
    public void CloneOwnsMutableCrisisNodesAndLists()
    {
        var g=CrisisFixtures.Active();var copy=g.State.Clone();
        copy.Empires[0].Crisis!.Random.NextUInt32();copy.Empires[0].Crisis!.Episode!.CureProgress=8;
        copy.Empires[0].Crisis!.LostColonyLocations.Add(CrisisFixtures.Location(g.State,g.State.Colonies[0]));
        copy.Colonies[0].Crisis!.Case!.Severity=99;copy.Vessels[0].Crisis!.InspectionDueTurn=30;
        Assert.NotEqual(copy.Empires[0].Crisis!.Random.State,g.State.Empires[0].Crisis!.Random.State);
        Assert.Equal(0,g.State.Empires[0].Crisis!.Episode!.CureProgress);Assert.Empty(g.State.Empires[0].Crisis!.LostColonyLocations);
        Assert.Equal(20,g.State.Colonies[0].Crisis!.Case!.Severity);Assert.Null(g.State.Vessels[0].Crisis!.InspectionDueTurn);
    }
    [Theory]
    [InlineData("stream")] [InlineData("episode")] [InlineData("release")] [InlineData("counter")] [InlineData("expiry")] [InlineData("revision")] [InlineData("hold")]
    public void InvalidOwnedCrisisFactsAreRejected(string mutation)
    {
        var g=CrisisFixtures.Active();var e=g.State.Empires[0].Crisis!;var c=g.State.Colonies[0].Crisis!;
        Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        switch(mutation){case "stream":e.Random.Increment=2;break;case "episode":c.Case!.EpisodeId=7;break;case "release":c.ReleaseEpisode=7;break;case "counter":e.NextEpisodeNumber=long.MaxValue;break;case "expiry":e.CivicLevel=1;e.CivicExpiresAt=int.MaxValue;break;case "revision":c.Case!.AttentionRevision=long.MaxValue;break;case "hold":g.State.Vessels[0].Crisis!.InspectionDueTurn=g.State.Turn;break;}
        Assert.NotEmpty(GameStateValidator.Validate(g.Content,g.State));
    }
    [Fact]
    public void OffMustRemainDormantAndZeroColoniesAreLegal()
    {
        var g=CrisisFixtures.Game(enabled:false);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        g.State.Empires[0].Crisis!.Authority=CrisisAuthority.Contain;Assert.NotEmpty(GameStateValidator.Validate(g.Content,g.State));
        g=CrisisFixtures.Game();g.State.Colonies.Clear();OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(g.State,g.State.Turn);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
    }
    [Fact]
    public void EveryLabelledFixtureStartsValid()
    {
        foreach(var g in new[]{CrisisFixtures.Active(),CrisisFixtures.Warning(),CrisisFixtures.TwoHumansAndAi(),CrisisFixtures.CargoArrival(),CrisisFixtures.CargoArrival(true),CrisisFixtures.CargoArrival(found:true)})
            Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
    }
}
