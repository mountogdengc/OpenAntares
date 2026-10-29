using System;
using System.Linq;
using System.Collections.Immutable;
using Godot;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class CombatReportPanel:VBoxContainer
{
    public bool ShowRoutine {get;set;}=true;
    private CombatEmpireView? _view;private EmpireTurnReportView? _report;private bool _expanded;
    public void SetView(CombatEmpireView? view){_view=view;Render();}
    public void SetReport(EmpireTurnReportView report){_report=report;_view=report.Combat;Render();}
    private void Render()
    {
        DevelopmentUi.Clear(this);Visible=_view is not null;if(_view is not {} view)return;
        AddChild(DevelopmentUi.Text("Combat and finite returns","HeaderLabel"));var totals=view.Totals;
        AddChild(DevelopmentUi.Text($"Lifetime: {totals.Encounters} encounters; {totals.ShipsLost} ships, {totals.HullLost} hull and {totals.CargoLost} embarked population lost; {totals.HullRepaired} hull repaired."));
        foreach(var notice in view.Notices)AddChild(DevelopmentUi.Text(notice.Level+": "+notice.Reason,"WarningLabel"));
        var events=_report?.CombatEvents??view.Events;
        foreach(var e in events.Where(e=>ShowRoutine||e.Kind is "emergency" or "withdrawal_queued" or "repair_blocked" or "repair_unavailable"))AddChild(DevelopmentUi.Text($"Turn {e.Turn}: {e.Kind.Replace('_',' ')}. {e.Reason}",e.Kind is "emergency" or "repair_blocked"?"WarningLabel":"SubtleLabel"));
        if(_report is {} report)foreach(var g in report.CombatDepartures)AddChild(DevelopmentUi.Text($"Returning cohort: ships {string.Join(", ",g.Members.Select(id=>id.Value))}, speed {g.Speed}, committed {g.TotalTurns} turn(s), On arrival: {g.OnArrival}. Detached ships remain owned; no automatic resend."));
        foreach(var ship in view.Ships.Where(s=>s.ReturnColony is not null||s.Repairing))AddChild(DevelopmentUi.Text($"Ship {ship.Id.Value}: {Status(ship)}, hull {ship.Hull}/{ship.MaxHull}, cohesion {ship.Cohesion}; {ship.RepairIntervals} full eligible repair interval(s), after arrival. No arrival healing."));
        DevelopmentUi.Button(this,_expanded?"Hide combat calculations":"Expand combat calculations",()=>{_expanded=!_expanded;Callable.From(Render).CallDeferred();},"CombatExpandDetails");
        if(!_expanded)return;
        foreach(var history in view.History.Reverse())AddChild(DevelopmentUi.Text($"Encounter {history.Encounter.Value}: {history.Outcome}; {history.Rounds} rounds, actual hull lost {history.HullLost}, cargo lost {history.CargoLost}. Access to a cleared system does not grant ownership."));
        var rounds=_report?.CombatRounds??view.History.SelectMany(h=>h.Details).Concat(view.Encounters.SelectMany(e=>e.Details)).ToImmutableArray();
        foreach(var round in rounds.TakeLast(20))
        {
            AddChild(DevelopmentUi.Text($"Encounter {round.Encounter.Value}, round {round.Round}, {(round.Interception?"enemy-only interception":"simultaneous exchange")}; {round.Before.Orders.Doctrine}. Frozen targets; no spill or combat XP."));
            foreach(var shot in round.Result.Shots)AddChild(DevelopmentUi.Text($"{(shot.Source.Sentry?"Sentry":"Ship")} {shot.Source.Id} → {(shot.Target.Sentry?"sentry":"ship")} {shot.Target.Id}: {shot.Damage} damage."));
            foreach(var f in round.Forecasts){var hull=round.Result.Ships.Single(s=>s.Id==f.Id);AddChild(DevelopmentUi.Text($"Ship {f.Id.Value}: hull {hull.Before} → {hull.After}, actual loss {hull.ActualLoss}; band {f.Before} → {f.After?.ToString()??"destroyed"}; cohesion {f.CohesionBefore} → {f.CohesionAfter}."));}
        }
    }
    public static string Status(ShipCombatView ship)=>ship.Status switch{CombatShipStatus.AttackCommitted=>"Attack committed",CombatShipStatus.InCombat=>"In combat",CombatShipStatus.WithdrawalQueued=>"Withdrawing",CombatShipStatus.ResponseRequired=>"Response required",CombatShipStatus.ReturnOrdered=>"Return ordered",CombatShipStatus.RepairBlocked=>"Repair blocked",CombatShipStatus.Hold or CombatShipStatus.Damaged=>ship.DamageBand==CombatDamageBand.Crippled?"Crippled":ship.Hull<ship.MaxHull?"Damaged":"Hold",_=>ship.Status.ToString()};
}
