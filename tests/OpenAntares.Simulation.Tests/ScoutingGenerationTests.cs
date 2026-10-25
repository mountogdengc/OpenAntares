using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ScoutingGenerationTests
{
    [Fact] public void PublishedIndependentVectorsMatch()
    {
        var placement=ScoutInitializer.PlacementRandom(42);Assert.Equal(15004228035630008857UL,placement.State);Assert.Equal(10413031053861234691UL,placement.Increment);Assert.Equal(new[]{1,0,1,1},Enumerable.Range(0,4).Select(_=>placement.NextInt(2)));
        foreach(var row in new[]{(1,13725908560590527421UL,9990820718965293057UL,new[]{55,85,11,23}),(10,6162952895543560625UL,9990820718965293079UL,new[]{67,67,32,1})})
        {var rng=ScoutInitializer.RiskRandom(42,new(row.Item1));Assert.Equal(row.Item2,rng.State);Assert.Equal(row.Item3,rng.Increment);Assert.Equal(row.Item4,Enumerable.Range(0,4).Select(_=>rng.NextInt(100)));}
    }
    [Fact] public void EverySpeciesSizeAndRivalPreservesOriginalGalaxyAndIds()
    {
        var c=ScoutingFixtures.Content();
        foreach(var species in c.Species.Keys.OrderBy(x=>x))foreach(var size in c.GalaxySizes.Keys.OrderBy(x=>x))foreach(int rivals in new[]{0,1})foreach(bool enabled in new[]{false,true})
        {
            var old=GalaxyGenerator.CreateNewGame(c,new(size,42,6,species,rivals,rivals==1?"peldra":null));
            var next=GalaxyGenerator.CreateNewGame(c,new(size,42,7,species,rivals,rivals==1?"peldra":null,AmebasEnabled:enabled));
            Assert.True(old.Succeeded,old.Error);Assert.True(next.Succeeded,next.Error);var s=next.State!;
            Assert.Equal(old.State!.NextEntityId,s.NextEntityId);Assert.Equal(Facts(old.State),Facts(s));
            Assert.Equal(enabled,s.Scouting!.Enabled);Assert.All(s.Empires,e=>Assert.Equal(0,e.Scouting!.RiskRollCount));
            Assert.All(s.Vessels,v=>{Assert.Equal(ScoutPosture.Observe,v.Scout!.Posture);Assert.True(v.Scout.WithdrawFromAmeba);});
            if(enabled){Assert.Equal(1,s.Scouting.SelectionCount);Assert.DoesNotContain(s.Colonies,col=>s.FindPlanet(col.PlanetId)!.StarId==s.Scouting.Habitat!.Star);}else{Assert.Null(s.Scouting.PlacementRandom);Assert.Null(s.Scouting.Habitat);Assert.All(s.Empires,e=>Assert.Null(e.Scouting!.RiskRandom));}
        }
    }
    [Theory][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)]
    public void TinyMapsLeaveOneEligibleSystemClear(int stars)
    {
        var g=ScoutingFixtures.Game();var s=g.State;s.GalaxySeed=42;
        while(s.Stars.Count<stars)s.Stars.Add(new(){Id=new(s.AllocateId()),Name="Extra",X=400,Y=400,RegionId="arm"});
        s.Stars=s.Stars.Take(stars).ToList();s.Scouting=null;ScoutInitializer.Initialize(g.Content,s,true);
        Assert.NotNull(s.Scouting);Assert.Equal(stars>=3?1:0,s.Scouting.SelectionCount);
        if(stars==3)Assert.Equal(s.Stars[2].Id,s.Scouting.Habitat!.Star);
        if(stars<3)Assert.Null(s.Scouting.Habitat);
    }
    [Fact] public void ExplicitSetupAndContentAreRequiredWithoutActivatingOldRules()
    {
        var c=ScoutingFixtures.Content();Assert.False(GalaxyGenerator.CreateNewGame(c,new("small",42,7,"harl")).Succeeded);
        Assert.False(GalaxyGenerator.CreateNewGame(c,new("small",42,6,"harl",AmebasEnabled:false)).Succeeded);
        foreach(bool enabled in new[]{false,true})Assert.False(GalaxyGenerator.CreateNewGame(c with{SpaceAmeba=null},new("small",42,7,"harl",AmebasEnabled:enabled)).Succeeded);
        var old=GalaxyGenerator.CreateNewGame(c,new("small",42,6,"harl"));Assert.True(old.Succeeded,old.Error);Assert.Null(old.State!.Scouting);
    }
    internal static string Facts(GameState s)
    {
        var node=JsonSerializer.SerializeToNode(s)!.AsObject();node["RulesVersion"]=6;Strip(node);return node.ToJsonString();
        static void Strip(JsonNode node){if(node is JsonObject o){o.Remove("Scouting");o.Remove("Scout");o.Remove("AmebaEncounter");foreach(var p in o)if(p.Value is {} child)Strip(child);}else if(node is JsonArray a)foreach(var n in a)if(n is not null)Strip(n);}
    }
}
