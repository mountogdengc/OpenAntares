using System.Collections.Generic;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
namespace OpenAntares.Simulation.Movement;

public static class MovementResolver
{
    public static void Apply(ContentSet content, GameState snapshot, GameState candidate, List<ExpansionEvent> events,Crises.CrisisResolutionContext? context=null)
    {
        foreach (var before in snapshot.Vessels)
        {
            var v = candidate.FindVessel(before.Id)!; var t = before.Transit;
            if(context is not null&&before.CarriedPopulation>0&&before.Transit is null&&(Crises.CrisisQueries.IsHeld(snapshot,before.Id)||(before.StationaryStarId is {} port&&context.ClosedPorts.Contains((before.EmpireId,port)))||(before.PendingMove is {} target&&context.ClosedPorts.Contains((before.EmpireId,target)))))continue;
            if (before.PendingMove is { } dest)
            {
                var start = snapshot.FindStar(before.StationaryStarId!.Value)!; var end = snapshot.FindStar(dest)!;
                int turns = TravelCalculator.Duration(start.X, start.Y, end.X, end.Y, content.Vessels[before.RoleId].Speed);
                t = new(start.Id, end.Id, turns, turns); v.PendingMove = null; v.StationaryStarId = null;
            }
            if (t is null) continue;
            int remaining = t.RemainingTurns - 1;
            if (remaining > 0) v.Transit = t with { RemainingTurns = remaining };
            else
            {
                v.Transit = null; v.StationaryStarId = t.Destination;
                context?.ArrivedVessels.Add(v.Id);
                events.Add(new("arrived", v.EmpireId, v.Id, t.Destination));
                if (!snapshot.FindEmpire(v.EmpireId)!.Surveys.Exists(o => o.StarId == t.Destination)) events.Add(new("discovered", v.EmpireId, v.Id, t.Destination));
            }
        }
    }
}
