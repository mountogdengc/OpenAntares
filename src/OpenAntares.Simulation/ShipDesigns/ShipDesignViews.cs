using OpenAntares.Simulation.Combat;
using System.Collections.Immutable;
namespace OpenAntares.Simulation.ShipDesigns;
public sealed record VesselLoadout(string RoleId,DesignId? Design,string? FrameId,long BuildCost,int BaseSpeed,CombatProfile? Profile);
public sealed record DesignReferences(int Vessels,int ActiveProjects,int QueueEntries,int RequestSlots,int Refits)
{
    public bool Any=>Vessels!=0||ActiveProjects!=0||QueueEntries!=0||RequestSlots!=0||Refits!=0;
}
public sealed record DesignPreview(bool Allowed,string? RejectionCode,DesignStats? Stats,ImmutableArray<string> MissingTechnologyIds);
public sealed record DesignView(DesignId Id,string Name,ShipRecipe Recipe,DesignStats Stats,bool Archived,bool Unlocked,DesignReferences References);
public sealed record ShipDesignEmpireView(long CatalogueRevision,ImmutableArray<DesignView> Designs);
