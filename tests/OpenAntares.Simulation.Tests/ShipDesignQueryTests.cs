using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ShipDesignQueryTests
{
    [Fact]
    public void CompletedRefitReportKeepsOriginalFactsAfterArchiveAndDelete()
    {
        var game=RefitResolutionTests.Economic(1300,0);RefitResolutionTests.Start(game);var first=Ready(game);Assert.Equal(600,game.State.Colonies[0].ProductionReserve);RefitResolutionTests.Start(game,new(1));var second=Ready(game);Assert.Equal(0,game.State.Colonies[0].ProductionReserve);var owner=game.State.Empires[0];
        Assert.Empty(owner.ShipDesigns!.PendingEvents);Assert.Equal(4,owner.ShipDesigns.RecentEvents.Count);var firstFacts=JsonSerializer.Serialize(owner.ShipDesigns.RecentEvents);
        Design(game,new ShipDesignAction.Archive(new(2)));Design(game,new ShipDesignAction.Delete(new(2)));Assert.Equal(firstFacts,JsonSerializer.Serialize(game.State.Empires[0].ShipDesigns!.RecentEvents));Assert.DoesNotContain(game.State.Empires[0].ShipDesigns!.Designs,d=>d.Id.Value==2);Assert.Empty(game.View(owner.Id).ShipDesigns!.Refits);Assert.Equal(4,game.View(owner.Id).ShipDesigns!.Events.Length);
        Assert.Contains(first.Report!.ShipDesignEvents!.Value,e=>e.Kind=="refit_accepted");Assert.Single(first.Report.ShipDesignEvents.Value,e=>e.Kind=="refit_completed"&&e.Paid==700&&e.After!.Stats.BaseSpeed==400);Assert.Single(second.Report!.ShipDesignEvents!.Value,e=>e.Kind=="refit_completed"&&e.Paid==600&&e.Before!.Stats.BaseSpeed==400&&e.After!.Stats.BaseSpeed==300);
        Ready(game);Assert.Equal(firstFacts,JsonSerializer.Serialize(game.State.Empires[0].ShipDesigns!.RecentEvents));
    }
    [Fact]
    public void HiddenRivalRecipeDoesNotChangeOwnPreview()
    {
        var game=ShipDesignAtomicityTests.MultiRefit();var owner=game.State.Empires[0].Id;var foreignOwner=game.State.Empires[1].Id;var foreignPort=game.State.Colonies.First(c=>c.EmpireId==foreignOwner);var foreignShip=game.State.Vessels[0].Clone();foreignShip.Id=new(game.State.AllocateId());foreignShip.EmpireId=foreignOwner;foreignShip.Fleet=null;foreignShip.Design=new(1);foreignShip.StationaryStarId=game.State.FindPlanet(foreignPort.PlanetId)!.StarId;game.State.Vessels.Add(foreignShip);OpenAntares.Simulation.Combat.CombatInitializer.InitializeVessel(game.Content,game.State,foreignShip);OpenAntares.Simulation.Scouting.ScoutInitializer.InitializeVessel(game.Content,10,foreignShip);var before=JsonSerializer.Serialize(game.View(owner));var action=new RefitAction.Cancel(new(1),1);var token=RefitQueries.Preview(game.Content,game.View(owner),action).Token;var foreign=game.State.Empires[1];foreign.KnownTechnologyIds=game.Content.Technologies.Keys.Order(StringComparer.Ordinal).ToList();foreign.ResearchReserve=998;var data=foreign.ShipDesigns!;data.Designs.Add(new(new(2),"Hidden fast",game.Content.ShipDesigns!.StartingRecipe with{EngineId="vector_drive"},false));data.NextDesignId=3;data.CatalogueRevision++;foreach(var colony in game.State.Colonies.Where(c=>c.EmpireId==foreign.Id))colony.ProductionReserve=99999;
        Assert.Empty(GameStateValidator.Validate(game.Content,game.State));Assert.Equal(before,JsonSerializer.Serialize(game.View(owner)));Assert.Equal(token,RefitQueries.Preview(game.Content,game.View(owner),action).Token);
        var ship=game.State.Vessels.First(v=>v.EmpireId==foreign.Id);ship.RoleId=game.Content.ShipDesigns.WarshipRoleId;ship.Design=new(2);OpenAntares.Simulation.Combat.CombatInitializer.InitializeVessel(game.Content,game.State,ship);OpenAntares.Simulation.Scouting.ScoutInitializer.InitializeVessel(game.Content,10,ship);var port=game.State.Colonies.First(c=>c.EmpireId==foreign.Id);var start=new RefitAction.Start(ship.Id,port.Id,new(1));var preview=RefitQueries.Preview(game.Content,game.View(foreign.Id),start);Assert.True(preview.Allowed,preview.RejectionCode);var accepted=game.Execute(new RefitCommand(foreign.Id,game.State.Turn,start,preview.Token));Assert.True(accepted.Accepted,accepted.Rejection?.Message);Assert.Equal(before,JsonSerializer.Serialize(game.View(owner)));Assert.Equal(token,RefitQueries.Preview(game.Content,game.View(owner),action).Token);
    }
    [Fact]
    public void EventsAreBoundedDeduplicatedAndDoNotGrantFurtherJobAuthority()
    {
        var game=RefitResolutionTests.Economic(10000,0);for(int n=0;n<14;n++){RefitResolutionTests.Start(game);var job=game.State.Empires[0].ShipDesigns!.Refits.Single();RefitCommandTests.Apply(game,new RefitAction.Cancel(job.Id,job.Revision));}
        var view=game.View(game.State.Empires[0].Id);Assert.Equal(20,view.ShipDesigns!.Events.Length);Assert.Equal(20,view.ShipDesigns.Events.Select(e=>e.Id).Distinct().Count());Ready(game);Assert.Empty(game.State.Empires[0].ShipDesigns!.PendingEvents);Assert.Equal(20,game.State.Empires[0].ShipDesigns!.RecentEvents.Count);Assert.Empty(game.View(game.State.Empires[0].Id).ShipDesigns!.Refits);Assert.False(RefitCommandTests.Preview(game,new RefitAction.Resume(new(14),1)).Allowed);Assert.Equal(10000,game.State.Colonies[0].ProductionReserve);
    }
    [Fact]
    public void CataloguePagesAndReferencesRemainStable()
    {
        var game=ShipDesignFixtures.Session();var d=game.State.Empires[0].ShipDesigns!;for(int n=2;n<=256;n++)d.Designs.Add(new(new(n),"Row "+n.ToString("D3"),game.Content.ShipDesigns!.StartingRecipe,n>32));d.NextDesignId=257;Assert.Empty(GameStateValidator.Validate(game.Content,game.State));var view=game.View(game.State.Empires[0].Id);var first=ShipDesignQueries.CataloguePage(view,-9);Assert.Equal(0,first.PageIndex);Assert.Equal(2,first.PageCount);Assert.Equal(32,first.Total);Assert.Equal(16,first.Rows.Length);Assert.NotNull(first.Rows[0].ArchiveBlockedReason);Assert.NotNull(first.Rows[0].DeleteBlockedReason);var last=ShipDesignQueries.CataloguePage(view,int.MaxValue,new(true));Assert.Equal(15,last.PageIndex);Assert.Equal(256,last.Total);Assert.Equal(16,last.PageCount);Assert.Equal(new DesignId(256),last.Rows.Last().Design.Id);Assert.Null(last.Rows.Last().DeleteBlockedReason);Assert.NotNull(last.Rows.Last().RestoreBlockedReason);
        var filtered=ShipDesignQueries.CataloguePage(view,15,new(true,"Row 255"),new(true));Assert.Equal(0,filtered.PageIndex);Assert.Single(filtered.Rows);Assert.Equal(new DesignId(255),filtered.Rows[0].Design.Id);var empty=ShipDesignQueries.CataloguePage(view,99,new(true,"missing"));Assert.Empty(empty.Rows);Assert.Equal(0,empty.PageIndex);Assert.Equal(1,empty.PageCount);
        var bytes=JsonSerializer.Serialize(game.State);var changed=first.Rows.SetItem(0,first.Rows[0] with{Design=first.Rows[0].Design with{Name="Changed view"}});Assert.Equal("Changed view",changed[0].Design.Name);Assert.Equal(bytes,JsonSerializer.Serialize(game.State));Assert.Equal(32,ShipDesignQueries.BuildChoices(view).Length);
    }
    [Theory]
    [InlineData(300,200,false,"refit_insufficient_reserve",2L)]
    [InlineData(700,0,false,"refit_ready_next_interval",1L)]
    [InlineData(300,0,false,"refit_port_inactive",null)]
    [InlineData(700,200,true,"refit_paused",null)]
    public void RefitStatusExplainsSnapshotReserveAndPausedAuthority(long reserve,long output,bool paused,string code,long? turns)
    {
        var game=RefitResolutionTests.Economic(reserve,output);RefitResolutionTests.Start(game);if(paused){var j=game.State.Empires[0].ShipDesigns!.Refits.Single();j.Status=RefitStatus.Paused;j.Reason="Labelled saved pause";j.Revision++;}var view=game.View(game.State.Empires[0].Id);var job=Assert.Single(view.ShipDesigns!.Refits);Assert.Equal(reserve,job.Reserve);Assert.Equal(output,job.OutputPerTurn);Assert.Equal(turns,job.EstimatedTurns);Assert.Equal(code,job.StatusCode);Assert.NotEmpty(job.StatusExplanation);Assert.Equal(300,job.Before!.Stats.BaseSpeed);Assert.Equal(400,job.After!.Stats.BaseSpeed);Assert.True(RefitQueries.Preview(game.Content,view,new RefitAction.Cancel(job.Id,job.Revision)).Allowed);
    }
    [Fact]
    public void OwnerReportsFilterForeignEventsAndDeduplicateOnlyWithinOwner()
    {
        var game=ShipDesignAtomicityTests.MultiRefit();var result=TurnResolver.Resolve(game.Content,game.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var own=result.Report!.ShipDesignEvents!.Value;var foreignOwner=game.State.Empires[1].Id;var forgedOther=own[0] with{Owner=foreignOwner,Reason="Private foreign reason"};var report=result.Report with{ShipDesignEvents=own.Add(own[0]).Add(forgedOther)};var view=EmpireViewProjector.ProjectReport(game.Content,result.State,report,game.State.Empires[0].Id);Assert.Equal(own.Length,view.ShipDesignEvents!.Value.Length);Assert.DoesNotContain(view.ShipDesignEvents.Value,e=>e.Owner==foreignOwner);Assert.Single(EmpireViewProjector.ProjectReport(game.Content,result.State,report,foreignOwner).ShipDesignEvents!.Value);var old=CombatFixtures.Game();var oldTurn=TurnResolver.Resolve(old.Content,old.State);Assert.True(oldTurn.Succeeded,string.Join(";",oldTurn.Errors));Assert.Null(EmpireViewProjector.ProjectReport(old.Content,oldTurn.State,oldTurn.Report!,old.State.Empires[0].Id).ShipDesignEvents);
    }
    [Fact]
    public void EmptyAndOldPermittedFramesHaveEmptyPagesAndNoBuildChoices()
    {
        var game=ShipDesignFixtures.Session();var view=game.View(game.State.Empires[0].Id) with{Colonies=ImmutableArray<OwnedColonyView>.Empty,ShipDesigns=new(1,ImmutableArray<DesignView>.Empty)};var page=ShipDesignQueries.CataloguePage(view,99);Assert.Empty(page.Rows);Assert.Equal(0,page.PageIndex);Assert.Equal(1,page.PageCount);Assert.Empty(ShipDesignQueries.BuildChoices(view));var old=CombatFixtures.Game();var oldView=EmpireViewProjector.Project(old.Content,old.State,old.State.Empires[0].Id);Assert.Null(oldView.ShipDesigns);Assert.Empty(ShipDesignQueries.CataloguePage(oldView).Rows);Assert.Empty(ShipDesignQueries.BuildChoices(oldView));
    }
    [Fact]
    public void CancellationPreviewMakesNoCompletionPromise()
    {
        var game=RefitResolutionTests.Economic(700,0);RefitResolutionTests.Start(game);var preview=RefitCommandTests.Preview(game,new RefitAction.Cancel(new(1),1));Assert.True(preview.Allowed);Assert.Null(preview.EstimatedTurns);Assert.Contains(preview.Consequences,s=>s.Contains("banked reserve",StringComparison.OrdinalIgnoreCase));Assert.DoesNotContain(preview.Consequences,s=>s.Contains("single completion payment",StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public void ReferenceIndexCountsEveryOwnBindingInOnePass()
    {
        // Labelled index supplement; these synthetic retained bindings are never published as a game.
        var game=RefitResolutionTests.Economic();RefitResolutionTests.Start(game);var s=game.State;var owner=s.Empires[0];var port=s.Colonies[0];port.Development!.Queue.Add(new(1,ShipDesignTargets.Build(new(2))));owner.Fleets!.Requests.Add(new(){Slots=new(){new(){Design=new(2)}},Status=OpenAntares.Simulation.Fleets.RequestStatus.Complete});var before=ShipDesignCatalogue.Index(s,owner.Id);Assert.Equal(new DesignReferences(1,0,0,0,1),before[new(1)]);Assert.Equal(new DesignReferences(0,0,1,1,1),before[new(2)]);port.ProjectKind=ProductionKinds.ShipDesign;port.ProjectId="2";var after=ShipDesignCatalogue.Index(s,owner.Id);Assert.Equal(new DesignReferences(0,1,1,1,1),after[new(2)]);Assert.Equal(ShipDesignCatalogue.References(s,owner.Id,new(2)),after[new(2)]);Assert.Empty(ShipDesignCatalogue.Index(s,new(999)));
    }
    private static CommandOutcome Ready(GameSession game){var result=game.Execute(new SetReadyCommand(game.State.Empires[0].Id,game.State.Turn,true));Assert.True(result.Accepted,result.Rejection?.Message);return result;}
    private static void Design(GameSession game,ShipDesignAction a){var e=game.State.Empires[0];var result=game.Execute(new ShipDesignCommand(e.Id,game.State.Turn,e.ShipDesigns!.CatalogueRevision,a));Assert.True(result.Accepted,result.Rejection?.Message);}
}
