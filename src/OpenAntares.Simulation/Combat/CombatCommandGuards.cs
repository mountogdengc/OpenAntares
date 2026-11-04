using System;
using System.Linq;
using System.Collections.Generic;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Simulation.Combat;
internal static class CombatCommandGuards
{
    internal static bool Conflicts(EmpireView view,Command command)
    {
        if(Conflict.ConflictCommandGuards.Conflicts(view,command))return true;
        if(!Rules.HasCombat(view.RulesVersion)||view.Combat is not {} combat||combat.Encounters.IsEmpty)return false;
        var locked=combat.Encounters.SelectMany(e=>e.Members).ToHashSet();var fleets=combat.Encounters.Select(e=>e.Fleet).ToHashSet();
        bool Ships(IEnumerable<VesselId> ids)=>ids.Any(locked.Contains);
        bool Officer(OfficerId id)=>view.Fleets!.Officers.Any(o=>o.Id==id&&o.Flagship is {} ship&&locked.Contains(ship));
        bool Draft(AssemblyDraft draft)=>draft.RallyFleet is {} f&&fleets.Contains(f)||Ships(draft.AcceptedIdleVessels);
        return command switch
        {
            SetMoveOrderCommand a=>locked.Contains(a.Vessel),SetFoundingOrderCommand a=>locked.Contains(a.Vessel),
            SetScoutPostureCommand a=>locked.Contains(a.Vessel),SetScoutRetreatOrderCommand a=>locked.Contains(a.Vessel),SetAmebaRiskOrderCommand a=>locked.Contains(a.Vessel),SetAmebaApproachOrderCommand a=>locked.Contains(a.Vessel),
            FleetCommand a=>Fleet(a.Action),
            OfficerCommand {Action:OfficerAction.AssignFlagship a}=>fleets.Contains(a.Fleet)||Officer(a.Officer),
            OfficerCommand {Action:OfficerAction.Transfer a}=>locked.Contains(a.From)||locked.Contains(a.To)||Officer(a.Officer),
            OfficerCommand {Action:OfficerAction.Remove a}=>Officer(a.Officer),OfficerCommand {Action:OfficerAction.Post a}=>Officer(a.Officer),OfficerCommand {Action:OfficerAction.Dismiss a}=>Officer(a.Officer),
            AssemblyCommand {Action:AssemblyAction.Accept a}=>Draft(a.Draft),AssemblyCommand {Action:AssemblyAction.Amend a}=>Draft(a.Draft),AssemblyCommand {Action:AssemblyAction.ChangeRally a}=>a.RallyFleet is {} f&&fleets.Contains(f),
            AssemblyCommand {Action:AssemblyAction.Resume a}=>view.Fleets!.Requests.Any(r=>r.Id==a.Request&&r.RallyFleet is {} f&&fleets.Contains(f)),
            _=>false
        };
        bool Fleet(FleetAction action)=>action switch
        {
            FleetAction.Create a=>Ships(a.Members),FleetAction.Disband a=>fleets.Contains(a.Fleet),FleetAction.Merge a=>fleets.Contains(a.Target)||a.Sources.Any(fleets.Contains),FleetAction.Split a=>fleets.Contains(a.Source)||Ships(a.Members),FleetAction.SetFlagship a=>fleets.Contains(a.Fleet)||locked.Contains(a.Flagship),FleetAction.SetOrder a=>a.Fleets.Any(fleets.Contains),FleetAction.MoveSelection a=>Ships(a.Vessels)||a.Fleets.Any(fleets.Contains),FleetAction.DetachSelection a=>Ships(a.Vessels),_=>false
        };
    }
}
