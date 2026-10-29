using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Saves;
/// <summary>Rules9-only state. Every field, including nullable leaves, is explicit.</summary>
internal static class CombatSaveData
{
    private static readonly JsonSerializerOptions Options=CreateOptions();
    private static JsonSerializerOptions CreateOptions()
    {
        var resolver=new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info=>{if(info.Type==typeof(CombatFrame))for(int i=info.Properties.Count-1;i>=0;i--)if(info.Properties[i].Name=="definition")info.Properties.RemoveAt(i);});
        var o=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,TypeInfoResolver=resolver};
        o.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower,allowIntegerValues:false));
        o.Converters.Add(new IdConverter<PocketId>(n=>new(checked((int)n)),id=>id.Value));o.Converters.Add(new IdConverter<SentryId>(n=>new(checked((int)n)),id=>id.Value));o.Converters.Add(new IdConverter<EncounterId>(n=>new(n),id=>id.Value));
        o.Converters.Add(new IdConverter<FleetId>(n=>new(checked((int)n)),id=>id.Value));o.Converters.Add(new IdConverter<EmpireId>(n=>new(checked((int)n)),id=>id.Value));o.Converters.Add(new IdConverter<ColonyId>(n=>new(checked((int)n)),id=>id.Value));o.Converters.Add(new IdConverter<VesselId>(n=>new(checked((int)n)),id=>id.Value));o.Converters.Add(new IdConverter<StarId>(n=>new(checked((int)n)),id=>id.Value));return o;
    }
    private sealed class IdConverter<T>(Func<long,T> create,Func<T,long> value):JsonConverter<T> where T:struct
    {
        public override T Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)
        {if(reader.TokenType!=JsonTokenType.Number||!reader.TryGetInt64(out long id)||id<1)throw new JsonException("Expected a positive identifier.");return create(id);}
        public override void Write(Utf8JsonWriter writer,T id,JsonSerializerOptions options)=>writer.WriteNumberValue(value(id));
    }
    internal static void WriteGame(Utf8JsonWriter writer,GameState state)=>Write(writer,state.Combat,state.RulesVersion);
    internal static CombatGameState? ReadGame(EntryReader parent,int rules)=>Read<CombatGameState>(parent,rules);
    internal static void WriteEmpire(Utf8JsonWriter writer,CombatEmpireState? state,int rules)=>Write(writer,state,rules);
    internal static CombatEmpireState? ReadEmpire(EntryReader parent,int rules)=>Read<CombatEmpireState>(parent,rules);
    internal static void WriteVessel(Utf8JsonWriter writer,VesselState vessel,int rules)=>Write(writer,vessel.Combat,rules);
    internal static void ReadVessel(EntryReader parent,VesselState vessel,int rules)=>vessel.Combat=Read<ShipCombatState>(parent,rules);
    private static void Write<T>(Utf8JsonWriter writer,T state,int rules)
    {if(!Rules.HasCombat(rules))return;writer.WritePropertyName("combat");JsonSerializer.Serialize(writer,state,Options);}
    private static T? Read<T>(EntryReader parent,int rules) where T:class
    {
        if(!Rules.HasCombat(rules))return null;
        if(!parent.HasField("combat")){parent.Error("combat","Required field is missing.");return null;}
        if(parent.OptionalObjectElement("combat") is not {} element)return null;
        try{Unique(element);var result=element.Deserialize<T>(Options)??throw new JsonException("Combat state is required.");Shape(element,JsonSerializer.SerializeToElement(result,Options),"combat");return result;}
        catch(Exception error)when(error is JsonException or NotSupportedException or ArgumentException or InvalidOperationException or OverflowException){parent.Error("combat",error.Message);return null;}
    }
    private static void Unique(JsonElement e)
    {
        if(e.ValueKind==JsonValueKind.Object)
        {
            var names=new HashSet<string>(StringComparer.Ordinal);
            foreach(var p in e.EnumerateObject())
            {
                if(!names.Add(p.Name))throw new JsonException($"Duplicate field '{p.Name}'.");
                if(p.Value.ValueKind==JsonValueKind.Null&&p.Name is not ("return_colony" or "on_arrival" or "last_repair_turn" or "last_repair_blocked_turn" or "last_resolved_turn" or "last_resolved_phase" or "consent_revision" or "continue_risk_turn" or "encounter" or "vessel" or "encounter_id" or "route" or "after"))throw new JsonException($"Null required field '{p.Name}'.");
                Unique(p.Value);
            }
        }
        else if(e.ValueKind==JsonValueKind.Array)foreach(var item in e.EnumerateArray()){if(item.ValueKind==JsonValueKind.Null)throw new JsonException("Null collection element.");Unique(item);}
    }
    private static void Shape(JsonElement input,JsonElement canonical,string path)
    {
        if(input.ValueKind!=canonical.ValueKind)throw new JsonException($"Invalid shape at {path}.");
        if(input.ValueKind==JsonValueKind.Object)
        {
            var fields=input.EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value,StringComparer.Ordinal);var expected=canonical.EnumerateObject().ToArray();
            if(fields.Count!=expected.Length)throw new JsonException($"Every field is required at {path}.");
            foreach(var p in expected){if(!fields.TryGetValue(p.Name,out var child))throw new JsonException($"Required field {path}.{p.Name} is missing.");Shape(child,p.Value,path+"."+p.Name);}
        }
        else if(input.ValueKind==JsonValueKind.Array)
        {var values=input.EnumerateArray().ToArray();var expected=canonical.EnumerateArray().ToArray();if(values.Length!=expected.Length)throw new JsonException($"Invalid collection at {path}.");for(int i=0;i<values.Length;i++)Shape(values[i],expected[i],path+"["+i+"]");}
        else if(input.ValueKind==JsonValueKind.String&&input.GetString()!=canonical.GetString())throw new JsonException($"Noncanonical enum or value at {path}.");
    }
    // Frames use the one effective embedded definition, never a second editable copy.
    internal static void AttachDefinition(ContentSet content,GameState state)
    {
        if(!Rules.HasCombat(state.RulesVersion))return;
        CombatRoundReceipt Attach(CombatRoundReceipt r)=>r with{Before=r.Before with{Definition=content.Combat}};
        foreach(var e in state.Combat!.Encounters)e.Rounds=e.Rounds.Select(Attach).ToList();
        foreach(var e in state.Empires)e.Combat!.History=e.Combat.History.Select(h=>h with{Details=h.Details.Select(Attach).ToImmutableArray()}).ToList();
    }
}
