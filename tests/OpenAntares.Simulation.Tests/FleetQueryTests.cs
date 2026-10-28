using System;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Fleets;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class FleetQueryTests
{
    [Fact]
    public void ForeignRosterAndUnknownDangerNeverAppear()
    {
        var g = FleetFixtures.Game(amebas: true, rivals: 1); FleetFixtures.AttachForStateTest(g);
        var own = g.State.Empires[0].Id;
        var first = FleetQueries.Project(g.Content, g.State, own);
        Assert.Single(first.Officers); Assert.Single(first.Ships);
        var other = g.State.Clone(); other.Empires[1].Fleets!.Officers[0].Name = "Hidden commander";
        var habitat = other.Scouting!.Habitat!;
        var unknown = other.Stars.First(star => star.Id != habitat.Star && !other.Colonies.Any(col => other.FindPlanet(col.PlanetId)!.StarId == star.Id));
        other.Scouting.Habitat = habitat with { Star = unknown.Id };
        string before = FleetFixtures.Bytes(g.State), otherBefore = FleetFixtures.Bytes(other);
        FleetFixtures.Valid(new(g.Content, other));
        string expected = JsonSerializer.Serialize(first);
        for (int i = 0; i < 10; i++)
            Assert.Equal(expected, JsonSerializer.Serialize(FleetQueries.Project(g.Content, other, own)));
        Assert.DoesNotContain("Hidden commander", expected);
        Assert.Equal(before, FleetFixtures.Bytes(g.State)); Assert.Equal(otherBefore, FleetFixtures.Bytes(other));
    }

    [Fact]
    public void OwnViewsIncludeReservedIndependentHullsAndImmutableRecords()
    {
        var g = FleetFixtures.Nested();
        var owner = g.State.Empires[0].Id;
        var view = FleetQueries.Project(g.Content, g.State, owner);
        Assert.Single(view.Fleets); Assert.Single(view.Requests); Assert.Single(view.Officers);
        Assert.Equal(JsonSerializer.Serialize(view), JsonSerializer.Serialize(OpenAntares.Simulation.Knowledge.EmpireViewProjector.Project(g.Content, g.State, owner).Fleets));
        string projected = JsonSerializer.Serialize(view);
        g.State.Empires[0].Fleets!.Fleets[0].Name = "Changed later";
        g.State.Empires[0].Fleets!.Requests[0].Slots[0].RoleId = "Changed later";
        Assert.Equal(projected, JsonSerializer.Serialize(view));
    }
}
