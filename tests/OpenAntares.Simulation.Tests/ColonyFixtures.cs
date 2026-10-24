using System;
using System.IO;
using System.Linq;
using OpenAntares.Simulation.Appearance;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Tests;
internal static class ColonyFixtures
{
    internal static string Json() => File.ReadAllText(CompatibilityFixtures.PathFor("colony-development-content.json"));
    internal static ContentSet Content() { var result=ContentLoader.Load(new[]{new ContentSource("colony-fixture.json",Json())},requireDevelopment:true); return result.Content ?? throw new InvalidOperationException(string.Join("; ",result.Errors)); }
    internal static LoadedGame Game(string species="harl",int population=6)
    {
        var content=Content(); var state=SpeciesFixtures.AtHome(species); state.RulesVersion=5;
        foreach(var e in state.Empires) e.Development=new();
        foreach(var c in state.Colonies) { c.Development=new(); c.Population=population; c.Workforce=new Workforce(population,0,0); }
        foreach(var p in state.Planets) p.Appearance=new(1,p.Id.Value%8,(ulong)p.Id.Value);
        foreach(var e in state.Empires) foreach(var o in e.Surveys) o.Planets=o.Planets.Select(p=>p with{Appearance=state.FindPlanet(p.PlanetId)!.Appearance}).ToList();
        return new(content,state);
    }
    internal static LoadedGame WithFoundations(string species,int population,string templateId)
    {
        var g=Game(species,population);var c=g.State.Colonies[0];c.CompletedBuildingIds=new(){"analysis_lab","cultivation_hub","fabrication_hall"};
        g.State.Empires[0].KnownTechnologyIds=g.Content.Technologies.Keys.ToList();
        c.Development!.Template=Snapshot(g.Content,templateId);c.Development.WorkforceEnabled=true;return g;
    }
    internal static GovernorTemplateSnapshot Snapshot(ContentSet content,string id) { var d=content.GovernorTemplates[id];return new(id,1,new(d.Name,d.FoundationIds,d.DevelopmentIds,d.SupportTarget,d.ProductionWeight,d.ResearchWeight)); }
    internal static LoadedGame TwoHumansAndAi() {
        var g=WithFoundations("harl",6,"industrial");var s=g.State;s.Colonies[0].Development!.ConstructionEnabled=true;
        var second=new EmpireState{Id=new(10),Controller=ControllerKind.Human,SpeciesId="harl",KnownTechnologyIds=g.Content.Technologies.Keys.ToList(),Development=new()};
        var ai=new EmpireState{Id=new(12),Controller=ControllerKind.Ai,SpeciesId="peldra",AiPolicyId="expansion_v1",Development=new()};s.Empires.Add(second);s.Empires.Add(ai);
        s.Stars.Add(new(){Id=new(8),Name="Third",X=600,Y=0});s.Planets.Add(new(){Id=new(9),StarId=new(8),Orbit=1,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea",Appearance=new(1,1,9)});
        s.Colonies.Add(new(){Id=new(11),PlanetId=new(7),EmpireId=second.Id,Population=6,Workforce=new(6,0,0),HabitatMode="native",Development=new(){Template=Snapshot(g.Content,"balanced"),WorkforceEnabled=true,ConstructionEnabled=true}});
        s.Colonies.Add(new(){Id=new(13),PlanetId=new(9),EmpireId=ai.Id,Population=6,Workforce=new(6,0,0),HabitatMode="native",Development=new()});s.NextEntityId=14;
        OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(s,s.Turn);return g;
    }
}
