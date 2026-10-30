using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.ShipDesigns;
namespace OpenAntares.Game.Components;
public partial class ShipDesignDialog:AcceptDialog
{
    public event Action<ShipDesignAction>? ActionRequested;
    public event Action<ShipDesign?>? EditorRequested;
    private ContentSet _content=null!;private EmpireView _view=null!;private int _page;private DesignId? _selected;
    private readonly ItemList _list=new(){Name="DesignCatalogue",CustomMinimumSize=new(0,170)};
    private readonly CheckBox _archived=new(){Name="DesignIncludeArchived",Text="Include archived"};
    private readonly LineEdit _search=new(){Name="DesignSearch",PlaceholderText="Filter by design name"};
    private readonly Label _pages=new(){Name="DesignPageLabel"},_details=DevelopmentUi.Text("");private CataloguePage _rows=null!;
    private readonly Label _error=DevelopmentUi.Text("","WarningLabel");
    private Button _archive=null!,_restore=null!,_delete=null!;
    public ShipDesignDialog() { }
    public ShipDesignDialog(ContentSet content,EmpireView view){_content=content;_view=view;}
    public override void _Ready()
    {
        ForceNative=true;Name="ShipDesignDialog";Title="Ship designs";OkButtonText="Close";var body=DevelopmentUi.DialogBody(this);body.AddChild(DevelopmentUi.Text("Your catalogue: at most 32 active and 256 retained designs. Archive stops new choices; existing commitments retain their exact recipes."));body.AddChild(_search);body.AddChild(_archived);body.AddChild(_list);
        var pages=new HBoxContainer();body.AddChild(pages);DevelopmentUi.Button(pages,"Previous",()=>{_page--;Rows();},"DesignPagePrevious");pages.AddChild(_pages);DevelopmentUi.Button(pages,"Next",()=>{_page++;Rows();},"DesignPageNext");_details.Name="DesignDetails";body.AddChild(_details);
        var actions=new GridContainer{Columns=2};body.AddChild(actions);DevelopmentUi.Button(actions,"New design",()=>EditorRequested?.Invoke(null),"NewDesign");DevelopmentUi.Button(actions,"Copy selected",()=>{var d=Selected();if(d is not null)EditorRequested?.Invoke(new(d.Id,d.Name,d.Recipe,d.Archived));},"CopyDesign");
        _archive=DevelopmentUi.Button(actions,"Archive",()=>Act(id=>new ShipDesignAction.Archive(id)),"ArchiveDesign");_restore=DevelopmentUi.Button(actions,"Restore",()=>Act(id=>new ShipDesignAction.Restore(id)),"RestoreDesign");_delete=DevelopmentUi.Button(actions,"Delete archived design",()=>Act(id=>new ShipDesignAction.Delete(id)),"DeleteDesign");
        _error.Name="CatalogueError";body.AddChild(_error);
        _list.ItemSelected+=_=>{_selected=Selected()?.Id;Details();};_search.TextChanged+=_=>{_page=0;Rows();};_archived.Toggled+=_=>{_page=0;Rows();};if(_content is not null)ShowCatalogue(_content,_view);
    }
    public void ShowCatalogue(ContentSet content,EmpireView view){_content=content;_view=view;Rows();PopupCenteredClamped(new(720,560));}
    public void ShowError(string error)=>_error.Text=error;
    private DesignView? Selected(){int i=_list.GetSelectedItems().FirstOrDefault(-1);return i>=0&&i<_rows.Rows.Length?_rows.Rows[i].Design:null;}
    private void Act(Func<DesignId,ShipDesignAction> action){if(Selected() is {} d)ActionRequested?.Invoke(action(d.Id));}
    private void Rows()
    {
        _rows=ShipDesignQueries.CataloguePage(_view,_page,new(_archived.ButtonPressed,_search.Text));_page=_rows.PageIndex;_list.Clear();foreach(var row in _rows.Rows){var d=row.Design;_list.AddItem(d.Name+(d.Archived?" (archived)":""));if(d.Id==_selected)_list.Select(_list.ItemCount-1);}if(_list.GetSelectedItems().Length==0&&_list.ItemCount>0)_list.Select(0);_selected=Selected()?.Id;_pages.Text=$"Page {_page+1}/{_rows.PageCount}; {_rows.Total} designs";Details();
    }
    private void Details()
    {
        var d=Selected();var row=_rows.Rows.FirstOrDefault(r=>r.Design.Id==d?.Id);_archive.Disabled=row?.ArchiveBlockedReason is not null||row is null;_restore.Disabled=row?.RestoreBlockedReason is not null||row is null;_delete.Disabled=row?.DeleteBlockedReason is not null||row is null;_archive.TooltipText=row?.ArchiveBlockedReason??"Archive for new choices";_restore.TooltipText=row?.RestoreBlockedReason??"Restore for new choices";_delete.TooltipText=row?.DeleteBlockedReason??"Delete an unreferenced archived recipe";
        _details.Text=d is null?"No matching designs.":$"{d.Name} — {_content.ShipDesigns!.Frames[d.Recipe.FrameId].Name}\nCost {Format.Points(d.Stats.BuildCost)}; hull {d.Stats.MaxHull}; attack {d.Stats.Attack}; armor {d.Stats.Armor}; speed {d.Stats.BaseSpeed}; space {d.Stats.SpaceUsed}/{d.Stats.SpaceCapacity}\nReferences: {d.References.Vessels} ships, {d.References.ActiveProjects} projects, {d.References.QueueEntries} queue entries, {d.References.RequestSlots} request slots, {d.References.Refits} refits.";
    }
}
