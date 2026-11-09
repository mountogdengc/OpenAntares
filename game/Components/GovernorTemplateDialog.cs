using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class GovernorTemplateDialog:ConfirmationDialog
{
    public event Action<Command>? CommandIssued;
    private readonly LineEdit _name=new(){Name="TemplateName",PlaceholderText="Template name",MaxLength=80};
    private readonly OptionButton _support=new(){Name="SupportTarget"};
    private readonly SpinBox _production=new(){Name="ProductionWeight",MinValue=0,MaxValue=100,Step=1};
    private readonly SpinBox _research=new(){Name="ResearchWeight",MinValue=0,MaxValue=100,Step=1};
    private readonly BuildingPreferenceEditor _foundations=new(){Name="FoundationPreferences"},_development=new(){Name="DevelopmentPreferences"};
    private readonly Label _error=DevelopmentUi.Text("","WarningLabel");
    private ContentSet? _content;private EmpireView? _view;private string? _editId;
    public override void _Ready() {
        Title="Colony template";OkButtonText="Save template";DialogHideOnOk=false;
        var body=DevelopmentUi.DialogBody(this);body.AddChild(DevelopmentUi.Text("Defaults are read-only originals. Copy one to make your own priorities. Buildings remain independently available."));
        body.AddChild(_name);body.AddChild(DevelopmentUi.Text("Support target"));_support.AddItem("Minimum support");_support.AddItem("Capped growth below capacity");body.AddChild(_support);
        var weights=new HBoxContainer();body.AddChild(weights);weights.AddChild(new Label{Text="Production weight"});weights.AddChild(_production);weights.AddChild(new Label{Text="Research weight"});weights.AddChild(_research);
        body.AddChild(DevelopmentUi.Text("Foundations, in priority order"));body.AddChild(_foundations);body.AddChild(DevelopmentUi.Text("Development, after foundations"));body.AddChild(_development);body.AddChild(_error);Confirmed+=Save;
    }
    public void Open(ContentSet content,EmpireView view,GovernorTemplateSnapshot snapshot,bool editExisting=false) {
        _content=content;_view=view;_editId=editExisting&&view.Development!.CustomTemplates.Any(t=>t.Id==snapshot.Id)?snapshot.Id:null;
        _name.Text=_editId is null?"My "+snapshot.Definition.Name:snapshot.Definition.Name;_support.Select((int)snapshot.Definition.SupportTarget);_production.Value=snapshot.Definition.ProductionWeight;_research.Value=snapshot.Definition.ResearchWeight;
        _foundations.Show(content,snapshot.Definition.FoundationIds);_development.Show(content,snapshot.Definition.DevelopmentIds);_error.Text="";PopupCenteredClamped(new(680,540));_name.GrabFocus();
    }
    private void Save() {
        if(_content is null||_view is null)return;
        var draft=new GovernorTemplateDraft(_name.Text,_foundations.Ids,_development.Ids,(SupportTargetMode)_support.Selected,(int)_production.Value,(int)_research.Value);
        var validation=TemplatePresetCodec.Deserialize(TemplatePresetCodec.Serialize(ImmutableArray.Create(draft)),_content);
        if(!validation.Succeeded){_error.Text=string.Join("\n",validation.Errors);return;}
        if(_editId is null&&_view.Development!.CustomTemplates.Length>=64){_error.Text="Your library already has64 templates. Delete an unused custom template first.";return;}
        CommandIssued?.Invoke(new UpsertGovernorTemplateCommand(_view.Viewer,_view.Turn,_editId,draft));Hide();
    }
}
internal partial class BuildingPreferenceEditor:VBoxContainer
{
    private readonly ItemList _list=new(){CustomMinimumSize=new(0,80)};private readonly OptionButton _picker=new(){SizeFlagsHorizontal=SizeFlags.ExpandFill};private readonly List<string> _ids=new();private ContentSet? _content;
    internal ImmutableArray<string> Ids=>_ids.ToImmutableArray();
    public override void _Ready(){AddChild(_list);AddChild(_picker);var row=new HBoxContainer();AddChild(row);DevelopmentUi.Button(row,"Add",()=>{if(_picker.Selected>=0&&_ids.Count<32){string id=_picker.GetSelectedMetadata().AsString();if(!_ids.Contains(id))_ids.Add(id);Refresh();}});DevelopmentUi.Button(row,"Remove",()=>{if(_list.GetSelectedItems().FirstOrDefault(-1) is int i&&i>=0){_ids.RemoveAt(i);Refresh();}});DevelopmentUi.Button(row,"Up",()=>Move(-1));DevelopmentUi.Button(row,"Down",()=>Move(1));}
    internal void Show(ContentSet content,ImmutableArray<string> ids){_content=content;_ids.Clear();_ids.AddRange(ids);_picker.Clear();foreach(var b in content.Buildings.Values){_picker.AddItem(b.Name);_picker.SetItemMetadata(_picker.ItemCount-1,b.Id);}Refresh();}
    private void Move(int delta){int i=_list.GetSelectedItems().FirstOrDefault(-1),j=i+delta;if(i<0||j<0||j>=_ids.Count)return;(_ids[i],_ids[j])=(_ids[j],_ids[i]);Refresh();_list.Select(j);}
    private void Refresh(){_list.Clear();foreach(string id in _ids)_list.AddItem(_content!.Buildings[id].Name);}
}
internal static class DevelopmentUi
{
    internal static Label Text(string text,string variation="SubtleLabel")=>new(){Text=text,ThemeTypeVariation=variation,AutowrapMode=TextServer.AutowrapMode.WordSmart,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};
    internal static Button Button(Node parent,string text,Action action,string? name=null){var button=new Button{Text=text,Name=name??text.Replace(" ",""),ClipText=true,TooltipText=text,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};button.Pressed+=action;parent.AddChild(button);return button;}
    internal static VBoxContainer DialogBody(Window dialog){var margin=new MarginContainer{ThemeTypeVariation="PanelPadding"};dialog.AddChild(margin);var scroll=new ScrollContainer{SizeFlagsVertical=Control.SizeFlags.ExpandFill,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled,VerticalScrollMode=ScrollContainer.ScrollMode.Reserve,FollowFocus=true};margin.AddChild(scroll);var body=new VBoxContainer{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};scroll.AddChild(body);return body;}
    internal static void Clear(Node node){foreach(Node child in node.GetChildren()){node.RemoveChild(child);child.QueueFree();}}
}
