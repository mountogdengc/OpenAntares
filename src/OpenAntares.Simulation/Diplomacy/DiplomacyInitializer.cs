using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static class DiplomacyInitializer
{
    public static void Initialize(ContentSet content,GameState state){state.Diplomacy=new();}
}
