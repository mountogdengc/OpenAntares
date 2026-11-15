using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Components;

public partial class MapFleetSplitDialog : ConfirmationDialog
{
    private readonly LineEdit _name=new(){Name="MapSplitName",Text="Split fleet",MaxLength=80};
    private readonly ItemList _members=new(){Name="MapSplitMembers",SelectMode=ItemList.SelectModeEnum.Multi};
    private readonly Label _preview=DevelopmentUi.Text("");
    private ContentSet? _content;private EmpireView? _view;private FleetId _fleet;private FleetCommand? _pending;
    public event Action<Command>? CommandIssued;
    public override void _Ready()
    {
        Name="MapFleetSplit";Title="Split fleet";OkButtonText="Split fleet";
        var body=DevelopmentUi.DialogBody(this);body.AddChild(DevelopmentUi.Text("Choose the ships for the new fleet. Leave at least one ship in the original fleet."));body.AddChild(_name);body.AddChild(_members);body.AddChild(_preview);
        _members.CustomMinimumSize=new Vector2(0,body.GetThemeConstant("fleet_list_height","ConsoleMetrics"));
        _members.MultiSelected+=(_,_)=>Preview();_name.TextChanged+=_=>Preview();
        Confirmed+=()=>{if(_pending is {} command)CommandIssued?.Invoke(command);};
    }
    public void Open(ContentSet content,EmpireView view,FleetId fleet)
    {
        _content=content;_view=view;_fleet=fleet;_members.Clear();_name.Text="Split fleet";
        foreach(var id in view.Fleets!.Fleets.Single(f=>f.Id==fleet).Members.OrderBy(id=>id))
        {
            var ship=view.Vessels.Single(v=>v.Id==id);_members.AddItem(OpenAntares.Game.Presentation.FleetNaming.Ship(content,view,ship));_members.SetItemMetadata(_members.ItemCount-1,id.Value);
        }
        Preview();PopupCenteredClamped(new Vector2I(_name.GetThemeConstant("fleet_split_width","ConsoleMetrics"),_name.GetThemeConstant("fleet_split_height","ConsoleMetrics")));
    }
    private void Preview()
    {
        if(_view is null||_content is null)return;
        var ids=_members.GetSelectedItems().Select(i=>new OpenAntares.Simulation.VesselId(_members.GetItemMetadata(i).AsInt32())).ToImmutableArray();
        var action=new FleetAction.Split(_fleet,_name.Text,ids);var preview=FleetQueries.PreviewAction(_content,_view,action);
        _pending=preview.Allowed?new FleetCommand(_view.Viewer,_view.Turn,action){Fingerprint=preview.Fingerprint}:null;
        _preview.Text=preview.Rejection??preview.Consequences;GetOkButton().Disabled=!preview.Allowed;
    }
}
