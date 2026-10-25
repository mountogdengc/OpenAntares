using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Godot;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class TemplatePresetDialog:AcceptDialog
{
    public event Action<Command>? CommandIssued;
    private readonly LineEdit _path=new(){Name="PresetPath",PlaceholderText="Choose a preset file path"};private readonly Label _message=DevelopmentUi.Text("","WarningLabel");private ContentSet? _content;private EmpireView? _view;private ImmutableArray<GovernorTemplateDraft> _drafts;
    public override void _Ready(){Title="Governor template presets";OkButtonText="Close";var body=DevelopmentUi.DialogBody(this);body.AddChild(DevelopmentUi.Text("Portable template definitions. Import creates new custom templates in this campaign; it does not change assignments."));body.AddChild(_path);DevelopmentUi.Button(body,"Import preset",Import);DevelopmentUi.Button(body,"Export preset",Export);body.AddChild(_message);}
    public void Open(ContentSet content,EmpireView view,ImmutableArray<GovernorTemplateDraft> drafts){_content=content;_view=view;_drafts=drafts;_path.Text="";_message.Text="Enter the file path to import or export.";PopupCenteredClamped(new(640,430));}
    private string PathValue(){if(string.IsNullOrWhiteSpace(_path.Text))throw new ArgumentException("Enter a file path first.");return ProjectSettings.GlobalizePath(_path.Text);}
    private void Import(){try{string path=PathValue();if(new FileInfo(path).Length>256*1024)throw new ArgumentException("Preset exceeds256KiB.");var decoded=TemplatePresetCodec.Deserialize(File.ReadAllText(path,Encoding.UTF8),_content!);if(!decoded.Succeeded){_message.Text=string.Join("\n",decoded.Errors);return;}if(decoded.Templates.Length+_view!.Development!.CustomTemplates.Length>64){_message.Text="Import would exceed your64-template library. Nothing was imported.";return;}CommandIssued?.Invoke(new ImportGovernorTemplatesCommand(_view.Viewer,_view.Turn,decoded.Templates));_message.Text=$"Imported {decoded.Templates.Length} templates.";}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or ArgumentException){_message.Text=ex.Message;}}
    private void Export(){string? temporary=null;bool created=false;try{string path=PathValue();string text=TemplatePresetCodec.Serialize(_drafts);temporary=path+".openantares-tmp";using(var stream=new FileStream(temporary,FileMode.CreateNew,System.IO.FileAccess.Write,FileShare.None)){created=true;byte[] bytes=Encoding.UTF8.GetBytes(text);stream.Write(bytes);stream.Flush(true);}File.Move(temporary,path,true);created=false;_message.Text="Preset exported.";}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or ArgumentException){_message.Text=ex.Message;}finally{if(created&&temporary is not null)File.Delete(temporary);}}
}
