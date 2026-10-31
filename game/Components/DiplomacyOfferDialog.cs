using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Diplomacy;
namespace OpenAntares.Game.Components;
public partial class DiplomacyOfferDialog:AcceptDialog
{
    public event Action<Command>? CommandIssued;
    private readonly OptionButton _picker=new(){Name="DiplomacyTarget",SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};
    private readonly Label _details=DevelopmentUi.Text(""),_error=DevelopmentUi.Text("","WarningLabel");
    private readonly CheckBox _consent=new(){Name="DiplomacyConsent",Text="I confirm the disclosure or termination described above."};
    private Button _apply=null!;private ContentSet? _content;private EmpireView? _view;private EmpireId _other;private DiplomacyAction? _draft;private DiplomacyPreview? _preview;private bool _hub,_star;
    public override void _Ready(){Name="DiplomacyOfferDialog";Title="Diplomatic terms";OkButtonText="Close";var body=DevelopmentUi.DialogBody(this);body.AddChild(_picker);_details.Name="DiplomacyTerms";_error.Name="DiplomacyError";body.AddChild(_details);body.AddChild(_consent);body.AddChild(_error);_apply=DevelopmentUi.Button(body,"Submit diplomatic order",Submit,"DiplomacySubmit");_picker.ItemSelected+=_=>Preview();_consent.Toggled+=_=>Enable();}
    public void Open(ContentSet content,EmpireView view,EmpireId other,DiplomacyAction action)
    {
        _content=content;_view=view;_other=other;_draft=action;_error.Text="";_consent.SetPressedNoSignal(false);_hub=action is DiplomacyAction.OfferTrade||action is DiplomacyAction.Answer{Accept:true}&&view.Diplomacy!.Contacts.Single(c=>c.Identity.OtherEmpireId==other).Pending?.Kind==AgreementKind.Trade;_star=action is DiplomacyAction.OfferSettlement;
        FillPicker(null);_consent.Visible=_star||action is DiplomacyAction.Terminate;Preview();PopupCenteredClamped(new(700,540));
    }
    private void FillPicker(int? selected)
    {
        _picker.Clear();_picker.Visible=_hub||_star;
        if(_hub)foreach(var c in _view!.Colonies){_picker.AddItem(_view.Research.ColonyNames[c.Id.ToString()]);_picker.SetItemMetadata(_picker.ItemCount-1,c.Id.Value);}
        if(_star)foreach(var s in _view!.Stars.Where(s=>s.Current&&s.Planets is {} planets&&planets.Any(p=>p.OwnerId==_view.Viewer||p.OwnerId==_other))){_picker.AddItem(s.Chart.Name);_picker.SetItemMetadata(_picker.ItemCount-1,s.Chart.Id.Value);}
        if(selected is {} id)for(int i=0;i<_picker.ItemCount;i++)if(_picker.GetItemMetadata(i).AsInt32()==id)_picker.Select(i);
    }
    public void UpdateView(EmpireView view){if(_view is null)return;int? id=_picker.ItemCount>0?_picker.GetSelectedMetadata().AsInt32():null;_view=view;FillPicker(id);Preview();}
    public void ShowError(string error)=>_error.Text=error;
    private DiplomacyAction Action()
    {
        int id=_picker.ItemCount>0?_picker.GetSelectedMetadata().AsInt32():0;
        return _draft switch{DiplomacyAction.OfferTrade=>new DiplomacyAction.OfferTrade(new(id)),DiplomacyAction.OfferSettlement=>new DiplomacyAction.OfferSettlement(new(id)),DiplomacyAction.Answer a when _hub=>a with{Hub=new ColonyId(id)},_=>_draft!};
    }
    private void Preview()
    {
        if(_view is null||_content is null)return;_preview=DiplomacyQueries.Preview(_content,_view,_other,Action());
        _details.Text=_preview.Terms+"\n"+string.Join("\n",_preview.Consequences)+"\n"+_preview.PolicyReason;
        if(_preview.Hub is {} h)_details.Text+=$"\nOwn bank: {Format.Points(h.Bank)} ({h.Bank} internal); {Format.Points(h.Reserved)} ({h.Reserved}) reserved; {Format.Points(h.Spendable)} ({h.Spendable}) spendable. "+(h.Allowed?"Hub ready.":h.Reason?.Replace('_',' '));
        if(_preview.BreakEvenBoundaries is {} n)_details.Text+=$"\nConditional break-even: {n} full effective turns if both hubs survive. No refund on termination or hub loss.";
        if(Action() is DiplomacyAction.Terminate)_details.Text+=$"\nNotice submitted now ends the agreement at turn {_view.Turn+_content.Diplomacy!.Notice}. Cancellation memory is published at the next resolution.";
        if(!_preview.Allowed)_details.Text+="\nUnavailable: "+_preview.RejectionCode?.Replace('_',' ');Enable();
    }
    private void Enable(){if(_apply is not null)_apply.Disabled=_preview?.Allowed!=true||_consent.Visible&&!_consent.ButtonPressed;}
    private void Submit(){if(_view is null||_preview?.Allowed!=true||_consent.Visible&&!_consent.ButtonPressed)return;CommandIssued?.Invoke(new DiplomacyCommand(_view.Viewer,_view.Turn,_other,_preview.PublicRevision,_preview.PrivateRevision,Action()));}
}
