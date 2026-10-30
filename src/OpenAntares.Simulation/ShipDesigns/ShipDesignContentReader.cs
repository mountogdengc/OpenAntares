using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;

namespace OpenAntares.Simulation.ShipDesigns;

internal static class ShipDesignContentReader
{
    internal static readonly JsonSerializerOptions Options=new()
    {
        PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,
        Converters={new JsonStringEnumConverter<ShipComponentKind>(JsonNamingPolicy.SnakeCaseLower,allowIntegerValues:false)}
    };

    internal static ShipDesignDefinition? Read(JsonElement element,string file,List<ContentError> errors)
    {
        try
        {
            Unique(element);
            var value=element.Deserialize<ShipDesignDefinition>(Options)??throw new JsonException("ship_designs object is required.");
            return value.Frames is not null&&value.Components is not null
                ?value with{Frames=value.Frames.ToImmutableSortedDictionary(StringComparer.Ordinal),Components=value.Components.ToImmutableSortedDictionary(StringComparer.Ordinal)}
                :value;
        }
        catch(Exception error)when(error is JsonException or NotSupportedException or ArgumentException)
        {errors.Add(new(file,"ship_designs","ship_designs",error.Message));return null;}
    }

    internal static void Validate(ShipDesignDefinition value,IEnumerable<VesselDefinition> roles,CombatDefinition? combat,IEnumerable<TechnologyDefinition> technologies,string file,List<ContentError> errors)
    {
        void Error(string field,string message)=>errors.Add(new(file,"ship_designs","ship_designs."+field,message));
        bool Text(string? text)=>!string.IsNullOrWhiteSpace(text)&&text.Length<=80&&!text.Any(char.IsControl);
        var tech=technologies.ToDictionary(t=>t.Id,StringComparer.Ordinal);
        var role=roles.SingleOrDefault(v=>v.Id==value.WarshipRoleId);
        if(!Text(value.WarshipRoleId)||role is not{CombatOnly:true,EmbarkPopulation:0,CanFoundColony:false}||combat is null||combat.RoleProfiles is null||!combat.RoleProfiles.TryGetValue(value.WarshipRoleId,out var bound)||bound is not{Attack:>0})
            Error("warship_role_id","Bind an existing armed, zero-cargo, non-founding combat-only role.");
        if(value.Frames is null||value.Frames.Count==0)Error("frames","At least one frame is required.");
        else foreach(var item in value.Frames)
        {
            var frame=item.Value;
            if(frame is null){Error("frames."+item.Key,"A frame object is required.");continue;}
            if(item.Key!=frame.Id||!Text(frame.Id)||!Text(frame.Name))Error("frames."+item.Key+".id","Use a matching bounded ID and name.");
            if(frame.Hull<=0)Error("frames."+item.Key+".hull","Hull must be positive.");
            if(frame.Space<=0)Error("frames."+item.Key+".space","Space must be positive.");
            if(frame.Cost<=0)Error("frames."+item.Key+".cost","Frame cost must be positive.");
            if(frame.TravelPercent<=0)Error("frames."+item.Key+".travel_percent","Travel percent must be positive.");
            Prerequisites(frame.Prerequisites,"frames."+item.Key+".prerequisites");
        }
        if(value.Components is null||value.Components.Count==0)Error("components","Components are required.");
        else foreach(var item in value.Components)
        {
            var part=item.Value;
            if(part is null){Error("components."+item.Key,"A component object is required.");continue;}
            if(item.Key!=part.Id||!Text(part.Id)||!Text(part.Name))Error("components."+item.Key+".id","Use a matching bounded ID and name.");
            if(!Enum.IsDefined(part.Kind))Error("components."+item.Key+".kind","Unknown component kind.");
            if(part.Space<0)Error("components."+item.Key+".space","Space cannot be negative.");
            if(part.Cost<0)Error("components."+item.Key+".cost","Cost cannot be negative.");
            if(part.Value<(part.Kind==ShipComponentKind.Armor?0:1))Error("components."+item.Key+".value","Weapon/engine values must be positive; armor may be zero.");
            Prerequisites(part.Prerequisites,"components."+item.Key+".prerequisites");
        }
        if(value.StartingRecipe is null){Error("starting_recipe","A starting recipe is required.");return;}
        try
        {
            var stats=ShipDesignMath.Calculate(value,value.StartingRecipe);
            if(role is null||combat is null||combat.RoleProfiles is null||!combat.RoleProfiles.TryGetValue(role.Id,out var profile)||profile is null||
                stats.BuildCost!=role.Cost||stats.BaseSpeed!=role.Speed||stats.MaxHull!=profile.MaxHull||stats.Attack!=profile.Attack||stats.Armor!=profile.Armor)
                Error("starting_recipe","Starting recipe must reproduce its bound role's cost, speed, hull, attack and armor.");
            var frame=value.Frames![value.StartingRecipe.FrameId];
            var selected=value.StartingRecipe.WeaponBankIds.Prepend(value.StartingRecipe.EngineId);
            if(value.StartingRecipe.ArmorId is {} armor)selected=selected.Append(armor);
            if(!frame.Prerequisites.IsDefaultOrEmpty||selected.Any(id=>!value.Components![id].Prerequisites.IsDefaultOrEmpty))
                Error("starting_recipe","Starting recipe must be available without research.");
        }
        catch(Exception error)when(error is ArgumentException or OverflowException or KeyNotFoundException)
        {Error("starting_recipe",error.Message);}

        void Prerequisites(ImmutableArray<string> prerequisites,string path)
        {
            if(prerequisites.IsDefault){Error(path,"Prerequisites array is required.");return;}
            if(prerequisites.Distinct(StringComparer.Ordinal).Count()!=prerequisites.Length)Error(path,"Duplicate prerequisites.");
            foreach(string id in prerequisites)if(id is null||!tech.ContainsKey(id))Error(path,"Undefined prerequisite: "+id);
        }
    }

    private static void Unique(JsonElement element)
    {
        if(element.ValueKind==JsonValueKind.Object)
        {
            var names=new HashSet<string>(StringComparer.Ordinal);
            foreach(var field in element.EnumerateObject()){if(!names.Add(field.Name))throw new JsonException("Duplicate ship field: "+field.Name);Unique(field.Value);}
        }
        else if(element.ValueKind==JsonValueKind.Array)foreach(var item in element.EnumerateArray())Unique(item);
    }
}
