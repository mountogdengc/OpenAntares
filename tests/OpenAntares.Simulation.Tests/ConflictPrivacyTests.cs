using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Fleets;
using System.Text.Json;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictPrivacyTests
{
    private static GameSession Observed()
    {var g=ConflictFixtures.WithEncounter();ConflictObservations.Refresh(g.Content,g.State);return g;}
    private static ConflictAction.Attack Action(GameSession g)
    {var e=g.State.Conflict!.Encounters[0];return new(e.LowSide.Fleet!.Value,e.HighSide.Members[0],e.LowSide.Orders);}
    [Fact] public void HiddenOrdersNeverChangeAnotherViewOrToken()
    {
        var g=Observed();var s=g.State;var e=s.Conflict!.Encounters[0];var viewer=e.Pair.Low;
        var before=g.View(viewer);Assert.Single(before.Conflict!.Military);string token=ConflictTokens.For(g.Content,before,Action(g));string bytes=JsonSerializer.Serialize(before);
        var other=s.FindEmpire(e.Pair.High)!;other.Ready=true;other.ResearchReserve+=123;other.Fleets!.Officers[0].Name="Private unseen commander";s.Colonies.First(c=>c.EmpireId==other.Id).ProductionReserve+=999;
        s.FindVessel(e.HighSide.Members[0])!.Combat!.Cohesion=17;e.HighSide.Orders=e.HighSide.Orders with{Doctrine=CombatDoctrine.Hold};e.HighSide.WithdrawQueued=true;s.NextEntityId++;
        var after=g.View(viewer);Assert.Equal(bytes,JsonSerializer.Serialize(after));Assert.Equal(token,ConflictTokens.For(g.Content,after,Action(g)));
    }
    [Fact] public void PublicMilitaryFactsExposeEquipmentWithoutForeignPrivateOrders()
    {
        var g=Observed();var own=g.State.Empires[0].Id;var row=Assert.Single(g.View(own).Conflict!.Military);
        Assert.Equal(100,row.Hull);Assert.Equal("conventional_drive",row.EngineId);Assert.True(row.Current);
        string json=JsonSerializer.Serialize(row);foreach(string hidden in new[]{"Cohesion","Officer","ReturnColony","Doctrine","Catalogue","Private unseen commander"})Assert.DoesNotContain(hidden,json);
    }
    [Fact] public void OwnRelevantHullChangeInvalidatesToken()
    {var g=Observed();var owner=g.State.Empires[0].Id;string before=ConflictTokens.For(g.Content,g.View(owner),Action(g));g.State.FindVessel(g.State.Conflict!.Encounters[0].LowSide.Members[0])!.Combat!.Hull=70;Assert.NotEqual(before,ConflictTokens.For(g.Content,g.View(owner),Action(g)));}
    [Fact] public void StaleRemoteFactsDoNotRefreshFromHiddenDamage()
    {
        var g=ConflictFixtures.WithEncounter();var s=g.State;var e=s.Conflict!.Encounters[0];var remote=s.Stars.First(st=>s.Colonies.All(c=>s.FindPlanet(c.PlanetId)!.StarId!=st.Id)).Id;
        e.Star=remote;foreach(var id in e.LowSide.Members.Concat(e.HighSide.Members))s.FindVessel(id)!.StationaryStarId=remote;
        ObservationUpdater.Refresh(s,s.Turn);ConflictObservations.Refresh(g.Content,s);Assert.True(Assert.Single(g.View(e.Pair.Low).Conflict!.Military).Current);
        s.Conflict.Encounters.Clear();s.FindVessel(e.LowSide.Members[0])!.StationaryStarId=s.FindPlanet(s.Colonies[0].PlanetId)!.StarId;
        ObservationUpdater.Refresh(s,s.Turn);ConflictObservations.Refresh(g.Content,s);s.FindVessel(e.HighSide.Members[0])!.Combat!.Hull=9;
        var view=g.View(e.Pair.Low);var stale=Assert.Single(view.Conflict!.Military);Assert.False(stale.Current);Assert.Equal(100,stale.Hull);Assert.False(ConflictQueries.Preview(g.Content,view,ActionFrom(e)).Allowed);
    }
    private static ConflictAction.Attack ActionFrom(ConflictEncounterState e)=>new(e.LowSide.Fleet!.Value,e.HighSide.Members[0],e.LowSide.Orders);
    [Fact] public void UnsubmittedDeclarationAndPeaceAnswerRemainOwnerOnly()
    {
        var g=ConflictFixtures.WithContact(ConflictFixtures.Create(humans:2));var s=g.State;var p=s.Conflict!.Pairs[0];var owner=p.Pair.Low;
        string before=JsonSerializer.Serialize(g.View(owner));p.Declarations.Add(new(p.Pair.High,s.Turn));Assert.Equal(before,JsonSerializer.Serialize(g.View(owner)));
        p.Declarations.Clear();p.Relation=RelationKind.War;p.WarTurn=0;p.Declarers.Add(owner);p.NextPeaceOfferId=2;p.PendingPeace=new(){Id=new(1),Proposer=owner,CreatedTurn=0,ExpiryTurn=3};
        var v=g.View(owner);string token=ConflictTokens.For(g.Content,v,new ConflictAction.WithdrawPeace(p.Pair.High,new(1)));string data=JsonSerializer.Serialize(v);
        p.PendingPeace.Answer=true;p.Parties[1].PrivateRevision++;Assert.Equal(data,JsonSerializer.Serialize(g.View(owner)));Assert.Equal(token,ConflictTokens.For(g.Content,g.View(owner),new ConflictAction.WithdrawPeace(p.Pair.High,new(1))));
    }
    [Fact] public void NeutralObserverSeesLocalHullsButNotOtherPartiesWarOrReports()
    {
        var g=ConflictFixtures.WithEncounter();var e=g.State.Conflict!.Encounters[0];var neutral=ConflictFixtures.AddNeutralObserver(g,e.Star);ConflictObservations.Refresh(g.Content,g.State);
        Assert.Empty(GameStateValidator.Validate(g.Content,g.State));var v=g.View(neutral).Conflict!;Assert.Equal(2,v.Military.Length);Assert.Empty(v.Encounters);Assert.Empty(v.Events);Assert.All(v.Relations,r=>Assert.Equal(RelationKind.Peace,r.Relation));
    }
    [Fact] public void ForgedCurrentObservationWithoutOwnPresenceIsRejected()
    {
        var g=Observed();var owner=g.State.Conflict!.Owners[0];var row=Assert.Single(owner.Observations);
        var remote=g.State.Stars.First(s=>!g.State.Colonies.Any(c=>c.EmpireId==owner.Owner&&g.State.FindPlanet(c.PlanetId)!.StarId==s.Id)&&!g.State.Vessels.Any(v=>v.EmpireId==owner.Owner&&v.StationaryStarId==s.Id)).Id;
        owner.Observations[0]=row with{Star=remote};Assert.NotEmpty(ConflictStateValidator.Validate(g.Content,g.State));
    }
    [Fact] public void UnpublishedForeignGroupingDoesNotChangeAnAttackToken()
    {
        var g=Observed();var old=g.State.Conflict!.Encounters[0];var action=ActionFrom(old);var viewer=old.Pair.Low;var other=old.Pair.High;var high=old.HighSide.Members[0];g.State.Conflict.Encounters.Clear();
        string before=ConflictTokens.For(g.Content,g.View(viewer),action);var create=new FleetAction.Create("Private formation",ImmutableArray.Create(high),high);var fp=FleetQueries.PreviewAction(g.Content,g.View(other),create);Assert.True(fp.Allowed,fp.Rejection);
        var outcome=g.Execute(new FleetCommand(other,g.State.Turn,create){Fingerprint=fp.Fingerprint});Assert.True(outcome.Accepted,outcome.Rejection?.Message);
        Assert.Equal(before,ConflictTokens.For(g.Content,g.View(viewer),action));Assert.Null(Assert.Single(g.View(viewer).Conflict!.Military).Fleet);
    }
    [Fact] public void MixedCivilianFlagshipDoesNotBecomeAnObservedMilitaryTarget()
    {var g=ConflictFixtures.WithMixedDefense();ConflictObservations.Refresh(g.Content,g.State);var view=g.View(g.State.Empires[0].Id);var ship=Assert.Single(view.Conflict!.Military);Assert.NotEqual(g.State.Conflict!.Encounters[0].HighSide.Flagship,ship.Vessel);}
    [Fact] public void SoloAndOldCampaignsExposeNoInventedConflict()
    {
        var c=OpenAntares.Simulation.Content.ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        var created=OpenAntares.Simulation.Galaxy.GalaxyGenerator.CreateNewGame(c,new("small",42,12,"harl",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));Assert.True(created.Succeeded,created.Error);
        var g=new GameSession(c,created.State!);var view=g.View(g.State.Empires[0].Id);Assert.Empty(view.Conflict!.Military);Assert.Empty(view.Conflict.Relations);Assert.Empty(view.Conflict.Encounters);
        var old=DiplomacyFixtures.Create();Assert.Null(old.View(old.State.Empires[0].Id).Conflict);
    }
    [Fact] public void RepeatedViewsAndPreviewsConsumeNothing()
    {var g=Observed();string before=ConflictFixtures.Canonical(g.State);for(int n=0;n<12;n++){var view=g.View(g.State.Empires[0].Id);ConflictQueries.Preview(g.Content,view,Action(g));}Assert.Equal(before,ConflictFixtures.Canonical(g.State));}
}
