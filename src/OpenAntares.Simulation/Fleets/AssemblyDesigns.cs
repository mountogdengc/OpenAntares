using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Fleets;
internal static class AssemblyDesigns
{
    internal static ProductionTarget Target(string role,DesignId? design)=>design is {} id?ShipDesignTargets.Build(id):new(ProductionKinds.Vessel,role);
    internal static AssemblyQuota At(AssemblyDraft draft,int index)=>draft.Quotas.IsDefaultOrEmpty?new(draft.Roles[index],null):draft.Quotas[index];
    internal static AssemblyQuota From(IFleetPlanningContext context,ProductionTarget target)
    {
        if(target.Kind==ProductionKinds.ShipDesign&&ShipDesignTargets.TryId(target.ContentId,out int id))return new(context.Content.ShipDesigns!.WarshipRoleId,new(id));
        if(target.Kind==ProductionKinds.Vessel)return new(target.ContentId,null);
        throw new ArgumentException("A ship target is required.");
    }
    internal static long Cost(IFleetPlanningContext context,AssemblyQuota quota)=>quota.Design is {} id?context.View.ShipDesigns!.Designs.Single(d=>d.Id==id).Stats.BuildCost:context.Content.Vessels[quota.RoleId].Cost;
    internal static int Speed(IFleetPlanningContext context,string role,DesignId? design)=>design is {} id?context.View.ShipDesigns!.Designs.Single(d=>d.Id==id).Stats.BaseSpeed:context.Content.Vessels[role].Speed;
    internal static AssemblyDraft Normalize(IFleetPlanningContext context,AssemblyDraft draft,RequestId? replacing)
    {
        if(!Rules.HasShipDesigns(context.View.RulesVersion))
        {
            if(!draft.Quotas.IsDefaultOrEmpty)throw new ArgumentException("Old campaigns cannot contain design quotas.");
            return draft;
        }
        var definition=context.Content.ShipDesigns??throw new ArgumentException("Ship-design content required.");
        AssemblyQuota Role(string role)=>new(role,role==definition.WarshipRoleId?new DesignId(1):null);
        var aliases=draft.Roles.IsDefault?ImmutableArray<AssemblyQuota>.Empty:draft.Roles.Select(Role).ToImmutableArray();
        var quotas=draft.Quotas.IsDefaultOrEmpty?aliases:draft.Quotas.Select(q=>q is null?throw new ArgumentException("Missing quota."):q.RoleId==definition.WarshipRoleId&&q.Design is null?q with{Design=new(1)}:q).ToImmutableArray();
        if(!draft.Quotas.IsDefaultOrEmpty&&!aliases.IsEmpty&&!aliases.SequenceEqual(quotas))throw new ArgumentException("Roles and design quotas contradict one another.");
        var original=context.View.Fleets?.Requests.SingleOrDefault(r=>r.Id==replacing);
        foreach(var quota in quotas)
        {
            if(quota.RoleId==definition.WarshipRoleId)
            {
                var design=context.View.ShipDesigns?.Designs.SingleOrDefault(d=>d.Id==quota.Design);
                if(design is null||!design.Unlocked)throw new ArgumentException("An owned unlocked design is required.");
                if(design.Archived&&quotas.Count(q=>q==quota)>(original?.Slots.Count(s=>s.RoleId==quota.RoleId&&s.Design==quota.Design)??0))throw new ArgumentException("Archived designs cannot be newly requested.");
            }
            else if(quota.Design is not null)throw new ArgumentException("Civilian quotas cannot have a custom design.");
        }
        return draft with{Roles=quotas.Select(q=>q.RoleId).ToImmutableArray(),Quotas=quotas};
    }
}
