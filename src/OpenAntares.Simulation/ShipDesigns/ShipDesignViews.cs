using OpenAntares.Simulation.Combat;
using System.Collections.Immutable;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.ShipDesigns;
public sealed record VesselLoadout(string RoleId,DesignId? Design,string? FrameId,long BuildCost,int BaseSpeed,CombatProfile? Profile);
public sealed record DesignReferences(int Vessels,int ActiveProjects,int QueueEntries,int RequestSlots,int Refits)
{
    public bool Any=>Vessels!=0||ActiveProjects!=0||QueueEntries!=0||RequestSlots!=0||Refits!=0;
}
public sealed record DesignPreview(bool Allowed,string? RejectionCode,DesignStats? Stats,ImmutableArray<string> MissingTechnologyIds);
public sealed record DesignView(DesignId Id,string Name,ShipRecipe Recipe,DesignStats Stats,bool Archived,bool Unlocked,DesignReferences References);
public sealed record CatalogueFilter(bool IncludeArchived=false,string? Search=null);
public sealed record CatalogueRow(DesignView Design,string? ArchiveBlockedReason,string? RestoreBlockedReason,string? DeleteBlockedReason);
public sealed record CataloguePage(int PageIndex,int PageCount,int Total,ImmutableArray<CatalogueRow> Rows);
public sealed record ShipDesignEmpireView(long CatalogueRevision,ImmutableArray<DesignView> Designs)
{
    public ImmutableArray<RefitView> Refits {get;init;}=ImmutableArray<RefitView>.Empty;
    public ImmutableArray<ShipDesignEvent> Events {get;init;}=ImmutableArray<ShipDesignEvent>.Empty;
}
public sealed record RefitView(RefitId Id,VesselId Vessel,ColonyId Port,DesignId SourceDesign,DesignId TargetDesign,int AcceptedTurn,long Revision,long Price,RefitStatus Status,string? Reason)
{
    public long Reserve {get;init;}
    public long OutputPerTurn {get;init;}
    public long? EstimatedTurns {get;init;}
    public string StatusCode {get;init;}=string.Empty;
    public string StatusExplanation {get;init;}=string.Empty;
    public EquipmentFact? Before {get;init;}
    public EquipmentFact? After {get;init;}
}
public sealed record RefitPreview(bool Allowed,string? RejectionCode,string Token,RefitId? Job,VesselId? Vessel,ColonyId? Port,DesignId? Source,DesignId? Target,DesignStats? Before,DesignStats? After,long? Price,long? EstimatedTurns,ImmutableArray<string> Consequences);
