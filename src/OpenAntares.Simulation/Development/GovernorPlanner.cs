using System;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Development;
public static class GovernorPlanner
{
    public static GovernorPlan Plan(IGovernorPlanningContext context)
    {
        var orders=ImmutableArray.CreateBuilder<GovernorColonyOrder>();
        try {
            foreach(var c in context.View.Colonies.OrderBy(c=>c.Id)) {
                var d=c.Development??throw new InvalidOperationException("Missing colony development projection.");
                var issues=ImmutableArray.CreateBuilder<GovernorIssue>();
                ProductionTarget? target=c.ProjectId is {} id?new(c.ProjectKind,id):null;
                int? token=d.ActiveQueueToken;var origin=d.Origin;
                int requestLimit=Fleets.RequestQueueAuthority.LastAuthorizedQueueIndex(context.View,c.Id);
                if(target is null&&(d.ConstructionEnabled||requestLimit>=0)) {
                    string? locked=null;
                    foreach(var entry in d.Queue.Take(d.ConstructionEnabled?d.Queue.Length:requestLimit+1)) {
                        if(!Fleets.RequestQueueAuthority.MaySelect(context.View,c.Id,entry.Token))continue;
                        if(Legal(entry.Target)){target=entry.Target;token=entry.Token;origin=ProductionOrigin.Queue;break;}
                        locked??=entry.Target.ContentId;
                    }
                    if(target is null&&d.ConstructionEnabled&&d.Template is {} template)foreach(string building in template.Definition.FoundationIds.Concat(template.Definition.DevelopmentIds)) {
                        if(c.CompletedBuildingIds.Contains(building))continue;
                        var proposed=new ProductionTarget(ProductionKinds.Building,building);
                        if(Legal(proposed)){target=proposed;origin=ProductionOrigin.Template;break;}
                        locked??=building;
                    }
                    if(target is null)issues.Add(new(locked is null?"plan_complete":"construction_locked",locked));
                }
                var workforce=c.Workforce;
                if(d.WorkforceEnabled) {
                    var template=d.Template??throw new InvalidOperationException("Workforce automation requires template.");
                    var profile=context.Content.Species[context.View.SpeciesId!];
                    var star=context.View.Stars.Single(s=>s.Chart.Id==c.StarId);
                    var planet=star.Planets!.Value.Single(p=>p.Id==c.PlanetId);
                    long rate=context.Content.PlanetTypes[planet.PlanetTypeId].SupportPerWorker;
                    Int128 flat=0,flatProduction=0;
                    foreach(string building in c.CompletedBuildingIds)foreach(var effect in context.Content.Buildings[building].Effects){if(effect.Kind==EffectKinds.Support)flat+=effect.Amount;if(effect.Kind==EffectKinds.Production)flatProduction+=effect.Amount;}
                    Int128 required=(Int128)c.Population*profile.SupportPerPopulation;
                    if(template.Definition.SupportTarget==SupportTargetMode.CappedGrowth&&c.Population<c.Economy.Growth.Capacity)required+=(Int128)profile.MaxSurplusGrowth*profile.SupportPerGrowthPoint;
                    Int128 need=required-flat;int support=need<=0?0:rate==0?c.Population:checked((int)Int128.Min(c.Population,(need+rate-1)/rate));
                    if(target is {Kind:ProductionKinds.Vessel} vessel&&context.Content.Vessels.TryGetValue(vessel.ContentId,out var role))support=Math.Max(support,Math.Min(c.Population,role.EmbarkPopulation));
                    int available=c.Population-support;int pw=template.Definition.ProductionWeight,rw=template.Definition.ResearchWeight;int total=pw+rw;
                    if(total<=0||pw is <0 or >100||rw is <0 or >100)throw new InvalidOperationException("Invalid template weights.");
                    long p=(long)available*pw,r=(long)available*rw;int production=(int)(p/total),research=(int)(r/total);
                    if(production+research<available){if(p%total>=r%total)production++;else research++;}
                    if(target is not null&&flatProduction==0&&available>0&&production==0){production++;research--;}
                    workforce=new(support,production,research);
                }
                var forecast=context.Forecast(c.Id,workforce,target)??throw new OverflowException("Own colony forecast is invalid or exceeds supported arithmetic.");
                if(forecast.SupportDeficit>0)issues.Add(new("support_shortage",null));
                if(forecast.Production.BlockedReasonCode=="awaiting_embarkation")issues.Add(new("embarkation_wait",target?.ContentId));
                if(Rules.HasCrises(context.View.RulesVersion)&&forecast.Production.BlockedReasonCode=="quarantine")issues.Add(new("quarantine_wait",target?.ContentId));
                orders.Add(new(c.Id,target,workforce,token,target is null?ProductionOrigin.None:origin,issues.ToImmutable()));
                bool Legal(ProductionTarget t)=>t.Kind==ProductionKinds.Building?context.Content.Buildings.TryGetValue(t.ContentId,out var b)&&!c.CompletedBuildingIds.Contains(t.ContentId)&&b.PrerequisiteTechnologyIds.All(context.View.Research.KnownTechnologyIds.Contains):t.Kind==ProductionKinds.ShipDesign?ShipDesigns.ShipDesignTargets.Available(context.View,t):t.Kind==ProductionKinds.Vessel&&context.View.AvailableVesselRoleIds.Contains(t.ContentId)&&context.Content.Vessels.ContainsKey(t.ContentId);
            }
            return new(true,orders.ToImmutable(),null);
        }catch(Exception ex) when(ex is OverflowException or InvalidOperationException or ArgumentException or System.Collections.Generic.KeyNotFoundException){return new(false,ImmutableArray<GovernorColonyOrder>.Empty,ex.Message);}
    }
}
