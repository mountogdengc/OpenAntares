using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public static class CombatWithdrawal
{
    public static ImmutableArray<CombatDeparture> Preview(ContentSet content,CombatFrame frame)
    {
        if(frame.Route is not {} route||frame.Ships.IsDefaultOrEmpty)return ImmutableArray<CombatDeparture>.Empty;
        var detach=frame.Orders.Stragglers==StragglerPolicy.DetachCrippled?frame.Ships.Where(s=>!s.Flagship&&CombatMath.Band(new(s.MaxHull,s.Attack,s.Armor),s.Hull,frame.Definition)==CombatDamageBand.Crippled).Select(s=>s.VesselId).ToImmutableHashSet():ImmutableHashSet<VesselId>.Empty;
        var groups=new[]{frame.Ships.Where(s=>!detach.Contains(s.VesselId)).OrderBy(s=>s.VesselId).ToImmutableArray()}.Concat(frame.Ships.Where(s=>detach.Contains(s.VesselId)).OrderBy(s=>s.VesselId).Select(s=>ImmutableArray.Create(s)));
        return groups.Where(g=>g.Length>0).Select(g=>
        {
            int speed=g.Min(s=>detach.Contains(s.VesselId)?CombatMath.Stats(new(s.MaxHull,s.Attack,s.Armor),s.Hull,content.Vessels[s.RoleId].Speed,0,frame.Definition).Speed:s.EffectiveSpeed);
            return new CombatDeparture(g.Select(s=>s.VesselId).ToImmutableArray(),route.Origin,route.Destination,speed,TravelCalculator.Duration(route.OriginX,route.OriginY,route.DestinationX,route.DestinationY,speed),frame.Orders.OnArrival);
        }).ToImmutableArray();
    }
    internal static bool ReturnBlocked(GameState state,EncounterState encounter)
    {
        var port=state.FindColony(encounter.Orders.ReturnColony);if(port?.EmpireId!=encounter.Owner)return true;
        var destination=state.FindPlanet(port.PlanetId)?.StarId;if(destination is null)return true;
        return encounter.Members.Any(id=>state.FindVessel(id) is {} v&&v.CarriedPopulation>0&&(Crises.CrisisQueries.IsHeld(state,id)||Crises.CrisisQueries.PortClosed(state,encounter.Owner,destination.Value)||v.StationaryStarId is {} origin&&Crises.CrisisQueries.PortClosed(state,encounter.Owner,origin)));
    }
    public static void Resolve(ContentSet content,GameState snapshot,GameState candidate,CombatResolutionContext context)
    {
        if(context.WithdrawalsResolved||context.FromTurn!=snapshot.Turn)throw new InvalidOperationException("Withdrawal phase already applied or snapshot changed.");context.WithdrawalsResolved=true;
        if(!Rules.HasCombat(snapshot.RulesVersion))return;
        var departures=context.Departures.ToBuilder();
        foreach(var id in context.Encounters)
        {
            var before=snapshot.Combat!.Encounters.Single(e=>e.Id==id);var e=candidate.Combat!.Encounters.SingleOrDefault(e=>e.Id==id);
            if(e is null||before.Phase!=CombatPhase.WithdrawalQueued||e.Phase!=CombatPhase.WithdrawalQueued||e.LastResolvedTurn==context.FromTurn)continue;
            if(ReturnBlocked(candidate,e))
            {
                var frame=CombatQueries.Frame(content,candidate,e.Id);var exchange=CombatExchange.Calculate(frame,false,ImmutableArray<VesselId>.Empty);CombatResolver.ApplyExchange(content,candidate,e,frame,exchange,context);
                if(candidate.Combat.Encounters.Contains(e))BlockReturn(content,candidate,e);continue;
            }
            var frozen=CombatQueries.Frame(content,candidate,e.Id);var groups=Preview(content,frozen);var flagship=frozen.Ships.Single(s=>s.Flagship).VesselId;var main=groups.Single(g=>g.Members.Contains(flagship));var detached=groups.Where(g=>!g.Members.Contains(flagship)).SelectMany(g=>g.Members).ToImmutableHashSet();
            // Detach under the frozen authority before M8 loss normalization chooses a replacement.
            var fleet=candidate.FindEmpire(e.Owner)!.Fleets!.Fleets.Single(f=>f.Id==e.Fleet);fleet.Members.RemoveAll(detached.Contains);foreach(var member in detached)candidate.FindVessel(member)!.Fleet=null;
            var exposed=frozen.Sentries.Length==0||e.Round==0?ImmutableArray<VesselId>.Empty:groups.Where(g=>g.Speed<=frozen.Sentries.Max(s=>s.PursuitSpeed)).SelectMany(g=>g.Members).OrderBy(v=>v).ToImmutableArray();
            if(exposed.Length>0)
            {
                var exchange=CombatExchange.Calculate(frozen,true,exposed);CombatResolver.ApplyExchange(content,candidate,e,frozen,exchange,context,true,exposed);
            }
            foreach(var group in groups)
            {
                var members=group.Members.Where(id=>candidate.FindVessel(id) is not null).OrderBy(id=>id).ToImmutableArray();if(members.Length==0)continue;
                int speed=members.Min(id=>OfficerBonuses.EffectiveSpeed(content,candidate,id));var route=frozen.Route!;
                var departure=group with {Members=members,Speed=speed,TotalTurns=TravelCalculator.Duration(route.OriginX,route.OriginY,route.DestinationX,route.DestinationY,speed)};
                foreach(var member in members)
                {
                    departures.Add(member,departure);var ship=candidate.FindVessel(member)!;ship.PendingMove=departure.Destination;ship.Combat!.ReturnColony=e.Orders.ReturnColony;ship.Combat.OnArrival=departure.OnArrival;ship.Combat.Repairing=false;
                }
            }
            if(candidate.Combat.Encounters.Contains(e))CombatResolver.Finish(content,candidate,e,"Withdrawn");
        }
        context.Departures=departures.ToImmutable();
        // A port may close during an ordinary exchange too; preserve a concrete next-planning response.
        foreach(var e in candidate.Combat!.Encounters.ToArray())if(ReturnBlocked(candidate,e))BlockReturn(content,candidate,e);
    }
    private static void BlockReturn(ContentSet content,GameState candidate,EncounterState encounter)
    {
        var owner=candidate.FindEmpire(encounter.Owner)!.Combat!;var old=owner.Responses.SingleOrDefault(r=>r.Encounter==encounter.Id);
        if(old?.Need.HasFlag(CombatResponseNeed.BlockedReturn)==true)return;
        bool force=encounter.ConsentRevision==encounter.Revision;encounter.Revision=checked(encounter.Revision+1);if(force)encounter.ConsentRevision=encounter.Revision;
        encounter.Phase=CombatPhase.BlockedResponse;encounter.ContinueRiskTurn=null;owner.Responses.RemoveAll(r=>r.Encounter==encounter.Id);owner.Responses.Add(new(encounter.Id,encounter.Revision,"The selected return route is blocked; choose a legal destination or explicitly remain for defensive combat.",(old?.Need??(force?0:CombatResponseNeed.RevisedForce))|CombatResponseNeed.BlockedReturn));owner.Responses=owner.Responses.OrderBy(r=>r.Encounter).ToList();
        CombatResolver.Event(content,candidate,encounter,"emergency","Withdrawal was blocked. No destination was substituted; the next Ready requires a revised route or explicit continued risk.");
    }
}
