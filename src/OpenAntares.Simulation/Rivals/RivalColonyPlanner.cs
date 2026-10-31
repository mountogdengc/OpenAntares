using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Rivals;

internal sealed record RivalColonyGoal(ColonyId Colony, ProductionTarget? Target, Workforce Workforce);
internal sealed record RivalWorld(KnownPlanetView Planet, StarId Star, VesselDefinition Role);

internal static class RivalColonyPlanner
{
    internal static IReadOnlyList<RivalColonyGoal> Plan(IRivalPlanningContext context)
    {
        var colonies = context.View.Colonies.OrderBy(c => c.Id).ToArray();
        var targets = colonies.ToDictionary(c => c.Id, c => LocalTarget(context,c));
        bool IsScout(string id) => context.Content.Vessels.TryGetValue(id,out var role) && !role.CanFoundColony;
        bool haveScout = context.View.Vessels.Any(v => IsScout(v.RoleId));
        var scoutProjects = colonies.Where(c => c.ProjectKind == ProductionKinds.Vessel && c.ProjectId is { } id && IsScout(id)).Select(c => c.Id).ToHashSet();
        ColonyId? newScout = null;
        if (!haveScout && scoutProjects.Count == 0 && context.Content.Expansion is { } expansion)
        {
            foreach (var colony in colonies)
            {
                if (!MinimumSupport(context,colony).Supported) continue;
                var target = new ProductionTarget(ProductionKinds.Vessel,expansion.StartingVesselId);
                var workers = WorkforceFor(context,colony,target);
                if (context.Forecast(colony.Id,workers,target)?.NetProduction.Total is > 0)
                { targets[colony.Id] = target; newScout = colony.Id; break; }
            }
        }
        bool liveFounder = context.View.Vessels.Any(v => context.Content.Vessels[v.RoleId].CanFoundColony);
        if (!liveFounder)
        {
            var worlds = Candidates(context).ToArray();
            var combinations = new List<(OwnedColonyView Source,RivalWorld World,int Travel)>();
            foreach (var colony in colonies)
            {
                if (newScout == colony.Id || (!haveScout && scoutProjects.Contains(colony.Id))) continue;
                foreach (var world in worlds)
                {
                    if (colony.Economy.Growth.Capacity < Math.Max(world.Role.MinimumSourcePopulation,(long)world.Role.EmbarkPopulation+1)) continue;
                    var target = new ProductionTarget(ProductionKinds.Vessel,world.Role.Id);
                    var forecast = context.Forecast(colony.Id,WorkforceFor(context,colony,target),target);
                    if (forecast is null || (forecast.NetProduction.Total <= 0 && colony.SpendableProduction < world.Role.Cost)) continue;
                    if (context.TravelTurns(colony.StarId,world.Star,world.Role.Id) is { } travel) combinations.Add((colony,world,travel));
                }
            }
            var retained = combinations.Where(x => x.Source.ProjectKind == ProductionKinds.Vessel && x.Source.ProjectId == x.World.Role.Id)
                .OrderBy(x => x.Source.Id).ThenBy(x => x.Travel).ThenBy(x => x.World.Planet.Id).FirstOrDefault();
            var chosen = retained.Source is not null ? retained : newScout is null ? combinations.OrderBy(x => x.Travel)
                .ThenBy(x => x.World.Role.Cost).ThenBy(x => x.World.Planet.Id).ThenBy(x => x.Source.Id).ThenBy(x => x.World.Role.Id,StringComparer.Ordinal).FirstOrDefault() : default;
            if (chosen.Source is not null) targets[chosen.Source.Id] = new(ProductionKinds.Vessel,chosen.World.Role.Id);
        }
        return colonies.Select(c => new RivalColonyGoal(c.Id,targets[c.Id],WorkforceFor(context,c,targets[c.Id]))).ToArray();
    }

    internal static IEnumerable<RivalWorld> Candidates(IRivalPlanningContext context, string? roleId = null)
    {
        var roles = context.View.AvailableVesselRoleIds.Where(id => roleId is null || id == roleId)
            .Select(id => context.Content.Vessels[id]).Where(r => r.CanFoundColony).OrderBy(r => r.Id,StringComparer.Ordinal).ToArray();
        foreach (var star in context.View.Stars.OrderBy(s => s.Chart.Id))
        foreach (var planet in star.Planets ?? System.Collections.Immutable.ImmutableArray<KnownPlanetView>.Empty)
        {
            if (planet.OwnerId is not null || !planet.Colonizable||context.View.Scouting?.Habitats.Any(h=>h.Star==star.Chart.Id)==true) continue;
            foreach (var role in roles)
                if (context.PreviewSettlement(planet.Id,role.Id) is { RejectionCode: null, Economy: { } economy } && economy.SupportProduced.Total >= economy.SupportRequired.Total)
                    yield return new(planet,star.Chart.Id,role);
        }
    }

