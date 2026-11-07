using System;
using System.Linq;
using Godot;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Components;

public partial class TurnGuidancePanel : VBoxContainer
{
    private bool _considerExpanded;
    public event Action<string>? DestinationRequested;
    public void ShowGuidance(EmpireView view)
    {
        foreach(Node child in GetChildren()){RemoveChild(child);child.QueueFree();}
        var items=TurnGuidance.Build(view);
        AddChild(new Label{Text="Responses to review",ThemeTypeVariation="ConsoleHeading"});
        if(!items.Any(i=>i.Severity==GuidanceSeverity.Required))
            AddChild(new Label{Name="ResponseNoticeSummary",Text=TurnGuidance.NoResponseNotices,AutowrapMode=TextServer.AutowrapMode.WordSmart,ThemeTypeVariation="SubtleLabel"});
        foreach(var item in items.Where(i=>i.Severity==GuidanceSeverity.Required))AddItem(this,item);
        var optional=items.Where(i=>i.Severity==GuidanceSeverity.Consider).ToArray();
        if(optional.Length==0)return;
        var toggle=new Button{Name="ToggleOptionalGuidance",Text=OptionalLabel()};AddChild(toggle);
        var body=new VBoxContainer{Name="OptionalGuidanceBody",Visible=_considerExpanded};AddChild(body);
        foreach(var item in optional)AddItem(body,item);
        toggle.Pressed+=()=>{_considerExpanded=!_considerExpanded;body.Visible=_considerExpanded;toggle.Text=OptionalLabel();};
    }
    private string OptionalLabel()=>"Consider (optional) "+(_considerExpanded?"−":"+");
    private void AddItem(VBoxContainer parent,TurnGuidanceItem item)
    {
        parent.AddChild(new Label{Text=item.Text,AutowrapMode=TextServer.AutowrapMode.WordSmart,ThemeTypeVariation=item.Severity==GuidanceSeverity.Required?"WarningLabel":"SubtleLabel"});
        var link=new Button{Name="Guidance_"+item.Key,Text="Review "+item.Destination.ToLowerInvariant()};
        parent.AddChild(link);link.Pressed+=()=>DestinationRequested?.Invoke(item.Destination);
    }
}
