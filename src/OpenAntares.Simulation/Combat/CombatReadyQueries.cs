using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public static class CombatReadyQueries
{
    public static ImmutableArray<string> ValidateResponse(ContentSet content,GameState state,EmpireId owner)
    {
        if(!Rules.HasCombat(state.RulesVersion))return ImmutableArray<string>.Empty;
        var result=ImmutableArray.CreateBuilder<string>();var c=state.FindEmpire(owner)?.Combat;
        if(c is null){result.Add(CombatRejectionCodes.ResponseRequired);return result.ToImmutable();}
        if(c.Responses.Count!=0)result.Add(CombatRejectionCodes.ResponseRequired);
        var encounters=state.Combat!.Encounters.Where(e=>e.Owner==owner).ToArray();
        var forecast=encounters.Any(e=>e.ContinueRiskTurn!=state.Turn)?Crises.CrisisTurnCoordinator.PreviewForFleetConsent(content,state,owner):new Crises.CrisisPreparationResult(state,true);
        if(!forecast.Succeeded){if(!result.Contains(CombatRejectionCodes.ResponseRequired))result.Add(CombatRejectionCodes.ResponseRequired);return result.ToImmutable();}
        foreach(var encounter in encounters)
        {
            if(encounter.ConsentRevision!=encounter.Revision&&!result.Contains(CombatRejectionCodes.ResponseRequired))result.Add(CombatRejectionCodes.ResponseRequired);
            if(encounter.ContinueRiskTurn!=state.Turn&&CombatWithdrawal.ReturnBlocked(forecast.State,encounter)&&!result.Contains(CombatRejectionCodes.PortClosed))result.Add(CombatRejectionCodes.PortClosed);
        }
        return result.ToImmutable();
    }
}
