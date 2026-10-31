using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
internal static class DiplomacyProductionForecasts
{
    internal static EmpireView Apply(ContentSet content,EmpireView view)
    {
        return view with{Colonies=view.Colonies.Select(colony=>
        {
            if(colony.ReservedProduction==0)return colony;
            // This is an alternative, not a prediction of a partner's private answer.
            var released=view with{Colonies=view.Colonies.Select(c=>c.Id==colony.Id?c with{ReservedProduction=0}:c).ToImmutableArray()};
            var target=colony.ProjectId is {} id?new ProductionTarget(colony.ProjectKind,id):null;
            var forecast=GovernorPlanningQueries.Create(content,released).Forecast(colony.Id,colony.Workforce,target);
            return colony with{ProductionIfSetupReleased=forecast?.Production};
        }).ToImmutableArray()};
    }
}
