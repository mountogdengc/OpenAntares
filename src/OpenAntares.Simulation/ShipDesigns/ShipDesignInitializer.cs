using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using System;
namespace OpenAntares.Simulation.ShipDesigns;
public static class ShipDesignInitializer
{
    public static void Initialize(ContentSet content,GameState state)
    {
        if(!Rules.HasShipDesigns(state.RulesVersion))return;
        var definition=content.ShipDesigns??throw new ArgumentException("Ship-design content required, including Off.");
        foreach(var owner in state.Empires)
        {
            if(owner.ShipDesigns is not null)throw new InvalidOperationException("Ship designs are initialized only once during generation.");
            owner.ShipDesigns=new(){NextDesignId=2,Designs=new(){new(new(1),"Standard Escort",definition.StartingRecipe,false)}};
        }
        foreach(var ship in state.Vessels)if(ship.RoleId==definition.WarshipRoleId)ship.Design=new(1);
    }
}
