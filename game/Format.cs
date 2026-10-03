using System.Globalization;

namespace OpenAntares.Game;

/// <summary>Display formatting for simulation values. Formatting only; it never recalculates.</summary>
public static class Format
{
    /// <summary>Formats stored hundredths as points: 500 → "5", 514 → "5.14".</summary>
    public static string Points(long hundredths) =>
        (hundredths / 100m).ToString("0.##", CultureInfo.InvariantCulture);
}
