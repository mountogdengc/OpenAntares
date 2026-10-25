using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Random;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Scouting;
public static class ScoutInitializer
{
    public static Pcg32 PlacementRandom(ulong seed)=>Pcg32.FromSeed(seed^0x414D454241000001UL,0x4841424954415401UL);
    public static Pcg32 RiskRandom(ulong seed,EmpireId owner)=>Pcg32.FromSeed(seed^0x414D454241000001UL,(ulong)owner.Value^0x4553434150450001UL);
    public static void Initialize(ContentSet content,GameState state,bool enabled)
    {
        if(!Rules.HasScouting(state.RulesVersion))return;
        ulong seed=state.GalaxySeed??throw new ArgumentException("Scouting requires a galaxy seed.");
        var game=new ScoutGameState{Enabled=enabled,PlacementRandom=enabled?PlacementRandom(seed):null};
        if(enabled)
        {
            var occupied=state.Colonies.Select(c=>state.FindPlanet(c.PlanetId)!.StarId).ToHashSet();
            var candidates=state.Stars.Where(s=>!occupied.Contains(s.Id)).OrderBy(s=>s.Id).ToArray();
            if(candidates.Length>=2){game.Habitat=new(1,content.SpaceAmeba!.Id,candidates[game.PlacementRandom!.NextInt(candidates.Length)].Id);game.SelectionCount=1;}
        }
        state.Scouting=game;
        foreach(var e in state.Empires)e.Scouting=new(){RiskRandom=enabled?RiskRandom(seed,e.Id):null};
        foreach(var v in state.Vessels)InitializeVessel(content,state.RulesVersion,v);
    }
    public static void InitializeVessel(ContentSet content,int rules,VesselState vessel)
    {
        if(Rules.HasScouting(rules)&&content.Vessels.TryGetValue(vessel.RoleId,out var role)&&!role.CanFoundColony&&role.EmbarkPopulation==0&&vessel.CarriedPopulation==0)vessel.Scout=new();
    }
}
