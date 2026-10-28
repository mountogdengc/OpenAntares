using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetEconomyTests
{
    // Labelled pre-session rank/commission-completion supplements; not earned-rank evidence.
    internal static Saves.LoadedGame Organizer(int xp=10,bool request=true,long scoutCost=800,string role="scout",bool crises=false)
    {
        var c=FleetContentTests.Core();c=c with{Vessels=c.Vessels.SetItem("scout",c.Vessels["scout"] with{Cost=scoutCost})};
        var made=Galaxy.GalaxyGenerator.CreateNewGame(c,new("small",42,8,"harl",CrisesEnabled:crises,AmebasEnabled:false));Assert.True(made.Succeeded,made.Error);var g=new Saves.LoadedGame(c,made.State!);
        string profile=c.FleetCommand!.Profiles.Single(p=>p.CultureId==c.Species["harl"].CommandCultureId&&p.Specialty==OfficerSpecialty.Organizer).Id;
        var commissioned=FleetOfficerTests.Run(g,new OfficerAction.Commission(profile,g.State.Colonies[0].Id));Assert.True(commissioned.Accepted,commissioned.Rejection?.Message);g=new(c,commissioned.State);
        g.State.Turn=2;var officer=g.State.Empires[0].Fleets!.Officers.Last();officer.Status=OfficerStatus.Posted;officer.AvailableAtTurn=null;officer.Xp=xp;officer.Rank=xp>=30?2:xp>=10?1:0;ObservationUpdater.Refresh(g.State,2);FleetFixtures.Valid(g);
        if(!request)return g;
        g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,1) with{Roles=ImmutableArray.Create(role),Coordinator=officer.Id,AuthorizeCargo=c.Vessels[role].EmbarkPopulation>0});
        var prepared=GovernorTurnCoordinator.Prepare(c,g.State,g.State.Empires[0].Id);Assert.True(prepared.Succeeded);return FleetFixtures.Valid(new(c,prepared.State));
    }
    [Theory]
    [InlineData(0,800)]
    [InlineData(10,760)]
    [InlineData(30,720)]
    public void OrganizerScoutCosts760And720OnlyWhileActiveCoordinator(int xp,long expected)
    {
        var g=Organizer(xp);var col=g.State.Colonies[0];
        Assert.Equal(expected,OfficerBonuses.RequestShipCost(g.Content,g.State,col.Id,"scout"));Assert.Equal(expected,EconomyCalculator.CalculateColony(g.Content,g.State,col).Production.TargetCost);
        var context=FleetPlanningQueries.Create(g.Content,EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id));Assert.Equal(expected,context.Forecast(col.Id,col.Workforce,new(ProductionKinds.Vessel,"scout"))!.Production.TargetCost);
        Assert.Equal(expected,FleetRequestTests.Retained(g,g.State.Empires[0].Fleets!.Requests[0]).Slots[0].Cost);
        Assert.Equal(g.Content.Vessels["harl_settler"].Cost,OfficerBonuses.RequestShipCost(g.Content,g.State,col.Id,"harl_settler"));
        var ordinary=g.State.Clone();var other=ordinary.Colonies[0];other.Development!.Queue.Add(new(99,new(ProductionKinds.Vessel,"scout")));other.Development.NextQueueToken=100;other.Development.ActiveQueueToken=99;
        Assert.Equal(800,OfficerBonuses.RequestShipCost(g.Content,ordinary,col.Id,"scout"));Assert.Equal(800,EconomyCalculator.CalculateColony(g.Content,ordinary,other).Production.TargetCost);
    }
    [Fact]
    public void PauseRemoveCancelAndPromotionRepriceOnlyNextSnapshotWithoutRefund()
    {
        var g=Organizer();var col=g.State.Colonies[0];col.ProductionReserve=700;int population=col.Population;var captured=EconomyCalculator.CalculateColony(g.Content,g.State,col);Assert.Equal(760,captured.Production.TargetCost);Assert.Equal(700+captured.NetProduction.Total-760,captured.Production.Ending);Assert.Equal(700,col.ProductionReserve);
        var officer=g.State.Empires[0].Fleets!.Officers.Last();officer.Xp=30;officer.Rank=2;Assert.Equal(720,EconomyCalculator.CalculateColony(g.Content,g.State,col).Production.TargetCost);Assert.Equal(760,captured.Production.TargetCost);Assert.Equal(population,col.Population);
        var r=g.State.Empires[0].Fleets!.Requests[0];var paused=FleetRequestTests.Run(g,new AssemblyAction.Pause(r.Id,r.Revision));Assert.True(paused.Accepted);g=new(g.Content,paused.State);col=g.State.Colonies[0];Assert.Equal(700,col.ProductionReserve);Assert.Equal(800,EconomyCalculator.CalculateColony(g.Content,g.State,col).Production.TargetCost);Assert.Equal(800,FleetRequestTests.Retained(g,g.State.Empires[0].Fleets!.Requests[0]).Slots[0].Cost);
        r=g.State.Empires[0].Fleets!.Requests[0];var resumePreview=FleetRequestTests.Retained(g,r);var resumed=FleetRequestTests.Run(g,new AssemblyAction.Resume(r.Id,r.Revision,resumePreview.Fingerprint));Assert.True(resumed.Accepted,resumed.Rejection?.Message);g=new(g.Content,resumed.State);Assert.Equal(700,g.State.Colonies[0].ProductionReserve);Assert.Equal(720,OfficerBonuses.RequestShipCost(g.Content,g.State,col.Id,"scout"));
        var removed=FleetOfficerTests.Run(g,new OfficerAction.Remove(officer.Id,col.Id));Assert.True(removed.Accepted);g=new(g.Content,removed.State);r=g.State.Empires[0].Fleets!.Requests[0];Assert.True(r.CoordinatorXpForfeited);Assert.Equal(800,OfficerBonuses.RequestShipCost(g.Content,g.State,col.Id,"scout"));
        var cancelled=FleetRequestTests.Run(g,new AssemblyAction.Cancel(r.Id,r.Revision,RequestCancellation.KeepQueue));Assert.True(cancelled.Accepted);Assert.Equal(700,cancelled.State.Colonies[0].ProductionReserve);Assert.Equal(800,EconomyCalculator.CalculateColony(g.Content,cancelled.State,cancelled.State.Colonies[0]).Production.TargetCost);
    }
    [Fact]
    public void DraftPriceDoesNotGrantAuthorityOrReducePopulationRequirements()
    {
        var g=Organizer(request:false,crises:true);var col=g.State.Colonies[0];var officer=g.State.Empires[0].Fleets!.Officers.Last();var draft=FleetPlanningTests.Draft(g,1) with{Coordinator=officer.Id};
        var preview=FleetPlanningTests.Preview(g,draft);Assert.True(preview.Allowed,string.Join(";",preview.Rejections));Assert.Equal(760,preview.Slots[0].Cost);Assert.Equal(800,OfficerBonuses.RequestShipCost(g.Content,g.State,col.Id,"scout"));Assert.Empty(g.State.Empires[0].Fleets!.Requests);Assert.Null(col.ProjectId);
        col.Crisis!.Quarantined=true;draft=draft with{Roles=ImmutableArray.Create("harl_settler"),AuthorizeCargo=true,Rally=g.State.Stars.First(s=>s.Id!=draft.Rally).Id};preview=FleetPlanningTests.Preview(g,draft);Assert.True(preview.Allowed,string.Join(";",preview.Rejections));Assert.Equal(1520,preview.Slots[0].Cost);Assert.Equal(1,preview.Slots[0].Cargo);Assert.Null(preview.Slots[0].CompletionTurns);Assert.Equal(3,g.Content.Vessels["harl_settler"].MinimumSourcePopulation);
    }
    [Fact]
    public void CeilingAndOverflowRejectAtomically()
    {
        var g=Organizer(scoutCost:801);Assert.Equal(761,OfficerBonuses.RequestShipCost(g.Content,g.State,g.State.Colonies[0].Id,"scout"));
        var extreme=Organizer(scoutCost:long.MaxValue);long expected=checked((long)(((Int128)long.MaxValue*95+99)/100));Assert.Equal(expected,OfficerBonuses.RequestShipCost(extreme.Content,extreme.State,extreme.State.Colonies[0].Id,"scout"));
        g.State.Colonies[0].ProductionReserve=long.MaxValue;Assert.Throws<OverflowException>(()=>EconomyCalculator.CalculateColony(g.Content,g.State,g.State.Colonies[0]));string before=FleetFixtures.Bytes(g.State);
        var rejected=CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(g.State.Empires[0].Id,g.State.Turn,true));Assert.False(rejected.Accepted);Assert.Same(g.State,rejected.State);Assert.Null(rejected.Report);Assert.Equal(before,FleetFixtures.Bytes(g.State));
    }
    [Theory]
    [InlineData(1,30,1)]
    [InlineData(801,10,761)]
    [InlineData(801,30,721)]
    [InlineData(999,30,900)]
    public void AlternativeCostsRoundUpAndRemainPositive(int basis,int xp,long expected)
    {
        var g=Organizer(xp,scoutCost:basis);Assert.Equal(expected,OfficerBonuses.RequestShipCost(g.Content,g.State,g.State.Colonies[0].Id,"scout"));
    }
    [Fact]
    public void BlockedCargoBanksOutputAndBuildingsRemainOrdinary()
    {
        var g=Organizer(role:"harl_settler",crises:true);var r=g.State.Empires[0].Fleets!.Requests[0];var target=g.State.Stars.First(s=>s.Id!=r.Rally).Id;var preview=FleetRequestTests.Retained(g,r,target);
        var changed=FleetRequestTests.Run(g,new AssemblyAction.ChangeRally(r.Id,r.Revision,target,null,preview.Fingerprint));Assert.True(changed.Accepted,changed.Rejection?.Message);g=new(g.Content,changed.State);var col=g.State.Colonies[0];col.Crisis!.Quarantined=true;col.ProductionReserve=1600;FleetFixtures.Valid(g);
        int population=col.Population;var forecast=EconomyCalculator.CalculateColony(g.Content,g.State,col);Assert.Equal(1520,forecast.Production.TargetCost);Assert.Equal("quarantine",forecast.Production.BlockedReasonCode);Assert.Equal(1600+forecast.NetProduction.Total,forecast.Production.Ending);Assert.Null(forecast.VesselCompletionPlan);Assert.Equal(population,col.Population);
        string building=g.Content.Buildings.Values.First(b=>b.PrerequisiteTechnologyIds.IsEmpty&&!col.CompletedBuildingIds.Contains(b.Id)).Id;
        var ordinary=CommandProcessor.Execute(g.Content,g.State,new SetProjectCommand(g.State.Empires[0].Id,g.State.Turn,col.Id,building));Assert.True(ordinary.Accepted,ordinary.Rejection?.Message);Assert.Equal(g.Content.Buildings[building].Cost,EconomyCalculator.CalculateColony(g.Content,ordinary.State,ordinary.State.Colonies[0]).Production.TargetCost);Assert.Equal(800,OfficerBonuses.RequestShipCost(g.Content,ordinary.State,col.Id,"scout"));
    }
}
