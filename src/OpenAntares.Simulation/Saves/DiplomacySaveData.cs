using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Saves;
internal static class DiplomacySaveData
{
    private static readonly JsonSerializerOptions Options=Create();
    private static JsonSerializerOptions Create()
    {
        var o=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
        o.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower,allowIntegerValues:false));ShipDesignSaveData.AddIds(o);
        o.Converters.Add(new Id<StarId>(n=>new(checked((int)n)),x=>x.Value));o.Converters.Add(new Id<OfferId>(n=>new(n),x=>x.Value));return o;
    }
    private sealed class Id<T>(Func<long,T> create,Func<T,long> value):JsonConverter<T> where T:struct
    {
        public override T Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)
        {if(reader.TokenType!=JsonTokenType.Number||!reader.TryGetInt64(out long id)||id<1)throw new JsonException("Positive diplomacy identifier required.");return create(id);}
        public override void Write(Utf8JsonWriter writer,T id,JsonSerializerOptions options)=>writer.WriteNumberValue(value(id));
    }
    internal static void WriteGame(Utf8JsonWriter writer,GameState state)
    {if(!Rules.HasDiplomacy(state.RulesVersion))return;writer.WritePropertyName("diplomacy");JsonSerializer.Serialize(writer,state.Diplomacy,Options);}
    internal static DiplomacyState? ReadGame(EntryReader parent,int rules)
    {
        if(!Rules.HasDiplomacy(rules))return null;
        if(!parent.HasField("diplomacy")){parent.Error("diplomacy","Required state is missing.");return null;}
        if(parent.OptionalObjectElement("diplomacy") is not {} element){parent.Error("diplomacy","Required state cannot be null.");return null;}
        try
        {Unique(element);var value=element.Deserialize<DiplomacyState>(Options)??throw new JsonException("State is null.");Shape(element,JsonSerializer.SerializeToElement(value,Options),"diplomacy");return value;}
        catch(Exception e)when(e is JsonException or NotSupportedException or ArgumentException or InvalidOperationException or OverflowException){parent.Error("diplomacy",e.Message);return null;}
    }
    private static void Unique(JsonElement element)
    {
        if(element.ValueKind==JsonValueKind.Object)
        {
            var seen=new HashSet<string>(StringComparer.Ordinal);foreach(var p in element.EnumerateObject())
            {
                if(!seen.Add(p.Name))throw new JsonException("Duplicate diplomacy field: "+p.Name);
                if(p.Value.ValueKind==JsonValueKind.Null&&p.Name is not("last_counterpart_notice_turn" or "pending" or "star" or "proposer_hub" or "answer" or "hub" or "low_hub" or "high_hub" or "low_notice_turn" or "high_notice_turn" or "end_turn" or "policy_reason" or "own_hub"))throw new JsonException("Null required diplomacy field: "+p.Name);
                Unique(p.Value);
            }
        }
        else if(element.ValueKind==JsonValueKind.Array)foreach(var item in element.EnumerateArray()){if(item.ValueKind==JsonValueKind.Null)throw new JsonException("Null diplomacy list entry.");Unique(item);}
    }
    private static void Shape(JsonElement input,JsonElement canonical,string path)
    {
        if(input.ValueKind!=canonical.ValueKind)throw new JsonException("Invalid shape at "+path);
        if(input.ValueKind==JsonValueKind.Object)
        {
            var fields=input.EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value,StringComparer.Ordinal);var expected=canonical.EnumerateObject().ToArray();if(fields.Count!=expected.Length)throw new JsonException("Every diplomacy field is required at "+path);
            foreach(var p in expected){if(!fields.TryGetValue(p.Name,out var value))throw new JsonException("Missing diplomacy field: "+path+"."+p.Name);Shape(value,p.Value,path+"."+p.Name);}
        }
        else if(input.ValueKind==JsonValueKind.Array){var a=input.EnumerateArray().ToArray();var b=canonical.EnumerateArray().ToArray();if(a.Length!=b.Length)throw new JsonException("Invalid collection shape.");for(int i=0;i<a.Length;i++)Shape(a[i],b[i],path+"[]");}
        else if(input.ValueKind==JsonValueKind.String&&input.GetString()!=canonical.GetString())throw new JsonException("Noncanonical diplomacy string/enum at "+path);
    }
}
