using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Presentation;

/// <summary>Ephemeral semantic navigation only; never serialized or used as command authority.</summary>
public sealed class PlayabilityContext
{
    public string Workspace { get; set; } = "Galaxy";
    public StarId? Star { get; set; }
    public PlanetId? Planet { get; set; }
    public ColonyId? Colony { get; set; }
    public VesselId? Vessel { get; set; }
    public Stack<PlayabilityReturn> Returns { get; } = new();
    public Stack<PlayabilityOverviewReturn> OverviewReturns { get; } = new();
    public bool LostSelection { get; private set; }

    public void ResetForCampaign()
    {
        Workspace = "Galaxy";
        Star = null; Planet = null; Colony = null; Vessel = null;
        Returns.Clear(); OverviewReturns.Clear(); LostSelection = false;
    }

    public void Reconcile(EmpireView view)
    {
        LostSelection = false;
        if (Star is {} star && !view.Stars.Any(s=>s.Chart.Id==star)) { Star=null; Planet=null; LostSelection=true; }
        if (Planet is {} planet && !view.Stars.Any(s=>s.Planets?.Any(p=>p.Id==planet)==true)) { Planet=null; LostSelection=true; }
        if (Colony is {} colony && !view.Colonies.Any(c=>c.Id==colony)) { Colony=null; LostSelection=true; }
        if (Vessel is {} vessel && !view.Vessels.Any(v=>v.Id==vessel)) { Vessel=null; LostSelection=true; }
        if (LostSelection) { Returns.Clear(); OverviewReturns.Clear(); }
    }
}

public sealed record PlayabilityReturn(string Workspace, string FocusKey, int ScrollVertical)
{
    public StarId? Star { get; init; }
    public PlanetId? Planet { get; init; }
    public ColonyId? Colony { get; init; }
    public VesselId? Vessel { get; init; }
    public StarId? Destination { get; init; }
    public bool RequireColonyChoice { get; init; }
}

public sealed record PlayabilityOverviewReturn(string Parent, string Child, string FocusKey, int ScrollVertical);
