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
            BreakdownKinds.Building => content.Buildings.TryGetValue(line.SourceId, out var building) ? building.Name : line.SourceId,
            BreakdownKinds.Population => $"{Count(line, "population unit")} to support",
            BreakdownKinds.Shortage when line.Amount == 0 => "Growth paused by support shortage",
            BreakdownKinds.Shortage => "Support shortage" + rounding,
            BreakdownKinds.GrowthBase => "Base growth",
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
}
