using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
namespace OpenAntares.Simulation.Development;
internal static class DevelopmentCompletion
{
    internal static void Apply(ContentSet content,GameState candidate,TurnReport report)
    {
        if(!Rules.HasDevelopment(candidate.RulesVersion))return;
        foreach(var economy in report.Economy.Colonies) {
            if(economy.Production.CompletedId is not {} id)continue;
            var colony=candidate.FindColony(economy.Colony);
            if(Rules.HasCrises(candidate.RulesVersion)&&colony is null)continue;
            var d=colony!.Development!;
            if(economy.Production.TargetKind==ProductionKinds.Building)d.Queue.RemoveAll(q=>q.Target.Kind==ProductionKinds.Building&&q.Target.ContentId==id);
            else if(d.ActiveQueueToken is {} token)d.Queue.RemoveAll(q=>q.Token==token&&q.Target.Kind==economy.Production.TargetKind&&q.Target.ContentId==id);
            d.ActiveQueueToken=null;d.Origin=ProductionOrigin.None;
        }
        foreach(var owner in candidate.Empires.Where(e=>e.Controller==ControllerKind.Human)) {
            var d=owner.Development!;var completed=d.PendingReport??new(report.FromTurn,ImmutableArray<GovernorReportEntry>.Empty,candidate.Colonies.Count(c=>c.EmpireId==owner.Id));
            d.CompletedReports.Add(completed);if(d.CompletedReports.Count>10)d.CompletedReports.RemoveRange(0,d.CompletedReports.Count-10);d.PendingReport=null;
        }
    }
}
