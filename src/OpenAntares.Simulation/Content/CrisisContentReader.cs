using System;
using System.Collections.Generic;
namespace OpenAntares.Simulation.Content;
public static partial class ContentLoader
{
    private sealed partial class Builder
    {
        private readonly SortedDictionary<string,(PlagueDefinition Value,string File)> _plagues=new(StringComparer.Ordinal);
        private PlagueDefinition ReadPlague(EntryReader r,string id)
        {
            var d=new PlagueDefinition(id,r.RequiredString("name"),
                r.RequiredInt("first_check_turn",1,10000),
                r.RequiredInt("check_interval",1,10000),
                r.RequiredInt("check_chance_percent",0,100),
                r.RequiredInt("cooldown_turns",1,10000),
                r.RequiredInt("grace_turns",1,10000),
                r.RequiredInt("recovery_intervals",1,10000),
                r.RequiredInt("initial_severity",1,100),
                r.RequiredInt("severity_cap",1,100),
                r.RequiredInt("open_severity_step",0,100),
                r.RequiredInt("quarantined_severity_step",0,100),
                r.RequiredInt("mortality_threshold",1,100),
                r.RequiredInt("mortality_units",1,10000),
                r.RequiredInt("illness_factor_bp",0,10000),
                r.RequiredInt("quarantine_factor_bp",0,10000),
                r.RequiredLong("cure_base",0,long.MaxValue),
                r.RequiredLong("cure_per_population",0,long.MaxValue),
                r.RequiredInt("immunity_turns",1,10000),
                r.RequiredInt("purge_numerator",1,10000),
                r.RequiredInt("purge_denominator",1,10000),
                r.RequiredInt("civic_factor_loss_bp",0,10000),
                r.RequiredInt("civic_max_level",1,100),
                r.RequiredInt("civic_turns",1,10000));
            if(d.Name.Length>80)r.Error("name","At most80 characters.");
            if(d.InitialSeverity>d.SeverityCap)r.Error("initial_severity","Must not exceed severity_cap.");
            if(d.MortalityThreshold>d.SeverityCap)r.Error("mortality_threshold","Must not exceed severity_cap.");
            if(d.PurgeNumerator>d.PurgeDenominator)r.Error("purge_numerator","Must not exceed denominator.");
            if((long)d.CivicMaxLevel*d.CivicFactorLossBp>10000)r.Error("civic_factor_loss_bp","Maximum civic reduction exceeds100%.");
            Int128 maxCost=(Int128)d.CureBase+(Int128)d.CurePerPopulation*int.MaxValue;
            if(maxCost<=0||maxCost>long.MaxValue)r.Error("cure_per_population","Combined positive cost must fit for maximum population.");
            return d;
        }
        private void CheckCrises()
        {
            if((requireCrises||_plagues.Count>0)&&_plagues.Count!=1)_errors.Add(new(AllFiles,null,"plagues","Crisis rules require exactly one supported plague family."));
        }
    }
}
