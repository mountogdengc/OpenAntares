using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Commands;
public static partial class CommandProcessor
{
    private static CommandOutcome ExecuteScout(ContentSet content,GameState state,EmpireState issuer,ScoutCommand command)
    {
        CommandOutcome Reject(string code)=>CommandOutcome.Reject(state,code,"Scout order is unavailable, stale, or requires the displayed encounter response.");
        if(!Rules.HasScouting(state.RulesVersion)||issuer.Scouting is null)return Reject("scout_unavailable");
        if(command is AcknowledgeAmebaCommand ack)
        {
            var expected=ScoutQueries.Project(content,state,issuer.Id).Attention;
            if(ack.Tokens.IsDefault||!ack.Tokens.SequenceEqual(expected))return Reject("ameba_encounter_stale");
            if(issuer.Scouting.Receipts.Select(r=>r.Token).SequenceEqual(expected))return CommandOutcome.NoChange(state);
            return ApplyPlanningChange(state,issuer.Id,s=>s.FindEmpire(issuer.Id)!.Scouting!.Receipts=expected.Select(t=>new AmebaReceipt(t,state.Turn)).ToList());
        }
        VesselId id=command switch{SetScoutPostureCommand p=>p.Vessel,SetAmebaRiskOrderCommand r=>r.Vessel,SetAmebaApproachOrderCommand a=>a.Vessel,SetScoutRetreatOrderCommand r=>r.Vessel,_=>default};
        var v=state.FindVessel(id);if(v is null||v.EmpireId!=issuer.Id)return Reject(RejectionCodes.NotOwner);
        if(command is SetScoutPostureCommand posture)
        {
            if(v.Scout is null||!Enum.IsDefined(posture.Posture))return Reject("scout_not_eligible");
            if(v.Scout.Posture==posture.Posture)return CommandOutcome.NoChange(state);
            var view=EmpireViewProjector.ProjectBase(content,state,issuer.Id);
            bool cancel=posture.Posture==ScoutPosture.AvoidEncounters&&v.PendingMove is {} destination&&(issuer.Scouting.KnownHabitats.Any(h=>h.Star==destination)||ScoutQueries.NeutralExcluded(view,destination));
            return ApplyPlanningChange(state,issuer.Id,s=>{var unit=s.FindVessel(id)!;unit.Scout!.Posture=posture.Posture;if(cancel){unit.PendingMove=null;unit.Scout.ApproachAcceptance=null;}});
        }
        if(v.Transit is not null)return Reject("vessel_in_transit");
        if(command is SetAmebaApproachOrderCommand approach)
        {
            if(state.FindStar(approach.Destination) is null)return Reject("unknown_star");
            if(v.Scout is null)return Reject("scout_not_eligible");
            if(v.Scout.Posture!=ScoutPosture.Observe)return Reject("scout_destination_excluded");
            if(!issuer.Scouting.KnownHabitats.Any(h=>h.HabitatId==approach.HabitatId&&h.Star==approach.Destination))return Reject("ameba_encounter_stale");
            return SetMoveOrder(content,state,new(issuer.Id,state.Turn,id,approach.Destination),new(approach.Destination,approach.HabitatId));
        }
        if(command is SetScoutRetreatOrderCommand retreat)
        {
            if(v.Scout is null||v.AmebaEncounter is null)return Reject("scout_not_eligible");
            return SetMoveOrder(content,state,new(issuer.Id,state.Turn,id,retreat.Destination),retreat:true);
        }
        if(command is not SetAmebaRiskOrderCommand risk)return Reject("scout_unavailable");
        var encounter=v.AmebaEncounter;
        if(encounter is null||encounter.Number!=risk.EncounterNumber||encounter.Revision!=risk.ExpectedRevision||risk.Action is {} action&&!Enum.IsDefined(action))return Reject("ameba_encounter_stale");
        if(risk.Action==AmebaRiskAction.Study)
        {
            if(v.Scout is null)return Reject("scout_not_eligible");
            if(issuer.Scouting.SuccessfulStudies.Any(c=>c.HabitatId==encounter.HabitatId))return Reject("ameba_study_already_rewarded");
            if(encounter.Stage!=AmebaStage.Warning)return Reject("ameba_encounter_stale");
            if(state.Vessels.Any(other=>other.EmpireId==issuer.Id&&other.Id!=id&&other.AmebaEncounter?.HabitatId==encounter.HabitatId&&other.AmebaEncounter.RiskOrder?.Action==AmebaRiskAction.Study))return Reject("ameba_study_reserved");
        }
        if(encounter.RiskOrder?.Action==risk.Action)return CommandOutcome.NoChange(state);
        return ApplyPlanningChange(state,issuer.Id,s=>{var unit=s.FindVessel(id)!;unit.AmebaEncounter!.RiskOrder=risk.Action is {} a?new(a,state.Turn,encounter.Revision):null;if(risk.Action is not null){unit.PendingMove=null;unit.PendingFounding=null;if(unit.Scout is {} scout)scout.ApproachAcceptance=null;}});
    }

