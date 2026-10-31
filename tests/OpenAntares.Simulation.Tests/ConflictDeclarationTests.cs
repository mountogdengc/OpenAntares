using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictDeclarationTests
{
    internal static CommandOutcome Issue(GameSession g,EmpireId owner,ConflictAction action)
    {var p=ConflictQueries.Preview(g.Content,g.View(owner),action);Assert.True(p.Allowed,p.RejectionCode);var result=g.Execute(new ConflictCommand(owner,g.State.Turn,action,p.Token));Assert.True(result.Accepted,result.Rejection?.Message);return result;}
    // Isolated relationship boundary; complete public turn integration belongs to Task15.
    internal static GameSession Publish(GameSession g)
    {
        var before=g.State;var candidate=before.Clone();var context=ConflictResolutionContext.Capture(g.Content,before);ConflictRelations.Prepare(g.Content,before,candidate,context);candidate.Turn=checked(before.Turn+1);ConflictRelations.Finalize(g.Content,before,candidate,context);ObservationUpdater.Refresh(candidate,candidate.Turn);ConflictObservations.Refresh(g.Content,candidate);return new(g.Content,candidate);
    }
    [Fact] public void DeclarationsPublishOnceAndCanonicalPairWins()
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;int allocator=g.State.NextEntityId;ulong random=g.State.Random.State;
        Issue(g,a,new ConflictAction.DeclareWar(b));Assert.Equal(RelationKind.Peace,g.State.Conflict!.Pairs[0].Relation);Assert.Equal(allocator,g.State.NextEntityId);Assert.Equal(random,g.State.Random.State);Assert.Empty(g.State.Conflict.Owners.SelectMany(o=>o.Events));
        var after=Publish(g);var p=after.State.Conflict!.Pairs[0];Assert.Equal(RelationKind.War,p.Relation);Assert.Equal(1,p.WarTurn);Assert.Equal(new[]{a},p.Declarers);Assert.Empty(p.Declarations);Assert.Empty(after.State.Conflict.Encounters);Assert.Empty(ConflictStateValidator.Validate(after.Content,after.State));
    }
    [Fact] public void CrossedDeclarationsHaveOneWarAndBothDeclarers()
    {foreach(bool reverse in new[]{false,true}){var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;foreach(var actor in reverse?new[]{b,a}:new[]{a,b})Issue(g,actor,new ConflictAction.DeclareWar(actor==a?b:a));var p=Publish(g).State.Conflict!.Pairs[0];Assert.Equal(RelationKind.War,p.Relation);Assert.Equal(new[]{a,b},p.Declarers);}}
    [Fact] public void ChangedOrdersClearOnlyOwnReadyAndValidNoOpKeepsIt()
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;Issue(g,a,new ConflictAction.DeclareWar(b));var ready=g.Execute(new SetReadyCommand(a,g.State.Turn,true));Assert.True(ready.Accepted,ready.Rejection?.Message);Assert.True(g.State.FindEmpire(a)!.Ready);
        var same=Issue(g,a,new ConflictAction.DeclareWar(b));Assert.False(same.Changed);Assert.True(g.State.FindEmpire(a)!.Ready);
        Issue(g,a,new ConflictAction.RevokeDeclaration(b));Assert.False(g.State.FindEmpire(a)!.Ready);Assert.False(g.State.FindEmpire(b)!.Ready);Assert.Equal(RelationKind.Peace,Publish(g).State.Conflict!.Pairs[0].Relation);
    }
    [Theory][InlineData("self")][InlineData("unknown")][InlineData("stale")][InlineData("token")][InlineData("issuer")]
    public void RejectedInputRetainsEveryField(string defect)
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var action=new ConflictAction.DeclareWar(defect=="self"?a:defect=="unknown"?new EmpireId(999):b);var token=ConflictTokens.For(g.Content,g.View(a),action);
        string before=ConflictFixtures.Canonical(g.State);var result=g.Execute(new ConflictCommand(defect=="issuer"?b:a,defect=="stale"?1:0,action,defect=="token"?"bad":token));Assert.False(result.Accepted);Assert.Equal(before,ConflictFixtures.Canonical(g.State));
    }
    [Fact] public void UnmetTargetAndCurrentWarOrTruceAreUnavailable()
    {
        var unmet=ConflictFixtures.Create();Assert.False(ConflictQueries.Preview(unmet.Content,unmet.View(unmet.State.Empires[0].Id),new ConflictAction.DeclareWar(unmet.State.Empires[1].Id)).Allowed);
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var p=g.State.Conflict!.Pairs[0];p.LastPeaceTurn=0;p.TruceEndTurn=10;Assert.False(ConflictQueries.Preview(g.Content,g.View(a),new ConflictAction.DeclareWar(b)).Allowed);
        g.State.Turn=10;ObservationUpdater.Refresh(g.State,10);Issue(g,a,new ConflictAction.DeclareWar(b));g=Publish(g);Assert.Equal(11,g.State.Conflict!.Pairs[0].WarTurn);Assert.False(ConflictQueries.Preview(g.Content,g.View(a),new ConflictAction.DeclareWar(b)).Allowed);
    }
    [Fact] public void CompetingDeclarationsChoosePairOrderNotSubmissionOrder()
    {
        foreach(bool reverse in new[]{false,true})
        {
            var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var third=ConflictFixtures.AddNeutralObserver(g,g.State.FindPlanet(g.State.Colonies[0].PlanetId)!.StarId);
            if(reverse){Issue(g,third,new ConflictAction.DeclareWar(a));Issue(g,a,new ConflictAction.DeclareWar(b));}else{Issue(g,a,new ConflictAction.DeclareWar(b));Issue(g,third,new ConflictAction.DeclareWar(a));}
            var after=Publish(g);var wars=after.State.Conflict!.Pairs.Where(p=>p.Relation==RelationKind.War).ToArray();Assert.Single(wars);Assert.True(wars[0].Pair.Contains(a)&&wars[0].Pair.Contains(b));Assert.Single(after.State.Conflict.Owners.Single(o=>o.Owner==third).Events);Assert.DoesNotContain(b.ToString(),after.State.Conflict.Owners.Single(o=>o.Owner==third).Events[0].Reason);
        }
    }
    [Fact] public void DisjointPairsCanDeclareIndependentWars()
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var star=g.State.FindPlanet(g.State.Colonies[0].PlanetId)!.StarId;var c=ConflictFixtures.AddNeutralObserver(g,star);var d=ConflictFixtures.AddNeutralObserver(g,star);
        Issue(g,a,new ConflictAction.DeclareWar(b));Issue(g,c,new ConflictAction.DeclareWar(d));var after=Publish(g);Assert.Equal(2,after.State.Conflict!.Pairs.Count(p=>p.Relation==RelationKind.War));Assert.Empty(GameStateValidator.Validate(after.Content,after.State));
    }
    [Fact] public void PreparationStagesFutureRelationAndCannotBeAppliedTwice()
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create());Issue(g,g.State.Empires[0].Id,new ConflictAction.DeclareWar(g.State.Empires[1].Id));var next=g.State.Clone();var context=ConflictResolutionContext.Capture(g.Content,g.State);string bytes=ConflictFixtures.Canonical(next);ConflictRelations.Prepare(g.Content,g.State,next,context);Assert.Single(context.NewWars);Assert.Equal(bytes,ConflictFixtures.Canonical(next));Assert.Throws<InvalidOperationException>(()=>ConflictRelations.Prepare(g.Content,g.State,next,context));
    }
}
