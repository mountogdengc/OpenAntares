using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Appearance;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Saves;
internal static class DevelopmentSaveData
{
    internal static void WriteAppearance(Utf8JsonWriter w,PlanetAppearanceDescriptor? a,int rules)
    {
        if(!Rules.HasDevelopment(rules))return;
        w.WriteStartObject("appearance");w.WriteNumber("version",a!.Version);w.WriteNumber("variant",a.Variant);w.WriteNumber("surface_seed",a.SurfaceSeed);w.WriteEndObject();
    }
    internal static PlanetAppearanceDescriptor? ReadAppearance(EntryReader parent,int rules)
    {
        if(!Rules.HasDevelopment(rules))return null;var r=parent.RequiredObject("appearance");if(r is null)return null;
        var value=new PlanetAppearanceDescriptor(r.RequiredInt("version",1,1),r.RequiredInt("variant",0,7),r.RequiredUInt64("surface_seed"));r.RejectUnknownFields();return value;
    }
    internal static void WriteEmpire(Utf8JsonWriter w,EmpireDevelopmentState? d,int rules)
    {
        if(!Rules.HasDevelopment(rules))return;
        w.WriteStartObject("development");w.WriteNumber("next_template_number",d!.NextTemplateNumber);w.WriteStartArray("custom_templates");foreach(var t in d.CustomTemplates)Snapshot(w,t);w.WriteEndArray();
        if(d.NewColonyPlan is {} plan){w.WriteStartObject("new_colony_plan");w.WritePropertyName("template");Snapshot(w,plan.Template);w.WriteBoolean("workforce_enabled",plan.WorkforceEnabled);w.WriteBoolean("construction_enabled",plan.ConstructionEnabled);w.WriteEndObject();}else w.WriteNull("new_colony_plan");
        w.WritePropertyName("pending_report");Report(w,d.PendingReport);w.WriteStartArray("completed_reports");foreach(var r in d.CompletedReports)Report(w,r);w.WriteEndArray();w.WriteEndObject();
    }
    internal static EmpireDevelopmentState? ReadEmpire(EntryReader parent,int rules)
    {
        if(!Rules.HasDevelopment(rules))return null;var r=parent.RequiredObject("development");if(r is null)return null;
        var d=new EmpireDevelopmentState{NextTemplateNumber=r.RequiredInt("next_template_number",1,int.MaxValue)};
        Items(r,"custom_templates",t=>d.CustomTemplates.Add(ReadSnapshot(t)));
        if(r.RequiredNullableObject("new_colony_plan") is {} plan){var t=plan.RequiredObject("template");if(t is not null)d.NewColonyPlan=new(ReadSnapshot(t),plan.RequiredBool("workforce_enabled"),plan.RequiredBool("construction_enabled"));plan.RejectUnknownFields();}
        if(r.RequiredNullableObject("pending_report") is {} pending)d.PendingReport=ReadReport(pending);
        Items(r,"completed_reports",item=>d.CompletedReports.Add(ReadReport(item)));r.RejectUnknownFields();return d;
    }
    internal static void WriteColony(Utf8JsonWriter w,ColonyDevelopmentState? d,int rules)
    {
        if(!Rules.HasDevelopment(rules))return;w.WriteStartObject("development");w.WritePropertyName("template");if(d!.Template is {} t)Snapshot(w,t);else w.WriteNullValue();
        w.WriteBoolean("workforce_enabled",d.WorkforceEnabled);w.WriteBoolean("construction_enabled",d.ConstructionEnabled);w.WriteNumber("next_queue_token",d.NextQueueToken);NullableInt(w,"active_queue_token",d.ActiveQueueToken);w.WriteString("origin",Origin(d.Origin));
        w.WriteStartArray("queue");foreach(var entry in d.Queue){w.WriteStartObject();w.WriteNumber("token",entry.Token);Target(w,"target",entry.Target);w.WriteEndObject();}w.WriteEndArray();w.WriteEndObject();
    }
    internal static ColonyDevelopmentState? ReadColony(EntryReader parent,int rules)
    {
        if(!Rules.HasDevelopment(rules))return null;var r=parent.RequiredObject("development");if(r is null)return null;
        var d=new ColonyDevelopmentState{WorkforceEnabled=r.RequiredBool("workforce_enabled"),ConstructionEnabled=r.RequiredBool("construction_enabled"),NextQueueToken=r.RequiredInt("next_queue_token",1,int.MaxValue),ActiveQueueToken=r.RequiredNullableInt("active_queue_token",1,int.MaxValue)};
        string origin=r.RequiredString("origin");d.Origin=origin switch{"none"=>ProductionOrigin.None,"manual"=>ProductionOrigin.Manual,"queue"=>ProductionOrigin.Queue,"template"=>ProductionOrigin.Template,_=>ProductionOrigin.None};if(origin is not ("none" or "manual" or "queue" or "template"))r.Error("origin","Unknown production origin.");
        if(r.RequiredNullableObject("template") is {} t)d.Template=ReadSnapshot(t);
        Items(r,"queue",entry=>{int token=entry.RequiredInt("token",1,int.MaxValue);if(entry.RequiredObject("target") is {} target)d.Queue.Add(new(token,ReadTarget(target)));});r.RejectUnknownFields();return d;
    }
    private static void Snapshot(Utf8JsonWriter w,GovernorTemplateSnapshot t){w.WriteStartObject();w.WriteString("id",t.Id);w.WriteNumber("revision",t.Revision);w.WriteStartObject("definition");GovernorTemplateJson.WriteFields(w,t.Definition);w.WriteEndObject();w.WriteEndObject();}
    private static GovernorTemplateSnapshot ReadSnapshot(EntryReader r){string id=r.RequiredString("id");int revision=r.RequiredInt("revision",1,int.MaxValue);var definition=r.RequiredObject("definition");var d=definition is null?new GovernorTemplateDraft("",ImmutableArray<string>.Empty,ImmutableArray<string>.Empty,SupportTargetMode.Minimum,0,0):GovernorTemplateJson.Read(definition);r.RejectUnknownFields();return new(id,revision,d);}
    private static void Target(Utf8JsonWriter w,string field,ProductionTarget? t){if(t is null){w.WriteNull(field);return;}w.WriteStartObject(field);w.WriteString("kind",t.Kind);w.WriteString("content_id",t.ContentId);w.WriteEndObject();}
    private static ProductionTarget ReadTarget(EntryReader r){var t=new ProductionTarget(r.RequiredString("kind"),r.RequiredString("content_id"));r.RejectUnknownFields();return t;}
    private static ProductionTarget? NullableTarget(EntryReader r,string field)=>r.RequiredNullableObject(field) is {} t?ReadTarget(t):null;
    private static void WorkforceValue(Utf8JsonWriter w,string field,Workforce? f){if(f is not {} v){w.WriteNull(field);return;}w.WriteStartObject(field);w.WriteNumber("support",v.Support);w.WriteNumber("production",v.Production);w.WriteNumber("research",v.Research);w.WriteEndObject();}
    private static Workforce? ReadWorkforce(EntryReader r,string field){if(r.RequiredNullableObject(field) is not {} w)return null;var f=new Workforce(w.RequiredInt("support",0,int.MaxValue),w.RequiredInt("production",0,int.MaxValue),w.RequiredInt("research",0,int.MaxValue));w.RejectUnknownFields();return f;}
    private static void Report(Utf8JsonWriter w,GovernorTurnReport? report)
    {
        if(report is null){w.WriteNullValue();return;}w.WriteStartObject();w.WriteNumber("turn",report.Turn);w.WriteNumber("unchanged_count",report.UnchangedCount);w.WriteStartArray("entries");
        foreach(var e in report.Entries){w.WriteStartObject();w.WriteNumber("colony",e.Colony.Value);WorkforceValue(w,"before_workforce",e.BeforeWorkforce);WorkforceValue(w,"after_workforce",e.AfterWorkforce);Target(w,"before_target",e.BeforeTarget);Target(w,"after_target",e.AfterTarget);w.WriteBoolean("workforce_overridden",e.WorkforceOverridden);w.WriteBoolean("construction_overridden",e.ConstructionOverridden);w.WriteStartArray("issues");foreach(var i in e.Issues){w.WriteStartObject();w.WriteString("code",i.Code);w.WriteString("content_id",i.ContentId);w.WriteEndObject();}w.WriteEndArray();w.WriteEndObject();}
        w.WriteEndArray();w.WriteEndObject();
    }
    private static GovernorTurnReport ReadReport(EntryReader r)
    {
        int turn=r.RequiredInt("turn",0,int.MaxValue),unchanged=r.RequiredInt("unchanged_count",0,int.MaxValue);var entries=ImmutableArray.CreateBuilder<GovernorReportEntry>();
        Items(r,"entries",e=>{var id=new ColonyId(e.RequiredInt("colony",1,int.MaxValue));var before=ReadWorkforce(e,"before_workforce");var after=ReadWorkforce(e,"after_workforce");var beforeTarget=NullableTarget(e,"before_target");var afterTarget=NullableTarget(e,"after_target");bool wf=e.RequiredBool("workforce_overridden"),construction=e.RequiredBool("construction_overridden");var issues=ImmutableArray.CreateBuilder<GovernorIssue>();Items(e,"issues",i=>issues.Add(new(i.RequiredString("code"),i.RequiredNullableString("content_id"))));entries.Add(new(id,before,after,beforeTarget,afterTarget,issues.ToImmutable(),wf,construction));});
        r.RejectUnknownFields();return new(turn,entries.ToImmutable(),unchanged);
    }
    private static string Origin(ProductionOrigin origin)=>origin switch{ProductionOrigin.Manual=>"manual",ProductionOrigin.Queue=>"queue",ProductionOrigin.Template=>"template",_=>"none"};
    private static void NullableInt(Utf8JsonWriter w,string field,int? number){if(number is {} n)w.WriteNumber(field,n);else w.WriteNull(field);}
    private static void Items(EntryReader r,string field,Action<EntryReader> action){if(r.RequiredArray(field) is not {} array)return;int index=0;foreach(var element in array.EnumerateArray()){if(r.Nested(element,$"{field}[{index}]") is {} item){action(item);item.RejectUnknownFields();}index++;}}
}
