using System.Linq;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class SpeciesStateTests
{
    [Theory][InlineData("peldra")][InlineData("harl")][InlineData("sulan")][InlineData("wenth")]
    public void ClonesRetainAllSpeciesMetadata(string id)
    {
        var state = SpeciesFixtures.AtHome(id);
        Assert.Empty(GameStateValidator.Validate(SpeciesFixtures.Content(), state));
        Assert.Equal(SpeciesFixtures.Snapshot(state), SpeciesFixtures.Snapshot(state.Clone()));
    }
    [Theory][InlineData("species")][InlineData("unknown_species")][InlineData("environment")]
    [InlineData("unknown_environment")][InlineData("mode")][InlineData("sealed")]
    [InlineData("observation")][InlineData("vessel")][InlineData("target")][InlineData("capacity")]
    [InlineData("founding_habitat")]
    public void InvalidSpeciesStateIsRejected(string mutation)
    {
        var state = SpeciesFixtures.AtHome("wenth");
        switch(mutation)
        {
            case "species": state.Empires[0].SpeciesId = null; break;
            case "unknown_species": state.Empires[0].SpeciesId = "missing"; break;
            case "environment": state.Planets[0].EnvironmentId = null; break;
            case "unknown_environment": state.Planets[0].EnvironmentId = "missing"; break;
            case "mode": state.Colonies[0].HabitatMode = null; break;
            case "sealed": state.Colonies[0].HabitatMode = "sealed"; break;
            case "observation": state.Empires[0].Surveys[0].Planets[0] = state.Empires[0].Surveys[0].Planets[0] with { EnvironmentId = "dry" }; break;
            case "vessel": state.Vessels[0].RoleId = "harl_settler"; state.Vessels[0].CarriedPopulation = 1; break;
            case "target": state.Colonies[0].ProjectKind = ProductionKinds.Vessel; state.Colonies[0].ProjectId = "harl_settler"; break;
            case "capacity": state.Colonies[0].Population = 7; state.Colonies[0].Workforce = new(5,1,1); break;
            case "founding_habitat":
                state = SpeciesFixtures.AtHome("sulan"); state.Planets[1].EnvironmentId = "dry";
                state.Vessels[0].RoleId = "settler"; state.Vessels[0].CarriedPopulation = 2; state.Vessels[0].StationaryStarId = state.Stars[1].Id; state.Vessels[0].PendingFounding = state.Planets[1].Id;
                OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(state, state.Turn); break;
        }
        Assert.NotEmpty(GameStateValidator.Validate(SpeciesFixtures.Content(),state));
    }
    [Fact]
    public void BankedImmatureSeedIsValidAndClonable()
    {
        var s=SpeciesFixtures.AtHome("wenth");s.Colonies[0].Population=5;s.Colonies[0].Workforce=new(3,1,1);
        s.Colonies[0].ProjectKind=ProductionKinds.Vessel;s.Colonies[0].ProjectId="daughter_seed";s.Colonies[0].ProductionReserve=9000;
        Assert.Empty(GameStateValidator.Validate(SpeciesFixtures.Content(),s));
        Assert.Empty(GameStateValidator.Validate(SpeciesFixtures.Content(),s.Clone()));
    }
    [Theory][InlineData("native",12)][InlineData("sealed",8)]
    public void SulanModesHaveTheirOwnCapacityIncludingExtension(string mode,int capacity)
    {
        var s=SpeciesFixtures.AtHome("sulan");var c=s.Colonies[0];c.HabitatMode=mode;c.Population=capacity;c.Workforce=new(capacity,0,0);
        Assert.Empty(GameStateValidator.Validate(SpeciesFixtures.Content(),s));
        c.CompletedBuildingIds.Add("habitat_extension");c.Population+=2;c.Workforce=new(capacity+2,0,0);
        Assert.Empty(GameStateValidator.Validate(SpeciesFixtures.Content(),s));
        c.Population++;c.Workforce=new(capacity+3,0,0);
        Assert.NotEmpty(GameStateValidator.Validate(SpeciesFixtures.Content(),s));
    }
}
