using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;

namespace OpenAntares.Game;

/// <summary>
/// Turns a simulation <see cref="Quantity"/> into readable breakdown text. It labels and formats the
/// lines the simulation supplies; it never recomputes amounts.
/// </summary>
public static class BreakdownText
{
    /// <param name="colonyName">Names a colony from its ID, for empire totals made of colony lines.</param>
    public static string Describe(Quantity quantity, ContentSet content, string totalLabel = "Total", System.Func<string, string>? colonyName = null)
    {
        var lines = new List<string>();
        foreach (BreakdownLine line in quantity.Lines)
        {
            lines.Add($"{Label(line, content, colonyName)}: {Signed(line.Amount)}");
        }

        lines.Add($"{totalLabel}: {Format.Points(quantity.Total)}");
        return string.Join("\n", lines);
    }

    public static string Signed(long hundredths) =>
        hundredths < 0 ? "−" + Format.Points(-hundredths) : "+" + Format.Points(hundredths);

    private static string Label(BreakdownLine line, ContentSet content, System.Func<string, string>? colonyName)
    {
        string rounding = line.Division is { Remainder: > 0 } ? " (rounded down)" : string.Empty;
        return line.Kind switch
        {
            BreakdownKinds.Workers => $"{Count(line, "worker")} on {PlanetTypeName(line.SourceId, content)}",
            BreakdownKinds.Species => $"{SpeciesName(line.SourceId, content)} worker modifier" + rounding,
            BreakdownKinds.SpeciesWorkers => $"{Count(line, "worker")} after {SpeciesName(line.SourceId, content)} multiplier" + rounding,
            BreakdownKinds.Building => content.Buildings.TryGetValue(line.SourceId, out var building) ? building.Name : line.SourceId,
            BreakdownKinds.Population => content.Species.TryGetValue(line.SourceId, out var species) ? $"Support for {line.Count} {species.PopulationUnitLabel} ({species.Name})" : $"{Count(line, "population unit")} to support",
            BreakdownKinds.Shortage when line.Amount == 0 => "Growth paused by support shortage",
            BreakdownKinds.Shortage => "Support shortage" + rounding,
            BreakdownKinds.GrowthBase => line.SourceId.Length > 0 ? $"{SpeciesName(line.SourceId, content)} base growth" : "Base growth",
            BreakdownKinds.GrowthSurplus => $"Spare support {Format.Points((long)line.Division!.Numerator)}" + rounding,
            BreakdownKinds.Cap => "Above the growth bonus limit",
            BreakdownKinds.CapacityLimit => "Colony is at capacity",
            BreakdownKinds.Colony => colonyName?.Invoke(line.SourceId) ?? line.SourceId,
            BreakdownKinds.Earned => "Earned this turn",
            BreakdownKinds.CostPaid => $"Completes {Name(line.SourceId, content)}",
            _ => line.Kind,
        };
    }

    private static string Count(BreakdownLine line, string noun) =>
        line.Count == 1 ? $"1 {noun}" : $"{line.Count} {noun}s";

    private static string PlanetTypeName(string id, ContentSet content) =>
        content.PlanetTypes.TryGetValue(id, out var type) ? type.Name : id;

    private static string Name(string id, ContentSet content) =>
        content.Buildings.TryGetValue(id, out var building) ? building.Name
        : content.Technologies.TryGetValue(id, out var technology) ? technology.Name
        : content.Vessels.TryGetValue(id, out var vessel) ? vessel.Name
        : id;

    /// <summary>Describes a completion estimate in words.</summary>
    public static string Estimate(TurnEstimate? estimate) => estimate switch
    {
        null => string.Empty,
        { Stalled: true } => "stalled: no output",
        { Turns: 1 } => "completes next turn",
        { Turns: var turns } => $"{turns} turns",
    };

    /// <summary>The progress shown against a target: the reserve, capped at the cost.</summary>
    public static string Progress(ReserveOutcome reserve) =>
        reserve.TargetCost is { } cost
            ? $"{Format.Points(System.Math.Min(reserve.Starting, cost))} / {Format.Points(cost)}"
            : $"reserve {Format.Points(reserve.Starting)}";

    public static IEnumerable<string> Names(IEnumerable<string> buildingIds, ContentSet content) =>
        buildingIds.Select(id => Name(id, content));
    private static string SpeciesName(string id, ContentSet content) => content.Species.TryGetValue(id, out var species) ? species.Name : id;
    public static string EmbarkRequirement(EmbarkationRequirement requirement, string unit = "population") => requirement.Code switch
    {
        "minimum_population" => $"Minimum {unit}: {requirement.Actual} / {requirement.Required}.",
        "support_workers" => $"Support workers for cargo: {requirement.Actual} / {requirement.Required}.",
        "remaining_population" => $"{unit} remaining after cargo: {requirement.Actual}; at least {requirement.Required} required.",
        "full_colony" => $"Full colony required: {requirement.Actual} / {requirement.Required} at current capacity.",
        _ => requirement.Code
    };
    public static string FoundingReason(string? code) => code switch
    {
        null => string.Empty,
        "target_not_known" => "Select a surveyed planet.",
        "wrong_system" => "Travel to this system first.",
        "not_colonizable" => "This world cannot support this settlement.",
        "habitat_not_suitable" => "This environment is unsuitable for a native colony.",
        "habitat_mode_not_available" => "This habitat mode is unavailable to your species.",
        "target_occupied" => "This planet is already colonized.",
        "target_reserved" => "Another expedition has reserved this world.",
        "vessel_in_transit" => "Travel is committed until arrival.",
        _ => "This vessel cannot found here."
    };

}
