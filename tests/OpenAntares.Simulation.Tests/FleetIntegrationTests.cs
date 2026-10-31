using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetIntegrationTests
{
    [Fact]
    public void ExplicitRulesEightKeepsItsOriginalSetupChoices()
    {
        var c=FleetContentTests.Core();var settings=new NewGameSettings("small",42,8,SpeciesId:"harl",RivalCount:1,RivalSpeciesId:"peldra",AmebasEnabled:true);
        var made=GalaxyGenerator.CreateNewGame(c,settings);Assert.True(made.Succeeded,made.Error);var s=made.State!;
        Assert.Equal(8,s.RulesVersion);Assert.Equal("harl",s.Empires[0].SpeciesId);Assert.Equal("peldra",s.Empires[1].SpeciesId);
        Assert.True(settings.CrisesEnabled);Assert.NotNull(s.Crisis);Assert.NotNull(s.Scouting!.Habitat);Assert.All(s.Empires,e=>Assert.NotNull(e.Fleets));
        using var saved=JsonDocument.Parse(SaveGame.Serialize(c,s));Assert.Equal(12,saved.RootElement.GetProperty("schema_version").GetInt32());
        var old=GalaxyGenerator.CreateNewGame(c,settings with{RulesVersion=7});Assert.True(old.Succeeded);var loaded=SaveGame.Deserialize(SaveGame.Serialize(c,old.State!));Assert.True(loaded.Succeeded,string.Join(";",loaded.Errors));Assert.Equal(7,loaded.Game!.State.RulesVersion);Assert.All(loaded.Game.State.Empires,e=>Assert.Null(e.Fleets));
    }
}
