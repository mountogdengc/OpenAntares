using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
namespace OpenAntares.Game.Components;
public partial class CrisisOverviewDialog:AcceptDialog
{
    private VBoxContainer _body=null!;private ContentSet? _content;private CrisisView? _view;private ImmutableArray<CrisisCaseView> _rows=ImmutableArray<CrisisCaseView>.Empty;
    private ItemList _list=null!;private OptionButton _filter=null!;private readonly CrisisAuthorityDialog _authority=new();private bool _all;private System.Collections.Generic.HashSet<ColonyId> _selected=new();
    public event Action<Command>? CommandRequested;
    public event Action<ImmutableArray<ColonyId>,bool,bool>? QuarantineRequested;
    public event Action<ColonyId>? PurgeRequested;public event Action<StarId>? StarRequested;
    public override void _Ready(){Name="CrisisOverview";Title="Colony crises";OkButtonText="Close";_body=DevelopmentUi.DialogBody(this);AddChild(_authority);_authority.CommandRequested+=c=>CommandRequested?.Invoke(c);}
    public void ShowCrisis(ContentSet content,CrisisView view)
    {
        _content=content;_view=view;DevelopmentUi.Clear(_body);
        _body.AddChild(DevelopmentUi.Text(view.Enabled?$"Turn {view.Turn}: {view.Cases.Length} cases, {view.Carriers.Length} carriers/inspection holds. Authority: {view.Authority}; research policy {(view.ResearchPolicyPaused?"paused":"available")}. Crisis acknowledgement {(view.RequireAcknowledgement?"required":"explicitly waived")}.":"Crises are Off for this campaign."));
        if(view.Episode is {} ep){_body.AddChild(DevelopmentUi.Text($"{ep.Name}: source {ep.Source.Name} ({ep.Source.Colony}), source population {ep.SourcePopulation}. Cure {Format.Points(ep.CureProgress)} / {Format.Points(ep.CureCost)} points, frozen at onset."));_body.AddChild(DevelopmentUi.Text($"Next interval: new research diversion {Format.Points(ep.Forecast.Diverted)}, remaining {Format.Points(ep.CureCost-ep.CureProgress-ep.Forecast.Diverted)}; old reserve is untouched. Manual research override: {ep.ResearchOverride}."));}
        _body.AddChild(DevelopmentUi.Text($"Empire civic strain {view.CivicLevel}; expires turn {view.CivicExpiresAt}."));
        if(view.ColonyConditions.IsEmpty)_body.AddChild(DevelopmentUi.Text($"No colonies remain. Surviving founders: {view.Vessels.Count(v=>content.Vessels[v.RoleId].CanFoundColony&&v.Population>0)}. Recover by settling with an existing populated founder; no free population is granted.","WarningLabel"));
        _filter=new(){Name="CrisisFilter"};_filter.AddItem("Current cases");_filter.AddItem("All owned colonies / preventive quarantine");_filter.Select(_all?1:0);_body.AddChild(_filter);
        _list=new(){Name="CrisisCaseList",SelectMode=ItemList.SelectModeEnum.Multi,CustomMinimumSize=new(0,160)};_body.AddChild(_list);_filter.ItemSelected+=i=>{_all=i==1;Rows();};Rows();
        DevelopmentUi.Button(_body,"Select all shown",()=>{for(int i=0;i<_list.ItemCount;i++)_list.Select(i,false);},"CrisisSelectAll");
        DevelopmentUi.Button(_body,"Inspect selected location",()=>{var selected=Selected();if(selected.Length==1){Hide();StarRequested?.Invoke(_rows.Single(r=>r.Location.Colony==selected[0]).Location.Star);}},"CrisisInspect");
        DevelopmentUi.Button(_body,"Preview quarantine until recovery",()=>QuarantineRequested?.Invoke(Selected(),true,true),"CrisisQuarantine");
        DevelopmentUi.Button(_body,"Preview quarantine until ordered",()=>QuarantineRequested?.Invoke(Selected(),true,false),"CrisisQuarantineKeep");
        DevelopmentUi.Button(_body,"Preview reopening",()=>QuarantineRequested?.Invoke(Selected(),false,false),"CrisisReopen");
        DevelopmentUi.Button(_body,"Preview purge of one colony",()=>{var ids=Selected();if(ids.Length==1)PurgeRequested?.Invoke(ids[0]);},"CrisisPurge");
        DevelopmentUi.Button(_body,"Resume selected crisis policy and research",()=>CommandRequested?.Invoke(new ResumeCrisisPolicyCommand(view.Viewer,view.Turn,Selected(),true)),"CrisisResume");
        var shares=new OptionButton{Name="CrisisResearchShare"};_body.AddChild(shares);foreach(int share in new[]{0,25,50,100}){shares.AddItem(share+"% new research for cure");shares.SetItemMetadata(shares.ItemCount-1,share);if(view.Episode?.ResearchShare==share)shares.Select(shares.ItemCount-1);}shares.Disabled=view.Episode is null||!view.Enabled;
        shares.ItemSelected+=i=>{if(view.Episode is {} episode)CommandRequested?.Invoke(new SetCureShareCommand(view.Viewer,view.Turn,episode.Id,shares.GetItemMetadata((int)i).AsInt32()));};
        DevelopmentUi.Button(_body,"Set crisis authority",()=>_authority.ShowAuthority(_view!),"CrisisSetAuthority");
        foreach(var v in view.Carriers){_body.AddChild(DevelopmentUi.Text($"Vessel {v.Id} {content.Vessels[v.RoleId].Name}: cargo {v.Population}, episode {v.CarrierEpisode?.ToString()??"none"}, {(v.Held?"inspection until turn "+v.InspectionDueTurn:v.PortClosed?"owned system port closed":"open port")}."));if(v.CanDecontaminate)DevelopmentUi.Button(_body,"Inspect vessel "+v.Id,()=>CommandRequested?.Invoke(new DecontaminateVesselCommand(view.Viewer,view.Turn,v.Id,ImmutableArray.Create(new VesselOrderFact(v.Id,v.Station,v.Transit,v.PendingMove,v.PendingFounding,v.InspectionDueTurn)))),"CrisisInspectVessel_"+v.Id);}
        foreach(var lost in view.LostColonyLocations)_body.AddChild(DevelopmentUi.Text($"Lost colony {lost.Name} ({lost.Colony}): historical location retained; founding recovery requires an existing populated vessel.","WarningLabel"));
        foreach(var digest in view.History.Reverse().Concat(view.PendingDigests))foreach(var e in digest.Entries)_body.AddChild(DevelopmentUi.Text($"Turn {digest.Turn}, {e.Location.Name} ({e.Location.Colony}): {e.Kind} — {e.Reason}; {e.Disposition}; population lost {e.PopulationLost}, research {Format.Points(e.ResearchDiverted)}."));
        PopupCenteredClamped(new(760,600));
    }
    private void Rows(){_rows=_all?_view!.ColonyConditions:_view!.Cases;_list.Clear();foreach(var c in _rows){string text=$"{c.Location.Name} ({c.Location.Colony}) | {c.Stage?.ToString()??"healthy"} | population {c.Population}, severity {c.Severity}, warning {c.GraceRemaining}, recovery {c.IntervalsUntilRecovery} | {(c.Quarantined?"quarantined":"open")} | {(c.NextIntervalMayLosePopulation?"population loss risk":"no forecast death")} | {(c.PolicyPaused?"policy paused":"policy available")}";_list.AddItem(text);_list.SetItemTooltip(_list.ItemCount-1,text);if(_selected.Contains(c.Location.Colony))_list.Select(_list.ItemCount-1,false);}}
    private ImmutableArray<ColonyId> Selected(){var ids=_list.GetSelectedItems().Select(i=>_rows[i].Location.Colony).ToImmutableArray();_selected=ids.ToHashSet();return ids;}
}
