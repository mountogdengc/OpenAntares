using System;
using System.Linq;
using System.Collections.Immutable;
using Godot;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.ShipDesigns;
namespace OpenAntares.Game.Components;
public partial class ShipDesignEditor:AcceptDialog
{
    public event Action<string,ShipRecipe>? Saved;
    public event Action? CancelRequested;
    private ContentSet _content=null!;private EmpireView _view=null!;
    private readonly LineEdit _name=new(){Name="DesignName",PlaceholderText="Design name (1–40 characters)"};
    private readonly OptionButton _frame=new(){Name="DesignFrame"},_engine=new(){Name="DesignEngine"},_armor=new(){Name="DesignArmor"},_bank1=new(){Name="DesignBank1"},_bank2=new(){Name="DesignBank2"};
    private readonly Label _preview=DevelopmentUi.Text("Choose equipment to see its preview.");
    private readonly Label _error=DevelopmentUi.Text("","WarningLabel");private bool _loading;
    private Button _save=null!;
    public ShipDesignEditor() { }
    public ShipDesignEditor(ContentSet content,EmpireView view,ShipDesign? copy=null){_content=content;_view=view;_copy=copy;}
    private ShipDesign? _copy;
    public override void _Ready()
    {
        ForceNative=true;Name="ShipDesignEditor";Title="Ship designer";OkButtonText="Cancel";GetOkButton().Name="CancelDesign";
        var body=DevelopmentUi.DialogBody(this);body.AddChild(DevelopmentUi.Text("Save a new immutable design. Existing designs and equipped ships keep their recipes."));body.AddChild(_name);
        foreach(var pair in new[]{("Frame",_frame),("Engine",_engine),("Armor",_armor),("Weapon bank 1",_bank1),("Weapon bank 2",_bank2)}){body.AddChild(DevelopmentUi.Text(pair.Item1));body.AddChild(pair.Item2);}
        _preview.Name="DesignPreview";body.AddChild(_preview);_error.Name="DesignError";body.AddChild(_error);_save=DevelopmentUi.Button(body,"Save design",Save,"SaveDesign");
        _name.TextChanged+=_=>Preview();foreach(var picker in new[]{_frame,_engine,_armor,_bank1,_bank2})picker.ItemSelected+=_=>Preview();
        GetOkButton().Pressed+=()=>CancelRequested?.Invoke();CloseRequested+=()=>{Hide();CancelRequested?.Invoke();};
        if(_content is not null)ShowEditor(_content,_view,_copy);
    }
    public void ShowEditor(ContentSet content,EmpireView view,ShipDesign? copy=null)
    {
        _loading=true;_content=content;_view=view;_error.Text="";var d=content.ShipDesigns!;var recipe=copy?.Recipe??d.StartingRecipe;_name.Text=copy is null?"New design":copy.Name+" copy";
        Fill(_frame,d.Frames.Values.Select(f=>(f.Id,f.Name)),false);Fill(_engine,d.Components.Values.Where(c=>c.Kind==ShipComponentKind.Engine).Select(c=>(c.Id,c.Name)),false);Fill(_armor,d.Components.Values.Where(c=>c.Kind==ShipComponentKind.Armor).Select(c=>(c.Id,c.Name)),true);Fill(_bank1,d.Components.Values.Where(c=>c.Kind==ShipComponentKind.Weapon).Select(c=>(c.Id,c.Name)),false);Fill(_bank2,d.Components.Values.Where(c=>c.Kind==ShipComponentKind.Weapon).Select(c=>(c.Id,c.Name)),true);
        Select(_frame,recipe.FrameId);Select(_engine,recipe.EngineId);Select(_armor,recipe.ArmorId);Select(_bank1,recipe.WeaponBankIds[0]);Select(_bank2,recipe.WeaponBankIds.Length>1?recipe.WeaponBankIds[1]:null);_loading=false;Preview();PopupCenteredClamped(new(660,560));
    }
    public void UpdateView(ContentSet content,EmpireView view){_content=content;_view=view;Preview();}
    public void ShowError(string error)=>_error.Text=error;
    private ShipRecipe Recipe(){var second=_bank2.GetSelectedMetadata().AsString();return new(_frame.GetSelectedMetadata().AsString(),_engine.GetSelectedMetadata().AsString(),_armor.GetSelectedMetadata().AsString() is {Length:>0} armor?armor:null,second.Length==0?ImmutableArray.Create(_bank1.GetSelectedMetadata().AsString()):ImmutableArray.Create(_bank1.GetSelectedMetadata().AsString(),second));}
    private void Preview()
    {
        if(_loading||_content is null||_save is null)return;var result=ShipDesignQueries.PreviewDraft(_content,_view,_name.Text,Recipe());_save.Disabled=!result.Allowed;
        string values=result.Stats is {} s?$"Build cost {Format.Points(s.BuildCost)} ({s.BuildCost} internal); hull {s.MaxHull}; attack {s.Attack}; armor {s.Armor}; speed {s.BaseSpeed}; space {s.SpaceUsed}/{s.SpaceCapacity}\n":"";
        string reason=result.Allowed?"Save creates a new immutable catalogue record; it does not equip or purchase a ship.":result.RejectionCode switch{"ship_design_name"=>"Use a trimmed name of 1–40 characters, without control characters.","ship_design_locked"=>"Requires research: "+string.Join(", ",result.MissingTechnologyIds.Select(id=>_content.Technologies[id].Name+" ("+id+")")),"ship_design_duplicate_name"=>"Another retained design already uses that name. Choose a different name.","ship_design_active_limit"=>"The catalogue already has 32 active designs. Archive one before saving another.","ship_design_retained_limit"=>"The catalogue already has 256 retained designs. Delete an eligible archived record first.",_=>result.Explanation??"Invalid equipment combination or frame space capacity."};
        _preview.Text=values+reason;
    }
    private void Save(){var recipe=Recipe();var preview=ShipDesignQueries.PreviewDraft(_content,_view,_name.Text,recipe);if(preview.Allowed)Saved?.Invoke(_name.Text.Trim(),recipe);else Preview();}
    private static void Fill(OptionButton picker,System.Collections.Generic.IEnumerable<(string Id,string Name)> rows,bool none){picker.Clear();if(none){picker.AddItem("None");picker.SetItemMetadata(0,"");}foreach(var row in rows){picker.AddItem(row.Name);picker.SetItemMetadata(picker.ItemCount-1,row.Id);}}
    private static void Select(OptionButton picker,string? id){for(int i=0;i<picker.ItemCount;i++)if(picker.GetItemMetadata(i).AsString()==(id??"")){picker.Select(i);return;}}
}
