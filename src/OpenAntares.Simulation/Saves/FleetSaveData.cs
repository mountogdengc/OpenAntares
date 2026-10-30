using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Saves;

/// <summary>Writer9 rules8 layout. Every serialized member is required, including explicit nulls.
/// Canonical shape comparison prevents constructor defaults from accepting missing fields;
/// enum names, duplicate members and unknown fields are checked before state validation.</summary>
internal static class FleetSaveData
{
    private static readonly JsonSerializerOptions Options=CreateOptions(false);
    private static readonly JsonSerializerOptions DesignOptions=CreateOptions(true);
    private static JsonSerializerOptions CreateOptions(bool designs)
    {
        var resolver=new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info=>{if(info.Type==typeof(AssemblySlot))for(int i=info.Properties.Count-1;i>=0;i--)if(info.Properties[i].Name=="design"){if(!designs)info.Properties.RemoveAt(i);else info.Properties[i].ShouldSerialize=(_,_)=>true;}});
        var options=new JsonSerializerOptions{TypeInfoResolver=resolver,PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
        if(designs)ShipDesignSaveData.AddIds(options);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower,allowIntegerValues:false));
        options.Converters.Add(new IdConverter<FleetId>(n=>new(n),id=>id.Value));options.Converters.Add(new IdConverter<RequestId>(n=>new(n),id=>id.Value));options.Converters.Add(new IdConverter<OfficerId>(n=>new(n),id=>id.Value));
        options.Converters.Add(new IdConverter<EmpireId>(n=>new(n),id=>id.Value));options.Converters.Add(new IdConverter<ColonyId>(n=>new(n),id=>id.Value));options.Converters.Add(new IdConverter<VesselId>(n=>new(n),id=>id.Value));options.Converters.Add(new IdConverter<StarId>(n=>new(n),id=>id.Value));
        return options;
    }
    private sealed class IdConverter<T>(Func<int,T> create,Func<T,int> value):JsonConverter<T> where T:struct
    {
        public override T Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)
        {if(reader.TokenType!=JsonTokenType.Number||!reader.TryGetInt32(out int id)||id<1)throw new JsonException("Expected a positive identifier.");return create(id);}
        public override void Write(Utf8JsonWriter writer,T id,JsonSerializerOptions options)=>writer.WriteNumberValue(value(id));
    }
    internal static void WriteEmpire(Utf8JsonWriter writer,FleetEmpireState? state,int rules)
    {if(!Rules.HasFleets(rules))return;writer.WritePropertyName("fleet_command");JsonSerializer.Serialize(writer,state,Rules.HasShipDesigns(rules)?DesignOptions:Options);}
    internal static FleetEmpireState? ReadEmpire(EntryReader parent,int rules)
    {
        if(!Rules.HasFleets(rules))return null;
        if(!parent.HasField("fleet_command")){parent.Error("fleet_command","Required field is missing.");return null;}
        if(parent.OptionalObjectElement("fleet_command") is not {} element)return null;
        try
        {
            var options=Rules.HasShipDesigns(rules)?DesignOptions:Options;Unique(element);var state=element.Deserialize<FleetEmpireState>(options)??throw new JsonException("Fleet state is required.");
            if(state.ProfileCommissions is not null)state.ProfileCommissions=new(state.ProfileCommissions,StringComparer.Ordinal);
            Shape(element,JsonSerializer.SerializeToElement(state,options),"fleet_command");return state;
        }
        catch(Exception error)when(error is JsonException or NotSupportedException or ArgumentException or InvalidOperationException or OverflowException)
        {parent.Error("fleet_command",error.Message);return null;}
    }
    private static void Unique(JsonElement element)
    {
        if(element.ValueKind==JsonValueKind.Object)
        {var names=new HashSet<string>(StringComparer.Ordinal);foreach(var p in element.EnumerateObject()){if(!names.Add(p.Name))throw new JsonException($"Duplicate field '{p.Name}'.");Unique(p.Value);}}
        else if(element.ValueKind==JsonValueKind.Array)foreach(var item in element.EnumerateArray()){if(item.ValueKind==JsonValueKind.Null)throw new JsonException("Null collection element.");Unique(item);}
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
        {var values=input.EnumerateArray().ToArray();var expected=canonical.EnumerateArray().ToArray();if(values.Length!=expected.Length)throw new JsonException($"Duplicate or invalid collection entry at {path}.");for(int i=0;i<values.Length;i++)Shape(values[i],expected[i],path+"["+i+"]");}
        else if(input.ValueKind==JsonValueKind.String&&input.GetString()!=canonical.GetString())throw new JsonException($"Noncanonical enum or value at {path}.");
    }
    internal static void WriteVessel(Utf8JsonWriter writer,VesselState ship,int rules)
    {
        if(!Rules.HasFleets(rules))return;
        if(ship.Fleet is {} fleet)writer.WriteNumber("fleet",fleet.Value);else writer.WriteNull("fleet");writer.WriteBoolean("newly_completed",ship.NewlyCompleted);writer.WritePropertyName("build_origin");JsonSerializer.Serialize(writer,ship.BuildOrigin,Options);
    }
    internal static void ReadVessel(EntryReader parent,VesselState ship,int rules)
    {
        if(!Rules.HasFleets(rules))return;
        ship.Fleet=parent.RequiredNullableInt("fleet",1,int.MaxValue) is {} id?new(id):null;ship.NewlyCompleted=parent.RequiredBool("newly_completed");
        if(parent.RequiredNullableObject("build_origin") is {} origin)
        {ship.BuildOrigin=new(new(origin.RequiredInt("request",1,int.MaxValue)),origin.RequiredInt("slot",1,int.MaxValue),new(origin.RequiredInt("colony",1,int.MaxValue)),origin.RequiredInt("queue_token",1,int.MaxValue));origin.RejectUnknownFields();}
    }
}
