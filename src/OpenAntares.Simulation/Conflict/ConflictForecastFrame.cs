using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
/// <summary>Current published pressure on own colonies, including colonies with zero output.</summary>
public sealed record OwnBlockadePressure(StarId Star,EmpireId Other,int RetainedPercent);
/// <summary>Pure own-fact adapter. These incomplete calculator frames must never be published.</summary>
internal static class ConflictForecastFrame
{
    internal static ImmutableArray<OwnBlockadePressure> PublishedForViewer(GameState state,EmpireId viewer)=>state.Conflict!.Owners.Single(o=>o.Owner==viewer).PublishedPressure.ToImmutableArray();
    internal static ImmutableArray<OwnBlockadePressure> ForViewer(ConflictBlockadeFrame frame,EmpireId viewer)
        =>frame.Pressure.Where(p=>p.Victim==viewer).Select(p=>new OwnBlockadePressure(p.Star,p.Blockader,p.RetainedPercent)).ToImmutableArray();
    internal static void Apply(GameState frame,EmpireView view)
    {
        if(!Rules.HasConflict(view.RulesVersion))return;
        Apply(frame,view.Viewer,view.Conflict?.OwnPressure??ImmutableArray<OwnBlockadePressure>.Empty);
    }
    internal static void Apply(GameState frame,EmpireId viewer,ImmutableArray<OwnBlockadePressure> pressure)
    {
        if(frame.Empires.Any(e=>e.Id!=viewer)||frame.Colonies.Any(c=>c.EmpireId!=viewer)||frame.Vessels.Any(v=>v.EmpireId!=viewer))throw new ArgumentException("Conflict calculator requires an own-only frame.");
        if(pressure.Any(p=>p.Other==viewer||p.RetainedPercent is <1 or >100)||pressure.Select(p=>p.Star).Distinct().Count()!=pressure.Length)throw new ArgumentException("Invalid own blockade pressure.");
        frame.Conflict=null;
        frame.ConflictCalculatorFrame=new(frame.Turn,pressure.Where(p=>frame.Colonies.Any(c=>frame.FindPlanet(c.PlanetId)?.StarId==p.Star)).Select(p=>new ConflictBlockadePressure(DiplomacyPair.For(viewer,p.Other),viewer,p.Other,p.Star,p.RetainedPercent)).ToImmutableArray(),ImmutableArray<ConflictBlockadeStatus>.Empty);
    }
}
