using System.Linq;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static class DiplomacyRules
{
    public static bool BlocksFounding(DiplomacyFrame frame,EmpireId owner,StarId star)=>frame.Agreements.Any(a=>a.Kind==AgreementKind.Settlement&&a.Pair.Contains(owner)&&a.Star==star&&a.ActivatedTurn<=frame.Turn&&(a.EndTurn is null||a.EndTurn>frame.Turn));
}
