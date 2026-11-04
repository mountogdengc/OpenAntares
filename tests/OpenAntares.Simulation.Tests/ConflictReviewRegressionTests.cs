using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Commands;
using System.Text.Json.Nodes;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictReviewRegressionTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public void ForeignPrivateLiftOrMoveDoesNotChangeVictimForecastBeforePublication(bool move)
    {
        var g=ConflictBlockadeTests.Activate(ConflictBlockadeTests.Economic());var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var fleet=g.State.FindEmpire(a)!.Fleets!.Fleets[0];var before=g.View(b);
        Assert.Equal(450,before.Colonies.Single().Economy.NetProduction.Total);
        if(move)
        {
            var destination=g.State.FindPlanet(g.State.Colonies.First(c=>c.EmpireId==a).PlanetId)!.StarId;
            var action=new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(fleet.Id),destination);var preview=FleetQueries.PreviewAction(g.Content,g.View(a),action);Assert.True(preview.Allowed,preview.Rejection);Assert.True(g.Execute(new FleetCommand(a,g.State.Turn,action){Fingerprint=preview.Fingerprint}).Accepted);
        }
        else ConflictDeclarationTests.Issue(g,a,new ConflictAction.LiftBlockade(fleet.Id));
        Assert.Empty(g.State.Conflict!.Owners.Single(o=>o.Owner==a).Blockades);
        Assert.Equal(801,EconomyCalculator.Calculate(g.Content,g.State).Colonies.Single(c=>c.Empire==b).NetProduction.Total);
        Assert.Equal(450,g.View(b).Colonies.Single().Economy.NetProduction.Total);
        Assert.Equal(JsonSerializer.Serialize(before),JsonSerializer.Serialize(g.View(b)));
        var saved=SaveGame.Deserialize(SaveGame.Serialize(g.Content,g.State));Assert.True(saved.Succeeded,string.Join("; ",saved.Errors));var loaded=new GameSession(saved.Game!.Content,saved.Game.State);Assert.Equal(450,loaded.View(b).Colonies.Single().Economy.NetProduction.Total);
        var published=ConflictAtomicTurnTests.Advance(loaded);Assert.Equal(801,EmpireViewProjector.Project(g.Content,published.State,b).Colonies.Single().Economy.NetProduction.Total);
    }
    [Fact]
    public void DetachedStragglerLossCannotLowerCleanMainCohesion()
    {
        var g=ConflictFixtures.WithEncounter();var e=g.State.Conflict!.Encounters.Single();var straggler=ConflictFixtures.AddEscort(g,e.Pair.Low,e.Star,20);var fleet=g.State.FindEmpire(e.Pair.Low)!.Fleets!.Fleets.Single(f=>f.Id==e.LowSide.Fleet);fleet.Members.Add(straggler.Id);straggler.Fleet=fleet.Id;e.LowSide.Members.Add(straggler.Id);ConflictWithdrawalTests.Upgrade(g,e.Pair.Low);e.LowSide.WithdrawQueued=true;e.LowSide.Orders=ConflictOrders.Default with{Stragglers=StragglerPolicy.DetachCrippled};int before=g.State.FindVessel(e.LowSide.Flagship)!.Combat!.Cohesion;
        var result=ConflictWithdrawalTests.Resolve(g);Assert.Null(result.Candidate.FindVessel(straggler.Id));Assert.Equal(before,result.Candidate.FindVessel(e.LowSide.Flagship)!.Combat!.Cohesion);
    }
    [Fact]
    public void ForgedArmedFlagshipInMixedLiveDefenseIsRejected()
    {
        var g=ConflictFixtures.WithMixedDefense();var side=g.State.Conflict!.Encounters.Single().HighSide;Assert.NotEqual(side.Flagship,side.Members[0]);side.Flagship=side.Members[0];
        Assert.NotEmpty(GameStateValidator.Validate(g.Content,g.State));Assert.Throws<ArgumentException>(()=>SaveGame.Serialize(g.Content,g.State));
    }
    [Fact]
    public void HumanCanProposeUnconditionalPeaceImmediatelyAfterPublishedWithdrawal()
    {
        var g=ConflictFixtures.WithEncounter();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;ConflictDeclarationTests.Issue(g,a,new ConflictAction.ProposePeace(b));g=ConflictDeclarationTests.Publish(g);var offer=g.State.Conflict!.Pairs.Single().PendingPeace!;ConflictDeclarationTests.Issue(g,a,new ConflictAction.WithdrawPeace(b,offer.Id));
        Assert.True(ConflictQueries.Preview(g.Content,g.View(a),new ConflictAction.ProposePeace(b)).Allowed);ConflictDeclarationTests.Issue(g,a,new ConflictAction.ProposePeace(b));
    }
    [Fact]
    public void ActualRoundReadbackIncludesSourceTargetDamageAndEachHull()
    {
        var g=ConflictFixtures.WithEncounter();var outcome=ConflictAtomicTurnTests.Advance(g);var receipt=outcome.State.Conflict!.Encounters.Single().Rounds.Single();
        var method=typeof(ConflictReports).GetMethod("DescribeRound");Assert.NotNull(method);string text=(string)method.Invoke(null,new object[]{receipt})!;
        foreach(var shot in receipt.Shots){Assert.Contains($"Ship {shot.Source.Id} → armed ship {shot.Target.Id}",text);Assert.Contains($"{shot.Damage} damage",text);}
        foreach(var hull in receipt.Own.Concat(receipt.Opposing))Assert.Contains($"{hull.Id.Value}: {hull.Before} → {hull.After}",text);
        Assert.Contains("no spill",text);Assert.Contains("armor",text);
    }
    [Fact]
    public void ActualShotCalculationsSurviveSaveAndRejectAlteredDamage()
    {
        var g=ConflictFixtures.WithEncounter();var outcome=ConflictAtomicTurnTests.Advance(g);var receipt=outcome.State.Conflict!.Encounters.Single().Rounds.Single();Assert.Equal(receipt.Shots.Length,receipt.Calculations.Length);
        string saved=SaveGame.Serialize(g.Content,outcome.State);Assert.True(SaveGame.Deserialize(saved).Succeeded);
        var node=JsonNode.Parse(saved)!;var calculation=node["state"]!["conflict"]!["encounters"]![0]!["rounds"]![0]!["calculations"]![0]!;calculation["damage"]=calculation["damage"]!.GetValue<int>()+1;
        Assert.False(SaveGame.Deserialize(node.ToJsonString()).Succeeded);
    }
    [Fact]
    public void AiStillUsesTheEmbeddedOutgoingPeaceCooldown()
    {
        var original=ConflictPeaceTests.War(10);var content=original.Content with{Conflict=original.Content.Conflict! with{PeaceCooldown=7}};var a=original.State.Empires[0];var b=original.State.Empires[1].Id;a.Controller=ControllerKind.Ai;a.AiPolicyId=content.Conflict!.AlgorithmId;var g=new GameSession(content,original.State);
        GameSession Issue(GameSession current,ConflictAction action){var preview=ConflictQueries.Preview(content,current.View(a.Id),action);Assert.True(preview.Allowed);var result=CommandProcessor.ExecuteConflictPreparation(content,current.State,new ConflictCommand(a.Id,current.State.Turn,action,preview.Token));Assert.True(result.Accepted,result.Rejection?.Message);return new(content,result.State);}
        g=Issue(g,new ConflictAction.ProposePeace(b));g=ConflictDeclarationTests.Publish(g);g=Issue(g,new ConflictAction.WithdrawPeace(b,g.State.Conflict!.Pairs.Single().PendingPeace!.Id));long next=g.State.Conflict.Pairs.Single().Parties.Single(p=>p.Owner==a.Id).NextPeaceOfferTurn;Assert.Equal((long)g.State.Turn+7,next);
        Assert.DoesNotContain(ConflictPlanner.Plan(content,g.View(a.Id),ConflictPlanningStage.Responses),command=>command is ConflictCommand{Action:ConflictAction.ProposePeace});
        var view=g.View(a.Id);view=view with{Turn=(int)next,Conflict=view.Conflict! with{Turn=(int)next}};
        Assert.Contains(ConflictPlanner.Plan(content,view,ConflictPlanningStage.Responses),command=>command is ConflictCommand{Action:ConflictAction.ProposePeace});
    }
}
