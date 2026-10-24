namespace OpenAntares.Simulation.State;

/// <summary>A retained identity learned through the owning empire's observations.</summary>
public sealed record ContactState(EmpireId OtherEmpireId, string SpeciesId, int FirstObservedTurn);
