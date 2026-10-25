using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class ColonyOrdersPanel:VBoxContainer
{
    public event Action<Command>? CommandIssued;
    public void ShowColony(ContentSet content,EmpireView view,OwnedColonyView colony)
    {
        DevelopmentUi.Clear(this);AddChild(new HSeparator());AddChild(new Label{Text="Colony plans",ThemeTypeVariation="HeaderLabel"});
        if(!Rules.HasDevelopment(view.RulesVersion)||colony.Development is not {} d){AddChild(DevelopmentUi.Text("This campaign keeps its original rules. Start a new game to use governor plans and construction queues."));return;}
        var picker=new OptionButton{Name="GovernorTemplatePicker",SizeFlagsHorizontal=SizeFlags.ExpandFill};picker.AddItem("Manual / no template");picker.SetItemMetadata(0,"");
        foreach(var t in content.GovernorTemplates.Values){picker.AddItem(t.Name+" (default)");picker.SetItemMetadata(picker.ItemCount-1,t.Id);}
        foreach(var t in view.Development!.CustomTemplates){picker.AddItem(t.Definition.Name+$" (revision {t.Revision})");picker.SetItemMetadata(picker.ItemCount-1,t.Id);}
        for(int i=0;i<picker.ItemCount;i++)if(picker.GetItemMetadata(i).AsString()==(d.Template?.Id??"balanced"))picker.Select(i);if(d.Template is null&&picker.Selected==0&&picker.ItemCount>1)picker.Select(1);AddChild(picker);
        var workforce=new CheckBox{Name="WorkforceGovernorToggle",Text="Governor manages workforce",ButtonPressed=d.WorkforceEnabled,Disabled=d.Template is null};var construction=new CheckBox{Name="ConstructionGovernorToggle",Text="Governor manages construction",ButtonPressed=d.ConstructionEnabled};AddChild(workforce);AddChild(construction);
        workforce.Toggled+=on=>CommandIssued?.Invoke(new SetColonyAutomationCommand(view.Viewer,view.Turn,colony.Id,on,construction.ButtonPressed));construction.Toggled+=on=>CommandIssued?.Invoke(new SetColonyAutomationCommand(view.Viewer,view.Turn,colony.Id,workforce.ButtonPressed,on));
        var replace=new CheckBox{Name="ReplaceActiveProject",Text="Replace active project when applying (keep reserve)"};AddChild(replace);
        var preview=DevelopmentUi.Text("");AddChild(preview);
        GovernorTemplateSnapshot? Chosen(){string id=picker.GetSelectedMetadata().AsString();if(id.Length==0)return null;if(content.GovernorTemplates.TryGetValue(id,out var t))return new(id,1,new(t.Name,t.FoundationIds,t.DevelopmentIds,t.SupportTarget,t.ProductionWeight,t.ResearchWeight));return view.Development.CustomTemplates.Single(t=>t.Id==id);}
        void Preview()
        {
            var row=GovernorPlanningQueries.Preview(content,view,colony.Id,Chosen(),workforce.ButtonPressed,construction.ButtonPressed,replace.ButtonPressed);
            string effect=replace.ButtonPressed&&colony.ProjectId is not null?"Active project cleared on Apply.":"Active work retained.";
            if(row is null)
            {
                preview.Text=$"Manual control. {effect} Reserve retained.";
                return;
            }
            string next=row.Target is null?"bank production":row.Target.Kind=="building"?content.Buildings[row.Target.ContentId].Name:content.Vessels[row.Target.ContentId].Name;
            preview.Text=$"Preview Support/Production/Research {colony.Workforce.Support}/{colony.Workforce.Production}/{colony.Workforce.Research} to {row.Workforce.Support}/{row.Workforce.Production}/{row.Workforce.Research} at next Ready. {effect} Reserve retained; next Ready: {next}.";
        }

        picker.ItemSelected+=_=>Preview();replace.Toggled+=_=>Preview();Preview();
        DevelopmentUi.Button(this,"Apply template",()=>CommandIssued?.Invoke(new AssignGovernorTemplateCommand(view.Viewer,view.Turn,ImmutableArray.Create(colony.Id),Chosen()?.Id,Chosen() is not null&&workforce.ButtonPressed,construction.ButtonPressed,replace.ButtonPressed)),"ApplyTemplate");
        var editor=new GovernorTemplateDialog();AddChild(editor);editor.CommandIssued+=c=>CommandIssued?.Invoke(c);
        DevelopmentUi.Button(this,"Copy template",()=>{if(Chosen() is {} t)editor.Open(content,view,t);});
        var edit=DevelopmentUi.Button(this,"Edit custom template",()=>{if(Chosen() is {} t)editor.Open(content,view,t,true);});
        var delete=DevelopmentUi.Button(this,"Delete custom template",()=>{if(Chosen() is {} t)CommandIssued?.Invoke(new DeleteGovernorTemplateCommand(view.Viewer,view.Turn,t.Id));});
        void Buttons(){bool custom=view.Development.CustomTemplates.Any(t=>t.Id==Chosen()?.Id);edit.Disabled=!custom;delete.Disabled=!custom;}picker.ItemSelected+=_=>Buttons();Buttons();
        AddChild(DevelopmentUi.Text(d.Template is {} assigned?$"Assigned: {assigned.Definition.Name}, revision {assigned.Revision}. Library edits take effect only after Apply.":"No assigned template."));
        AddChild(DevelopmentUi.Text($"Workforce: {(d.WorkforceEnabled?"automatic":"manual / paused")}. Construction: {(d.ConstructionEnabled?"automatic":"manual / paused")}. Active origin: {d.Origin}."));
        var queue=new ItemList{Name="ConstructionQueue",CustomMinimumSize=new(0,90)};AddChild(queue);foreach(var entry in d.Queue){string title=entry.Target.Kind=="building"?content.Buildings[entry.Target.ContentId].Name:content.Vessels[entry.Target.ContentId].Name;bool locked=entry.Target.Kind=="building"&&content.Buildings[entry.Target.ContentId].PrerequisiteTechnologyIds.Any(id=>!view.Research.KnownTechnologyIds.Contains(id));queue.AddItem(title+(locked?" (technology locked)":"")+(entry.Token==d.ActiveQueueToken?" (active)":""));}
        var queueEditor=new ConstructionQueueEditor();AddChild(queueEditor);queueEditor.CommandIssued+=c=>CommandIssued?.Invoke(c);DevelopmentUi.Button(this,"Edit queue",()=>queueEditor.Open(content,view,colony));
        var presets=new TemplatePresetDialog();AddChild(presets);presets.CommandIssued+=c=>CommandIssued?.Invoke(c);DevelopmentUi.Button(this,"Template presets",()=>presets.Open(content,view,view.Development.CustomTemplates.Length>0?view.Development.CustomTemplates.Select(t=>t.Definition).ToImmutableArray():Chosen() is {} t?ImmutableArray.Create(t.Definition):ImmutableArray<GovernorTemplateDraft>.Empty));
    }
}
