using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
namespace OpenAntares.Simulation.Movement;

public static class MovementResolver
{
    public static void Apply(ContentSet content, GameState snapshot, GameState candidate, List<ExpansionEvent> events,Crises.CrisisResolutionContext? context=null,Scouting.AmebaResolutionContext? scouting=null,Fleets.FleetResolutionContext? fleetContext=null,Combat.CombatResolutionContext? combat=null,Conflict.ConflictResolutionContext? conflict=null)
    {
        if(conflict is not null)
        {
            if(!Rules.HasConflict(snapshot.RulesVersion)||!conflict.AftermathApplied||conflict.MovementApplied||conflict.FromTurn!=snapshot.Turn||candidate.Turn!=snapshot.Turn||!ReferenceEquals(fleetContext??combat?.FleetContext??conflict.FleetContext,conflict.FleetContext))throw new System.InvalidOperationException("Conflict movement requires its shared aftermath once.");
            conflict.MovementApplied=true;
        }
        var militaryDepartures=conflict?.Departures.SelectMany(d=>d.Members.Select(id=>(Id:id,Departure:d))).ToDictionary(x=>x.Id,x=>x.Departure);
        foreach (var before in snapshot.Vessels)
        {
            if(scouting?.ConsumedVessels.Contains(before.Id)==true)continue;
            var v = candidate.FindVessel(before.Id);
            if(v is null){if(combat is not null||conflict?.Destructions.Any(d=>d.Vessel==before.Id)==true||conflict?.FleetContext?.IntegratedLosses.Contains(before.Id)==true)continue;throw new System.InvalidOperationException("Unexpected missing movement vessel.");}
            var military=militaryDepartures?.GetValueOrDefault(before.Id);
            if(military is {Main:true}&&military.Members.Any(id=>snapshot.FindVessel(id)?.PendingFounding is not null))
            {
                if(!conflict!.DeferredDepartures.Any(d=>d.Encounter==military.Encounter&&d.Owner==military.Owner&&d.Main))conflict.DeferredDepartures.Add(military);
                continue;
            }
            if(military is {Destination:null}){v.PendingMove=null;continue;}
            if(military is null&&conflict?.UsedVessels.Contains(before.Id)==true){v.PendingMove=null;continue;}
            var t = before.Transit;var departure=military is {Destination:{} destination}?new Combat.CombatDeparture(military.Members,military.Origin,destination,military.Speed,military.TotalTurns,military.OnArrival):combat?.Departures.GetValueOrDefault(before.Id);
            if(context is not null&&before.CarriedPopulation>0&&before.Transit is null&&(Crises.CrisisQueries.IsHeld(snapshot,before.Id)||(before.StationaryStarId is {} port&&context.ClosedPorts.Contains((before.EmpireId,port)))||((departure?.Destination??before.PendingMove) is {} target&&context.ClosedPorts.Contains((before.EmpireId,target)))))continue;
            if(departure is not null)
            {
                int turns=departure.TotalTurns;
                if(v.Fleet is {} group)candidate.FindEmpire(v.EmpireId)!.Fleets!.Fleets.Single(f=>f.Id==group).Cohort=new(departure.Origin,departure.Destination,turns,snapshot.Turn,departure.Speed);
                t=new(departure.Origin,departure.Destination,turns,turns);v.PendingMove=null;v.StationaryStarId=null;if(v.Scout is {} scout)scout.ApproachAcceptance=null;
            }
            else if (before.PendingMove is { } dest)
            {
                var start = snapshot.FindStar(before.StationaryStarId!.Value)!; var end = snapshot.FindStar(dest)!;
                int turns = fleetContext?.Launches.GetValueOrDefault(before.Id)?.TotalTurns ?? (Rules.HasFleets(snapshot.RulesVersion)?Fleets.FleetTravel.LaunchDuration(content,snapshot,before,dest):TravelCalculator.Duration(start.X, start.Y, end.X, end.Y, content.Vessels[before.RoleId].Speed));
                if(Rules.HasFleets(snapshot.RulesVersion)&&before.Fleet is {} group)
                {
                    var fleet=candidate.FindEmpire(before.EmpireId)!.Fleets!.Fleets.Single(f=>f.Id==group);
                    int speed=fleetContext?.Launches.GetValueOrDefault(before.Id)?.EffectiveSpeed??fleet.Members.Min(id=>Fleets.OfficerBonuses.EffectiveSpeed(content,snapshot,id));
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
    public static void CompleteConflictFounding(ContentSet content,GameState snapshot,GameState candidate,List<ExpansionEvent> events,Conflict.ConflictResolutionContext conflict,Crises.CrisisResolutionContext? crisis=null)
    {
        if(!Rules.HasConflict(snapshot.RulesVersion)||!conflict.MovementApplied||conflict.FoundingDeparturesApplied||conflict.FromTurn!=snapshot.Turn||candidate.Turn!=snapshot.Turn)throw new System.InvalidOperationException("Deferred conflict departures require the original movement boundary once.");
        conflict.FoundingDeparturesApplied=true;
        foreach(var original in conflict.DeferredDepartures)
        {
            var members=original.Members.Where(id=>candidate.FindVessel(id) is not null).OrderBy(id=>id).ToImmutableArray();
            var retained=conflict.Departures.Where(d=>!(d.Encounter==original.Encounter&&d.Owner==original.Owner&&d.Main==original.Main)).ToImmutableArray();
            if(members.IsEmpty){conflict.Departures=retained;continue;}
            bool foundingPaused=members.Any(id=>candidate.FindVessel(id)!.PendingFounding is not null);var destination=foundingPaused?null:original.Destination;
            int speed=members.Min(id=>
            {
                var ship=candidate.FindVessel(id)!;var load=ShipDesigns.VesselLoadouts.Resolve(content,candidate,ship);var fleet=candidate.FindEmpire(ship.EmpireId)!.Fleets!.Fleets.SingleOrDefault(f=>f.Id==ship.Fleet);
                bool navigator=fleet is not null&&candidate.FindVessel(fleet.Flagship) is {} flagship&&Conflict.ConflictObservations.IsMilitary(content,candidate,flagship);
                return Combat.CombatMath.Stats(load.Profile!,ship.Combat!.Hull,load.BaseSpeed,navigator?Fleets.OfficerBonuses.NavigatorBonus(content,candidate,ship):0,content.Combat).Speed;
            });
            var origin=candidate.FindStar(original.Origin)!;var target=destination is {} selected?candidate.FindStar(selected):null;int turns=target is null?0:TravelCalculator.Duration(origin.X,origin.Y,target.X,target.Y,speed);
            var departure=original with{Members=members,Destination=destination,ReturnColony=destination is null?null:original.ReturnColony,Speed=speed,TotalTurns=turns,OnArrival=destination is null?Combat.CombatArrivalOrder.Hold:original.OnArrival};conflict.Departures=retained.Add(departure);
            if(foundingPaused)Conflict.ConflictRelations.RecordEvent(candidate,original.Owner,candidate.Conflict!.Encounters.Single(e=>e.Id==original.Encounter).Pair.Other(original.Owner),"local_break","Protected civilian founding remains pending. The main cohort broke contact locally rather than replacing that finite order or inventing a route.",original.Origin,attention:true);
            foreach(var id in members)
            {
                var ship=candidate.FindVessel(id)!;ship.PendingMove=null;ship.Combat!.Repairing=false;ship.Combat.ReturnColony=departure.ReturnColony;ship.Combat.OnArrival=destination is null?null:departure.OnArrival;
                if(destination is not {} dest)continue;
                if(ship.StationaryStarId!=original.Origin||ship.Transit is not null)throw new System.InvalidOperationException("Deferred combat cohort already moved.");
                if(ship.Scout is {} scout)scout.ApproachAcceptance=null;
                if(turns>1)
                {
                    ship.StationaryStarId=null;ship.Transit=new(original.Origin,dest,turns,turns-1);
                    if(ship.Fleet is {} group)candidate.FindEmpire(ship.EmpireId)!.Fleets!.Fleets.Single(f=>f.Id==group).Cohort=new(original.Origin,dest,turns,snapshot.Turn,speed);
                }
                else
                {
                    ship.Transit=null;ship.StationaryStarId=dest;
                    if(ship.Fleet is {} group)candidate.FindEmpire(ship.EmpireId)!.Fleets!.Fleets.Single(f=>f.Id==group).Cohort=null;
                    events.Add(new("arrived",ship.EmpireId,ship.Id,dest));if(!snapshot.FindEmpire(ship.EmpireId)!.Surveys.Exists(s=>s.StarId==dest))events.Add(new("discovered",ship.EmpireId,ship.Id,dest));
                    crisis?.ArrivedVessels.Add(ship.Id);
                }
            }
        }
    }
}
