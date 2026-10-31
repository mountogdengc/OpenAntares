using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Rivals;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictForecastTests
{
    [Fact] public void Blockade701PlusNeutralTrade100Equals450Everywhere()
    {
        var g=ConflictBlockadeTests.Activate(ConflictBlockadeTests.Economic());var b=g.State.Empires[1].Id;var view=g.View(b);var col=Assert.Single(view.Colonies);
        var forecasts=new[]{col.Economy,GovernorPlanningQueries.Create(g.Content,view).Forecast(col.Id,col.Workforce,null)!,RivalPlanningQueries.Create(g.Content,g.State,b).Forecast(col.Id,col.Workforce,null)!,FleetPlanningQueries.Create(g.Content,view).Forecast(col.Id,col.Workforce,null)!};
        Assert.All(forecasts,f=>Assert.Equal(450,f.NetProduction.Total));
        Assert.Equal(col.ProductionReserve-col.ReservedProduction,col.SpendableProduction);
    }
    [Fact] public void ZeroCurrentOutputStillCarriesPressureIntoAlternativeWorkforce()
    {
        var g=ConflictBlockadeTests.Activate(ConflictBlockadeTests.Economic(false,0));var view=g.View(g.State.Empires[1].Id);var col=view.Colonies[0];Assert.Equal(0,col.Economy.NetProduction.Total);Assert.Single(view.Conflict!.OwnPressure);
        var workers=new Workforce(col.Population-1,1,0);Assert.Equal(100,GovernorPlanningQueries.Create(g.Content,view).Forecast(col.Id,workers,null)!.NetProduction.Total);
    }
    [Fact] public void ClonedCalculatorFactsRemainEffectiveAndCannotBecomePublishedState()
    {
        var g=ConflictBlockadeTests.Activate(ConflictBlockadeTests.Economic());var b=g.State.Empires[1].Id;var view=g.View(b);var frame=g.State.Clone();frame.Empires.RemoveAll(e=>e.Id!=b);frame.Colonies.RemoveAll(c=>c.EmpireId!=b);frame.Vessels.RemoveAll(v=>v.EmpireId!=b);ConflictForecastFrame.Apply(frame,view);var clone=frame.Clone();
        Assert.Null(clone.Conflict);Assert.Equal(450,EconomyCalculator.Calculate(g.Content,clone).Colonies[0].NetProduction.Total);Assert.Contains(GameStateValidator.Validate(g.Content,clone),e=>e.Contains("Calculator-only"));Assert.Throws<ArgumentException>(()=>SaveGame.Serialize(g.Content,clone));
        var outcome=CommandProcessor.Execute(g.Content,clone,new SetResearchTargetCommand(b,clone.Turn,null));Assert.False(outcome.Accepted);
    }
    [Fact] public void AssemblyUsesTheSamePressureBeforeItsQueueEstimates()
    {
        var g=ConflictBlockadeTests.Activate(ConflictBlockadeTests.Economic());var view=g.View(g.State.Empires[1].Id);var col=view.Colonies[0];var role=g.Content.ShipDesigns!.WarshipRoleId;
        var draft=new AssemblyDraft("Pressure estimate",col.StarId,null,ImmutableArray<string>.Empty,ImmutableArray.Create(col.Id),RequestPriority.Normal,ImmutableArray<VesselId>.Empty,false,null){Quotas=ImmutableArray.Create(new AssemblyQuota(role,new DesignId(1)))};
        var preview=AssemblyPlanner.Preview(FleetPlanningQueries.Create(g.Content,view),draft);Assert.True(preview.Allowed,string.Join(";",preview.Rejections));Assert.Equal(450,Assert.Single(preview.Colonies).ProductionPerTurn);
    }
    [Fact] public void RefitEstimateUsesAdjustedIncomeAndOwnSpendableBank()
    {
        var g=ConflictBlockadeTests.Activate(ConflictBlockadeTests.Economic());var b=g.State.Empires[1].Id;var colony=g.State.Colonies.First(c=>c.EmpireId==b);colony.ProductionReserve=0;var ship=ConflictFixtures.AddEscort(g,b,g.State.FindPlanet(colony.PlanetId)!.StarId);ConflictWithdrawalTests.Upgrade(g,b);ship.Design=new(1);
        // Immutable published pressure is kept fixed while previewing an own refit alternative.
        var view=g.View(b);var pressure=new OwnBlockadePressure(view.Colonies[0].StarId,g.State.Empires[0].Id,50);view=view with{Conflict=view.Conflict! with{OwnPressure=ImmutableArray.Create(pressure)}};
        var forecast=GovernorPlanningQueries.Create(g.Content,view).Forecast(colony.Id,colony.Workforce,null)!;view=view with{Colonies=view.Colonies.Select(c=>c with{Economy=forecast}).ToImmutableArray()};
        var preview=RefitQueries.Preview(g.Content,view,new RefitAction.Start(ship.Id,colony.Id,new(2)));Assert.True(preview.Allowed,preview.RejectionCode);Assert.Equal((preview.Price+449)/450,preview.EstimatedTurns);
    }
    [Fact] public void HiddenPeaceChoicesDoNotChangeForecastOrOwnConsent()
    {
        var g=ConflictBlockadeTests.Activate(ConflictBlockadeTests.Economic());var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var before=g.View(b);var action=new ConflictAction.ProposePeace(a);var token=ConflictQueries.Preview(g.Content,before,action).Token;
        var pair=g.State.Conflict!.Pairs.Single(p=>p.Pair.Contains(a)&&p.Pair.Contains(b));pair.PeaceIntents.Add(new(a,g.State.Turn,true));var after=g.View(b);
        Assert.Equal(token,ConflictQueries.Preview(g.Content,after,action).Token);Assert.Equal(JsonSerializer.Serialize(before.Colonies[0].Economy),JsonSerializer.Serialize(after.Colonies[0].Economy));Assert.Equal(before.Conflict!.OwnPressure.ToArray(),after.Conflict!.OwnPressure.ToArray());
    }
    [Fact] public void CalculatorMarkerRejectsOtherwiseValidOrdinaryCommand()
    {
        var g=ConflictBlockadeTests.Activate(ConflictBlockadeTests.Economic());var state=g.State.Clone();state.ConflictCalculatorFrame=ConflictBlockades.Capture(g.Content,state);var b=state.Empires[1].Id;
        Assert.False(CommandProcessor.Execute(g.Content,state,new SetReadyCommand(b,state.Turn,false)).Accepted);
    }
    [Fact] public void ConditionalSetupReleaseRetainsPressureAndTradeWithoutDoubleApplication()
    {
        var g=ConflictBlockadeTests.Activate(ConflictBlockadeTests.Economic());var view=g.View(g.State.Empires[1].Id);var colony=view.Colonies[0];var reserved=view with{Colonies=ImmutableArray.Create(colony with{ReservedProduction=400})};var result=OpenAntares.Simulation.Diplomacy.DiplomacyProductionForecasts.Apply(g.Content,reserved).Colonies[0];
        Assert.Equal(4600,result.SpendableProduction);Assert.Equal(5450,result.ProductionIfSetupReleased!.Ending);Assert.Equal(5050,GovernorPlanningQueries.Create(g.Content,reserved).Forecast(colony.Id,colony.Workforce,null)!.Production.Ending);Assert.Equal(5000,g.State.FindColony(colony.Id)!.ProductionReserve);
    }
}
