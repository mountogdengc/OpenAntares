using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class SpeciesFoundingTests
{
    private static GameState Stationed(string species,string role,string environment="shelf_sea",string type="verdant_world")
    {
        var state=SpeciesFixtures.AtHome(species);var v=state.Vessels[0];v.RoleId=role;v.CarriedPopulation=SpeciesFixtures.Content().Vessels[role].EmbarkPopulation;v.StationaryStarId=new(6);
        state.Planets[1].EnvironmentId=environment;state.Planets[1].PlanetTypeId=type;ObservationUpdater.Refresh(state,0);return state;
    }
    private static CommandOutcome Ready(GameSession session)
    {
        var outcome=session.Execute(new SetReadyCommand(Fixtures.Empire,session.State.Turn,true));Assert.True(outcome.Accepted,outcome.Rejection?.Message);return outcome;
    }
    private static JsonNode Preview(GameSession session,VesselId vessel,PlanetId target)
    {
        var view=session.View(Fixtures.Empire).Vessels.Single(v=>v.Id==vessel);var node=JsonNode.Parse(JsonSerializer.Serialize(view))!;
        Assert.NotNull(node["FoundingPreviews"]);return node["FoundingPreviews"]!.AsArray().Single(p=>p!["Target"]!["Value"]!.GetValue<int>()==target.Value)!;
    }
    [Theory][InlineData("settler","dry",false,"native",12)][InlineData("settler","open_ocean",false,"native",12)]
    [InlineData("settler","shelf_sea",true,"native",12)][InlineData("harbor_settler","dry",true,"sealed",8)]
    [InlineData("harbor_settler","open_ocean",true,"sealed",8)][InlineData("harbor_settler","shelf_sea",true,"sealed",8)]
    public void SulanHabitatEligibilityAndFoundedModeAgree(string role,string environment,bool allowed,string mode,int capacity)
    {
        var state=Stationed("sulan",role,environment);var session=new GameSession(SpeciesFixtures.Content(),state);string before=SpeciesFixtures.Snapshot(state);
        var outcome=session.Execute(new SetFoundingOrderCommand(Fixtures.Empire,0,new(5),new(7)));Assert.Equal(allowed,outcome.Accepted);
        if(!allowed){Assert.Equal("habitat_not_suitable",outcome.Rejection!.Code);Assert.Equal(before,SpeciesFixtures.Snapshot(session.State));return;}
        var preview=Preview(session,new(5),new(7));Assert.Null(preview["RejectionCode"]);Assert.Equal(capacity,preview["Capacity"]!.GetValue<int>());
        var report=Ready(session).Report!;var child=session.State.Colonies.Single(c=>c.PlanetId==new PlanetId(7));Assert.Equal(mode,child.HabitatMode);Assert.Equal(environment,session.State.FindPlanet(child.PlanetId)!.EnvironmentId);
        Assert.Equal(capacity,session.Forecast().Colony(child.Id).Growth.Capacity);Assert.Equal(new Workforce(2,0,0),child.Workforce);Assert.Single(report.Economy.Colonies);
    }
    [Theory][InlineData("peldra","peldra_settler","verdant_world",600,400,15)][InlineData("peldra","peldra_settler","forge_world",400,400,5)]
    [InlineData("sulan","settler","forge_world",400,200,30)]
    public void SharedFoundingPreviewMatchesTheFirstRealSnapshot(string species,string role,string type,long produced,long required,long growth)
    {
        var session=new GameSession(SpeciesFixtures.Content(),Stationed(species,role,type:type));var preview=Preview(session,new(5),new(7));
        Assert.Equal(produced,preview["Economy"]!["SupportProduced"]!["Total"]!.GetValue<long>());Assert.Equal(required,preview["Economy"]!["SupportRequired"]!["Total"]!.GetValue<long>());Assert.Equal(growth,preview["Economy"]!["GrowthEarned"]!["Total"]!.GetValue<long>());
        Assert.True(session.Execute(new SetFoundingOrderCommand(Fixtures.Empire,0,new(5),new(7))).Accepted);Ready(session);
        var actual=session.Forecast().Colony(session.State.Colonies.Single(c=>c.PlanetId==new PlanetId(7)).Id);Assert.Equal(produced,actual.SupportProduced.Total);Assert.Equal(required,actual.SupportRequired.Total);Assert.Equal(growth,actual.GrowthEarned.Total);
    }
    [Theory][InlineData("peldra","peldra_settler",2,10,false)][InlineData("harl","harl_settler",1,10,false)]
    [InlineData("sulan","settler",2,12,false)][InlineData("sulan","harbor_settler",2,8,false)]
    [InlineData("wenth","daughter_seed",3,6,false)][InlineData("wenth","daughter_seed",3,6,true)]
    public void EverySpeciesBuildsTravelsFoundsAndKeepsDaughterEconomyForNextSnapshot(string species,string role,int cargo,int capacity,bool extended)
    {
        var content=SpeciesFixtures.Content();var s=SpeciesFixtures.AtHome(species);s.Planets[1].PlanetTypeId="verdant_world";
        if(species=="wenth"){s.Colonies[0].Population=extended?8:6;s.Colonies[0].Workforce=new(extended?6:4,1,1);if(extended)s.Colonies[0].CompletedBuildingIds.Add("habitat_extension");}
        s.Colonies[0].ProductionReserve=content.Vessels[role].Cost;var session=new GameSession(content,s);
        Assert.True(session.Execute(new SetProductionTargetCommand(Fixtures.Empire,0,Fixtures.HomeColony,new(ProductionKinds.Vessel,role))).Accepted);Ready(session);var ship=session.State.Vessels.Single(v=>v.RoleId==role);Assert.Null(ship.PendingMove);Assert.Single(session.State.Colonies);if(extended)Assert.Equal(5,session.State.Colonies[0].Population);
        Assert.True(session.Execute(new SetMoveOrderCommand(Fixtures.Empire,1,ship.Id,new(6))).Accepted);
        Ready(session);Assert.NotNull(session.State.FindVessel(ship.Id)!.Transit);Ready(session);Assert.Equal(new StarId(6),session.State.FindVessel(ship.Id)!.StationaryStarId);
        Assert.Single(session.State.Colonies);Assert.True(session.Execute(new SetFoundingOrderCommand(Fixtures.Empire,3,ship.Id,new(7))).Accepted);var report=Ready(session).Report!;
        var child=session.State.Colonies.Single(c=>c.PlanetId==new PlanetId(7));Assert.Equal(cargo,child.Population);Assert.Equal(new Workforce(cargo,0,0),child.Workforce);Assert.Equal(0,child.GrowthProgress);Assert.Equal(0,child.ProductionReserve);Assert.Empty(child.CompletedBuildingIds);Assert.Single(report.Economy.Colonies);Assert.Null(session.State.FindVessel(ship.Id));Assert.Equal(capacity,session.Forecast().Colony(child.Id).Growth.Capacity);
        if(species=="harl"){Assert.Equal(50,session.Forecast().Colony(child.Id).GrowthEarned.Total);Ready(session);Assert.Equal(1,session.State.FindColony(child.Id)!.Population);Ready(session);Assert.Equal(2,session.State.FindColony(child.Id)!.Population);}
        if(extended){Assert.Equal(6,session.State.Colonies[0].Population);Assert.Equal(8,session.Forecast().Colonies[0].Growth.Capacity);}
    }
    [Fact]
    public void UnknownExistingAndNonexistentTargetsHaveIdenticalRejections()
    {
        var s=SpeciesFixtures.AtHome("sulan");s.Vessels[0].RoleId="settler";s.Vessels[0].CarriedPopulation=2;var content=SpeciesFixtures.Content();
        var a=CommandProcessor.Execute(content,s,new SetFoundingOrderCommand(Fixtures.Empire,0,new(5),new(7)));var b=CommandProcessor.Execute(content,s,new SetFoundingOrderCommand(Fixtures.Empire,0,new(5),new(999)));
        Assert.Equal("target_not_known",a.Rejection!.Code);Assert.Equal(a.Rejection,b.Rejection);Assert.Same(s,a.State);
    }
    [Fact]
    public void EditedCargoIsValidatedAndUsedByBothPreviewAndFounding()
    {
        var content=SpeciesFixtures.Content();content=content with{Vessels=content.Vessels.SetItem("harl_settler",content.Vessels["harl_settler"] with{EmbarkPopulation=2})};
        using var buffer=new MemoryStream();using(var writer=new Utf8JsonWriter(buffer))ContentWriter.Write(writer,content);
        var loaded=ContentLoader.Load(new[]{new ContentSource("edited.json",Encoding.UTF8.GetString(buffer.ToArray()))},requireSpecies:true);Assert.True(loaded.Succeeded,string.Join(";",loaded.Errors));
        var s=Stationed("harl","harl_settler");s.Vessels[0].CarriedPopulation=2;var session=new GameSession(loaded.Content!,s);
        Assert.Equal(2,Preview(session,new(5),new(7))["Population"]!.GetValue<int>());Assert.True(session.Execute(new SetFoundingOrderCommand(Fixtures.Empire,0,new(5),new(7))).Accepted);Ready(session);Assert.Equal(new Workforce(2,0,0),session.State.Colonies[1].Workforce);
    }
    [Fact]
    public void CompetingEmpiresConserveLosingCargoAndUseEmpireThenVesselOrder()
    {
        var s=Stationed("harl","harl_settler");s.Empires.Add(new EmpireState{Id=new(8),SpeciesId="harl",Controller=ControllerKind.Ai});s.Vessels[0].EmpireId=new(8);s.Vessels.Add(new VesselState{Id=new(9),EmpireId=Fixtures.Empire,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=new(6)});s.NextEntityId=10;ObservationUpdater.Refresh(s,0);
        var session=new GameSession(SpeciesFixtures.Content(),s);Assert.True(session.Execute(new SetFoundingOrderCommand(new(8),0,new(5),new(7))).Accepted);Assert.True(session.Execute(new SetFoundingOrderCommand(Fixtures.Empire,0,new(9),new(7))).Accepted);Ready(session);
        Assert.Equal(Fixtures.Empire,session.State.Colonies[1].EmpireId);var loser=Assert.Single(session.State.Vessels);Assert.Equal(new VesselId(5),loser.Id);Assert.Equal(1,loser.CarriedPopulation);Assert.Null(loser.PendingFounding);Assert.Equal(8,session.State.Colonies.Sum(c=>c.Population)+session.State.Vessels.Sum(v=>v.CarriedPopulation));
    }
    [Fact]
    public void FoundingIdOverflowPublishesNothing()
    {
        var s=Stationed("harl","harl_settler");s.NextEntityId=int.MaxValue;var session=new GameSession(SpeciesFixtures.Content(),s);Assert.True(session.Execute(new SetFoundingOrderCommand(Fixtures.Empire,0,new(5),new(7))).Accepted);
        var before=session.State;Assert.False(session.Execute(new SetReadyCommand(Fixtures.Empire,0,true)).Accepted);Assert.Same(before,session.State);Assert.Equal(new PlanetId(7),session.State.Vessels[0].PendingFounding);Assert.False(before.Empires[0].Ready);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void BothContrastingSurveyedMapsOfferDifferentNativeAndHarborChoices(bool reversed)
    {
        var state=Stationed("sulan","settler",reversed?"dry":"shelf_sea","forge_world");
        state.Planets.Add(new PlanetState{Id=new(8),StarId=new(6),Orbit=2,PlanetTypeId="verdant_world",EnvironmentId=reversed?"shelf_sea":"dry"});state.NextEntityId=9;ObservationUpdater.Refresh(state,0);
        var native=new GameSession(SpeciesFixtures.Content(),state);string before=SpeciesFixtures.Snapshot(state);
        var forge=Preview(native,new(5),new(7));var verdant=Preview(native,new(5),new(8));
        Assert.Equal(reversed?"habitat_not_suitable":null,forge["RejectionCode"]?.GetValue<string>());Assert.Equal(reversed?null:"habitat_not_suitable",verdant["RejectionCode"]?.GetValue<string>());
        Assert.Equal(before,SpeciesFixtures.Snapshot(state));
        state=state.Clone();state.Vessels[0].RoleId="harbor_settler";var harbor=new GameSession(SpeciesFixtures.Content(),state);
        Assert.Null(Preview(harbor,new(5),new(7))["RejectionCode"]);Assert.Null(Preview(harbor,new(5),new(8))["RejectionCode"]);
    }
    [Fact]
    public void RejectedHabitatRetainsQueuedRouteCargoAndReadiness()
    {
        var state=Stationed("sulan","settler","dry");state.Vessels[0].PendingMove=Fixtures.HomeStar;
        state.Empires.Add(new EmpireState{Id=new(8),SpeciesId="harl",Controller=ControllerKind.Human});state.NextEntityId=9;state.Empires[0].Ready=true;
        string before=SpeciesFixtures.Snapshot(state);var outcome=CommandProcessor.Execute(SpeciesFixtures.Content(),state,new SetFoundingOrderCommand(Fixtures.Empire,0,new(5),new(7)));
        Assert.Equal("habitat_not_suitable",outcome.Rejection!.Code);Assert.Same(state,outcome.State);Assert.Equal(before,SpeciesFixtures.Snapshot(outcome.State));
    }

}
