using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Scouting;
public static class ScoutReadyQueries
{
    public static bool HasConsent(ContentSet c,GameState s,EmpireId owner)
    {
        if(!Rules.HasScouting(s.RulesVersion))return true;
        var e=s.FindEmpire(owner);if(e?.Scouting is null)return false;if(e.Controller==ControllerKind.Ai)return true;
        var attention=ScoutQueries.Project(c,s,owner).Attention;
        return e.Scouting.Receipts.Select(r=>r.Token).SequenceEqual(attention);
    }
    public static ImmutableArray<string> ValidateResponse(ContentSet c,GameState s,EmpireId owner)
    {
        if(!Rules.HasScouting(s.RulesVersion))return ImmutableArray<string>.Empty;
        var errors=ImmutableArray.CreateBuilder<string>();if(!HasConsent(c,s,owner))errors.Add("ameba_acknowledgement_required");
        var view=EmpireViewProjector.ProjectBase(c,s,owner);var preview=ScoutQueries.PreviewPreparation(c,s,owner);
        foreach(var token in preview.Attention)
        {
            var v=view.Vessels.Single(v=>v.Id==token.Vessel);
            if(token.Action=="response_required"||token.Action=="depart"&&(token.Destination is not {} destination||v.PendingMove!=destination||!ScoutQueries.LegalRoute(c,view,v,destination)))errors.Add("ameba_response_required");
        }
        return errors.Distinct().ToImmutableArray();
    }
}
