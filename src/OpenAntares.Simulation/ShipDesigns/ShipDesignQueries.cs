using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.ShipDesigns;
public static class ShipDesignQueries
{
    public static CataloguePage CataloguePage(EmpireView view,int page=0,CatalogueFilter? filter=null,CatalogueFilter? previousFilter=null)
    {
        filter=Normalize(filter);if(previousFilter is not null&&Normalize(previousFilter)!=filter)page=0;
        var designs=view.ShipDesigns?.Designs??ImmutableArray<DesignView>.Empty;int active=designs.Count(d=>!d.Archived);
        var rows=designs.Where(d=>(filter.IncludeArchived||!d.Archived)&&(filter.Search is null||d.Name.Contains(filter.Search,StringComparison.OrdinalIgnoreCase))).OrderBy(d=>d.Id).ToArray();int pages=Math.Max(1,(rows.Length+15)/16);page=Math.Clamp(page,0,pages-1);
        return new(page,pages,rows.Length,rows.Skip(page*16).Take(16).Select(d=>new CatalogueRow(d,d.Id.Value==1?"Standard Escort is permanent.":d.Archived?"Already archived.":null,!d.Archived?"Already active.":active>=32?"The catalogue already has 32 active designs.":null,d.Id.Value==1?"Standard Escort is permanent.":!d.Archived?"Archive this design before deleting it.":d.References.Any?"Owned ships, projects, queues, request slots or refits still reference this design.":null)).ToImmutableArray());
        static CatalogueFilter Normalize(CatalogueFilter? value)=>new(value?.IncludeArchived??false,string.IsNullOrWhiteSpace(value?.Search)?null:value.Search.Trim());
    }
    public static ImmutableArray<DesignView> BuildChoices(EmpireView view)=>!Rules.HasShipDesigns(view.RulesVersion)?ImmutableArray<DesignView>.Empty:view.ShipDesigns?.Designs.Where(d=>!d.Archived&&d.Unlocked).OrderBy(d=>d.Id).ToImmutableArray()??ImmutableArray<DesignView>.Empty;
    public static DesignPreview PreviewDraft(ContentSet content,EmpireView view,string name,ShipRecipe recipe)
    {
        DesignPreview Reject(string code,DesignStats? stats=null,ImmutableArray<string> missing=default,string? explanation=null)=>new(false,code,stats,missing.IsDefault?ImmutableArray<string>.Empty:missing){Explanation=explanation};
        if(!Rules.HasShipDesigns(view.RulesVersion)||content.ShipDesigns is not {} definition||view.ShipDesigns is not {} catalogue)return Reject("ship_design_unavailable");
        name=name?.Trim()??string.Empty;
        if(!ValidName(name))return Reject("ship_design_name");
        DesignStats stats;
        try{stats=ShipDesignMath.Calculate(definition,recipe);}
        catch(Exception error)when(error is ArgumentException or OverflowException or System.Collections.Generic.KeyNotFoundException){return Reject("ship_design_recipe",explanation:error.Message);}
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
    internal static ShipDesignEmpireView? Project(ContentSet content,GameState state,EmpireId viewer,Economy.TurnEconomy forecast)
    {
        if(!Rules.HasShipDesigns(state.RulesVersion)||state.FindEmpire(viewer)?.ShipDesigns is not {} data||content.ShipDesigns is not {} definition)return null;
        var owner=state.FindEmpire(viewer)!;
        var references=ShipDesignCatalogue.Index(state,viewer);
        EquipmentFact Fact(DesignId id){var d=data.Designs.Single(d=>d.Id==id);return new(d.Id,d.Name,d.Recipe,ShipDesignMath.Calculate(definition,d.Recipe));}
        RefitView Refit(RefitJob j)
        {
            var port=state.FindColony(j.Port)!;var economy=forecast.Colony(j.Port);string code=j.Status==RefitStatus.Paused?"refit_paused":port.ProductionReserve>=j.Price?"refit_ready_next_interval":economy.NetProduction.Total<=0?"refit_port_inactive":"refit_insufficient_reserve";
            string explanation=code switch{"refit_paused"=>j.Reason??"Review the ship and port before explicitly resuming.","refit_ready_next_interval"=>"Reserve covers the cost; completion still requires a full stationary interval.","refit_port_inactive"=>"The port has no net production and reserve does not cover the refit.",_=>"The port is banking production until reserve covers the frozen refit price."};
            return new(j.Id,j.Vessel,j.Port,j.SourceDesign,j.TargetDesign,j.AcceptedTurn,j.Revision,j.Price,j.Status,j.Reason){Reserve=port.ProductionReserve,OutputPerTurn=economy.NetProduction.Total,EstimatedTurns=j.Status==RefitStatus.Paused?null:economy.Production.Estimate?.Turns,StatusCode=code,StatusExplanation=explanation,Before=Fact(j.SourceDesign),After=Fact(j.TargetDesign)};
        }
        return new(data.CatalogueRevision,data.Designs.OrderBy(d=>d.Id).Select(d=>new DesignView(d.Id,d.Name,d.Recipe,ShipDesignMath.Calculate(definition,d.Recipe),d.Archived,ShipDesignStateValidator.Unlocked(definition,d.Recipe,owner),references[d.Id])).ToImmutableArray())
        {
            NextRefitId=data.NextRefitId,
            Refits=data.Refits.OrderBy(j=>j.Id).Select(Refit).ToImmutableArray(),
            Events=data.RecentEvents.Concat(data.PendingEvents).OrderBy(e=>e.Id).TakeLast(20).ToImmutableArray()
        };
    }
}
