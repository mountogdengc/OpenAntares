namespace OpenAntares.Simulation.Turns;

/// <summary>An observed fact for one viewer; never an omniscient foreign activity report.</summary>
public sealed record EncounterEvent(string Kind, EmpireId Viewer, EmpireId OtherEmpireId, StarId StarId, PlanetId? PlanetId, int ObservedTurn)
{
    /// <summary>Captured from the viewer's observation; later surveys cannot rewrite this report.</summary>
    public int? PlanetOrbit { get; init; }
    // Display identity is populated only by the viewer's report projection, from known contacts.
    public string? SpeciesId { get; init; }
    public string? SpeciesName { get; init; }
    public string? CultureName { get; init; }
}
