using System;
using System.IO;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Tests;

// Labelled synthetic states. Generated campaign evidence uses GalaxyGenerator separately.
internal static class CrisisFixtures
{
    internal static ContentSet Content() => ContentLoader.Load(new[]{new ContentSource("labelled-crisis.json",File.ReadAllText(CompatibilityFixtures.PathFor("crisis-content.json")))},requireCrises:true).Content ?? throw new InvalidOperationException("Invalid crisis fixture content.");
    internal static LoadedGame Game(string species="harl",int population=6,bool enabled=true)
    {
        var old=ColonyFixtures.Game(species,population);var content=Content();
        old.State.RulesVersion=6;old.State.GalaxySeed=42;old.State.GalaxyShapeId="spiral";old.State.GalaxyWidth=1000;old.State.GalaxyHeight=1000;
        foreach(var s in old.State.Stars)s.RegionId="arm";
        CrisisInitializer.Initialize(content,old.State,enabled);
        return new(content,old.State);
    }
    internal static CrisisLocation Location(GameState state,ColonyState colony)
    {
        var planet=state.FindPlanet(colony.PlanetId)!;var star=state.FindStar(planet.StarId)!;
        return new(colony.Id,planet.Id,star.Id,$"{star.Name} {planet.Orbit}");
    }
    internal static LoadedGame Active(int severity=20,int age=0,string species="harl",int population=6,bool quarantine=false)
    {
        var g=Game(species,population);g.State.Turn=22;
        var colony=g.State.Colonies[0];var owner=g.State.FindEmpire(colony.EmpireId)!;var location=Location(g.State,colony);
        owner.Crisis!.NextEpisodeNumber=2;
        owner.Crisis.Episode=new(){Id=1,FamilyId="ash_bloom",Source=location,SourcePopulation=population,CureCost=600+100*population};
        colony.Crisis!.Case=new(){EpisodeId=1,Stage=CrisisStage.Active,Severity=severity,ActiveAge=age,AttentionRevision=2,HandledRevision=2,Origin=location};
        colony.Crisis.Quarantined=quarantine;
        ObservationUpdater.Refresh(g.State,g.State.Turn);
        return g;
    }
    internal static LoadedGame Warning(int grace=2)
    {
        var g=Active();g.State.Turn=20;var c=g.State.Colonies[0].Crisis!.Case!;
        c.Stage=CrisisStage.Warning;c.GraceRemaining=grace;c.Severity=0;c.ActiveAge=0;c.AttentionRevision=1;c.HandledRevision=0;
        ObservationUpdater.Refresh(g.State,g.State.Turn);
        return g;
    }
    internal static LoadedGame TwoHumansAndAi()
    {
        var g=ColonyFixtures.TwoHumansAndAi();g.State.RulesVersion=6;g.State.GalaxySeed=42;g.State.GalaxyShapeId="spiral";g.State.GalaxyWidth=1000;g.State.GalaxyHeight=1000;
        foreach(var s in g.State.Stars)s.RegionId="arm";
        var content=Content();CrisisInitializer.Initialize(content,g.State,true);return new(content,g.State);
    }
    internal static LoadedGame CargoArrival(bool quarantined=false,bool found=false)
    {
        var g=Active();var s=g.State;var owner=s.Empires[0].Id;
        if(!found)s.Colonies.Add(new(){Id=new(s.AllocateId()),EmpireId=owner,PlanetId=new(7),Population=3,Workforce=new(3,0,0),HabitatMode="native",Development=new(),Crisis=new(){Quarantined=quarantined}});
        s.Vessels.Clear();s.Vessels.Add(new(){Id=new(s.AllocateId()),EmpireId=owner,RoleId="harl_settler",CarriedPopulation=1,Transit=new(Fixtures.HomeStar,new(6),2,1),Crisis=new(){CarrierEpisode=1}});
        ObservationUpdater.Refresh(s,s.Turn);return g;
    }
}
