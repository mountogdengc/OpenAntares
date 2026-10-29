using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public static class CombatCommandProcessor
{
    public static CommandOutcome Execute(ContentSet content,GameState state,CombatActionCommand command)
    {
        CommandOutcome Reject(string code)=>CommandOutcome.Reject(state,code,"Combat order is unavailable, stale, or requires its displayed response.");
        var owner=state.FindEmpire(command.Issuer);
        if(owner is null)return Reject(RejectionCodes.UnknownEmpire);
        if(command.ExpectedTurn!=state.Turn)return Reject(RejectionCodes.StaleTurn);
        if(!Rules.HasCombat(state.RulesVersion)||state.Combat is null||owner.Combat is null||command.Action is null)return Reject(CombatRejectionCodes.Unavailable);
        if(command.Action is CombatAction.AttackPocket own&&owner.Fleets!.Fleets.All(f=>f.Id!=own.Fleet)||command.Action is CombatAction.SetRepair ships&&(ships.Vessels.IsDefault||ships.Vessels.Any(id=>state.FindVessel(id)?.EmpireId!=owner.Id)))return Reject(RejectionCodes.NotOwner);
        var id=command.Action switch {CombatAction.ChangeCombatOrders a=>(EncounterId?)a.Encounter,CombatAction.WithdrawCombat a=>a.Encounter,CombatAction.RespondCombat a=>a.Encounter,_=>null};
        if(id is {} reference&&state.Combat.Encounters.All(e=>e.Id!=reference||e.Owner!=owner.Id))return Reject(RejectionCodes.NotOwner);
        var view=EmpireViewProjector.Project(content,state,owner.Id);var preview=CombatQueries.Preview(content,view,command.Action);
        if(!preview.Allowed)return Reject(preview.RejectionCode??CombatRejectionCodes.Unavailable);
        if(string.IsNullOrEmpty(command.PreviewToken)||!string.Equals(preview.Token,command.PreviewToken,StringComparison.Ordinal))return Reject(CombatRejectionCodes.StalePreview);
        var candidate=state.Clone();var c=candidate.Combat!;var e=candidate.FindEmpire(owner.Id)!;var ownCombat=e.Combat!;bool changed=true;
        EncounterState Encounter(EncounterId id)=>c.Encounters.Single(x=>x.Id==id&&x.Owner==owner.Id);
        void ClearResponse(EncounterId id)=>ownCombat.Responses.RemoveAll(r=>r.Encounter==id);
        switch(command.Action)
        {
            case CombatAction.AttackPocket attack:
                long number=c.NextEncounterId;c.NextEncounterId=checked(number+1);
                c.Encounters.Add(new(){Id=new(number),Owner=owner.Id,Fleet=attack.Fleet,Pocket=attack.Pocket,Members=preview.AffectedShips.Select(s=>s.VesselId).ToList(),Orders=attack.Orders,Phase=CombatPhase.Pending,ConsentRevision=1,CivilianRiskAuthorized=attack.AuthorizeCivilianRisk});
                ownCombat.LifetimeEncounters=checked(ownCombat.LifetimeEncounters+1);break;
            case CombatAction.ChangeCombatOrders change:
                var encounter=Encounter(change.Encounter);
                if(encounter.Orders==change.Orders&&encounter.CivilianRiskAuthorized==change.AuthorizeCivilianRisk&&!ownCombat.Responses.Any(r=>r.Encounter==encounter.Id))return CommandOutcome.NoChange(state);
                encounter.Orders=change.Orders;encounter.CivilianRiskAuthorized=change.AuthorizeCivilianRisk;encounter.Revision=checked(encounter.Revision+1);encounter.ConsentRevision=encounter.Revision;encounter.ContinueRiskTurn=null;
                if(encounter.Phase==CombatPhase.BlockedResponse)encounter.Phase=encounter.Round==0?CombatPhase.Pending:CombatPhase.Active;
                ClearResponse(encounter.Id);break;
            case CombatAction.WithdrawCombat withdraw:
                var departing=Encounter(withdraw.Encounter);
                if(departing.Round==0)
                {
                    c.Encounters.Remove(departing);ClearResponse(departing.Id);e.Ready=false;
                    var destination=candidate.FindPlanet(candidate.FindColony(departing.Orders.ReturnColony)!.PlanetId)!.StarId;
                    // Cancel the reservation first, then use the ordinary fleet route validator.
                    var move=FleetCommandProcessor.Execute(content,candidate,new(owner.Id,state.Turn,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(departing.Fleet),destination)));
                    if(!move.Accepted)return Reject(CombatRejectionCodes.Unavailable);candidate=move.State;
                    foreach(var member in departing.Members){var ship=candidate.FindVessel(member)!.Combat!;ship.ReturnColony=departing.Orders.ReturnColony;ship.OnArrival=departing.Orders.OnArrival;ship.Repairing=false;}
                }
                else
                {
                    if(departing.Phase==CombatPhase.WithdrawalQueued)return CommandOutcome.NoChange(state);
                    departing.Phase=CombatPhase.WithdrawalQueued;departing.Revision=checked(departing.Revision+1);departing.ConsentRevision=departing.Revision;ClearResponse(departing.Id);
                }
                break;
            case CombatAction.RespondCombat response:
                var responding=Encounter(response.Encounter);var pending=ownCombat.Responses.Single(r=>r.Encounter==responding.Id);
                var accepted=response.Response==CombatResponse.AcceptRevisedForce?CombatResponseNeed.RevisedForce:CombatResponseNeed.BlockedReturn;
                if(accepted==CombatResponseNeed.RevisedForce)responding.ConsentRevision=responding.Revision;else responding.ContinueRiskTurn=state.Turn;
                var outstanding=pending.Need&~accepted;ClearResponse(responding.Id);
                if(outstanding!=0)ownCombat.Responses.Add(pending with {Need=outstanding});else responding.Phase=responding.Round==0?CombatPhase.Pending:CombatPhase.Active;
                ownCombat.Responses=ownCombat.Responses.OrderBy(r=>r.Encounter).ToList();break;
            case CombatAction.SetRepair repair:
                changed=false;
                foreach(var member in repair.Vessels)
                {
                    var ship=candidate.FindVessel(member)!.Combat!;
                    if(repair.Enabled)
                    {
                        var port=preview.RepairPorts[member];var profile=content.Combat!.RoleProfiles[candidate.FindVessel(member)!.RoleId];if(ship.Hull==profile.MaxHull&&ship.Cohesion==100)continue;
                        if(ship.Repairing&&ship.ReturnColony==port&&ship.OnArrival==CombatArrivalOrder.Repair)continue;
                        ship.Repairing=true;ship.ReturnColony=port;ship.OnArrival=CombatArrivalOrder.Repair;changed=true;
                    }
                    else if(ship.Repairing||ship.ReturnColony is not null||ship.OnArrival is not null){ship.Repairing=false;ship.ReturnColony=null;ship.OnArrival=null;changed=true;}
                }
                break;
            default:return Reject(CombatRejectionCodes.Unavailable);
        }
        if(!changed)return CommandOutcome.NoChange(state);
        candidate.FindEmpire(owner.Id)!.Ready=false;
        if(GameStateValidator.Validate(content,candidate).Count!=0)return Reject(CombatRejectionCodes.Unavailable);
        return new(candidate,null,true,null);
    }
}
