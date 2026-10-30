using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class RefitResolutionTests
{
    [Fact]
    public void RefitBanksThenPaysOnceAfterFullInterval()
    {
        var session=Economic();var owner=session.State.Empires[0];var v=session.State.Vessels[0];var port=session.State.Colonies[0];Fleet(session,new FleetAction.Create("Fixture command",ImmutableArray.Create(v.Id),v.Id));var group=session.State.Empires[0].Fleets!.Fleets.Single();var posted=session.State.Empires[0].Fleets!.Officers[0];var assigned=FleetOfficerTests.Run(new LoadedGame(session.Content,session.State),new OfficerAction.AssignFlagship(posted.Id,group.Id));Assert.True(assigned.Accepted,assigned.Rejection?.Message);session=new(session.Content,assigned.State);
        var officer=OfficerFacts(session.State.Empires[0].Fleets!.Officers[0]);Start(session);Assert.Equal(300,session.State.Colonies[0].ProductionReserve);Assert.Equal(new DesignId(1),session.State.Vessels[0].Design);
        var first=Ready(session);Assert.Equal(11,session.State.Turn);Assert.Equal(500,session.State.Colonies[0].ProductionReserve);Assert.Equal(300,VesselLoadouts.Resolve(session.Content,session.State,session.State.Vessels[0]).BaseSpeed);Assert.Null(first.Report!.Economy.Colony(port.Id).Production.CompletedId);
        var second=Ready(session);var receipt=second.Report!.Economy.Colony(port.Id).Production;Assert.Equal(12,session.State.Turn);Assert.Equal(0,session.State.Colonies[0].ProductionReserve);Assert.Equal(400,VesselLoadouts.Resolve(session.Content,session.State,session.State.Vessels[0]).BaseSpeed);Assert.Equal("1",receipt.CompletedId);Assert.Equal(700,receipt.TargetCost);Assert.Equal(0,receipt.Ending);Assert.Equal(receipt.Starting+receipt.Change.Total,receipt.Ending);Assert.Equal(-700,Assert.Single(receipt.Change.Lines,l=>l.Kind==BreakdownKinds.CostPaid).Amount);
        var after=session.State.FindVessel(v.Id)!;Assert.Equal(100,after.Combat!.Hull);Assert.Equal(100,after.Combat.Cohesion);Assert.Equal(group.Id,after.Fleet);Assert.Equal(posted.Id,session.State.Empires[0].Fleets!.Fleets.Single().Officer);Assert.Equal(officer,OfficerFacts(session.State.Empires[0].Fleets!.Officers[0]));Assert.Empty(session.State.Empires[0].ShipDesigns!.Refits);Assert.False(session.State.Colonies[0].Development!.ConstructionEnabled);
        Ready(session);Assert.Equal(200,session.State.Colonies[0].ProductionReserve);Assert.Single(session.State.Empires[0].ShipDesigns!.RecentEvents.Concat(session.State.Empires[0].ShipDesigns!.PendingEvents),e=>e.Kind=="refit_completed");
    }
    [Fact]
    public void CancelAfterFirstIntervalKeepsFiveHundredAndStandardEquipment()
    {
        var session=Economic();Start(session);Ready(session);var job=session.State.Empires[0].ShipDesigns!.Refits.Single();RefitCommandTests.Apply(session,new RefitAction.Cancel(job.Id,job.Revision));Assert.Equal(500,session.State.Colonies[0].ProductionReserve);Assert.Equal(new DesignId(1),session.State.Vessels[0].Design);Assert.False(session.State.Colonies[0].Development!.ConstructionEnabled);Ready(session);Assert.Equal(700,session.State.Colonies[0].ProductionReserve);Assert.DoesNotContain(session.State.Empires[0].ShipDesigns!.PendingEvents,e=>e.Kind=="refit_completed");
    }
    [Fact]
    public void CoveredZeroOutputStillWaitsForOneBoundary()
    {
        var session=Economic(700,0);Start(session);Assert.Equal(700,session.State.Colonies[0].ProductionReserve);Assert.Equal(new DesignId(1),session.State.Vessels[0].Design);Assert.True(session.Forecast().Colonies[0].RefitPlan!.Covered);Ready(session);Assert.Equal(11,session.State.Turn);Assert.Equal(0,session.State.Colonies[0].ProductionReserve);Assert.Equal(new DesignId(2),session.State.Vessels[0].Design);Ready(session);Assert.Equal(0,session.State.Colonies[0].ProductionReserve);
    }
    [Fact]
    public void ArchiveAfterCommitStillFinishesPinnedRecipe()
    {
        var session=Economic(700,0);Start(session);var e=session.State.Empires[0];Do(session,new ShipDesignCommand(e.Id,session.State.Turn,e.ShipDesigns!.CatalogueRevision,new ShipDesignAction.Archive(new(2))));Ready(session);Assert.Equal(new DesignId(2),session.State.Vessels[0].Design);Assert.Equal(0,session.State.Colonies[0].ProductionReserve);Assert.Empty(GameStateValidator.Validate(session.Content,session.State));
    }
    [Fact]
    public void PausedRefitBanksWithoutPayingAndResumeRequiresAnInterval()
    {
        var session=Economic(700,0);Start(session);var job=session.State.Empires[0].ShipDesigns!.Refits.Single();job.Status=RefitStatus.Paused;job.Reason="Labelled paused supplement";job.Revision++;Assert.Empty(GameStateValidator.Validate(session.Content,session.State));Assert.False(session.Forecast().Colonies[0].RefitPlan!.Covered);
        var result=Ready(session);Assert.Equal(700,session.State.Colonies[0].ProductionReserve);Assert.Equal(new DesignId(1),session.State.Vessels[0].Design);Assert.Null(result.Report!.Economy.Colonies[0].Production.CompletedId);job=session.State.Empires[0].ShipDesigns!.Refits.Single();RefitCommandTests.Apply(session,new RefitAction.Resume(job.Id,job.Revision));Assert.Equal(new DesignId(1),session.State.Vessels[0].Design);Ready(session);Assert.Equal(new DesignId(2),session.State.Vessels[0].Design);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public void MissingShipCancelsActiveOrPausedJobWithoutPayment(bool paused)
    {
        var session=Economic();Start(session);if(paused){var job=session.State.Empires[0].ShipDesigns!.Refits.Single();job.Status=RefitStatus.Paused;job.Reason="Labelled pause";}
        var snapshot=session.State;var context=RefitResolutionContext.Capture(session.Content,snapshot);var economy=EconomyCalculator.Calculate(session.Content,snapshot);var candidate=Bank(snapshot,economy);candidate.Vessels.Clear();
        var result=RefitResolver.Apply(session.Content,snapshot,candidate,context,economy);Assert.Empty(candidate.Empires[0].ShipDesigns!.Refits);Assert.Equal(500,candidate.Colonies[0].ProductionReserve);Assert.Null(candidate.Colonies[0].ProjectId);Assert.Null(result.Economy.Colonies[0].Production.CompletedId);Assert.Equal(0,Assert.Single(result.Events).Paid);Assert.Throws<InvalidOperationException>(()=>RefitResolver.Apply(session.Content,snapshot,candidate,context,result.Economy));
    }
    [Fact]
    public void LateConditionLossPausesWithoutPayingOrCompletingAnotherProject()
    {
        var session=Economic(700,0);var col=session.State.Colonies[0];col.Development!.Queue.Add(new(1,new(ProductionKinds.Building,session.Content.Buildings.Values.First(b=>b.PrerequisiteTechnologyIds.IsEmpty).Id)));col.Development.NextQueueToken=2;Start(session);
        var snapshot=session.State;var context=RefitResolutionContext.Capture(session.Content,snapshot);var economy=EconomyCalculator.Calculate(session.Content,snapshot);var candidate=Bank(snapshot,economy);candidate.Vessels[0].Combat!.Hull=99;
        var result=RefitResolver.Apply(session.Content,snapshot,candidate,context,economy);Assert.Equal(700,candidate.Colonies[0].ProductionReserve);Assert.Equal(RefitStatus.Paused,candidate.Empires[0].ShipDesigns!.Refits.Single().Status);Assert.Equal(new DesignId(1),candidate.Vessels[0].Design);Assert.Equal(99,candidate.Vessels[0].Combat!.Hull);Assert.Null(result.Economy.Colonies[0].Production.CompletedId);Assert.Single(candidate.Colonies[0].Development!.Queue);Assert.Empty(GameStateValidator.Validate(session.Content,candidate));
    }
    [Fact]
    public void FrigateRefitPaysTheTwelveTwentyFiveCeilingWithoutHealing()
    {
        var session=ShipDesignMathTests.Equipped("frigate");var source=ShipDesignFixtures.SaveDesign(session,"Light frigate",new("frigate_frame","conventional_drive",null,ImmutableArray.Create("pulse_bank","pulse_bank")));session.State.Vessels[0].Design=source;session=Economic(1225,0,session);
        Start(session,new(2));Assert.Equal(1225,session.State.Empires[0].ShipDesigns!.Refits.Single().Price);var result=Ready(session);Assert.Equal(1225,result.Report!.Economy.Colonies[0].Production.TargetCost);Assert.Equal(0,session.State.Colonies[0].ProductionReserve);Assert.Equal(180,session.State.Vessels[0].Combat!.Hull);Assert.Equal(100,session.State.Vessels[0].Combat!.Cohesion);Assert.Equal(new DesignId(2),session.State.Vessels[0].Design);
    }
    [Fact]
    public void CompletedRequestSurvivesLaterPaidRefitWithoutNewXp()
    {
        var session=Economic(700,0,ShipDesignMathTests.Equipped("gun"));var e=session.State.Empires[0];var ship=session.State.Vessels[0];var draft=new AssemblyDraft("Historical exact slot",ship.StationaryStarId!.Value,null,ImmutableArray<string>.Empty,ImmutableArray.Create(session.State.Colonies[0].Id),RequestPriority.Normal,ImmutableArray.Create(ship.Id),false,null){Quotas=ImmutableArray.Create(new AssemblyQuota("escort",new(2)))};
        var p=AssemblyPlanner.Preview(FleetPlanningQueries.Create(session.Content,session.View(e.Id)),draft);Assert.True(p.Allowed,string.Join("; ",p.Rejections));Do(session,new AssemblyCommand(e.Id,10,new AssemblyAction.Accept(draft,p.Fingerprint)));Ready(session);Assert.Equal(RequestStatus.Complete,session.State.Empires[0].Fleets!.Requests.Single().Status);
        var fast=ShipDesignFixtures.SaveDesign(session,"Fast",session.Content.ShipDesigns!.StartingRecipe with{EngineId="vector_drive"});var careers=JsonSerializer.Serialize(session.State.Empires[0].Fleets!.Officers.Select(o=>new{o.Xp,o.Rank,o.Career}));Start(session,fast);Ready(session);
        Assert.Equal(fast,session.State.FindVessel(ship.Id)!.Design);Assert.Equal(new DesignId(2),session.State.Empires[0].Fleets!.Requests.Single().Slots.Single().Design);Assert.Equal(0,session.State.Colonies[0].ProductionReserve);Ready(session);Assert.Equal(careers,JsonSerializer.Serialize(session.State.Empires[0].Fleets!.Officers.Select(o=>new{o.Xp,o.Rank,o.Career})));Assert.Empty(GameStateValidator.Validate(session.Content,session.State));
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public void ArrivalOrNewConstructionCannotCountAsTheRefitInterval(bool construction)
    {
        var session=Economic(5000,0);var e=session.State.Empires[0];var port=session.State.Colonies[0];VesselId subject=session.State.Vessels[0].Id;
        if(construction)Do(session,new SetProductionTargetCommand(e.Id,10,port.Id,ShipDesignTargets.Build(new(1))));
        else
        {
            var ship=session.State.Vessels[0];var home=session.State.FindStar(ship.StationaryStarId!.Value)!;var origin=session.State.Stars.First(st=>st.Id!=home.Id);int total=OpenAntares.Simulation.Movement.TravelCalculator.Duration(origin.X,origin.Y,home.X,home.Y,300);ship.StationaryStarId=null;ship.Transit=new(origin.Id,home.Id,total,1);ObservationUpdater.Refresh(session.State,10);
            Assert.False(RefitCommandTests.Preview(session,new RefitAction.Start(ship.Id,port.Id,new(2))).Allowed);
        }
        Ready(session);Assert.Equal(11,session.State.Turn);if(construction)subject=session.State.Vessels.Last().Id;long reserve=session.State.Colonies[0].ProductionReserve;int count=session.State.Vessels.Count;
        RefitCommandTests.Apply(session,new RefitAction.Start(subject,port.Id,new(2)));Assert.Equal(reserve,session.State.Colonies[0].ProductionReserve);Assert.Equal(new DesignId(1),session.State.FindVessel(subject)!.Design);Ready(session);Assert.Equal(12,session.State.Turn);Assert.Equal(new DesignId(2),session.State.FindVessel(subject)!.Design);Assert.Equal(reserve-700,session.State.Colonies[0].ProductionReserve);Assert.Equal(count,session.State.Vessels.Count);
    }
    [Fact]
    public void TwoPortsAtOneStarOwnIndependentJobsAndStablePayments()
    {
        var session=Economic(700,0);var s=session.State;var first=s.Colonies[0];var home=s.FindPlanet(first.PlanetId)!;var secondPlanet=s.Planets.First(p=>p.StarId==home.StarId&&p.Id!=home.Id);secondPlanet.PlanetTypeId=home.PlanetTypeId;secondPlanet.EnvironmentId=home.EnvironmentId;var second=first.Clone();second.Id=new(s.AllocateId());second.PlanetId=secondPlanet.Id;s.Colonies.Add(second);var ship=s.Vessels[0].Clone();ship.Id=new(s.AllocateId());s.Vessels.Add(ship);ObservationUpdater.Refresh(s,10);Assert.Empty(GameStateValidator.Validate(session.Content,s));
        Start(session);RefitCommandTests.Apply(session,new RefitAction.Start(ship.Id,second.Id,new(2)));Assert.Equal(new[]{1,2},session.State.Empires[0].ShipDesigns!.Refits.Select(j=>j.Id.Value));var result=Ready(session);Assert.All(session.State.Vessels,v=>Assert.Equal(new DesignId(2),v.Design));Assert.All(session.State.Colonies,c=>Assert.Equal(0,c.ProductionReserve));Assert.Equal(new[]{s.Vessels[0].Id,ship.Id},result.Report!.ShipDesignEvents!.Value.Where(e=>e.Kind=="refit_completed").Select(e=>e.Vessel!.Value));
    }
    internal static GameSession Economic(long reserve=300,long output=200,GameSession? supplied=null)
    {
        // Labelled fixed-output economic supplement; generated campaigns never call this helper.
        var session=supplied??RefitCommandTests.Setup();var c=session.Content;var s=session.State;var col=s.Colonies[0];var planet=s.FindPlanet(col.PlanetId)!;var type=c.PlanetTypes[planet.PlanetTypeId];var sp=c.Species[s.Empires[0].SpeciesId!];
        c=c with{Species=c.Species.SetItem(sp.Id,sp with{ProductionBasisPoints=10000,BaseGrowth=0,MaxSurplusGrowth=0}),PlanetTypes=c.PlanetTypes.SetItem(type.Id,type with{ProductionPerWorker=output==0?200:output,SupportPerWorker=300})};
        col.Workforce=new(col.Population-(output==0?0:1),output==0?0:1,0);col.ProductionReserve=reserve;s.Turn=10;ObservationUpdater.Refresh(s,10);session=new(c,s);Assert.Equal(output,session.Forecast().Colonies[0].NetProduction.Total);Assert.Empty(GameStateValidator.Validate(c,s));return session;
    }
    internal static void Start(GameSession s,DesignId? target=null)=>RefitCommandTests.Apply(s,new RefitAction.Start(s.State.Vessels[0].Id,s.State.Colonies[0].Id,target??new(2)));
    private static CommandOutcome Ready(GameSession s)=>Do(s,new SetReadyCommand(s.State.Empires[0].Id,s.State.Turn,true));
    private static CommandOutcome Do(GameSession s,Command c){var r=s.Execute(c);Assert.True(r.Accepted,r.Rejection?.Message);return r;}
    private static string OfficerFacts(OfficerRecord o)=>JsonSerializer.Serialize(new{o.Id,o.Name,o.ProfileId,o.SpeciesId,o.CultureId,o.Embodiment,o.SourceBody,o.Flagship,o.Status,o.Doctrine,o.Xp,o.Rank,o.Career});
    private static GameState Bank(GameState s,TurnEconomy economy){var c=s.Clone();foreach(var result in economy.Colonies)c.FindColony(result.Colony)!.ProductionReserve=result.Production.Ending;return c;}
    private static void Fleet(GameSession s,FleetAction a){var r=FleetCommandTests.Run(new LoadedGame(s.Content,s.State),a);Assert.True(r.Accepted,r.Rejection?.Message);var command=new FleetCommand(s.State.Empires[0].Id,s.State.Turn,a){Fingerprint=FleetQueries.PreviewAction(s.Content,s.View(s.State.Empires[0].Id),a).Fingerprint};Do(s,command);}
}
