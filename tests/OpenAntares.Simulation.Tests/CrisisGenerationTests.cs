using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisGenerationTests
{
    [Fact]
    public void ExplicitRules6StartsStandardAndOffWithoutChangingGalaxy()
    {
        var content=CrisisContentTests.Load(CrisisContentTests.Json());
        Assert.True(content.Succeeded,string.Join("; ",content.Errors));
        var game=GalaxyGenerator.CreateNewGame(content.Content!,new("small",42,6,"harl"));
        Assert.True(game.Succeeded,game.Error);
        Assert.Equal(10,Rules.CurrentVersion);
        var standard=GalaxyGenerator.CreateNewGame(content.Content!,new("small",42,6,"harl",1,"peldra"));
        var off=GalaxyGenerator.CreateNewGame(content.Content!,new("small",42,6,"harl",1,"peldra",CrisesEnabled:false));
        Assert.True(standard.Succeeded,standard.Error);Assert.True(off.Succeeded,off.Error);
        Assert.True(standard.State!.Crisis!.Enabled);Assert.False(off.State!.Crisis!.Enabled);
        Assert.Equal(Normalize(standard.State),Normalize(off.State));
        Assert.Equal(standard.State.Random.State,off.State.Random.State);
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

    [Theory][InlineData("harl")][InlineData("peldra")][InlineData("sulan")][InlineData("wenth")]
    public void CurrentCoreOffPreservesFrozenRules5OpeningAndFirstTurn(string species)
    {
        var core=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        var frozen=ContentLoader.Load(new[]{new ContentSource("frozen-rules5",File.ReadAllText(CompatibilityFixtures.PathFor("rules5-content.json")))},requireSpecies:true,requireRivals:true,requireDevelopment:true).Content!;
        var old=GalaxyGenerator.CreateNewGame(frozen,new("small",42,5,species,1,"peldra"));
        var current=GalaxyGenerator.CreateNewGame(core,new("small",42,6,species,1,"peldra",false));
        Assert.True(old.Succeeded,old.Error);Assert.True(current.Succeeded,current.Error);
        Assert.Equal(Normalize(old.State!),Normalize(current.State!));
        var previous=new GameSession(frozen,old.State!);var next=new GameSession(core,current.State!);
        Assert.True(previous.Execute(new SetReadyCommand(previous.State.Empires.Single(e=>e.Controller==ControllerKind.Human).Id,0,true)).Accepted);
        Assert.True(next.Execute(new SetReadyCommand(next.State.Empires.Single(e=>e.Controller==ControllerKind.Human).Id,0,true)).Accepted);
        Assert.Equal(Normalize(previous.State),Normalize(next.State));
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
