using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class ConflictOrdersDialog:AcceptDialog
{
    public const string DefenseExplanation="Armed vessels defend automatically. Civilians in mixed defending fleets remain protected; no civilian combat opt-in is required. New and ungrouped warships begin with Cautious defense and local Break contact.";
    public const string LocalFallbackExplanation="Local Break contact stays in the same system on Hold, without healing or immunity. A closed, lost or same-system return port also falls back locally. A single withdrawal can take one enemy-only interception volley; equal speed is exposed.";
    public event Action<Command>? CommandIssued;
    private ContentSet? _content;private EmpireView? _view;private ConflictCommand? _pending;
    private readonly OptionButton _force=new(){Name="ConflictOwnForce"},_target=new(){Name="ConflictArmedTarget"},_port=new(){Name="ConflictReturnPort"},_doctrine=new(){Name="ConflictDoctrine"},_stragglers=new(){Name="ConflictStragglers"},_arrival=new(){Name="ConflictArrival"};
    private readonly List<VesselId> _anchors=new(),_targets=new();private readonly List<ColonyId?> _ports=new();
    private readonly Label _own=DevelopmentUi.Text(""),_foreign=DevelopmentUi.Text(""),_details=DevelopmentUi.Text(""),_error=DevelopmentUi.Text("","WarningLabel");
    private Button _apply=null!;private readonly Dictionary<string,(Button Button,Func<ConflictAction?> Action)> _actions=new();
    public static string ForeignDetails(MilitaryObservation o)=>$"Observed armed ship {o.Vessel.Value}, turn {o.Turn}{(o.Current?"":"; stale")}: hull {o.Hull}/{o.MaxHull}, attack {o.Attack}, armor {o.Armor}, public speed {o.Speed}. Equipment: frame {o.FrameId??"unknown"}, engine {o.EngineId??"unknown"}, armor {o.ArmorId??"unknown"}, banks {string.Join(", ",o.WeaponBanks)}. Enemy orders, intentions and future withdrawals are unknown.";
    public override void _Ready()
    {
        Name="ConflictOrdersDialog";Title="Empire conflict orders";ForceNative=true;OkButtonText="Close";var body=DevelopmentUi.DialogBody(this);
        body.AddChild(DevelopmentUi.Text(DefenseExplanation));body.AddChild(DevelopmentUi.Text(LocalFallbackExplanation));
        body.AddChild(DevelopmentUi.Text("Your armed force"));body.AddChild(_force);_own.Name="ConflictOwnDetails";body.AddChild(_own);
        body.AddChild(DevelopmentUi.Text("Armed wartime target — observed in this system"));body.AddChild(_target);_foreign.Name="ConflictForeignDetails";body.AddChild(_foreign);
        body.AddChild(DevelopmentUi.Text("Doctrine"));body.AddChild(_doctrine);foreach(var s in new[]{"Cautious","Press","Hit and run","Hold despite losses"})_doctrine.AddItem(s);
        body.AddChild(DevelopmentUi.Text("Owned return port — an attack requires a legal port in another system"));body.AddChild(_port);
        body.AddChild(DevelopmentUi.Text("Stragglers"));body.AddChild(_stragglers);_stragglers.AddItem("Keep together");_stragglers.AddItem("Detach crippled non-flagships");
        body.AddChild(DevelopmentUi.Text("On arrival"));body.AddChild(_arrival);_arrival.AddItem("Repair");_arrival.AddItem("Hold");
        var actions=new GridContainer{Columns=2};body.AddChild(actions);
        Add(actions,"Review defensive preferences","ConflictReviewDefense",()=>Ship() is {} s?new ConflictAction.ConfigureDefense(s.Id,Orders()):null);
        Add(actions,"Review attack","ConflictReviewAttack",()=>Ship()?.Fleet is {} f&&_target.Selected>=0&&_target.Selected<_targets.Count?new ConflictAction.Attack(f,_targets[_target.Selected],Orders()):null);
        Add(actions,"Review cancel pending attack","ConflictCancelAttack",()=>Ship()?.Fleet is {} f?new ConflictAction.CancelAttack(f):null);
        Add(actions,"Review changed battle orders","ConflictReviewChange",()=>Encounter() is {} e?new ConflictAction.ChangeOrders(e.Id,e.Revision,Orders()):null);
        Add(actions,"Review withdrawal","ConflictReviewWithdraw",()=>Encounter() is {} e?new ConflictAction.Withdraw(e.Id,e.Revision):null);
        Add(actions,"Review revised force response","ConflictReviewRespond",()=>Encounter() is {} e?new ConflictAction.Respond(e.Id,e.Revision):null);
        Add(actions,"Review blockade","ConflictReviewBlockade",()=>Ship()?.Fleet is {} f&&_view?.Conflict?.Relations.SingleOrDefault(r=>r.Relation==RelationKind.War) is {} r?new ConflictAction.Blockade(f,r.Other):null);
        Add(actions,"Review lift blockade","ConflictReviewLift",()=>Ship()?.Fleet is {} f?new ConflictAction.LiftBlockade(f):null);
        _details.Name="ConflictOrderPreview";_error.Name="ConflictOrderError";body.AddChild(_details);body.AddChild(_error);
        _apply=DevelopmentUi.Button(body,"Apply reviewed conflict command",Submit,"ConflictOrdersApply");_apply.Disabled=true;
        _force.ItemSelected+=_=>SelectForce();_target.ItemSelected+=_=>{Invalidate();Foreign();};foreach(var picker in new[]{_port,_doctrine,_stragglers,_arrival})picker.ItemSelected+=_=>Invalidate();
        VisibilityChanged+=()=>{if(!Visible&&_apply is not null)Invalidate();};
    }
    private void Add(GridContainer parent,string label,string id,Func<ConflictAction?> action)
    {var button=DevelopmentUi.Button(parent,label,()=>Review(action()),id);_actions.Add(id,(button,action));}
    public void Open(ContentSet content,EmpireView view,VesselId? anchor=null)
    {
        _content=content;_view=view;_anchors.Clear();_force.Clear();
        foreach(var ship in view.Conflict?.Ships.Where(s=>s.Military).OrderBy(s=>s.Id)??Enumerable.Empty<OwnConflictShipView>())
        {if(ship.Fleet is {} f&&_anchors.Any(id=>view.Conflict!.Ships.Single(s=>s.Id==id).Fleet==f))continue;_anchors.Add(ship.Id);_force.AddItem(ship.Fleet is {} group?$"Fleet {group.Value} — ship {ship.Id.Value}":$"Ungrouped ship {ship.Id.Value}");_force.SetItemMetadata(_force.ItemCount-1,ship.Id.Value);}
        if(anchor is {} selected&&view.Conflict?.Ships.SingleOrDefault(s=>s.Id==selected) is {} chosen){int i=_anchors.FindIndex(id=>id==selected||chosen.Fleet is {} f&&view.Conflict.Ships.Single(s=>s.Id==id).Fleet==f);if(i>=0)_force.Select(i);}
        SelectForce();PopupCenteredClamped(new(700,560));
    }
    public void UpdateView(EmpireView view)
    {
        if(_view?.Viewer!=view.Viewer){Hide();return;}_view=view;
        if(_pending is {} pending&&(!ConflictQueries.Preview(_content!,view,pending.Action).Allowed||ConflictQueries.Preview(_content!,view,pending.Action).Token!=pending.Token)){Invalidate();_error.Text="The reviewed situation changed. Reopen and review a fresh command.";}
        if(Ship() is null){Invalidate();_own.Text="This force is no longer available. Reopen to select your current ships.";}RefreshActions();
    }
    private OwnConflictShipView? Ship()=>_force.Selected>=0&&_force.Selected<_anchors.Count?_view?.Conflict?.Ships.SingleOrDefault(s=>s.Id==_anchors[_force.Selected]):null;
    private OwnConflictEncounterView? Encounter()=>Ship() is {} s?_view?.Conflict?.Encounters.SingleOrDefault(e=>e.OwnMembers.Contains(s.Id)):null;
    private ConflictOrders Orders()=>new((CombatDoctrine)_doctrine.Selected,_port.Selected>=0&&_port.Selected<_ports.Count?_ports[_port.Selected]:null,(StragglerPolicy)_stragglers.Selected,(CombatArrivalOrder)_arrival.Selected);
    private void SelectForce()
    {
        Invalidate();_target.Clear();_targets.Clear();_port.Clear();_ports.Clear();_port.AddItem("Local Break contact — same system");_ports.Add(null);_port.SetItemMetadata(0,0);
        var ship=Ship();var encounter=Encounter();var orders=encounter?.OwnOrders??_view?.Conflict?.OwnAttacks.SingleOrDefault(a=>a.Fleet==ship?.Fleet)?.Orders??ship?.Defense??ConflictOrders.Default;
        foreach(var colony in _view?.Colonies.OrderBy(c=>c.Id)??Enumerable.Empty<OwnedColonyView>())
        {_ports.Add(colony.Id);_port.AddItem($"{_view!.Galaxy.Stars.Single(s=>s.Id==colony.StarId).Name} — colony {colony.Id.Value}");_port.SetItemMetadata(_port.ItemCount-1,colony.Id.Value);if(colony.Id==orders.ReturnColony)_port.Select(_ports.Count-1);}
        _doctrine.Select((int)orders.Doctrine);_stragglers.Select((int)orders.Stragglers);_arrival.Select((int)orders.OnArrival);
        foreach(var target in _view?.Conflict?.Military.Where(m=>m.Current&&m.Turn==_view.Turn&&m.Star==ship?.Star&&_view.Conflict.Relations.Any(r=>r.Other==m.Owner&&r.Relation==RelationKind.War)).OrderBy(m=>m.Vessel)??Enumerable.Empty<MilitaryObservation>())
        {_targets.Add(target.Vessel);_target.AddItem($"Armed ship {target.Vessel.Value} — empire {target.Owner.Value}");_target.SetItemMetadata(_target.ItemCount-1,target.Vessel.Value);}
        _own.Text=ship is null?"No armed force is available.":$"Your ship {ship.Id.Value}: hull {ship.Hull}/{ship.MaxHull}, cohesion {ship.Cohesion}, attack {ship.Attack}, armor {ship.Armor}, effective speed {ship.Speed}."+(encounter is null?"":$"\nBattle round {encounter.Round}; {encounter.ProtectedCivilians.Length} protected civilian(s). {(encounter.OwnNeedsResponse?"Your revised force requires an explicit response before Ready.":encounter.OwnWithdrawQueued?"Your withdrawal is queued.":"Your battle orders are committed.")}");Foreign();RefreshActions();
    }
    private void Foreign(){_foreign.Text=_target.Selected>=0&&_target.Selected<_targets.Count&&_view?.Conflict?.Military.SingleOrDefault(m=>m.Vessel==_targets[_target.Selected]) is {} target?ForeignDetails(target):Encounter() is {} e?string.Join("\n",e.Opposing.Select(ForeignDetails)):"No current armed wartime target is observed here. Movement and opening this window never attack.";}
    private void RefreshActions(){foreach(var item in _actions.Values){var a=item.Action();bool allowed=_content is not null&&_view is not null&&a is not null&&ConflictQueries.Preview(_content,_view,a).Allowed;item.Button.Disabled=!allowed;item.Button.TooltipText=allowed?"Review consequences before applying.":"Requires a current eligible force, battle revision, wartime target or legal return port. Repairing, travelling, new and reserved fleets cannot initiate combat.";}}
    private void Invalidate(){_pending=null;if(_apply is not null)_apply.Disabled=true;_details.Text="Review a command before applying. Changing a field invalidates its review.";if(_view is not null)RefreshActions();}
    private void Review(ConflictAction? action)
    {
        Invalidate();if(action is null||_content is null||_view is null)return;var preview=ConflictQueries.Preview(_content,_view,action);_error.Text=preview.Allowed?"":"Command unavailable. Check the selected force, current battle revision, target and return port.";if(!preview.Allowed)return;
        var lines=new List<string>(preview.Explanations){"One automatic exchange at most per strategic turn. Observed targets and public equipment may change before resolution. Enemy withdrawal, orders and officer effects are unknown."};
        if(preview.Volley is {} volley){lines.Add(volley.Explanation);foreach(var shot in volley.Shots)lines.Add($"Conditional ship {shot.Source.Id} → armed ship {shot.Target.Id}: {shot.Damage} damage after damage band and armor; frozen target, no spill. Enemy withdrawal and command effects can change whether this exchange occurs.");foreach(var row in volley.Own)lines.Add($"Your ship {row.Id.Value}: conditional hull {row.Before} → {row.After}.");foreach(var row in volley.Opposing)lines.Add($"Observed armed ship {row.Id.Value}: conditional hull {row.Before} → {row.After}.");}
        if(preview.Withdrawal is {} withdrawal){lines.Add(withdrawal.Explanation);lines.Add(withdrawal.Destination is {} destination?$"Conditional return toward {_view.Galaxy.Stars.Single(s=>s.Id==destination).Name}; finite repair starts after arrival, without an arrival healing tick.":LocalFallbackExplanation);foreach(var cohort in withdrawal.Cohorts)lines.Add($"Your cohort [{string.Join(", ",cohort.Members.Select(v=>v.Value))}]: speed {cohort.Speed}. Exposure depends on observed pursuit and changed forces at resolution.");}
        if(action is ConflictAction.Attack or ConflictAction.ChangeOrders or ConflictAction.ConfigureDefense)lines.Add($"Your reviewed orders: {_doctrine.GetItemText(_doctrine.Selected)}; {_port.GetItemText(_port.Selected)}; {_stragglers.GetItemText(_stragglers.Selected)}; on arrival {_arrival.GetItemText(_arrival.Selected)}.");
        if(action is ConflictAction.Respond)lines.Add("Accept only your currently displayed revised force and committed orders. No other player's response is implied.");
        if(action is ConflictAction.Withdraw)lines.Add("Withdrawal uses your committed battle orders. Apply changed orders first if you want a different port or straggler policy.");
        if(action is ConflictAction.Blockade)lines.Add("Production pressure can begin after the next publication. Any stationary enemy armed vessel can contest it. Moving, repairing, refitting or regrouping lifts the order.");
        _details.Text=string.Join("\n",lines);_pending=new(_view.Viewer,_view.Turn,action,preview.Token);_apply.Disabled=false;
    }
    private void Submit(){if(_pending is not {} command)return;_pending=null;_apply.Disabled=true;CommandIssued?.Invoke(command);}
    public void ShowResult(string error,bool accepted){if(accepted)Hide();else{Invalidate();_error.Text=error;}}
}
