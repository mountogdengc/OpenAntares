using System;

using System.Collections.Generic;

using System.Collections.Immutable;

using System.Linq;

using Godot;

using OpenAntares.Simulation.Commands;

using OpenAntares.Simulation.Content;

using OpenAntares.Simulation.Development;

using OpenAntares.Simulation.Knowledge;

using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Components;

public partial class ConstructionQueueEditor:ConfirmationDialog

{

    public event Action<Command>? CommandIssued;

    private readonly ItemList _list=new(){Name="QueueEntries",CustomMinimumSize=new(0,160)};

    private readonly OptionButton _picker=new(){Name="QueueTargetPicker",SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};

    private readonly CheckBox _replace=new(){Name="ReplaceQueuedActive",Text="Confirm replacing active queued work (keep reserve)"};

    private readonly Label _bank=DevelopmentUi.Text("");
    private readonly Label _error=DevelopmentUi.Text("","WarningLabel");private readonly List<QueueEntryDraft> _entries=new();private ContentSet? _content;private EmpireView? _view;private OwnedColonyView? _colony;

    public override void _Ready(){Title="Construction queue";OkButtonText="Save queue";DialogHideOnOk=false;var body=DevelopmentUi.DialogBody(this);body.AddChild(DevelopmentUi.Text("Finite player orders. Locked projects wait while later legal work can proceed. Governors never add ships."));body.AddChild(_bank);body.AddChild(_list);body.AddChild(_picker);var row=new HBoxContainer();body.AddChild(row);DevelopmentUi.Button(row,"Add project",Add,"AddQueueEntry");DevelopmentUi.Button(row,"Remove selected",()=>{int i=_list.GetSelectedItems().FirstOrDefault(-1);if(i>=0){_entries.RemoveAt(i);Refresh();}});DevelopmentUi.Button(row,"Move up",()=>Move(-1));DevelopmentUi.Button(row,"Move down",()=>Move(1));body.AddChild(_replace);body.AddChild(_error);Confirmed+=Save;}

    public void Open(ContentSet content,EmpireView view,OwnedColonyView colony){_content=content;_view=view;_colony=colony;_bank.Text=$"Bank {Format.Points(colony.ProductionReserve)}; diplomacy reserved {Format.Points(colony.ReservedProduction)}; spendable {Format.Points(colony.SpendableProduction)}."+(colony.ProductionIfSetupReleased is null?"":" Current-project completion may improve if the setup reservation is released; this is conditional on the offer ending without activation.");_entries.Clear();_entries.AddRange(colony.Development!.Queue.Select(e=>new QueueEntryDraft(e.Token,e.Target)));_picker.Clear();foreach(var b in content.Buildings.Values.Where(b=>!colony.CompletedBuildingIds.Contains(b.Id))){_picker.AddItem(b.Name);_picker.SetItemMetadata(_picker.ItemCount-1,"building|"+b.Id);}foreach(string id in view.AvailableVesselRoleIds.Where(id=>!ShipDesignUi.UsesDesigns(content,view,id))){_picker.AddItem(content.Vessels[id].Name+" (one vessel)");_picker.SetItemMetadata(_picker.ItemCount-1,"vessel|"+id);}foreach(var design in OpenAntares.Simulation.ShipDesigns.ShipDesignQueries.BuildChoices(view)){_picker.AddItem(ShipDesignUi.DesignName(content,view,design.Id)+" ("+Format.Points(design.Stats.BuildCost)+"; one ship)");_picker.SetItemMetadata(_picker.ItemCount-1,ShipDesignUi.Encode(OpenAntares.Simulation.ShipDesigns.ShipDesignTargets.Build(design.Id)));}_replace.ButtonPressed=false;_error.Text="";Refresh();PopupCenteredClamped(new(660,500));}

    private void Add(){if(_entries.Count>=32){_error.Text="A queue holds at most32 projects.";return;}if(_picker.Selected<0)return;var target=ShipDesignUi.Decode(_picker.GetSelectedMetadata().AsString());if(target.Kind=="building"&&_entries.Any(e=>e.Target==target)){_error.Text="That building is already queued.";return;}_entries.Add(new(null,target));Refresh();}

    private void Move(int delta){int i=_list.GetSelectedItems().FirstOrDefault(-1),j=i+delta;if(i<0||j<0||j>=_entries.Count)return;(_entries[i],_entries[j])=(_entries[j],_entries[i]);Refresh();_list.Select(j);}

    private void Refresh(){_list.Clear();foreach(var e in _entries){bool building=e.Target.Kind=="building";string name=ShipDesignUi.Name(_content!,_view,e.Target);string needs=building?string.Join(", ",_content!.Buildings[e.Target.ContentId].PrerequisiteTechnologyIds.Where(id=>!_view!.Research.KnownTechnologyIds.Contains(id)).Select(id=>_content.Technologies[id].Name)):"";_list.AddItem(name+(needs.Length>0?" — needs "+needs:"")+(e.Token==_colony!.Development!.ActiveQueueToken?" (active)":""));}}

    private void Save(){if(_view is null||_colony is null)return;var active=_colony.Development!.ActiveQueueToken;if(active is {} token&&!_entries.Any(e=>e.Token==token)&&!_replace.ButtonPressed){_error.Text="Active queued work would be removed. Confirm replacement or keep its entry.";return;}CommandIssued?.Invoke(new SetColonyQueueCommand(_view.Viewer,_view.Turn,_colony.Id,_entries.ToImmutableArray(),_replace.ButtonPressed));Hide();}

}
