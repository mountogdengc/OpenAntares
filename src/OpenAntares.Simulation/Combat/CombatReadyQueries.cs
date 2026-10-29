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
        foreach(var encounter in state.Combat!.Encounters.Where(e=>e.Owner==owner))
        {
            if(encounter.ConsentRevision!=encounter.Revision&&!result.Contains(CombatRejectionCodes.ResponseRequired))result.Add(CombatRejectionCodes.ResponseRequired);
            var port=state.FindColony(encounter.Orders.ReturnColony);
            bool cargo=encounter.Members.Any(id=>state.FindVessel(id)?.CarriedPopulation>0);
            if(encounter.ContinueRiskTurn!=state.Turn&&(port?.EmpireId!=owner||cargo&&(Crises.CrisisQueries.PortClosed(state,owner,state.FindPlanet(port.PlanetId)!.StarId)||encounter.Members.Any(id=>Crises.CrisisQueries.IsHeld(state,id)))))
                if(!result.Contains(CombatRejectionCodes.PortClosed))result.Add(CombatRejectionCodes.PortClosed);
        }
        return result.ToImmutable();
    }
}
