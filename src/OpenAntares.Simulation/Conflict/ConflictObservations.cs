using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public static class ConflictObservations
{
    internal static bool OwnPresence(GameState state,EmpireId viewer,StarId star)=>state.Colonies.Any(c=>c.EmpireId==viewer&&state.FindPlanet(c.PlanetId)?.StarId==star)||state.Vessels.Any(v=>v.EmpireId==viewer&&v.StationaryStarId==star&&v.Transit is null);
    internal static bool IsMilitary(ContentSet content,GameState state,VesselState v)=>content.Vessels.TryGetValue(v.RoleId,out var role)&&role.CombatOnly&&!role.CanFoundColony&&role.EmbarkPopulation==0&&v.CarriedPopulation==0&&VesselLoadouts.Resolve(content,state,v).Profile?.Attack>0;
    public static void Refresh(ContentSet content,GameState state)
    {
        if(!Rules.HasConflict(state.RulesVersion))return;
        foreach(var pair in state.Diplomacy!.Pairs)ConflictInitializer.EnsurePair(state,pair.Pair.Low,pair.Pair.High);
        foreach(var owner in state.Empires.OrderBy(e=>e.Id))
        {
            var data=state.Conflict!.Owners.Single(o=>o.Owner==owner.Id);
            var seen=state.Vessels.Where(v=>v.EmpireId!=owner.Id&&v.StationaryStarId is {} star&&OwnPresence(state,owner.Id,star)&&owner.Contacts.Any(c=>c.OtherEmpireId==v.EmpireId)&&IsMilitary(content,state,v)).OrderBy(v=>v.Id).Select(v=>
            {
                var load=VesselLoadouts.Resolve(content,state,v);var p=load.Profile!;
                // Sensors expose equipment capability, never private Navigator bonuses.
                int equipmentSpeed=CombatMath.Stats(p,v.Combat!.Hull,load.BaseSpeed,0,content.Combat).Speed;
                var row=new MilitaryObservation(v.Id,v.EmpireId,v.RoleId,v.StationaryStarId!.Value,state.Turn,true,v.Fleet,v.Combat.Hull,p.MaxHull,p.Attack,p.Armor,equipmentSpeed);
                if(v.Design is {} id)
                {
                    var recipe=state.FindEmpire(v.EmpireId)!.ShipDesigns!.Designs.Single(d=>d.Id==id).Recipe;
                    row=row with{FrameId=recipe.FrameId,EngineId=recipe.EngineId,ArmorId=recipe.ArmorId,WeaponBanks=recipe.WeaponBankIds};
                }
                return row;
            }).ToArray();
            var ids=seen.Select(x=>x.Vessel).ToHashSet();
            var stale=data.Observations.Where(o=>!ids.Contains(o.Vessel)).Select(o=>o with{Current=false}).OrderByDescending(o=>o.Turn).ThenBy(o=>o.Vessel).Take(256);
            data.Observations=seen.Concat(stale).OrderBy(o=>o.Vessel).ToList();
        }
    }
    internal static ImmutableArray<MilitaryObservation> ForViewer(GameState state,EmpireId viewer)=>state.Conflict!.Owners.Single(o=>o.Owner==viewer).Observations.Select(o=>o with{Current=o.Current&&o.Turn==state.Turn&&OwnPresence(state,viewer,o.Star)}).OrderBy(o=>o.Vessel).ToImmutableArray();
}
