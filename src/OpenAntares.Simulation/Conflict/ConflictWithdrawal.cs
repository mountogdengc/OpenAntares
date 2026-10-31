using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.ShipDesigns;
namespace OpenAntares.Simulation.Conflict;
public sealed record ConflictDeparture(ConflictEncounterId Encounter,EmpireId Owner,FleetId? Fleet,ImmutableArray<VesselId> Members,StarId Origin,StarId? Destination,ColonyId? ReturnColony,int Speed,int TotalTurns,CombatArrivalOrder OnArrival,bool Main);
public sealed record ConflictEscapeCohort(ImmutableArray<VesselId> Members,int Speed,bool Main);
public sealed record ConflictWithdrawalPreview(ImmutableArray<ConflictEscapeCohort> Cohorts,StarId? Destination,int? ObservedPursuitSpeed,bool Conditional,string Explanation);
public static class ConflictWithdrawal
{
    public static ConflictWithdrawalPreview? Preview(ContentSet content,Knowledge.EmpireView view,ConflictAction action)
    {
        if(view.Conflict is not {} own||!ConflictEligibility.Allowed(content,view,action))return null;
        var id=action switch{ConflictAction.Withdraw a=>(ConflictEncounterId?)a.Encounter,ConflictAction.ChangeOrders a=>a.Encounter,_=>null};var encounter=own.Encounters.SingleOrDefault(e=>e.Id==id);if(encounter is null)return null;
        var orders=action is ConflictAction.ChangeOrders changed?changed.Orders:encounter.OwnOrders;
        var members=own.Ships.Where(s=>encounter.OwnMembers.Contains(s.Id)).ToArray();if(members.Length==0||members[0].Star is not {} star)return null;
        var fleet=view.Fleets!.Fleets.SingleOrDefault(f=>f.Id==encounter.OwnFleet);var flagship=fleet?.Flagship??members[0].Id;
        var detached=orders.Stragglers==StragglerPolicy.DetachCrippled?members.Where(s=>s.Id!=flagship&&CombatMath.Band(new(s.MaxHull,s.Attack,s.Armor),s.Hull,content.Combat)==CombatDamageBand.Crippled).Select(s=>s.Id).ToHashSet():new HashSet<VesselId>();
        var main=members.Where(s=>!detached.Contains(s.Id)).Select(s=>s.Id).Concat(encounter.ProtectedCivilians).OrderBy(v=>v).ToImmutableArray();var groups=ImmutableArray.CreateBuilder<ConflictEscapeCohort>();
        if(!main.IsEmpty)groups.Add(new(main,main.Min(id=>own.Ships.Single(s=>s.Id==id).Speed),true));
        foreach(var ship in members.Where(s=>detached.Contains(s.Id)).OrderBy(s=>s.Id))
        {var load=VesselLoadouts.Resolve(content,view,ship.Id);groups.Add(new(ImmutableArray.Create(ship.Id),CombatMath.Stats(load.Profile!,ship.Hull,load.BaseSpeed,0,content.Combat).Speed,false));}
        var port=view.Colonies.SingleOrDefault(c=>c.Id==orders.ReturnColony);StarId? destination=port?.StarId;
        var all=main.Concat(detached).ToArray();bool cargo=all.Any(id=>view.Vessels.Single(v=>v.Id==id).CarriedPopulation>0);
        if(destination==star||destination is {} dest&&CombatQueries.PortClosed(view,dest)||cargo&&(all.Any(id=>view.Vessels.Single(v=>v.Id==id).Crisis?.Held==true)||CombatQueries.PortClosed(view,star)))destination=null;
        var enemies=encounter.Opposing.Where(m=>m.Current&&m.Turn==view.Turn).ToArray();int? pursuit=enemies.Length==0?null:enemies.Max(m=>m.Speed);
        return new(groups.ToImmutable(),destination,pursuit,true,destination is null?"Conditional local Break contact: possible enemy interception occurs before survivors Hold here. It creates no immunity, travel or repair; opposing departure and command bonuses remain private.":"Conditional withdrawal along your selected return route. Speeds use your current damage; observed pursuit speed excludes hidden command bonuses. If both forces depart there is no volley; otherwise interception and later damage can change your travel time.");
    }
    public static void Resolve(ContentSet content,GameState snapshot,GameState candidate,ConflictResolutionContext context)
    {
        if(!context.MatchingResolved||context.WithdrawalsResolved||context.ExchangesResolved||context.FromTurn!=snapshot.Turn||candidate.Turn!=snapshot.Turn)throw new InvalidOperationException("Conflict withdrawal already applied or phase changed.");
        context.WithdrawalsResolved=true;if(!Rules.HasConflict(snapshot.RulesVersion))return;
        var departures=ImmutableArray.CreateBuilder<ConflictDeparture>();
        foreach(var frame in context.Battles)
        {
            var encounter=candidate.Conflict!.Encounters.Single(e=>e.Id==frame.Encounter);
            if(encounter.Phase==ConflictPhase.Completed||encounter.LastResolvedTurn==context.FromTurn||context.PausedSet.Contains(frame.Encounter)||context.AcceptedPeace.Contains(frame.Pair))continue;
            bool low=frame.LowWithdraw||frame.LowDepartureStar is not null,high=frame.HighWithdraw||frame.HighDepartureStar is not null;if(!low&&!high)continue;
            var lowGroups=low?Groups(encounter.LowSide,frame.Low):ImmutableArray<Group>.Empty;var highGroups=high?Groups(encounter.HighSide,frame.High):ImmutableArray<Group>.Empty;
            if(low!=high)
            {
                var groups=low?lowGroups:highGroups;var enemies=low?frame.High:frame.Low;
                var exposed=groups.Where(g=>g.Speed<=enemies.Max(s=>s.EffectiveSpeed)).SelectMany(g=>g.Members).Where(id=>(low?frame.Low:frame.High).Any(s=>s.VesselId==id)).ToImmutableHashSet();
                if(exposed.Count>0)
                {var result=ConflictExchange.Calculate(frame,!low,!high,low?exposed:null,high?exposed:null);ConflictExchange.ApplyResult(content,candidate,context,frame,result,true);}
            }
            if(low)Emit(encounter.LowSide,lowGroups,frame.LowDepartureStar);if(high)Emit(encounter.HighSide,highGroups,frame.HighDepartureStar);
            encounter.Phase=ConflictPhase.Completed;encounter.CompletedTurn=checked(context.FromTurn+1);
            foreach(var side in new[]{encounter.LowSide,encounter.HighSide})
            {
                side.WithdrawQueued=false;side.NeedsResponse=false;side.ConsentRevision=checked(side.ConsentRevision+1);
                if(side.Fleet is {} id&&candidate.FindEmpire(side.Owner)!.Fleets!.Fleets.SingleOrDefault(f=>f.Id==id) is {} fleet)fleet.Order=new(FleetOrderKind.Hold);
                if((side.Owner==frame.Pair.Low?!low:!high))ConflictRelations.RecordEvent(candidate,side.Owner,frame.Pair.Other(side.Owner),"opposing_disengaged","The opposing force disengaged after the departure exposure check. Your survivors remain in Hold.",frame.Star);
            }
            ImmutableArray<Group> Groups(ConflictSideState side,ImmutableArray<CombatShipFrame> actors)
            {
                var detached=side.Orders.Stragglers==StragglerPolicy.DetachCrippled?actors.Where(s=>s.VesselId!=side.Flagship&&CombatMath.Band(new(s.MaxHull,s.Attack,s.Armor),s.Hull,content.Combat)==CombatDamageBand.Crippled).Select(s=>s.VesselId).ToHashSet():new HashSet<VesselId>();
                var main=side.Members.Where(id=>!detached.Contains(id)).Concat(side.ProtectedCivilians).Where(id=>candidate.FindVessel(id) is not null).OrderBy(id=>id).ToImmutableArray();
                var groups=new List<Group>();if(main.Length>0)groups.Add(new(main,true,Speed(side,main,true)));
                foreach(var id in detached.OrderBy(id=>id))groups.Add(new(ImmutableArray.Create(id),false,Speed(side,ImmutableArray.Create(id),false)));
                return groups.ToImmutableArray();
            }
            int Speed(ConflictSideState side,ImmutableArray<VesselId> members,bool main)
            {
                bool navigation=main&&side.Members.Contains(side.Flagship)&&candidate.FindVessel(side.Flagship) is not null;
                return members.Min(id=>{var ship=candidate.FindVessel(id)!;var load=VesselLoadouts.Resolve(content,candidate,ship);return CombatMath.Stats(load.Profile!,ship.Combat!.Hull,load.BaseSpeed,navigation?OfficerBonuses.NavigatorBonus(content,candidate,ship):0,content.Combat).Speed;});
            }
            void Emit(ConflictSideState side,ImmutableArray<Group> groups,StarId? ordinary)
            {
                ColonyId? port=ordinary is null?side.Orders.ReturnColony:null;
                var colony=port is {} p?candidate.FindColony(p):null;StarId? destination=ordinary??(colony?.EmpireId==side.Owner?candidate.FindPlanet(colony.PlanetId)?.StarId:null);
                var ids=groups.SelectMany(g=>g.Members).Where(id=>candidate.FindVessel(id) is not null).ToArray();
                bool cargo=ids.Any(id=>candidate.FindVessel(id)!.CarriedPopulation>0);
                bool illegal=destination is null||destination==frame.Star||candidate.FindStar(destination.Value) is null||ordinary is null&&CrisisQueries.PortClosed(candidate,side.Owner,destination.Value)||cargo&&(ids.Any(id=>CrisisQueries.IsHeld(candidate,id))||CrisisQueries.PortClosed(candidate,side.Owner,frame.Star)||destination is {} selectedStar&&CrisisQueries.PortClosed(candidate,side.Owner,selectedStar));
                if(illegal){destination=null;port=null;}
                foreach(var group in groups)
                {
                    var survivors=group.Members.Where(id=>candidate.FindVessel(id) is not null).ToImmutableArray();if(survivors.IsEmpty)continue;
                    int speed=Speed(side,survivors,group.Main);var origin=candidate.FindStar(frame.Star)!;var target=destination is {} dest?candidate.FindStar(dest):null;int turns=target is null?0:TravelCalculator.Duration(origin.X,origin.Y,target.X,target.Y,speed);
                    departures.Add(new(encounter.Id,side.Owner,side.Fleet,survivors,frame.Star,destination,port,speed,turns,destination is null||ordinary is not null?CombatArrivalOrder.Hold:side.Orders.OnArrival,group.Main));
                }
                ConflictRelations.RecordEvent(candidate,side.Owner,frame.Pair.Other(side.Owner),illegal?"local_break":"withdrawn",illegal?"Local Break contact ended this encounter without travel or repair. Ships remain at this system and can be attacked again. A missing or closed selected port is never replaced automatically.":"Your surviving departure cohorts follow your authorized route at their post-interception speed.",frame.Star,attention:illegal);
            }
        }
        context.Departures=departures.ToImmutable();
    }
    private sealed record Group(ImmutableArray<VesselId> Members,bool Main,int Speed);
}
