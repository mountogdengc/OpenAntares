using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Galaxy;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class SpeciesGenerationTests
{
    internal static NewGameSettings Settings(string size,ulong seed,string? species) => JsonSerializer.Deserialize<NewGameSettings>(JsonSerializer.Serialize(new{GalaxySizeId=size,Seed=seed,RulesVersion=3,SpeciesId=species}))!;
    [Theory][InlineData("small")][InlineData("medium")][InlineData("large")]
    public void AllFourSpeciesHaveExactOpeningsAndIdenticalGalaxyFacts(string size)
    {
        var content=SpeciesFixtures.Content();string? facts=null;
        foreach(string id in new[]{"peldra","harl","sulan","wenth"})
        {
            var result=GalaxyGenerator.CreateNewGame(content,Settings(size,42,id));Assert.True(result.Succeeded,result.Error);var s=result.State!;var c=Assert.Single(s.Colonies);
            var sp=content.Species[id];Assert.Equal(sp.StartingPopulation,c.Population);Assert.Equal(sp.StartingWorkforce,c.Workforce);Assert.Equal(0,c.GrowthProgress);Assert.Equal(0,c.ProductionReserve);Assert.Empty(c.CompletedBuildingIds);
            Assert.Equal(id,JsonSerializer.SerializeToElement(s.Empires[0]).GetProperty("SpeciesId").GetString());
            Assert.Equal("shelf_sea",JsonSerializer.SerializeToElement(s.FindPlanet(c.PlanetId)).GetProperty("EnvironmentId").GetString());
            Assert.Single(s.Vessels);Assert.Equal("scout",s.Vessels[0].RoleId);Assert.Single(s.Empires[0].Surveys);
            string snapshot=JsonSerializer.Serialize(new{s.Stars,s.Planets,s.Random,s.NextEntityId});if(facts is not null)Assert.Equal(facts,snapshot);facts=snapshot;
            var again=GalaxyGenerator.CreateNewGame(content,Settings(size,42,id));Assert.True(again.Succeeded,again.Error);Assert.Equal(SpeciesFixtures.Snapshot(s),SpeciesFixtures.Snapshot(again.State!));
        }
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(42)][InlineData(999)]
    public void EnvironmentSamplingConsumesOneOrderedPassAfterBaseGeneration(int seed)
    {
        var content=SpeciesFixtures.Content();var original=GalaxyGenerator.CreateNewGame(content,new("small",(ulong)seed,2));Assert.True(original.Succeeded,original.Error);
        var current=GalaxyGenerator.CreateNewGame(content,Settings("small",(ulong)seed,"harl"));Assert.True(current.Succeeded,current.Error);
        var before=original.State!;var after=current.State!;
        Assert.Equal(JsonSerializer.Serialize(before.Stars),JsonSerializer.Serialize(after.Stars));
        Assert.Equal(before.Planets.Select(p=>(p.Id,p.StarId,p.Orbit,p.PlanetTypeId)),after.Planets.Select(p=>(p.Id,p.StarId,p.Orbit,p.PlanetTypeId)));
        var rng=before.Random.Clone();var home=before.Colonies[0].PlanetId;
        foreach(var planet in before.Planets.OrderBy(p=>p.Id))
        {
            int draw=rng.NextInt(8);int dry=planet.PlanetTypeId switch{"verdant_world"=>2,"forge_world"=>6,"crystal_world"=>4,_=>8};
            int ocean=planet.PlanetTypeId switch{"verdant_world"=>2,"forge_world"=>1,"crystal_world"=>3,_=>0};
            string expected=planet.Id==home?"shelf_sea":draw<dry?"dry":draw<dry+ocean?"open_ocean":"shelf_sea";
            Assert.Equal(expected,JsonSerializer.SerializeToElement(after.FindPlanet(planet.Id)).GetProperty("EnvironmentId").GetString());
        }
        Assert.Equal(rng.State,after.Random.State);Assert.Equal(rng.Increment,after.Random.Increment);
    }
    [Theory][InlineData(null)][InlineData("missing")]
    public void InvalidSpeciesPublishesNoNewGame(string? id)
    {
        var result=GalaxyGenerator.CreateNewGame(SpeciesFixtures.Content(),Settings("small",42,id));Assert.False(result.Succeeded);Assert.Null(result.State);Assert.Contains("species",result.Error!,System.StringComparison.OrdinalIgnoreCase);
    }
}
