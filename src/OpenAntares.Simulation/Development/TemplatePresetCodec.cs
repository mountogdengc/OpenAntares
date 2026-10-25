using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using OpenAntares.Simulation.Content;
namespace OpenAntares.Simulation.Development;
public sealed record TemplatePresetResult(ImmutableArray<GovernorTemplateDraft> Templates,ImmutableArray<string> Errors){public bool Succeeded=>Errors.IsEmpty;}
public static class TemplatePresetCodec
{
    public static string Serialize(ImmutableArray<GovernorTemplateDraft> templates)
    {
        if(templates.IsDefault||templates.Length>64)throw new ArgumentException("Presets support0..64 templates.");
        using var buffer=new MemoryStream();using(var w=new Utf8JsonWriter(buffer,new(){Indented=true})) {
            w.WriteStartObject();w.WriteString("format","OpenAntaresGovernorTemplates");w.WriteNumber("version",1);w.WriteStartArray("templates");
            foreach(var d in templates){w.WriteStartObject();GovernorTemplateJson.WriteFields(w,d);w.WriteEndObject();}w.WriteEndArray();w.WriteEndObject();
        }
        string result=Encoding.UTF8.GetString(buffer.ToArray());if(Encoding.UTF8.GetByteCount(result)>256*1024)throw new ArgumentException("Preset exceeds256KiB.");return result;
    }
    public static TemplatePresetResult Deserialize(string json,ContentSet content)
    {
        if(Encoding.UTF8.GetByteCount(json)>256*1024)return Failed("Preset exceeds256KiB UTF8.");
        var errors=new List<ContentError>();var drafts=ImmutableArray.CreateBuilder<GovernorTemplateDraft>();
        try {
            using var document=JsonDocument.Parse(json);var root=EntryReader.Create(document.RootElement,"preset",null,null,errors);
            if(root is null)return Failed("Preset must be an object.");
            if(root.RequiredString("format")!="OpenAntaresGovernorTemplates")root.Error("format","Unknown preset format.");
            root.RequiredInt("version",1,1);
            if(root.RequiredArray("templates") is {} array) {
                if(array.GetArrayLength()>64)root.Error("templates","At most64 templates.");
                else {int index=0;foreach(var item in array.EnumerateArray()) {
                    if(root.Nested(item,$"templates[{index}]") is {} r){var draft=GovernorTemplateJson.Read(r);foreach(var error in DevelopmentStateValidator.DraftErrors(content,draft))r.Error(null,error);drafts.Add(draft);}index++;
                }}
            }
            root.RejectUnknownFields();
            return errors.Count==0?new(drafts.ToImmutable(),ImmutableArray<string>.Empty):new(ImmutableArray<GovernorTemplateDraft>.Empty,errors.Select(e=>e.ToString()).ToImmutableArray());
        }catch(JsonException){return Failed("Invalid preset JSON.");}
    }
    private static TemplatePresetResult Failed(string message)=>new(ImmutableArray<GovernorTemplateDraft>.Empty,ImmutableArray.Create(message));
}
internal static class GovernorTemplateJson
{
    internal static void WriteFields(Utf8JsonWriter w,GovernorTemplateDraft d)
    {
        w.WriteString("name",d.Name);Strings(w,"foundation_ids",d.FoundationIds);Strings(w,"development_ids",d.DevelopmentIds);w.WriteString("support_target",d.SupportTarget==SupportTargetMode.Minimum?"minimum":"capped_growth");w.WriteNumber("production_weight",d.ProductionWeight);w.WriteNumber("research_weight",d.ResearchWeight);
    }
    internal static GovernorTemplateDraft Read(EntryReader r)
    {
        string name=r.RequiredString("name");var foundations=r.RequiredStringArray("foundation_ids");var development=r.RequiredStringArray("development_ids");string mode=r.RequiredString("support_target");
        if(mode is not ("minimum" or "capped_growth"))r.Error("support_target","Expected minimum or capped_growth.");
        var result=new GovernorTemplateDraft(name,foundations,development,mode=="capped_growth"?SupportTargetMode.CappedGrowth:SupportTargetMode.Minimum,r.RequiredInt("production_weight",0,100),r.RequiredInt("research_weight",0,100));r.RejectUnknownFields();return result;
    }
    private static void Strings(Utf8JsonWriter w,string field,ImmutableArray<string> strings){w.WriteStartArray(field);foreach(string value in strings)w.WriteStringValue(value);w.WriteEndArray();}
}
