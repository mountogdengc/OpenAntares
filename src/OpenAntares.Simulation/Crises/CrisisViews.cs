using System.Collections.Immutable;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Crises;

public sealed record CrisisEpisodeView(long Id,string FamilyId,string Name,CrisisLocation Source,int SourcePopulation,long CureCost,long CureProgress,int ResearchShare,bool ResearchOverride,CureAllocation Forecast);
public sealed record CrisisCaseView(CrisisKey? Key,CrisisLocation Location,CrisisStage? Stage,int GraceRemaining,int ActiveAge,int Severity,long AttentionRevision,long HandledRevision,long WaivedRevision,bool Quarantined,long? ReleaseEpisode,bool PolicyPaused,string? ImmunityFamilyId,int ImmuneUntilTurn,int Population,int IntervalsUntilRecovery,bool NextIntervalMayLosePopulation,bool ColonyLossRisk,ColonyEconomy Economy);
public sealed record CrisisVesselView(VesselId Id,string RoleId,int Population,long? CarrierEpisode,int? InspectionDueTurn,StarId? Station,TransitState? Transit,StarId? PendingMove,PlanetId? PendingFounding,bool PortClosed,bool Held,bool CanDecontaminate,ImmutableArray<CrisisLocation> ContactRisks);
public sealed record CrisisView(int Turn,EmpireId Viewer,bool Enabled,CrisisAuthority Authority,int AuthorityResearchShare,bool ResearchPolicyPaused,bool RequireAcknowledgement,int CivicLevel,int CivicExpiresAt,CrisisEpisodeView? Episode,ImmutableArray<CrisisCaseView> Cases,ImmutableArray<CrisisVesselView> Carriers,ImmutableArray<CrisisReceipt> PendingReceipts,ImmutableArray<CrisisDigest> History,ImmutableArray<CrisisLocation> LostColonyLocations)
{
    public ImmutableArray<CrisisCaseView> ColonyConditions { get; init; }=ImmutableArray<CrisisCaseView>.Empty;
    public ImmutableArray<CrisisVesselView> Vessels { get; init; }=ImmutableArray<CrisisVesselView>.Empty;
    public ImmutableArray<CrisisDigest> PendingDigests { get; init; }=ImmutableArray<CrisisDigest>.Empty;
    public QuarantinePreview? ContainmentPreview { get; init; }
}
