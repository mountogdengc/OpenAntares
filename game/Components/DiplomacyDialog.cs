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
    private readonly ConflictDeclarationDialog _declaration=new();
    public event Action<OpenAntares.Simulation.Commands.Command>? CommandIssued;
    public void ShowError(string error,bool accepted){if(accepted)_offer.Hide();else _offer.ShowError(error);_declaration.ShowError(error,accepted);}
    public override void _Ready(){Name="DiplomacyDialog";Title="Diplomacy";OkButtonText="Close";_body=DevelopmentUi.DialogBody(this);AddChild(_offer);_offer.CommandIssued+=c=>CommandIssued?.Invoke(c);AddChild(_declaration);_declaration.CommandIssued+=c=>CommandIssued?.Invoke(c);VisibilityChanged+=()=>{if(!Visible){_offer.Hide();_declaration.Hide();}};}
    public void ShowEmpire(ContentSet content,EmpireView view,EmpireId? selected=null)
    {
        if(!Visible)ForceNative=false;
        _selected=selected??_selected;DevelopmentUi.Clear(_body);
        if(view.Diplomacy is not {} diplomacy){_body.AddChild(DevelopmentUi.Text("Diplomacy is unavailable in this historical ruleset."));}
        else if(diplomacy.Contacts.IsEmpty){_body.AddChild(DevelopmentUi.Text("No other civilization has been met. Explore to establish contact. Solo games have no diplomatic partners."));}
        else
        {
            foreach(var c in diplomacy.Contacts){var id=c.Identity.OtherEmpireId;DevelopmentUi.Button(_body,c.Identity.CultureName+" — "+c.Identity.SpeciesName,()=>ShowEmpire(content,view,id),"DiplomacyContact_"+id.Value);
                string pendingSummary=c.Pending is {} pendingOffer?(pendingOffer.Recipient==view.Viewer?"Incoming ":"Outgoing ")+pendingOffer.Kind+FormattableString.Invariant($"; {Math.Max(0L,(long)pendingOffer.ExpiryTurn-view.Turn)} answer boundaries remain."):"No pending offer.";
                _body.AddChild(DevelopmentUi.Text($"You: {c.OwnAttitude.Label} ({c.OwnAttitude.Score}); them: {c.OtherAttitude.Label} ({c.OtherAttitude.Score}). "+pendingSummary+" Agreements: "+(c.Agreements.IsEmpty?"none":string.Join(", ",c.Agreements.Select(a=>a.Kind)))));
            }
            var contact=diplomacy.Contacts.FirstOrDefault(c=>c.Identity.OtherEmpireId==_selected)??diplomacy.Contacts[0];_selected=contact.Identity.OtherEmpireId;
            var panel=new DiplomacyContactPanel();_body.AddChild(panel);panel.ShowContact(content,contact,view.Conflict?.Relations.FirstOrDefault(r=>r.Other==contact.Identity.OtherEmpireId));
            if(view.Conflict?.Relations.FirstOrDefault(r=>r.Other==contact.Identity.OtherEmpireId) is {} relation)
            {
                var peacePanel=new ConflictPeacePanel();_body.AddChild(peacePanel);peacePanel.CommandIssued+=c=>CommandIssued?.Invoke(c);peacePanel.ShowPeace(content,view,relation);
                if(relation.OwnDeclarationQueued)
                {
                    _body.AddChild(DevelopmentUi.Text($"Your declaration is queued for publication at turn {(long)view.Turn+1}."));
                    var action=new OpenAntares.Simulation.Conflict.ConflictAction.RevokeDeclaration(relation.Other);var preview=OpenAntares.Simulation.Conflict.ConflictQueries.Preview(content,view,action);var revoke=DevelopmentUi.Button(_body,"Revoke pending declaration",()=>CommandIssued?.Invoke(new OpenAntares.Simulation.Conflict.ConflictCommand(view.Viewer,view.Turn,action,preview.Token)),"ConflictRevokeDeclaration");revoke.Disabled=!preview.Allowed;
                }
                else if(relation.Relation==OpenAntares.Simulation.Conflict.RelationKind.Peace)
                {var preview=OpenAntares.Simulation.Conflict.ConflictQueries.Preview(content,view,new OpenAntares.Simulation.Conflict.ConflictAction.DeclareWar(relation.Other));var declare=DevelopmentUi.Button(_body,"Review declaration of war",()=>_declaration.Open(content,view,relation.Other),"ConflictDeclareWar");declare.Disabled=!preview.Allowed;}
            }
            if(contact.Pending is {} offer)_body.AddChild(DevelopmentUi.Text($"{offer.Kind} offer"+(offer.Star is {} offered?" in "+view.Stars.Single(s=>s.Chart.Id==offered).Chart.Name:"")+ $": created turn {offer.CreatedTurn}, expires at resolution to turn {offer.ExpiryTurn}. "+(offer.OwnAnswered?(offer.OwnAccept==true?"Your Accept is queued.":"Your Decline is queued."):"No answer is required to Ready.")));
            string HubName(ColonyId id)=>view.Research.ColonyNames.TryGetValue(id.ToString(),out var name)?name:"recorded former hub";
            if(contact.Pending?.OwnHub is {} promised)_body.AddChild(DevelopmentUi.Text("Your selected hub: "+HubName(promised)+". Setup remains reserved until activation, withdrawal, answer revocation or resolution."));
            foreach(var a in contact.Agreements){string system=a.Star is {} star?" in "+view.Stars.Single(s=>s.Chart.Id==star).Chart.Name:"";_body.AddChild(DevelopmentUi.Text($"{a.Kind}{system}: activated {a.ActivatedTurn}; earliest notice {a.FirstTerminationTurn}; "+(a.EndTurn is {} end?$"ends {end}.":"no termination notice.")+ $" Warmup remaining: {a.WarmupRemaining}."));
                if(a.Kind==AgreementKind.Trade&&a.OwnHub is {} hub)
                {
                    var colony=view.Colonies.FirstOrDefault(c=>c.Id==hub);string location=colony is null?"recorded location":view.Stars.Single(s=>s.Chart.Id==colony.StarId).Chart.Name;
                    _body.AddChild(DevelopmentUi.Text("Your trade hub: "+HubName(hub)+" at "+location+". Setup paid: "+Format.Points(content.Diplomacy!.SetupCost)+" production. Income: +"+Format.Points(content.Diplomacy.TradeOutput)+" production per full eligible turn. "+FormattableString.Invariant($"First payout resolves at turn {(long)a.ActivatedTurn+content.Diplomacy.Warmup+1}. No refund or hub switching.")));
                }
            }
            void Order(DiplomacyAction action)=>_offer.Open(content,view,contact.Identity.OtherEmpireId,action);
            if(contact.Pending is {} pending)
            {
                if(pending.Proposer==view.Viewer)DevelopmentUi.Button(_body,"Withdraw offer",()=>Order(new DiplomacyAction.Withdraw(pending.Id)),"DiplomacyWithdraw");
                else if(pending.OwnAnswered)DevelopmentUi.Button(_body,"Revoke queued answer",()=>Order(new DiplomacyAction.RevokeAnswer(pending.Id)),"DiplomacyRevoke");
                else{DevelopmentUi.Button(_body,"Accept offer",()=>Order(new DiplomacyAction.Answer(pending.Id,true,null)),"DiplomacyAccept");DevelopmentUi.Button(_body,"Decline offer",()=>Order(new DiplomacyAction.Answer(pending.Id,false,null)),"DiplomacyDecline");}
            }
            else if(view.Conflict?.Relations.Any(r=>r.Other==contact.Identity.OtherEmpireId&&r.Relation==OpenAntares.Simulation.Conflict.RelationKind.War)!=true)
            {
                if(!contact.Agreements.Any(a=>a.Kind==AgreementKind.Trade))DevelopmentUi.Button(_body,"Offer production trade",()=>Order(new DiplomacyAction.OfferTrade(default)),"DiplomacyOfferTrade");
                if(!contact.Agreements.Any(a=>a.Kind==AgreementKind.Settlement))DevelopmentUi.Button(_body,"Offer settlement accord",()=>Order(new DiplomacyAction.OfferSettlement(default)),"DiplomacyOfferSettlement");
            }
            foreach(var a in contact.Agreements.Where(a=>!a.OwnNotice)){var kind=a.Kind;var button=DevelopmentUi.Button(_body,"Give termination notice: "+kind,()=>Order(new DiplomacyAction.Terminate(kind)),"DiplomacyTerminate_"+kind);button.Disabled=view.Turn<a.FirstTerminationTurn;}
            foreach(var result in contact.Results)_body.AddChild(DevelopmentUi.Text(result.Kind.Replace('_',' ')+": "+result.Reason));
        }
        if(_offer.Visible)_offer.UpdateView(view);
        if(_declaration.Visible)_declaration.UpdateView(view);
        if(!Visible)PopupCenteredClamped(new(700,540));
    }
}

