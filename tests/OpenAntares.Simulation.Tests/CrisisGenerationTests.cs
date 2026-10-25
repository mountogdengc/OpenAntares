using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisGenerationTests
{
    [Fact]
    public void ExplicitRules6CanStartWithoutChangingCurrentRules5()
    {
        var content=CrisisContentTests.Load(CrisisContentTests.Json());
        Assert.True(content.Succeeded,string.Join("; ",content.Errors));
        var game=GalaxyGenerator.CreateNewGame(content.Content!,new("small",42,6,"harl"));
        Assert.True(game.Succeeded,game.Error);
        Assert.Equal(5,Rules.CurrentVersion);
    }

    [Theory]
    [InlineData("harl",true)] [InlineData("peldra",true)] [InlineData("sulan",true)] [InlineData("wenth",true)]
    [InlineData("harl",false)] [InlineData("peldra",false)] [InlineData("sulan",false)] [InlineData("wenth",false)]
    public void GenerationPreservesGalaxyIdsAppearanceAndOriginalRandom(string species,bool enabled)
    {
        var content=CrisisFixtures.Content();
        var old=GalaxyGenerator.CreateNewGame(content,new("small",42,5,species,1,"peldra"));
        var current=GalaxyGenerator.CreateNewGame(content,new("small",42,6,species,1,"peldra",enabled));
        Assert.True(old.Succeeded,old.Error);Assert.True(current.Succeeded,current.Error);
        Assert.Equal(Normalize(old.State!),Normalize(current.State!));
        string before=JsonSerializer.Serialize(current.State);
        var session=new GameSession(content,current.State!);session.View(current.State!.Empires[0].Id);session.Forecast();
        Assert.Equal(before,JsonSerializer.Serialize(current.State));
    }

    private static string Normalize(GameState state)
    {
        var json=JsonSerializer.SerializeToNode(state)!.AsObject();json["RulesVersion"]=5;Strip(json);return json.ToJsonString();
        static void Strip(JsonNode node)
        {
            if(node is JsonObject obj){obj.Remove("Crisis");foreach(var p in obj)if(p.Value is {} child)Strip(child);}
            else if(node is JsonArray array)foreach(var child in array)if(child is not null)Strip(child);
        }
    }
}
