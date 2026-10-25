using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Scouting;
public static class ScoutPlanningQueries
{
    public static IScoutPlanningContext Create(ContentSet c,GameState s,EmpireId owner)=>new Context(c,EmpireViewProjector.Project(c,s,owner),ScoutQueries.PreviewPreparation(c,s,owner));
    private sealed record Context(ContentSet Content,EmpireView View,ScoutPreparationPreview Preparation):IScoutPlanningContext
    {
        public int? TravelTurns(StarId origin,StarId destination,string roleId)
        {
            var a=View.Galaxy.Stars.FirstOrDefault(s=>s.Id==origin);var b=View.Galaxy.Stars.FirstOrDefault(s=>s.Id==destination);
            if(a is null||b is null||!Content.Vessels.TryGetValue(roleId,out var role))return null;
            try{return TravelCalculator.Duration(a.X,a.Y,b.X,b.Y,role.Speed);}catch(Exception ex)when(ex is OverflowException or ArgumentOutOfRangeException){return null;}
        }
    }
}
