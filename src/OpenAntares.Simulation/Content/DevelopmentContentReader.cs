using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Development;
namespace OpenAntares.Simulation.Content;
public static partial class ContentLoader
{
    private sealed partial class Builder
    {
        private readonly SortedDictionary<string,(GovernorTemplateDefinition Value,string File)> _governorTemplates=new(StringComparer.Ordinal);
        private GovernorTemplateDefinition ReadGovernorTemplate(EntryReader r,string id)
        {
            string name=r.RequiredString("name");
            var foundations=r.RequiredStringArray("foundation_ids");var development=r.RequiredStringArray("development_ids");
            string mode=r.RequiredString("support_target");
            if(mode is not ("minimum" or "capped_growth"))r.Error("support_target","Expected minimum or capped_growth.");
            int production=r.RequiredInt("production_weight",0,100),research=r.RequiredInt("research_weight",0,100);
            return new(id,name,foundations,development,mode=="capped_growth"?SupportTargetMode.CappedGrowth:SupportTargetMode.Minimum,production,research);
        }
        private void CheckDevelopment()
        {
            if(requireDevelopment&&_governorTemplates.Count==0)_errors.Add(new(AllFiles,null,"governor_templates","Development rules require explicit governor defaults."));
            foreach(var (id,item) in _governorTemplates)
            {
                var t=item.Value;
                if(string.IsNullOrWhiteSpace(t.Name)||t.Name.Length>80)_errors.Add(new(item.File,id,"name","Expected 1..80 characters."));
                if(t.ProductionWeight+t.ResearchWeight==0)_errors.Add(new(item.File,id,"production_weight","Total weights must be positive."));
                foreach(var pair in new[]{("foundation_ids",t.FoundationIds),("development_ids",t.DevelopmentIds)}) {
                    if(pair.Item2.Length>32)_errors.Add(new(item.File,id,pair.Item1,"At most32 building IDs."));
                    foreach(string building in pair.Item2)if(!_buildings.ContainsKey(building))_errors.Add(new(item.File,id,pair.Item1,$"Unknown building '{building}'."));
                }
                if(t.FoundationIds.Concat(t.DevelopmentIds).Distinct(StringComparer.Ordinal).Count()!=t.FoundationIds.Length+t.DevelopmentIds.Length)_errors.Add(new(item.File,id,"development_ids","Duplicate building preferences."));
            }
        }
    }
}
