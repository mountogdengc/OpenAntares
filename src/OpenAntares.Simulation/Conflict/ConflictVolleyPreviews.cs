using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Simulation.Conflict;
public static partial class ConflictQueries
{
    public static ConflictVolleyView? PreviewVolley(ContentSet content,EmpireView view,ConflictAction action)
    {
        if(!Rules.HasConflict(view.RulesVersion)||view.Conflict is not {} own||content.Combat is null||!ConflictEligibility.Allowed(content,view,action))return null;
        ImmutableArray<OwnConflictShipView> ships;ImmutableArray<MilitaryObservation> observed;ConflictOrders orders;EmpireId other;StarId star;
        if(action is ConflictAction.Attack attack)
        {
            var target=own.Military.Single(m=>m.Vessel==attack.Target);other=target.Owner;star=target.Star;orders=attack.Orders;
            ships=own.Ships.Where(s=>s.Fleet==attack.Fleet&&s.Military).ToImmutableArray();observed=own.Military.Where(m=>m.Owner==other&&m.Star==star&&(target.Fleet is {} fleet?m.Fleet==fleet:m.Vessel==target.Vessel)&&m.Current&&m.Turn==view.Turn).ToImmutableArray();
        }
        else if(action is ConflictAction.ChangeOrders change)
        {
            var encounter=own.Encounters.Single(e=>e.Id==change.Encounter);other=encounter.Other;orders=change.Orders;ships=own.Ships.Where(s=>encounter.OwnMembers.Contains(s.Id)).ToImmutableArray();observed=encounter.Opposing.Where(m=>m.Current&&m.Turn==view.Turn).ToImmutableArray();if(ships.IsEmpty||ships[0].Star is not {} current)return null;star=current;
        }
        else return null;
        if(ships.IsEmpty||observed.IsEmpty||ships.Length>32||observed.Length>32)return null;
        var key=DiplomacyPair.For(view.Viewer,other);
        var actors=ships.Select(s=>new CombatShipFrame(s.Id,view.Vessels.Single(v=>v.Id==s.Id).RoleId,s.Hull,s.MaxHull,s.Cohesion,s.Attack,s.Armor,s.Speed,view.Fleets!.Fleets.Any(f=>f.Flagship==s.Id),0)).ToImmutableArray();
        // Only shot/hull calculations are projected. No guessed enemy command state is exposed.
        var targets=observed.Select(m=>new CombatShipFrame(m.Vessel,m.RoleId,m.Hull,m.MaxHull,0,m.Attack,m.Armor,m.Speed,false,0)).ToImmutableArray();
        var frame=new ConflictBattleFrame(default,key,star,view.Turn,0,ConflictPhase.Pending,key.Low==view.Viewer?actors:targets,key.Low==view.Viewer?targets:actors,key.Low==view.Viewer?orders:ConflictOrders.Default,key.Low==view.Viewer?ConflictOrders.Default:orders){Definition=content.Combat};
        var result=ConflictExchange.Calculate(frame);
        return new(result.Shots,key.Low==view.Viewer?result.Low:result.High,key.Low==view.Viewer?result.High:result.Low,true,"Conditional ordinary volley if both forces remain and the observed force stays available. Opposing withdrawal and command settings are private; this does not predict the battle outcome.");
    }
}
