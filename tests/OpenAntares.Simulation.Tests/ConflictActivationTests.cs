using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictActivationTests
{
    [Fact]
    public void NewGamesUse12OldSavesRemainPeaceful11()
    {
        var content = ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        var current = GalaxyGenerator.CreateNewGame(content, new("small",42,SpeciesId:"harl",RivalCount:1,RivalSpeciesId:"wenth",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));
        Assert.True(current.Succeeded,current.Error);
        Assert.Equal(12,current.State!.RulesVersion);
        Assert.NotNull(current.State.Conflict);
        var old = GalaxyGenerator.CreateNewGame(content,new("small",42,11,"harl",1,"wenth",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));
        Assert.True(old.Succeeded,old.Error);
        Assert.Equal(old.State!.Random.State,current.State.Random.State);
        Assert.Equal(old.State.Random.Increment,current.State.Random.Increment);
        Assert.Equal(old.State.Stars.Select(s=>s.Id),current.State.Stars.Select(s=>s.Id));
        Assert.Equal(old.State.Vessels.Select(s=>s.Id),current.State.Vessels.Select(s=>s.Id));
        var manifest=JsonNode.Parse(File.ReadAllText(CompatibilityFixtures.PathFor("rules11-conflict-baselines/manifest.json")))!;
        var saved=SaveGame.ReadFile(CompatibilityFixtures.PathFor("rules11-conflict-baselines/"+manifest["cases"]![0]!["save"]!.GetValue<string>()));
        Assert.True(saved.Succeeded,string.Join("; ",saved.Errors));
        Assert.Equal(11,saved.Game!.State.RulesVersion);
        Assert.Null(saved.Game.State.Conflict);
        Assert.Equal(13,SaveGame.SchemaVersion);
    }
    [Fact]
    public void DefaultSoloRequiresConflictContentAndInventsNoOpponent()
    {
        var content = ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        var settings=new NewGameSettings("small",42,SpeciesId:"harl",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false);
        var solo=GalaxyGenerator.CreateNewGame(content,settings);
        Assert.True(solo.Succeeded,solo.Error);
        Assert.Single(solo.State!.Empires);
        Assert.NotNull(solo.State.Conflict);
        Assert.Empty(solo.State.Conflict!.Pairs);
        Assert.False(GalaxyGenerator.CreateNewGame(content with{Conflict=null},settings).Succeeded);
    }
}
