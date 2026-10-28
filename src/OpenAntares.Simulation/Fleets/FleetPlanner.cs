using System.Collections.Immutable;
using System;
using System.Linq;
using System.Collections.Generic;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Simulation.Fleets;
public sealed record FleetPlannedMove(VesselId Vessel,StarId Destination,FleetId? Fleet=null,RequestId? Request=null);
public sealed record FleetPlannedOrder(FleetId Fleet,FleetOrder Order,string? Attention=null);
public sealed record FleetPlan(ImmutableArray<FleetPlannedMove> Moves,ImmutableArray<FleetPlannedOrder> Orders);
public static class FleetPlanner
{
    public static FleetPlan Plan(IFleetPlanningContext context)
    {
        var view=context.View;var c=context.Content;
        var moves=new SortedDictionary<VesselId,FleetPlannedMove>();var orders=ImmutableArray.CreateBuilder<FleetPlannedOrder>();
        if(!Rules.HasFleets(view.RulesVersion)||view.Fleets is not {} f)return new(ImmutableArray<FleetPlannedMove>.Empty,orders.ToImmutable());
        bool Blocked(OwnedVesselView ship)=>ship.Transit is not null||ship.PendingMove is not null||ship.PendingFounding is not null||ship.Crisis?.Held==true||view.Scouting?.Vessels.Any(row=>row.Id==ship.Id&&row.Stage is not null)==true;
        bool Legal(OwnedVesselView ship,StarId target)=>Scouting.ScoutQueries.LegalRoute(c,view,ship,target)&&view.Scouting?.Habitats.Any(h=>h.Star==target)!=true&&
            !(view.Scouting?.Vessels.Any(row=>row.Id==ship.Id&&row.Posture==Scouting.ScoutPosture.AvoidEncounters)==true&&Scouting.ScoutQueries.NeutralExcluded(view,target));
        foreach(var request in f.Requests.Where(r=>r.Status==RequestStatus.Active).OrderBy(r=>r.Id))
        {
            foreach(var slot in request.Slots.OrderBy(s=>s.Ordinal))
            {
                if(slot.Status!=SlotStatus.Built||slot.Vessel is not {} id||slot.Colony is {} col&&request.PausedContributions.Contains(col))continue;
                var ship=view.Vessels.Single(v=>v.Id==id);var membership=f.Ships.Single(v=>v.Id==id).Fleet;
                if(Blocked(ship)||membership is not null||ship.StationaryStarId==request.Rally||!Legal(ship,request.Rally))continue;
                moves.Add(id,new(id,request.Rally,Request:request.Id));
            }
        }
        foreach(var fleet in f.Fleets.OrderBy(fleet=>fleet.Id))
        {
            if(fleet.Order.Kind==FleetOrderKind.Hold||fleet.Members.Any(id=>moves.ContainsKey(id)))continue;
            var ships=fleet.Members.Select(id=>view.Vessels.Single(v=>v.Id==id)).ToArray();
            if(ships.Any(Blocked)||f.Requests.Any(r=>r.Status==RequestStatus.Active&&r.RallyFleet==fleet.Id))continue;
            StarId? target=fleet.Order.Destination;string? attention=null;
            if(fleet.Order.Kind==FleetOrderKind.Explore)
            {
                target=view.Stars.Where(star=>star.Chart.Id!=ships[0].StationaryStarId&&(star.LastObservedTurn is null||(long)view.Turn-star.LastObservedTurn>=5)&&ships.All(ship=>Legal(ship,star.Chart.Id)))
                    .OrderBy(star=>star.LastObservedTurn is null?0:1).ThenBy(star=>FleetTravel.PreviewDuration(c,view,ships[0],star.Chart.Id)).ThenBy(star=>star.Chart.Id).Select(star=>(StarId?)star.Chart.Id).FirstOrDefault();
                if(target is null){orders.Add(new(fleet.Id,new(FleetOrderKind.Hold),"No currently eligible unsurveyed or five-turn resurvey destination."));continue;}
            }
            if(target==ships[0].StationaryStarId){orders.Add(new(fleet.Id,new(FleetOrderKind.Hold)));continue;}
            if(fleet.Order.Kind==FleetOrderKind.Return&&!view.Colonies.Any(col=>col.StarId==target))attention="Return destination is no longer an owned colony system.";
            if(target is null||ships.Any(ship=>!Legal(ship,target.Value)))attention??="Standing destination is unavailable under current safety or port rules.";
            if(attention is not null){if(attention!=fleet.Attention)orders.Add(new(fleet.Id,fleet.Order,attention));continue;}
            foreach(var ship in ships)moves.Add(ship.Id,new(ship.Id,target!.Value,fleet.Id));
            if(fleet.Attention is not null)orders.Add(new(fleet.Id,fleet.Order));
        }
        if(moves.Count>f.Ships.Length)throw new InvalidOperationException("Fleet proposal budget exceeded.");
        return new(moves.Values.ToImmutableArray(),orders.ToImmutable());
    }
}
