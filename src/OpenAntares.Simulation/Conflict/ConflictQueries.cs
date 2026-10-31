using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public static partial class ConflictQueries
{
    public static ConflictView Project(ContentSet content,GameState state,EmpireId viewer)
    {
        if(!Rules.HasConflict(state.RulesVersion)||state.FindEmpire(viewer) is not {} empire||state.Conflict is not {} conflict)throw new ArgumentException("An own conflict campaign is required.");
        var owner=conflict.Owners.Single(o=>o.Owner==viewer);var d=content.Conflict!;
        var military=ConflictObservations.ForViewer(state,viewer);
        var relations=conflict.Pairs.Where(p=>p.Pair.Contains(viewer)&&empire.Contacts.Any(c=>c.OtherEmpireId==p.Pair.Other(viewer))).OrderBy(p=>p.Pair).Select(p=>
        {
            var own=p.Parties.Single(p=>p.Owner==viewer);var other=p.Parties.Single(p=>p.Owner!=viewer);var offer=p.PendingPeace;
            PeaceOfferView? peace=offer is null||offer.CreatedTurn>state.Turn?null:new(offer.Id,offer.Proposer,offer.CreatedTurn,offer.ExpiryTurn,offer.Proposer==viewer?null:offer.Answer);
            var intent=p.PeaceIntents.SingleOrDefault(i=>i.Owner==viewer);
            int Penalty(ConflictParty party)=>party.DeclarationGrievance&&party.GrievancePeaceBoundaries<d.GrievancePeaceLifetime?d.DeclarationGrievance:0;
            return new ConflictRelationView(p.Pair.Other(viewer),p.Relation,p.Revision,own.PrivateRevision,p.WarTurn,p.TruceEndTurn,p.Declarations.Any(i=>i.Owner==viewer),intent is {Proposed:true,Withdrawn:false,Revoked:false},peace,Penalty(own),Penalty(other),own.GrievancePeaceBoundaries,own.ClaimObservedSince,own.LastVisibleActivityTurn,own.NextPeaceOfferTurn);
        }).ToImmutableArray();
        var ships=state.Vessels.Where(v=>v.EmpireId==viewer).OrderBy(v=>v.Id).Select(v=>
        {
            var load=VesselLoadouts.Resolve(content,state,v);var p=load.Profile!;var fleet=v.Fleet is {} fid?empire.Fleets!.Fleets.Single(f=>f.Id==fid):null;
            bool participatingFlagship=fleet is null||state.FindVessel(fleet.Flagship) is {} flagship&&ConflictObservations.IsMilitary(content,state,flagship);
            int nav=participatingFlagship?OfficerBonuses.NavigatorBonus(content,state,v):0;
            var stats=CombatMath.Stats(p,v.Combat!.Hull,load.BaseSpeed,nav,content.Combat);
            var pref=owner.Preferences.SingleOrDefault(x=>v.Fleet is {} group?x.Fleet==group:x.Fleet is null&&x.Anchor==v.Id);
            bool inConflict=conflict.Encounters.Any(e=>e.Phase!=ConflictPhase.Completed&&(e.LowSide.Owner==viewer&&e.LowSide.Members.Contains(v.Id)||e.HighSide.Owner==viewer&&e.HighSide.Members.Contains(v.Id)));
            return new OwnConflictShipView(v.Id,v.Fleet,v.StationaryStarId,v.Combat.Hull,p.MaxHull,v.Combat.Cohesion,p.Attack,p.Armor,stats.Speed,ConflictObservations.IsMilitary(content,state,v),v.NewlyCompleted,v.Combat.Repairing,empire.ShipDesigns!.Refits.Any(j=>j.Vessel==v.Id),state.Combat!.Encounters.Any(e=>e.Members.Contains(v.Id)),inConflict,empire.Fleets!.Requests.Any(r=>FleetQueries.IsActive(r.Status)&&r.Slots.Any(s=>s.Vessel==v.Id)),pref?.Orders??ConflictOrders.Default);
        }).ToImmutableArray();
        var encounters=conflict.Encounters.Where(e=>e.Pair.Contains(viewer)&&e.Phase!=ConflictPhase.Completed).OrderBy(e=>e.Id).Select(e=>
        {
            var own=e.LowSide.Owner==viewer?e.LowSide:e.HighSide;var other=e.LowSide.Owner==viewer?e.HighSide:e.LowSide;
            return new OwnConflictEncounterView(e.Id,e.Pair.Other(viewer),e.Star,own.Fleet,own.ConsentRevision,e.Round,own.WithdrawQueued?ConflictPhase.WithdrawalQueued:own.NeedsResponse?ConflictPhase.ResponseRequired:e.Phase,own.Members.ToImmutableArray(),own.ProtectedCivilians.ToImmutableArray(),own.Orders,own.WithdrawQueued,own.NeedsResponse,military.Where(o=>other.Members.Contains(o.Vessel)).ToImmutableArray());
        }).ToImmutableArray();
        var blockadeFrame=ConflictBlockades.Capture(content,state);
        var blockades=blockadeFrame.Orders.Where(b=>b.Owner==viewer).Select(b=>new OwnBlockadeView(b.Fleet,b.Other,b.Star,b.EffectiveTurn,b.Status)).ToImmutableArray();
        var history=owner.History.Select(h=>new ConflictHistoryView(h.Encounter,h.Other,h.Star,h.StartedTurn,h.EndedTurn,h.Outcome,h.LostMembers.ToImmutableArray(),h.Rounds.ToImmutableArray())).ToImmutableArray();
        return new(viewer,state.Turn,owner.Revision,relations,ships,military,encounters,blockades,owner.Events.ToImmutableArray(),owner.Notices.ToImmutableArray(),history){OwnAttacks=owner.Attacks.ToImmutableArray(),OwnPressure=ConflictForecastFrame.ForViewer(blockadeFrame,viewer)};
    }
    public static ConflictPreview Preview(ContentSet content,EmpireView view,ConflictAction action)
    {
        string token=ConflictTokens.For(content,view,action);bool allowed=false;long? effective=null;
        if(Rules.HasConflict(view.RulesVersion)&&view.Conflict is {} own)
        {
            EmpireId? other=action switch{ConflictAction.DeclareWar a=>a.Other,ConflictAction.RevokeDeclaration a=>a.Other,_=>ConflictPeace.Other(action)};
            var relation=own.Relations.SingleOrDefault(r=>r.Other==other);
            if(relation is not null)
            {
                if(action is ConflictAction.DeclareWar&&view.Turn<int.MaxValue&&relation.Relation==RelationKind.Peace&&(relation.TruceEndTurn is null||relation.TruceEndTurn<=view.Turn)&&!own.Relations.Any(r=>r.Relation==RelationKind.War))
                {allowed=true;effective=(long)view.Turn+1;}
                else if(action is ConflictAction.RevokeDeclaration&&relation.Relation==RelationKind.Peace)allowed=true;
                else if(ConflictPeace.Allowed(relation,view.Viewer,action,view.Turn)){allowed=true;effective=(long)view.Turn+1;}
            }
        }
        if(!allowed&&Rules.HasConflict(view.RulesVersion))allowed=ConflictEligibility.Allowed(content,view,action);
        return new(allowed,allowed?null:"conflict_action_unavailable",token){Volley=allowed?PreviewVolley(content,view,action):null,Withdrawal=allowed?ConflictWithdrawal.Preview(content,view,action):null,EffectiveTurn=effective,Explanations=allowed&&action is ConflictAction.DeclareWar?ImmutableArray.Create("Declaration takes effect at the next publication; no attack or blockade is authorized this planning turn. Counterpart availability is rechecked at resolution."):allowed&&action is ConflictAction.Attack?ImmutableArray.Create("This commits only your armed fleet. The observed anchor is checked again at resolution; changed forces can prevent the attack. Protected civilians cannot be targeted."):allowed&&action is ConflictAction.Blockade?ImmutableArray.Create("The order can first reduce production after its next publication. Current planning income is unchanged. Movement, repair, refit or regrouping lifts it immediately."):allowed&&action is ConflictAction.ConfigureDefense?ImmutableArray.Create("These are standing defensive preferences. With no selected return port, withdrawal uses local Break contact and may take an interception volley."):ImmutableArray<string>.Empty};
    }
}
