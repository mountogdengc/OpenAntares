using System;
using System.Linq;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;

public sealed class ScoutingStateTests
{
    [Fact] public void ValidExplicitStatesHaveNoErrors()
    {
        foreach(var g in new[]{ScoutingFixtures.Game(),ScoutingFixtures.Game(false),ScoutingFixtures.AtAmeba(),ScoutingFixtures.AtAmeba(AmebaStage.Exposed,200),ScoutingFixtures.TwoHumansAndAi(),ScoutingFixtures.WithSecondScout()})Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
    }
    [Fact] public void DeepCloneRetainsAndSeparatesEveryMutableScoutPart()
    {
        var g=ScoutingFixtures.AtAmeba();var clone=g.State.Clone();Assert.NotNull(clone.Scouting);Assert.NotNull(clone.Empires[0].Scouting);Assert.NotNull(clone.Vessels[0].Scout);Assert.NotNull(clone.Vessels[0].AmebaEncounter);
        ulong before=g.State.Empires[0].Scouting!.RiskRandom!.State;
        clone.Empires[0].Scouting!.RiskRandom!.NextUInt32();clone.Empires[0].Scouting!.KnownHabitats.Clear();clone.Vessels[0].AmebaEncounter!.Revision++;
        Assert.Equal(before,g.State.Empires[0].Scouting!.RiskRandom!.State);Assert.Single(g.State.Empires[0].Scouting!.KnownHabitats);Assert.Equal(1,g.State.Vessels[0].AmebaEncounter!.Revision);
        Assert.NotSame(g.State.Scouting!.PlacementRandom,clone.Scouting!.PlacementRandom);
        Assert.NotSame(g.State.Empires[0].Scouting!.CompletedDigests,clone.Empires[0].Scouting!.CompletedDigests);
        Assert.NotSame(g.State.Empires[0].Scouting!.Receipts,clone.Empires[0].Scouting!.Receipts);
    }
    [Theory] [InlineData("game")] [InlineData("empire")] [InlineData("scout")] [InlineData("survival")] [InlineData("increment")]
    [InlineData("habitat_id")] [InlineData("colony_habitat")] [InlineData("future_known")] [InlineData("duplicate_known")]
    [InlineData("unknown_claim")] [InlineData("future_arrival")] [InlineData("encounter_number")] [InlineData("revision")]
    [InlineData("transit_encounter")] [InlineData("stage")] [InlineData("legacy_injection")] [InlineData("off_injection")] [InlineData("ready_unread")]
    public void CorruptScoutStateIsRejected(string kind)
    {
        var g=ScoutingFixtures.AtAmeba();var s=g.State;var e=s.Empires[0].Scouting!;var v=s.Vessels[0];
        switch(kind)
        {
            case "game":s.Scouting=null;break;case "empire":s.Empires[0].Scouting=null;break;case "scout":v.Scout=null;break;case "survival":v.Scout!.WithdrawFromAmeba=false;break;case "increment":e.RiskRandom!.Increment=2;break;
            case "habitat_id":s.Scouting!.Habitat=s.Scouting.Habitat! with{Id=2};break;case "colony_habitat":s.Scouting!.Habitat=s.Scouting.Habitat! with{Star=Fixtures.HomeStar};break;
            case "future_known":e.KnownHabitats[0]=e.KnownHabitats[0] with{ObservedTurn=11};break;case "duplicate_known":e.KnownHabitats.Add(e.KnownHabitats[0]);break;case "unknown_claim":e.SuccessfulStudies.Add(new(99,s.Turn,v.Id));break;
            case "future_arrival":v.AmebaEncounter!.ArrivalTurn=11;break;case "encounter_number":v.AmebaEncounter!.Number=e.NextEncounterNumber;break;case "revision":v.AmebaEncounter!.Revision=0;break;
            case "transit_encounter":v.StationaryStarId=null;v.Transit=new(new(8),Fixtures.HomeStar,2,1);break;case "stage":v.AmebaEncounter!.Stage=(AmebaStage)99;break;
            case "legacy_injection":s.RulesVersion=6;break;case "off_injection":s.Scouting!.Enabled=false;break;case "ready_unread":s.Empires[0].Ready=true;break;
        }
        Assert.NotEmpty(GameStateValidator.Validate(g.Content,s,allowAllHumansReady:true));
    }
    [Fact] public void CargoHasEncounterButNeverScoutOrExposedState()
    {
        var g=ScoutingFixtures.AtAmeba();var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=g.Content.Vessels[v.RoleId].EmbarkPopulation;v.Scout=null;
        Assert.Empty(GameStateValidator.Validate(g.Content,g.State));Assert.Null(v.Scout);
        v.AmebaEncounter!.Stage=AmebaStage.Exposed;Assert.NotEmpty(GameStateValidator.Validate(g.Content,g.State));
    }
}
