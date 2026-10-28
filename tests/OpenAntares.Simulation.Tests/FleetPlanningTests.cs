using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Knowledge;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetPlanningTests
{
    internal static AssemblyDraft Draft(Saves.LoadedGame g,int ships=4)=>new("Survey flotilla",g.State.FindPlanet(g.State.Colonies[0].PlanetId)!.StarId,null,Enumerable.Repeat("scout",ships).ToImmutableArray(),g.State.Colonies.Where(c=>c.EmpireId==g.State.Empires[0].Id).Select(c=>c.Id).ToImmutableArray(),RequestPriority.Normal,ImmutableArray<VesselId>.Empty,false,null);
    internal static AssemblyPreview Preview(Saves.LoadedGame g,AssemblyDraft draft)=>AssemblyPlanner.Preview(FleetPlanningQueries.Create(g.Content,EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id)),draft);
    [Fact]
    public void OffersIdleShipsThenRanksConditionalCompletionAndArrival()
    {
        var g=FleetFixtures.Game();FleetOfficerTests.AddColony(g);var zero=FleetOfficerTests.AddColony(g);zero.Workforce=new(zero.Population,0,0);zero.ProductionReserve=0;
        foreach(var col in g.State.Colonies){col.ProjectId=null;col.ProductionReserve=0;}
        var draft=Draft(g);var offered=Preview(g,draft);Assert.True(offered.Allowed,string.Join(";",offered.Rejections));Assert.Single(offered.IdleCandidates);Assert.All(offered.Slots,s=>Assert.Null(s.Vessel));
        var accepted=Preview(g,draft with{AcceptedIdleVessels=ImmutableArray.Create(g.State.Vessels[0].Id)});Assert.True(accepted.Allowed);Assert.Equal(g.State.Vessels[0].Id,accepted.Slots[0].Vessel);
        Assert.Equal(3,accepted.Slots.Count(s=>s.Colony is not null));Assert.DoesNotContain(accepted.Slots,s=>s.Colony==zero.Id);Assert.All(accepted.Slots,s=>Assert.NotNull(s.ArrivalTurns));
        var choices=g.State.Colonies.Where(c=>c.Id!=zero.Id).Select(c=>
        {
            long output=Economy.EconomyCalculator.CalculateColony(g.Content,g.State,c).NetProduction.Total;
            var start=g.State.FindStar(g.State.FindPlanet(c.PlanetId)!.StarId)!;var end=g.State.FindStar(draft.Rally)!;
            long travel=start.Id==end.Id?0:Movement.TravelCalculator.Duration(start.X,start.Y,end.X,end.Y,300);
            return new{c.Id,Arrival=checked((800+output-1)/output+travel)};
        }).OrderBy(x=>x.Arrival).ThenBy(x=>x.Id).ToArray();Assert.Equal(choices[0].Id,accepted.Slots[1].Colony);
        string before=FleetFixtures.Bytes(g.State),json=JsonSerializer.Serialize(accepted);
        var context=FleetPlanningQueries.Create(g.Content,EmpireViewProjector.Project(g.Content,g.State,g.State.Empires[0].Id));Assert.Null(context.View.Galaxy.Seed);
        for(int i=0;i<100;i++)Assert.Equal(json,JsonSerializer.Serialize(AssemblyPlanner.Preview(context,draft with{AcceptedIdleVessels=ImmutableArray.Create(g.State.Vessels[0].Id)})));
        Assert.Equal(before,FleetFixtures.Bytes(g.State));
    }
    [Fact]
    public void UrgentPreviewRetainsBlockedActiveProjectAndRecomputesTail()
    {
        var g=FleetFixtures.Game();var col=g.State.Colonies[0];col.Workforce=new(col.Population,0,0);col.ProductionReserve=0;col.ProjectId="fabrication_hall";col.ProjectKind=ProductionKinds.Building;col.Development!.Origin=Development.ProductionOrigin.Manual;
        col.Development.Queue.Add(new(1,new(ProductionKinds.Vessel,"scout")));col.Development.NextQueueToken=2;
        var draft=Draft(g,2) with{Priority=RequestPriority.Urgent};var result=Preview(g,draft);Assert.True(result.Allowed,string.Join(";",result.Rejections));
        var preview=Assert.Single(result.Colonies);Assert.Equal("fabrication_hall",preview.ActiveProject!.ContentId);Assert.Single(preview.PriorQueue);Assert.Equal(new int?[]{1,2,null},preview.ProposedQueue.Select(q=>q.SlotOrdinal));Assert.Equal(1,preview.ProposedQueue.Last().Token);Assert.All(preview.ProposedQueue,q=>Assert.Null(q.CompletionTurns));Assert.All(result.Slots,s=>Assert.Null(s.ArrivalTurns));Assert.NotEmpty(result.Warnings);
        var normal=Preview(g,draft with{Priority=RequestPriority.Normal});Assert.Equal(1,normal.Colonies[0].ProposedQueue[0].Token);Assert.NotEqual(result.Fingerprint,normal.Fingerprint);
        col.Workforce=new(col.Population-1,1,0);var finite=Preview(g,draft);Assert.True(finite.Slots[1].CompletionTurns>finite.Slots[0].CompletionTurns);Assert.True(finite.Colonies[0].ProposedQueue.Last().CompletionTurns>finite.Slots[1].CompletionTurns);
    }
    [Fact]
    public void HiddenHabitatDoesNotChangePreviewButKnownUnsafeRallyRejects()
    {
        var g=FleetFixtures.Game(amebas:true,rivals:1);var draft=Draft(g,1);var initial=Preview(g,draft);Assert.True(initial.Allowed,string.Join(";",initial.Rejections));
        var other=g.State.Clone();var habitat=other.Scouting!.Habitat!;other.Scouting.Habitat=habitat with{Star=other.Stars.First(s=>s.Id!=habitat.Star&&!other.Colonies.Any(c=>other.FindPlanet(c.PlanetId)!.StarId==s.Id)).Id};other.Empires[1].Fleets!.Officers[0].Name="Hidden name";
        Assert.Equal(JsonSerializer.Serialize(initial),JsonSerializer.Serialize(Preview(new(g.Content,other),draft)));
        g.State.Empires[0].Scouting!.KnownHabitats.Add(new(habitat.Id,habitat.Star,0));Assert.False(Preview(g,draft with{Rally=habitat.Star}).Allowed);
    }
    [Fact]
    public void CargoAuthorizationAndQuotaBoundsRemainExplicit()
    {
        var g=FleetFixtures.Game();var draft=Draft(g,2) with{Roles=ImmutableArray.Create("harl_settler","harl_settler")};Assert.False(Preview(g,draft).Allowed);
        var authorized=Preview(g,draft with{AuthorizeCargo=true});Assert.True(authorized.Allowed,string.Join(";",authorized.Rejections));Assert.Equal(2,authorized.Colonies.Sum(c=>c.CargoTotal));
        Assert.False(Preview(g,Draft(g,33)).Allowed);var first=Preview(g,Draft(g,1));g.State.Colonies[0].ProductionReserve++;Assert.NotEqual(first.Fingerprint,Preview(g,Draft(g,1)).Fingerprint);
    }
    [Fact]
    public void AlternativeRoleAndFundedZeroOutputUseDeclaredValues()
    {
        var g=FleetFixtures.Game();var alternative=g.Content.Vessels["scout"] with{Id="courier",Name="Courier",Cost=1000,Speed=400};
        var species=g.Content.Species["harl"] with{AllowedVesselIds=g.Content.Species["harl"].AllowedVesselIds.Add("courier")};
        var c=g.Content with{Vessels=g.Content.Vessels.Add("courier",alternative),Species=g.Content.Species.SetItem("harl",species)};g=new(c,g.State);
        var col=g.State.Colonies[0];col.Workforce=new(col.Population,0,0);col.ProjectId=null;col.ProductionReserve=1000;
        var result=Preview(g,Draft(g,2) with{Roles=ImmutableArray.Create("courier","courier")});Assert.True(result.Allowed,string.Join(";",result.Rejections));
        Assert.Equal(1000,result.Slots[0].Cost);Assert.Equal(1,result.Slots[0].CompletionTurns);Assert.Null(result.Slots[1].CompletionTurns);Assert.Empty(result.IdleCandidates);
    }
    [Fact]
    public void QueueCounterAloneInvalidatesFingerprint()
    {
        var g=FleetFixtures.Game();var draft=Draft(g,1);var before=Preview(g,draft);Assert.True(before.Allowed);
        g.State.Colonies[0].Development!.NextQueueToken++;
        Assert.NotEqual(before.Fingerprint,Preview(g,draft).Fingerprint);
    }
}
