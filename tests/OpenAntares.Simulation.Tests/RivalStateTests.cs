using System.Text.Json;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Colonization;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class RivalStateTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void ExtendedWenthAndSealedSulanKeepTheirExactRules(int version)
    {
        var content = SpeciesFixtures.Content() with { RivalAi = new("expansion_v1", 1, 3) };
        var wenth = SpeciesFixtures.AtHome("wenth"); wenth.RulesVersion = version;
        var colony = wenth.Colonies[0]; colony.Population = 6; colony.Workforce = new(4, 1, 1);
        colony.CompletedBuildingIds.Add("habitat_extension");
        var seed = content.Vessels["daughter_seed"];
        Assert.Equal(8, Rules.ColonyCapacity(content, wenth, colony));
        Assert.Contains(EmbarkationRules.Evaluate(content, wenth, colony, seed).Unmet, r => r.Code == "full_colony" && r.Required == 8);
        colony.Population = 8; colony.Workforce = new(6, 1, 1);
        Assert.True(EmbarkationRules.Evaluate(content, wenth, colony, seed).Eligible);

        var sulan = SpeciesFixtures.AtHome("sulan"); sulan.RulesVersion = version;
        var vessel = sulan.Vessels[0]; vessel.RoleId = "harbor_settler"; vessel.CarriedPopulation = 2; vessel.StationaryStarId = new(6);
        sulan.Planets[1].EnvironmentId = "dry"; sulan.Planets[1].PlanetTypeId = "verdant_world"; ObservationUpdater.Refresh(sulan, 0);
        var preview = FoundingRules.Preview(content, sulan, sulan.Empires[0].Id, vessel.Id, new(7));
        Assert.Null(preview.RejectionCode); Assert.Equal("sealed", preview.HabitatMode); Assert.Equal(8, preview.Capacity);
        vessel.RoleId = "settler";
        Assert.Equal("habitat_not_suitable", FoundingRules.Preview(content, sulan, sulan.Empires[0].Id, vessel.Id, new(7)).RejectionCode);
    }

    [Fact]
    public void ContactsMustBeSortedAndLegacyCannotKeepContactLists()
    {
        var state = RivalFixtures.AtHome("peldra"); var content = RivalFixtures.Content();
        var third = new EmpireState { Id = new(state.AllocateId()), Controller = ControllerKind.Ai, SpeciesId = "wenth", AiPolicyId = "expansion_v1" };
        state.Empires.Add(third); var human = state.Empires[0]; var second = state.Empires[1];
        human.Contacts.Add(new(third.Id, third.SpeciesId!, 0)); human.Contacts.Add(new(second.Id, second.SpeciesId!, 0));
        Assert.NotEmpty(GameStateValidator.Validate(content, state));
        human.Contacts.Reverse(); Assert.Empty(GameStateValidator.Validate(content, state));
        var old = SpeciesFixtures.AtHome("harl"); old.Empires[0].Contacts.Add(new(new(9999), "harl", 0));
        Assert.NotEmpty(GameStateValidator.Validate(content, old));
    }
    [Fact]
    public void CloneAndValidateMetadata()
    {
        var content = RivalFixtures.Content();
        var state = RivalFixtures.AtHome("peldra");
        Assert.Empty(GameStateValidator.Validate(content, state));
        var ai = state.Empires.Single(e => e.Controller == ControllerKind.Ai);
        var human = state.Empires.Single(e => e.Controller == ControllerKind.Human);
        var clone = state.Clone();
        clone.FindEmpire(ai.Id)!.Contacts.Add(new(human.Id, human.SpeciesId!, 0));
        Assert.Empty(ai.Contacts);
        var invalid = state.Clone(); invalid.FindEmpire(ai.Id)!.AiPolicyId = null;
        Assert.NotEmpty(GameStateValidator.Validate(content, invalid));
        invalid = state.Clone(); invalid.FindEmpire(human.Id)!.AiPolicyId = "expansion_v1";
        Assert.NotEmpty(GameStateValidator.Validate(content, invalid));
    }

    [Theory]
    [InlineData("self")]
    [InlineData("missing")]
    [InlineData("identity")]
    [InlineData("past")]
    [InlineData("future")]
    [InlineData("duplicate")]
    [InlineData("unknown_policy")]
    public void InvalidContactOrPolicyCannotBePublished(string fault)
    {
        var state = RivalFixtures.AtHome("peldra"); var content = RivalFixtures.Content();
        var ai = state.Empires.Single(e => e.Controller == ControllerKind.Ai); var human = state.Empires[0];
        var contact = new ContactState(human.Id, human.SpeciesId!, 0);
        if (fault == "self") contact = contact with { OtherEmpireId = ai.Id, SpeciesId = ai.SpeciesId! };
        if (fault == "missing") contact = contact with { OtherEmpireId = new(9999) };
        if (fault == "identity") contact = contact with { SpeciesId = "wenth" };
        if (fault == "past") contact = contact with { FirstObservedTurn = -1 };
        if (fault == "future") contact = contact with { FirstObservedTurn = 1 };
        if (fault == "unknown_policy") ai.AiPolicyId = "unsupported";
        ai.Contacts.Add(contact);
        if (fault == "duplicate") ai.Contacts.Add(contact);
        Assert.NotEmpty(GameStateValidator.Validate(content, state));
    }

    [Fact]
    public void CurrentForeignOwnershipRequiresContactAndLegacyRejectsMetadata()
    {
        var state = RivalFixtures.AtHome("peldra"); var content = RivalFixtures.Content();
        var ai = state.Empires.Single(e => e.Controller == ControllerKind.Ai); var human = state.Empires[0];
        state.Vessels.Single(v => v.EmpireId == ai.Id).StationaryStarId = state.Stars[0].Id;
        ObservationUpdater.Refresh(state, state.Turn);
        Assert.NotEmpty(GameStateValidator.Validate(content, state));
        ai.Contacts.Add(new(human.Id, human.SpeciesId!, 0)); human.Contacts.Add(new(ai.Id, ai.SpeciesId!, 0));
        Assert.Empty(GameStateValidator.Validate(content, state));
        var old = SpeciesFixtures.AtHome("harl"); old.Empires[0].AiPolicyId = "expansion_v1";
        Assert.NotEmpty(GameStateValidator.Validate(content, old));
    }

    [Fact]
    public void RivalRulesAlwaysRequireTheirEmbeddedPolicy()
    {
        var state = RivalFixtures.AtHome("harl");
        Assert.NotEmpty(GameStateValidator.Validate(RivalFixtures.Content() with { RivalAi = null }, state));
        var missing = ContentLoader.Load(RivalContentTests.Sources(), requireSpecies: true, requireRivals: true);
        Assert.False(missing.Succeeded);
    }
    [Theory]
    [InlineData("peldra")]
    [InlineData("harl")]
    [InlineData("sulan")]
    [InlineData("wenth")]
    public void FeaturesKeepExactOldEconomyAndEnableSameSpeciesAt4(string species)
    {
        var content = SpeciesFixtures.Content() with { RivalAi = new("expansion_v1", 1, 3) };
        var state = SpeciesFixtures.AtHome(species);
        string oldEconomy = JsonSerializer.Serialize(EconomyCalculator.Calculate(content, state));
        int capacity = Rules.ColonyCapacity(content, state, state.Colonies[0]);
        var oldView = EmpireViewProjector.Project(content, state, state.Empires[0].Id);
        state.RulesVersion = 4;
        Assert.Empty(GameStateValidator.Validate(content, state));
        Assert.Equal(oldEconomy, JsonSerializer.Serialize(EconomyCalculator.Calculate(content, state)));
        Assert.Equal(capacity, Rules.ColonyCapacity(content, state, state.Colonies[0]));
        Assert.Equal(oldView.AvailableVesselRoleIds, EmpireViewProjector.Project(content, state, state.Empires[0].Id).AvailableVesselRoleIds);
    }
}
