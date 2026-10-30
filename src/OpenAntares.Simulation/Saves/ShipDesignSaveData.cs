using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Saves;
/// <summary>Explicit rules10 state, with complete shapes and positive local identifiers.</summary>
internal static class ShipDesignSaveData
{
    internal static readonly JsonSerializerOptions Options=CreateOptions();
    private static JsonSerializerOptions CreateOptions()
    {
        var options=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower,allowIntegerValues:false));AddIds(options);return options;
    }
    internal static void AddIds(JsonSerializerOptions options)
    {
        options.Converters.Add(new PositiveId<DesignId>(n=>new(checked((int)n)),id=>id.Value));options.Converters.Add(new PositiveId<RefitId>(n=>new(checked((int)n)),id=>id.Value));
        options.Converters.Add(new PositiveId<EmpireId>(n=>new(checked((int)n)),id=>id.Value));options.Converters.Add(new PositiveId<VesselId>(n=>new(checked((int)n)),id=>id.Value));options.Converters.Add(new PositiveId<ColonyId>(n=>new(checked((int)n)),id=>id.Value));
    }
    private sealed class PositiveId<T>(Func<long,T> create,Func<T,long> value):JsonConverter<T> where T:struct
    {
        public override T Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options){if(reader.TokenType!=JsonTokenType.Number||!reader.TryGetInt64(out long id)||id<1)throw new JsonException("Expected a positive identifier.");return create(id);}
        public override void Write(Utf8JsonWriter writer,T id,JsonSerializerOptions options)=>writer.WriteNumberValue(value(id));
    }
    internal static void WriteEmpire(Utf8JsonWriter writer,ShipDesignEmpireState? data,int rules)
    {if(!Rules.HasShipDesigns(rules))return;writer.WritePropertyName("ship_designs");JsonSerializer.Serialize(writer,data,Options);}
    internal static void ReadEmpire(EntryReader parent,EmpireState owner,int rules)
    {if(Rules.HasShipDesigns(rules))owner.ShipDesigns=ReadObject<ShipDesignEmpireState>(parent,"ship_designs",false);}
    internal static void WriteVessel(Utf8JsonWriter writer,VesselState ship,int rules)
    {if(!Rules.HasShipDesigns(rules))return;if(ship.Design is {} id)writer.WriteNumber("design",id.Value);else writer.WriteNull("design");}
    internal static void ReadVessel(EntryReader parent,VesselState ship,int rules)
    {if(Rules.HasShipDesigns(rules))ship.Design=parent.RequiredNullableInt("design",1,int.MaxValue) is {} id?new(id):null;}
    internal static void WriteFact(Utf8JsonWriter writer,string field,EquipmentFact? fact,int rules)
    {if(!Rules.HasShipDesigns(rules))return;writer.WritePropertyName(field);JsonSerializer.Serialize(writer,fact,Options);}
    internal static EquipmentFact? ReadFact(EntryReader parent,string field,int rules)=>Rules.HasShipDesigns(rules)?ReadObject<EquipmentFact>(parent,field,true):null;
    private static T? ReadObject<T>(EntryReader parent,string field,bool nullable) where T:class
    {
        if(!parent.HasField(field)){parent.Error(field,"Required field is missing.");return null;}
        if((nullable?parent.RequiredNullableObjectElement(field):parent.OptionalObjectElement(field)) is not {} element){if(!nullable)parent.Error(field,"Required object cannot be null.");return null;}
        try{Unique(element);var value=element.Deserialize<T>(Options)??throw new JsonException("Required object is null.");Shape(element,JsonSerializer.SerializeToElement(value,Options),field);return value;}
        catch(Exception error)when(error is JsonException or NotSupportedException or ArgumentException or InvalidOperationException or OverflowException){parent.Error(field,error.Message);return null;}
    }
    private static void Unique(JsonElement element)
    {
        if(element.ValueKind==JsonValueKind.Object)
        {
            var fields=new HashSet<string>(StringComparer.Ordinal);foreach(var p in element.EnumerateObject())
            {if(!fields.Add(p.Name))throw new JsonException($"Duplicate field '{p.Name}'.");if(p.Value.ValueKind==JsonValueKind.Null&&p.Name is not("armor_id" or "reason" or "vessel" or "port" or "before" or "after"))throw new JsonException($"Null required field '{p.Name}'.");Unique(p.Value);}
        }
        else if(element.ValueKind==JsonValueKind.Array)foreach(var item in element.EnumerateArray()){if(item.ValueKind==JsonValueKind.Null)throw new JsonException("Null collection element.");Unique(item);}
    }
    private static void Shape(JsonElement input,JsonElement canonical,string path)
    {
        if(input.ValueKind!=canonical.ValueKind)throw new JsonException($"Invalid shape at {path}.");
        if(input.ValueKind==JsonValueKind.Object)
        {
            var fields=input.EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value,StringComparer.Ordinal);var expected=canonical.EnumerateObject().ToArray();if(fields.Count!=expected.Length)throw new JsonException($"Every field is required at {path}.");foreach(var p in expected){if(!fields.TryGetValue(p.Name,out var child))throw new JsonException($"Missing field {path}.{p.Name}.");Shape(child,p.Value,path+"."+p.Name);}
        }
        else if(input.ValueKind==JsonValueKind.Array)
        {
            var a=input.EnumerateArray().ToArray();var b=canonical.EnumerateArray().ToArray();
            if(a.Length!=b.Length)throw new JsonException("Collection mismatch.");
            // Earlier rules10 writers retained bank input order. Preserve those saves while
            // validating the exact multiset; all other arrays and enum spellings remain strict.
            if(path.EndsWith(".weapon_bank_ids",StringComparison.Ordinal))a=a.OrderBy(e=>e.GetString(),StringComparer.Ordinal).ToArray();
            for(int i=0;i<a.Length;i++)Shape(a[i],b[i],path+"["+i+"]");
        }
        else if(input.ValueKind==JsonValueKind.String&&input.GetString()!=canonical.GetString())throw new JsonException($"Noncanonical enum at {path}.");
    }
}
