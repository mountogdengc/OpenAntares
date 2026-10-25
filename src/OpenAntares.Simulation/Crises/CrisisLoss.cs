using System;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Crises;

public static class CrisisLoss
{
    public static void Remove(GameState candidate,ColonyId colony,CrisisLocation location)
    {
        var c=candidate.FindColony(colony)??throw new ArgumentException("Missing colony.",nameof(colony));
        if(!Rules.HasCrises(candidate.RulesVersion)||location!=CrisisQueries.Location(candidate,c))throw new ArgumentException("Certified own location required.",nameof(location));
        var retired=candidate.FindEmpire(c.EmpireId)!.Crisis!.LostColonyLocations;
        if(!retired.Contains(location)){retired.Add(location);retired.Sort((a,b)=>a.Colony.CompareTo(b.Colony));}
        candidate.Colonies.Remove(c);ObservationUpdater.Refresh(candidate,candidate.Turn);
    }
}
