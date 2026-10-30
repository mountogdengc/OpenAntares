using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Combat;
using System.Linq;
using System.Collections.Generic;
namespace OpenAntares.Simulation.ShipDesigns;
public static class RefitGuards
{
    public static bool Conflicts(EmpireView view,Command command)
    {
        if(!Rules.HasShipDesigns(view.RulesVersion)||view.ShipDesigns is not {} own||own.Refits.IsEmpty||view.Fleets is not {} fleets)return false;
        var locked=own.Refits.Select(j=>j.Vessel).ToHashSet();var ports=own.Refits.Select(j=>j.Port).ToHashSet();
        var groups=fleets.Fleets.Where(f=>f.Members.Any(locked.Contains)).ToArray();var groupIds=groups.Select(f=>f.Id).ToHashSet();
        bool Ships(IEnumerable<VesselId> ids)=>ids.Any(locked.Contains);
        bool ChangesFlagship(IEnumerable<VesselId> ids)=>groups.Any(f=>ids.Contains(f.Flagship));
        bool Officer(OfficerId id)=>groups.Any(f=>f.Officer==id)||fleets.Officers.Any(o=>o.Id==id&&o.Flagship is {} v&&locked.Contains(v));
        bool Request(RequestId id,bool yard)=>fleets.Requests.Any(r=>r.Id==id&&(yard&&r.Colonies.Any(ports.Contains)||r.RallyFleet is {} f&&groupIds.Contains(f)||r.Slots.Any(s=>s.Vessel is {} v&&locked.Contains(v))));
        bool Draft(AssemblyDraft d)=>d.Colonies.Any(ports.Contains)||Ships(d.AcceptedIdleVessels)||d.RallyFleet is {} f&&groupIds.Contains(f);
        return command switch
        {
            SetProjectCommand a=>ports.Contains(a.Colony),SetProductionTargetCommand a=>ports.Contains(a.Colony),SetColonyQueueCommand a=>ports.Contains(a.Colony),SetColonyAutomationCommand a=>a.ConstructionEnabled&&ports.Contains(a.Colony),
            AssignGovernorTemplateCommand a=>a.Colonies.Any(ports.Contains)&&(a.ConstructionEnabled||a.ReplaceActive),
            SetMoveOrderCommand a=>locked.Contains(a.Vessel),SetFoundingOrderCommand a=>locked.Contains(a.Vessel),
            SetScoutPostureCommand a=>locked.Contains(a.Vessel),SetScoutRetreatOrderCommand a=>locked.Contains(a.Vessel),SetAmebaRiskOrderCommand a=>locked.Contains(a.Vessel),SetAmebaApproachOrderCommand a=>locked.Contains(a.Vessel),
            FleetCommand a=>Fleet(a.Action),
            OfficerCommand {Action:OfficerAction.AssignFlagship a}=>groupIds.Contains(a.Fleet)||Officer(a.Officer),
            OfficerCommand {Action:OfficerAction.Transfer a}=>locked.Contains(a.From)||locked.Contains(a.To)||Officer(a.Officer)||ChangesFlagship(new[]{a.From,a.To}),
            OfficerCommand {Action:OfficerAction.Remove a}=>Officer(a.Officer),OfficerCommand {Action:OfficerAction.Post a}=>Officer(a.Officer),OfficerCommand {Action:OfficerAction.Dismiss a}=>Officer(a.Officer),
            AssemblyCommand {Action:AssemblyAction.Accept a}=>Draft(a.Draft),AssemblyCommand {Action:AssemblyAction.Amend a}=>Draft(a.Draft)||Request(a.Request,true),
            AssemblyCommand {Action:AssemblyAction.Resume a}=>Request(a.Request,true),AssemblyCommand {Action:AssemblyAction.SetPriority a}=>Request(a.Request,true),
            AssemblyCommand {Action:AssemblyAction.SetContributionPaused a}=>!a.Paused&&ports.Contains(a.Colony),
            AssemblyCommand {Action:AssemblyAction.Cancel a}=>a.Choice==RequestCancellation.RemoveUnstarted&&Request(a.Request,true),
            AssemblyCommand {Action:AssemblyAction.ChangeRally a}=>Request(a.Request,false)||a.RallyFleet is {} f&&groupIds.Contains(f),
            AssemblyCommand {Action:AssemblyAction.DetachSlot a}=>fleets.Requests.Any(r=>r.Id==a.Request&&r.Slots.Any(s=>s.Ordinal==a.Ordinal&&s.Vessel is {} v&&locked.Contains(v))),
            CombatActionCommand {Action:CombatAction.SetRepair a}=>Ships(a.Vessels),CombatActionCommand {Action:CombatAction.AttackPocket a}=>groupIds.Contains(a.Fleet),
            CombatActionCommand {Action:CombatAction.ChangeCombatOrders a}=>view.Combat?.Encounters.Any(e=>e.Id==a.Encounter&&Ships(e.Members))==true,
            _=>false
        };
        bool Fleet(FleetAction a)=>a switch
        {
            FleetAction.Create x=>Ships(x.Members),FleetAction.Disband x=>groupIds.Contains(x.Fleet),FleetAction.Merge x=>groupIds.Contains(x.Target)||x.Sources.Any(groupIds.Contains),
            FleetAction.Split x=>Ships(x.Members)||groupIds.Contains(x.Source)&&groups.Any(f=>f.Id==x.Source&&x.Members.Contains(f.Flagship)),
            FleetAction.SetFlagship x=>groupIds.Contains(x.Fleet)||locked.Contains(x.Flagship),FleetAction.SetOrder x=>x.Fleets.Any(groupIds.Contains),
            FleetAction.MoveSelection x=>Ships(x.Vessels)||x.Fleets.Any(groupIds.Contains)||ChangesFlagship(x.Vessels),FleetAction.DetachSelection x=>Ships(x.Vessels)||ChangesFlagship(x.Vessels),_=>false
        };
    }
}
