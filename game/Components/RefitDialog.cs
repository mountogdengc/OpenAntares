using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.ShipDesigns;
namespace OpenAntares.Game.Components;
public partial class RefitDialog:AcceptDialog
{
    public event Action<RefitAction,string>? ActionRequested;
    public VesselId Vessel {get;private set;}
    private ContentSet _content=null!;private EmpireView _view=null!;private RefitAction? _pending;private RefitPreview? _preview;private bool _loading;
    private readonly OptionButton _port=new(){Name="RefitPort"},_target=new(){Name="RefitTarget"};
    private readonly Label _details=DevelopmentUi.Text("Review the selected ship, port and target."),_error=DevelopmentUi.Text("","WarningLabel");
    private Button _apply=null!,_cancel=null!,_resume=null!;
    public RefitDialog() { }
    public RefitDialog(ContentSet content,EmpireView view,VesselId vessel){_content=content;_view=view;Vessel=vessel;}
    public override void _Ready()
    {
        ForceNative=true;Name="RefitDialog";Title="Review ship refit";OkButtonText="Close";var body=DevelopmentUi.DialogBody(this);body.AddChild(DevelopmentUi.Text("Replace equipment on the same surviving frame. Start requires full hull/cohesion, zero cargo and a stationary idle ship at your own idle port."));body.AddChild(DevelopmentUi.Text("Shipyard port"));body.AddChild(_port);body.AddChild(DevelopmentUi.Text("Target equipment"));body.AddChild(_target);_details.Name="RefitPreviewDetails";body.AddChild(_details);_error.Name="RefitError";body.AddChild(_error);
        DevelopmentUi.Button(body,"Refresh reviewed preview",StartPreview,"RefitPreview");_apply=DevelopmentUi.Button(body,"Apply reviewed refit",()=>{if(_pending is not null&&_preview is{Allowed:true})ActionRequested?.Invoke(_pending,_preview.Token);},"RefitApply");_cancel=DevelopmentUi.Button(body,"Review Cancel refit",()=>{if(Job() is {} job)Preview(new RefitAction.Cancel(job.Id,job.Revision));},"RefitCancel");_resume=DevelopmentUi.Button(body,"Review Resume refit",()=>{if(Job() is {} job)Preview(new RefitAction.Resume(job.Id,job.Revision));},"RefitResume");
        _port.ItemSelected+=_=>{if(!_loading)StartPreview();};_target.ItemSelected+=_=>{if(!_loading)StartPreview();};
        if(_content is not null)ShowRefit(_content,_view,Vessel);
    }
    public void ShowRefit(ContentSet content,EmpireView view,VesselId vessel,bool resetError=true)
    {
        int oldPort=Vessel==vessel&&_port.ItemCount>0?_port.GetSelectedMetadata().AsInt32():0,oldTarget=Vessel==vessel&&_target.ItemCount>0?_target.GetSelectedMetadata().AsInt32():0;_loading=true;_content=content;_view=view;Vessel=vessel;_port.Clear();_target.Clear();if(resetError)_error.Text="";var ship=view.Vessels.Single(v=>v.Id==vessel);var job=view.ShipDesigns!.Refits.SingleOrDefault(j=>j.Vessel==vessel);
        foreach(var port in view.Colonies.OrderBy(c=>c.StarId==ship.StationaryStarId?0:1).ThenBy(c=>c.Id)){_port.AddItem("Port #"+port.Id.Value+" — "+view.Stars.Single(s=>s.Chart.Id==port.StarId).Chart.Name);_port.SetItemMetadata(_port.ItemCount-1,port.Id.Value);if(job?.Port==port.Id||job is null&&oldPort==port.Id.Value)_port.Select(_port.ItemCount-1);}
        foreach(var target in view.ShipDesigns.Designs.Where(d=>d.Id!=ship.Loadout?.Design&&(!d.Archived||job?.TargetDesign==d.Id))){_target.AddItem(ShipDesignUi.DesignName(content,view,target.Id)+(target.Archived?" (archived; committed)":""));_target.SetItemMetadata(_target.ItemCount-1,target.Id.Value);if(job?.TargetDesign==target.Id||job is null&&oldTarget==target.Id.Value)_target.Select(_target.ItemCount-1);}
        _port.Disabled=job is not null;_target.Disabled=job is not null;_cancel.Visible=job is not null;_resume.Visible=job?.Status==RefitStatus.Paused;_loading=false;StartPreview();if(!Visible)PopupCenteredClamped(new(700,560));
    }
    public void ShowError(string error)=>_error.Text=error;
    private RefitView? Job()=>_view.ShipDesigns?.Refits.SingleOrDefault(j=>j.Vessel==Vessel);
    private void StartPreview()
    {
        if(_port.ItemCount==0||_target.ItemCount==0){_pending=null;_preview=null;_apply.Disabled=true;_details.Text="Save another same-frame design and select an owned port before starting a refit.";return;}
        Preview(Job() is{Status:RefitStatus.Paused} job?new RefitAction.Resume(job.Id,job.Revision):new RefitAction.Start(Vessel,new(_port.GetSelectedMetadata().AsInt32()),new(_target.GetSelectedMetadata().AsInt32())));
    }
    private void Preview(RefitAction action)
    {
        _pending=action;_preview=RefitQueries.Preview(_content,_view,action);var job=Job();var ship=_view.Vessels.Single(v=>v.Id==Vessel);var source=_view.ShipDesigns!.Designs.Single(d=>d.Id==ship.Loadout!.Design);var target=_view.ShipDesigns.Designs.SingleOrDefault(d=>d.Id.Value==_target.GetSelectedMetadata().AsInt32());var port=_view.Colonies.SingleOrDefault(c=>c.Id.Value==_port.GetSelectedMetadata().AsInt32());var condition=_view.Combat!.Ships.Single(s=>s.Id==Vessel);
        long? price=job?.Price??_preview.Price??(target is null?null:ShipDesignMath.RefitPrice(target.Stats.BuildCost));string stats(DesignView d)=>$"{d.Name} — {_content.ShipDesigns!.Frames[d.Recipe.FrameId].Name}: hull {d.Stats.MaxHull}, attack {d.Stats.Attack}, armor {d.Stats.Armor}, speed {d.Stats.BaseSpeed}.\n{ShipDesignUi.EquipmentSummary(_content,d.Recipe)}";
        string why=_preview.Allowed?string.Join("\n",_preview.Consequences):_preview.RejectionCode switch{"refit_condition"=>"Full hull and cohesion with zero cargo are required. Repair this ship before refitting.","refit_project_busy"=>"This port already has a project. Review that work or choose another idle port.","refit_frame"=>"Choose equipment on the same frame.","refit_location"=>"The ship must remain stationary at the selected owned port.","refit_activity"=>"Finish or cancel the ship's travel, combat, return, repair or survival orders first.","refit_request" or "refit_request_port"=>"Pause or finish the affected finite assembly commitment before refitting.","refit_fleet_order"=>"The affected fleet must remain in Hold.",_=>"This refit is unavailable or changed. Review the current ship, port and target."};
        _details.SetMeta("rejection_code",_preview.RejectionCode??"");_details.Text=$"Current: {stats(source)}\nCurrent condition: hull {condition.Hull}/{condition.MaxHull}, cohesion {condition.Cohesion}.\nTarget: {(target is null?"None":stats(target))}\n{(target is null?"":ShipDesignUi.EquipmentChanges(_content,source.Recipe,target.Recipe))}\nRefit fee: {(price is {} fee?Format.Points(fee)+$" ({fee} internal)":"unavailable")}. Ceiling of target undiscounted build cost /4, minimum1 internal; no Organizer discount.\nPort bank: {Format.Points(port?.ProductionReserve??0)}; diplomacy reserved {Format.Points(port?.ReservedProduction??0)}; spendable {Format.Points(port?.SpendableProduction??0)}. Construction stays paused; workforce, template and finite queue are retained.\n"+(job is null?"":job.StatusExplanation+"\n")+(_preview.EstimatedTurns is {} turns?$"Estimate: {turns} full stationary interval(s); equipment changes apply only to the next snapshot.\n":"No completion estimate for this action.\n")+why+(port is null?"":"\n"+ConflictBlockadePanel.Explain(_view,port.StarId));
        _apply.Text=action is RefitAction.Cancel?"Cancel refit — keep reserve":action is RefitAction.Resume?"Resume reviewed refit":"Apply reviewed refit";_apply.Disabled=!_preview.Allowed||action is RefitAction.Start&&job is not null;
    }
}
