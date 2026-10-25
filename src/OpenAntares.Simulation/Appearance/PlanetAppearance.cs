using System.Globalization;
using System.Text;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Appearance;
/// <summary>Versioned cosmetic identity; never consumes authoritative randomness or IDs.</summary>
public static class PlanetAppearance
{
    public static PlanetAppearanceDescriptor Create(ulong galaxySeed,PlanetId planet)
    {
        string key="OpenAntares/planet/v1|"+galaxySeed.ToString(CultureInfo.InvariantCulture)+"|"+planet.Value.ToString(CultureInfo.InvariantCulture);
        ulong hash=14695981039346656037UL;
        foreach(byte value in Encoding.UTF8.GetBytes(key))hash=unchecked((hash^value)*1099511628211UL);
        return new(1,(int)(hash&7),hash>>3);
    }
}
