using System.Linq;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static class DiplomacyReservations
{
    public static long Reserved(GameState state,ColonyId hub)
    {
        long sum=0;if(state.Diplomacy is null)return 0;
        foreach(var pair in state.Diplomacy.Pairs)
        {if(pair.Pending is not {} o||o.Withdraw)continue;if(o.ProposerHub?.Hub==hub)sum=checked(sum+o.ProposerHub.Reserved);if(o.Answer?.Accept==true&&o.Answer.Hub?.Hub==hub)sum=checked(sum+o.Answer.Hub.Reserved);}
        return sum;
    }
    public static long Spendable(GameState state,ColonyId hub)=>checked((state.FindColony(hub)?.ProductionReserve??0)-Reserved(state,hub));
    public static bool CanCommit(GameState state,ColonyId hub,long amount)=>amount>0&&Reserved(state,hub)==0&&Spendable(state,hub)>=amount;

    internal static OpenAntares.Simulation.Economy.ReserveOutcome CalculateReserve(GameState state,ColonyState colony,long output,string? target,long? cost,bool legal)
    {
        long reserved=Reserved(state,colony.Id);
        var result=OpenAntares.Simulation.Economy.EconomyCalculator.Reserve(checked(colony.ProductionReserve-reserved),output,target,cost,legal);
        return reserved==0?result:result with{Starting=colony.ProductionReserve,Ending=checked(result.Ending+reserved)};
    }
}
