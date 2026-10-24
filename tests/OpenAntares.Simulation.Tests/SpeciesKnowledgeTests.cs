using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class SpeciesKnowledgeTests
{
    [Fact]
    public void CurrentAndStaleObservationsExposeEnvironmentAndOwnEffectiveRates()
    {
        var content=SpeciesFixtures.Content();var s=SpeciesFixtures.AtHome("peldra");
        var json=JsonNode.Parse(JsonSerializer.Serialize(EmpireViewProjector.Project(content,s,Fixtures.Empire)))!;
        var known=json["Stars"]![0]!["Planets"]![0]!;
        Assert.Equal("shelf_sea",known["EnvironmentId"]?.GetValue<string>());Assert.NotNull(known["Potential"]);
        Assert.Equal(600,known["Potential"]!["ProductionPerWorker"]!["Total"]!.GetValue<long>());
        Assert.Null(json["Stars"]![1]!["Planets"]);
        s.Vessels[0].StationaryStarId=s.Stars[1].Id;ObservationUpdater.Refresh(s,0);s.Vessels.Clear();s.Turn=1;ObservationUpdater.Refresh(s,1);
        var stale=EmpireViewProjector.Project(content,s,Fixtures.Empire).Stars[1];Assert.False(stale.Current);Assert.Equal(0,stale.LastObservedTurn);
        var node=JsonNode.Parse(JsonSerializer.Serialize(stale))!;Assert.Equal("shelf_sea",node["Planets"]![0]!["EnvironmentId"]!.GetValue<string>());
    }
    [Fact]
    public void UnseenEnvironmentAndForeignSpeciesOverflowCannotChangeOwnPermittedView()
    {
        var content=SpeciesFixtures.Content();var s=SpeciesFixtures.AtHome("harl");
        s.Empires.Add(new EmpireState{Id=new(8),SpeciesId="peldra",Controller=ControllerKind.Ai});
        s.Colonies.Add(new ColonyState{Id=new(9),EmpireId=new(8),PlanetId=s.Planets[1].Id,Population=6,Workforce=new(4,1,1),HabitatMode="native"});s.NextEntityId=10;
        string before=JsonSerializer.Serialize(EmpireViewProjector.Project(content,s,Fixtures.Empire));
        s.Planets[1].EnvironmentId="dry";s.Empires[1].SpeciesId="missing";s.Colonies[1].ProductionReserve=long.MaxValue;s.Colonies[1].Population=int.MaxValue;s.Colonies[1].Workforce=new(int.MaxValue,0,0);
        Assert.Equal(before,JsonSerializer.Serialize(EmpireViewProjector.Project(content,s,Fixtures.Empire)));
    }
}
