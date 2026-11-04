using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ConflictBlockadeTests
{
    // Labelled pre-session economic fixture, never earned campaign evidence.
    internal static GameSession Economic(bool trade=true,long output=701)
    {
        var g=ConflictOrderTests.LocalForces();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var target=ConflictOrderTests.Attack(g).Target;g.State.Vessels.Remove(g.State.FindVessel(target)!);var colony=g.State.Colonies.First(c=>c.EmpireId==b);var planet=g.State.FindPlanet(colony.PlanetId)!;var type=g.Content.PlanetTypes[planet.PlanetTypeId];var species=g.Content.Species[g.State.FindEmpire(b)!.SpeciesId!];
        var content=g.Content with{Species=g.Content.Species.SetItem(species.Id,species with{ProductionBasisPoints=10000,BaseGrowth=0,MaxSurplusGrowth=0}),PlanetTypes=g.Content.PlanetTypes.SetItem(type.Id,type with{ProductionPerWorker=output==0?200:output,SupportPerWorker=300})};colony.Workforce=new(colony.Population-(output==0?0:1),output==0?0:1,0);colony.ProjectId=null;colony.ProductionReserve=5000;g=new(content,g.State);
        if(trade)
        {
            var c=ConflictFixtures.AddNeutralObserver(g,planet.StarId);var template=g.State.Colonies.First(x=>x.EmpireId==a);var home=g.State.FindPlanet(template.PlanetId)!;var free=home.Clone();free.Id=new(g.State.AllocateId());free.StarId=planet.StarId;free.Orbit=g.State.Planets.Where(p=>p.StarId==planet.StarId).Max(p=>p.Orbit)+1;g.State.Planets.Add(free);var neutral=template.Clone();neutral.Id=new(g.State.AllocateId());neutral.PlanetId=free.Id;neutral.EmpireId=c;g.State.Colonies.Add(neutral);
            var key=DiplomacyPair.For(b,c);g.State.Diplomacy!.Pairs.Single(p=>p.Pair==key).Agreements.Add(new(){Kind=AgreementKind.Trade,ActivatedTurn=0,LowHub=new(colony.Id,b,0),HighHub=new(neutral.Id,c,0)});
        }
        g.State.Turn=10;ObservationUpdater.Refresh(g.State,10);ConflictObservations.Refresh(content,g.State);Assert.Empty(GameStateValidator.Validate(content,g.State));Assert.Equal(output+(trade?content.Diplomacy!.TradeOutput:0),EconomyCalculator.CalculateColony(content,g.State,colony).NetProduction.Total);return g;
    }
    internal static GameSession Activate(GameSession g)
    {
        var a=g.State.Empires[0].Id;var fleet=g.State.FindEmpire(a)!.Fleets!.Fleets[0];ConflictDeclarationTests.Issue(g,a,new ConflictAction.Blockade(fleet.Id,g.State.Empires[1].Id));return ConflictDeclarationTests.Publish(g);
    }
    [Fact] public void Blockade701PlusNeutralTrade100Equals450Everywhere()
    {
        var g=Economic();var b=g.State.Empires[1].Id;var colony=g.State.Colonies.First(c=>c.EmpireId==b);var normal=EconomyCalculator.CalculateColony(g.Content,g.State,colony);Assert.Equal(350,ConflictEconomy.Retained(701,50));g=Activate(g);var blocked=EconomyCalculator.CalculateColony(g.Content,g.State,g.State.FindColony(colony.Id)!);
        Assert.Equal(450,blocked.NetProduction.Total);Assert.Equal(normal.NetResearch.Total,blocked.NetResearch.Total);Assert.Equal(normal.NetResearch.Lines.ToArray(),blocked.NetResearch.Lines.ToArray());Assert.Equal(normal.SupportProduced.Total,blocked.SupportProduced.Total);Assert.Equal(normal.SupportProduced.Lines.ToArray(),blocked.SupportProduced.Lines.ToArray());Assert.Equal(normal.GrowthEarned.Total,blocked.GrowthEarned.Total);Assert.Equal(normal.GrowthEarned.Lines.ToArray(),blocked.GrowthEarned.Lines.ToArray());Assert.Equal(5000,g.State.FindColony(colony.Id)!.ProductionReserve);Assert.Equal(-351,Assert.Single(blocked.NetProduction.Lines,l=>l.Kind=="conflict_blockade").Amount);Assert.Equal(5450,blocked.Production.Ending);
    }
    [Theory][InlineData(0,50,0)][InlineData(701,50,350)][InlineData(long.MaxValue,100,long.MaxValue)][InlineData(long.MaxValue,1,92233720368547758)]
    public void RetentionUsesFloorAndWideMultiplication(long income,int percent,long expected)=>Assert.Equal(expected,ConflictEconomy.Retained(income,percent));
    [Fact] public void AcceptedOrderWaitsForPublicationAndManualLiftStopsCurrentPressure()
    {
        var g=Economic(false);var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var col=g.State.Colonies.First(c=>c.EmpireId==b);var fleet=g.State.FindEmpire(a)!.Fleets!.Fleets[0];ConflictDeclarationTests.Issue(g,a,new ConflictAction.Blockade(fleet.Id,b));Assert.Equal(701,EconomyCalculator.CalculateColony(g.Content,g.State,col).NetProduction.Total);
        g=ConflictDeclarationTests.Publish(g);Assert.Equal(350,EconomyCalculator.CalculateColony(g.Content,g.State,g.State.FindColony(col.Id)!).NetProduction.Total);ConflictDeclarationTests.Issue(g,a,new ConflictAction.LiftBlockade(fleet.Id));Assert.Equal(701,EconomyCalculator.CalculateColony(g.Content,g.State,g.State.FindColony(col.Id)!).NetProduction.Total);
    }
    [Fact] public void MultipleForcesDoNotStackAndNeutralCoColonyRemainsUnchanged()
    {
        var g=Economic();var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var neutral=g.State.Colonies.First(c=>c.EmpireId!=a&&c.EmpireId!=b);long ordinary=EconomyCalculator.CalculateColony(g.Content,g.State,neutral).NetProduction.Total;var original=g.State.FindEmpire(a)!.Fleets!.Fleets[0];var ship=ConflictFixtures.AddEscort(g,a,g.State.FindVessel(original.Flagship)!.StationaryStarId!.Value);var data=g.State.FindEmpire(a)!.Fleets!;var fleet=new FleetRecord{Id=new(data.NextFleetId++),Name="Labelled second blockade",Members=new(){ship.Id},Flagship=ship.Id};data.Fleets.Add(fleet);ship.Fleet=fleet.Id;
        ConflictDeclarationTests.Issue(g,a,new ConflictAction.Blockade(original.Id,b));ConflictDeclarationTests.Issue(g,a,new ConflictAction.Blockade(fleet.Id,b));g=ConflictDeclarationTests.Publish(g);Assert.Single(ConflictBlockades.Capture(g.Content,g.State).Pressure);var result=EconomyCalculator.CalculateColony(g.Content,g.State,g.State.Colonies.First(c=>c.EmpireId==b));Assert.Equal(450,result.NetProduction.Total);Assert.Single(result.NetProduction.Lines,l=>l.Kind=="conflict_blockade");Assert.Equal(ordinary,EconomyCalculator.CalculateColony(g.Content,g.State,g.State.FindColony(neutral.Id)!).NetProduction.Total);
    }
    [Theory][InlineData("idle")][InlineData("repair")][InlineData("new")][InlineData("crippled")]
    public void EveryStationaryArmedDefenderContestsEvenWhenItCannotInitiateAttack(string activity)
    {
        var g=Activate(Economic(false));var b=g.State.Empires[1].Id;var col=g.State.Colonies.First(c=>c.EmpireId==b);var ship=ConflictFixtures.AddEscort(g,b,g.State.FindPlanet(col.PlanetId)!.StarId,activity=="crippled"?1:100);
        if(activity=="new")ship.NewlyCompleted=true;if(activity=="repair"){ship.Combat!.Hull=60;ship.Combat.Repairing=true;ship.Combat.ReturnColony=col.Id;ship.Combat.OnArrival=CombatArrivalOrder.Repair;}
        ObservationUpdater.Refresh(g.State,g.State.Turn);ConflictObservations.Refresh(g.Content,g.State);Assert.Empty(ConflictBlockades.Capture(g.Content,g.State).Pressure);Assert.Equal("Contested",g.View(g.State.Empires[0].Id).Conflict!.Blockades[0].Status);Assert.Equal(701,EconomyCalculator.CalculateColony(g.Content,g.State,col).NetProduction.Total);
    }
    [Theory][InlineData(50,100,false)][InlineData(51,40,false)][InlineData(51,41,true)]
    public void OperationalHullAndCohesionThresholdsAreStrict(int hull,int cohesion,bool effective)
    {
        var g=Activate(Economic(false));var a=g.State.Empires[0].Id;var ship=g.State.FindVessel(g.State.FindEmpire(a)!.Fleets!.Fleets[0].Flagship)!;ship.Combat!.Hull=hull;ship.Combat.Cohesion=cohesion;Assert.Equal(effective,ConflictBlockades.Capture(g.Content,g.State).Pressure.Length==1);
    }
    [Fact] public void FrozenIncomeIsNotRepricedByArrivalOrBlockaderDeathLaterInTheBoundary()
    {
        var g=Activate(Economic(false));var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;var col=g.State.Colonies.First(c=>c.EmpireId==b);var frame=ConflictBlockades.Capture(g.Content,g.State);var blocked=EconomyCalculator.CalculateColony(g.Content,g.State,col);Assert.Equal(350,blocked.NetProduction.Total);
        var arrived=g.State.Clone();var session=new GameSession(g.Content,arrived);ConflictFixtures.AddEscort(session,b,arrived.FindPlanet(col.PlanetId)!.StarId);Assert.Empty(ConflictBlockades.Capture(g.Content,arrived).Pressure);var normal=blocked with{NetProduction=Quantity.From(blocked.NetProduction.Lines.Where(l=>l.Kind!="conflict_blockade").ToImmutableArray())};Assert.Equal(350,ConflictEconomy.Apply(g.Content,arrived,arrived.FindColony(col.Id)!,normal,frame).NetProduction.Total);
        var dead=g.State.Clone();dead.Vessels.Remove(dead.FindVessel(dead.FindEmpire(a)!.Fleets!.Fleets[0].Flagship)!);Assert.Empty(ConflictBlockades.Capture(g.Content,dead).Pressure);Assert.Equal(350,ConflictEconomy.Apply(g.Content,dead,dead.FindColony(col.Id)!,normal,frame).NetProduction.Total);Assert.Equal(350,ConflictEconomy.Apply(g.Content,g.State,col,blocked,frame).NetProduction.Total);
    }
    [Fact] public void AcceptedPeaceSuppressesTheCapturedCurrentIntervalPressure()
    {
        var g=Activate(Economic(false));var a=g.State.Empires[0].Id;var b=g.State.Empires[1].Id;foreach(var owner in new[]{a,b})ConflictDeclarationTests.Issue(g,owner,new ConflictAction.ProposePeace(owner==a?b:a));var context=ConflictResolutionContext.Capture(g.Content,g.State);Assert.Single(context.FrozenBlockades.Pressure);var candidate=g.State.Clone();ConflictRelations.Prepare(g.Content,g.State,candidate,context);Assert.Empty(context.FrozenBlockades.Pressure);Assert.Equal("Stopped by peace",context.FrozenBlockades.Orders[0].Status);Assert.Equal(701,EconomyCalculator.CalculateColony(g.Content,candidate,candidate.Colonies.First(c=>c.EmpireId==b)).NetProduction.Total);
    }
    [Fact] public void ZeroIncomeAndOutgoingStatusNeverExposeVictimBankOrOutput()
    {
        var g=Activate(Economic(false,0));var col=g.State.Colonies.First(c=>c.EmpireId==g.State.Empires[1].Id);col.ProductionReserve=987654321;var result=EconomyCalculator.CalculateColony(g.Content,g.State,col);Assert.Equal(0,result.NetProduction.Total);Assert.Equal(987654321,result.Production.Ending);Assert.Single(ConflictBlockades.Capture(g.Content,g.State).Pressure);string own=System.Text.Json.JsonSerializer.Serialize(g.View(g.State.Empires[0].Id).Conflict);Assert.DoesNotContain("987654321",own);Assert.DoesNotContain("ProductionReserve",own);
    }
    [Fact] public void RefittingArmedDefenderStillContestsPressure()
    {
        var g=Activate(Economic(false));var b=g.State.Empires[1].Id;var col=g.State.Colonies.First(c=>c.EmpireId==b);var ship=ConflictFixtures.AddEscort(g,b,g.State.FindPlanet(col.PlanetId)!.StarId);ConflictWithdrawalTests.Upgrade(g,b);ship.Design=new(1);ObservationUpdater.Refresh(g.State,g.State.Turn);ConflictObservations.Refresh(g.Content,g.State);
        var action=new ShipDesigns.RefitAction.Start(ship.Id,col.Id,new(2));var preview=ShipDesigns.RefitQueries.Preview(g.Content,g.View(b),action);Assert.True(preview.Allowed,preview.RejectionCode);var outcome=g.Execute(new ShipDesigns.RefitCommand(b,g.State.Turn,action,preview.Token));Assert.True(outcome.Accepted,outcome.Rejection?.Message);Assert.Empty(ConflictBlockades.Capture(g.Content,g.State).Pressure);Assert.Equal(701,EconomyCalculator.CalculateColony(g.Content,g.State,g.State.FindColony(col.Id)!).NetProduction.Total);
    }
}
