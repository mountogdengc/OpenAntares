using System;
using System.Linq;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.ShipDesigns;
public static class VesselLoadouts
{
    public static VesselLoadout Resolve(ContentSet content,GameState state,VesselState vessel)
    {
        if(!Rules.HasShipDesigns(state.RulesVersion))return Legacy(content,state.RulesVersion,vessel.RoleId);
        var definition=content.ShipDesigns??throw new ArgumentException("Ship-design content required.");
        if(vessel.RoleId!=definition.WarshipRoleId)
        {
            if(vessel.Design is not null)throw new ArgumentException("Civilian hull cannot carry a design.");
            return Legacy(content,state.RulesVersion,vessel.RoleId);
        }
        if(vessel.Design is not {} id||state.FindEmpire(vessel.EmpireId)?.ShipDesigns?.Designs.SingleOrDefault(d=>d.Id==id) is not {} design)
            throw new ArgumentException("Configured warship requires an owned installed design.");
        var stats=ShipDesignMath.Calculate(definition,design.Recipe);
        return new(vessel.RoleId,design.Id,design.Recipe.FrameId,stats.BuildCost,stats.BaseSpeed,new(stats.MaxHull,stats.Attack,stats.Armor));
    }
    public static VesselLoadout Resolve(ContentSet content,EmpireView view,VesselId vessel)
    {
        var own=view.Vessels.SingleOrDefault(v=>v.Id==vessel)??throw new ArgumentException("Owned vessel required.");
        if(!Rules.HasShipDesigns(view.RulesVersion))return Legacy(content,view.RulesVersion,own.RoleId);
        return own.Loadout is {} loadout&&loadout.RoleId==own.RoleId?loadout:throw new ArgumentException("Immutable own loadout facts required.");
    }
    public static CombatProfile CombatProfile(ContentSet content,GameState state,VesselState vessel)=>Resolve(content,state,vessel).Profile??throw new ArgumentException("Combat rules and an equipped profile are required.");
    private static VesselLoadout Legacy(ContentSet content,int rules,string roleId)
    {
        if(!content.Vessels.TryGetValue(roleId,out var role))throw new ArgumentException("Unknown vessel role.");
        CombatProfile? profile=null;
        if(Rules.HasCombat(rules))profile=content.Combat?.RoleProfiles.TryGetValue(roleId,out var found)==true?found:throw new ArgumentException("Combat role profile required.");
        return new(roleId,null,null,role.Cost,role.Speed,profile);
    }
}
