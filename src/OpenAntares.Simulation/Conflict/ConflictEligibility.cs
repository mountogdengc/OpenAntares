using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public static class ConflictEligibility
{
    public static bool Allowed(ContentSet content,EmpireView view,ConflictAction action)
    {
        if(view.Conflict is not {} own||view.Fleets is not {} fleets||view.Turn==int.MaxValue)return false;
        bool War(EmpireId other)=>own.Relations.Any(r=>r.Other==other&&r.Relation==RelationKind.War);
        bool Orders(ConflictOrders? orders,StarId? star,bool required=false)=>orders is not null&&Enum.IsDefined(orders.Doctrine)&&Enum.IsDefined(orders.Stragglers)&&Enum.IsDefined(orders.OnArrival)&&(orders.ReturnColony is {} id?view.Colonies.Any(c=>c.Id==id&&c.StarId!=star&&!CombatQueries.PortClosed(view,c.StarId)):!required);
        bool Fleet(FleetId id)
        {
            var fleet=fleets.Fleets.SingleOrDefault(f=>f.Id==id);if(fleet is null||fleet.Members.Length is <1 or >32||fleet.Cohort is not null||fleet.Order.Kind!=FleetOrderKind.Hold||fleets.Requests.Any(r=>FleetQueries.IsActive(r.Status)&&r.RallyFleet==id))return false;
            var ships=own.Ships.Where(s=>s.Fleet==id).ToArray();
            return ships.Length==fleet.Members.Length&&ships.Select(s=>s.Star).Distinct().Count()==1&&ships.All(s=>s.Star is not null&&s.Military&&!s.NewlyCompleted&&!s.Repairing&&!s.Refitting&&!s.AssemblyBound&&!s.InSentry&&!s.InConflict&&view.Vessels.Any(v=>v.Id==s.Id&&v.Transit is null&&v.PendingMove is null&&v.PendingFounding is null&&v.Crisis?.Held!=true)&&view.Scouting?.Vessels.Any(v=>v.Id==s.Id&&v.Stage is not null)!=true);
        }
        switch(action)
        {
            case ConflictAction.Attack a:
                var target=own.Military.SingleOrDefault(m=>m.Vessel==a.Target);return target is {Current:true}&&target.Turn==view.Turn&&War(target.Owner)&&Fleet(a.Fleet)&&own.Ships.Any(s=>s.Fleet==a.Fleet&&s.Star==target.Star)&&Orders(a.Orders,target.Star,true);
            case ConflictAction.CancelAttack a:return own.OwnAttacks.Any(i=>i.Fleet==a.Fleet);
            case ConflictAction.ConfigureDefense a:
                var anchor=own.Ships.SingleOrDefault(s=>s.Id==a.Anchor);return anchor is {Military:true,InConflict:false}&&!own.OwnAttacks.Any(i=>i.Fleet==anchor.Fleet)&&Orders(a.Orders,anchor.Star);
            case ConflictAction.ChangeOrders a:
                var change=own.Encounters.SingleOrDefault(e=>e.Id==a.Encounter);return change is not null&&change.Revision==a.Revision&&Orders(a.Orders,view.Vessels.FirstOrDefault(v=>change.OwnMembers.Contains(v.Id))?.StationaryStarId);
            case ConflictAction.Withdraw a:return own.Encounters.Any(e=>e.Id==a.Encounter&&e.Revision==a.Revision);
            case ConflictAction.Respond a:return own.Encounters.Any(e=>e.Id==a.Encounter&&e.Revision==a.Revision&&e.OwnNeedsResponse);
            case ConflictAction.Blockade a:
                if(!War(a.Other)||!Fleet(a.Fleet)||own.OwnAttacks.Any(i=>i.Fleet==a.Fleet))return false;
                var members=own.Ships.Where(s=>s.Fleet==a.Fleet).ToArray();var star=members[0].Star;
                return members.Any(s=>(Int128)s.Hull*100>(Int128)s.MaxHull*content.Combat!.HullThresholds[0]&&s.Cohesion>content.Combat.RetreatCohesionThreshold)&&view.Stars.Any(s=>s.Chart.Id==star&&s.Current&&s.Planets is {} planets&&planets.Any(p=>p.OwnerId==a.Other&&p.ColonyId is not null));
            case ConflictAction.LiftBlockade a:return own.Blockades.Any(b=>b.Fleet==a.Fleet);
            default:return false;
        }
    }
    internal static CommandOutcome Execute(ContentSet content,GameState state,ConflictCommand command)
    {
        var candidate=state.Clone();var owner=candidate.Conflict!.Owners.Single(o=>o.Owner==command.Issuer);
        switch(command.Action)
        {
            case ConflictAction.Attack a:
                var old=owner.Attacks.SingleOrDefault(i=>i.Fleet==a.Fleet);if(old is not null&&old.Target==a.Target&&old.Orders==a.Orders)return CommandOutcome.NoChange(state);
                owner.Attacks.RemoveAll(i=>i.Fleet==a.Fleet);owner.Blockades.RemoveAll(b=>b.Fleet==a.Fleet);owner.Attacks.Add(new(a.Fleet,a.Target,a.Orders,state.Turn,checked((old?.Revision??0)+1)));owner.Attacks.Sort((a,b)=>a.Fleet.CompareTo(b.Fleet));break;
            case ConflictAction.CancelAttack a:owner.Attacks.RemoveAll(i=>i.Fleet==a.Fleet);break;
            case ConflictAction.ConfigureDefense a:
                var ship=candidate.FindVessel(a.Anchor)!;var previous=owner.Preferences.SingleOrDefault(p=>ship.Fleet is {} fleet?p.Fleet==fleet:p.Fleet is null&&p.Anchor==ship.Id);if(previous?.Orders==a.Orders||previous is null&&a.Orders==ConflictOrders.Default)return CommandOutcome.NoChange(state);
                if(previous is not null)owner.Preferences.Remove(previous);owner.Preferences.Add(new(ship.Fleet,ship.Id,a.Orders,checked((previous?.Revision??0)+1)));break;
            case ConflictAction.ChangeOrders a:
                var changing=Side(a.Encounter);if(changing.Orders==a.Orders&&!changing.WithdrawQueued&&!changing.NeedsResponse)return CommandOutcome.NoChange(state);
                changing.Orders=a.Orders;changing.WithdrawQueued=false;changing.NeedsResponse=false;changing.ConsentRevision=checked(changing.ConsentRevision+1);break;
            case ConflictAction.Withdraw a:
                var departing=Side(a.Encounter);if(departing.WithdrawQueued&&!departing.NeedsResponse)return CommandOutcome.NoChange(state);departing.WithdrawQueued=true;departing.NeedsResponse=false;departing.ConsentRevision=checked(departing.ConsentRevision+1);break;
            case ConflictAction.Respond a:var responding=Side(a.Encounter);responding.NeedsResponse=false;responding.ConsentRevision=checked(responding.ConsentRevision+1);break;
            case ConflictAction.Blockade a:
                var blockade=owner.Blockades.SingleOrDefault(b=>b.Fleet==a.Fleet);if(blockade?.Other==a.Other)return CommandOutcome.NoChange(state);
                owner.Blockades.RemoveAll(b=>b.Fleet==a.Fleet);var station=candidate.FindVessel(candidate.FindEmpire(command.Issuer)!.Fleets!.Fleets.Single(f=>f.Id==a.Fleet).Flagship)!.StationaryStarId!.Value;
                owner.Blockades.Add(new(a.Fleet,a.Other,station,state.Turn,checked((blockade?.Revision??0)+1)));owner.Blockades.Sort((a,b)=>a.Fleet.CompareTo(b.Fleet));break;
            case ConflictAction.LiftBlockade a:owner.Blockades.RemoveAll(b=>b.Fleet==a.Fleet);break;
            default:return CommandOutcome.Reject(state,"conflict_unavailable","Military action unavailable.");
        }
        owner.Revision=checked(owner.Revision+1);candidate.FindEmpire(command.Issuer)!.Ready=false;
        if(GameStateValidator.Validate(content,candidate,allowAllHumansReady:state.FindEmpire(command.Issuer)!.Controller==ControllerKind.Ai).Count!=0)return CommandOutcome.Reject(state,"conflict_invalid","Military action could not be applied coherently.");
        return new(candidate,null,true,null);
        ConflictSideState Side(ConflictEncounterId id){var encounter=candidate.Conflict.Encounters.Single(e=>e.Id==id);return encounter.LowSide.Owner==command.Issuer?encounter.LowSide:encounter.HighSide;}
    }
}
