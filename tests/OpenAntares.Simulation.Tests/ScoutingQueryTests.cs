using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Scouting;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ScoutingQueryTests
{
    [Fact] public void ExactOddsUseMathematicalFloorAndBounds()
    {
        var d=ScoutingFixtures.Content().SpaceAmeba!;
        Assert.Equal(95,AmebaOdds.LateEscape(d,300));Assert.Equal(75,AmebaOdds.Study(d,300));Assert.Equal(5,AmebaOdds.LateEscape(d,100));Assert.Equal(25,AmebaOdds.Study(d,100));Assert.Equal(49,AmebaOdds.Study(d,199));
        Assert.Equal(50,AmebaOdds.Study(d,200));Assert.Equal(50,AmebaOdds.LateEscape(d,200));
        d=d with{Study=new ProbabilityRule(0,0,0,100),LateEscape=new ProbabilityRule(100,0,0,100)};
        Assert.Equal(0,AmebaOdds.Study(d,300));Assert.Equal(100,AmebaOdds.LateEscape(d,300));
        Assert.False(AmebaOdds.Succeeds(75,75));Assert.True(AmebaOdds.Succeeds(75,74));Assert.False(AmebaOdds.Succeeds(0,0));Assert.True(AmebaOdds.Succeeds(100,99));
    }
    [Fact] public void AutomaticDepartureHasExactStableConsequencesAndPureReads()
    {
        var g=ScoutingFixtures.AtAmeba();var id=g.State.Empires[0].Id;string before=ScoutingFixtures.Bytes(g.State);
        for(int i=0;i<3;i++)
        {
            var view=ScoutQueries.Project(g.Content,g.State,id);var ship=Assert.Single(view.Vessels);var token=Assert.Single(view.Attention);
            Assert.Equal(Fixtures.HomeStar,ship.EffectiveDestination);Assert.Equal("depart",token.Action);Assert.Equal(100,token.Percent);Assert.Equal(7125,ship.CombinedRetentionNumerator);Assert.Equal(10,ship.DepartureDeadline);Assert.Equal(600,ship.ResearchReward);
            Assert.IsType<SetScoutRetreatOrderCommand>(Assert.Single(ScoutQueries.PreviewPreparation(g.Content,g.State,id).Commands));
            _=EmpireViewProjector.Project(g.Content,g.State,id);_=new GameSession(g.Content,g.State).Forecast();
        }
        Assert.Equal(before,ScoutingFixtures.Bytes(g.State));
        var tokenBefore=ScoutQueries.Project(g.Content,g.State,id).Attention;g.State.Vessels[0].PendingMove=Fixtures.HomeStar;
        Assert.Equal(tokenBefore.ToArray(),ScoutQueries.Project(g.Content,g.State,id).Attention.ToArray());
        g.State.Empires[0].Scouting!.Receipts.Add(new(tokenBefore[0],10));g.State.Vessels[0].PendingMove=new(6);g.State.Empires[0].Ready=true;
        Assert.Contains(OpenAntares.Simulation.State.GameStateValidator.Validate(g.Content,g.State,true),e=>e.Contains("consent"));
    }
    [Fact] public void HiddenForeignFactsCannotChangeOwnPreviewOrTokens()
    {
        var g=ScoutingFixtures.TwoHumansAndAi();var owner=g.State.Empires[0].Id;
        g.State.Vessels.Add(new(){Id=new(g.State.AllocateId()),EmpireId=g.State.Empires[2].Id,RoleId="scout",StationaryStarId=new(8),Scout=new(),Crisis=new()});
        string before=JsonSerializer.Serialize(ScoutQueries.Project(g.Content,g.State,owner));
        var foreign=g.State.Empires[2];foreign.Scouting!.KnownHabitats.Add(new(1,new(16),0));foreign.Scouting.SuccessfulStudies.Add(new(1,0,g.State.Vessels.Last().Id));
        g.State.Vessels.Last().PendingMove=new(16);g.State.Vessels.Last().CarriedPopulation=999;foreign.ResearchReserve=9999;
        Assert.Equal(before,JsonSerializer.Serialize(ScoutQueries.Project(g.Content,g.State,owner)));
    }
    [Fact] public void SurvivalMayOverrideNeutralCautionOnlyForAnActualFallbackAndKeepsItsToken()
    {
        var g=ScoutingFixtures.TwoHumansAndAi();var s=g.State;var e=s.Empires[0];var v=s.Vessels[0];v.StationaryStarId=new(16);v.Scout!.Posture=ScoutPosture.AvoidEncounters;v.AmebaEncounter=new(){Number=1,HabitatId=1,ArrivalTurn=s.Turn};e.Scouting!.NextEncounterNumber=2;e.Scouting.KnownHabitats.Add(new(1,new(16),s.Turn));
        s.Vessels.Add(new(){Id=new(s.AllocateId()),EmpireId=s.Empires[2].Id,RoleId="scout",StationaryStarId=Fixtures.HomeStar,Scout=new(),Crisis=new()});ObservationUpdater.Refresh(s,s.Turn);
        var before=ScoutQueries.Project(g.Content,s,e.Id);Assert.Equal(Fixtures.HomeStar,before.Vessels[0].EffectiveDestination);
        v.PendingMove=Fixtures.HomeStar;Assert.Equal(before.Attention.ToArray(),ScoutQueries.Project(g.Content,s,e.Id).Attention.ToArray());Assert.Empty(ScoutQueries.PreviewPreparation(g.Content,s,e.Id).Commands);
        // A dated foreign colony remains excluded even though the ship has departed.
        e.Surveys.Add(ObservationUpdater.Observe(s,new(6),s.Turn));e.Surveys.Sort((a,b)=>a.StarId.CompareTo(b.StarId));
        Assert.Equal(Fixtures.HomeStar,ScoutQueries.Project(g.Content,s,e.Id).Vessels[0].EffectiveDestination);
    }
    [Fact] public void RetreatPrefersOwnColonyThenSurveyAndNeverInventsAnUnknownRouteForHumans()
    {
        var g=ScoutingFixtures.AtAmeba();var e=g.State.Empires[0];var v=g.State.Vessels[0];v.Scout!.Posture=ScoutPosture.AvoidEncounters;
        Assert.Equal(Fixtures.HomeStar,ScoutQueries.Project(g.Content,g.State,e.Id).Vessels[0].EffectiveDestination);
        g.State.Colonies.Clear();e.Surveys.RemoveAll(x=>x.StarId==Fixtures.HomeStar);
        Assert.Null(ScoutQueries.Project(g.Content,g.State,e.Id).Vessels[0].EffectiveDestination);
        Assert.Single(ScoutQueries.PreviewPreparation(g.Content,g.State,e.Id).Blockers);
        v.PendingMove=new(6);Assert.Empty(ScoutQueries.PreviewPreparation(g.Content,g.State,e.Id).Blockers);
        Assert.Equal(new StarId(6),ScoutQueries.Project(g.Content,g.State,e.Id).Vessels[0].EffectiveDestination);
    }
    [Fact] public void KnownHabitatIsDatedStaticKnowledgeAndCargoNeedsManualResponse()
    {
        var g=ScoutingFixtures.AtAmeba();var v=g.State.Vessels[0];v.RoleId="harl_settler";v.CarriedPopulation=2;v.Scout=null;
        var view=ScoutQueries.Project(g.Content,g.State,g.State.Empires[0].Id);Assert.Single(view.Habitats);Assert.Equal(10,view.Habitats[0].ObservedTurn);Assert.Null(view.Vessels[0].EffectiveDestination);Assert.Single(ScoutQueries.PreviewPreparation(g.Content,g.State,view.Owner).Blockers);
    }
}
