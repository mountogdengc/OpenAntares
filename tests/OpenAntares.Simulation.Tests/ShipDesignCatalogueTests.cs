using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ShipDesignCatalogueTests
{
    [Fact]
    public void ReversedBanksSaveCanonicallyAndRetryWithoutAllocating()
    {
        var session=ShipDesignFixtures.Session(ImmutableArray.Create("industrial_coordination","frigate_construction","measurement_methods","focused_emitters"));
        var recipe=new ShipRecipe("frigate_frame","conventional_drive",null,ImmutableArray.Create("pulse_bank","lance_bank"));
        var id=ShipDesignFixtures.SaveDesign(session,"Mixed",recipe);
        Assert.Equal(new[]{"lance_bank","pulse_bank"},Row(session,id).Recipe.WeaponBankIds);
        session.State.Empires[0].Ready=true;string before=Bytes(session);
        var retry=Issue(session,new ShipDesignAction.Save(" mixed ",recipe with{WeaponBankIds=ImmutableArray.Create("lance_bank","pulse_bank")}));
        Assert.True(retry.Accepted,retry.Rejection?.Message);Assert.False(retry.Changed);Assert.Equal(before,Bytes(session));
        session.State.Empires[0].Ready=false;
        var saved=System.Text.Json.Nodes.JsonNode.Parse(OpenAntares.Simulation.Saves.SaveGame.Serialize(session.Content,session.State))!;
        saved["state"]!["empires"]![0]!["ship_designs"]!["designs"]![1]!["recipe"]!["weapon_bank_ids"]=new System.Text.Json.Nodes.JsonArray("pulse_bank","lance_bank");
        var read=OpenAntares.Simulation.Saves.SaveGame.Deserialize(saved.ToJsonString());
        Assert.True(read.Succeeded,string.Join(";",read.Errors));
        Assert.Equal(new[]{"lance_bank","pulse_bank"},read.Game!.State.Empires[0].ShipDesigns!.Designs.Single(d=>d.Id==id).Recipe.WeaponBankIds);
    }
    [Fact]
    public void SaveCopyArchiveRestoreDeleteRespectIdentity()
    {
        var session=ShipDesignFixtures.Session(ImmutableArray.Create("industrial_coordination","vector_drives"));var standard=session.Content.ShipDesigns!.StartingRecipe;
        int nextGlobal=session.State.NextEntityId;ulong rng=session.State.Random.State;
        var fast=ShipDesignFixtures.SaveDesign(session,"  Fast Escort  ",standard with{EngineId="vector_drive"});Assert.Equal(new DesignId(2),fast);
        var copy=ShipDesignFixtures.SaveDesign(session,"Copy",standard with{EngineId="vector_drive"});Assert.Equal(new DesignId(3),copy);
        var before=session.State.Empires[0].ShipDesigns!.Designs[1];Accepted(session,new ShipDesignAction.Archive(fast));Assert.True(Row(session,fast).Archived);Assert.Equal(before.Recipe,Row(session,fast).Recipe);
        Accepted(session,new ShipDesignAction.Restore(fast));Assert.False(Row(session,fast).Archived);Accepted(session,new ShipDesignAction.Archive(copy));Accepted(session,new ShipDesignAction.Delete(copy));
        Assert.DoesNotContain(session.State.Empires[0].ShipDesigns!.Designs,d=>d.Id==copy);Assert.Equal(new DesignId(4),ShipDesignFixtures.SaveDesign(session,"Later",standard));
        Assert.Equal("Fast Escort",Row(session,fast).Name);Assert.Equal(standard,Row(session,new(1)).Recipe);Assert.Equal(nextGlobal,session.State.NextEntityId);Assert.Equal(rng,session.State.Random.State);Assert.Empty(GameStateValidator.Validate(session.Content,session.State));
    }
    [Fact]
    public void IdenticalValidSaveDoesNotAllocateOrClearReady()
    {
        var session=ShipDesignFixtures.Session();var recipe=session.Content.ShipDesigns!.StartingRecipe;var id=ShipDesignFixtures.SaveDesign(session,"Same",recipe);var e=session.State.Empires[0];e.Ready=true;
        var bytes=Bytes(session);var result=Issue(session,new ShipDesignAction.Save(" same ",recipe));Assert.True(result.Accepted);Assert.False(result.Changed);Assert.Equal(bytes,Bytes(session));Assert.True(session.State.Empires[0].Ready);
        result=session.Execute(new ShipDesignCommand(e.Id,session.State.Turn,e.ShipDesigns!.CatalogueRevision-1,new ShipDesignAction.Save("Same",recipe)));Assert.False(result.Accepted);Assert.Equal(bytes,Bytes(session));Assert.Equal(id,Row(session,id).Id);
    }
    [Theory]
    [InlineData("")][InlineData("   ")][InlineData("a\nb")]
    public void InvalidNamesRejectAtomically(string name)
    {var session=ShipDesignFixtures.Session();RejectUnchanged(session,new ShipDesignAction.Save(name,session.Content.ShipDesigns!.StartingRecipe));}
    [Fact]
    public void NamesAreTrimmedCaseInsensitiveAndBounded()
    {
        var session=ShipDesignFixtures.Session(ImmutableArray.Create("industrial_coordination","vector_drives"));var r=session.Content.ShipDesigns!.StartingRecipe;
        var id=ShipDesignFixtures.SaveDesign(session,new string('a',40),r);Assert.Equal(40,Row(session,id).Name.Length);
        RejectUnchanged(session,new ShipDesignAction.Save(new string('b',41),r));RejectUnchanged(session,new ShipDesignAction.Save(new string('A',40),r with{EngineId="vector_drive"}));
    }
    [Fact]
    public void ActiveAndRetainedBoundsEnforceTheNextAcceptance()
    {
        var active=WithRows(31,false);ShipDesignFixtures.SaveDesign(active,"Thirty two",active.Content.ShipDesigns!.StartingRecipe);Assert.Equal(32,active.State.Empires[0].ShipDesigns!.Designs.Count(d=>!d.Archived));RejectUnchanged(active,new ShipDesignAction.Save("Thirty three",active.Content.ShipDesigns.StartingRecipe));
        var retained=WithRows(255,true);var id=ShipDesignFixtures.SaveDesign(retained,"Two fifty six",retained.Content.ShipDesigns!.StartingRecipe);Assert.Equal(256,retained.State.Empires[0].ShipDesigns!.Designs.Count);RejectUnchanged(retained,new ShipDesignAction.Save("Two fifty seven",retained.Content.ShipDesigns.StartingRecipe));
        Accepted(retained,new ShipDesignAction.Archive(id));Accepted(retained,new ShipDesignAction.Delete(id));Assert.Equal(new DesignId(257),ShipDesignFixtures.SaveDesign(retained,"Next unused",retained.Content.ShipDesigns.StartingRecipe));
    }
    [Fact]
    public void PermanentAndReferencedDesignsCannotBeDeleted()
    {
        var session=ShipDesignMathTests.Equipped("fast");RejectUnchanged(session,new ShipDesignAction.Archive(new(1)));RejectUnchanged(session,new ShipDesignAction.Delete(new(1)));RejectUnchanged(session,new ShipDesignAction.Delete(new(2)));
        Accepted(session,new ShipDesignAction.Archive(new(2)));Assert.Equal(1,ShipDesignCatalogue.References(session.State,session.State.Empires[0].Id,new(2)).Vessels);RejectUnchanged(session,new ShipDesignAction.Delete(new(2)));
        Assert.Equal(400,VesselLoadouts.Resolve(session.Content,session.State,session.State.Vessels[0]).BaseSpeed);
    }
    [Fact]
    public void RestoreHonorsActiveCapAndRepeatedActionsAreNoOps()
    {
        var session=WithRows(32,false);var data=session.State.Empires[0].ShipDesigns!;data.Designs.Add(new(new(33),"Archived",session.Content.ShipDesigns!.StartingRecipe,true));data.NextDesignId=34;
        RejectUnchanged(session,new ShipDesignAction.Restore(new(33)));Accepted(session,new ShipDesignAction.Archive(new(2)));Accepted(session,new ShipDesignAction.Restore(new(33)));
        string bytes=Bytes(session);Assert.False(Issue(session,new ShipDesignAction.Restore(new(33))).Changed);Assert.Equal(bytes,Bytes(session));
        Accepted(session,new ShipDesignAction.Archive(new(33)));bytes=Bytes(session);Assert.False(Issue(session,new ShipDesignAction.Archive(new(33))).Changed);Assert.Equal(bytes,Bytes(session));
    }
    [Theory]
    [InlineData("locked")][InlineData("capacity")][InlineData("missing")][InlineData("revision_overflow")][InlineData("id_overflow")][InlineData("invalid_publication")]
    public void InvalidRecipesAndLimitsDoNotAllocateOrChangeReadiness(string fault)
    {
        var session=ShipDesignFixtures.Session();var e=session.State.Empires[0];var r=session.Content.ShipDesigns!.StartingRecipe;
        switch(fault)
        {
            case "locked":r=r with{EngineId="vector_drive"};break;
            case "capacity":r=r with{WeaponBankIds=ImmutableArray.Create("pulse_bank","pulse_bank")};break;
            case "missing":r=r with{FrameId="absent"};break;
            case "revision_overflow":e.ShipDesigns!.CatalogueRevision=long.MaxValue;break;
            case "id_overflow":e.ShipDesigns!.NextDesignId=int.MaxValue;break;
            case "invalid_publication":session.State.Colonies[0].ProductionReserve=-1;break;
        }
        e.Ready=true;RejectUnchanged(session,new ShipDesignAction.Save("Reject",r));
    }
    [Fact]
    public void OldRulesAndStaleTurnRejectUnchanged()
    {
        var old=CombatFixtures.Game(enabled:false);var session=new GameSession(old.Content,old.State);string before=Bytes(session);var outcome=session.Execute(new ShipDesignCommand(session.State.Empires[0].Id,session.State.Turn,1,new ShipDesignAction.Save("New",session.Content.ShipDesigns!.StartingRecipe)));Assert.False(outcome.Accepted);Assert.Equal(before,Bytes(session));
        session=ShipDesignFixtures.Session();before=Bytes(session);outcome=session.Execute(new ShipDesignCommand(session.State.Empires[0].Id,session.State.Turn+1,1,new ShipDesignAction.Save("New",session.Content.ShipDesigns!.StartingRecipe)));Assert.False(outcome.Accepted);Assert.Equal(before,Bytes(session));
    }
    [Fact]
    public void DraftPreviewReportsLockedResearchAndOwnCatalogueOnly()
    {
        var session=ShipDesignFixtures.Session();var e=session.State.Empires[0];var view=session.View(e.Id);var r=session.Content.ShipDesigns!.StartingRecipe;
        Assert.True(ShipDesignQueries.PreviewDraft(session.Content,view,"New",r).Allowed);var locked=ShipDesignQueries.PreviewDraft(session.Content,view,"Fast",r with{EngineId="vector_drive"});Assert.False(locked.Allowed);Assert.Equal(new[]{"vector_drives"},locked.MissingTechnologyIds);
        var c=session.Content;var game=GalaxyGenerator.CreateNewGame(c,new("small",42,10,"harl",1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false)).State!;e=game.Empires[0];
        var before=JsonSerializer.Serialize(OpenAntares.Simulation.Knowledge.EmpireViewProjector.Project(c,game,e.Id));var foreign=game.Empires[1].ShipDesigns!;foreign.Designs.Add(new(new(2),"Private rival recipe",r,false));foreign.NextDesignId=3;foreign.CatalogueRevision++;
        Assert.Equal(before,JsonSerializer.Serialize(OpenAntares.Simulation.Knowledge.EmpireViewProjector.Project(c,game,e.Id)));
    }
    [Fact]
    public void ReferenceIndexCountsOwnBoundLocationsAndPreservesRecipes()
    {
        // Labelled reference-index supplement: future target kinds are inspected without publication.
        var session=ShipDesignMathTests.Equipped("fast");var s=session.State;var e=s.Empires[0];var port=s.Colonies[0];port.ProjectKind="ship_design";port.ProjectId="2";port.Development!.Queue.Add(new(1,new("ship_design","2")));
        e.ShipDesigns!.Refits.Add(new(){Id=new(1),Vessel=s.Vessels[0].Id,Port=port.Id,SourceDesign=new(2),TargetDesign=new(1)});
        e.Fleets!.Requests.Add(new(){Id=new(1),Slots=new(){new(){Ordinal=1,RoleId="escort",Design=new(2),Status=OpenAntares.Simulation.Fleets.SlotStatus.Arrived}},Status=OpenAntares.Simulation.Fleets.RequestStatus.Complete});
        Assert.Equal(new DesignReferences(1,1,1,1,1),ShipDesignCatalogue.References(s,e.Id,new(2)));Assert.Equal(new DesignReferences(0,0,0,0,0),ShipDesignCatalogue.References(s,new EmpireId(999),new(2)));
    }
    private static GameSession WithRows(int count,bool archived)
    {
        var session=ShipDesignFixtures.Session();var d=session.State.Empires[0].ShipDesigns!;for(int n=2;n<=count;n++)d.Designs.Add(new(new(n),"Fixture "+n,session.Content.ShipDesigns!.StartingRecipe,archived));d.NextDesignId=count+1;Assert.Empty(GameStateValidator.Validate(session.Content,session.State));return session;
    }
    private static ShipDesign Row(GameSession s,DesignId id)=>s.State.Empires[0].ShipDesigns!.Designs.Single(d=>d.Id==id);
    private static string Bytes(GameSession s)=>JsonSerializer.Serialize(s.State);
    private static CommandOutcome Issue(GameSession s,ShipDesignAction a){var e=s.State.Empires[0];return s.Execute(new ShipDesignCommand(e.Id,s.State.Turn,e.ShipDesigns!.CatalogueRevision,a));}
    private static void Accepted(GameSession s,ShipDesignAction a){var result=Issue(s,a);Assert.True(result.Accepted,result.Rejection?.Message);}
    private static void RejectUnchanged(GameSession s,ShipDesignAction a){string before=Bytes(s);var result=Issue(s,a);Assert.False(result.Accepted);Assert.False(result.Changed);Assert.Equal(before,Bytes(s));}
}
