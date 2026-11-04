using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public static class ConflictEconomy
{
    public static long Retained(long income,int percent)
    {if(income<0||percent is <0 or >100)throw new ArgumentOutOfRangeException(nameof(percent));return checked((long)((Int128)income*percent/100));}
    public static ColonyEconomy Apply(ContentSet content,GameState state,ColonyState colony,ColonyEconomy result)=>Apply(content,state,colony,result,ConflictBlockades.Capture(content,state));
    internal static ColonyEconomy Apply(ContentSet content,GameState state,ColonyState colony,ColonyEconomy result,ConflictBlockadeFrame frame)
    {
        if(!Rules.HasConflict(state.RulesVersion)||result.NetProduction.Lines.Any(l=>l.Kind=="conflict_blockade"))return result;
        var star=state.FindPlanet(colony.PlanetId)!.StarId;var pressure=frame.Pressure.SingleOrDefault(p=>p.Victim==colony.EmpireId&&p.Star==star);if(pressure is null)return result;
        long trade=0;foreach(var line in result.NetProduction.Lines)if(line.Kind=="diplomacy_trade")trade=checked(trade+line.Amount);
        long basis=checked(result.NetProduction.Total-trade);long loss=checked(basis-Retained(basis,pressure.RetainedPercent));
        return loss==0?result:result with{NetProduction=Quantity.From(result.NetProduction.Lines.Add(new("conflict_blockade","blockade",null,-loss)))};
    }
}
