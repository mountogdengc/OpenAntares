using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Simulation.Rivals;

internal static class RivalVesselPlanner
{
    internal static bool Plan(Func<IRivalPlanningContext> current, Func<Command,bool> emit)
    {
        var reserved = new HashSet<PlanetId>();
        foreach (var id in current().View.Vessels.OrderBy(v => v.Id).Select(v => v.Id).ToArray())
        {
            var context = current(); var view = context.View; var vessel = view.Vessels.Single(v => v.Id == id);
            var role = context.Content.Vessels[vessel.RoleId];
            if(Rules.HasScouting(view.RulesVersion)&&view.Scouting?.Vessels.FirstOrDefault(v=>v.Id==id) is {Posture:Scouting.ScoutPosture.Observe})
            {
                if(!emit(new SetScoutPostureCommand(view.Viewer,view.Turn,id,Scouting.ScoutPosture.AvoidEncounters)))return false;
                context=current();view=context.View;vessel=view.Vessels.Single(v=>v.Id==id);
            }
            if (vessel.Transit is { } transit)
            {
                if (role.CanFoundColony)
                {
                    var target = RivalColonyPlanner.Candidates(context,role.Id).Where(w => w.Star == transit.Destination && !reserved.Contains(w.Planet.Id)).OrderBy(w => w.Planet.Id).FirstOrDefault();
                    if (target is not null) reserved.Add(target.Planet.Id);
                }
                continue;
            }
            if(Rules.HasScouting(view.RulesVersion))
            {
                var scout=view.Scouting?.Vessels.FirstOrDefault(v=>v.Id==id);
                bool responding=scout?.Stage is not null||scout?.Posture==Scouting.ScoutPosture.AvoidEncounters&&vessel.StationaryStarId is {} origin&&Scouting.ScoutQueries.NeutralExcluded(view,origin);
                if(responding)
                {
                    if(scout?.Stage is not null&&scout.Blocker is not null)return false;
                    var response=context.ScoutPreparation?.Commands.FirstOrDefault(c=>c switch{SetMoveOrderCommand move=>move.Vessel==id,SetScoutRetreatOrderCommand retreat=>retreat.Vessel==id,_=>false});
                    if(response is not null&&!emit(response))return false;
                    continue;
                }
            }
            if(vessel.CarriedPopulation>0&&vessel.Crisis is{ } health&&(health.Held||health.PortClosed))continue;
            Command? command;
            if (role.CanFoundColony) command = Founder(context,vessel,reserved);
            else command = Scout(context,vessel);
            if (command is not null && !emit(command)) return false;
        }
        return true;
    }

    private static Command? Founder(IRivalPlanningContext context, OwnedVesselView vessel, HashSet<PlanetId> reserved)
    {
        var view = context.View;
        var otherIntents = view.Vessels.Where(v => v.Id != vessel.Id && v.PendingFounding.HasValue).Select(v => v.PendingFounding!.Value).ToHashSet();
        var choices = RivalColonyPlanner.Candidates(context,vessel.RoleId).Where(w => !reserved.Contains(w.Planet.Id) && !otherIntents.Contains(w.Planet.Id))
            .Select(w => (World:w,Travel:context.TravelTurns(vessel.StationaryStarId!.Value,w.Star,vessel.RoleId)))
            .Where(x => x.Travel.HasValue).OrderBy(x => x.Travel).ThenBy(x => x.World.Planet.Id).ToArray();
        var keep = choices.FirstOrDefault(x => vessel.PendingFounding == x.World.Planet.Id);
        var chosen = keep.World is not null ? keep : choices.FirstOrDefault();
        if (chosen.World is null)
            return vessel.PendingFounding.HasValue ? new SetFoundingOrderCommand(view.Viewer,view.Turn,vessel.Id,null)
                : vessel.PendingMove.HasValue ? new SetMoveOrderCommand(view.Viewer,view.Turn,vessel.Id,null) : null;
        reserved.Add(chosen.World.Planet.Id);
        if (chosen.World.Star == vessel.StationaryStarId)
        {
            if (!vessel.FoundingPreviews.Any(p => p.Target == chosen.World.Planet.Id && p.RejectionCode is null)) return null;
            return vessel.PendingFounding == chosen.World.Planet.Id ? null : new SetFoundingOrderCommand(view.Viewer,view.Turn,vessel.Id,chosen.World.Planet.Id);
        }
        return vessel.PendingMove == chosen.World.Star ? null : new SetMoveOrderCommand(view.Viewer,view.Turn,vessel.Id,chosen.World.Star);
    }

    private static Command? Scout(IRivalPlanningContext context, OwnedVesselView vessel)
    {
        var view = context.View;
        int? Travel(StarId target)=>Rules.HasFleets(view.RulesVersion)&&view.Fleets!.Ships.Single(s=>s.Id==vessel.Id).Fleet is not null?Fleets.FleetTravel.PreviewDuration(context.Content,view,vessel,target):context.TravelTurns(vessel.StationaryStarId!.Value,target,vessel.RoleId);
        bool Excluded(StarId star)=>Rules.HasScouting(view.RulesVersion)&&(view.Scouting?.Habitats.Any(h=>h.Star==star)==true||Scouting.ScoutQueries.NeutralExcluded(view,star));
        if(Rules.HasScouting(view.RulesVersion)&&vessel.PendingMove is {} pending&&!Excluded(pending)&&Travel(pending).HasValue)return null;
        var claimed = view.Vessels.Where(v => v.Id != vessel.Id && !context.Content.Vessels[v.RoleId].CanFoundColony)
            .Select(v => v.Transit?.Destination ?? v.PendingMove).Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
        var unknown = view.Stars.Where(s => s.Planets is null && s.Chart.Id != vessel.StationaryStarId&&!Excluded(s.Chart.Id)).ToArray();
        var candidates = (unknown.Length > 0 ? unknown : view.Stars.Where(s => !s.Current && s.Chart.Id != vessel.StationaryStarId &&
            s.LastObservedTurn.HasValue && (long)view.Turn-s.LastObservedTurn.Value >= context.Policy.ResurveyMinAge&&!Excluded(s.Chart.Id)).ToArray())
            .Select(s => (Star:s,Travel:Travel(s.Chart.Id))).Where(x => x.Travel.HasValue).ToArray();
        var unclaimed = candidates.Where(x => !claimed.Contains(x.Star.Chart.Id)).ToArray();
        if (unclaimed.Length > 0) candidates = unclaimed;
        var chosen = candidates.OrderBy(x => unknown.Length > 0 ? 0 : x.Star.LastObservedTurn!.Value).ThenBy(x => x.Travel).ThenBy(x => x.Star.Chart.Id).FirstOrDefault();
        StarId? destination = chosen.Star?.Chart.Id;
        return destination == vessel.PendingMove ? null : new SetMoveOrderCommand(view.Viewer,view.Turn,vessel.Id,destination);
    }
}
