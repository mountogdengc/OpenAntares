using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
namespace OpenAntares.GameValidation;
public partial class ColonyGraphicalValidation:GraphicalValidation
{
    public override async void _Ready()
    {
        string? error=null;
        try{
            _output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output=",StringComparison.Ordinal))[20..];Directory.CreateDirectory(_output);
            Check(DisplayServer.GetName()!="headless","Colony validation uses a graphical display");
            await InitialControls();
            await BlockedCargoControls();
        }catch(Exception ex){error=ex.ToString();GD.PrintErr(error);}
        finally{if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{runId=_runId,scenario="Colony",method="Actual controls/signals with labelled fixtures; no physical OS input or human-readability claim.",passed=error is null,error,results=_results,retainedQaSavePaths=_savePaths},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(error is null?0:1);}
    }
    private static ContentSet DevelopmentContent(){var loaded=ContentLoader.Load(new[]{new ContentSource("labelled-colony-content",File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/colony-development-content.json")))},requireDevelopment:true);return loaded.Content??throw new InvalidOperationException(string.Join("; ",loaded.Errors));}
    private async Task InitialControls()
    {
        GetTree().Root.Size=new(800,600);var content=DevelopmentContent();var generated=GalaxyGenerator.CreateNewGame(content,new("small",42,5,"harl"));Check(generated.Succeeded,"Labelled rules5 initial campaign generated normally");
        string name=$"qa-{_runId}-colony-initial";string path=SaveFiles.PathFor(name);Check(!File.Exists(path),"Initial fixture path unique");SaveGame.WriteFile(path,content,generated.State!);_savePaths.Add(path);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();Press(Button(One<NewGameScreen>(main),"Start"));await Frames();var screen=await LoadThroughDialog(main,name);Check(Session(screen).State.RulesVersion==5,"Ordinary Load opens embedded rules5 campaign");
        Check(Descendants<OptionButton>(screen).Any(p=>p.Name=="GovernorTemplatePicker"),"Governor template picker exists on actual colony panel");
        Check(Picker(screen,"GovernorTemplatePicker").ItemCount==5,"Four defaults plus manual are visible");
        Select(Picker(screen,"GovernorTemplatePicker"),"industrial");await Frames();await Click(screen,"Copy template");var edit=One<GovernorTemplateDialog>(screen);
        One<LineEdit>(edit).Text="";Press(edit.GetOkButton());await Frames();Check(edit.Visible&&Descendants<Label>(edit).Any(l=>l.Text.Contains("non-empty",StringComparison.Ordinal)),"Invalid empty name is diagnosed in place");
        One<LineEdit>(edit).Text="Factory plan";var weights=Descendants<SpinBox>(edit).ToArray();foreach(var weight in weights)weight.Value=0;Press(edit.GetOkButton());await Frames();Check(edit.Visible&&Descendants<Label>(edit).Any(l=>l.Text.Contains("weights",StringComparison.Ordinal)),"Invalid zero weights are diagnosed before publication");weights.Single(w=>w.Name=="ProductionWeight").Value=3;weights.Single(w=>w.Name=="ResearchWeight").Value=1;Press(edit.GetOkButton());await Frames();Check(Session(screen).State.Empires[0].Development!.CustomTemplates.Count==1,"Actual copy control creates an independent template");
        Select(Picker(screen,"GovernorTemplatePicker"),"custom_1");await Click(screen,"Apply template");Check(Session(screen).State.Colonies[0].Development!.Template!.Revision==1,"Apply assigns revision1 snapshot");
        Toggle(screen,"WorkforceGovernorToggle",true);await Frames();Check(Session(screen).State.Colonies[0].Development!.WorkforceEnabled&&!Session(screen).State.Colonies[0].Development!.ConstructionEnabled,"Workforce switch is independent");
        Toggle(screen,"ConstructionGovernorToggle",true);await Frames();Check(Session(screen).State.Colonies[0].Development!.ConstructionEnabled,"Construction switch is independent");
        await Click(screen,"Edit custom template");edit=One<GovernorTemplateDialog>(screen);One<LineEdit>(edit).Text="Factory revised";Press(edit.GetOkButton());await Frames();
        Check(Session(screen).State.Empires[0].Development!.CustomTemplates[0].Revision==2&&Session(screen).State.Colonies[0].Development!.Template!.Revision==1,"Library edit does not change an assigned snapshot");
        await Click(screen,"Apply template");Check(Session(screen).State.Colonies[0].Development!.Template!.Revision==2,"Explicit reapply advances assigned revision");
        await Click(screen,"Edit queue");var queue=One<ConstructionQueueEditor>(screen);Select(Picker(queue,"QueueTargetPicker"),"building|production_complex");Press(Button(queue,"Add project"));Select(Picker(queue,"QueueTargetPicker"),"building|fabrication_hall");Press(Button(queue,"Add project"));Press(queue.GetOkButton());await Frames();
        Check(Session(screen).State.Colonies[0].Development!.Queue.Count==2,"Actual queue editor retains locked first and legal next entries");
        await EndTurn(screen);One<TurnReportDialog>(screen).Hide();await Frames();var colony=Session(screen).State.Colonies[0];Check(colony.ProjectId=="fabrication_hall"&&colony.Development!.ActiveQueueToken==2,"Ready chooses legal next queue entry");Check(colony.Development!.Queue[0].Target.ContentId=="production_complex","Locked first entry remains queued");
        long reserve=colony.ProductionReserve;Select(Picker(screen,"GovernorTemplatePicker"),"research");await Click(screen,"Apply template");Check(Session(screen).State.Colonies[0].ProjectId=="fabrication_hall","Applying a different template retains active work by default");
        Toggle(screen,"ReplaceActiveProject",true);await Click(screen,"Apply template");Check(Session(screen).State.Colonies[0].ProjectId is null&&Session(screen).State.Colonies[0].ProductionReserve==reserve,"Explicit replacement retains reserve");
        await Click(screen,"Template presets");var presets=One<TemplatePresetDialog>(screen);string preset=Path.Combine(_output,"portable-templates.json");One<LineEdit>(presets).Text=preset;Press(Button(presets,"Export preset"));await Frames();Check(File.Exists(preset),"Explicit preset export writes selected path");Press(Button(presets,"Import preset"));await Frames();Check(Session(screen).State.Empires[0].Development!.CustomTemplates.Count==2,"Preset import creates new local IDs");
        await Click(screen,"Template presets");presets=One<TemplatePresetDialog>(screen);string invalid=Path.Combine(_output,"invalid-preset.json");File.WriteAllText(invalid,"{invalid");One<LineEdit>(presets).Text=invalid;string before=Snapshot(Session(screen));Press(Button(presets,"Import preset"));await Frames();Check(Snapshot(Session(screen))==before&&Descendants<Label>(presets).Any(l=>l.Text.Contains("Invalid preset JSON",StringComparison.Ordinal)),"Invalid preset import preserves the complete campaign");Press(presets.GetOkButton());await Frames();
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)}) {
            GetTree().Root.Size=size;await Frames();Select(Picker(screen,"GovernorTemplatePicker"),"custom_1");foreach(string action in new[]{"Apply template","Copy template","Edit custom template","Delete custom template","Edit queue","Template presets"}){var b=Descendants<Button>(One<ColonyPanel>(screen)).Single(b=>b.Text==action);if(!b.Disabled){await Reach(b);Check(Visible(b),action+" focus reachable at"+size);}}
            await Capture("colony-controls-"+size.X);Select(Picker(screen,"GovernorTemplatePicker"),"industrial");await Click(screen,"Copy template");edit=One<GovernorTemplateDialog>(screen);await Frames();Check(Visible(edit.GetOkButton())&&Visible(edit.GetCancelButton()),"Template confirm and cancel reachable at"+size);await Capture("colony-template-"+size.X);edit.Hide();await Click(screen,"Edit queue");queue=One<ConstructionQueueEditor>(screen);Check(Visible(queue.GetOkButton()),"Queue confirmation reachable at"+size);await Capture("colony-queue-"+size.X);queue.Hide();
        }
        string saved=await SaveThroughDialog(screen,"colony-controls-roundtrip");string expected=Snapshot(Session(screen));screen=await LoadThroughDialog(main,saved);Check(Snapshot(Session(screen))==expected,"Actual UI save/reload preserves queues, revisions and reports");
        await Click(screen,"Template presets");presets=One<TemplatePresetDialog>(screen);var drafts=System.Collections.Immutable.ImmutableArray.CreateRange(Enumerable.Repeat(Session(screen).Content.GovernorTemplates["balanced"],62).Select(t=>new OpenAntares.Simulation.Development.GovernorTemplateDraft(t.Name,t.FoundationIds,t.DevelopmentIds,t.SupportTarget,t.ProductionWeight,t.ResearchWeight)));
        string full=Path.Combine(_output,"fill-library.json");File.WriteAllText(full,OpenAntares.Simulation.Development.TemplatePresetCodec.Serialize(drafts));One<LineEdit>(presets).Text=full;Press(Button(presets,"Import preset"));await Frames();Check(Session(screen).State.Empires[0].Development!.CustomTemplates.Count==64,"Actual preset import reaches bounded library64");
        Select(Picker(screen,"GovernorTemplatePicker"),"balanced");await Click(screen,"Copy template");edit=One<GovernorTemplateDialog>(screen);string fullBefore=Snapshot(Session(screen));Press(edit.GetOkButton());await Frames();Check(edit.Visible&&Snapshot(Session(screen))==fullBefore&&Descendants<Label>(edit).Any(l=>l.Text.Contains("64 templates",StringComparison.Ordinal)),"Full library rejects copy with useful diagnostics and unchanged campaign");edit.Hide();
        RemoveChild(main);main.QueueFree();await Frames();
    }
    private static OptionButton Picker(Node node,string name)=>Descendants<OptionButton>(node).Single(p=>p.Name==name);
    private static void Toggle(Node node,string name,bool value){var b=Descendants<CheckBox>(node).Single(b=>b.Name==name);if(b.Disabled)throw new InvalidOperationException("Disabled toggle "+name);b.SetPressedNoSignal(value);b.EmitSignal(BaseButton.SignalName.Toggled,value);}
    private async Task Reach(Control control){if(control.FocusMode!=Control.FocusModeEnum.None)control.GrabFocus();await Frames();for(Node? p=control.GetParent();p is not null;p=p.GetParent())if(p is ScrollContainer scroll)scroll.EnsureControlVisible(control);await Frames();}
    private async Task Click(Node node,string text){var b=Button(node,text);await Reach(b);Press(b);await Frames();}
    private static bool Visible(Control c)=>c.IsVisibleInTree()&&c.GetViewport().GetVisibleRect().Encloses(c.GetGlobalRect());
    private async Task BlockedCargoControls() {
        var content=DevelopmentContent();var generated=GalaxyGenerator.CreateNewGame(content,new("small",42,5,"wenth"));Check(generated.Succeeded,"Labelled Wenth initial campaign generates normally");string name=$"qa-{_runId}-colony-wenth-initial";string path=SaveFiles.PathFor(name);SaveGame.WriteFile(path,content,generated.State!);_savePaths.Add(path);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();Press(Button(One<NewGameScreen>(main),"Start"));await Frames();var screen=await LoadThroughDialog(main,name);
        await Click(screen,"Edit queue");var queue=One<ConstructionQueueEditor>(screen);Select(Picker(queue,"QueueTargetPicker"),"vessel|daughter_seed");Press(Button(queue,"Add project"));Press(queue.GetOkButton());await Frames();Toggle(screen,"ConstructionGovernorToggle",true);await Frames();await EndTurn(screen);One<TurnReportDialog>(screen).Hide();await Frames();
        var c=Session(screen).State.Colonies[0];Check(c.ProjectId=="daughter_seed"&&c.Development!.Queue.Count==1,"Unembarkable explicit vessel remains active and queued");Check(Descendants<Label>(One<ColonyPanel>(screen)).Any(l=>l.Text.Contains("Awaiting embarkation",StringComparison.Ordinal)),"Blocked cargo explains waiting through ordinary colony UI");
        GetTree().Root.Size=new(800,600);await Frames();var label=Descendants<Label>(One<ColonyPanel>(screen)).First(l=>l.Text.Contains("Awaiting embarkation",StringComparison.Ordinal));await Reach(label);await Capture("colony-wenth-cargo-800");
        var legacy=SaveGame.ReadFile(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/legacy-rules4-v5-save.json")).Game!;string oldName=$"qa-{_runId}-colony-original-rules4";string oldPath=SaveFiles.PathFor(oldName);SaveGame.WriteFile(oldPath,legacy.Content,legacy.State);_savePaths.Add(oldPath);screen=await LoadThroughDialog(main,oldName);Check(!Descendants<OptionButton>(screen).Any(p=>p.Name=="GovernorTemplatePicker")&&Session(screen).State.RulesVersion==4,"Loading old campaign preserves original rules and hides unavailable controls");
        RemoveChild(main);main.QueueFree();await Frames();
    }
}
