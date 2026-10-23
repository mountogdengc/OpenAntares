namespace OpenAntares.Simulation.Turns;

public sealed record ExpansionEvent(string Kind, EmpireId Owner, VesselId? Vessel = null, StarId? Star = null, PlanetId? Planet = null, ColonyId? Colony = null, string? Reason = null);
