using System;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class ConflictPeacePanel:VBoxContainer
{
    public event Action<Command>? CommandIssued;
    public void ShowPeace(ContentSet content,EmpireView view,ConflictRelationView relation)
    {
        DevelopmentUi.Clear(this);Name="ConflictPeacePanel";AddChild(new Label{Name="ConflictRelationText",Text=relation.Relation.ToString(),ThemeTypeVariation="HeaderLabel"});
        if(relation.TruceEndTurn is {} end&&end>view.Turn)AddChild(DevelopmentUi.Text($"Truce: declaration becomes available in planning turn {end}; {end-view.Turn} complete peaceful boundaries remain."));
        if(relation.Relation!=RelationKind.War)return;
        var terms=DevelopmentUi.Text($"Peace is unconditional. Successful peace takes effect before income and combat at the next boundary: no final volley, interception or bilateral blockade loss. The truce lasts {content.Conflict!.Truce} complete peaceful boundaries. Surviving ships keep their damage and cohesion; there is no automatic repair, repatriation or restoration of agreements.");terms.Name="ConflictPeaceTerms";AddChild(terms);
        void Order(string text,string name,ConflictAction action){var preview=ConflictQueries.Preview(content,view,action);var button=DevelopmentUi.Button(this,text,()=>CommandIssued?.Invoke(new ConflictCommand(view.Viewer,view.Turn,action,preview.Token)),name);button.Disabled=!preview.Allowed;}
        if(relation.Peace is {} offer)
        {
            AddChild(DevelopmentUi.Text($"Peace offer: published turn {offer.CreatedTurn}; answer by the resolution to turn {offer.ExpiryTurn}. No answer is required to Ready."));
            if(offer.Proposer==view.Viewer){AddChild(DevelopmentUi.Text("Your outgoing peace offer awaits a response. Their queued answer remains private."));Order("Withdraw peace offer","ConflictWithdrawPeace",new ConflictAction.WithdrawPeace(relation.Other,offer.Id));}
            else if(offer.OwnAnswer is {} answer){AddChild(DevelopmentUi.Text(answer?"Your Accept is queued.":"Your Decline is queued."));Order("Revoke queued peace answer","ConflictRevokePeaceAnswer",new ConflictAction.RevokePeaceAnswer(relation.Other,offer.Id));}
            else{Order("Accept peace","ConflictAcceptPeace",new ConflictAction.AnswerPeace(relation.Other,offer.Id,true));Order("Decline peace","ConflictDeclinePeace",new ConflictAction.AnswerPeace(relation.Other,offer.Id,false));}
        }
        else if(relation.OwnPeaceProposed){AddChild(DevelopmentUi.Text($"Your peace proposal is queued for publication at turn {(long)view.Turn+1}."));Order("Withdraw queued peace proposal","ConflictWithdrawPeace",new ConflictAction.WithdrawPeace(relation.Other,new(0)));}
        else{Order("Propose unconditional peace","ConflictProposePeace",new ConflictAction.ProposePeace(relation.Other));}
    }
}
