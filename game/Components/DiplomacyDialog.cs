using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Diplomacy;
namespace OpenAntares.Game.Components;
public partial class DiplomacyDialog:AcceptDialog
{
    private VBoxContainer _body=null!;private EmpireId? _selected;
    public override void _Ready(){Name="DiplomacyDialog";Title="Diplomacy";OkButtonText="Close";_body=DevelopmentUi.DialogBody(this);}
    public void ShowEmpire(ContentSet content,EmpireView view,EmpireId? selected=null)
    {
        _selected=selected??_selected;DevelopmentUi.Clear(_body);
        if(view.Diplomacy is not {} diplomacy){_body.AddChild(DevelopmentUi.Text("Diplomacy is unavailable in this historical ruleset."));}
        else if(diplomacy.Contacts.IsEmpty){_body.AddChild(DevelopmentUi.Text("No other civilization has been met. Explore to establish contact. Solo games have no diplomatic partners."));}
        else
        {
            foreach(var c in diplomacy.Contacts){var id=c.Identity.OtherEmpireId;DevelopmentUi.Button(_body,c.Identity.CultureName+" — "+c.Identity.SpeciesName,()=>ShowEmpire(content,view,id),"DiplomacyContact_"+id.Value);}
            var contact=diplomacy.Contacts.FirstOrDefault(c=>c.Identity.OtherEmpireId==_selected)??diplomacy.Contacts[0];_selected=contact.Identity.OtherEmpireId;
            var panel=new DiplomacyContactPanel();_body.AddChild(panel);panel.ShowContact(contact);
            if(contact.Pending is {} offer)_body.AddChild(DevelopmentUi.Text($"{offer.Kind} offer: created turn {offer.CreatedTurn}, expires at resolution to turn {offer.ExpiryTurn}. "+(offer.OwnAnswered?"Your answer is queued.":"No answer is required to Ready.")));
            foreach(var a in contact.Agreements){string system=a.Star is {} star?" in "+view.Stars.Single(s=>s.Chart.Id==star).Chart.Name:"";_body.AddChild(DevelopmentUi.Text($"{a.Kind}{system}: activated {a.ActivatedTurn}; earliest notice {a.FirstTerminationTurn}; "+(a.EndTurn is {} end?$"ends {end}.":"no termination notice.")+ $" Warmup remaining: {a.WarmupRemaining}."));}
            foreach(var result in contact.Results)_body.AddChild(DevelopmentUi.Text(result.Kind.Replace('_',' ')+": "+result.Reason));
        }
        PopupCenteredClamped(new(700,540));
    }
}
