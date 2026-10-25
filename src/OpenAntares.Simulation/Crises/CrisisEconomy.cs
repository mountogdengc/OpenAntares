using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Crises;

public sealed record CureAllocation(long Starting,long Cost,long Requested,long Diverted,long RemainingForTechnology,bool Completes);

public static class CrisisEconomy
{
    public static long Scale(long amount,int factorBasisPoints)
    {
        if(amount<0||factorBasisPoints is <0 or >10000)throw new ArgumentOutOfRangeException(nameof(amount));
        return checked((long)((Int128)amount*factorBasisPoints/10000));
    }
    public static ColonyEconomy ApplyColony(ContentSet content,GameState state,ColonyState colony,ColonyEconomy result)
    {
        if(!Rules.HasCrises(state.RulesVersion)||state.Crisis is not{Enabled:true})return result;
        var d=content.Plagues.Values.Single();var c=colony.Crisis;var owner=state.FindEmpire(colony.EmpireId)!.Crisis!;
        var production=result.NetProduction;var research=result.NetResearch;
        bool ill=c?.Case?.Stage==CrisisStage.Active,quarantine=c?.Quarantined==true;
        if(ill){production=Factor(production,d.IllnessFactorBp,BreakdownKinds.CrisisIllness,d.Id);research=Factor(research,d.IllnessFactorBp,BreakdownKinds.CrisisIllness,d.Id);}
        if(quarantine){production=Factor(production,d.QuarantineFactorBp,BreakdownKinds.CrisisQuarantine,d.Id);research=Factor(research,d.QuarantineFactorBp,BreakdownKinds.CrisisQuarantine,d.Id);}
        if(owner.CivicLevel>0&&state.Turn<owner.CivicExpiresAt)
        {
            int factor=checked(10000-d.CivicFactorLossBp*owner.CivicLevel);
            production=Factor(production,factor,BreakdownKinds.CrisisCivic,d.Id);research=Factor(research,factor,BreakdownKinds.CrisisCivic,d.Id);
        }
        var growthEarned=result.GrowthEarned;var growth=result.Growth;
        if(ill||quarantine)
        {
            growthEarned=Quantity.From(result.GrowthEarned.Lines.Add(new(ill?BreakdownKinds.CrisisIllness:BreakdownKinds.CrisisQuarantine,d.Id,null,-result.GrowthEarned.Total)));
            growth=EconomyCalculator.ApplyGrowth(colony,result.Growth.Capacity,0);
        }
        return result with{NetProduction=production,NetResearch=research,GrowthEarned=growthEarned,Growth=growth};
    }
    public static CureAllocation SplitResearch(ContentSet content,GameState state,EmpireId owner,long newResearch)
    {
        if(newResearch<0)throw new ArgumentOutOfRangeException(nameof(newResearch));
        if(!Rules.HasCrises(state.RulesVersion)||state.Crisis is not{Enabled:true}||state.FindEmpire(owner)?.Crisis?.Episode is not {} e)return new(0,0,0,0,newResearch,false);
        long requested=Scale(newResearch,checked(e.ResearchShare*100));long remaining=checked(e.CureCost-e.CureProgress);
        long diverted=Math.Min(requested,remaining);
        return new(e.CureProgress,e.CureCost,requested,diverted,checked(newResearch-diverted),diverted==remaining);
    }
    private static Quantity Factor(Quantity quantity,int factor,string kind,string family)
    {
        long scaled=Scale(quantity.Total,factor);
        var division=DivisionDetails.Divide(checked((Int128)quantity.Total*factor),10000);
        return Quantity.From(quantity.Lines.Add(new(kind,family,null,checked(scaled-quantity.Total),division)));
    }
}
