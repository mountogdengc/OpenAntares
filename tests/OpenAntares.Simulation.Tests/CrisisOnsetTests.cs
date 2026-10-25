using System;
using System.Linq;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisOnsetTests
{
    [Fact]
    public void ScheduledChanceAndTargetUseIndependentLiteralStream()
    {
        var g=CrisisFixtures.Game();var state=g.State;var first=state.Colonies[0];
        state.Colonies.Add(new(){Id=new(state.AllocateId()),PlanetId=new(7),EmpireId=first.EmpireId,Population=3,Workforce=new(3,0,0),HabitatMode="native",Development=new(),Crisis=new()});
        var p=new PlanetState{Id=new(state.AllocateId()),StarId=new(6),Orbit=2,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea",Appearance=new(1,1,9)};state.Planets.Add(p);
        state.Colonies.Add(new(){Id=new(state.AllocateId()),PlanetId=p.Id,EmpireId=first.EmpireId,Population=2,Workforce=new(2,0,0),HabitatMode="native",Development=new(),Crisis=new()});state.Turn=19;ObservationUpdater.Refresh(state,19);
        Assert.Empty(GameStateValidator.Validate(g.Content,state));int nextId=state.NextEntityId;ulong originalRandom=state.Random.State;
        var candidate=state.Clone();var context=CrisisResolutionContext.Capture(g.Content,state);CrisisOnset.Apply(g.Content,candidate,context);
        var e=candidate.Empires[0].Crisis!;Assert.NotNull(e.Episode);Assert.Equal(first.Id,e.Episode!.Source.Colony);Assert.Equal(1200,e.Episode.CureCost);Assert.Equal(6,e.Episode.SourcePopulation);Assert.Equal(2,e.NextEpisodeNumber);
        Assert.Equal(9431316384986380083UL,e.Random.State);Assert.Equal(originalRandom,candidate.Random.State);Assert.Equal(nextId,candidate.NextEntityId);Assert.Equal(2,candidate.Colonies[0].Crisis!.Case!.GraceRemaining);Assert.Equal(0,candidate.Colonies[0].Crisis!.Case!.Severity);
    }
    [Fact]
    public void FailedOwner46ChanceConsumesNoSelectionDrawAndCannotChangeHuman()
    {
        var g=CrisisFixtures.Game();var s=g.State;s.Empires.Add(new(){Id=new(46),Controller=ControllerKind.Ai,AiPolicyId="expansion_v1",SpeciesId="harl",Development=new(),Crisis=new(){Random=CrisisInitializer.CreateRandom(42,new(46))}});
        s.Colonies.Add(new(){Id=new(47),PlanetId=new(7),EmpireId=new(46),Population=3,Workforce=new(3,0,0),HabitatMode="native",Development=new(),Crisis=new()});s.NextEntityId=48;s.Turn=19;ObservationUpdater.Refresh(s,19);Assert.Empty(GameStateValidator.Validate(g.Content,s));
        var candidate=s.Clone();var context=CrisisResolutionContext.Capture(g.Content,s);CrisisOnset.Apply(g.Content,candidate,context);
        Assert.Null(candidate.Empires[1].Crisis!.Episode);Assert.Equal(15243192261855343020UL,candidate.Empires[1].Crisis!.Random.State);Assert.Equal(9431316384986380083UL,candidate.Empires[0].Crisis!.Random.State);
    }
    [Theory][InlineData("before20")][InlineData("unscheduled")][InlineData("cooldown")][InlineData("immune")][InlineData("zero_colonies")][InlineData("episode")][InlineData("off")]
    public void IneligibleChecksConsumeNoDraws(string kind)
    {
        var g=kind=="episode"?CrisisFixtures.Active():CrisisFixtures.Game(enabled:kind!="off");g.State.Turn=kind=="before20"?18:kind=="unscheduled"?20:19;
        if(kind=="cooldown")g.State.Empires[0].Crisis!.NextEligibleTurn=21;
        if(kind=="immune"){g.State.Colonies[0].Crisis!.ImmunityFamilyId="ash_bloom";g.State.Colonies[0].Crisis!.ImmuneUntilTurn=30;}
        if(kind=="zero_colonies")g.State.Colonies.Clear();ObservationUpdater.Refresh(g.State,g.State.Turn);
        string before=SaveGame.Serialize(g.Content,g.State);var candidate=g.State.Clone();var ctx=CrisisResolutionContext.Capture(g.Content,g.State);CrisisOnset.Apply(g.Content,candidate,ctx);
        Assert.Equal(before,SaveGame.Serialize(g.Content,candidate));Assert.Empty(ctx.Events);
    }
    [Fact]
    public void ExhaustedLocalCounterFailsWithoutChangingOriginal()
    {
        var g=CrisisFixtures.Game();g.State.Turn=19;g.State.Empires[0].Crisis!.NextEpisodeNumber=long.MaxValue-1;ObservationUpdater.Refresh(g.State,19);
        string before=SaveGame.Serialize(g.Content,g.State);var candidate=g.State.Clone();var context=CrisisResolutionContext.Capture(g.Content,g.State);
        Assert.Throws<OverflowException>(()=>CrisisOnset.Apply(g.Content,candidate,context));Assert.Equal(before,SaveGame.Serialize(g.Content,g.State));
    }
    [Fact]
    public void EditedFamilyScheduleAndCostRemainDataDriven()
    {
        var g=CrisisFixtures.Game();var definition=g.Content.Plagues["ash_bloom"] with{Id="silver_fever",FirstCheckTurn=3,CheckInterval=7,CheckChancePercent=100,CureBase=100,CurePerPopulation=50,GraceTurns=3};
        var content=g.Content with{Plagues=g.Content.Plagues.Remove("ash_bloom").Add(definition.Id,definition)};
        g=g with{Content=content};g.State.Turn=2;ObservationUpdater.Refresh(g.State,2);g=CrisisProgressionTests.Step(g);
        Assert.Equal("silver_fever",g.State.Empires[0].Crisis!.Episode!.FamilyId);Assert.Equal(400,g.State.Empires[0].Crisis!.Episode!.CureCost);Assert.Equal(3,g.State.Colonies[0].Crisis!.Case!.GraceRemaining);
    }
    [Fact]
    public void ViewsAndOffPhasesNeverAdvanceCrisisStream()
    {
        var g=CrisisFixtures.Game(enabled:false);string before=SaveGame.Serialize(g.Content,g.State);var context=CrisisResolutionContext.Capture(g.Content,g.State);var candidate=g.State.Clone();
        new GameSession(g.Content,g.State).View(g.State.Empires[0].Id);CrisisProgression.Apply(g.Content,g.State,candidate,EconomyCalculator.Calculate(g.Content,g.State),context);CrisisOnset.Apply(g.Content,candidate,context);CrisisReports.CloseFinishedEpisodes(g.Content,candidate,context);CrisisReports.Finalize(g.State,candidate,context);
        Assert.Equal(before,SaveGame.Serialize(g.Content,candidate));Assert.Empty(context.Events);
    }
}
