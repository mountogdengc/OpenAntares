using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.ShipDesigns;
public static class ShipDesignQueries
{
    public static DesignPreview PreviewDraft(ContentSet content,EmpireView view,string name,ShipRecipe recipe)
    {
        DesignPreview Reject(string code,DesignStats? stats=null,ImmutableArray<string> missing=default)=>new(false,code,stats,missing.IsDefault?ImmutableArray<string>.Empty:missing);
        if(!Rules.HasShipDesigns(view.RulesVersion)||content.ShipDesigns is not {} definition||view.ShipDesigns is not {} catalogue)return Reject("ship_design_unavailable");
        name=name?.Trim()??string.Empty;
        if(!ValidName(name))return Reject("ship_design_name");
        DesignStats stats;
        try{stats=ShipDesignMath.Calculate(definition,recipe);}
        catch(Exception error)when(error is ArgumentException or OverflowException or System.Collections.Generic.KeyNotFoundException){return Reject("ship_design_recipe");}
        var selected=recipe.WeaponBankIds.Prepend(recipe.EngineId);if(recipe.ArmorId is {} armor)selected=selected.Append(armor);
        var missing=definition.Frames[recipe.FrameId].Prerequisites.Concat(selected.SelectMany(id=>definition.Components[id].Prerequisites)).Where(id=>!view.Research.KnownTechnologyIds.Contains(id)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        if(!missing.IsEmpty)return Reject("ship_design_locked",stats,missing);
        var same=catalogue.Designs.SingleOrDefault(d=>string.Equals(d.Name,name,StringComparison.OrdinalIgnoreCase));
        if(same is not null)return ShipDesignStateValidator.SameRecipe(same.Recipe,recipe)?new(true,null,stats,missing):Reject("ship_design_duplicate_name",stats);
        if(catalogue.Designs.Length>=256)return Reject("ship_design_retained_limit",stats);
        if(catalogue.Designs.Count(d=>!d.Archived)>=32)return Reject("ship_design_active_limit",stats);
        return new(true,null,stats,missing);
    }
    internal static bool ValidName(string name)=>!string.IsNullOrWhiteSpace(name)&&name.Length<=40&&!name.Any(char.IsControl)&&name==name.Trim();
    internal static ShipDesignEmpireView? Project(ContentSet content,GameState state,EmpireId viewer)
    {
        if(!Rules.HasShipDesigns(state.RulesVersion)||state.FindEmpire(viewer)?.ShipDesigns is not {} data||content.ShipDesigns is not {} definition)return null;
        var owner=state.FindEmpire(viewer)!;
        return new(data.CatalogueRevision,data.Designs.OrderBy(d=>d.Id).Select(d=>new DesignView(d.Id,d.Name,d.Recipe,ShipDesignMath.Calculate(definition,d.Recipe),d.Archived,ShipDesignStateValidator.Unlocked(definition,d.Recipe,owner),ShipDesignCatalogue.References(state,viewer,d.Id))).ToImmutableArray());
    }
}
