using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ShipDesignProductionTests
{
    [Fact]
    public void BuildQueuePaysAndPinsExactDesign()
    {
        var session=ShipDesignFixtures.Session();var owner=session.State.Empires[0];var port=session.State.Colonies[0];port.ProductionReserve=28000;
        var gun=ShipDesignFixtures.SaveDesign(session,"Gun",new("escort_frame","conventional_drive",null,ImmutableArray.Create("pulse_bank","pulse_bank")));
        string civilian=session.State.Vessels[0].RoleId;var target=ShipDesignTargets.Build(gun);
        Accept(session,new SetColonyQueueCommand(owner.Id,0,port.Id,ImmutableArray.Create(new QueueEntryDraft(null,target),new QueueEntryDraft(null,target),new QueueEntryDraft(null,new(ProductionKinds.Vessel,civilian)))));
        var queue=session.State.Colonies[0].Development!.Queue.ToArray();Accept(session,new SetColonyQueueCommand(owner.Id,0,port.Id,ImmutableArray.Create(new QueueEntryDraft(queue[1].Token,target),new QueueEntryDraft(queue[0].Token,target),new QueueEntryDraft(queue[2].Token,queue[2].Target))));
        Accept(session,new SetColonyAutomationCommand(owner.Id,0,port.Id,false,true));int initial=session.State.Vessels.Count;
        for(int n=0;n<3;n++)
        {
            var before=session.State.Clone();var result=Accept(session,new SetReadyCommand(owner.Id,session.State.Turn,true));Assert.Equal(initial+n+1,session.State.Vessels.Count);var ship=session.State.Vessels.Last();
            var receipt=result.Report!.Economy.Colony(port.Id).Production;
            Assert.Null(ship.Transit);Assert.Null(ship.PendingMove);
            if(n<2){Assert.Equal(gun,ship.Design);Assert.Equal(2800,receipt.TargetCost);Assert.Equal(100,ship.Combat!.Hull);Assert.Equal(100,ship.Combat.Cohesion);Assert.Equal(48,VesselLoadouts.CombatProfile(session.Content,session.State,ship).Attack);}
            else{Assert.Equal(civilian,ship.RoleId);Assert.Null(ship.Design);}
            Assert.Equal(2-n,session.State.Colonies[0].Development!.Queue.Count);Assert.Equal(receipt.Ending,session.State.Colonies[0].ProductionReserve);Assert.Equal(before.Colonies[0].ProductionReserve,receipt.Starting);
        }
        Accept(session,new SetReadyCommand(owner.Id,session.State.Turn,true));Assert.Equal(initial+3,session.State.Vessels.Count);Assert.Empty(GameStateValidator.Validate(session.Content,session.State));
    }
    [Fact]
    public void ArchiveAfterCommitStillFinishesPinnedRecipe()
    {
        var session=ShipDesignFixtures.Session();var owner=session.State.Empires[0];var port=session.State.Colonies[0];port.ProductionReserve=9000;
        var id=ShipDesignFixtures.SaveDesign(session,"Queued",session.Content.ShipDesigns!.StartingRecipe);var target=ShipDesignTargets.Build(id);
        Accept(session,new SetColonyQueueCommand(owner.Id,0,port.Id,ImmutableArray.Create(new QueueEntryDraft(null,target))));
        Accept(session,new ShipDesignCommand(owner.Id,0,session.State.Empires[0].ShipDesigns!.CatalogueRevision,new ShipDesignAction.Archive(id)));
        RejectSame(session,new SetProductionTargetCommand(owner.Id,0,port.Id,target));RejectSame(session,new SetColonyQueueCommand(owner.Id,0,port.Id,ImmutableArray.Create(new QueueEntryDraft(null,target))));
        var retained=session.State.Colonies[0].Development!.Queue[0];Accept(session,new SetColonyQueueCommand(owner.Id,0,port.Id,ImmutableArray.Create(new QueueEntryDraft(retained.Token,retained.Target))));
        Accept(session,new SetColonyAutomationCommand(owner.Id,0,port.Id,false,true));var result=Accept(session,new SetReadyCommand(owner.Id,0,true));Assert.Equal(id,session.State.Vessels.Last().Design);Assert.Equal(2400,result.Report!.Economy.Colony(port.Id).Production.TargetCost);
    }
    [Fact]
    public void RoleOnlyAliasPinsStandardAtAcceptance()
    {
        var session=ShipDesignFixtures.Session();var owner=session.State.Empires[0];var port=session.State.Colonies[0];
        Accept(session,new SetProductionTargetCommand(owner.Id,0,port.Id,new(ProductionKinds.Vessel,session.Content.ShipDesigns!.WarshipRoleId)));
        Assert.Equal(ProductionKinds.ShipDesign,session.State.Colonies[0].ProjectKind);Assert.Equal("1",session.State.Colonies[0].ProjectId);
        Accept(session,new SetColonyQueueCommand(owner.Id,0,port.Id,ImmutableArray.Create(new QueueEntryDraft(null,new(ProductionKinds.Vessel,"escort")))));Assert.Equal(ShipDesignTargets.Build(new(1)),session.State.Colonies[0].Development!.Queue[0].Target);
        session.State.Colonies[0].ProductionReserve=10000;
        var aliasForecast=GovernorPlanningQueries.Create(session.Content,session.View(owner.Id)).Forecast(port.Id,port.Workforce,new(ProductionKinds.Vessel,"escort"));Assert.NotNull(aliasForecast);Assert.Equal(new DesignId(1),aliasForecast!.VesselCompletionPlan?.Design);
        var invalid=session.State.Clone();invalid.Colonies[0].ProjectKind=ProductionKinds.Vessel;invalid.Colonies[0].ProjectId="escort";Assert.NotEmpty(GameStateValidator.Validate(session.Content,invalid));
    }
    [Fact]
    public void ManualSelectionPausesOnlyConstructionAndPreservesActiveQueues()
    {
        var session=ShipDesignFixtures.Session();var owner=session.State.Empires[0];var port=session.State.Colonies[0];var template=session.Content.GovernorTemplates.Keys.First();
        Accept(session,new AssignGovernorTemplateCommand(owner.Id,0,ImmutableArray.Create(port.Id),template,true,true));Accept(session,new SetColonyQueueCommand(owner.Id,0,port.Id,ImmutableArray.Create(new QueueEntryDraft(null,new(ProductionKinds.Building,session.Content.Buildings.Keys.First())))));
        var before=session.State.Colonies[0].Clone();Accept(session,new SetProductionTargetCommand(owner.Id,0,port.Id,ShipDesignTargets.Build(new(1))));var after=session.State.Colonies[0];
        Assert.True(after.Development!.WorkforceEnabled);Assert.False(after.Development.ConstructionEnabled);Assert.Equal(before.Development!.Template,after.Development.Template);Assert.Equal(before.Workforce,after.Workforce);Assert.Equal(before.Development.Queue,after.Development.Queue);Assert.Equal(before.ProductionReserve,after.ProductionReserve);
        var plan=GovernorPlanner.Plan(GovernorPlanningQueries.Create(session.Content,session.View(owner.Id)));Assert.True(plan.Succeeded,plan.Error);Assert.Equal(ShipDesignTargets.Build(new(1)),Assert.Single(plan.Orders).Target);
    }
    [Theory]
    [InlineData("0")][InlineData("-1")][InlineData("01")][InlineData("+1")][InlineData(" 1")][InlineData("2147483648")][InlineData("999")]
    public void InvalidDesignTargetsRejectWithoutAllocatingOrSpending(string key)
    {var session=ShipDesignFixtures.Session();RejectSame(session,new SetProductionTargetCommand(session.State.Empires[0].Id,0,session.State.Colonies[0].Id,new(ProductionKinds.ShipDesign,key)));}
    [Fact]
    public void WholeRecipeForecastAndFrigateCompletionUseInstalledHull()
    {
        var session=ShipDesignFixtures.Session(ImmutableArray.Create("industrial_coordination","measurement_methods","frigate_construction","focused_emitters","reinforced_laminates"));var owner=session.State.Empires[0];var port=session.State.Colonies[0];port.ProductionReserve=10000;
        var id=ShipDesignFixtures.SaveDesign(session,"Armored",new("frigate_frame","conventional_drive","reinforced_armor",ImmutableArray.Create("lance_bank","pulse_bank")));var target=ShipDesignTargets.Build(id);
        var forecast=GovernorPlanningQueries.Create(session.Content,session.View(owner.Id)).Forecast(port.Id,port.Workforce,target);Assert.NotNull(forecast);Assert.Equal(4900,forecast!.Production.TargetCost);
        Accept(session,new SetProductionTargetCommand(owner.Id,0,port.Id,target));var result=Accept(session,new SetReadyCommand(owner.Id,0,true));var vessel=session.State.Vessels.Last();Assert.Equal(id,vessel.Design);Assert.Equal(180,vessel.Combat!.Hull);Assert.Equal(4900,result.Report!.Economy.Colony(port.Id).Production.TargetCost);
        Assert.Throws<ArgumentException>(()=>CombatInitializer.InitializeVessel(session.Content,10,vessel));
    }
    [Fact]
    public void AlternateRoleAliasUsesConfiguredIdentity()
    {
        var session=ShipDesignFixtures.Session();var c=session.Content;var role=c.Vessels["escort"] with{Id="guardian"};c=c with{Vessels=c.Vessels.Remove("escort").Add(role.Id,role),ShipDesigns=c.ShipDesigns! with{WarshipRoleId=role.Id},Combat=c.Combat! with{RoleProfiles=c.Combat.RoleProfiles.Remove("escort").Add(role.Id,c.Combat.RoleProfiles["escort"])}};
        session=new(c,session.State);var owner=session.State.Empires[0];var port=session.State.Colonies[0];port.ProductionReserve=5000;Accept(session,new SetProductionTargetCommand(owner.Id,0,port.Id,new(ProductionKinds.Vessel,"guardian")));Assert.Equal("1",session.State.Colonies[0].ProjectId);
        Accept(session,new SetReadyCommand(owner.Id,0,true));Assert.Equal("guardian",session.State.Vessels.Last().RoleId);Assert.Equal(new DesignId(1),session.State.Vessels.Last().Design);
    }
    [Fact]
    public void QueueCapIsFiniteAndActiveManualWorkIsRetained()
    {
        var session=ShipDesignFixtures.Session();var e=session.State.Empires[0];var c=session.State.Colonies[0];var target=ShipDesignTargets.Build(new(1));
        Accept(session,new SetProductionTargetCommand(e.Id,0,c.Id,new(ProductionKinds.Building,session.Content.Buildings.Values.First(b=>b.PrerequisiteTechnologyIds.IsEmpty).Id)));string active=session.State.Colonies[0].ProjectId!;
        Accept(session,new SetColonyQueueCommand(e.Id,0,c.Id,Enumerable.Range(1,32).Select(_=>new QueueEntryDraft(null,target)).ToImmutableArray()));Assert.Equal(active,session.State.Colonies[0].ProjectId);Assert.Equal(32,session.State.Colonies[0].Development!.Queue.Count);
        RejectSame(session,new SetColonyQueueCommand(e.Id,0,c.Id,Enumerable.Range(1,33).Select(_=>new QueueEntryDraft(null,target)).ToImmutableArray()));
    }
    [Fact]
    public void BadPinnedRecipeReturnsValidationErrors()
    {
        var session=ShipDesignFixtures.Session();var s=session.State;s.Colonies[0].ProjectKind=ProductionKinds.ShipDesign;s.Colonies[0].ProjectId="1";var data=s.Empires[0].ShipDesigns!;
        data.Designs[0]=data.Designs[0] with{Recipe=data.Designs[0].Recipe with{FrameId="absent"}};
        Assert.NotEmpty(GameStateValidator.Validate(session.Content,s));
    }
    private static CommandOutcome Accept(GameSession s,Command c){var r=s.Execute(c);Assert.True(r.Accepted,r.Rejection?.Message);return r;}
    private static void RejectSame(GameSession s,Command c){string before=JsonSerializer.Serialize(s.State);var r=s.Execute(c);Assert.False(r.Accepted);Assert.Equal(before,JsonSerializer.Serialize(s.State));}
}
