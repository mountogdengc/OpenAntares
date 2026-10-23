using System;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public class KnowledgeTests
{
    [Fact] public void HiddenWorldChangesDoNotChangeEmpireView()
    {
        var s=ExpansionFixtures.AtHome();var c=ExpansionFixtures.Content();
        s.Empires.Add(new EmpireState {Id=new(8),Controller=ControllerKind.Ai});s.Colonies.Add(new ColonyState{Id=new(9),EmpireId=new(8),PlanetId=new(7),Population=2,Workforce=new(2,0,0)});
        s.Vessels.Add(new VesselState{Id=new(10),EmpireId=new(8),RoleId="settler",CarriedPopulation=2,StationaryStarId=new(6)});s.NextEntityId=11;
        var before=JsonSerializer.Serialize(EmpireViewProjector.Project(c,s,Fixtures.Empire));
        s.Planets[1].PlanetTypeId="crystal_world";s.Vessels[1].PendingMove=Fixtures.HomeStar;s.Colonies[1].ProductionReserve=long.MaxValue;
        s.Planets.Add(new PlanetState {Id=new(11),StarId=new(6),Orbit=2,PlanetTypeId="barren_rock"});s.NextEntityId=12;
        Assert.Equal(before,JsonSerializer.Serialize(EmpireViewProjector.Project(c,s,Fixtures.Empire)));
        Assert.Null(EmpireViewProjector.Project(c,s,Fixtures.Empire).Stars.Single(x=>x.Chart.Id==new StarId(6)).Planets);
    }
    [Fact] public void LastSeenOwnershipAndForeignSightingsHaveDifferentLifetimes()
    {
        var c=ExpansionFixtures.Content();var s=ExpansionFixtures.AtHome();s.Empires.Add(new EmpireState{Id=new(8),Controller=ControllerKind.Ai});
        s.Vessels.Add(new VesselState {Id=new(9),EmpireId=new(8),RoleId="settler",CarriedPopulation=2,StationaryStarId=new(6),PendingMove=Fixtures.HomeStar});s.NextEntityId=10;
        s.Vessels[0].StationaryStarId=new(6);s.Turn=1;ObservationUpdater.Refresh(s,1);
        var seen=EmpireViewProjector.Project(c,s,Fixtures.Empire);Assert.Single(seen.ForeignVessels);Assert.True(seen.Stars.Single(x=>x.Chart.Id==new StarId(6)).Current);
        s.Vessels[0].StationaryStarId=Fixtures.HomeStar;s.Turn=2;ObservationUpdater.Refresh(s,2);
        var stale=EmpireViewProjector.Project(c,s,Fixtures.Empire);Assert.Empty(stale.ForeignVessels);var star=stale.Stars.Single(x=>x.Chart.Id==new StarId(6));Assert.False(star.Current);Assert.Equal(1,star.LastObservedTurn);Assert.Single(star.Planets!.Value);
    }
    [Fact] public void ViewsAreCopiesAndLegacyIsAllVisible()
    {
        var s=ExpansionFixtures.AtHome();var view=new GameSession(ExpansionFixtures.Content(),s).View(Fixtures.Empire);
        s.Colonies[0].Population=7;s.Colonies[0].CompletedBuildingIds.Add("fabrication_hall");s.Stars[0].Name="Changed";
        Assert.Equal(6,view.Colonies[0].Population);Assert.Empty(view.Colonies[0].CompletedBuildingIds);Assert.NotEqual("Changed",view.Stars[0].Chart.Name);
        var legacy=ExpansionFixtures.AtHome();legacy.RulesVersion=1;legacy.Vessels.Clear();legacy.Empires[0].Surveys.Clear();
        Assert.All(EmpireViewProjector.Project(ExpansionFixtures.Content(),legacy,Fixtures.Empire).Stars,x=>Assert.True(x.Planets.HasValue));
        Assert.Throws<ArgumentException>(()=>EmpireViewProjector.Project(ExpansionFixtures.Content(),legacy,new(999)));
    }
}
