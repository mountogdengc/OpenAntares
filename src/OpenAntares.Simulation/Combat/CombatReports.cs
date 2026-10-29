using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public static class CombatReports
{
    public static void Finalize(ContentSet content,GameState snapshot,GameState candidate,CombatResolutionContext context)
    {
        if(context.ReportsFinalized||context.FromTurn!=snapshot.Turn)throw new InvalidOperationException("Combat reports already finalized or snapshot changed.");context.ReportsFinalized=true;
        var events=ImmutableArray.CreateBuilder<OwnedCombatEvent>();
        foreach(var owner in candidate.Empires.OrderBy(e=>e.Id))
        {
            var c=owner.Combat!;events.AddRange(c.PendingEvents.OrderBy(e=>e.Id).Select(e=>new OwnedCombatEvent(owner.Id,e)));
            c.RecentEvents=c.RecentEvents.Concat(c.PendingEvents).DistinctBy(e=>e.Id).OrderBy(e=>e.Id).TakeLast(content.Combat!.HistoryLimit).ToList();c.PendingEvents.Clear();
        }
        context.ReportEvents=events.ToImmutable();
        context.ReportDepartures=context.Departures.Values.Distinct().OrderBy(g=>g.Members[0]).Select(g=>new OwnedCombatDeparture(candidate.FindVessel(g.Members[0])!.EmpireId,g)).ToImmutableArray();
    }
}
