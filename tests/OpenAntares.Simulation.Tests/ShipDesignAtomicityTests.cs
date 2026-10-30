using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Collections.Immutable;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ShipDesignAtomicityTests
{
    public static IEnumerable<object[]> Faults()=>from phase in Enumerable.Range(0,6) from ready in new[]{false,true} select new object[]{phase,ready};
    [Theory,MemberData(nameof(Faults))]
    public void FinalReadyRefitFailureIsAtomic(int phase,bool ready)
    {
        var game=RefitResolutionTests.Economic(700,0);RefitResolutionTests.Start(game);var s=game.State;
        s.Colonies[0].Development!.Queue.Add(new(1,new(ProductionKinds.Vessel,"scout")));s.Colonies[0].Development!.NextQueueToken=2;
        Assert.Empty(GameStateValidator.Validate(game.Content,s));var bytes=Bytes(s);var reached=new List<ShipDesignResolutionPhase>();
        void Fault(ShipDesignResolutionPhase current){reached.Add(current);if(current==(ShipDesignResolutionPhase)phase)throw new InvalidOperationException("Labelled phase fault");}
        GameState failed;
        if(ready){var result=CommandProcessor.ExecuteShipDesigns(game.Content,s,new SetReadyCommand(s.Empires[0].Id,s.Turn,true),Fault);Assert.False(result.Accepted);Assert.Null(result.Report);failed=result.State;}
        else{var result=TurnResolver.ResolveShipDesigns(game.Content,s,Fault);Assert.False(result.Succeeded);Assert.Null(result.Report);failed=result.State;}
        Assert.Equal(Enumerable.Range(0,phase+1).Select(i=>(ShipDesignResolutionPhase)i),reached);Assert.Same(s,failed);Assert.Equal(bytes,Bytes(failed));Assert.False(failed.Empires[0].Ready);
        var retry=CommandProcessor.Execute(game.Content,s,new SetReadyCommand(s.Empires[0].Id,s.Turn,true));Assert.True(retry.Accepted,retry.Rejection?.Message);Assert.Equal(11,retry.State.Turn);Assert.Equal(0,retry.State.Colonies[0].ProductionReserve);Assert.Equal(new DesignId(2),retry.State.Vessels[0].Design);Assert.Single(retry.Report!.ShipDesignEvents!.Value,e=>e.Kind=="refit_completed");Assert.Equal(bytes,Bytes(s));
    }
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)]
    public void FinalHumanFailurePreservesFirstHumanPreparation(int phase)
    {
        var game=MultiRefit();var state=game.State;var first=state.Empires[0].Id;var second=state.Empires[1].Id;
        var prepared=game.Execute(new SetReadyCommand(first,state.Turn,true));Assert.True(prepared.Accepted,prepared.Rejection?.Message);Assert.Null(prepared.Report);Assert.True(game.State.FindEmpire(first)!.Ready);Assert.False(game.State.FindEmpire(second)!.Ready);string bytes=Bytes(game.State);bool reached=false;
        var result=CommandProcessor.ExecuteShipDesigns(game.Content,game.State,new SetReadyCommand(second,state.Turn,true),p=>{if(p==(ShipDesignResolutionPhase)phase){reached=true;throw new OverflowException("Labelled second-human fault");}});
        Assert.True(reached);Assert.False(result.Accepted);Assert.Same(game.State,result.State);Assert.Equal(bytes,Bytes(result.State));Assert.True(result.State.FindEmpire(first)!.Ready);Assert.False(result.State.FindEmpire(second)!.Ready);
    }
    [Theory]
    [InlineData("source")][InlineData("negative")][InlineData("unaccounted")]
    public void RefitLateInvalidationDoesNotSpendOrDoubleComplete(string fault)
    {
        var game=RefitResolutionTests.Economic(700,0);RefitResolutionTests.Start(game);var original=Bytes(game.State);var context=RefitResolutionContext.Capture(game.Content,game.State);var economy=EconomyCalculator.Calculate(game.Content,game.State);var candidate=game.State.Clone();candidate.Colonies[0].ProductionReserve=economy.Colonies[0].Production.Ending;
        if(fault=="source")candidate.Vessels[0].Design=new(2);else candidate.Colonies[0].ProductionReserve=fault=="negative"?-1:699;
        Assert.Throws<InvalidOperationException>(()=>RefitResolver.Apply(game.Content,game.State,candidate,context,economy));Assert.Equal(original,Bytes(game.State));Assert.Null(economy.Colonies[0].Production.CompletedId);Assert.DoesNotContain(economy.Colonies[0].Production.Change.Lines,l=>l.Kind==BreakdownKinds.CostPaid);Assert.Throws<InvalidOperationException>(()=>RefitResolver.Apply(game.Content,game.State,candidate,context,economy));
    }
    [Fact]
    public void InsufficientReserveRetainsEquipmentJobAndNoSuccessReceipt()
    {
        var game=RefitResolutionTests.Economic(0,0);RefitResolutionTests.Start(game);var result=CommandProcessor.Execute(game.Content,game.State,new SetReadyCommand(game.State.Empires[0].Id,10,true));Assert.True(result.Accepted,result.Rejection?.Message);Assert.Equal(new DesignId(1),result.State.Vessels[0].Design);Assert.Single(result.State.Empires[0].ShipDesigns!.Refits);Assert.Null(result.Report!.Economy.Colonies[0].Production.CompletedId);Assert.Equal("refit_accepted",Assert.Single(result.Report.ShipDesignEvents!.Value).Kind);Assert.DoesNotContain(result.Report.ShipDesignEvents.Value,e=>e.Kind=="refit_completed"||e.Paid!=0);Assert.Equal(0,result.State.Colonies[0].ProductionReserve);
    }
    [Fact]
    public void CancelledJobTokensCannotRestoreTheJobOrSpendReserve()
    {
        var game=RefitResolutionTests.Economic(500,0);RefitResolutionTests.Start(game);var job=game.State.Empires[0].ShipDesigns!.Refits.Single();var cancel=new RefitAction.Cancel(job.Id,job.Revision);var token=RefitCommandTests.Preview(game,cancel).Token;RefitCommandTests.Apply(game,cancel);var bytes=Bytes(game.State);
        foreach(var action in new RefitAction[]{cancel,new RefitAction.Resume(job.Id,job.Revision)}){var result=game.Execute(new RefitCommand(game.State.Empires[0].Id,10,action,token));Assert.False(result.Accepted);Assert.Equal(bytes,Bytes(game.State));}Assert.Equal(500,game.State.Colonies[0].ProductionReserve);
    }
    [Fact]
    public void OneLockedShipRejectsAnEntireMultiSelectionAndUnknownTargetKind()
    {
        var game=RefitResolutionTests.Economic();var other=game.State.Vessels[0].Clone();other.Id=new(game.State.AllocateId());game.State.Vessels.Add(other);RefitResolutionTests.Start(game);var bytes=Bytes(game.State);var owner=game.State.Empires[0];var target=game.State.Stars.First(s=>s.Id!=other.StationaryStarId).Id;
        var move=game.Execute(new FleetCommand(owner.Id,10,new FleetAction.MoveSelection(ImmutableArray.Create(other.Id,game.State.Vessels[0].Id),ImmutableArray<FleetId>.Empty,target)));Assert.False(move.Accepted);Assert.Equal(bytes,Bytes(game.State));
        var invalid=game.Execute(new SetProductionTargetCommand(owner.Id,10,game.State.Colonies[0].Id,new("unsupported_kind","2")));Assert.False(invalid.Accepted);Assert.Equal(bytes,Bytes(game.State));
        var idle=RefitResolutionTests.Economic();var idleBytes=Bytes(idle.State);var invalidIdle=idle.Execute(new SetProductionTargetCommand(idle.State.Empires[0].Id,10,idle.State.Colonies[0].Id,new("unsupported_kind","2")));Assert.False(invalidIdle.Accepted);Assert.Equal(idleBytes,Bytes(idle.State));
    }
    [Theory]
    [InlineData("job")][InlineData("event")][InlineData("design")][InlineData("catalogue")]
    public void CounterOverflowRejectsWithoutPublishingPartialIdsOrReadiness(string counter)
    {
        var game=RefitResolutionTests.Economic();var owner=game.State.Empires[0];var data=owner.ShipDesigns!;owner.Ready=true;
        if(counter=="job")data.NextRefitId=int.MaxValue;else if(counter=="event")data.NextEventId=long.MaxValue;else if(counter=="design")data.NextDesignId=int.MaxValue;else data.CatalogueRevision=long.MaxValue;
        string bytes=Bytes(game.State);Command command;
        if(counter is "job" or "event"){var action=new RefitAction.Start(game.State.Vessels[0].Id,game.State.Colonies[0].Id,new(2));command=new RefitCommand(owner.Id,10,action,RefitCommandTests.Preview(game,action).Token);}
        else command=new ShipDesignCommand(owner.Id,10,data.CatalogueRevision,new ShipDesignAction.Save("Overflow",game.Content.ShipDesigns!.StartingRecipe));
        var result=game.Execute(command);Assert.False(result.Accepted);Assert.Equal(bytes,Bytes(game.State));Assert.Same(game.State,result.State);Assert.True(result.State.Empires[0].Ready);
    }
    [Fact]
    public void IdenticalValidCatalogueAndRefitCommandsKeepTheirFingerprintsAndReadiness()
    {
        var game=RefitResolutionTests.Economic();RefitResolutionTests.Start(game);game.State.Empires[0].Ready=true;var action=new RefitAction.Start(game.State.Vessels[0].Id,game.State.Colonies[0].Id,new(2));var token=RefitCommandTests.Preview(game,action).Token;string bytes=Bytes(game.State);var data=game.State.Empires[0].ShipDesigns!;
        var duplicate=game.Execute(new ShipDesignCommand(game.State.Empires[0].Id,10,data.CatalogueRevision,new ShipDesignAction.Save("Standard Escort",game.Content.ShipDesigns!.StartingRecipe)));Assert.True(duplicate.Accepted,duplicate.Rejection?.Message);Assert.False(duplicate.Changed);Assert.Equal(bytes,Bytes(game.State));Assert.Equal(token,RefitCommandTests.Preview(game,action).Token);
        var repeated=game.Execute(new RefitCommand(game.State.Empires[0].Id,10,action,token));Assert.True(repeated.Accepted,repeated.Rejection?.Message);Assert.False(repeated.Changed);Assert.Equal(bytes,Bytes(game.State));Assert.Equal(token,RefitCommandTests.Preview(game,action).Token);
    }
    private static string Bytes(GameState state)=>JsonSerializer.Serialize(state);
    internal static GameSession MultiRefit()
    {
        // Explicit multi-controller refit supplement; no earned-campaign claim.
        var old=CombatReadyTests.Multi();var s=old.State;s.RulesVersion=10;ShipDesignInitializer.Initialize(old.Content,s);var owner=s.Empires[0];owner.KnownTechnologyIds=old.Content.Technologies.Keys.Order(StringComparer.Ordinal).ToList();var ship=s.Vessels.First(v=>v.EmpireId==owner.Id);ship.RoleId=old.Content.ShipDesigns!.WarshipRoleId;ship.Design=new(1);CombatInitializer.InitializeVessel(old.Content,s,ship);OpenAntares.Simulation.Scouting.ScoutInitializer.InitializeVessel(old.Content,10,ship);
        var game=new GameSession(old.Content,s);var fast=ShipDesignFixtures.SaveDesign(game,"Fast supplement",old.Content.ShipDesigns.StartingRecipe with{EngineId="vector_drive"});var port=game.State.Colonies.First(c=>c.EmpireId==owner.Id);port.ProductionReserve=700;RefitCommandTests.Apply(game,new RefitAction.Start(ship.Id,port.Id,fast));Assert.Empty(GameStateValidator.Validate(game.Content,game.State));return game;
    }
}
