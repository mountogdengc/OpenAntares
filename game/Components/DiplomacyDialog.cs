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
    private readonly DiplomacyOfferDialog _offer=new();
    public event Action<OpenAntares.Simulation.Commands.Command>? CommandIssued;
    public void ShowError(string error,bool accepted){if(accepted)_offer.Hide();else _offer.ShowError(error);}
    public override void _Ready(){Name="DiplomacyDialog";Title="Diplomacy";OkButtonText="Close";_body=DevelopmentUi.DialogBody(this);AddChild(_offer);_offer.CommandIssued+=c=>CommandIssued?.Invoke(c);}
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
            if(contact.Pending is {} offer)_body.AddChild(DevelopmentUi.Text($"{offer.Kind} offer"+(offer.Star is {} offered?" in "+view.Stars.Single(s=>s.Chart.Id==offered).Chart.Name:"")+ $": created turn {offer.CreatedTurn}, expires at resolution to turn {offer.ExpiryTurn}. "+(offer.OwnAnswered?"Your answer is queued.":"No answer is required to Ready.")));
            foreach(var a in contact.Agreements){string system=a.Star is {} star?" in "+view.Stars.Single(s=>s.Chart.Id==star).Chart.Name:"";_body.AddChild(DevelopmentUi.Text($"{a.Kind}{system}: activated {a.ActivatedTurn}; earliest notice {a.FirstTerminationTurn}; "+(a.EndTurn is {} end?$"ends {end}.":"no termination notice.")+ $" Warmup remaining: {a.WarmupRemaining}."));}
            void Order(DiplomacyAction action)=>_offer.Open(content,view,contact.Identity.OtherEmpireId,action);
            if(contact.Pending is {} pending)
            {
                if(pending.Proposer==view.Viewer)DevelopmentUi.Button(_body,"Withdraw offer",()=>Order(new DiplomacyAction.Withdraw(pending.Id)),"DiplomacyWithdraw");
                else if(pending.OwnAnswered)DevelopmentUi.Button(_body,"Revoke queued answer",()=>Order(new DiplomacyAction.RevokeAnswer(pending.Id)),"DiplomacyRevoke");
                else{DevelopmentUi.Button(_body,"Accept offer",()=>Order(new DiplomacyAction.Answer(pending.Id,true,null)),"DiplomacyAccept");DevelopmentUi.Button(_body,"Decline offer",()=>Order(new DiplomacyAction.Answer(pending.Id,false,null)),"DiplomacyDecline");}
            }
            else
            {
                if(!contact.Agreements.Any(a=>a.Kind==AgreementKind.Trade))DevelopmentUi.Button(_body,"Offer production trade",()=>Order(new DiplomacyAction.OfferTrade(default)),"DiplomacyOfferTrade");
                if(!contact.Agreements.Any(a=>a.Kind==AgreementKind.Settlement))DevelopmentUi.Button(_body,"Offer settlement accord",()=>Order(new DiplomacyAction.OfferSettlement(default)),"DiplomacyOfferSettlement");
            }
            foreach(var a in contact.Agreements.Where(a=>!a.OwnNotice)){var kind=a.Kind;var button=DevelopmentUi.Button(_body,"Give termination notice: "+kind,()=>Order(new DiplomacyAction.Terminate(kind)),"DiplomacyTerminate_"+kind);button.Disabled=view.Turn<a.FirstTerminationTurn;}
            foreach(var result in contact.Results)_body.AddChild(DevelopmentUi.Text(result.Kind.Replace('_',' ')+": "+result.Reason));
        }
        if(_offer.Visible)_offer.UpdateView(view);
        PopupCenteredClamped(new(700,540));
    }
}
