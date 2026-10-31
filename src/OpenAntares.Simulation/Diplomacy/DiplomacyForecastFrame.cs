using System.Linq;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
/// <summary>Calculator-only own trade facts. Never save or resolve this unpublished frame.</summary>
internal static class DiplomacyForecastFrame
{
    internal static void Apply(GameState frame,EmpireView view)
    {
        if(!Rules.HasDiplomacy(view.RulesVersion)||view.Diplomacy is not {} diplomacy)return;
        frame.Diplomacy=new();
        foreach(var contact in diplomacy.Contacts)
        {
            var pair=DiplomacyPair.For(view.Viewer,contact.Identity.OtherEmpireId);var projected=new DiplomacyPairState{Pair=pair};
            foreach(var a in contact.Agreements.Where(a=>a.Kind==AgreementKind.Trade&&a.OwnHub is {} hub&&frame.Colonies.Any(c=>c.Id==hub&&c.EmpireId==view.Viewer)))
            {
                var origin=new HubOrigin(a.OwnHub!.Value,view.Viewer,a.ActivatedTurn);projected.Agreements.Add(new(){Kind=AgreementKind.Trade,ActivatedTurn=a.ActivatedTurn,EndTurn=a.EndTurn,LowHub=pair.Low==view.Viewer?origin:null,HighHub=pair.High==view.Viewer?origin:null});
            }
            if(projected.Agreements.Count>0)frame.Diplomacy.Pairs.Add(projected);
        }
    }
}
