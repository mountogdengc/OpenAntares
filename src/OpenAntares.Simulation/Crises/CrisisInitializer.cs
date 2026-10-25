using System;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Random;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Crises;

public static class CrisisInitializer
{
    public static Pcg32 CreateRandom(ulong seed,EmpireId owner) => Pcg32.FromSeed(seed ^ 0x4352495349530001UL,(ulong)owner.Value ^ 0x504C414755450001UL);
    public static void Initialize(ContentSet content,GameState state,bool enabled)
    {
        if(!Rules.HasCrises(state.RulesVersion))return;
        if(content.Plagues.Count!=1||state.GalaxySeed is not {} seed)throw new ArgumentException("Crisis campaigns require one plague family and a galaxy seed.");
        state.Crisis=new(){Enabled=enabled};
        foreach(var e in state.Empires)e.Crisis=new(){Random=CreateRandom(seed,e.Id)};
        foreach(var c in state.Colonies)c.Crisis=new();
        foreach(var v in state.Vessels)v.Crisis=new();
    }
}
