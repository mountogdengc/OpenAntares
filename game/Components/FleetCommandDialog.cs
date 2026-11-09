using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class FleetCommandDialog:AcceptDialog
{
    public event Action<Command>? CommandIssued;
    public event Action<CombatAction>? CombatRequested;
    public event Action<VesselId?>? ConflictOrdersRequested;
    private readonly ConflictBattlePanel _conflictReport=new(){Name="FleetConflictReport"};
    public string CombatPreferencesPath {get;set;}="user://combat-notifications.json";
    private readonly CombatReportPanel _combatReport=new(){Name="FleetCombatReport"};
    public event Action<VesselId>? InspectRequested;
    public event Action<StarId>? StarRequested;
    public event Action<RequestId?>? RequestRequested;
    public event Action? OfficersRequested;
    public event Action? DesignsRequested;
    public event Action<VesselId>? RefitRequested;
    public event Action? DisplayApplied;
    public string PreferencesPath {get;set;}="user://fleet-notifications.json";
    private ContentSet _content=null!;private EmpireView _view=null!;private VBoxContainer _body=null!;
    private readonly TabContainer _tabs=new(){Name="FleetTabs",SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};
    private readonly FleetList _ships=new(){Name="FleetShipList",SizeFlagsVertical=Control.SizeFlags.ExpandFill};
    private readonly ItemList _fleets=new(){Name="FleetGroupList",SelectMode=ItemList.SelectModeEnum.Multi,CustomMinimumSize=new(0,120)};
    private readonly ItemList _members=new(){Name="FleetMemberList",SelectMode=ItemList.SelectModeEnum.Multi,CustomMinimumSize=new(0,100)};
    private readonly ItemList _requests=new(){Name="FleetRequestList",CustomMinimumSize=new(0,140)};
    private readonly OptionButton _filter=new(){Name="FleetShipFilter"},_roleFilter=new(){Name="FleetRoleFilter",FitToLongestItem=false,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill},_sort=new(){Name="FleetSort"},_destination=new(){Name="FleetDestination"},_groupDestination=new(){Name="FleetGroupDestination"},_order=new(){Name="FleetStandingOrder"},_flagship=new(){Name="FleetFlagshipPicker"};
    private readonly LineEdit _name=new(){Name="FleetName",PlaceholderText="Fleet name",MaxLength=80},_groupName=new(){Name="FleetGroupName",PlaceholderText="New or edited fleet name",MaxLength=80};
    private readonly Label _detail=DevelopmentUi.Text(""),_groupDetail=DevelopmentUi.Text(""),_requestDetail=DevelopmentUi.Text(""),_error=DevelopmentUi.Text("","WarningLabel");
    private int _requestPage,_eventPage,_quickRole;
    private readonly Label _requestPages=DevelopmentUi.Text(""),_eventPages=DevelopmentUi.Text("");
    private readonly VBoxContainer _events=new(){Name="FleetEventRows"};private readonly Label _confirmationText=DevelopmentUi.Text("");private readonly ConfirmationDialog _confirm=new(){Name="FleetActionConfirmation",Title="Review fleet command",OkButtonText="Apply reviewed command"};
    private readonly List<VesselId> _shipRows=new();private readonly List<FleetId> _fleetRows=new();private readonly List<VesselId> _memberRows=new();private readonly List<RequestId> _requestRows=new();private FleetCommand? _pending;
    public override void _Ready()
    {
        Name="FleetCommandDialog";Title="Fleet command";OkButtonText="Close";_body=DevelopmentUi.DialogBody(this);var introduction=DevelopmentUi.Text("Select ships to review travel and fleet orders.");introduction.TooltipText="All your ships, finite construction requests and command appointments. Every move uses normal travel; manual orders and live safety responses take precedence.";_body.AddChild(introduction);
        var top=new HBoxContainer();_body.AddChild(top);DevelopmentUi.Button(top,"New assembly request",()=>RequestRequested?.Invoke(null),"FleetNewRequest");DevelopmentUi.Button(top,"Admirals and service records",()=>OfficersRequested?.Invoke(),"FleetOpenOfficers");
        DevelopmentUi.Button(_body,"Ship designs",()=>DesignsRequested?.Invoke(),"OpenShipDesigns");
        _body.AddChild(_tabs);var ships=Page("Ships");var filters=new HBoxContainer();ships.AddChild(filters);filters.AddChild(_filter);filters.AddChild(_sort);ships.AddChild(_roleFilter);foreach(var f in new[]{"All","Idle","Unassigned","New","Travelling","Reserved","Attention"})_filter.AddItem(f);foreach(var s in new[]{"Ship ID","Role","Location"})_sort.AddItem(s);var chips=new HFlowContainer();ships.AddChild(chips);
        foreach(var chip in new[]{("All",0),("Military",1),("Support",2)}){var button=DevelopmentUi.Button(chips,chip.Item1,()=>{_quickRole=chip.Item2;Rows();},"FleetChip"+chip.Item1);button.ThemeTypeVariation="CompactCommandKey";}
        ships.AddChild(_ships); _ships.CustomMinimumSize=new Vector2(0,GetThemeConstant("fleet_list_height","ConsoleMetrics"));
        var select=new HBoxContainer();ships.AddChild(select);DevelopmentUi.Button(select,"Select all shown",()=>{for(int i=0;i<_ships.ItemCount;i++)_ships.Select(i,false);Details();},"FleetSelectAll");DevelopmentUi.Button(select,"Clear selection",()=>{_ships.DeselectAll();Details();},"FleetClearSelection");
        ships.AddChild(_detail);ships.AddChild(_destination);ships.AddChild(_name);var shipActions=new GridContainer{Columns=2};ships.AddChild(shipActions);
        DevelopmentUi.Button(shipActions,"Preview move",()=>Review(new FleetAction.MoveSelection(Ships(),ImmutableArray<FleetId>.Empty,Target(_destination))),"FleetPreviewMove");
        DevelopmentUi.Button(shipActions,"Cancel unstarted moves",()=>Review(new FleetAction.MoveSelection(Ships(),ImmutableArray<FleetId>.Empty,null)),"FleetCancelMoves");
        DevelopmentUi.Button(shipActions,"Create named fleet",()=>Review(new FleetAction.Create(_name.Text,Ships())),"FleetCreate");
        DevelopmentUi.Button(shipActions,"Detach selected ships",()=>Review(new FleetAction.DetachSelection(Ships())),"FleetDetach");
        DevelopmentUi.Button(shipActions,"Clear new markers",()=>Review(new FleetAction.DismissNewMarker(Ships())),"FleetDismissNew");
        DevelopmentUi.Button(shipActions,"Inspect ship / choose founding planet",()=>{if(Ships().Length==1){Hide();InspectRequested?.Invoke(Ships()[0]);}else _error.Text="Select one ship to inspect its separate planet choices.";},"FleetInspectShip");
        DevelopmentUi.Button(ships,"Review selected ship refit",()=>{var ids=Ships();if(ids.Length==1&&_view.Vessels.Single(v=>v.Id==ids[0]).Loadout?.Design is not null)RefitRequested?.Invoke(ids[0]);else _error.Text="Select one owned warship to review its equipment and refit eligibility.";},"FleetOpenRefit");
        DevelopmentUi.Button(ships,"Locate selected ship",()=>{if(Ships().Length!=1)return;var ship=_view.Vessels.Single(v=>v.Id==Ships()[0]);StarRequested?.Invoke(ship.StationaryStarId??ship.Transit!.Destination);},"FleetLocateShip");
        DevelopmentUi.Button(ships,"Preview Repair for selected ships",()=>CombatRequested?.Invoke(new CombatAction.SetRepair(Ships(),true)),"FleetRepair");
        DevelopmentUi.Button(ships,"Preview Hold / stop finite repair",()=>CombatRequested?.Invoke(new CombatAction.SetRepair(Ships(),false)),"FleetStopRepair");
        var groups=Page("Fleets");groups.AddChild(_fleets);groups.AddChild(_groupDetail);groups.AddChild(_groupDestination);groups.AddChild(_order);foreach(var order in Enum.GetValues<FleetOrderKind>())_order.AddItem(order.ToString());groups.AddChild(_groupName);var groupActions=new GridContainer{Columns=2};groups.AddChild(groupActions);
        DevelopmentUi.Button(groupActions,"Preview fleet move",()=>Review(new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,Groups(),Target(_groupDestination))),"FleetPreviewGroupMove");
        DevelopmentUi.Button(groupActions,"Apply standing order",()=>Review(new FleetAction.SetOrder(Groups(),new((FleetOrderKind)_order.Selected,_order.Selected is 0 or 2?null:Target(_groupDestination)))),"FleetSetStandingOrder");
        DevelopmentUi.Button(groupActions,"Rename fleet",()=>SingleGroup(id=>Review(new FleetAction.Rename(id,_groupName.Text))),"FleetRename");
        DevelopmentUi.Button(groupActions,"Disband fleet",()=>SingleGroup(id=>Review(new FleetAction.Disband(id))),"FleetDisband");
        DevelopmentUi.Button(groupActions,"Merge into lowest selected fleet ID",()=>{var ids=Groups();if(ids.Length>1)Review(new FleetAction.Merge(ids[0],ids.Skip(1).ToImmutableArray()));else _error.Text="Select at least two co-located fleets.";},"FleetMerge");
        groups.AddChild(DevelopmentUi.Text("Selected fleet members; choose a flagship or an explicit subset to split."));groups.AddChild(_members);groups.AddChild(_flagship);
        DevelopmentUi.Button(groups,"Set flagship",()=>SingleGroup(id=>{if(_flagship.ItemCount>0)Review(new FleetAction.SetFlagship(id,new(_flagship.GetSelectedMetadata().AsInt32())));}),"FleetSetFlagship");
        DevelopmentUi.Button(groups,"Split selected members into named fleet",()=>SingleGroup(id=>Review(new FleetAction.Split(id,_groupName.Text,_members.GetSelectedItems().Select(i=>_memberRows[i]).ToImmutableArray()))),"FleetSplit");
        DevelopmentUi.Button(groups,"Attack / combat orders / Withdraw",()=>SingleGroup(CombatOrder),"FleetCombatOrders");
        var requests=Page("Requests");requests.AddChild(_requests);Navigation(requests,"FleetRequests",_requestPages,delta=>{_requestPage+=delta;RequestRows();});requests.AddChild(_requestDetail);DevelopmentUi.Button(requests,"Review selected finite request",()=>{int i=_requests.GetSelectedItems().FirstOrDefault(-1);if(i>=0)RequestRequested?.Invoke(_requestRows[i]);},"FleetEditRequest");
        var history=Page("Reports");history.AddChild(DevelopmentUi.Text("Attention remains visible in the command badge and service/request records. Routine display is local to this computer."));var levels=new OptionButton{Name="FleetRoutineDisplay"};levels.AddItem("Routine: Summary");levels.AddItem("Routine: Off");history.AddChild(levels);DevelopmentUi.Button(history,"Apply local display",()=>{try{new FleetNotificationPreferences(levels.Selected==0).Save(ProjectSettings.GlobalizePath(PreferencesPath));Events();DisplayApplied?.Invoke();}catch(Exception e)when(e is System.IO.IOException or UnauthorizedAccessException or ArgumentException){_error.Text="Display setting was not saved: "+e.Message;}},"FleetApplyDisplay");Navigation(history,"FleetEvents",_eventPages,delta=>{_eventPage+=delta;Events();});history.AddChild(_events);
        history.AddChild(_combatReport);var combatLevel=new OptionButton{Name="CombatRoutineDisplay"};combatLevel.AddItem("Combat routine: Summary");combatLevel.AddItem("Combat routine: Off");history.AddChild(combatLevel);
        DevelopmentUi.Button(history,"Apply local combat display",()=>{try{new CombatNotificationPreferences(combatLevel.Selected==0).Save(ProjectSettings.GlobalizePath(CombatPreferencesPath));CombatReport();DisplayApplied?.Invoke();}catch(Exception e)when(e is System.IO.IOException or UnauthorizedAccessException or ArgumentException){_error.Text="Combat display was not saved: "+e.Message;}},"CombatApplyDisplay");
        var conflict=Page("Empire conflict");conflict.AddChild(_conflictReport);_conflictReport.OrdersRequested+=anchor=>ConflictOrdersRequested?.Invoke(anchor);_conflictReport.StarRequested+=star=>StarRequested?.Invoke(star);
        _error.Name="FleetError";_confirmationText.Name="FleetConfirmationDetails";_body.AddChild(_error);AddChild(_confirm);DevelopmentUi.DialogBody(_confirm).AddChild(_confirmationText);_confirm.Confirmed+=()=>{if(_pending is {} command)CommandIssued?.Invoke(command);_pending=null;};
        _filter.ItemSelected+=_=>{Rows();};_roleFilter.ItemSelected+=_=>{Rows();};_sort.ItemSelected+=_=>{Rows();};_ships.MultiSelected+=(_,_,_)=>Details();_fleets.MultiSelected+=(_,_)=>GroupDetails();_requests.ItemSelected+=_=>RequestDetails();
    }
    private void Navigation(VBoxContainer parent,string id,Label label,Action<int> changed){var row=new HBoxContainer();parent.AddChild(row);DevelopmentUi.Button(row,"Previous page",()=>changed(-1),id+"Previous");row.AddChild(label);DevelopmentUi.Button(row,"Next page",()=>changed(1),id+"Next");}
    private static System.Collections.Generic.IEnumerable<T> VisiblePage<T>(System.Collections.Generic.IEnumerable<T> items,ref int page,int size,Label label){var rows=items.ToArray();int count=Math.Max(1,(rows.Length+size-1)/size);page=Math.Clamp(page,0,count-1);label.Text=$"Page {page+1}/{count}; {rows.Length} total";return rows.Skip(page*size).Take(size);}
    private VBoxContainer Page(string name){var page=new VBoxContainer{Name=name,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};_tabs.AddChild(page);return page;}
    public void ShowEmpire(ContentSet content,EmpireView view)
    {
        _content=content;_view=view;if(view.Fleets is null)return;
        _conflictReport.SetView(view.Conflict);_tabs.SetTabHidden(_tabs.GetTabCount()-1,view.Conflict is null);
        NamedDesignEntry();
        if(view.Combat is not null&&_filter.ItemCount==7)foreach(var label in new[]{"Warships","In combat","Withdrawing","Damaged","Crippled","Repairing"})_filter.AddItem(label);
        if(view.Combat is null)while(_filter.ItemCount>7)_filter.RemoveItem(_filter.ItemCount-1);
        foreach(var id in new[]{"FleetCombatOrders","FleetRepair","FleetStopRepair","CombatRoutineDisplay","CombatApplyDisplay"})FindChild(id,true,false)?.Set("visible",view.Combat is not null);
        CombatReport();FillRoles();FillDestinations(_destination);FillDestinations(_groupDestination);Rows();GroupRows();RequestRows();Events();if(!Visible)PopupCenteredClamped(new(760,560));
    }
    private void NamedDesignEntry(){if(FindChild("FleetOpenRefit",true,false) is Button refit)refit.Visible=OpenAntares.Simulation.Rules.HasShipDesigns(_view.RulesVersion);if(FindChild("OpenShipDesigns",true,false) is Button button)button.Visible=OpenAntares.Simulation.Rules.HasShipDesigns(_view.RulesVersion);}
    private void CombatReport(){_combatReport.ShowRoutine=CombatNotificationPreferences.Load(ProjectSettings.GlobalizePath(CombatPreferencesPath)).ShowRoutine;if(FindChild("CombatRoutineDisplay",true,false) is OptionButton picker)picker.Select(_combatReport.ShowRoutine?0:1);_combatReport.SetView(_view.Combat);}
    private void CombatOrder(FleetId fleet)
    {
        if(_view.Combat is not {} combat)return;var existing=combat.Encounters.SingleOrDefault(e=>e.Fleet==fleet);
        if(existing is not null){CombatRequested?.Invoke(new CombatAction.ChangeCombatOrders(existing.Id,existing.Revision,existing.Orders,existing.CivilianRiskAuthorized));return;}
        var group=_view.Fleets!.Fleets.Single(f=>f.Id==fleet);var station=_view.Vessels.Single(v=>v.Id==group.Flagship).StationaryStarId;var pocket=combat.Pockets.FirstOrDefault(p=>p.Star==station&&p.Current&&!p.Cleared);
        if(pocket is null){_error.Text="Select a stationary own fleet at a currently observed active pocket. Arrival and movement never start combat.";return;}
        CombatRequested?.Invoke(new CombatAction.AttackPocket(fleet,pocket.Id,new(CombatDoctrine.Cautious,default,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair),false));
    }
    private void FillRoles()
    {
        string old=_roleFilter.ItemCount>0?_roleFilter.GetSelectedMetadata().AsString():"";_roleFilter.Clear();_roleFilter.AddItem("All ship roles");_roleFilter.SetItemMetadata(0,"");
        foreach(string role in _view.Fleets!.Ships.Select(s=>s.RoleId).Distinct().OrderBy(id=>_content.Vessels[id].Name,StringComparer.Ordinal).ThenBy(id=>id,StringComparer.Ordinal)){_roleFilter.AddItem(_content.Vessels[role].Name);_roleFilter.SetItemMetadata(_roleFilter.ItemCount-1,role);if(role==old)_roleFilter.Select(_roleFilter.ItemCount-1);}
    }
    private void FillDestinations(OptionButton picker){int old=picker.ItemCount>0?picker.GetSelectedMetadata().AsInt32():0;picker.Clear();foreach(var star in _view.Stars.OrderBy(s=>s.Chart.Id)){picker.AddItem($"{star.Chart.Name} — {(star.LastObservedTurn is {} t?$"survey turn {t}":"unsurveyed chart")}");picker.SetItemMetadata(picker.ItemCount-1,star.Chart.Id.Value);if(star.Chart.Id.Value==old)picker.Select(picker.ItemCount-1);}}
    private static StarId? Target(OptionButton picker)=>picker.ItemCount==0?null:new(picker.GetSelectedMetadata().AsInt32());
    private ImmutableArray<VesselId> Ships()=>_ships.GetSelectedItems().Select(i=>_shipRows[i]).OrderBy(id=>id).ToImmutableArray();
    private ImmutableArray<FleetId> Groups()=>_fleets.GetSelectedItems().Select(i=>_fleetRows[i]).OrderBy(id=>id).ToImmutableArray();
    private void SingleGroup(Action<FleetId> action){var groups=Groups();if(groups.Length==1)action(groups[0]);else _error.Text="Select exactly one fleet.";}
    private bool Attention(VesselId id)=>_view.Combat?.Notices.Any(n=>n.Vessel==id&&n.Level!=CombatNoticeLevel.Summary)==true||_view.Scouting?.Vessels.Any(s=>s.Id==id&&(s.Stage is not null||s.Blocker is not null||s.Held))==true||_view.Fleets!.Fleets.Any(f=>f.Attention is not null&&f.Members.Contains(id))||_view.Fleets.Requests.Any(r=>FleetQueries.IsActive(r.Status)&&r.Slots.Any(s=>s.Vessel==id&&(s.Status==SlotStatus.BlockedSafety||s.BlockedReason is not null)));
    private string Location(FleetShipView ship)=>ship.Transit is {} t?$"Travelling to {Star(t.Destination)}, {t.RemainingTurns} turns":Star(ship.Station!.Value);
    private string Star(StarId id)=>_view.Stars.FirstOrDefault(s=>s.Chart.Id==id)?.Chart.Name??id.ToString();
    private void Rows()
    {
        var selected=Ships().ToHashSet();_ships.Clear();_shipRows.Clear();var rows=_view.Fleets!.Ships.Where(s=>_filter.Selected switch{1=>s.Transit is null&&s.PendingMove is null&&_view.Vessels.Single(v=>v.Id==s.Id).PendingFounding is null,2=>s.Fleet is null&&s.Request is null,3=>s.NewlyCompleted,4=>s.Transit is not null,5=>s.Request is not null,6=>Attention(s.Id),7=>_content.Combat!.RoleProfiles[s.RoleId].Attack>0,8=>_view.Combat!.Encounters.Any(e=>e.Members.Contains(s.Id)),9=>_view.Combat!.Ships.Any(v=>v.Id==s.Id&&v.Status is CombatShipStatus.WithdrawalQueued or CombatShipStatus.ReturnOrdered or CombatShipStatus.Returning),10=>_view.Combat!.Ships.Any(v=>v.Id==s.Id&&v.Hull<v.MaxHull),11=>_view.Combat!.Ships.Any(v=>v.Id==s.Id&&v.DamageBand==CombatDamageBand.Crippled),12=>_view.Combat!.Ships.Any(v=>v.Id==s.Id&&v.Status is CombatShipStatus.Repairing or CombatShipStatus.RepairBlocked),_=>true});
        string role=_roleFilter.GetSelectedMetadata().AsString();if(role.Length>0)rows=rows.Where(s=>s.RoleId==role);
        if(_quickRole>0&&_content.Combat is {} profiles)rows=rows.Where(s=>profiles.RoleProfiles.TryGetValue(s.RoleId,out var profile)&&(_quickRole==1?profile.Attack>0:profile.Attack==0));
        rows=_sort.Selected switch{1=>rows.OrderBy(s=>_content.Vessels[s.RoleId].Name,StringComparer.Ordinal).ThenBy(s=>s.Id),2=>rows.OrderBy(Location,StringComparer.Ordinal).ThenBy(s=>s.Id),_=>rows.OrderBy(s=>s.Id)};
        foreach(var ship in rows){_ships.BeginGroup(GroupKey(ship),Location(ship)+" / "+_content.Vessels[ship.RoleId].Name+(ship.Fleet is {} grouped?$" / fleet {grouped.Value}":""));_shipRows.Add(ship.Id);string text=$"{(ship.Design is {} design?ShipDesignUi.DesignName(_content,_view,design):_content.Vessels[ship.RoleId].Name)} #{ship.Id.Value} | {Location(ship)} | {(ship.Fleet is {} f?$"fleet {f.Value}":"independent")}{(ship.Request is {} r?$" / request {r.Value}":"")}{(ship.NewlyCompleted?" / NEW":"")}{(Attention(ship.Id)?" / ATTENTION":"")}";if(_view.Combat?.Ships.FirstOrDefault(s=>s.Id==ship.Id) is {} combat)text+=$" | {CombatReportPanel.Status(combat)} | hull {combat.Hull}/{combat.MaxHull}, cohesion {combat.Cohesion}, {combat.DamageBand}, speed {combat.EffectiveSpeed}";_ships.AddItem(text);_ships.SetItemMetadata(_ships.ItemCount-1,ship.Id.Value);_ships.SetItemTooltip(_ships.ItemCount-1,text);if(selected.Contains(ship.Id))_ships.Select(_ships.ItemCount-1,_ships.GetSelectedItems().Length==0);}Details();Callable.From(()=>{if(IsInstanceValid(_ships)&&_ships.IsInsideTree()&&_ships.GetSelectedItems().Length>0)_ships.EnsureCurrentIsVisible();}).CallDeferred();
    }
    private string GroupKey(FleetShipView ship)
    {
        var own = _view.Vessels.Single(v=>v.Id==ship.Id);
        var combat = _view.Combat?.Ships.FirstOrDefault(v=>v.Id==ship.Id);
        // Explicit order commitments, cargo, damage and identities must never disappear into a stack.
        var combatFacts = combat is null ? null : new { combat.Hull, combat.Cohesion, combat.ReturnColony, combat.OnArrival, combat.Repairing, combat.MaxHull, combat.DamageBand, combat.Firepower, combat.Armor, combat.EffectiveSpeed, combat.Status, combat.RepairIntervals, combat.HullLost, combat.HullRepaired };
        var health = own.Crisis;
        var healthFacts = health is null ? null : new { health.RoleId, health.Population, health.CarrierEpisode, health.InspectionDueTurn, health.Station, health.Transit, health.PendingMove, health.PendingFounding, health.PortClosed, health.Held, health.CanDecontaminate, health.ContactRisks };
        return System.Text.Json.JsonSerializer.Serialize(new { ship.Station, ship.RoleId, ship.Design, ship.Fleet, ship.Request, ship.PendingMove, ship.Transit, ship.ApproachAcceptance, own.PendingFounding, own.CarriedPopulation, healthFacts, combatFacts });
    }
    private void Details(){var ids=Ships();_detail.Text=$"{ids.Length} selected; {_shipRows.Count} shown / {_view.Fleets!.Ships.Length} owned. Named/request ships need explicit detachment for incompatible individual orders. New ships remain independent until you assign them.";if(_view.Combat is {} combat)_detail.Text+="\n"+string.Join("\n",combat.Ships.Where(s=>ids.Contains(s.Id)).Select(s=>$"Ship {s.Id.Value}: {CombatReportPanel.Status(s)}; hull {s.Hull}/{s.MaxHull}, cohesion {s.Cohesion}, {s.DamageBand}; attack {s.Firepower}, armor {s.Armor}, speed {s.EffectiveSpeed}; repair {s.RepairIntervals} eligible interval(s)."));}
    private void GroupRows(){var selected=Groups().ToHashSet();_fleets.Clear();_fleetRows.Clear();foreach(var f in _view.Fleets!.Fleets.OrderBy(f=>f.Id)){_fleetRows.Add(f.Id);_fleets.AddItem($"{f.Name} [{f.Id.Value}] — {f.Members.Length} ships, {f.Order.Kind}, flagship {f.Flagship.Value}");if(selected.Contains(f.Id))_fleets.Select(_fleets.ItemCount-1,_fleets.GetSelectedItems().Length==0);}GroupDetails();}
    private void GroupDetails(){_members.Clear();_memberRows.Clear();_flagship.Clear();var ids=Groups();_groupDetail.Text=$"{ids.Length} selected fleet(s). Mixed-speed groups travel at their slowest effective member speed; launched transit is committed.";if(ids.Length!=1)return;var f=_view.Fleets!.Fleets.Single(f=>f.Id==ids[0]);_groupDetail.Text+=$" {f.Attention}";foreach(var id in f.Members){_memberRows.Add(id);_members.AddItem($"{_content.Vessels[_view.Fleets.Ships.Single(s=>s.Id==id).RoleId].Name} #{id.Value}");_flagship.AddItem($"Flagship #{id.Value}");_flagship.SetItemMetadata(_flagship.ItemCount-1,id.Value);if(id==f.Flagship)_flagship.Select(_flagship.ItemCount-1);}}
    private void RequestRows(){int old=_requests.GetSelectedItems().FirstOrDefault(-1);RequestId? selected=old>=0?_requestRows[old]:null;_requests.Clear();_requestRows.Clear();foreach(var r in VisiblePage(_view.Fleets!.Requests.OrderBy(r=>r.Id),ref _requestPage,32,_requestPages)){_requestRows.Add(r.Id);_requests.AddItem($"{r.Name} [{r.Id.Value}] — {r.Status}, {r.Priority}, {r.Slots.Count(s=>s.Status==SlotStatus.Arrived)}/{r.Slots.Length} arrived");if(r.Id==selected)_requests.Select(_requests.ItemCount-1);}RequestDetails();}
    private void RequestDetails(){int i=_requests.GetSelectedItems().FirstOrDefault(-1);_requestDetail.Text=i<0?"Finite quotas never refill a missing or lost ship automatically.":_view.Fleets!.Requests.Single(r=>r.Id==_requestRows[i]).Attention??"Review slots, yard authority, rally and conditional estimates before changing this request.";}
    private void Events(){DevelopmentUi.Clear(_events);bool routine=FleetNotificationPreferences.Load(ProjectSettings.GlobalizePath(PreferencesPath)).ShowRoutine;foreach(var e in VisiblePage(_view.Fleets!.Events.Where(e=>routine||e.Importance==FleetImportance.Attention).OrderByDescending(e=>e.Id),ref _eventPage,100,_eventPages))_events.AddChild(DevelopmentUi.Text($"Turn {e.Turn}: {e.Kind.Replace('_',' ')}. {e.Reason}",e.Importance==FleetImportance.Attention?"WarningLabel":"SubtleLabel"));}
    private void Review(FleetAction action)
    {
        var preview=FleetQueries.PreviewAction(_content,_view,action);_error.Text=preview.Rejection??"";if(!preview.Allowed)return;_pending=new FleetCommand(_view.Viewer,_view.Turn,action){Fingerprint=preview.Fingerprint};
        _confirmationText.Text=preview.Consequences+"\n"+string.Join("\n",preview.Routes.Select(r=>$"Ship {r.Vessel.Value}: {Star(r.Origin)} → {Star(r.Destination)}, {r.Turns} turn(s)."))+(preview.Detachments.IsEmpty?"":"\nExplicit detachments: "+string.Join(", ",preview.Detachments.Select(id=>id.Value)))+"\nChanged same-turn facts require a new preview. No founding, study or risk acceptance is implied.";_confirm.PopupCenteredClamped(new(660,420));
    }
    public void ShowError(string text)=>_error.Text=text;
}
