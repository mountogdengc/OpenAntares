using System.Linq;
using System.Collections.Generic;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.ShipDesigns;
namespace OpenAntares.Simulation.Conflict;
internal static class ConflictCommandGuards
{
    internal static bool Conflicts(EmpireView view,Command command)
    {
        if(view.Conflict is not {} own||view.Fleets is not {} fleets)return false;
        var groups=own.Encounters.Where(e=>e.OwnFleet is not null).Select(e=>e.OwnFleet!.Value).Concat(own.OwnAttacks.Select(i=>i.Fleet)).ToHashSet();
        var armed=own.Encounters.SelectMany(e=>e.OwnMembers).Concat(own.Ships.Where(s=>s.Fleet is {} f&&groups.Contains(f)).Select(s=>s.Id)).ToHashSet();
        var grouped=fleets.Fleets.Where(f=>groups.Contains(f.Id)).SelectMany(f=>f.Members).Concat(armed).ToHashSet();
        bool Ships(IEnumerable<VesselId> ids)=>ids.Any(grouped.Contains);
        bool Officer(OfficerId id)=>fleets.Officers.Any(o=>o.Id==id&&o.Flagship is {} ship&&grouped.Contains(ship));
        bool Draft(AssemblyDraft draft)=>draft.RallyFleet is {} f&&groups.Contains(f)||Ships(draft.AcceptedIdleVessels);
        return command switch
        {
            SetMoveOrderCommand a=>grouped.Contains(a.Vessel),SetFoundingOrderCommand a=>armed.Contains(a.Vessel),
            SetScoutPostureCommand a=>armed.Contains(a.Vessel),SetScoutRetreatOrderCommand a=>armed.Contains(a.Vessel),SetAmebaRiskOrderCommand a=>armed.Contains(a.Vessel),SetAmebaApproachOrderCommand a=>armed.Contains(a.Vessel),
            FleetCommand a=>a.Action switch
            {
                FleetAction.Create b=>Ships(b.Members),FleetAction.Disband b=>groups.Contains(b.Fleet),FleetAction.Merge b=>groups.Contains(b.Target)||b.Sources.Any(groups.Contains),FleetAction.Split b=>groups.Contains(b.Source)||Ships(b.Members),FleetAction.SetFlagship b=>groups.Contains(b.Fleet)||grouped.Contains(b.Flagship),FleetAction.SetOrder b=>b.Fleets.Any(groups.Contains),FleetAction.MoveSelection b=>Ships(b.Vessels)||b.Fleets.Any(groups.Contains),FleetAction.DetachSelection b=>Ships(b.Vessels),_=>false
            },
            CombatActionCommand {Action:CombatAction.SetRepair a}=>Ships(a.Vessels),CombatActionCommand {Action:CombatAction.AttackPocket a}=>groups.Contains(a.Fleet),
            RefitCommand {Action:RefitAction.Start a}=>grouped.Contains(a.Vessel),
            OfficerCommand {Action:OfficerAction.AssignFlagship a}=>groups.Contains(a.Fleet)||Officer(a.Officer),OfficerCommand {Action:OfficerAction.Transfer a}=>grouped.Contains(a.From)||grouped.Contains(a.To)||Officer(a.Officer),OfficerCommand {Action:OfficerAction.Remove a}=>Officer(a.Officer),OfficerCommand {Action:OfficerAction.Post a}=>Officer(a.Officer),OfficerCommand {Action:OfficerAction.Dismiss a}=>Officer(a.Officer),
            AssemblyCommand {Action:AssemblyAction.Accept a}=>Draft(a.Draft),AssemblyCommand {Action:AssemblyAction.Amend a}=>Draft(a.Draft),AssemblyCommand {Action:AssemblyAction.ChangeRally a}=>a.RallyFleet is {} f&&groups.Contains(f),AssemblyCommand {Action:AssemblyAction.Resume a}=>fleets.Requests.Any(r=>r.Id==a.Request&&r.RallyFleet is {} f&&groups.Contains(f)),
            _=>false
        };
    }
}
