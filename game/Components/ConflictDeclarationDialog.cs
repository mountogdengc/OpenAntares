using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class ConflictDeclarationDialog:AcceptDialog
{
    public event Action<Command>? CommandIssued;
    private readonly Label _terms=DevelopmentUi.Text(""),_error=DevelopmentUi.Text("","WarningLabel");
    private Button _apply=null!;private ContentSet? _content;private EmpireView? _view;private EmpireId _other;private ConflictPreview? _review;
    public override void _Ready()
    {
        Name="ConflictDeclarationDialog";Title="Review declaration of war";ForceNative=true;OkButtonText="Cancel";var body=DevelopmentUi.DialogBody(this);_terms.Name="ConflictDeclarationTerms";_error.Name="ConflictDeclarationError";body.AddChild(_terms);body.AddChild(_error);DevelopmentUi.Button(body,"Refresh reviewed declaration",Review,"ConflictDeclarationRefresh");_apply=DevelopmentUi.Button(body,"Confirm declaration of war",()=>{if(_review is{Allowed:true}&&_view is not null)CommandIssued?.Invoke(new ConflictCommand(_view.Viewer,_view.Turn,new ConflictAction.DeclareWar(_other),_review.Token));},"ConflictDeclarationApply");
    }
    public void Open(ContentSet content,EmpireView view,EmpireId other){_content=content;_view=view;_other=other;ForceNative=true;Review();PopupCenteredClamped(new(650,490));}
    public void UpdateView(EmpireView view)
    {
        if(_view is null)return;if(_view.Viewer!=view.Viewer){Hide();return;}_view=view;var current=ConflictQueries.Preview(_content!,view,new ConflictAction.DeclareWar(_other));if(_review is null||!current.Allowed||current.Token!=_review.Token){_apply.Disabled=true;_error.Text="The reviewed situation changed. Refresh this declaration before confirming.";}
    }
    private void Review()
    {
        if(_view is null||_content is null)return;_review=ConflictQueries.Preview(_content,_view,new ConflictAction.DeclareWar(_other));var contact=_view.Diplomacy!.Contacts.Single(c=>c.Identity.OtherEmpireId==_other);string agreements=contact.Agreements.IsEmpty?"There are no current bilateral agreements.":"Agreements ending at publication: "+string.Join(", ",contact.Agreements.Select(a=>a.Kind))+".";
        _terms.Text=$"Declare war on {contact.Identity.CultureName}?\nThe declaration is queued for publication at turn {(long)_view.Turn+1}. It does not authorize an attack or blockade in this planning turn. You may revoke it before the boundary.\n{agreements}\nPending bilateral offers and setup reservations are cancelled without a new setup payment. Paid setup is not refunded. Existing trade and settlement effects finish their current interval, then end at publication.\nArmed vessels become wartime targets. Scouts, settlers, passengers, colonies and population remain protected; colonies cannot be captured. Fleet battles may destroy your ships and endanger their officers.\nEach empire may have one active opponent. Counterpart availability is rechecked at resolution; this review does not reveal another empire's private planning.\nA declaration creates a {_content.Conflict!.DeclarationGrievance}-point grievance toward its declarer, lasting throughout war and {_content.Conflict.GrievancePeaceLifetime} complete peaceful boundaries afterward.";
        _apply.Disabled=!_review.Allowed;_error.Text=_review.Allowed?"":"Declaration is unavailable under the current contact, truce or active-opponent limit.";
    }
    public void ShowError(string error,bool accepted){if(accepted)Hide();else _error.Text=error;}
}
