using Godot;
using OpenAntares.Simulation.Diplomacy;
namespace OpenAntares.Game.Components;
public partial class DiplomacyContactPanel:VBoxContainer
{
    public void ShowContact(ContactDiplomacyView contact)
    {
        DevelopmentUi.Clear(this);
        AddChild(DevelopmentUi.Text(contact.Identity.CultureName+" — "+contact.Identity.SpeciesName,"HeaderLabel"));
        AddChild(DevelopmentUi.Text("A diplomatic channel is open. Identity is shared; remote locations and private resources remain undisclosed."));
        AddChild(DevelopmentUi.Text(Reason("Your attitude",contact.OwnAttitude)));
        AddChild(DevelopmentUi.Text(Reason("Their attitude",contact.OtherAttitude)));
    }
    private static string Reason(string name,AttitudeView a)=>$"{name}: {a.Label} ({a.Score}). Cooperation +{a.Cooperation} from {a.Streak} completed turns; cancellation memory −{a.Penalty}, {a.MemoryRemaining} turns remaining.";
}
