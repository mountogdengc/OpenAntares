using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Galaxy;
using static OpenAntares.Game.Validation.ScoutUiControlChecks;
namespace OpenAntares.Game.Validation;
public partial class ShipDesignGraphicalValidation:Node
{
    private string _output="";private readonly List<object> _checks=new();
    private void Check(bool value,string name){_checks.Add(new{name,passed=value});if(!value)throw new InvalidOperationException(name);}
    public override async void _Ready()
    {
        string? error=null;
        try{_output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output="))[20..];Directory.CreateDirectory(_output);Check(DisplayServer.GetName()!="headless","Actual graphical display");foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)}){await DesignerControls(size);await CatalogueBounds(size,32);await CatalogueBounds(size,256);await ArchiveBoundReferences(size);}}
        catch(Exception failure){error=failure.ToString();GD.PrintErr(error);}
        finally{if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{scenario="ShipDesign",passed=error is null,error,method="Actual Godot control signals; labelled pre-session fixtures; no physical input, comprehension or generated earned-campaign claim",checks=_checks},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(error is null?0:1);}
    }
    private async Task DesignerControls(Vector2I size)
    {
        GetTree().Root.Size=size;var content=CompleteContent();var made=GalaxyGenerator.CreateNewGame(content,new("small",42,10,"harl",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));Check(made.Succeeded,"Explicit10 designer fixture generates");var session=new GameSession(content,made.State!);var screen=Screen(session,session.State.Empires[0].Id,_output);AddChild(screen);await Frames(this);
        try
        {
            Press(Named<Button>(screen,"OpenFleetCommand"));await Frames(this);var fleet=Descendants<FleetCommandDialog>(screen).Single();Press(Named<Button>(fleet,"OpenShipDesigns"));await Frames(this);var catalogue=Descendants<ShipDesignDialog>(screen).Single();Check(catalogue.Visible,"Actual Fleet Command entry opens catalogue at "+size.X);Press(Named<Button>(catalogue,"CopyDesign"));await Frames(this);var editor=Descendants<ShipDesignEditor>(screen).Single();Check(editor.Visible,"Actual Copy opens immutable designer");Name(editor,"Gun Escort");await Pick(editor,"DesignArmor","");await Pick(editor,"DesignBank2","pulse_bank");var text=Named<Label>(editor,"DesignPreview").Text;Check(text.Contains("2800")&&text.Contains("48")&&text.Contains("6/6"),"Actual two-pulse/no-armor controls preview 2800/48/6 space at "+size.X);var save=Named<Button>(editor,"SaveDesign");Check(!save.Disabled,"Valid recipe enables explicit Save");var scroll=Descendants<ScrollContainer>(editor).First();save.GrabFocus();scroll.EnsureControlVisible(save);await Frames(this);Check(save.GetGlobalRect().Intersects(scroll.GetGlobalRect()),"Focused Save reachable within scroll at "+size.X);await Evidence(editor,"designer-gun-"+size.X);Press(save);await Frames(this);var design=session.State.Empires[0].ShipDesigns!.Designs.Single(d=>d.Name=="Gun Escort");Check(design.Recipe.ArmorId is null&&design.Recipe.WeaponBankIds.Length==2,"Actual Save pins the chosen immutable recipe");Check(editor.Size.X<=size.X&&editor.Size.Y<=size.Y,"Designer native container clamped at "+size.X);
            Press(Named<Button>(catalogue,"CopyDesign"));await Frames(this);var name=Named<LineEdit>(editor,"DesignName");Name(editor,new string('A',40));await Frames(this);Check(!save.Disabled,"Forty-character name accepted at "+size.X);Name(editor,new string('A',41));await Frames(this);Check(save.Disabled&&Named<Label>(editor,"DesignPreview").Text.Contains("40"),"Forty-one-character name visibly rejected");Name(editor,"Gun Escort");await Frames(this);Check(save.Disabled&&Named<Label>(editor,"DesignPreview").Text.Contains("name"),"Existing name with different recipe rejects");Name(editor,"Locked preview");await Pick(editor,"DesignEngine","vector_drive");Check(save.Disabled&&Named<Label>(editor,"DesignPreview").Text.Contains("vector_drives"),"Locked component exposes its research requirement");await Pick(editor,"DesignEngine","conventional_drive");await Pick(editor,"DesignArmor","");await Pick(editor,"DesignBank1","lance_bank");await Pick(editor,"DesignBank2","pulse_bank");Check(save.Disabled&&Named<Label>(editor,"DesignPreview").Text.Contains("7/6"),"Invalid seven-of-six capacity is visibly explained");string before=Snapshot(session);Press(editor.GetOkButton());await Frames(this);Check(before==Snapshot(session)&&!editor.Visible&&catalogue.Visible,"Actual Cancel returns to catalogue without mutation");
            await SelectDesign(catalogue,"Gun Escort");Check(Named<Button>(catalogue,"DeleteDesign").Disabled,"Active recipe cannot be deleted");Press(Named<Button>(catalogue,"ArchiveDesign"));await Frames(this);var include=Named<CheckBox>(catalogue,"DesignIncludeArchived");include.SetPressedNoSignal(true);include.EmitSignal(BaseButton.SignalName.Toggled,true);await Frames(this);await SelectDesign(catalogue,"Gun Escort");Check(!Named<Button>(catalogue,"DeleteDesign").Disabled,"Unreferenced archived recipe is deletable");Press(Named<Button>(catalogue,"DeleteDesign"));await Frames(this);Check(session.State.Empires[0].ShipDesigns!.Designs.All(d=>d.Name!="Gun Escort"),"Actual Delete removes eligible archived recipe");await Evidence(catalogue,"catalogue-after-delete-"+size.X);
        }
        finally{RemoveChild(screen);screen.QueueFree();await Frames(this);}
    }
    private async Task CatalogueBounds(Vector2I size,int count)
    {
        var c=CompleteContent();var made=GalaxyGenerator.CreateNewGame(c,new("small",42,10,"harl",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));var s=made.State!;var data=s.Empires[0].ShipDesigns!;for(int n=2;n<=count;n++)data.Designs.Add(new(new(n),"Boundary "+n.ToString("D3"),c.ShipDesigns!.StartingRecipe,count==256&&n>31));data.NextDesignId=count+1;Check(OpenAntares.Simulation.State.GameStateValidator.Validate(c,s).Count==0,"Labelled pre-session catalogue boundary validates: "+count);var session=new GameSession(c,s);var screen=Screen(session,s.Empires[0].Id,_output);AddChild(screen);await Frames(this);
        try
        {
            Press(Named<Button>(screen,"OpenFleetCommand"));await Frames(this);Press(Named<Button>(screen,"OpenShipDesigns"));await Frames(this);var catalogue=Descendants<ShipDesignDialog>(screen).Single();Check(Named<ItemList>(catalogue,"DesignCatalogue").ItemCount<=16,"Actual catalogue page bounded to sixteen");Press(Named<Button>(catalogue,"NewDesign"));await Frames(this);var editor=Descendants<ShipDesignEditor>(screen).Single();Check(Named<Button>(editor,"SaveDesign").Disabled&&Named<Label>(editor,"DesignPreview").Text.Contains(count.ToString()),"Actual Save explains "+count+" catalogue bound at "+size.X);await Evidence(editor,"catalogue-bound-"+count+"-"+size.X);Press(editor.GetOkButton());await Frames(this);
            if(count==256){var include=Named<CheckBox>(catalogue,"DesignIncludeArchived");include.SetPressedNoSignal(true);include.EmitSignal(BaseButton.SignalName.Toggled,true);await Frames(this);Press(Named<Button>(catalogue,"DesignPageNext"));await Frames(this);Check(Named<Label>(catalogue,"DesignPageLabel").Text.Contains("2/16"),"Actual next catalogue page retains bounded rows");var search=Named<LineEdit>(catalogue,"DesignSearch");search.Text="Boundary 256";search.EmitSignal(LineEdit.SignalName.TextChanged,search.Text);await Frames(this);Check(Named<ItemList>(catalogue,"DesignCatalogue").ItemCount==1&&Named<Label>(catalogue,"DesignPageLabel").Text.Contains("1/1"),"Actual filter resets the page and finds final retained record");Check(!Named<Button>(catalogue,"RestoreDesign").Disabled,"Eligible archived record can be restored below active cap");Press(Named<Button>(catalogue,"RestoreDesign"));await Frames(this);Check(!session.State.Empires[0].ShipDesigns!.Designs.Single(d=>d.Id.Value==256).Archived,"Actual Restore retains the original design identity");}
        }
        finally{RemoveChild(screen);screen.QueueFree();await Frames(this);}
    }
    private async Task ArchiveBoundReferences(Vector2I size)
    {
        var c=CompleteContent();var generated=GalaxyGenerator.CreateNewGame(c,new("small",42,10,"harl",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));var session=new GameSession(c,generated.State!);var owner=session.State.Empires[0].Id;var recipe=c.ShipDesigns!.StartingRecipe with{ArmorId=null,WeaponBankIds=System.Collections.Immutable.ImmutableArray.Create("pulse_bank","pulse_bank")};var save=session.Execute(new OpenAntares.Simulation.ShipDesigns.ShipDesignCommand(owner,0,1,new OpenAntares.Simulation.ShipDesigns.ShipDesignAction.Save("Bound Gun",recipe)));Check(save.Accepted,"Labelled pre-session public-command design preparation");var bound=session.Execute(new OpenAntares.Simulation.Commands.SetProductionTargetCommand(owner,0,session.State.Colonies[0].Id,OpenAntares.Simulation.ShipDesigns.ShipDesignTargets.Build(new(2))));Check(bound.Accepted,"Labelled pre-session public-command exact-project binding");var screen=Screen(session,owner,_output);AddChild(screen);await Frames(this);
        try
        {
            Press(Named<Button>(screen,"OpenFleetCommand"));await Frames(this);Press(Named<Button>(screen,"OpenShipDesigns"));await Frames(this);var catalogue=Descendants<ShipDesignDialog>(screen).Single();await SelectDesign(catalogue,"Bound Gun");Press(Named<Button>(catalogue,"ArchiveDesign"));await Frames(this);var include=Named<CheckBox>(catalogue,"DesignIncludeArchived");include.SetPressedNoSignal(true);include.EmitSignal(BaseButton.SignalName.Toggled,true);await Frames(this);await SelectDesign(catalogue,"Bound Gun");Check(Named<Button>(catalogue,"DeleteDesign").Disabled&&Named<Label>(catalogue,"DesignDetails").Text.Contains("1 projects"),"Actual archived catalogue discloses pinned project and blocks deletion at "+size.X);Check(session.State.Colonies[0].ProjectId=="2"&&session.State.Colonies[0].ProjectKind=="ship_design","Archive preserves the exact committed build target");await Evidence(catalogue,"catalogue-bound-reference-"+size.X);File.WriteAllText(Path.Combine(_output,"labelled-bound-project-"+size.X+".json"),Snapshot(session));
        }
        finally{RemoveChild(screen);screen.QueueFree();await Frames(this);}
    }
    private async Task SelectDesign(ShipDesignDialog dialog,string name){var list=Named<ItemList>(dialog,"DesignCatalogue");int i=Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemText(i).StartsWith(name,StringComparison.Ordinal));list.DeselectAll();list.Select(i);list.EmitSignal(ItemList.SignalName.ItemSelected,(long)i);await Frames(this);}
    private async Task Evidence(Window window,string name)
    {
        var size=window.Size;var position=window.Position;bool forceNative=window.ForceNative;
        Check(!window.IsEmbedded(),"Primary geometry uses a native window: "+name);Check(size.X<=GetTree().Root.Size.X&&size.Y<=GetTree().Root.Size.Y,"Popup bounds: "+name);_checks.Add(new{name="native geometry "+name,passed=true,width=size.X,height=size.Y,forceNative,embedded=window.IsEmbedded()});File.WriteAllText(Path.Combine(_output,name+"-text.txt"),string.Join("\n",Descendants<Label>(window).Where(l=>l.IsVisibleInTree()).Select(l=>l.Text)));await Capture(this,window,Path.Combine(_output,name+"-embedded.png"));Check(window.IsEmbedded(),"Supplementary screenshot uses an explicitly embedded window: "+name);
        window.Hide();window.ForceNative=forceNative;window.Size=size;window.Position=position;window.Popup();await Frames(this);

    }
    private static void Name(Node root,string text){var editor=Named<LineEdit>(root,"DesignName");editor.Text=text;editor.EmitSignal(LineEdit.SignalName.TextChanged,text);}
    private async Task Pick(Node root,string name,string id){var picker=Named<OptionButton>(root,name);int index=Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsString()==id);picker.Select(index);picker.EmitSignal(OptionButton.SignalName.ItemSelected,(long)index);await Frames(this);}
}
