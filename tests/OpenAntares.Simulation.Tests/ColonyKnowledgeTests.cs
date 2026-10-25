using System;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ColonyKnowledgeTests
{
    [Fact] public void OwnAppearanceProjectsAndUnknownRemainsUnknown() {
        var g=ColonyFixtures.Game();var view=EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id);var home=view.Stars.Single(s=>s.Chart.Id==view.Colonies[0].StarId);Assert.Equal(g.State.Planets[0].Appearance,home.Planets!.Value[0].Appearance);Assert.Null(view.Stars.Single(s=>s.Chart.Id.Value==6).Planets);
    }
    [Fact] public void StalePortraitNeverReadsHiddenLiveDescriptor() {
        var g=ColonyFixtures.Game();var e=g.State.Empires[0];var ship=g.State.Vessels[0];ship.StationaryStarId=new(6);ObservationUpdater.Refresh(g.State,0);ship.StationaryStarId=g.State.Stars[0].Id;g.State.Turn=1;ObservationUpdater.Refresh(g.State,1);
        var first=EmpireViewProjector.Project(g.Content,g.State,e.Id).Stars.Single(s=>s.Chart.Id.Value==6);Assert.False(first.Current);var expected=first.Planets!.Value[0].Appearance;Assert.NotNull(expected);
        g.State.Planets.Single(p=>p.StarId.Value==6).Appearance=new(1,7,ulong.MaxValue);var after=EmpireViewProjector.Project(g.Content,g.State,e.Id).Stars.Single(s=>s.Chart.Id.Value==6);Assert.Equal(expected,after.Planets!.Value[0].Appearance);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
    }
    [Fact] public void PreviewIsReadOnlyAndRejectsLegacyUnknownAndForeign() {
        var g=ColonyFixtures.WithFoundations("harl",6,"industrial");var view=EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id);string before=JsonSerializer.Serialize(g.State);
        var preview=GovernorPlanningQueries.Preview(g.Content,view,view.Colonies[0].Id,ColonyFixtures.Snapshot(g.Content,"research"),true,true);Assert.NotNull(preview);Assert.Equal(new Workforce(2,1,3),preview!.Workforce);Assert.Equal(before,JsonSerializer.Serialize(g.State));
        Assert.Null(GovernorPlanningQueries.Preview(g.Content,view,new(int.MaxValue),null,true,true));Assert.Null(GovernorPlanningQueries.Preview(g.Content,view with{RulesVersion=4},view.Colonies[0].Id,null,false,true));Assert.Null(GovernorPlanningQueries.Preview(g.Content,view,view.Colonies[0].Id,null,true,true));Assert.Null(GovernorPlanningQueries.Preview(g.Content,view,view.Colonies[0].Id,null,false,false));
    }
}