    private static (int Workers,bool Supported) MinimumSupport(IRivalPlanningContext context, OwnedColonyView colony, string? building = null)
    {
        bool Enough(int workers) => context.Forecast(colony.Id,new(workers,0,colony.Population-workers),null,building) is { } forecast
            && forecast.SupportProduced.Total >= forecast.SupportRequired.Total;
        if (!Enough(colony.Population)) return (colony.Population,false);
        int low = 0, high = colony.Population;
        while (low < high)
        {
            int middle = low + (int)(((long)high-low)/2);
            if (Enough(middle)) high = middle; else low = middle+1;
        }
        return (low,true);
    }

    private static ProductionTarget? LocalTarget(IRivalPlanningContext context, OwnedColonyView colony)
    {
        bool LegalBuilding(BuildingDefinition b) => !colony.CompletedBuildingIds.Contains(b.Id) && b.PrerequisiteTechnologyIds.All(context.View.Research.KnownTechnologyIds.Contains);
        if(Rules.HasCrises(context.View.RulesVersion)&&colony.ProjectKind==ProductionKinds.Vessel&&colony.ProjectId is {} waiting&&context.Content.Vessels.TryGetValue(waiting,out var cargo)&&cargo.EmbarkPopulation>0&&context.View.Crisis?.ColonyConditions.Any(c=>c.Quarantined&&c.Location.Star==colony.StarId)==true)return new(ProductionKinds.Vessel,waiting);
        var support = MinimumSupport(context,colony);
        ProductionTarget? existing = null;
        if (colony.ProjectKind == ProductionKinds.Building && colony.ProjectId is { } id && context.Content.Buildings.TryGetValue(id,out var building) && LegalBuilding(building)) existing = new(ProductionKinds.Building,id);
        if (colony.ProjectKind == ProductionKinds.Vessel && colony.ProjectId is { } roleId && context.Content.Vessels.TryGetValue(roleId,out var role) && !role.CanFoundColony && context.View.AvailableVesselRoleIds.Contains(roleId)) existing = new(ProductionKinds.Vessel,roleId);
        if (existing is not null && support.Supported) return existing;
        var baseline = context.Forecast(colony.Id,new(colony.Population,0,0),null);
        int Rank(BuildingDefinition b)
        {
            var after = context.Forecast(colony.Id,new(colony.Population,0,0),null,b.Id);
            if (after is null || baseline is null) return int.MaxValue;
            bool improvedSupport = after.SupportProduced.Total > baseline.SupportProduced.Total;
            if (improvedSupport && (MinimumSupport(context,colony,b.Id).Workers < support.Workers || (!support.Supported && after.SupportDeficit < baseline.SupportDeficit))) return 0;
            if (colony.Population >= colony.Economy.Growth.Capacity && after.Growth.Capacity > baseline.Growth.Capacity) return 1;
            if (after.GrossProduction.Total > baseline.GrossProduction.Total) return 2;
            if (after.GrossResearch.Total > baseline.GrossResearch.Total) return 3;
            return improvedSupport ? 4 : int.MaxValue;
        }
        var choice = context.Content.Buildings.Values.Where(LegalBuilding).Select(b => (Building:b,Rank:Rank(b))).Where(x => x.Rank != int.MaxValue)
            .OrderBy(x => x.Rank).ThenBy(x => x.Building.Cost).ThenBy(x => x.Building.Id,StringComparer.Ordinal).FirstOrDefault();
        if (existing is not null && (choice.Building is null || choice.Rank != 0)) return existing;
        return choice.Building is null ? null : new(ProductionKinds.Building,choice.Building.Id);
    }

    private static Workforce WorkforceFor(IRivalPlanningContext context, OwnedColonyView colony, ProductionTarget? target)
    {
        int support = MinimumSupport(context,colony).Workers;
        if (target is { Kind: ProductionKinds.Vessel } && context.Content.Vessels[target.ContentId] is { CanFoundColony: true } role)
            support = Math.Max(support,Math.Min(colony.Population,role.EmbarkPopulation));
        Workforce Split(int workers)
        {
            int remaining = colony.Population-workers;
            if (remaining == 1) return target is null && context.View.Research.TargetId is not null ? new(workers,0,1) : new(workers,1,0);
            int production = remaining/2+remaining%2;
            return new(workers,production,remaining-production);
        }
        if (support == colony.Population && colony.Population >= 2 && target is { Kind: ProductionKinds.Building } &&
            context.Content.Buildings[target.ContentId].Effects.Any(e => e.Kind == EffectKinds.Support && e.Amount > 0))
        {
            var bridge = new Workforce(colony.Population-1,1,0);
            if (context.Forecast(colony.Id,bridge,target)?.NetProduction.Total is > 0) return bridge;
        }
        var workers = Split(support);
        if (context.Policy.GrowthSupportWorkers == 1 && support < colony.Population-1 && colony.Population < colony.Economy.Growth.Capacity &&
            context.Forecast(colony.Id,workers,target) is { } original && context.Forecast(colony.Id,Split(support+1),target) is { } extra &&
            extra.GrowthEarned.Total > original.GrowthEarned.Total) workers = Split(support+1);
        return workers;
    }
}
