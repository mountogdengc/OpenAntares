using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Development;
using System;
using System.Collections.Generic;
using OpenAntares.Simulation.Species;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Knowledge;

public static class EmpireViewProjector
{
    public static EmpireView Project(ContentSet content, GameState state, EmpireId viewer)
        => ProjectBase(content,state,viewer) with{Scouting=Rules.HasScouting(state.RulesVersion)?Scouting.ScoutQueries.Project(content,state,viewer):null};
    internal static EmpireView ProjectBase(ContentSet content, GameState state, EmpireId viewer)
    {
        var e = state.FindEmpire(viewer) ?? throw new ArgumentException("Empire not found.", nameof(viewer));
        // Forecast only this empire; foreign reserves/outputs cannot affect permitted rendering.
        var owned = state.Clone(); owned.Colonies.RemoveAll(c => c.EmpireId != viewer); owned.Empires.RemoveAll(e => e.Id != viewer);
        owned.Vessels.RemoveAll(v=>v.EmpireId!=viewer);
        var forecast = EconomyCalculator.Calculate(content, owned);
        var crisis=Rules.HasCrises(state.RulesVersion)?CrisisQueries.Project(content,owned,viewer):null;
        var profile = EconomyProfile.For(content,owned,viewer);
        var roles = Rules.HasSpecies(state.RulesVersion) ? content.Species[e.SpeciesId!].AllowedVesselIds :
            Rules.HasExpansion(state.RulesVersion) ? content.Vessels.Keys.Where(id => Rules.VesselAvailable(content, state.RulesVersion, id)).ToImmutableArray() : ImmutableArray<string>.Empty;
        if (Rules.HasCombat(state.RulesVersion)) roles = roles.Concat(content.Vessels.Values.Where(v => v.CombatOnly).Select(v => v.Id)).Distinct(StringComparer.Ordinal).ToImmutableArray();
        var charts = state.Stars.Select(s => new ChartedStar(s.Id, s.Name, s.X, s.Y, s.RegionId)).ToImmutableArray();
        bool legacy = state.RulesVersion == Rules.LegacyVersion;
        var stars = charts.Select(star =>
        {
            bool current = legacy || ObservationUpdater.IsCurrent(state, viewer, star.Id);
            var observation = legacy ? ObservationUpdater.Observe(state, star.Id, state.Turn) : e.Surveys.FirstOrDefault(o => o.StarId == star.Id);
            ImmutableArray<KnownPlanetView>? planets = observation is null ? null : observation.Planets.Select(p =>
            {
                var t = content.PlanetTypes[p.PlanetTypeId];
                return new KnownPlanetView(p.PlanetId, p.Orbit, p.PlanetTypeId, t.SupportPerWorker, t.ProductionPerWorker, t.ResearchPerWorker, t.Colonizable, p.ColonyId, p.OwnerId) { EnvironmentId = p.EnvironmentId, Appearance = p.Appearance, Potential = ProjectPotential(content,owned,viewer,p) };
            }).ToImmutableArray();
            return new StarView(star, planets, observation?.LastObservedTurn, current);
        }).ToImmutableArray();
        var colonies = owned.Colonies.Select(c => new OwnedColonyView(c.Id, c.PlanetId, state.FindPlanet(c.PlanetId)!.StarId, c.EmpireId, c.Population, c.Workforce, c.GrowthProgress, c.ProductionReserve, c.ProjectId, c.ProjectKind, c.CompletedBuildingIds.ToImmutableArray(), forecast.Colony(c.Id)) { Crisis=crisis?.ColonyConditions.FirstOrDefault(x=>x.Location.Colony==c.Id), Turn = state.Turn, Development = c.Development is {} cd ? new(cd.Template,cd.WorkforceEnabled,cd.ConstructionEnabled,cd.Queue.ToImmutableArray(),cd.ActiveQueueToken,cd.Origin) { NextQueueToken=Rules.HasFleets(state.RulesVersion)?cd.NextQueueToken:null } : null, RulesVersion = state.RulesVersion, KnownTechnologyIds = e.KnownTechnologyIds.ToImmutableArray(), SpeciesId = e.SpeciesId, HabitatMode = c.HabitatMode, PopulationUnitLabel = profile.PopulationUnitLabel, AvailableVesselRoleIds = roles }).ToImmutableArray();
        var map = new GalaxyMapView(charts, state.GalaxySeed, state.GalaxyShapeId, state.GalaxyWidth, state.GalaxyHeight, colonies.Select(c => c.StarId).Distinct().OrderBy(id => id).ToImmutableArray());
        var vessels = state.Vessels.Where(v => v.EmpireId == viewer).Select(v => new OwnedVesselView(v.Id, v.EmpireId, v.RoleId, v.StationaryStarId, v.Transit, v.PendingMove, v.PendingFounding, v.CarriedPopulation)
        {
            Crisis=crisis?.Vessels.FirstOrDefault(x=>x.Id==v.Id),
            FoundingPreviews = content.Vessels.TryGetValue(v.RoleId,out var role) && role.CanFoundColony ? e.Surveys.SelectMany(o=>o.Planets).OrderBy(p=>p.PlanetId).Select(p=>Colonization.FoundingRules.Preview(content,state,viewer,v.Id,p.PlanetId)).ToImmutableArray() : ImmutableArray<Colonization.FoundingPreview>.Empty
        }).ToImmutableArray();
        var foreign = state.Vessels.Where(v => v.EmpireId != viewer && v.StationaryStarId is { } star && ObservationUpdater.IsCurrent(state, viewer, star)).Select(v => new ForeignVesselView(v.Id, v.EmpireId, v.RoleId, v.StationaryStarId!.Value)).ToImmutableArray();
        map = map with { Units = vessels.Select(v => new MapVesselMarker(v.Id, v.RoleId, true, v.StationaryStarId, v.Transit?.Origin, v.Transit?.Destination, v.Transit?.RemainingTurns ?? 0)).Concat(foreign.Select(v => new MapVesselMarker(v.Id, v.RoleId, false, v.StarId, null, null, 0))).ToImmutableArray() };
        var names = colonies.ToImmutableDictionary(c => c.Id.ToString(), c => $"{state.FindStar(c.StarId)!.Name} {state.FindPlanet(c.PlanetId)!.Orbit}");
        return new EmpireView(state.Turn, viewer, state.RulesVersion, map, stars, colonies, vessels, foreign, new(e.Id, e.ResearchReserve, e.ResearchTargetId, e.KnownTechnologyIds.ToImmutableArray(), forecast.Empire(e.Id)) { RulesVersion = state.RulesVersion, Turn = state.Turn, Ready = e.Ready, ColonyNames = names }) { Combat=Combat.CombatQueries.Project(state,viewer), Crisis=crisis, Fleets=Rules.HasFleets(state.RulesVersion)?Fleets.FleetQueries.Project(content,state,viewer):null, Development = e.Development is {} ed ? new(ed.CustomTemplates.ToImmutableArray(),ed.NewColonyPlan,ed.PendingReport,ed.CompletedReports.ToImmutableArray()) : null, SpeciesId = e.SpeciesId, PopulationUnitLabel = profile.PopulationUnitLabel, AvailableVesselRoleIds = roles, Contacts = Rules.HasRivals(state.RulesVersion) ? e.Contacts.Select(c => new ContactView(c.OtherEmpireId, c.SpeciesId, content.Species[c.SpeciesId].Name, content.Species[c.SpeciesId].CultureName, c.FirstObservedTurn)).ToImmutableArray() : ImmutableArray<ContactView>.Empty };
    }
    private static PlanetPotentialView? ProjectPotential(ContentSet content, GameState owned, EmpireId viewer, PlanetObservation observation)
    {
        if(!Rules.HasSpecies(owned.RulesVersion)) return null;
        var type=content.PlanetTypes[observation.PlanetTypeId];
        var profile=EconomyProfile.For(content,owned,viewer);
        var species=content.Species[profile.SpeciesId!];
        var buildings=new List<BuildingDefinition>();
        bool habitable=type.Colonizable && type.SupportPerWorker > 0;
        return new(EconomyCalculator.Output(type.Id,1,type.SupportPerWorker,buildings,EffectKinds.Support),
            EconomyCalculator.Output(type.Id,1,type.ProductionPerWorker,buildings,EffectKinds.Production,profile),
            EconomyCalculator.Output(type.Id,1,type.ResearchPerWorker,buildings,EffectKinds.Research,profile),
            species.BaseCapacity,species.SealedBaseCapacity,
            habitable && observation.EnvironmentId is { } environment && SpeciesRules.HabitatAllowed(content,owned,viewer,environment,HabitatModes.Native),
            habitable && species.AllowsSealedHabitats);
    }
    public static EmpireTurnReportView ProjectReport(ContentSet content, GameState state, Turns.TurnReport report, EmpireId viewer)
    {
        var owner = state.FindEmpire(viewer) ?? throw new ArgumentException("Empire not found.", nameof(viewer));
        var colonies = report.Economy.Colonies.Where(c => c.Empire == viewer).Select(c =>
        {
            if(Rules.HasCrises(state.RulesVersion))
            {
                if(!report.ColonyLocations.TryGetValue(c.Colony,out var location))throw new InvalidOperationException("Missing original owned colony report location.");
                return new ColonyReportView(c,c.Colony,location.Star,location.Name);
            }
            var planet = state.FindPlanet(state.FindColony(c.Colony)!.PlanetId)!; var star = state.FindStar(planet.StarId)!;
            return new ColonyReportView(c, c.Colony, star.Id, $"{star.Name} {planet.Orbit}");
        }).ToImmutableArray();
        var encounters = Rules.HasRivals(state.RulesVersion) ? report.EncounterEvents.Where(e => e.Viewer == viewer).Select(e =>
        {
            var contact = owner.Contacts.FirstOrDefault(c => c.OtherEmpireId == e.OtherEmpireId);
            var species = contact is not null && content.Species.TryGetValue(contact.SpeciesId, out var known) ? known : null;
            return e with { SpeciesId = contact?.SpeciesId, SpeciesName = species?.Name, CultureName = species?.CultureName };
        }).ToImmutableArray() : ImmutableArray<Turns.EncounterEvent>.Empty;
        return new(report.FromTurn, report.ToTurn, colonies, report.Economy.Empire(viewer), report.ExpansionEvents.Where(e => e.Owner == viewer).ToImmutableArray(), state.Stars.ToImmutableDictionary(s => s.Id, s => s.Name)) { FleetEvents=report.FleetEvents.Where(e=>e.Owner==viewer).ToImmutableArray(), EncounterEvents = encounters, CrisisEvents=report.CrisisEvents.Where(e=>e.Key.Owner==viewer).ToImmutableArray(),ScoutEvents=report.ScoutEvents.Where(e=>e.Owner==viewer).ToImmutableArray(),Scouting=Rules.HasScouting(state.RulesVersion)?Scouting.ScoutQueries.Project(content,state,viewer):null };
    }
}
