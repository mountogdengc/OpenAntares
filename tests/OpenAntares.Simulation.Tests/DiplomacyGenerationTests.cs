using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacyGenerationTests
{
    [Theory][InlineData("harl",0)][InlineData("peldra",0)][InlineData("sulan",0)][InlineData("wenth",0)][InlineData("harl",1)][InlineData("peldra",1)][InlineData("sulan",1)][InlineData("wenth",1)]
    public void Rules11GenerationPreservesGalaxyIdsAndRandom(string species,int rivals)
    {
        var c=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        NewGameSettings Settings(int version)=>new("small",42,version,species,rivals,rivals==0?null:"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false);
        var old=GalaxyGenerator.CreateNewGame(c,Settings(10));var current=GalaxyGenerator.CreateNewGame(c,Settings(11));Assert.True(current.Succeeded,current.Error);
        var a=old.State!;var b=current.State!;Assert.Equal(JsonSerializer.Serialize(a.Stars),JsonSerializer.Serialize(b.Stars));Assert.Equal(JsonSerializer.Serialize(a.Planets),JsonSerializer.Serialize(b.Planets));Assert.Equal(a.NextEntityId,b.NextEntityId);Assert.Equal(JsonSerializer.Serialize(a.Random),JsonSerializer.Serialize(b.Random));Assert.Null(a.Diplomacy);Assert.NotNull(b.Diplomacy);Assert.Empty(b.Diplomacy!.Pairs);Assert.Empty(GameStateValidator.Validate(c,b));
        var defaults=GalaxyGenerator.CreateNewGame(c,new("small",42,SpeciesId:species,RivalCount:rivals,RivalSpeciesId:rivals==0?null:"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));Assert.True(defaults.Succeeded,defaults.Error);Assert.Equal(12,defaults.State!.RulesVersion);
    }
    [Fact]public void SoloStillRequiresDiplomacyDefinition()
    {
        var c=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content! with{Diplomacy=null};var result=GalaxyGenerator.CreateNewGame(c,new("small",42,11,"harl",0,null,CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));Assert.False(result.Succeeded);Assert.Contains("diplomacy",result.Error!,System.StringComparison.OrdinalIgnoreCase);
    }
}
