using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class CombatOrdersDialog:AcceptDialog
{
    private ContentSet _content=null!;private EmpireView _view=null!;private CombatAction _action=null!;private Action<CombatActionCommand>? _apply;
    private CombatActionCommand? _pending;private VBoxContainer _configuration=null!;
    private readonly OptionButton _doctrine=new(){Name="CombatDoctrine"},_return=new(){Name="CombatReturnColony",FitToLongestItem=false,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill},_stragglers=new(){Name="CombatStragglers"},_arrival=new(){Name="CombatOnArrival"};
    private readonly CheckBox _cargo=new(){Name="CombatCargoConsent",Text="I authorize the displayed civilian and embarked population risk"};
    private readonly Label _details=DevelopmentUi.Text(""),_error=DevelopmentUi.Text("","WarningLabel");private Button _accept=null!,_withdraw=null!,_force=null!,_remain=null!;
    public override void _Ready()
    {
        Name="CombatOrdersDialog";Title="Combat orders and risk";OkButtonText="Close";var body=DevelopmentUi.DialogBody(this);
        body.AddChild(DevelopmentUi.Text("Automatic battles: one exchange per strategic turn. Move never means attack. No combat XP, loot or ownership is awarded."));
        _configuration=new(){Name="CombatOrderFields"};body.AddChild(_configuration);
        _configuration.AddChild(DevelopmentUi.Text("Attack doctrine"));_configuration.AddChild(_doctrine);foreach(string d in new[]{"Cautious","Press","Hit and run","Hold despite losses"})_doctrine.AddItem(d);
        _configuration.AddChild(DevelopmentUi.Text("Owned return colony — explicitly select and review"));_configuration.AddChild(_return);
        _configuration.AddChild(DevelopmentUi.Text("Stragglers"));_configuration.AddChild(_stragglers);_stragglers.AddItem("Keep together");_stragglers.AddItem("Detach crippled non-flagships");
        _configuration.AddChild(DevelopmentUi.Text("On arrival"));_configuration.AddChild(_arrival);_arrival.AddItem("Repair");_arrival.AddItem("Hold");_configuration.AddChild(_cargo);
        DevelopmentUi.Button(body,"Preview exact combat order",()=>Review(OrderAction()),"CombatPreview");
        _withdraw=DevelopmentUi.Button(body,"Withdraw under committed return orders",()=>Review(new CombatAction.WithdrawCombat(Encounter()!.Id,Encounter()!.Revision)),"CombatWithdraw");
        _force=DevelopmentUi.Button(body,"Review / accept revised force",()=>Review(new CombatAction.RespondCombat(Encounter()!.Id,Encounter()!.Revision,CombatResponse.AcceptRevisedForce)),"CombatAcceptForce");
        _remain=DevelopmentUi.Button(body,"Review one-turn defensive risk with blocked return",()=>Review(new CombatAction.RespondCombat(Encounter()!.Id,Encounter()!.Revision,CombatResponse.RemainWithBlockedReturn)),"CombatRemainBlocked");
        _details.Name="CombatPreviewDetails";body.AddChild(_details);_error.Name="CombatError";body.AddChild(_error);
        _accept=DevelopmentUi.Button(body,"Apply reviewed command",()=>{if(_pending is {} command)_apply?.Invoke(command);},"CombatApply");_accept.Disabled=true;
        foreach(var picker in new[]{_doctrine,_return,_stragglers,_arrival})picker.ItemSelected+=_=>Invalidate();_cargo.Toggled+=_=>Invalidate();
    }
    public void Open(ContentSet content,EmpireView view,CombatAction action,Action<CombatActionCommand> apply)
    {
        _content=content;_view=view;_action=action;_apply=apply;_pending=null;_accept.Disabled=true;_details.Text="Choose the visible authority, then preview exact ships, shots, return groups and risk. Responses use the committed encounter orders.";_error.Text="";
        var current=Encounter();var orders=action is CombatAction.AttackPocket a?a.Orders:current?.Orders;
        _configuration.Visible=action is not CombatAction.SetRepair;
        if(orders is not null)
        {
            _doctrine.Select((int)orders.Doctrine);_stragglers.Select((int)orders.Stragglers);_arrival.Select((int)orders.OnArrival);
            _return.Clear();_return.AddItem("Select an owned return colony");_return.SetItemMetadata(0,0);
            foreach(var port in view.Colonies.OrderBy(c=>c.Id)){_return.AddItem($"{view.Galaxy.Stars.Single(s=>s.Id==port.StarId).Name} — colony {port.Id.Value}");_return.SetItemMetadata(_return.ItemCount-1,port.Id.Value);if(port.Id==orders.ReturnColony)_return.Select(_return.ItemCount-1);}
            _cargo.SetPressedNoSignal(action is CombatAction.AttackPocket attack?attack.AuthorizeCivilianRisk:current?.CivilianRiskAuthorized==true);
        }
        Responses();PopupCenteredClamped(new(740,560));_accept.Disabled=true;
    }
    public void UpdateView(EmpireView view)
    {
        bool changed=view.Turn!=_view.Turn;var old=Encounter();_view=view;var current=Encounter();changed|=old?.Revision!=current?.Revision;
        if(changed){_pending=null;_accept.Disabled=true;_details.Text="The planning facts changed. Open a fresh combat order and preview again.";}Responses();
    }
    public void ShowError(string message){_error.Text=message;if(message.Length>0){_pending=null;_accept.Disabled=true;}}
    private CombatEncounterView? Encounter()=>_view?.Combat?.Encounters.SingleOrDefault(e=>e.Id==(_action switch{CombatAction.ChangeCombatOrders a=>a.Encounter,CombatAction.WithdrawCombat a=>a.Encounter,CombatAction.RespondCombat a=>a.Encounter,_=>default}));
    private CombatOrders Orders()=>new((CombatDoctrine)_doctrine.Selected,new(_return.ItemCount==0?0:_return.GetSelectedMetadata().AsInt32()),(StragglerPolicy)_stragglers.Selected,(CombatArrivalOrder)_arrival.Selected);
    private CombatAction OrderAction()=>_action is CombatAction.SetRepair repair?repair:_action is CombatAction.AttackPocket attack?attack with{Orders=Orders(),AuthorizeCivilianRisk=_cargo.ButtonPressed}:Encounter() is {} e?new CombatAction.ChangeCombatOrders(e.Id,e.Revision,Orders(),_cargo.ButtonPressed):_action;
    private void Invalidate(){_pending=null;_accept.Disabled=true;_details.Text="Authority changed; preview again before applying.";Responses();}
    private void Responses()
    {
        var e=Encounter();_withdraw.Visible=_force.Visible=_remain.Visible=e is not null;if(e is null)return;
        _withdraw.Disabled=Orders()!=e.Orders||!CombatQueries.Preview(_content,_view,new CombatAction.WithdrawCombat(e.Id,e.Revision)).Allowed;_withdraw.TooltipText=Orders()!=e.Orders?"Apply changed combat orders first, then review withdrawal.":"Review the exact frozen departure cohorts.";
        _force.Disabled=!CombatQueries.Preview(_content,_view,new CombatAction.RespondCombat(e.Id,e.Revision,CombatResponse.AcceptRevisedForce)).Allowed;
        _remain.Disabled=!CombatQueries.Preview(_content,_view,new CombatAction.RespondCombat(e.Id,e.Revision,CombatResponse.RemainWithBlockedReturn)).Allowed;
    }
    private void Review(CombatAction action)
    {
        var preview=CombatQueries.Preview(_content,_view,action);_pending=null;_accept.Disabled=true;
        _error.Text=preview.Allowed?"":action is not CombatAction.SetRepair&&Orders().ReturnColony.Value==0?"Select an owned return colony before previewing.":preview.RejectionCode switch{CombatRejectionCodes.Reserved=>"This pocket is unavailable for a new commitment.",CombatRejectionCodes.StalePreview=>"The planning facts changed. Review a fresh preview.",CombatRejectionCodes.PortClosed=>"The selected return route is blocked for these ships or cargo. Choose a legal return, or review its specific defensive-risk response.",_=>"This action is unavailable under the current fleet orders or civilian-risk consent."};
        var lines=new System.Collections.Generic.List<string>{(action switch{CombatAction.AttackPocket=>"Attack pocket",CombatAction.ChangeCombatOrders=>"Change combat orders",CombatAction.WithdrawCombat=>"Withdraw",CombatAction.SetRepair {Enabled:true}=>"Repair selected ships",CombatAction.SetRepair=>"Hold / stop repair",CombatAction.RespondCombat {Response:CombatResponse.AcceptRevisedForce}=>"Accept revised force",_=>"One-turn defensive risk"})+" — exact planning preview"};
        if(preview.Frame is {} frame)lines.Add($"Turn {frame.Turn}, revision {frame.Revision}; doctrine {_doctrine.GetItemText((int)frame.Orders.Doctrine)}; return colony {frame.Orders.ReturnColony.Value}; stragglers {(frame.Orders.Stragglers==StragglerPolicy.KeepTogether?"Keep together":"Detach crippled non-flagships")}; On arrival: {frame.Orders.OnArrival}.");
        lines.Add($"Affected ships: {preview.AffectedShips.Length}; embarked population at risk: {preview.Cargo}. ");
        foreach(var ship in preview.AffectedShips)lines.Add($"Ship {ship.VesselId.Value}: hull {ship.Hull}/{ship.MaxHull}, cohesion {ship.Cohesion}, base attack {ship.Attack}, armor {ship.Armor}, effective speed {ship.EffectiveSpeed}{(ship.Flagship?"; flagship":"")}.");
        if(preview.Exchange is {} result)foreach(var shot in result.Shots)lines.Add($"{(shot.Source.Sentry?"Sentry":"Ship")} {shot.Source.Id} fires at {(shot.Target.Sentry?"sentry":"ship")} {shot.Target.Id}: {shot.Damage} damage; frozen targets, no spill.");
        foreach(var forecast in preview.Forecasts)lines.Add($"Ship {forecast.Id.Value}: {forecast.Before} → {forecast.After?.ToString()??"destroyed"}; cohesion {forecast.CohesionBefore} → {forecast.CohesionAfter}.");
        foreach(var group in preview.Departures)lines.Add($"Return cohort [{string.Join(", ",group.Members.Select(v=>v.Value))}]: {_view.Galaxy.Stars.Single(s=>s.Id==group.Origin).Name} → {_view.Galaxy.Stars.Single(s=>s.Id==group.Destination).Name}; speed {group.Speed}, estimated {group.TotalTurns} turn(s); {(group.Members.Any(preview.Exposed.Contains)?"EXPOSED to one shared enemy-only interception volley":"clean departure under current observed speeds")}; On arrival: {group.OnArrival}.");
        foreach(var repair in preview.RepairForecasts)lines.Add($"Ship {repair.Id.Value}: {repair.Intervals} full eligible repair interval(s) after reaching an owned legal port; no arrival tick.");
        if(preview.OfficerRisk is {} officer)
        {
            string doctrine=officer.Doctrine==OpenAntares.Simulation.Fleets.OfficerDoctrine.LastWatch?"Last watch":"Recover command";
            lines.Add(officer.Embodiment==OpenAntares.Simulation.Fleets.CommandEmbodiment.ColonyBody?$"Flagship officer {officer.Name}: {doctrine}. The whole source body remains in its colony. If that body survives flagship loss, the link returns after {officer.Delay} full subsequent turn(s), with no officer random draw and identity / XP retained.":$"Flagship officer {officer.Name}: {doctrine}, recovery {officer.RecoveryPercent}%, delay {officer.Delay} full subsequent turn(s). {(officer.RecoveryPercent==0?"Last watch makes permanent loss certain, without a recovery draw.":"Successful recovery retains identity and XP.")} Combat gives no XP.");
        }
        lines.AddRange(preview.Explanations);_details.Text=string.Join("\n",lines);
        if(preview.Allowed){_pending=new(_view.Viewer,_view.Turn,action,preview.Token);_accept.Disabled=false;}
    }
}
