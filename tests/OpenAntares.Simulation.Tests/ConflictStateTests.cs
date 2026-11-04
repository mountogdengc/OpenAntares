using System;
using System.Linq;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictStateTests
{
    [Fact] public void NewGenerationInitializesOneOwnStatePerEmpireAndNoWar()
    {var s=ConflictFixtures.Create();Assert.Equal(2,s.State.Conflict!.Owners.Count);Assert.Empty(s.State.Conflict.Encounters);Assert.Empty(ConflictStateValidator.Validate(s.Content,s.State));}
    [Fact] public void CloneAndValidateLiveAuthorityWithoutRequiringHistoricalTargets()
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create());var s=g.State;var c=s.Conflict!;
        c.NextEventId=2;c.Owners[0].NextEventId=2;c.Owners[0].Events.Add(new(1,s.Turn,c.Owners[0].Owner,c.Owners[1].Owner,"fixture","Before"));
        string before=ConflictFixtures.Canonical(s);var clone=s.Clone();Assert.NotSame(c,clone.Conflict);
        clone.Conflict!.Pairs[0].Declarations.Add(new(c.Pairs[0].Pair.Low,s.Turn));clone.Conflict.Owners[0].Events[0]=clone.Conflict.Owners[0].Events[0] with{Reason="Changed"};
        Assert.Equal(before,ConflictFixtures.Canonical(s));Assert.Empty(ConflictStateValidator.Validate(g.Content,s));
    }
    [Fact] public void HistoricalLossRemainsValidAfterVesselDisappears()
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create());var s=g.State;var c=s.Conflict!;var lost=ConflictFixtures.AddEscort(g,s.Empires[0].Id,s.FindPlanet(s.Colonies[0].PlanetId)!.StarId);s.Vessels.Remove(lost);
        c.NextEncounterId=2;c.Owners[0].History.Add(new(){Encounter=new(1),Other=s.Empires[1].Id,Star=s.FindPlanet(s.Colonies[0].PlanetId)!.StarId,StartedTurn=0,EndedTurn=0,Outcome="Lost",LostMembers=new(){lost.Id}});
        Assert.Empty(ConflictStateValidator.Validate(g.Content,s));
    }
    [Theory][InlineData("counter")][InlineData("event")][InlineData("revision")][InlineData("duplicate_pair")][InlineData("party")][InlineData("contact")][InlineData("owner")][InlineData("missing_owner")][InlineData("old")][InlineData("missing_state")][InlineData("null_owners")][InlineData("peace_dates")]
    public void MalformedCurrentAuthorityRejectsWithoutThrowing(string defect)
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create());var s=g.State;var c=s.Conflict!;
        switch(defect)
        {
            case "counter":c.NextEncounterId=0;break;case "event":c.NextEventId=0;break;case "revision":c.Pairs[0].Revision=0;break;
            case "duplicate_pair":c.Pairs.Add(c.Pairs[0]);break;case "party":c.Pairs[0].Parties.RemoveAt(0);break;
            case "contact":s.Empires[0].Contacts.Clear();break;case "owner":c.Owners.Add(c.Owners[0]);break;
            case "missing_owner":c.Owners.RemoveAt(0);break;case "old":s.RulesVersion=11;break;case "missing_state":s.Conflict=null;break;
            case "null_owners":c.Owners=null!;break;case "peace_dates":c.Pairs[0].LastPeaceTurn=0;c.Pairs[0].TruceEndTurn=9;break;
        }
        Assert.NotEmpty(ConflictStateValidator.Validate(g.Content,s));
    }
    [Fact] public void LiveDuplicateCombatParticipantIsRejected()
    {var g=ConflictFixtures.WithEncounter();Assert.Empty(ConflictStateValidator.Validate(g.Content,g.State));var second=g.State.Conflict!.Encounters[0];g.State.Conflict.Encounters.Add(second);Assert.NotEmpty(ConflictStateValidator.Validate(g.Content,g.State));}
    [Theory][InlineData("design")][InlineData("diplomacy_null")]
    public void NullOrForgedPrerequisitesRejectWithoutThrowing(string defect)
    {
        var g=ConflictFixtures.WithEncounter();
        if(defect=="design")g.State.FindVessel(g.State.Conflict!.Encounters[0].LowSide.Members[0])!.Design=new(999);
        else g.State.Diplomacy!.Pairs=null!;
        System.Collections.Generic.IReadOnlyList<string>? errors=null;
        var exception=Record.Exception(()=>errors=ConflictStateValidator.Validate(g.Content,g.State));
        Assert.Null(exception);Assert.NotEmpty(errors!);
    }
    [Fact] public void LabelledMixedRefitAndSentrySupplementsPassBothValidators()
    {
        foreach(var game in new[]{ConflictFixtures.WithMixedDefense(),ConflictFixtures.FromFrozenSupplement("refit-completing"),ConflictFixtures.FromFrozenSupplement("sentry-pending")})
        {Assert.Empty(ConflictStateValidator.Validate(game.Content,game.State));Assert.Empty(GameStateValidator.Validate(game.Content,game.State));}
    }
    [Fact] public void OldRulesWithoutConflictRemainValidForThisModule()
    {var g=ConflictFixtures.Create();g.State.RulesVersion=11;g.State.Conflict=null;Assert.Empty(ConflictStateValidator.Validate(g.Content,g.State));}
}
