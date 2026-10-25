using System;
using System.Linq;
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
        candidate.Colonies.Remove(c);
        // An empire knows its own colony ceased to exist even without a surviving observer.
        // Preserve the survey's timestamp and all other recorded facts; foreign stale surveys remain stale.
        foreach(var survey in candidate.FindEmpire(c.EmpireId)!.Surveys)
            survey.Planets=survey.Planets.Select(p=>p.ColonyId==c.Id?p with{ColonyId=null,OwnerId=null}:p).ToList();
        ObservationUpdater.Refresh(candidate,candidate.Turn);
    }
}
