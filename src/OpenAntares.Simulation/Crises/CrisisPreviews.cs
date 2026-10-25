using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Crises;

public sealed record VesselOrderFact(VesselId Id,StarId? Station,TransitState? Transit,StarId? Move,PlanetId? Founding,int? InspectionDueTurn);
public sealed record PurgeAssetFact(ImmutableArray<string> Buildings,ProductionTarget? Target,ImmutableArray<ProductionQueueEntry> Queue,GovernorTemplateSnapshot? Template,bool WorkforceEnabled,bool ConstructionEnabled,int GrowthProgress);
public sealed record CrisisCaseFact(CrisisKey Key,CrisisLocation Location,int Population,CrisisStage Stage,int Grace,int Age,int Severity,long Revision,bool Quarantined,long? ReleaseEpisode,bool PolicyPaused);
public sealed record CrisisCarrierFact(VesselId Id,string RoleId,int Population,long? Episode,int? InspectionDueTurn,StarId? Station,TransitState? Transit,StarId? Move,PlanetId? Founding);
public sealed record QuarantineColonyFact(ColonyId Colony,CrisisLocation Location,long? Episode,long Revision,bool Before,bool After,long? ReleaseBefore,long? ReleaseAfter,bool PolicyPaused,int Population);
public sealed record QuarantineEconomyFact(ColonyId Colony,Workforce Workforce,int Population,int GrowthProgress,long Reserve,ProductionTarget? Target,long ProductionBefore,long ProductionAfter,long ResearchBefore,long ResearchAfter,long GrowthBefore,long GrowthAfter,bool CargoHeld);
public sealed record PurgePreview
{
    public bool Allowed { get; init; }
    public string? RejectionReason { get; init; }
    public int Turn { get; init; }
    public EmpireId Owner { get; init; }
    public CrisisKey? Key { get; init; }
    public CrisisLocation? Location { get; init; }
    public CrisisCaseFact? Case { get; init; }
    public bool SourcePortClosedBefore { get; init; }
    public bool SourcePortClosedAfter { get; init; }
    public long Revision { get; init; }
    public int PopulationBefore { get; init; }
    public int PopulationLost { get; init; }
    public int PopulationAfter { get; init; }
    public Workforce WorkforceBefore { get; init; }
    public Workforce WorkforceAfter { get; init; }
    public long ReserveBefore { get; init; }
    public long ReserveAfter { get; init; }
    public int GrowthAfter { get; init; }
    public bool ColonyLost { get; init; }
    public bool LastColony { get; init; }
    public int OwnerColonyCount { get; init; }
    public int SurvivingFounders { get; init; }
    public int CivicLevelBefore { get; init; }
    public int CivicExpiresAtBefore { get; init; }
    public int NextCivicLevel { get; init; }
    public int NextCivicExpiresAt { get; init; }
    public PurgeAssetFact? Assets { get; init; }
    public ImmutableArray<CrisisCaseFact> OtherCases { get; init; }=ImmutableArray<CrisisCaseFact>.Empty;
    public ImmutableArray<CrisisCarrierFact> Carriers { get; init; }=ImmutableArray<CrisisCarrierFact>.Empty;
    public ImmutableArray<VesselOrderFact> Orders { get; init; }=ImmutableArray<VesselOrderFact>.Empty;
}
public sealed record QuarantinePreview
{
    public bool Allowed { get; init; }
    public string? RejectionReason { get; init; }
    public int Turn { get; init; }
    public EmpireId Owner { get; init; }
    public bool Enable { get; init; }
    public bool ReleaseOnRecovery { get; init; }
    public ImmutableArray<QuarantineColonyFact> Colonies { get; init; }=ImmutableArray<QuarantineColonyFact>.Empty;
    public ImmutableArray<StarId> ClosedPortsBefore { get; init; }=ImmutableArray<StarId>.Empty;
    public ImmutableArray<StarId> ClosedPortsAfter { get; init; }=ImmutableArray<StarId>.Empty;
    public ImmutableArray<VesselOrderFact> CancelledOrders { get; init; }=ImmutableArray<VesselOrderFact>.Empty;
    public ImmutableArray<QuarantineEconomyFact> Economy { get; init; }=ImmutableArray<QuarantineEconomyFact>.Empty;
}

/// <summary>Value comparison includes every immutable collection, including nested template lists.</summary>
public static class CrisisPreviewEquality
{
    public static bool Equal(PurgePreview a,PurgePreview b) => !a.OtherCases.IsDefault&&!b.OtherCases.IsDefault&&!a.Carriers.IsDefault&&!b.Carriers.IsDefault&&!a.Orders.IsDefault&&!b.Orders.IsDefault&&a with{Assets=b.Assets,OtherCases=b.OtherCases,Carriers=b.Carriers,Orders=b.Orders}==b
        &&Assets(a.Assets,b.Assets)&&a.OtherCases.SequenceEqual(b.OtherCases)&&a.Carriers.SequenceEqual(b.Carriers)&&a.Orders.SequenceEqual(b.Orders);
    public static bool Equal(QuarantinePreview a,QuarantinePreview b) => !a.Colonies.IsDefault&&!b.Colonies.IsDefault&&!a.ClosedPortsBefore.IsDefault&&!b.ClosedPortsBefore.IsDefault&&!a.ClosedPortsAfter.IsDefault&&!b.ClosedPortsAfter.IsDefault&&!a.CancelledOrders.IsDefault&&!b.CancelledOrders.IsDefault&&!a.Economy.IsDefault&&!b.Economy.IsDefault&&a with{Colonies=b.Colonies,ClosedPortsBefore=b.ClosedPortsBefore,ClosedPortsAfter=b.ClosedPortsAfter,CancelledOrders=b.CancelledOrders,Economy=b.Economy}==b
        &&a.Colonies.SequenceEqual(b.Colonies)&&a.ClosedPortsBefore.SequenceEqual(b.ClosedPortsBefore)&&a.ClosedPortsAfter.SequenceEqual(b.ClosedPortsAfter)&&a.CancelledOrders.SequenceEqual(b.CancelledOrders)&&a.Economy.SequenceEqual(b.Economy);
    private static bool Assets(PurgeAssetFact? a,PurgeAssetFact? b)
    {
        if(a is null||b is null)return a==b;
        return a with{Buildings=b.Buildings,Queue=b.Queue,Template=b.Template}==b&&a.Buildings.SequenceEqual(b.Buildings)&&a.Queue.SequenceEqual(b.Queue)&&Template(a.Template,b.Template);
    }
    private static bool Template(GovernorTemplateSnapshot? a,GovernorTemplateSnapshot? b)
    {
        if(a is null||b is null)return a==b;
        var x=a.Definition;var y=b.Definition;
        return a.Id==b.Id&&a.Revision==b.Revision&&x with{FoundationIds=y.FoundationIds,DevelopmentIds=y.DevelopmentIds}==y&&x.FoundationIds.SequenceEqual(y.FoundationIds)&&x.DevelopmentIds.SequenceEqual(y.DevelopmentIds);
    }
}
