using System;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Random;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Saves;

/// <summary>Explicit rules6 codec. Legacy readers leave injected fields unconsumed for rejection.</summary>
internal static class CrisisSaveData
{
    internal static void WriteGame(Utf8JsonWriter w,GameCrisisState? c,int rules)
    {
        if(!Rules.HasCrises(rules))return;
        w.WriteStartObject("crisis");w.WriteBoolean("enabled",c!.Enabled);w.WriteEndObject();
    }
    internal static GameCrisisState? ReadGame(EntryReader parent,int rules)
    {
        if(!Rules.HasCrises(rules)||parent.RequiredObject("crisis") is not {} r)return null;
        var c=new GameCrisisState{Enabled=r.RequiredBool("enabled")};r.RejectUnknownFields();return c;
    }
    internal static void WriteEmpire(Utf8JsonWriter w,EmpireCrisisState? value,int rules)
    {
        if(!Rules.HasCrises(rules))return;var c=value!;
        w.WriteStartObject("crisis");w.WriteStartObject("random");w.WriteNumber("state",c.Random.State);w.WriteNumber("increment",c.Random.Increment);w.WriteEndObject();
        w.WriteNumber("next_episode_number",c.NextEpisodeNumber);w.WriteNumber("next_eligible_turn",c.NextEligibleTurn);
        w.WritePropertyName("episode");
        if(c.Episode is {} e)
        {
            w.WriteStartObject();w.WriteNumber("id",e.Id);w.WriteString("family_id",e.FamilyId);Location(w,"source",e.Source);
            w.WriteNumber("source_population",e.SourcePopulation);w.WriteNumber("cure_cost",e.CureCost);w.WriteNumber("cure_progress",e.CureProgress);w.WriteNumber("research_share",e.ResearchShare);w.WriteBoolean("research_override",e.ResearchOverride);w.WriteEndObject();
        }
        else w.WriteNullValue();
        w.WriteString("authority",c.Authority switch{CrisisAuthority.Manual=>"manual",CrisisAuthority.Contain=>"contain",CrisisAuthority.ContainAndResearch=>"contain_and_research",_=>throw new ArgumentOutOfRangeException(nameof(value))});
        w.WriteNumber("authority_research_share",c.AuthorityResearchShare);w.WriteBoolean("research_policy_paused",c.ResearchPolicyPaused);w.WriteBoolean("require_acknowledgement",c.RequireAcknowledgement);
        w.WriteNumber("civic_level",c.CivicLevel);w.WriteNumber("civic_expires_at",c.CivicExpiresAt);
        w.WriteStartArray("pending_digests");foreach(var d in c.PendingDigests)Digest(w,d);w.WriteEndArray();
        w.WriteStartArray("completed_digests");foreach(var d in c.CompletedDigests)Digest(w,d);w.WriteEndArray();
        w.WriteStartArray("pending_receipts");foreach(var r in c.PendingReceipts){w.WriteStartObject();Key(w,r.Key);w.WriteNumber("revision",r.Revision);w.WriteNumber("turn",r.Turn);Location(w,"location",r.Location);w.WriteString("reason",r.Reason);w.WriteEndObject();}w.WriteEndArray();
        w.WriteStartArray("lost_colony_locations");foreach(var l in c.LostColonyLocations)LocationValue(w,l);w.WriteEndArray();
        w.WriteEndObject();
    }
    internal static EmpireCrisisState? ReadEmpire(EntryReader parent,int rules)
    {
        if(!Rules.HasCrises(rules)||parent.RequiredObject("crisis") is not {} r)return null;
        var c=new EmpireCrisisState{NextEpisodeNumber=r.RequiredLong("next_episode_number",1,long.MaxValue-1),NextEligibleTurn=r.RequiredInt("next_eligible_turn",0,int.MaxValue),AuthorityResearchShare=r.RequiredInt("authority_research_share",25,100),ResearchPolicyPaused=r.RequiredBool("research_policy_paused"),RequireAcknowledgement=r.RequiredBool("require_acknowledgement"),CivicLevel=r.RequiredInt("civic_level",0,100),CivicExpiresAt=r.RequiredInt("civic_expires_at",0,int.MaxValue)};
        if(r.RequiredObject("random") is {} rng){c.Random=new Pcg32{State=rng.RequiredUInt64("state"),Increment=rng.RequiredUInt64("increment")};rng.RejectUnknownFields();}
        string authority=r.RequiredString("authority");
        c.Authority=authority switch{"manual"=>CrisisAuthority.Manual,"contain"=>CrisisAuthority.Contain,"contain_and_research"=>CrisisAuthority.ContainAndResearch,_=>CrisisAuthority.Manual};
        if(authority is not("manual" or "contain" or "contain_and_research"))r.Error("authority","Unknown crisis authority.");
        if(r.RequiredNullableObject("episode") is {} e)
        {
            c.Episode=new(){Id=e.RequiredLong("id",1,long.MaxValue-1),FamilyId=e.RequiredString("family_id"),Source=ReadLocation(e,"source"),SourcePopulation=e.RequiredInt("source_population",1,int.MaxValue),CureCost=e.RequiredLong("cure_cost",1,long.MaxValue),CureProgress=e.RequiredLong("cure_progress",0,long.MaxValue),ResearchShare=e.RequiredInt("research_share",0,100),ResearchOverride=e.RequiredBool("research_override")};
            e.RejectUnknownFields();
        }
        Items(r,"pending_digests",d=>c.PendingDigests.Add(ReadDigest(d)));Items(r,"completed_digests",d=>c.CompletedDigests.Add(ReadDigest(d)));
        Items(r,"pending_receipts",p=>c.PendingReceipts.Add(new(ReadKey(p),p.RequiredLong("revision",1,long.MaxValue-1),p.RequiredInt("turn",0,int.MaxValue),ReadLocation(p,"location"),p.RequiredString("reason"))));
        Items(r,"lost_colony_locations",l=>c.LostColonyLocations.Add(ReadLocationValue(l)));
        r.RejectUnknownFields();return c;
    }
    internal static void WriteColony(Utf8JsonWriter w,ColonyCrisisState? value,int rules)
    {
        if(!Rules.HasCrises(rules))return;var c=value!;
        w.WriteStartObject("crisis");w.WriteBoolean("quarantined",c.Quarantined);NullableLong(w,"release_episode",c.ReleaseEpisode);w.WriteBoolean("policy_paused",c.PolicyPaused);w.WriteString("immunity_family_id",c.ImmunityFamilyId);w.WriteNumber("immune_until_turn",c.ImmuneUntilTurn);
        w.WritePropertyName("case");
        if(c.Case is {} e)
        {
            w.WriteStartObject();w.WriteNumber("episode_id",e.EpisodeId);w.WriteString("stage",e.Stage==CrisisStage.Warning?"warning":"active");
            w.WriteNumber("grace_remaining",e.GraceRemaining);w.WriteNumber("active_age",e.ActiveAge);w.WriteNumber("severity",e.Severity);
            w.WriteNumber("attention_revision",e.AttentionRevision);w.WriteNumber("handled_revision",e.HandledRevision);w.WriteNumber("waived_revision",e.WaivedRevision);
            Location(w,"origin",e.Origin);NullableLong(w,"source_vessel",e.SourceVessel?.Value);w.WriteEndObject();
        }
        else w.WriteNullValue();
        w.WriteEndObject();
    }
    internal static ColonyCrisisState? ReadColony(EntryReader parent,int rules)
    {
        if(!Rules.HasCrises(rules)||parent.RequiredObject("crisis") is not {} r)return null;
        var c=new ColonyCrisisState{Quarantined=r.RequiredBool("quarantined"),ReleaseEpisode=NullableLong(r,"release_episode"),PolicyPaused=r.RequiredBool("policy_paused"),ImmunityFamilyId=r.RequiredNullableString("immunity_family_id"),ImmuneUntilTurn=r.RequiredInt("immune_until_turn",0,int.MaxValue)};
        if(r.RequiredNullableObject("case") is {} e)
        {
            string stage=e.RequiredString("stage");if(stage is not("warning" or "active"))e.Error("stage","Expected warning or active.");
            int? source=e.RequiredNullableInt("source_vessel",1,int.MaxValue);
            c.Case=new(){EpisodeId=e.RequiredLong("episode_id",1,long.MaxValue-1),Stage=stage=="active"?CrisisStage.Active:CrisisStage.Warning,GraceRemaining=e.RequiredInt("grace_remaining",0,10000),ActiveAge=e.RequiredInt("active_age",0,10000),Severity=e.RequiredInt("severity",0,100),AttentionRevision=e.RequiredLong("attention_revision",1,long.MaxValue-1),HandledRevision=e.RequiredLong("handled_revision",0,long.MaxValue-1),WaivedRevision=e.RequiredLong("waived_revision",0,long.MaxValue-1),Origin=ReadLocation(e,"origin"),SourceVessel=source is {} id?new VesselId(id):null};
            e.RejectUnknownFields();
        }
        r.RejectUnknownFields();return c;
    }
    internal static void WriteVessel(Utf8JsonWriter w,VesselCrisisState? value,int rules)
    {
        if(!Rules.HasCrises(rules))return;var c=value!;w.WriteStartObject("crisis");NullableLong(w,"carrier_episode",c.CarrierEpisode);NullableLong(w,"inspection_due_turn",c.InspectionDueTurn);w.WriteEndObject();
    }
    internal static VesselCrisisState? ReadVessel(EntryReader parent,int rules)
    {
        if(!Rules.HasCrises(rules)||parent.RequiredObject("crisis") is not {} r)return null;
        var c=new VesselCrisisState{CarrierEpisode=NullableLong(r,"carrier_episode"),InspectionDueTurn=r.RequiredNullableInt("inspection_due_turn",0,int.MaxValue)};r.RejectUnknownFields();return c;
    }
    private static void NullableLong(Utf8JsonWriter w,string field,long? value){if(value is {} n)w.WriteNumber(field,n);else w.WriteNull(field);}
    private static long? NullableLong(EntryReader r,string field)
    {
        var n=r.RequiredNullableUInt64(field);if(n is null)return null;
        if(n<1||n>long.MaxValue-1){r.Error(field,"Expected positive local identifier below Int64.MaxValue, or null.");return null;}
        return (long)n.Value;
    }
    private static void Location(Utf8JsonWriter w,string field,CrisisLocation l){w.WritePropertyName(field);LocationValue(w,l);}
    private static void LocationValue(Utf8JsonWriter w,CrisisLocation l){w.WriteStartObject();w.WriteNumber("colony",l.Colony.Value);w.WriteNumber("planet",l.Planet.Value);w.WriteNumber("star",l.Star.Value);w.WriteString("name",l.Name);w.WriteEndObject();}
    private static CrisisLocation ReadLocation(EntryReader r,string field) => r.RequiredObject(field) is {} l?ReadLocationValue(l):new(default,default,default,string.Empty);
    private static CrisisLocation ReadLocationValue(EntryReader l){var value=new CrisisLocation(new(l.RequiredInt("colony",1,int.MaxValue)),new(l.RequiredInt("planet",1,int.MaxValue)),new(l.RequiredInt("star",1,int.MaxValue)),l.RequiredString("name"));l.RejectUnknownFields();return value;}
    private static void Key(Utf8JsonWriter w,CrisisKey key){w.WriteStartObject("key");w.WriteNumber("owner",key.Owner.Value);w.WriteNumber("episode",key.Episode);w.WriteNumber("colony",key.Colony.Value);w.WriteEndObject();}
    private static CrisisKey ReadKey(EntryReader parent){if(parent.RequiredObject("key") is not {} r)return new(default,0,default);var key=new CrisisKey(new(r.RequiredInt("owner",1,int.MaxValue)),r.RequiredLong("episode",1,long.MaxValue-1),new(r.RequiredInt("colony",1,int.MaxValue)));r.RejectUnknownFields();return key;}
    private static void Digest(Utf8JsonWriter w,CrisisDigest d)
    {
        w.WriteStartObject();w.WriteNumber("turn",d.Turn);w.WriteStartArray("entries");
        foreach(var e in d.Entries){w.WriteStartObject();Key(w,e.Key);Location(w,"location",e.Location);w.WriteString("kind",e.Kind);w.WriteNumber("revision",e.Revision);w.WriteString("disposition",e.Disposition);w.WriteNumber("research_diverted",e.ResearchDiverted);w.WriteNumber("population_lost",e.PopulationLost);w.WriteString("reason",e.Reason);w.WriteEndObject();}
        w.WriteEndArray();w.WriteEndObject();
    }
    private static CrisisDigest ReadDigest(EntryReader r)
    {
        int turn=r.RequiredInt("turn",0,int.MaxValue);var entries=ImmutableArray.CreateBuilder<CrisisDigestEntry>();
        Items(r,"entries",e=>entries.Add(new(ReadKey(e),ReadLocation(e,"location"),e.RequiredString("kind"),e.RequiredLong("revision",1,long.MaxValue-1),e.RequiredString("disposition"),e.RequiredLong("research_diverted",0,long.MaxValue),e.RequiredInt("population_lost",0,int.MaxValue),e.RequiredString("reason"))));
        r.RejectUnknownFields();return new(turn,entries.ToImmutable());
    }
    private static void Items(EntryReader r,string field,Action<EntryReader> action)
    {
        if(r.RequiredArray(field) is not {} array)return;int index=0;
        foreach(var element in array.EnumerateArray()){if(r.Nested(element,$"{field}[{index}]") is {} item){action(item);item.RejectUnknownFields();}index++;}
    }
}
