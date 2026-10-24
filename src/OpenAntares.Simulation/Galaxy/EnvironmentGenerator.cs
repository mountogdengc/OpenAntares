using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Galaxy;

/// <summary>Assigns surveyed physical habitats after every original galaxy draw, including home selection.</summary>
internal static class EnvironmentGenerator
{
    internal static void Apply(ContentSet content, GameState state, PlanetId home)
    {
        foreach(var planet in state.Planets.OrderBy(p => p.Id))
        {
            var weights=content.PlanetTypes[planet.PlanetTypeId].EnvironmentGenerationWeights;
            int draw=state.Random.NextInt(checked((int)weights.Values.Sum(w => (long)w)));
            foreach(var (id,weight) in weights)
            {
                if(draw < weight) {planet.EnvironmentId=id;break;}
                draw-=weight;
            }
        }
        // Consume the home draw too: all species receive identical galaxy facts and final RNG.
        state.FindPlanet(home)!.EnvironmentId="shelf_sea";
    }
}
