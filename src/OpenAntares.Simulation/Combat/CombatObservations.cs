using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public static class CombatObservations
{
    public static void Refresh(ContentSet content,GameState candidate,int observedTurn)
    {
        if(!Rules.HasCombat(candidate.RulesVersion))return;
        foreach(var owner in candidate.Empires)
        foreach(var pocket in candidate.Combat!.Pockets.Where(p=>ObservationUpdater.IsCurrent(candidate,owner.Id,p.Star)))
            Observe(content,owner,pocket,observedTurn);
    }
    internal static void Observe(ContentSet content,EmpireState owner,PocketState pocket,int turn)
    {
        var known=owner.Combat!.Observations;
        known.RemoveAll(o=>o.Pocket==pocket.Id);
        known.Add(new(pocket.Id,pocket.Star,turn,pocket.Cleared,pocket.Sentries.Select(s=>new ObservedSentry(s.Id,s.Profile,s.Hull,content.Combat!.SentryPursuitSpeed)).ToImmutableArray()));
        known.Sort((a,b)=>a.Pocket.CompareTo(b.Pocket));
    }
}
