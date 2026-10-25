using System;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Random;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Saves;

/// <summary>Explicit rules7 layout. Older readers leave every new field unconsumed.</summary>
internal static class ScoutSaveData
{
    internal static void WriteGame(Utf8JsonWriter w,ScoutGameState? value,int rules)
    {
        if(!Rules.HasScouting(rules))return;var g=value!;w.WriteStartObject("scouting");w.WriteBoolean("enabled",g.Enabled);Random(w,"placement_random",g.PlacementRandom);w.WriteNumber("selection_count",g.SelectionCount);w.WritePropertyName("habitat");
        if(g.Habitat is {} h){w.WriteStartObject();w.WriteNumber("id",h.Id);w.WriteString("definition",h.DefinitionId);w.WriteNumber("star",h.Star.Value);w.WriteEndObject();}else w.WriteNullValue();w.WriteEndObject();
    }
    internal static ScoutGameState? ReadGame(EntryReader parent,int rules)
    {
        if(!Rules.HasScouting(rules)||parent.RequiredObject("scouting") is not {} r)return null;
        var g=new ScoutGameState{Enabled=r.RequiredBool("enabled"),PlacementRandom=ReadRandom(r,"placement_random"),SelectionCount=r.RequiredLong("selection_count",0,1)};
        if(r.RequiredNullableObject("habitat") is {} h){g.Habitat=new(h.RequiredInt("id",1,1),h.RequiredString("definition"),new(h.RequiredInt("star",1,int.MaxValue)));h.RejectUnknownFields();}r.RejectUnknownFields();return g;
    }
    internal static void WriteEmpire(Utf8JsonWriter w,EmpireScoutState? value,int rules)
    {
        if(!Rules.HasScouting(rules))return;var e=value!;w.WriteStartObject("scouting");Random(w,"risk_random",e.RiskRandom);w.WriteNumber("risk_roll_count",e.RiskRollCount);w.WriteNumber("next_encounter_number",e.NextEncounterNumber);
        w.WriteStartArray("known_habitats");foreach(var h in e.KnownHabitats){w.WriteStartObject();w.WriteNumber("habitat",h.HabitatId);w.WriteNumber("star",h.Star.Value);w.WriteNumber("observed_turn",h.ObservedTurn);w.WriteEndObject();}w.WriteEndArray();
        w.WriteStartArray("successful_studies");foreach(var c in e.SuccessfulStudies){w.WriteStartObject();w.WriteNumber("habitat",c.HabitatId);w.WriteNumber("turn",c.Turn);w.WriteNumber("vessel",c.Vessel.Value);w.WriteEndObject();}w.WriteEndArray();
        w.WriteStartArray("receipts");foreach(var receipt in e.Receipts){w.WriteStartObject();w.WriteNumber("acknowledged_turn",receipt.AcknowledgedTurn);w.WritePropertyName("token");Token(w,receipt.Token);w.WriteEndObject();}w.WriteEndArray();
        w.WritePropertyName("pending_digest");if(e.PendingDigest is {} pending)Digest(w,pending);else w.WriteNullValue();
        w.WriteStartArray("completed_digests");foreach(var digest in e.CompletedDigests)Digest(w,digest);w.WriteEndArray();
        w.WriteStartArray("retired_vessels");foreach(var v in e.RetiredVessels){w.WriteStartObject();w.WriteNumber("id",v.Id.Value);w.WriteNumber("owner",v.Owner.Value);w.WriteString("role",v.RoleId);w.WriteNumber("cargo",v.Cargo);w.WriteNumber("star",v.Star.Value);w.WriteNumber("retired_turn",v.RetiredTurn);w.WriteEndObject();}w.WriteEndArray();w.WriteEndObject();
    }
    internal static EmpireScoutState? ReadEmpire(EntryReader parent,int rules)
    {
        if(!Rules.HasScouting(rules)||parent.RequiredObject("scouting") is not {} r)return null;
        var e=new EmpireScoutState{RiskRandom=ReadRandom(r,"risk_random"),RiskRollCount=r.RequiredLong("risk_roll_count",0,long.MaxValue-1),NextEncounterNumber=r.RequiredLong("next_encounter_number",1,long.MaxValue-1)};
        Items(r,"known_habitats",h=>e.KnownHabitats.Add(new(h.RequiredInt("habitat",1,1),new(h.RequiredInt("star",1,int.MaxValue)),h.RequiredInt("observed_turn",0,int.MaxValue))));
        Items(r,"successful_studies",c=>e.SuccessfulStudies.Add(new(c.RequiredInt("habitat",1,1),c.RequiredInt("turn",0,int.MaxValue),new(c.RequiredInt("vessel",1,int.MaxValue)))));
        Items(r,"receipts",receipt=>{int turn=receipt.RequiredInt("acknowledged_turn",0,int.MaxValue);if(receipt.RequiredObject("token") is {} token){e.Receipts.Add(new(ReadToken(token),turn));token.RejectUnknownFields();}});
        if(r.RequiredNullableObject("pending_digest") is {} pending){e.PendingDigest=ReadDigest(pending);pending.RejectUnknownFields();}
        Items(r,"completed_digests",d=>e.CompletedDigests.Add(ReadDigest(d)));
        Items(r,"retired_vessels",v=>e.RetiredVessels.Add(new(new(v.RequiredInt("id",1,int.MaxValue)),new(v.RequiredInt("owner",1,int.MaxValue)),v.RequiredString("role"),v.RequiredInt("cargo",0,int.MaxValue),new(v.RequiredInt("star",1,int.MaxValue)),v.RequiredInt("retired_turn",0,int.MaxValue))));r.RejectUnknownFields();return e;
    }
    internal static void WriteVessel(Utf8JsonWriter w,VesselState vessel,int rules)
    {
        if(!Rules.HasScouting(rules))return;
        w.WritePropertyName("scout");if(vessel.Scout is {} s)
        {
            w.WriteStartObject();w.WriteString("posture",s.Posture switch{ScoutPosture.Observe=>"observe",ScoutPosture.AvoidEncounters=>"avoid_encounters",_=>throw new ArgumentOutOfRangeException(nameof(vessel))});w.WriteBoolean("withdraw_from_ameba",s.WithdrawFromAmeba);w.WriteString("last_wait_signature",s.LastWaitSignature);w.WritePropertyName("approach_acceptance");
            if(s.ApproachAcceptance is {} approach){w.WriteStartObject();w.WriteNumber("destination",approach.Destination.Value);w.WriteNumber("habitat",approach.HabitatId);w.WriteEndObject();}else w.WriteNullValue();w.WriteEndObject();
        }else w.WriteNullValue();
        w.WritePropertyName("ameba_encounter");if(vessel.AmebaEncounter is {} a)
        {
            w.WriteStartObject();w.WriteNumber("number",a.Number);w.WriteNumber("habitat",a.HabitatId);w.WriteNumber("arrival_turn",a.ArrivalTurn);w.WriteString("stage",StageName(a.Stage));w.WriteNumber("revision",a.Revision);w.WritePropertyName("risk_order");
            if(a.RiskOrder is {} risk){w.WriteStartObject();w.WriteString("action",risk.Action switch{AmebaRiskAction.Study=>"study",AmebaRiskAction.Remain=>"remain",_=>throw new ArgumentOutOfRangeException(nameof(vessel))});w.WriteNumber("turn",risk.Turn);w.WriteNumber("revision",risk.Revision);w.WriteEndObject();}else w.WriteNullValue();w.WriteEndObject();
        }else w.WriteNullValue();
    }
    internal static void ReadVessel(EntryReader parent,VesselState vessel,int rules)
    {
        if(!Rules.HasScouting(rules))return;
        if(parent.RequiredNullableObject("scout") is {} s)
        {
            string posture=s.RequiredString("posture");if(posture is not("observe" or "avoid_encounters"))s.Error("posture","Unknown scout posture.");
            var scout=new ScoutVesselState{Posture=posture=="avoid_encounters"?ScoutPosture.AvoidEncounters:ScoutPosture.Observe,WithdrawFromAmeba=s.RequiredBool("withdraw_from_ameba"),LastWaitSignature=s.RequiredNullableString("last_wait_signature")};
            if(s.RequiredNullableObject("approach_acceptance") is {} approach){scout.ApproachAcceptance=new(new(approach.RequiredInt("destination",1,int.MaxValue)),approach.RequiredInt("habitat",1,1));approach.RejectUnknownFields();}s.RejectUnknownFields();vessel.Scout=scout;
        }
        if(parent.RequiredNullableObject("ameba_encounter") is {} a)
        {
            var encounter=new AmebaEncounterState{Number=a.RequiredLong("number",1,long.MaxValue-1),HabitatId=a.RequiredInt("habitat",1,1),ArrivalTurn=a.RequiredInt("arrival_turn",0,int.MaxValue),Stage=ReadStage(a,"stage"),Revision=a.RequiredLong("revision",1,long.MaxValue-1)};
            if(a.RequiredNullableObject("risk_order") is {} risk)
            {
                string action=risk.RequiredString("action");if(action is not("study" or "remain"))risk.Error("action","Unknown risk action.");encounter.RiskOrder=new(action=="study"?AmebaRiskAction.Study:AmebaRiskAction.Remain,risk.RequiredInt("turn",0,int.MaxValue),risk.RequiredLong("revision",1,long.MaxValue-1));risk.RejectUnknownFields();
            }
            a.RejectUnknownFields();vessel.AmebaEncounter=encounter;
        }
    }
    private static void Random(Utf8JsonWriter w,string name,Pcg32? random)
    {w.WritePropertyName(name);if(random is null){w.WriteNullValue();return;}w.WriteStartObject();w.WriteNumber("state",random.State);w.WriteNumber("increment",random.Increment);w.WriteEndObject();}
    private static Pcg32? ReadRandom(EntryReader parent,string name)
    {if(parent.RequiredNullableObject(name) is not {} r)return null;var rng=new Pcg32{State=r.RequiredUInt64("state"),Increment=r.RequiredUInt64("increment")};r.RejectUnknownFields();return rng;}
    private static void Number(Utf8JsonWriter w,string name,long? value){if(value is {} n)w.WriteNumber(name,n);else w.WriteNull(name);}
    private static long? ReadLong(EntryReader r,string name)
    {var n=r.RequiredNullableUInt64(name);if(n is null)return null;if(n>long.MaxValue-1){r.Error(name,"Counter exceeds supported bounds.");return null;}return (long)n;}
    private static StarId? ReadStar(EntryReader r,string name)=>r.RequiredNullableInt(name,1,int.MaxValue) is {} value?new(value):null;
    private static string StageName(AmebaStage stage)=>stage switch{AmebaStage.Warning=>"warning",AmebaStage.Exposed=>"exposed",_=>throw new ArgumentOutOfRangeException(nameof(stage))};
    private static AmebaStage ReadStage(EntryReader r,string name){string stage=r.RequiredString(name);if(stage is not("warning" or "exposed"))r.Error(name,"Unknown ameba stage.");return stage=="exposed"?AmebaStage.Exposed:AmebaStage.Warning;}
    private static void Token(Utf8JsonWriter w,AmebaAttentionToken t)
    {w.WriteStartObject();w.WriteNumber("vessel",t.Vessel.Value);w.WriteNumber("encounter",t.EncounterNumber);w.WriteNumber("revision",t.Revision);w.WriteString("stage",StageName(t.Stage));w.WriteString("action",t.Action);Number(w,"destination",t.Destination?.Value);w.WriteNumber("speed",t.Speed);w.WriteNumber("cargo",t.Cargo);w.WriteNumber("percent",t.Percent);w.WriteNumber("reward",t.Reward);w.WriteEndObject();}
    private static AmebaAttentionToken ReadToken(EntryReader r)
    {
        var id=new VesselId(r.RequiredInt("vessel",1,int.MaxValue));long encounter=r.RequiredLong("encounter",1,long.MaxValue-1),revision=r.RequiredLong("revision",1,long.MaxValue-1);var stage=ReadStage(r,"stage");string action=r.RequiredString("action");if(action is not("depart" or "study" or "remain" or "response_required"))r.Error("action","Unknown effective action.");
        return new(id,encounter,revision,stage,action,ReadStar(r,"destination"),r.RequiredInt("speed",1,int.MaxValue),r.RequiredInt("cargo",0,int.MaxValue),r.RequiredInt("percent",0,100),r.RequiredLong("reward",0,long.MaxValue));
    }
    private static void Digest(Utf8JsonWriter w,ScoutTurnDigest d)
    {
        w.WriteStartObject();w.WriteNumber("turn",d.Turn);w.WriteStartArray("entries");foreach(var e in d.Entries)
        {w.WriteStartObject();w.WriteNumber("owner",e.Owner.Value);w.WriteNumber("turn",e.Turn);w.WriteNumber("vessel",e.Vessel.Value);w.WriteString("role",e.RoleId);w.WriteNumber("cargo",e.Cargo);w.WriteNumber("star",e.Star.Value);w.WriteString("kind",e.Kind);w.WriteString("reason",e.Reason);Number(w,"destination",e.Destination?.Value);Number(w,"percent",e.Percent);w.WriteNumber("research_award",e.ResearchAward);Number(w,"encounter",e.EncounterNumber);Number(w,"revision",e.Revision);w.WriteEndObject();}w.WriteEndArray();w.WriteEndObject();
    }
    private static ScoutTurnDigest ReadDigest(EntryReader r)
    {
        int turn=r.RequiredInt("turn",0,int.MaxValue);var entries=ImmutableArray.CreateBuilder<ScoutOrderEvent>();Items(r,"entries",e=>entries.Add(new(new(e.RequiredInt("owner",1,int.MaxValue)),e.RequiredInt("turn",0,int.MaxValue),new(e.RequiredInt("vessel",1,int.MaxValue)),e.RequiredString("role"),e.RequiredInt("cargo",0,int.MaxValue),new(e.RequiredInt("star",1,int.MaxValue)),e.RequiredString("kind"),e.RequiredString("reason"),ReadStar(e,"destination"),e.RequiredNullableInt("percent",0,100),e.RequiredLong("research_award",0,long.MaxValue),ReadLong(e,"encounter"),ReadLong(e,"revision"))));return new(turn,entries.ToImmutable());
    }
    private static void Items(EntryReader r,string field,Action<EntryReader> action)
    {
        if(r.RequiredArray(field) is not {} array)return;int index=0;foreach(var value in array.EnumerateArray()){if(r.Nested(value,$"{field}[{index}]") is {} item){action(item);item.RejectUnknownFields();}index++;}
    }
}
