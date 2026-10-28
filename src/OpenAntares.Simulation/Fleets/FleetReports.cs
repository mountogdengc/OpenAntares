using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Fleets;
internal static class FleetReports
{
    internal static void Finalize(ContentSet content,GameState candidate,FleetResolutionContext context)
    {
        var d=content.FleetCommand!;var resolved=new System.Collections.Generic.List<FleetEvent>();
        foreach(var empire in candidate.Empires.OrderBy(e=>e.Id))
        {
            var f=empire.Fleets!;
            var completed=f.PendingEvents.Concat(context.Events.Where(e=>e.Owner==empire.Id)).OrderBy(e=>e.Id).ToArray();
            resolved.AddRange(completed);
            foreach(var officer in f.Officers)
            {
                var entries=completed.Where(e=>e.Officer==officer.Id).Select(e=>new OfficerHighlight(e.Id,e.Turn,e.Kind,e.Reason)).ToImmutableArray();
                officer.Highlights=officer.Highlights.Concat(entries).DistinctBy(e=>e.Event).OrderBy(e=>e.Event).TakeLast(d.HighlightLimit).ToList();
                if(!entries.IsEmpty)officer.Details.Add(new(candidate.Turn,entries));
                officer.Details=officer.Details.Where(t=>t.Turn>candidate.Turn-d.DetailTurnLimit).GroupBy(t=>t.Turn).Select(g=>new OfficerTurnDetail(g.Key,g.SelectMany(t=>t.Entries).DistinctBy(e=>e.Event).OrderBy(e=>e.Event).ToImmutableArray())).OrderBy(t=>t.Turn).TakeLast(d.DetailTurnLimit).ToList();
            }
            // Ten turns of report detail; current attention also remains in live request/officer state.
            f.RecentEvents=f.RecentEvents.Concat(completed).Where(e=>e.Turn>=Math.Max(0,candidate.Turn-d.DetailTurnLimit)).DistinctBy(e=>e.Id).OrderBy(e=>e.Id).ToList();
            f.PendingEvents.Clear();
        }
        var ordered=resolved.OrderBy(e=>e.Owner).ThenBy(e=>e.Id).ToArray();
        context.Events.Clear();context.Events.AddRange(ordered);
    }
}
