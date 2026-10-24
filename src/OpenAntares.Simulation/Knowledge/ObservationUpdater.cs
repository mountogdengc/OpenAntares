using System.Linq;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Knowledge;

public static class ObservationUpdater
{
    public static bool IsCurrent(GameState s, EmpireId empire, StarId star) => s.Colonies.Any(c => c.EmpireId == empire && s.FindPlanet(c.PlanetId)?.StarId == star) || s.Vessels.Any(v => v.EmpireId == empire && v.StationaryStarId == star);
    public static void Refresh(GameState candidate, int observedTurn)
    {
        if (!Rules.HasExpansion(candidate.RulesVersion)) return;
        foreach (var e in candidate.Empires)
        {
            foreach (var star in candidate.Stars.Where(star => IsCurrent(candidate, e.Id, star.Id)))
            {
                e.Surveys.RemoveAll(o => o.StarId == star.Id);
                e.Surveys.Add(Observe(candidate, star.Id, observedTurn));
            }
            e.Surveys.Sort((a, b) => a.StarId.CompareTo(b.StarId));
        }
    }
    internal static SystemObservation Observe(GameState s, StarId star, int turn) => new()
    {
        StarId = star,
        LastObservedTurn = turn,
        Planets = s.Planets.Where(p => p.StarId == star).OrderBy(p => p.Id).Select(p =>
        {
            var c = s.Colonies.FirstOrDefault(c => c.PlanetId == p.Id);
            return new PlanetObservation(p.Id, p.Orbit, p.PlanetTypeId, c?.Id, c?.EmpireId) { EnvironmentId = p.EnvironmentId };
        }).ToList()
    };
}
