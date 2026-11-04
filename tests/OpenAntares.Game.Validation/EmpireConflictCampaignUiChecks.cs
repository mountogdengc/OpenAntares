using System;
using System.Linq;
using OpenAntares.Game.Components;
using OpenAntares.Simulation.Conflict;
namespace OpenAntares.Game.Validation;
internal static class EmpireConflictCampaignUiChecks
{
    // Presentation supplement, not an earned campaign or physical-input check.
    internal static void MixedDefenseAndLocalFallbackAreExplained(Action<bool,string> check)
    {
        check(ConflictOrdersDialog.DefenseExplanation.Contains("protected"),"Mixed defense explains protected civilians without an opt-in");
        check(ConflictOrdersDialog.LocalFallbackExplanation.Contains("same system"),"Local fallback explains remaining in the same system");
        var observation=new MilitaryObservation(new(42),new(2),"armed",new(1),3,true,null,80,100,12,3,5);
        var text=ConflictOrdersDialog.ForeignDetails(observation);
        check(!text.Contains("cohesion",StringComparison.OrdinalIgnoreCase),"Enemy details omit private cohesion");
        check(text.Contains("80/100")&&text.Contains("unknown"),"Enemy details retain observed hull and unknown intent caveat");
    }
}
