using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.ShipDesigns;
public sealed record ShipDesignCommand(EmpireId Issuer,int ExpectedTurn,long CatalogueRevision,ShipDesignAction Action):Command(Issuer,ExpectedTurn);
public abstract record ShipDesignAction
{
    public sealed record Save(string Name,ShipRecipe Recipe):ShipDesignAction;
    public sealed record Archive(DesignId Id):ShipDesignAction;
    public sealed record Restore(DesignId Id):ShipDesignAction;
    public sealed record Delete(DesignId Id):ShipDesignAction;
}
