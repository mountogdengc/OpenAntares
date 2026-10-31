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
public static class ConflictQueries
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
            return new OwnConflictEncounterView(e.Id,e.Pair.Other(viewer),e.Star,own.Fleet,e.Revision,e.Round,own.WithdrawQueued?ConflictPhase.WithdrawalQueued:own.NeedsResponse?ConflictPhase.ResponseRequired:e.Phase,own.Members.ToImmutableArray(),own.ProtectedCivilians.ToImmutableArray(),own.Orders,own.WithdrawQueued,own.NeedsResponse,military.Where(o=>other.Members.Contains(o.Vessel)).ToImmutableArray());
        }).ToImmutableArray();
        var blockades=owner.Blockades.OrderBy(b=>b.Fleet).Select(b=>new OwnBlockadeView(b.Fleet,b.Other,b.Star,(long)b.AcceptedTurn+1,b.AcceptedTurn>=state.Turn?"Pending":"Awaiting evaluation")).ToImmutableArray();
        var history=owner.History.Select(h=>new ConflictHistoryView(h.Encounter,h.Other,h.Star,h.StartedTurn,h.EndedTurn,h.Outcome,h.LostMembers.ToImmutableArray(),h.Rounds.ToImmutableArray())).ToImmutableArray();
        return new(viewer,state.Turn,owner.Revision,relations,ships,military,encounters,blockades,owner.Events.ToImmutableArray(),owner.Notices.ToImmutableArray(),history){OwnAttacks=owner.Attacks.ToImmutableArray()};
    }
    public static ConflictPreview Preview(ContentSet content,EmpireView view,ConflictAction action)=>new(false,"conflict_action_unavailable",ConflictTokens.For(content,view,action));
}