    private static CommandOutcome? CheckScoutDestination(ContentSet content,GameState state,VesselState vessel,StarId destination,AmebaApproach? acceptance,bool retreat)
    {
        if(!Rules.HasScouting(state.RulesVersion))return null;
        var own=state.FindEmpire(vessel.EmpireId)!.Scouting!;
        if(own.KnownHabitats.FirstOrDefault(h=>h.Star==destination) is {} habitat)
        {
            if(vessel.Scout is null)return CommandOutcome.Reject(state,"scout_not_eligible","Population expeditions cannot approach a known ameba.");
            if(vessel.Scout.Posture==ScoutPosture.AvoidEncounters)return CommandOutcome.Reject(state,"scout_destination_excluded","Avoid encounters excludes this system.");
            if(acceptance!=new AmebaApproach(destination,habitat.HabitatId)&&vessel.Scout.ApproachAcceptance!=new AmebaApproach(destination,habitat.HabitatId))return CommandOutcome.Reject(state,"ameba_approach_confirmation_required","Confirm the known habitat approach explicitly.");
        }
        var view=EmpireViewProjector.ProjectBase(content,state,vessel.EmpireId);
        if(vessel.Scout?.Posture==ScoutPosture.AvoidEncounters&&ScoutQueries.NeutralExcluded(view,destination))
        {
            if(!retreat||vessel.AmebaEncounter is null||ScoutQueries.Project(content,state,vessel.EmpireId).Vessels.First(v=>v.Id==vessel.Id).EffectiveDestination!=destination)return CommandOutcome.Reject(state,"scout_destination_excluded","Avoid encounters excludes this system.");
        }
        if(retreat&&ScoutQueries.Project(content,state,vessel.EmpireId).Vessels.First(v=>v.Id==vessel.Id).EffectiveDestination!=destination)return CommandOutcome.Reject(state,"scout_destination_excluded","Retreat must match the current survival preview.");
        return null;
    }
    internal static void ReconcileFleetConsent(ContentSet content,GameState before,GameState after,EmpireId owner)=>ReconcileScoutConsent(content,before,after,owner);
    private static void ReconcileScoutConsent(ContentSet content,GameState before,GameState after,EmpireId owner)
    {
        var old=ScoutQueries.Project(content,before,owner).Attention;var current=ScoutQueries.Project(content,after,owner).Attention;var e=after.FindEmpire(owner)!.Scouting!;
        foreach(var token in current)
        {
            var previous=old.FirstOrDefault(t=>t.Vessel==token.Vessel);
            if(previous is null||(previous with{Revision=0})==(token with{Revision=0}))continue;
            var encounter=after.FindVessel(token.Vessel)!.AmebaEncounter!;
            long revision=checked(encounter.Revision+1);if(revision==long.MaxValue)throw new OverflowException();encounter.Revision=revision;
            if(encounter.RiskOrder is {} order)encounter.RiskOrder=order with{Revision=revision};
            e.Receipts.RemoveAll(r=>r.Token.Vessel==token.Vessel);
        }
        var fresh=ScoutQueries.Project(content,after,owner).Attention;e.Receipts.RemoveAll(r=>!fresh.Contains(r.Token));
    }
}
