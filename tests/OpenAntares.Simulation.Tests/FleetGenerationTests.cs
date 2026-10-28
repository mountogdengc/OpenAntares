using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetGenerationTests
{
    [Fact]
    public void AllSpeciesSizesModesPreserveGalaxyAndExistingRng()
    {
        var c=FleetContentTests.Core();int cases=0;
        foreach(string species in c.Species.Keys)foreach(string size in c.GalaxySizes.Keys)foreach(int rivals in new[]{0,1})foreach(bool plague in new[]{false,true})foreach(bool ameba in new[]{false,true})
        {
            var old=GalaxyGenerator.CreateNewGame(c,new(size,42,7,species,rivals,rivals==1?"peldra":null,CrisesEnabled:plague,AmebasEnabled:ameba));var next=GalaxyGenerator.CreateNewGame(c,new(size,42,8,species,rivals,rivals==1?"peldra":null,CrisesEnabled:plague,AmebasEnabled:ameba));Assert.True(old.Succeeded,old.Error);Assert.True(next.Succeeded,next.Error);Assert.Equal(Facts(old.State!),Facts(next.State!));
            foreach(var e in next.State!.Empires)
            {var f=e.Fleets!;var o=Assert.Single(f.Officers);Assert.Equal(0,o.Xp);Assert.Equal(0,f.RecoveryRollCount);Assert.Empty(f.Requests);Assert.Equal(e.Controller==ControllerKind.Ai?OfficerStatus.Commanding:OfficerStatus.Posted,o.Status);if(e.Controller==ControllerKind.Ai){var group=Assert.Single(f.Fleets);Assert.Single(group.Members);Assert.Equal(o.Id,group.Officer);Assert.Equal(group.Flagship,o.Flagship);}}
            cases++;
        }
        Assert.Equal(4*c.GalaxySizes.Count*8,cases);
    }
    internal static string Facts(GameState state)
    {var s=state.Clone();s.RulesVersion=7;foreach(var e in s.Empires)e.Fleets=null;foreach(var v in s.Vessels){v.Fleet=null;v.NewlyCompleted=false;v.BuildOrigin=null;}return JsonSerializer.Serialize(s);}
    [Fact]
    public void AlternateDeclaredBodySpeciesCanCommandExistingRivalScout()
    {
        var root=FleetContentTests.JoinedCore();root["species"]!.AsArray().Single(s=>s!["id"]!.GetValue<string>()=="wenth")!["id"]="chorus";var loaded=Content.ContentLoader.Load(new[]{new Content.ContentSource("alternate.json",root.ToJsonString())},requireFleets:true);Assert.True(loaded.Succeeded,string.Join(";",loaded.Errors));var made=GalaxyGenerator.CreateNewGame(loaded.Content!,new("small",42,8,"harl",1,"chorus",CrisesEnabled:false,AmebasEnabled:false));Assert.True(made.Succeeded,made.Error);var e=made.State!.Empires.Single(e=>e.Controller==ControllerKind.Ai);var o=Assert.Single(e.Fleets!.Officers);Assert.Equal(CommandEmbodiment.ColonyBody,o.Embodiment);Assert.Equal(OfficerStatus.Commanding,o.Status);Assert.Equal(made.State.Colonies.Single(c=>c.EmpireId==e.Id).Id,o.SourceBody);
    }
}
