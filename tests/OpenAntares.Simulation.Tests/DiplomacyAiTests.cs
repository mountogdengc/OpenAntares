using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacyAiTests
{
    [Fact] public void RivalUsesOwnKnowledgeThresholdsAndCooldown()
    {
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create());var ai=s.State.Empires[1].Id;var pair=s.State.Diplomacy!.Pairs.Single();
        foreach(var party in pair.Parties)party.Attitude.Streak=5;
        s.State.Colonies.Single(c=>c.EmpireId==ai).ProductionReserve=500;
        var view=s.View(ai);var commands=new List<DiplomacyCommand>();var before=s.State.Random;
        DiplomacyPlanner.Plan(s.Content,view,commands.Add);Assert.Single(commands);Assert.IsType<DiplomacyAction.OfferTrade>(commands[0].Action);Assert.True(commands.Count<=3*view.Contacts.Length);Assert.Equal(before,s.State.Random);
        pair.Parties.Single(p=>p.Owner==ai).NextAiOfferTurn=5;commands.Clear();DiplomacyPlanner.Plan(s.Content,s.View(ai),commands.Add);Assert.Empty(commands);
    }
    [Fact] public void RivalRespondsButNeverAcceptsUnfundedTrade()
    {
        var s=DiplomacyCommandTests.Funded();var human=s.State.Empires[0].Id;var ai=s.State.Empires[1].Id;s.State.Empires[1].Controller=ControllerKind.Ai;s.State.Empires[1].AiPolicyId="expansion_v1";
        foreach(var party in s.State.Diplomacy!.Pairs.Single().Parties)party.Attitude.Streak=5;
        Assert.True(DiplomacyCommandTests.Act(s,human,new DiplomacyAction.OfferTrade(s.State.Colonies.Single(c=>c.EmpireId==human).Id)).Accepted);
        s.State.Colonies.Single(c=>c.EmpireId==ai).ProductionReserve=0;
        var commands=new List<DiplomacyCommand>();DiplomacyPlanner.Plan(s.Content,s.View(ai),commands.Add);var answer=Assert.IsType<DiplomacyAction.Answer>(Assert.Single(commands).Action);Assert.False(answer.Accept);Assert.Null(answer.Hub);
    }
    [Theory] [InlineData("harl")][InlineData("peldra")][InlineData("sulan")][InlineData("wenth")]
    public void SamePolicyUsesDefinitionsWithoutSpeciesBranches(string species)
    {
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create(species));var pair=s.State.Diplomacy!.Pairs.Single();foreach(var p in pair.Parties)p.Attitude.Streak=5;
        var owner=s.State.Empires[0].Id;var hub=s.State.Colonies.Single(c=>c.EmpireId==owner);hub.ProductionReserve=500;
        var a=new List<DiplomacyCommand>();DiplomacyPlanner.Plan(s.Content,s.View(owner),a.Add);
        Assert.Single(a);Assert.IsType<DiplomacyAction.OfferTrade>(a[0].Action);
    }
    [Fact] public void CoordinatorUsesSharedCommandsOnPrivateAllReadySnapshot()
    {
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create());var ai=s.State.Empires[1].Id;
        foreach(var p in s.State.Diplomacy!.Pairs[0].Parties)p.Attitude.Streak=5;
        s.State.Colonies.Single(c=>c.EmpireId==ai).ProductionReserve=500;s.State.Empires[0].Ready=true;
        var original=s.State;var outcome=DiplomacyTurnCoordinator.Prepare(s.Content,s.State);
        Assert.True(outcome.Succeeded);Assert.Same(original,s.State);Assert.Null(original.Diplomacy!.Pairs[0].Pending);
        Assert.Equal(ai,outcome.State.Diplomacy!.Pairs[0].Pending!.Proposer);Assert.Equal(0,outcome.State.Turn);Assert.Equal(500,outcome.State.Colonies.Single(c=>c.EmpireId==ai).ProductionReserve);
    }
    [Fact] public void HiddenForeignEconomyCannotChangePlanAndCooldownDoesNotBlockAnswer()
    {
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create());var ai=s.State.Empires[1].Id;
        foreach(var p in s.State.Diplomacy!.Pairs[0].Parties)p.Attitude.Streak=5;
        s.State.Colonies.Single(c=>c.EmpireId==ai).ProductionReserve=500;
        var before=new List<DiplomacyCommand>();DiplomacyPlanner.Plan(s.Content,s.View(ai),before.Add);
        s.State.Colonies.Single(c=>c.EmpireId!=ai).ProductionReserve=99999;
        var after=new List<DiplomacyCommand>();DiplomacyPlanner.Plan(s.Content,s.View(ai),after.Add);Assert.Equal(before.ToArray(),after.ToArray());
        var human=s.State.Empires[0].Id;Assert.True(DiplomacyCommandTests.Act(s,human,new DiplomacyAction.OfferTrade(s.State.Colonies.Single(c=>c.EmpireId==human).Id)).Accepted);
        s.State.Diplomacy!.Pairs[0].Parties.Single(p=>p.Owner==ai).NextAiOfferTurn=100;
        after.Clear();DiplomacyPlanner.Plan(s.Content,s.View(ai),after.Add);Assert.True(Assert.IsType<DiplomacyAction.Answer>(Assert.Single(after).Action).Accept);
    }}
