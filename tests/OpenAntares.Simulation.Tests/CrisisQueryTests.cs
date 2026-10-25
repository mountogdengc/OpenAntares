using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisQueryTests
{
    [Fact]
    public void NoCrisisViewReadsForeignFacts()
    {
        var g=CrisisFixtures.TwoHumansAndAi();var owner=g.State.Empires[0].Id;
        string before=JsonSerializer.Serialize(CrisisQueries.Project(g.Content,g.State,owner));string view=JsonSerializer.Serialize(EmpireViewProjector.Project(g.Content,g.State,owner));
        var foreign=g.State.Empires[1];foreign.ResearchReserve=long.MaxValue;
        foreign.Crisis!.Random.NextUInt32();foreign.Crisis.RequireAcknowledgement=false;foreign.Crisis.Authority=CrisisAuthority.ContainAndResearch;foreign.Crisis.CivicLevel=3;foreign.Crisis.CivicExpiresAt=5;
        var colony=g.State.Colonies.Single(c=>c.EmpireId==foreign.Id);var location=CrisisFixtures.Location(g.State,colony);
        foreign.Crisis.NextEpisodeNumber=2;foreign.Crisis.Episode=new(){Id=1,FamilyId="ash_bloom",Source=location,SourcePopulation=6,CureCost=1200,ResearchShare=100};colony.Crisis!.Quarantined=true;colony.Crisis.Case=new(){EpisodeId=1,Stage=CrisisStage.Active,Severity=80,AttentionRevision=1,Origin=location};
        Assert.Equal(before,JsonSerializer.Serialize(CrisisQueries.Project(g.Content,g.State,owner)));
        Assert.Equal(view,JsonSerializer.Serialize(EmpireViewProjector.Project(g.Content,g.State,owner)));
    }
    [Fact]
    public void PreviewsAndForecastsPreservePublishedBytesAndRandom()
    {
        var g=CrisisFixtures.Active(population:5);var c=g.State.Colonies[0];c.Workforce=new(2,2,1);c.ProductionReserve=901;c.GrowthProgress=32;
        string before=SaveGame.Serialize(g.Content,g.State);var owner=c.EmpireId;
        var purge=CrisisQueries.PreviewPurge(g.Content,g.State,owner,c.Id);
        Assert.True(purge.Allowed);Assert.Equal(3,purge.PopulationLost);Assert.Equal(2,purge.PopulationAfter);Assert.Equal(new Workforce(2,0,0),purge.WorkforceAfter);Assert.Equal(450,purge.ReserveAfter);Assert.Equal(1,purge.NextCivicLevel);Assert.Equal(g.State.Turn+5,purge.NextCivicExpiresAt);
        var quarantine=CrisisQueries.PreviewQuarantine(g.Content,g.State,owner,ImmutableArray.Create(c.Id),true,true);Assert.True(quarantine.Allowed);
        var view=CrisisQueries.Project(g.Content,g.State,owner);Assert.Single(view.Cases);Assert.False(view.Cases[0].ColonyLossRisk);
        new GameSession(g.Content,g.State).View(owner);new GameSession(g.Content,g.State).Forecast();
        Assert.Equal(before,SaveGame.Serialize(g.Content,g.State));
    }
    [Fact]
    public void PortPreviewIncludesRemotePendingArrivalAndOnlyOwnCargo()
    {
        var g=CrisisFixtures.TwoHumansAndAi();var s=g.State;var own=s.Empires[0].Id;var source=s.Colonies[0];var star=s.FindPlanet(source.PlanetId)!.StarId;
        s.Vessels.Add(new(){Id=new(s.AllocateId()),EmpireId=own,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=new(6),PendingMove=star,Crisis=new()});
        s.Vessels.Add(new(){Id=new(s.AllocateId()),EmpireId=s.Empires[1].Id,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=star,PendingMove=new(6),Crisis=new()});
        var preview=CrisisQueries.PreviewQuarantine(g.Content,s,own,ImmutableArray.Create(source.Id),true,false);
        Assert.True(preview.Allowed);Assert.Single(preview.CancelledOrders);Assert.Equal(s.Vessels[1].Id,preview.CancelledOrders[0].Id);Assert.Contains(star,preview.ClosedPortsAfter);
        source.Crisis!.Quarantined=true;Assert.True(CrisisQueries.PortClosed(s,own,star));Assert.False(CrisisQueries.PortClosed(s,s.Empires[1].Id,star));
    }
    [Fact]
    public void ForeignTargetsAndIllegalReleaseAreRejectedWithoutPrivateFacts()
    {
        var g=CrisisFixtures.TwoHumansAndAi();var own=g.State.Empires[0].Id;
        Assert.False(CrisisQueries.PreviewPurge(g.Content,g.State,own,g.State.Colonies[1].Id).Allowed);
        Assert.False(CrisisQueries.PreviewQuarantine(g.Content,g.State,own,ImmutableArray.Create(g.State.Colonies[1].Id),true,false).Allowed);
        Assert.False(CrisisQueries.PreviewQuarantine(g.Content,g.State,own,ImmutableArray.Create(g.State.Colonies[0].Id),true,true).Allowed);
    }
    [Fact]
    public void PendingAttentionIncludesLiveAndResolvedReceiptsWithoutDuplicates()
    {
        var g=CrisisFixtures.Warning();var c=g.State.Colonies[0];var e=g.State.Empires[0].Crisis!;var l=CrisisFixtures.Location(g.State,c);
        e.PendingReceipts.Add(new(new(c.EmpireId,1,c.Id),1,g.State.Turn,l,"Exposure"));
        Assert.Single(CrisisQueries.PendingAttention(g.State,c.EmpireId));
        c.Crisis!.Case=null;Assert.Single(CrisisQueries.PendingAttention(g.State,c.EmpireId));
        e.PendingReceipts.Clear();Assert.Empty(CrisisQueries.PendingAttention(g.State,c.EmpireId));
    }
    [Fact]
    public void PublishedViewDoesNotAliasLaterSourceMutationAndZeroColonyIsSafe()
    {
        var g=CrisisFixtures.Active();var view=CrisisQueries.Project(g.Content,g.State,g.State.Empires[0].Id);string before=JsonSerializer.Serialize(view);
        g.State.Colonies[0].Crisis!.Case!.Severity=100;g.State.Empires[0].Crisis!.Episode!.CureProgress=99;g.State.Empires[0].Crisis!.PendingReceipts.Clear();
        Assert.Equal(before,JsonSerializer.Serialize(view));
        g.State.Colonies.Clear();var zero=CrisisQueries.Project(g.Content,g.State,g.State.Empires[0].Id);Assert.Empty(zero.Cases);
    }

    [Fact]
    public void QuarantinePreviewChangesWhenDisplayedEconomicCostsChange()
    {
        var g=CrisisFixtures.Active();var c=g.State.Colonies[0];c.Workforce=new(2,2,2);
        var first=CrisisQueries.PreviewQuarantine(g.Content,g.State,c.EmpireId,ImmutableArray.Create(c.Id),true,true);
        c.Workforce=new(2,3,1);
        var second=CrisisQueries.PreviewQuarantine(g.Content,g.State,c.EmpireId,ImmutableArray.Create(c.Id),true,true);
        Assert.False(CrisisPreviewEquality.Equal(first,second));
    }
    [Fact]
    public void PreviewEqualityComparesNestedAssetsAndOrderValues()
    {
        var g=CrisisFixtures.Active();var c=g.State.Colonies[0];c.Development!.Template=ColonyFixtures.Snapshot(g.Content,"industrial");
        var first=CrisisQueries.PreviewPurge(g.Content,g.State,c.EmpireId,c.Id);
        var roundtrip=SaveGame.Deserialize(SaveGame.Serialize(g.Content,g.State)).Game!;
        var copied=CrisisQueries.PreviewPurge(roundtrip.Content,roundtrip.State,c.EmpireId,c.Id);
        Assert.True(CrisisPreviewEquality.Equal(first,copied));
        Assert.False(CrisisPreviewEquality.Equal(first,copied with{Orders=copied.Orders.SetItem(0,copied.Orders[0] with{Move=new StarId(6)})}));
        Assert.False(CrisisPreviewEquality.Equal(first,copied with{Assets=copied.Assets! with{Queue=ImmutableArray.Create(new OpenAntares.Simulation.Development.ProductionQueueEntry(1,new("building","fabrication_hall")))}}));
    }
}
