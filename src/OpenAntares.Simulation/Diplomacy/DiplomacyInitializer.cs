using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static class DiplomacyInitializer
{
    public static void Initialize(ContentSet content,GameState state){state.Diplomacy=new();}

    internal static void EnsurePair(GameState state,EmpireId a,EmpireId b)
    {
        var key=DiplomacyPair.For(a,b);if(state.Diplomacy!.Pairs.Any(p=>p.Pair==key))return;
        state.Diplomacy.Pairs.Add(new(){Pair=key,Parties=new(){new(){Owner=key.Low},new(){Owner=key.High}}});state.Diplomacy.Pairs.Sort((x,y)=>x.Pair.CompareTo(y.Pair));
    }
}
