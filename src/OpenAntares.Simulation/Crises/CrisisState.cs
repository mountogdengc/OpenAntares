using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Random;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Crises;

public enum CrisisAuthority { Manual, Contain, ContainAndResearch }
public enum CrisisStage { Warning, Active }
public sealed class GameCrisisState
{
    public bool Enabled { get; set; }
    public GameCrisisState Clone() => new(){Enabled=Enabled};
}
public sealed class EmpireCrisisState
{
    public Pcg32 Random { get; set; } = Pcg32.FromSeed(0,0);
    public long NextEpisodeNumber { get; set; } = 1;
    public int NextEligibleTurn { get; set; }
    public CrisisEpisodeState? Episode { get; set; }
    public CrisisAuthority Authority { get; set; }
    public int AuthorityResearchShare { get; set; } = 50;
    public bool ResearchPolicyPaused { get; set; }
    public bool RequireAcknowledgement { get; set; } = true;
    public int CivicLevel { get; set; }
    public int CivicExpiresAt { get; set; }
    public List<CrisisDigest> PendingDigests { get; set; } = new();
    public List<CrisisDigest> CompletedDigests { get; set; } = new();
    public List<CrisisReceipt> PendingReceipts { get; set; } = new();
    public List<CrisisLocation> LostColonyLocations { get; set; } = new();
    public EmpireCrisisState Clone() => new(){Random=Random.Clone(),NextEpisodeNumber=NextEpisodeNumber,NextEligibleTurn=NextEligibleTurn,Episode=Episode?.Clone(),Authority=Authority,AuthorityResearchShare=AuthorityResearchShare,ResearchPolicyPaused=ResearchPolicyPaused,RequireAcknowledgement=RequireAcknowledgement,CivicLevel=CivicLevel,CivicExpiresAt=CivicExpiresAt,PendingDigests=new(PendingDigests),CompletedDigests=new(CompletedDigests),PendingReceipts=new(PendingReceipts),LostColonyLocations=new(LostColonyLocations)};
}
public sealed class CrisisEpisodeState
{
    public long Id { get; set; }
    public string FamilyId { get; set; } = string.Empty;
    public CrisisLocation Source { get; set; } = new(default,default,default,string.Empty);
    public int SourcePopulation { get; set; }
    public long CureCost { get; set; }
    public long CureProgress { get; set; }
    public int ResearchShare { get; set; }
    public bool ResearchOverride { get; set; }
    public CrisisEpisodeState Clone() => (CrisisEpisodeState)MemberwiseClone();
}
public sealed class ColonyCrisisState
{
    public CrisisCaseState? Case { get; set; }
    public bool Quarantined { get; set; }
    public long? ReleaseEpisode { get; set; }
    public bool PolicyPaused { get; set; }
    public string? ImmunityFamilyId { get; set; }
    public int ImmuneUntilTurn { get; set; }
    public ColonyCrisisState Clone() => new(){Case=Case?.Clone(),Quarantined=Quarantined,ReleaseEpisode=ReleaseEpisode,PolicyPaused=PolicyPaused,ImmunityFamilyId=ImmunityFamilyId,ImmuneUntilTurn=ImmuneUntilTurn};
}
public sealed class CrisisCaseState
{
    public long EpisodeId { get; set; }
    public CrisisStage Stage { get; set; }
    public int GraceRemaining { get; set; }
    public int ActiveAge { get; set; }
    public int Severity { get; set; }
    public long AttentionRevision { get; set; }
    public long HandledRevision { get; set; }
    public long WaivedRevision { get; set; }
    public CrisisLocation Origin { get; set; } = new(default,default,default,string.Empty);
    public VesselId? SourceVessel { get; set; }
    public CrisisCaseState Clone() => (CrisisCaseState)MemberwiseClone();
}
public sealed class VesselCrisisState
{
    public long? CarrierEpisode { get; set; }
    public int? InspectionDueTurn { get; set; }
    public VesselCrisisState Clone() => (VesselCrisisState)MemberwiseClone();
}
public sealed record CrisisKey(EmpireId Owner,long Episode,ColonyId Colony);
public sealed record CrisisLocation(ColonyId Colony,PlanetId Planet,StarId Star,string Name);
public sealed record CrisisReceipt(CrisisKey Key,long Revision,int Turn,CrisisLocation Location,string Reason);
public sealed record CrisisDigest(int Turn,ImmutableArray<CrisisDigestEntry> Entries);
public sealed record CrisisDigestEntry(CrisisKey Key,CrisisLocation Location,string Kind,long Revision,string Disposition,long ResearchDiverted,int PopulationLost,string Reason);
