using System.Linq;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Crises;

/// <summary>Economic/port inputs only, reconstructed from immutable own facts. Never save or resolve this frame.</summary>
internal static class CrisisForecastFrame
{
    internal static void Apply(GameState frame,EmpireView view)
    {
        if(!Rules.HasCrises(view.RulesVersion)||view.Crisis is not {} crisis)return;
        frame.Crisis=new(){Enabled=crisis.Enabled};frame.FindEmpire(view.Viewer)!.Crisis=new(){CivicLevel=crisis.CivicLevel,CivicExpiresAt=crisis.CivicExpiresAt};
        foreach(var colony in frame.Colonies)
        {
            var condition=crisis.ColonyConditions.FirstOrDefault(c=>c.Location.Colony==colony.Id);
            colony.Crisis=new(){Quarantined=condition?.Quarantined==true};
            if(condition?.Stage is {} stage)colony.Crisis.Case=new(){Stage=stage};
        }
        // A one-colony governor preview must still see another own world's system-wide port closure.
        foreach(var condition in crisis.ColonyConditions.Where(c=>c.Quarantined&&!frame.Colonies.Any(x=>x.Id==c.Location.Colony)))
        {
            if(frame.FindPlanet(condition.Location.Planet) is null)frame.Planets.Add(new(){Id=condition.Location.Planet,StarId=condition.Location.Star,PlanetTypeId=condition.Economy.PlanetTypeId});
            frame.Colonies.Add(new(){Id=condition.Location.Colony,PlanetId=condition.Location.Planet,EmpireId=view.Viewer,Crisis=new(){Quarantined=true}});
        }
    }
}
