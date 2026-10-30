using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using System.Collections.Immutable;
using System.Linq;
namespace OpenAntares.Simulation.ShipDesigns;
internal enum ShipDesignResolutionPhase { Capture,Economy,PostLoss,Refit,Observation,Validation }
public sealed class RefitResolutionContext
{
    private RefitResolutionContext() { }
    public int FromTurn {get;private init;}
    internal ImmutableArray<CapturedRefit> Jobs {get;private init;}
    internal bool Applied {get;set;}
    public static RefitResolutionContext Capture(ContentSet content,GameState state)
    {
        var jobs=ImmutableArray.CreateBuilder<CapturedRefit>();
        if(Rules.HasShipDesigns(state.RulesVersion))foreach(var owner in state.Empires.OrderBy(e=>e.Id))foreach(var job in owner.ShipDesigns!.Refits.OrderBy(j=>j.Id))
        {
            EquipmentFact Fact(DesignId id){var d=owner.ShipDesigns.Designs.Single(d=>d.Id==id);return new(d.Id,d.Name,d.Recipe,ShipDesignMath.Calculate(content.ShipDesigns!,d.Recipe));}
            jobs.Add(new(owner.Id,job.Clone(),Fact(job.SourceDesign),Fact(job.TargetDesign)));
        }
        return new(){FromTurn=state.Turn,Jobs=jobs.ToImmutable()};
    }
}
internal sealed record CapturedRefit(EmpireId Owner,RefitJob Job,EquipmentFact Before,EquipmentFact After);
