using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
using System;
namespace OpenAntares.Simulation.ShipDesigns;
public static class ShipDesignProduction
{
    public static ColonyEconomy Calculate(ContentSet content,GameState state,ColonyState colony,ColonyEconomy result)
    {
        if(colony.ProjectKind!=ProductionKinds.ShipDesign)return result;
        var design=ShipDesignTargets.Bound(content,state,colony.EmpireId,new(colony.ProjectKind,colony.ProjectId??string.Empty))??throw new ArgumentException("Owned pinned design required.");
        var definition=content.ShipDesigns!;
        long price=Fleets.OfficerBonuses.RequestShipCost(content,state,colony.Id,new ProductionTarget(colony.ProjectKind,colony.ProjectId!));
        var reserve=EconomyCalculator.Reserve(colony.ProductionReserve,result.NetProduction.Total,colony.ProjectId,price,true) with{TargetKind=ProductionKinds.ShipDesign};
        return result with{Production=reserve,VesselCompletionPlan=reserve.CompletedId is null?null:new VesselCompletionPlan(definition.WarshipRoleId,0,colony.Id,state.FindPlanet(colony.PlanetId)!.StarId){Design=design.Id}};
    }
}
