using System;
using System.Linq;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
namespace OpenAntares.Game.Components;
/// <summary>Own labels and typed picker metadata; all prices and equipment stats come from simulation.</summary>
internal static class ShipDesignUi
{
    internal static string Encode(ProductionTarget target)=>target.Kind+"|"+target.ContentId;
    internal static ProductionTarget Decode(string metadata){int split=metadata.IndexOf('|');if(split<0)throw new ArgumentException("Typed production metadata is required.");return new(metadata[..split],metadata[(split+1)..]);}
    internal static string Name(ContentSet content,EmpireView? view,ProductionTarget target)
    {
        if(target.Kind==ProductionKinds.ShipDesign&&ShipDesignTargets.TryId(target.ContentId,out int number))return DesignName(content,view,new(number));
        if(target.Kind==ProductionKinds.Refit)return "Committed refit #"+target.ContentId;
        return target.Kind==ProductionKinds.Building?content.Buildings[target.ContentId].Name:content.Vessels[target.ContentId].Name;
    }
    internal static string DesignName(ContentSet content,EmpireView? view,DesignId id)
    {
        var design=view?.ShipDesigns?.Designs.SingleOrDefault(d=>d.Id==id);return design is null?"Design #"+id.Value:design.Name+" — "+content.ShipDesigns!.Frames[design.Recipe.FrameId].Name;
    }
    internal static bool UsesDesigns(ContentSet content,EmpireView view,string role)=>Rules.HasShipDesigns(view.RulesVersion)&&role==content.ShipDesigns?.WarshipRoleId;
    internal static string EquipmentSummary(ContentSet content,ShipRecipe recipe)
    {
        var parts=content.ShipDesigns!.Components;
        string banks=string.Join(", ",recipe.WeaponBankIds.GroupBy(id=>id,StringComparer.Ordinal).OrderBy(g=>g.Key,StringComparer.Ordinal).Select(g=>parts[g.Key].Name+" ×"+g.Count()));
        return $"Engine: {parts[recipe.EngineId].Name}; armor: {(recipe.ArmorId is {} armor?parts[armor].Name:"None")}; weapon banks: {banks}.";
    }
    internal static string EquipmentChanges(ContentSet content,ShipRecipe before,ShipRecipe after)
    {
        string[] Parts(ShipRecipe recipe)=>recipe.WeaponBankIds.Prepend(recipe.EngineId).Concat(recipe.ArmorId is {} armor?new[]{armor}:Array.Empty<string>()).ToArray();
        var old=Parts(before);var target=Parts(after);
        string Difference(string[] from,string[] to)
        {
            var removed=from.GroupBy(id=>id,StringComparer.Ordinal).OrderBy(g=>g.Key,StringComparer.Ordinal)
                .Select(g=>(Id:g.Key,Count:g.Count()-to.Count(id=>id==g.Key))).Where(p=>p.Count>0)
                .Select(p=>content.ShipDesigns!.Components[p.Id].Name+" ×"+p.Count).ToArray();
            return removed.Length==0?"None":string.Join(", ",removed);
        }
        return "Removed: "+Difference(old,target)+".\nInstalled: "+Difference(target,old)+".";
    }
}
