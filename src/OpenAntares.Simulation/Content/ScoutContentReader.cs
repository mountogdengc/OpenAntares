using System.Text.Json;

namespace OpenAntares.Simulation.Content;

public static partial class ContentLoader
{
    private sealed partial class Builder
    {
        private (AmebaDefinition Value,string File)? _spaceAmeba;

        private AmebaDefinition? ReadAmeba(JsonElement element,string file)
        {
            if(EntryReader.Create(element,file,null,"space_ameba",_errors) is not {} r)return null;
            string id=r.RequiredString("id"),name=r.RequiredString("name");
            if(id.Length>80)r.Error("id","At most80 characters.");
            if(name.Length>80)r.Error("name","At most80 characters.");
            var d=new AmebaDefinition(id,name,r.RequiredInt("pursuit_speed",1,int.MaxValue),ReadProbability(r,"late_escape"),ReadProbability(r,"study"),r.RequiredLong("research_reward",1,long.MaxValue));
            r.RejectUnknownFields();return d;
        }

        private static ProbabilityRule ReadProbability(EntryReader parent,string field)
        {
            if(parent.RequiredObject(field) is not {} r)return new(0,0,0,0);
            var p=new ProbabilityRule(r.RequiredInt("base_percent",0,100),r.RequiredInt("relative_speed_coefficient",0,100),r.RequiredInt("minimum_percent",0,100),r.RequiredInt("maximum_percent",0,100));
            if(p.MinimumPercent>p.MaximumPercent)r.Error("minimum_percent","Must not exceed maximum_percent.");
            r.RejectUnknownFields();return p;
        }

        private void CheckScouting()
        {
            if(requireScouting&&_spaceAmeba is null)_errors.Add(new(AllFiles,null,"space_ameba","Scouting rules require explicit embedded ameba content, including Off."));
        }
    }
}
