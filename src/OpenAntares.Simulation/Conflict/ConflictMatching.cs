using System;
using System.Linq;
using System.Collections.Immutable;
using System.Collections.Generic;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public static class ConflictMatching
{
    public static void Resolve(ContentSet content,GameState snapshot,GameState candidate,ConflictResolutionContext context)
    {
        if(!context.RelationsPrepared||context.MatchingResolved||snapshot.Turn!=context.FromTurn||candidate.Turn!=snapshot.Turn)throw new InvalidOperationException("Conflict matching already applied or snapshot changed.");
        context.MatchingResolved=true;if(!Rules.HasConflict(snapshot.RulesVersion))return;
        var conflict=candidate.Conflict!;var frames=ImmutableArray.CreateBuilder<ConflictBattleFrame>();
        bool LostOrChanged(VesselId id)=>snapshot.FindVessel(id) is not {} before||candidate.FindVessel(id) is not {} after||before.Combat!.Hull!=after.Combat!.Hull||before.StationaryStarId!=after.StationaryStarId||before.Transit!=after.Transit||before.Design!=after.Design;
        bool ActiveSentry(IEnumerable<VesselId> ids)=>candidate.Combat!.Encounters.Any(e=>e.Phase!=CombatPhase.Pending&&e.Members.Any(ids.Contains));
        foreach(var encounter in conflict.Encounters.Where(e=>e.Phase!=ConflictPhase.Completed).OrderBy(e=>e.Id).ToArray())
        {
            if(context.AcceptedPeace.Contains(encounter.Pair))continue;
            var sides=new[]{encounter.LowSide,encounter.HighSide};var changed=sides.Where(s=>s.Members.Any(LostOrChanged)).ToArray();
            foreach(var side in sides)
            {
                var missing=side.Members.Where(id=>candidate.FindVessel(id) is null).ToArray();side.Members.RemoveAll(missing.Contains);side.DestroyedMembers.AddRange(missing.Where(id=>!side.DestroyedMembers.Contains(id)));side.DestroyedMembers.Sort();
                side.ProtectedCivilians.RemoveAll(id=>candidate.FindVessel(id) is null);
                context.UsedSet.UnionWith(side.Members);
            }
            if(changed.Length>0)
            {
                encounter.Revision=checked(encounter.Revision+1);
                if(encounter.Phase==ConflictPhase.Pending||sides.Any(s=>s.Members.Count==0)){Close(encounter);continue;}
                foreach(var side in changed)side.NeedsResponse=true;
                foreach(var side in sides)
                {
                    side.ConsentRevision=checked(side.ConsentRevision+1);
                    ConflictRelations.RecordEvent(candidate,side.Owner,encounter.Pair.Other(side.Owner),"force_changed",side.NeedsResponse?"Your participating force changed. No volley occurs this boundary; review the revised force or withdraw.":"Encounter paused: force changed. No volley occurs this boundary.",encounter.Star,attention:true);
                }
                context.PausedSet.Add(encounter.Id);continue;
            }
            if(sides.Any(s=>s.NeedsResponse)||ActiveSentry(sides.SelectMany(s=>s.Members))){context.PausedSet.Add(encounter.Id);continue;}
            frames.Add(ConflictBattleFrame.Capture(content,candidate,encounter));
        }
        var intents=snapshot.Conflict!.Owners.SelectMany(o=>o.Attacks.Select(a=>new{Owner=o.Owner,Attack=a,Fleet=snapshot.FindEmpire(o.Owner)!.Fleets!.Fleets.SingleOrDefault(f=>f.Id==a.Fleet)}))
            .Where(i=>i.Fleet is not null).OrderBy(i=>snapshot.FindVessel(i.Fleet!.Members.Min())?.StationaryStarId?.Value??int.MaxValue)
            .ThenBy(i=>DiplomacyPair.For(i.Owner,snapshot.FindVessel(i.Attack.Target)?.EmpireId??i.Owner)).ThenBy(i=>i.Fleet!.Members.Min());
        foreach(var intent in intents)
        {
            var target=candidate.FindVessel(intent.Attack.Target);var fleet=intent.Fleet!;
            var ownIds=fleet.Members;var key=target is null?default:DiplomacyPair.For(intent.Owner,target.EmpireId);
            if(ownIds.Any(context.UsedSet.Contains))continue; // A crossed attack already matched this own force.
            if(target is null||context.AcceptedPeace.Contains(key)||conflict.Pairs.All(p=>p.Pair!=key||p.Relation!=RelationKind.War)||target.StationaryStarId is not {} star||target.Transit is not null||!ConflictObservations.IsMilitary(content,candidate,target)||ownIds.Any(LostOrChanged))
            {Fail(intent.Owner,target?.EmpireId??snapshot.FindVessel(intent.Attack.Target)!.EmpireId);continue;}
            var ownShips=ownIds.Select(candidate.FindVessel).ToArray();
            if(ownShips.Any(v=>v is null||v.StationaryStarId!=star||v.Transit is not null||!ConflictObservations.IsMilitary(content,candidate,v))) {Fail(intent.Owner,target.EmpireId);continue;}
            var otherFleet=target.Fleet is {} f?candidate.FindEmpire(target.EmpireId)!.Fleets!.Fleets.SingleOrDefault(x=>x.Id==f):null;
            var allDefense=(otherFleet?.Members??new List<VesselId>{target.Id}).Select(candidate.FindVessel).Where(v=>v is not null&&v.StationaryStarId==star&&v.Transit is null).Select(v=>v!).OrderBy(v=>v.Id).ToArray();
            var armed=allDefense.Where(v=>ConflictObservations.IsMilitary(content,candidate,v)).Select(v=>v.Id).ToList();
            if(armed.Count is <1 or >32||armed.Any(LostOrChanged)||armed.Any(context.UsedSet.Contains)||ActiveSentry(ownIds.Concat(armed))||conflict.Encounters.Count(e=>e.Phase!=ConflictPhase.Completed&&e.Pair.Contains(intent.Owner))>=content.Conflict!.ActiveEncounterLimit||conflict.Encounters.Count(e=>e.Phase!=ConflictPhase.Completed&&e.Pair.Contains(target.EmpireId))>=content.Conflict.ActiveEncounterLimit)
            {Fail(intent.Owner,target.EmpireId);continue;}
            var own=new ConflictSideState{Owner=intent.Owner,Fleet=fleet.Id,Anchor=ownIds.Min(),Flagship=fleet.Flagship,Members=ownIds.OrderBy(id=>id).ToList(),Orders=intent.Attack.Orders};
            var preferences=conflict.Owners.Single(o=>o.Owner==target.EmpireId).Preferences.SingleOrDefault(p=>target.Fleet is {} group?p.Fleet==group:p.Fleet is null&&p.Anchor==target.Id);
            var other=new ConflictSideState{Owner=target.EmpireId,Fleet=target.Fleet,Anchor=target.Id,Flagship=otherFleet?.Flagship??target.Id,Members=armed,ProtectedCivilians=allDefense.Where(v=>!armed.Contains(v.Id)).Select(v=>v.Id).ToList(),Orders=preferences?.Orders??ConflictOrders.Default};
            var oppositeIntent=snapshot.Conflict.Owners.Single(o=>o.Owner==other.Owner).Attacks.SingleOrDefault(a=>a.Fleet==other.Fleet&&own.Members.Contains(a.Target));if(oppositeIntent is not null)other.Orders=oppositeIntent.Orders;
            var participants=own.Members.Concat(other.Members).ToArray();
            foreach(var pending in candidate.Combat!.Encounters.Where(e=>e.Phase==CombatPhase.Pending&&e.Members.Any(participants.Contains)).ToArray())CombatResolver.Finish(content,candidate,pending,"Superseded by an empire encounter; all ships retained.");
            var match=new ConflictEncounterState{Id=new(conflict.NextEncounterId),Pair=key,Star=star,CreatedTurn=snapshot.Turn,Phase=ConflictPhase.Pending,LowSide=own.Owner==key.Low?own:other,HighSide=own.Owner==key.High?own:other};conflict.NextEncounterId=checked(conflict.NextEncounterId+1);conflict.Encounters.Add(match);
            context.UsedSet.UnionWith(participants);foreach(var owner in conflict.Owners)owner.Blockades.RemoveAll(b=>key.Contains(owner.Owner)&&participants.Any(id=>candidate.FindVessel(id)?.EmpireId==owner.Owner&&candidate.FindVessel(id)?.Fleet==b.Fleet));
            frames.Add(ConflictBattleFrame.Capture(content,candidate,match));
        }
        foreach(var owner in conflict.Owners)owner.Attacks.Clear();context.Battles=frames.OrderBy(f=>f.Encounter).ToImmutableArray();
        void Fail(EmpireId owner,EmpireId other)=>ConflictRelations.RecordEvent(candidate,owner,other,"attack_unavailable","The observed target could not be engaged. Your force remains in Hold.");
        void Close(ConflictEncounterState encounter)
        {
            encounter.Phase=ConflictPhase.Completed;encounter.CompletedTurn=checked(snapshot.Turn+1);
            foreach(var side in new[]{encounter.LowSide,encounter.HighSide}){side.NeedsResponse=false;side.WithdrawQueued=false;if(side.Fleet is {} id&&candidate.FindEmpire(side.Owner)!.Fleets!.Fleets.SingleOrDefault(f=>f.Id==id) is {} fleet)fleet.Order=new(FleetOrderKind.Hold);}
        }
    }
}
