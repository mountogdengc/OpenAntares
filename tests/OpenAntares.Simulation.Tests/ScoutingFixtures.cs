using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Random;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Tests;

// Fully initialized labelled pre-session states, never generated campaign evidence.
internal static class ScoutingFixtures
{
    internal static ContentSet Content()=>ContentLoader.Load(new[]{new ContentSource("scouting.json",File.ReadAllText(CompatibilityFixtures.PathFor("scouting-content.json")))},requireScouting:true).Content??throw new InvalidOperationException("Invalid scout fixture content");
    internal static void Attach(ContentSet content,GameState s,bool enabled,StarId habitat)
    {
        s.RulesVersion=7;var placement=Pcg32.FromSeed(42^0x414D454241000001UL,0x4841424954415401UL);placement.NextInt(2);
        s.Scouting=new(){Enabled=enabled,PlacementRandom=enabled?placement:null,SelectionCount=enabled?1:0,Habitat=enabled?new(1,content.SpaceAmeba!.Id,habitat):null};
        foreach(var e in s.Empires)e.Scouting=new(){RiskRandom=enabled?Pcg32.FromSeed(42^0x414D454241000001UL,(ulong)e.Id.Value^0x4553434150450001UL):null};
        foreach(var v in s.Vessels)if(!content.Vessels[v.RoleId].CanFoundColony&&v.CarriedPopulation==0)v.Scout=new();
    }
    internal static LoadedGame Game(bool enabled=true,string species="harl")
    {
        var before=CrisisFixtures.Game(species);var c=Content();var s=before.State;
        s.FindStar(new(6))!.X=0;s.FindStar(new(6))!.Y=300;
        s.Stars.Add(new(){Id=new(8),Name="Debris",X=301,Y=0,RegionId="arm"});
        s.Planets.Add(new(){Id=new(9),StarId=new(8),Orbit=1,PlanetTypeId="forge_world",EnvironmentId="shelf_sea",Appearance=new(1,1,9)});s.NextEntityId=10;
        Attach(c,s,enabled,new(8));ObservationUpdater.Refresh(s,s.Turn);return Valid(new(c,s));
    }
    internal static LoadedGame AtAmeba(AmebaStage stage=AmebaStage.Warning,int scoutSpeed=300)
    {
        var g=Game();if(scoutSpeed!=300)g=g with{Content=g.Content with{Vessels=g.Content.Vessels.SetItem("scout",g.Content.Vessels["scout"] with{Speed=scoutSpeed})}};
        var s=g.State;s.Turn=10;var v=s.Vessels[0];v.StationaryStarId=new(8);v.AmebaEncounter=new(){Number=1,HabitatId=1,ArrivalTurn=10,Stage=stage};
        var e=s.Empires[0].Scouting!;e.NextEncounterNumber=2;e.KnownHabitats.Add(new(1,new(8),10));
        if(stage==AmebaStage.Exposed){v.AmebaEncounter.ArrivalTurn=9;e.SuccessfulStudies.Add(new(1,10,v.Id));e.RiskRandom!.NextInt(100);e.RiskRollCount=1;}
        ObservationUpdater.Refresh(s,s.Turn);return Valid(g);
    }
    internal static LoadedGame WithSecondScout()
    {
        var g=AtAmeba();var s=g.State;var e=s.Empires[0].Scouting!;s.Vessels.Add(new(){Id=new(s.AllocateId()),EmpireId=s.Empires[0].Id,RoleId="scout",StationaryStarId=new(8),Crisis=new(),Scout=new(),AmebaEncounter=new(){Number=e.NextEncounterNumber++,HabitatId=1,ArrivalTurn=s.Turn}});return Valid(g);
    }
    internal static LoadedGame TwoHumansAndAi()
    {
        var g=CrisisFixtures.TwoHumansAndAi();var c=Content();var s=g.State;
        foreach(var pair in new[]{(14,15,0,600),(16,17,600,600)}){s.Stars.Add(new(){Id=new(pair.Item1),Name="Empty",X=pair.Item3,Y=pair.Item4,RegionId="arm"});s.Planets.Add(new(){Id=new(pair.Item2),StarId=new(pair.Item1),Orbit=1,PlanetTypeId="forge_world",EnvironmentId="shelf_sea",Appearance=new(1,1,(ulong)pair.Item2)});}
        s.NextEntityId=18;Attach(c,s,true,new(16));ObservationUpdater.Refresh(s,s.Turn);return Valid(new(c,s));
    }
    internal static LoadedGame WithNextDraw(LoadedGame g,int draw)
    {
        var copy=g with{State=g.State.Clone()};var e=copy.State.Empires[0].Scouting!;
        for(int n=0;n<100000;n++){var probe=e.RiskRandom!.Clone();if(probe.NextInt(100)==draw)return Valid(copy);e.RiskRandom.NextInt(100);e.RiskRollCount++;}
        throw new InvalidOperationException("No reachable labelled draw found");
    }
    internal static string Bytes(GameState s)=>JsonSerializer.Serialize(s);
    internal static LoadedGame Valid(LoadedGame g){var errors=GameStateValidator.Validate(g.Content,g.State);if(errors.Count>0)throw new InvalidOperationException(string.Join("; ",errors));return g;}
}
