namespace OpenAntares.Simulation.Turns;

/// <summary>An observed fact for one viewer; never an omniscient foreign activity report.</summary>
public sealed record EncounterEvent(string Kind, EmpireId Viewer, EmpireId OtherEmpireId, StarId StarId, PlanetId? PlanetId, int ObservedTurn);
