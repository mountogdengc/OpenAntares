using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;

namespace OpenAntares.Simulation.Knowledge;

public static class ContactUpdater
{
    /// <summary>Run after ordinary observations refresh, on an unpublished candidate only.</summary>
    public static ImmutableArray<EncounterEvent> Refresh(GameState beforeRefresh, GameState candidate)
    {
        if (!Rules.HasRivals(candidate.RulesVersion)) return ImmutableArray<EncounterEvent>.Empty;
        var events = new List<EncounterEvent>();
        foreach (var viewer in candidate.Empires.OrderBy(e => e.Id))
        {
            var current = viewer.Surveys.Where(o => ObservationUpdater.IsCurrent(candidate, viewer.Id, o.StarId)).OrderBy(o => o.StarId).ToArray();
            var seen = current.SelectMany(o => o.Planets.Where(p => p.OwnerId is { } id && id != viewer.Id)
                    .Select(p => (Owner: p.OwnerId!.Value, Star: o.StarId)))
                .Concat(candidate.Vessels.Where(v => v.EmpireId != viewer.Id && v.StationaryStarId is { } star &&
                    ObservationUpdater.IsCurrent(candidate, viewer.Id, star)).Select(v => (Owner: v.EmpireId, Star: v.StationaryStarId!.Value)));
            foreach (var encounter in seen.GroupBy(x => x.Owner).OrderBy(g => g.Key))
            {
                if (viewer.Contacts.Any(c => c.OtherEmpireId == encounter.Key)) continue;
                if (candidate.FindEmpire(encounter.Key)?.SpeciesId is not { } species) continue;
                viewer.Contacts.Add(new(encounter.Key, species, candidate.Turn));
                events.Add(new("first_contact", viewer.Id, encounter.Key, encounter.OrderBy(x => x.Star).First().Star, null, candidate.Turn));
            }
            viewer.Contacts.Sort((a, b) => a.OtherEmpireId.CompareTo(b.OtherEmpireId));
            var previous = beforeRefresh.FindEmpire(viewer.Id);
            foreach (var survey in current)
            foreach (var planet in survey.Planets.OrderBy(p => p.PlanetId))
            {
                if (planet.OwnerId is not { } owner || owner == viewer.Id) continue;
                var old = previous?.Surveys.FirstOrDefault(o => o.StarId == survey.StarId)?.Planets.FirstOrDefault(p => p.PlanetId == planet.PlanetId);
                if (old?.OwnerId != owner || old.ColonyId != planet.ColonyId)
                    events.Add(new("observed_colony", viewer.Id, owner, survey.StarId, planet.PlanetId, candidate.Turn) { PlanetOrbit = planet.Orbit });
            }
        }
        return events.OrderBy(e => e.Viewer).ThenBy(e => e.OtherEmpireId).ThenBy(e => e.Kind, System.StringComparer.Ordinal)
            .ThenBy(e => e.StarId).ThenBy(e => e.PlanetId).ToImmutableArray();
    }
}
