using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
namespace OpenAntares.Simulation.State;
internal static class ExpansionStateValidator
{
    internal static void Validate(ContentSet content, GameState s, List<string> errors)
    {
        if (s.NextEntityId < 1) errors.Add("NextEntityId must be positive.");
        if (s.RulesVersion == Rules.LegacyVersion)
        {
            if (s.Vessels.Count != 0 || s.Empires.Any(e=>e.Surveys.Count!=0) || s.Colonies.Any(c=>c.ProjectKind!=ProductionKinds.Building)) errors.Add("Legacy games cannot contain expansion state.");
            return;
        }
        if (s.RulesVersion != Rules.ExpansionVersion) return;
        if (content.Expansion is null || content.Vessels.Count == 0) errors.Add("Expansion rules require embedded expansion content.");
        foreach (var c in s.Colonies)
            if (c.ProjectKind is not (ProductionKinds.Building or ProductionKinds.Vessel) || (c.ProjectId is null && c.ProjectKind!=ProductionKinds.Building)) errors.Add($"{c.Id}: invalid production kind.");
        foreach (var v in s.Vessels)
        {
            string label = v.Id.ToString();
            if (s.FindEmpire(v.EmpireId) is null) errors.Add($"{label}: owner not defined.");
            if (!content.Vessels.TryGetValue(v.RoleId, out var role)) errors.Add($"{label}: role not defined.");
            else if (role.Speed <= 0 || v.CarriedPopulation != role.EmbarkPopulation) errors.Add($"{label}: invalid speed or cargo.");
            if (v.StationaryStarId.HasValue == (v.Transit is not null)) errors.Add($"{label}: must be stationary XOR in transit.");
            if (v.StationaryStarId is { } star && s.FindStar(star) is null) errors.Add($"{label}: stationary star missing.");
            if (v.PendingMove.HasValue && v.PendingFounding.HasValue) errors.Add($"{label}: mutually exclusive orders.");
            if (v.Transit is { } t)
            {
                if (s.FindStar(t.Origin) is null || s.FindStar(t.Destination) is null || t.Origin==t.Destination || t.TotalTurns<1 || t.RemainingTurns<1 || t.RemainingTurns>t.TotalTurns) errors.Add($"{label}: invalid transit.");
                if (v.PendingMove.HasValue || v.PendingFounding.HasValue) errors.Add($"{label}: transit cannot hold station orders.");
            }
            if (v.PendingMove is { } move && (s.FindStar(move) is null || move==v.StationaryStarId)) errors.Add($"{label}: invalid pending destination.");
            if (v.PendingFounding is { } target)
            {
                var planet=s.FindPlanet(target);
                if (role is null || !role.CanFoundColony || planet is null || planet.StarId!=v.StationaryStarId || !content.PlanetTypes.TryGetValue(planet.PlanetTypeId,out var type) || !type.Colonizable || type.SupportPerWorker<=0 || content.Rules.BaseColonyCapacity<v.CarriedPopulation || s.Colonies.Any(c=>c.PlanetId==target) || !(s.FindEmpire(v.EmpireId)?.Surveys.Any(o=>o.Planets.Any(p=>p.PlanetId==target))??false)) errors.Add($"{label}: invalid founding intent.");
            }
        }
        foreach (var group in s.Vessels.Where(v=>v.PendingFounding.HasValue).GroupBy(v=>(v.EmpireId,v.PendingFounding)))
            if (group.Count()>1) errors.Add("Duplicate same-empire founding reservation.");
        foreach (var e in s.Empires)
        {
            int prev=0;
            foreach (var o in e.Surveys)
            {
                if(o.StarId.Value<=prev || s.FindStar(o.StarId) is null || o.LastObservedTurn<0 || o.LastObservedTurn>s.Turn) errors.Add($"{e.Id}: invalid survey order/reference/turn.");
                prev=o.StarId.Value;int pp=0;
                foreach(var p in o.Planets)
                {
                    var truth=s.FindPlanet(p.PlanetId);
                    if(p.PlanetId.Value<=pp || truth is null || truth.StarId!=o.StarId || truth.Orbit!=p.Orbit || truth.PlanetTypeId!=p.PlanetTypeId || !content.PlanetTypes.ContainsKey(p.PlanetTypeId) || p.ColonyId.HasValue!=p.OwnerId.HasValue || (p.OwnerId is {} owner && s.FindEmpire(owner) is null)) errors.Add($"{e.Id}: invalid planet observation.");
                    pp=p.PlanetId.Value;
                }
            }
        }
    }
}
