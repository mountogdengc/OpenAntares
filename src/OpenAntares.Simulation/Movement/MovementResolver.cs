using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
namespace OpenAntares.Simulation.Movement;

public static class MovementResolver
{
    public static void Apply(ContentSet content, GameState snapshot, GameState candidate, List<ExpansionEvent> events,Crises.CrisisResolutionContext? context=null,Scouting.AmebaResolutionContext? scouting=null)
    {
        foreach (var before in snapshot.Vessels)
        {
            if(scouting?.ConsumedVessels.Contains(before.Id)==true)continue;
            var v = candidate.FindVessel(before.Id)??throw new System.InvalidOperationException("Unexpected missing movement vessel."); var t = before.Transit;
            if(context is not null&&before.CarriedPopulation>0&&before.Transit is null&&(Crises.CrisisQueries.IsHeld(snapshot,before.Id)||(before.StationaryStarId is {} port&&context.ClosedPorts.Contains((before.EmpireId,port)))||(before.PendingMove is {} target&&context.ClosedPorts.Contains((before.EmpireId,target)))))continue;
            if (before.PendingMove is { } dest)
            {
                var start = snapshot.FindStar(before.StationaryStarId!.Value)!; var end = snapshot.FindStar(dest)!;
                int turns = Rules.HasFleets(snapshot.RulesVersion)?Fleets.FleetTravel.LaunchDuration(content,snapshot,before,dest):TravelCalculator.Duration(start.X, start.Y, end.X, end.Y, content.Vessels[before.RoleId].Speed);
                if(Rules.HasFleets(snapshot.RulesVersion)&&before.Fleet is {} group)
                {
                    var fleet=candidate.FindEmpire(before.EmpireId)!.Fleets!.Fleets.Single(f=>f.Id==group);
                    int speed=fleet.Members.Min(id=>Fleets.OfficerBonuses.EffectiveSpeed(content,snapshot,id));
                    fleet.Cohort=new(start.Id,end.Id,turns,snapshot.Turn,speed);
                }
                t = new(start.Id, end.Id, turns, turns); v.PendingMove = null; v.StationaryStarId = null;
                if(v.Scout is {} scout)scout.ApproachAcceptance=null;
            }
            if (t is null) continue;
            int remaining = t.RemainingTurns - 1;
            if (remaining > 0) v.Transit = t with { RemainingTurns = remaining };
            else
            {
                v.Transit = null; v.StationaryStarId = t.Destination;
                if(Rules.HasFleets(snapshot.RulesVersion)&&v.Fleet is {} group)candidate.FindEmpire(v.EmpireId)!.Fleets!.Fleets.Single(f=>f.Id==group).Cohort=null;
                context?.ArrivedVessels.Add(v.Id);
                events.Add(new("arrived", v.EmpireId, v.Id, t.Destination));
                if (!snapshot.FindEmpire(v.EmpireId)!.Surveys.Exists(o => o.StarId == t.Destination)) events.Add(new("discovered", v.EmpireId, v.Id, t.Destination));
            }
        }
    }
}
