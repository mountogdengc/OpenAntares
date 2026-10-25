using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Crises;

internal static class CrisisAudit
{
    // Coalesce a channel's latest planning action; repeated settings cannot grow an unbounded log.
    internal static void Add(GameState candidate,EmpireId owner,CrisisDigestEntry entry)
    {
        var e=candidate.FindEmpire(owner)!.Crisis!;var current=e.PendingDigests.SingleOrDefault();
        var entries=(current?.Entries??ImmutableArray<CrisisDigestEntry>.Empty).Where(x=>x.Key!=entry.Key||x.Kind!=entry.Kind).Append(entry).OrderBy(x=>x.Key.Episode).ThenBy(x=>x.Key.Colony).ThenBy(x=>x.Kind,System.StringComparer.Ordinal).ThenBy(x=>x.Revision).ToImmutableArray();
        e.PendingDigests.Clear();e.PendingDigests.Add(new(candidate.Turn,entries));
    }
}
