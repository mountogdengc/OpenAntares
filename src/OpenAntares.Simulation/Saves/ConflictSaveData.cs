using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Saves;
internal static class ConflictSaveData
{
    private static readonly JsonSerializerOptions Options=Create();
    private static JsonSerializerOptions Create()
    {
        var o=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
        o.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower,allowIntegerValues:false));ShipDesignSaveData.AddIds(o);
        o.Converters.Add(new Id<StarId>(n=>new(checked((int)n)),x=>x.Value));o.Converters.Add(new Id<PeaceOfferId>(n=>new(n),x=>x.Value));o.Converters.Add(new Id<ConflictEncounterId>(n=>new(n),x=>x.Value));o.Converters.Add(new Id<FleetId>(n=>new(checked((int)n)),x=>x.Value));return o;
    }
    private sealed class Id<T>(Func<long,T> create,Func<T,long> value):JsonConverter<T> where T:struct
    {
        public override T Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)
        {if(reader.TokenType!=JsonTokenType.Number||!reader.TryGetInt64(out long id)||id<1)throw new JsonException("Positive conflict identifier required.");return create(id);}
        public override void Write(Utf8JsonWriter writer,T id,JsonSerializerOptions options)=>writer.WriteNumberValue(value(id));
    }
    internal static void WriteGame(Utf8JsonWriter writer,GameState state)
    {if(!Rules.HasConflict(state.RulesVersion))return;writer.WritePropertyName("conflict");JsonSerializer.Serialize(writer,state.Conflict,Options);}
    internal static ConflictState? ReadGame(EntryReader parent,int rules)
    {
        if(!Rules.HasConflict(rules))return null;
        if(!parent.HasField("conflict")){parent.Error("conflict","Required state is missing.");return null;}
        if(parent.OptionalObjectElement("conflict") is not {} element){parent.Error("conflict","Required state cannot be null.");return null;}
        try
        {Unique(element);var value=element.Deserialize<ConflictState>(Options)??throw new JsonException("State is null.");Shape(element,JsonSerializer.SerializeToElement(value,Options),"conflict");return value;}
        catch(Exception e)when(e is JsonException or NotSupportedException or ArgumentException or InvalidOperationException or OverflowException){parent.Error("conflict",e.Message);return null;}
    }
    private static void Unique(JsonElement element)
    {
        if(element.ValueKind==JsonValueKind.Object)
        {
            var seen=new HashSet<string>(StringComparer.Ordinal);foreach(var p in element.EnumerateObject())
            {
                if(!seen.Add(p.Name))throw new JsonException("Duplicate conflict field: "+p.Name);
                if(p.Value.ValueKind==JsonValueKind.Null&&p.Name is not("answer" or "last_counterpart_declaration_turn" or "claim_observed_since" or "last_visible_activity_turn" or "war_turn" or "last_peace_turn" or "truce_end_turn" or "pending_peace" or "fleet" or "return_colony" or "completed_turn" or "last_resolved_turn" or "frame_id" or "engine_id" or "armor_id" or "star" or "own_fleet" or "own_vessel" or "encounter" or "other" or "revision"))throw new JsonException("Null required conflict field: "+p.Name);
                Unique(p.Value);
            }
        }
        else if(element.ValueKind==JsonValueKind.Array)foreach(var item in element.EnumerateArray()){if(item.ValueKind==JsonValueKind.Null)throw new JsonException("Null conflict list entry.");Unique(item);}
    }
    private static void Shape(JsonElement input,JsonElement canonical,string path)
    {
        if(input.ValueKind!=canonical.ValueKind)throw new JsonException("Invalid shape at "+path);
        if(input.ValueKind==JsonValueKind.Object)
        {
            var fields=input.EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value,StringComparer.Ordinal);var expected=canonical.EnumerateObject().ToArray();if(fields.Count!=expected.Length)throw new JsonException("Every conflict field is required at "+path);
            foreach(var p in expected){if(!fields.TryGetValue(p.Name,out var value))throw new JsonException("Missing conflict field: "+path+"."+p.Name);Shape(value,p.Value,path+"."+p.Name);}
        }
        else if(input.ValueKind==JsonValueKind.Array){var a=input.EnumerateArray().ToArray();var b=canonical.EnumerateArray().ToArray();if(a.Length!=b.Length)throw new JsonException("Invalid collection shape.");for(int i=0;i<a.Length;i++)Shape(a[i],b[i],path+"[]");}
        else if(input.ValueKind==JsonValueKind.String&&input.GetString()!=canonical.GetString())throw new JsonException("Noncanonical conflict string/enum at "+path);
    }
}
