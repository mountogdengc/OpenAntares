using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Knowledge;

public static class EmpireViewProjector
{
    public static EmpireView Project(ContentSet content, GameState state, EmpireId viewer)
    {
        var e = state.FindEmpire(viewer) ?? throw new ArgumentException("Empire not found.", nameof(viewer));
        // Forecast only this empire; foreign reserves/outputs cannot affect permitted rendering.
        var owned = state.Clone(); owned.Colonies.RemoveAll(c => c.EmpireId != viewer); owned.Empires.RemoveAll(e => e.Id != viewer);
        var forecast = EconomyCalculator.Calculate(content, owned);
        var charts = state.Stars.Select(s => new ChartedStar(s.Id, s.Name, s.X, s.Y, s.RegionId)).ToImmutableArray();
        bool legacy = state.RulesVersion == Rules.LegacyVersion;
        var stars = charts.Select(star =>
        {
            bool current = legacy || ObservationUpdater.IsCurrent(state, viewer, star.Id);
            var observation = legacy ? ObservationUpdater.Observe(state, star.Id, state.Turn) : e.Surveys.FirstOrDefault(o => o.StarId == star.Id);
            ImmutableArray<KnownPlanetView>? planets = observation is null ? null : observation.Planets.Select(p =>
            {
                var t = content.PlanetTypes[p.PlanetTypeId];
                return new KnownPlanetView(p.PlanetId, p.Orbit, p.PlanetTypeId, t.SupportPerWorker, t.ProductionPerWorker, t.ResearchPerWorker, t.Colonizable, p.ColonyId, p.OwnerId);
            }).ToImmutableArray();
            return new StarView(star, planets, observation?.LastObservedTurn, current);
        }).ToImmutableArray();
        var colonies = owned.Colonies.Select(c => new OwnedColonyView(c.Id, c.PlanetId, state.FindPlanet(c.PlanetId)!.StarId, c.EmpireId, c.Population, c.Workforce, c.GrowthProgress, c.ProductionReserve, c.ProjectId, c.ProjectKind, c.CompletedBuildingIds.ToImmutableArray(), forecast.Colony(c.Id)) { Turn = state.Turn, RulesVersion = state.RulesVersion, KnownTechnologyIds = e.KnownTechnologyIds.ToImmutableArray() }).ToImmutableArray();
        var map = new GalaxyMapView(charts, state.GalaxySeed, state.GalaxyShapeId, state.GalaxyWidth, state.GalaxyHeight, colonies.Select(c => c.StarId).Distinct().OrderBy(id => id).ToImmutableArray());
        var vessels = state.Vessels.Where(v => v.EmpireId == viewer).Select(v => new OwnedVesselView(v.Id, v.EmpireId, v.RoleId, v.StationaryStarId, v.Transit, v.PendingMove, v.PendingFounding, v.CarriedPopulation)).ToImmutableArray();
        var foreign = state.Vessels.Where(v => v.EmpireId != viewer && v.StationaryStarId is { } star && ObservationUpdater.IsCurrent(state, viewer, star)).Select(v => new ForeignVesselView(v.Id, v.EmpireId, v.RoleId, v.StationaryStarId!.Value)).ToImmutableArray();
        map = map with { Units = vessels.Select(v => new MapVesselMarker(v.Id, v.RoleId, true, v.StationaryStarId, v.Transit?.Origin, v.Transit?.Destination, v.Transit?.RemainingTurns ?? 0)).Concat(foreign.Select(v => new MapVesselMarker(v.Id, v.RoleId, false, v.StarId, null, null, 0))).ToImmutableArray() };
        var names = colonies.ToImmutableDictionary(c => c.Id.ToString(), c => $"{state.FindStar(c.StarId)!.Name} {state.FindPlanet(c.PlanetId)!.Orbit}");
        return new(state.Turn, viewer, state.RulesVersion, map, stars, colonies, vessels, foreign, new(e.Id, e.ResearchReserve, e.ResearchTargetId, e.KnownTechnologyIds.ToImmutableArray(), forecast.Empire(e.Id)) { Turn = state.Turn, Ready = e.Ready, ColonyNames = names });
    }
    public static EmpireTurnReportView ProjectReport(ContentSet content, GameState state, Turns.TurnReport report, EmpireId viewer)
    {
        if (state.FindEmpire(viewer) is null) throw new ArgumentException("Empire not found.", nameof(viewer));
        var colonies = report.Economy.Colonies.Where(c => c.Empire == viewer).Select(c =>
        {
            var planet = state.FindPlanet(state.FindColony(c.Colony)!.PlanetId)!; var star = state.FindStar(planet.StarId)!;
            return new ColonyReportView(c, c.Colony, star.Id, $"{star.Name} {planet.Orbit}");
        }).ToImmutableArray();
        return new(report.FromTurn, report.ToTurn, colonies, report.Economy.Empire(viewer), report.ExpansionEvents.Where(e => e.Owner == viewer).ToImmutableArray(), state.Stars.ToImmutableDictionary(s => s.Id, s => s.Name));
    }
}
