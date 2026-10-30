using System;
namespace OpenAntares.Simulation.ShipDesigns;
public static class ShipDesignMath
{
    public static long RefitPrice(long cost)
    {
        if(cost<0)throw new ArgumentOutOfRangeException(nameof(cost));
        return checked((long)Int128.Max(1,((Int128)cost+3)/4));
    }
    public static DesignStats Calculate(ShipDesignDefinition definition,ShipRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(definition);ArgumentNullException.ThrowIfNull(recipe);
        if(definition.Frames is null||recipe.FrameId is null||!definition.Frames.TryGetValue(recipe.FrameId,out var frame)||frame is null)
            throw new ArgumentException("Unknown frame.");
        if(frame.Hull<=0||frame.Space<=0||frame.Cost<=0||frame.TravelPercent<=0)throw new ArgumentException("Invalid frame hull/space/cost/travel_percent.");
        if(recipe.WeaponBankIds.IsDefaultOrEmpty||recipe.WeaponBankIds.Length>2)throw new ArgumentException("Choose one or two weapon banks.");
        var engine=Component(recipe.EngineId,ShipComponentKind.Engine);
        var armor=recipe.ArmorId is {} id?Component(id,ShipComponentKind.Armor):null;
        Int128 cost=(Int128)frame.Cost+engine.Cost+(armor?.Cost??0);
        Int128 space=(Int128)engine.Space+(armor?.Space??0),attack=0;
        foreach(string bank in recipe.WeaponBankIds)
        {var weapon=Component(bank,ShipComponentKind.Weapon);cost+=weapon.Cost;space+=weapon.Space;attack+=weapon.Value;}
        if(space>frame.Space)throw new ArgumentException("Equipment exceeds frame space capacity.");
        int speed=checked((int)Int128.Max(1,(Int128)engine.Value*frame.TravelPercent/100));
        return new(checked((long)cost),frame.Hull,checked((int)attack),armor?.Value??0,speed,checked((int)space),frame.Space);

        ShipComponentDefinition Component(string? key,ShipComponentKind kind)
        {
            if(key is null||definition.Components is null||!definition.Components.TryGetValue(key,out var component)||component is null||component.Kind!=kind)
                throw new ArgumentException("Unknown or incorrect "+kind.ToString().ToLowerInvariant()+" component.");
            if(component.Space<0||component.Cost<0||component.Value<(kind==ShipComponentKind.Armor?0:1))
                throw new ArgumentException("Invalid component space/cost/value.");
            return component;
        }
    }
}
