using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public static class ConflictInitializer
{
    public static void Initialize(ContentSet content,GameState state)
    {
        if(!Rules.HasConflict(state.RulesVersion))return;
        if(content.Conflict is null)throw new ArgumentException("Conflict content required, including Solo.");
        if(state.Conflict is not null)throw new InvalidOperationException("Conflict initializes only once during generation.");
        state.Conflict=new();foreach(var owner in state.Empires.OrderBy(e=>e.Id))state.Conflict.Owners.Add(new(){Owner=owner.Id});
        if(state.Diplomacy is not null)foreach(var pair in state.Diplomacy.Pairs)EnsurePair(state,pair.Pair.Low,pair.Pair.High);
    }
    internal static ConflictPairState EnsurePair(GameState state,EmpireId a,EmpireId b)
    {
        var key=DiplomacyPair.For(a,b);var existing=state.Conflict!.Pairs.SingleOrDefault(p=>p.Pair==key);if(existing is not null)return existing;
        var added=new ConflictPairState{Pair=key,Parties=new(){new(){Owner=key.Low},new(){Owner=key.High}}};state.Conflict.Pairs.Add(added);state.Conflict.Pairs.Sort((x,y)=>x.Pair.CompareTo(y.Pair));return added;
    }
}
