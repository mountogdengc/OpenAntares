using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static class DiplomacyEconomy
{
    public static long TradeOutput(ContentSet content,DiplomacyFrame frame,ColonyId hub)
    {
        if(content.Diplomacy is not {} d)return 0;long total=0;
        foreach(var a in frame.Agreements)
            if(a.Kind==AgreementKind.Trade&&(a.LowHub?.Hub==hub||a.HighHub?.Hub==hub)&&(long)frame.Turn-a.ActivatedTurn>=d.Warmup&&(a.EndTurn is null||a.EndTurn>frame.Turn))total=checked(total+d.TradeOutput);
        return total;
    }
    public static TurnEconomy IncludeSetupPayments(TurnEconomy economy,ImmutableArray<DiplomacyEvent> events)
    {
        var rows=economy.Colonies.Select(c=>
        {
            long paid=0;foreach(var e in events)if(e.Viewer==c.Empire&&e.OwnHub==c.Colony)paid=checked(paid+e.Paid);
            if(paid==0||c.Production.Change.Lines.Any(l=>l.Kind=="diplomacy_setup_paid"))return c;
            var change=Quantity.From(c.Production.Change.Lines.Add(new("diplomacy_setup_paid","trade_agreement",null,-paid)));
            return c with{Production=c.Production with{Starting=checked(c.Production.Starting+paid),Change=change}};
        }).ToImmutableArray();
        return economy with{Colonies=rows};
    }
}
