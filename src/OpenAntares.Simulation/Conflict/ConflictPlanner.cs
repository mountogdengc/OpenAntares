using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public enum ConflictPlanningStage { Responses, Declarations, Production, Grouping, Orders }
/// <summary>Deterministic decisions over one empire's immutable permitted facts.</summary>
public static class ConflictPlanner
{
    public static ImmutableArray<Command> Plan(ContentSet content,EmpireView view,ConflictPlanningStage stage)
    {
        if(!Rules.HasConflict(view.RulesVersion)||view.Conflict is not {} own||content.Conflict is not {} d)return ImmutableArray<Command>.Empty;
        var result=ImmutableArray.CreateBuilder<Command>();
        void Emit(ConflictAction action){var preview=ConflictQueries.Preview(content,view,action);if(preview.Allowed)result.Add(new ConflictCommand(view.Viewer,view.Turn,action,preview.Token));}
        bool Healthy(OwnConflictShipView s)=>(Int128)s.Hull*100>=(Int128)s.MaxHull*d.RepairHullPercent&&s.Cohesion>=d.HealthyCohesion;
        bool Idle(OwnConflictShipView s)=>s.Military&&s.Star is not null&&(stage==ConflictPlanningStage.Grouping||!s.NewlyCompleted)&&!s.Repairing&&!s.Refitting&&!s.InSentry&&!s.InConflict&&!s.AssemblyBound&&view.Vessels.Single(v=>v.Id==s.Id) is {Transit:null,PendingMove:null,PendingFounding:null};
        long Distance(StarId a,StarId b){var x=view.Galaxy.Stars.Single(s=>s.Id==a);var y=view.Galaxy.Stars.Single(s=>s.Id==b);return checked((long)(x.X-y.X)*(x.X-y.X)+(long)(x.Y-y.Y)*(x.Y-y.Y));}
        ColonyId? Port(StarId star,bool different=false)=>view.Colonies.Where(c=>(!different||c.StarId!=star)&&c.Crisis?.Quarantined!=true).OrderBy(c=>Distance(star,c.StarId)).ThenBy(c=>c.StarId).ThenBy(c=>c.Id).Select(c=>(ColonyId?)c.Id).FirstOrDefault();
        var legal= view.ShipDesigns!.Designs.Where(x=>x.Unlocked&&!x.Archived&&x.Stats.Attack>0).OrderBy(x=>x.Stats.BuildCost).ThenByDescending(x=>x.Stats.Attack).ThenByDescending(x=>x.Stats.Armor).ThenByDescending(x=>x.Stats.BaseSpeed).ThenBy(x=>x.Id).FirstOrDefault();
        if(stage==ConflictPlanningStage.Responses)
        {
            foreach(var encounter in own.Encounters.OrderBy(e=>e.Id))
                if(encounter.OwnNeedsResponse)Emit(new ConflictAction.Respond(encounter.Id,encounter.Revision));
            foreach(var relation in own.Relations.Where(r=>r.Relation==RelationKind.War).OrderBy(r=>r.Other))
            {
                if(relation.Peace is {} offer&&offer.Proposer!=view.Viewer){Emit(new ConflictAction.AnswerPeace(relation.Other,offer.Id,true));continue;}
                long age=(long)view.Turn-(relation.WarTurn??view.Turn),idle=(long)view.Turn-(relation.OwnLastActivityTurn??relation.WarTurn??view.Turn);
                if(!relation.OwnPeaceProposed&&relation.Peace is null&&view.Turn>=relation.NextOwnPeaceOfferTurn&&age>=d.PeaceEarliest&&(age>=d.PeaceDeadline||idle>=d.PeaceInactivity||legal is null||!own.Ships.Any(s=>s.Military&&Healthy(s))||!own.OwnPressure.IsEmpty))Emit(new ConflictAction.ProposePeace(relation.Other));
            }
        }
        if(stage==ConflictPlanningStage.Declarations&&!own.Relations.Any(r=>r.Relation==RelationKind.War||r.OwnDeclarationQueued)&&view.Fleets!.Fleets.Any(f=>f.Members.Length>=2&&f.Members.All(id=>own.Ships.Any(s=>s.Id==id&&Idle(s)&&Healthy(s)))))
        {
            var eligible=own.Relations.Where(r=>r.Relation==RelationKind.Peace&&(r.TruceEndTurn is null||r.TruceEndTurn<=view.Turn)&&r.OwnClaimObservedSince is {} since&&(long)view.Turn-since>=d.ClaimGate)
                .Where(r=>view.Contacts.Any(c=>c.OtherEmpireId==r.Other&&(long)view.Turn-c.FirstObservedTurn>=d.ContactGate))
                .Where(r=>view.Diplomacy!.Contacts.Any(c=>c.Identity.OtherEmpireId==r.Other&&c.OwnAttitude.Score<=0&&c.Pending is null&&c.Agreements.IsEmpty)).OrderBy(r=>r.Other).FirstOrDefault();
            if(eligible is not null)Emit(new ConflictAction.DeclareWar(eligible.Other));
        }
        if(stage==ConflictPlanningStage.Production&&legal is not null)
        {
            int existing=own.Ships.Count(s=>s.Military);int committed=view.Colonies.Count(c=>c.ProjectKind==ProductionKinds.ShipDesign&&c.ProjectId is not null)+view.Colonies.Sum(c=>c.Development?.Queue.Count(q=>q.Target.Kind==ProductionKinds.ShipDesign)??0);
            int target=own.Relations.Any(r=>r.Relation==RelationKind.War)?d.WarHullTarget:view.Colonies.Length<2?0:d.PeaceHullTarget;
            bool militaryProject=view.Colonies.Any(c=>c.ProjectKind==ProductionKinds.ShipDesign&&c.ProjectId is not null);
            if(!militaryProject&&existing+committed<Math.Min(target,d.MilitaryHullCap))
            {
                var port=view.Colonies.Where(c=>c.ProjectId is null&&c.Economy.NetProduction.Total>0&&c.Development?.Queue.IsEmpty!=false&&!view.Fleets!.Requests.Any(r=>FleetQueries.IsActive(r.Status)&&r.Colonies.Contains(c.Id)&&!r.PausedContributions.Contains(c.Id))).OrderBy(c=>c.Id).FirstOrDefault();
                if(port is not null)result.Add(new SetProductionTargetCommand(view.Viewer,view.Turn,port.Id,ShipDesignTargets.Build(legal.Id)));
            }
        }
        if(stage==ConflictPlanningStage.Grouping)
        {
            // A single normal grouping command per re-evaluation, at most two re-evaluations.
            var groups=view.Fleets!.Fleets.Where(f=>f.Members.All(id=>own.Ships.Any(s=>s.Id==id&&Idle(s)))&&f.Cohort is null&&f.Order.Kind==FleetOrderKind.Hold&&f.Members.Length<=d.FormationLimit).OrderBy(f=>f.Members.Min()).ToArray();
            foreach(var group in groups)
            {
                var station=own.Ships.Single(s=>s.Id==group.Flagship).Star;
                var other=groups.FirstOrDefault(f=>f.Id!=group.Id&&(group.Members.Length==1||f.Members.Length==1)&&group.Members.Length+f.Members.Length<=d.FormationLimit&&own.Ships.Single(s=>s.Id==f.Flagship).Star==station&&f.Officer is null);
                if(other is not null){result.Add(new FleetCommand(view.Viewer,view.Turn,new FleetAction.Merge(group.Id,ImmutableArray.Create(other.Id))));return result.ToImmutable();}
            }
            if(view.Fleets.Fleets.Count(f=>f.Members.Any(id=>own.Ships.Any(s=>s.Id==id&&s.Military)))<2&&view.Fleets.Fleets.Length<content.FleetCommand!.FleetLimit)
            {
                var members=own.Ships.Where(s=>Idle(s)&&s.Fleet is null).GroupBy(s=>s.Star).OrderBy(g=>g.Min(s=>s.Id)).FirstOrDefault();
                if(members is not null)result.Add(new FleetCommand(view.Viewer,view.Turn,new FleetAction.Create("Defense force",members.OrderBy(s=>s.Id).Take(d.FormationLimit).Select(s=>s.Id).ToImmutableArray())));
                else if(groups.Length==1&&groups[0].Members.Length>=4)
                {
                    var split=groups[0];var spare=split.Members.Where(id=>id!=split.Flagship).OrderBy(id=>id).Take(split.Members.Length/2).ToImmutableArray();
                    result.Add(new FleetCommand(view.Viewer,view.Turn,new FleetAction.Split(split.Id,"Spare defense force",spare)));
                }
            }
        }
        if(stage==ConflictPlanningStage.Orders)
        {
            foreach(var ship in own.Ships.Where(s=>s.Fleet is null&&Idle(s)&&!Healthy(s)).OrderBy(s=>s.Id))
                if(Port(ship.Star!.Value) is {} port)
                {
                    var home=view.Colonies.Single(c=>c.Id==port);
                    result.Add(home.StarId==ship.Star?new CombatActionCommand(view.Viewer,view.Turn,new CombatAction.SetRepair(ImmutableArray.Create(ship.Id),true),""):new SetMoveOrderCommand(view.Viewer,view.Turn,ship.Id,home.StarId));
                }
            var forces=view.Fleets!.Fleets.Where(f=>f.Order.Kind==FleetOrderKind.Hold&&f.Cohort is null&&f.Members.All(id=>own.Ships.Any(s=>s.Id==id&&Idle(s)))).OrderBy(f=>f.Id).ToArray();
            FleetId? retained=forces.Where(f=>f.Members.All(id=>Healthy(own.Ships.Single(s=>s.Id==id)))&&view.Colonies.Any(c=>c.StarId==own.Ships.Single(s=>s.Id==f.Flagship).Star)).Select(f=>(FleetId?)f.Id).FirstOrDefault();
            foreach(var fleet in forces)
            {
                var members=fleet.Members.Select(id=>own.Ships.Single(s=>s.Id==id)).ToArray();var star=members[0].Star!.Value;
                if(members.Any(s=>!Healthy(s)))
                {
                    if(Port(star) is {} port)
                    {
                        var home=view.Colonies.Single(c=>c.Id==port);Command? repair=home.StarId==star?new CombatActionCommand(view.Viewer,view.Turn,new CombatAction.SetRepair(fleet.Members,true),""):new FleetCommand(view.Viewer,view.Turn,new FleetAction.SetOrder(ImmutableArray.Create(fleet.Id),new FleetOrder(FleetOrderKind.Return,home.StarId)));
                        result.Add(repair);
                    }
                    continue;
                }
                var war=own.Relations.SingleOrDefault(r=>r.Relation==RelationKind.War);if(war is null)continue;
                var enemies=own.Military.Where(s=>s.Current&&s.Owner==war.Other&&s.Star==star).OrderBy(s=>s.Vessel).ToArray();
                if(enemies.Length>0)
                {
                    var anchor=enemies[0];var defenders=anchor.Fleet is {} f?enemies.Where(s=>s.Fleet==f).ToArray():new[]{anchor};
                    int Fire(int attack,int hp,int max)=>checked((int)((Int128)attack*content.Combat!.AttackPercent[hp*100L>max*(long)content.Combat.HullThresholds[0]?0:hp*100L>max*(long)content.Combat.HullThresholds[1]?1:2]/100));
                    if(members.Length>=2&&members.Sum(s=>(long)Fire(s.Attack,s.Hull,s.MaxHull))>=defenders.Sum(s=>(long)Fire(s.Attack,s.Hull,s.MaxHull))&&members.Sum(s=>(long)s.Hull)>=defenders.Sum(s=>(long)s.Hull)&&Port(star,true) is {} port)Emit(new ConflictAction.Attack(fleet.Id,anchor.Vessel,new(CombatDoctrine.Cautious,port,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair)));
                    else
                    {
                        var defense=new ConflictOrders(CombatDoctrine.Cautious,Port(star,true),StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair);
                        if(members[0].Defense!=defense)Emit(new ConflictAction.ConfigureDefense(members[0].Id,defense));
                    }
                    continue;
                }
                if(view.Stars.Any(s=>s.Chart.Id==star&&s.Current&&s.Planets is {} planets&&planets.Any(p=>p.OwnerId==war.Other)))Emit(new ConflictAction.Blockade(fleet.Id,war.Other));
                else
                {
                    bool OwnDanger(StarId target)=>own.OwnPressure.Any(p=>p.Star==target)||view.Colonies.Any(c=>c.StarId==target)&&own.Military.Any(s=>s.Current&&s.Owner==war.Other&&s.Star==target);
                    var destination=view.Stars.Where(s=>s.Chart.Id!=star&&(OwnDanger(s.Chart.Id)||s.Planets is {} planets&&planets.Any(p=>p.OwnerId==war.Other))).OrderBy(s=>OwnDanger(s.Chart.Id)?0:1).ThenBy(s=>Distance(star,s.Chart.Id)).ThenBy(s=>s.Chart.Id).FirstOrDefault();
                    if(fleet.Id==retained&&(forces.Length>1||destination is not null&&!OwnDanger(destination.Chart.Id)))continue;
                    if(destination is not null)result.Add(new FleetCommand(view.Viewer,view.Turn,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(fleet.Id),destination.Chart.Id)));
                }
            }
        }
        return result.ToImmutable();
    }
}
