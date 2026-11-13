using UiNavigation = OpenAntares.Game.Validation.CombinedUiNavigation;
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
using OpenAntares.Simulation;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Appearance;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Development;
using OpenAntares.Game.Presentation;
using System.Collections.Immutable;
using System.Security.Cryptography;
namespace OpenAntares.GameValidation;
public partial class ColonyGraphicalValidation:GraphicalValidation
{
    public override async void _Ready()
    {
        string? error=null;
        try{
            _output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output=",StringComparison.Ordinal))[20..];Directory.CreateDirectory(_output);
            Check(DisplayServer.GetName()!="headless","Colony validation uses a graphical display");
            await CurrentStartChecks();
            await PolishHistoryChecks();
            await PolishTemplateDeletionChecks();
            await PolishNotificationChecks();
            await ReplacementPreviewChecks();
            await InvalidDevelopmentContentRecovery();
            await PortraitChecks();
            await InitialControls();
            await BlockedCargoControls();
            PreferenceChecks();
            await ManagementChecks();
            await ManualOverrideDigest();
            await GeneratedDevelopmentCampaigns();
        }catch(Exception ex){error=ex.ToString();GD.PrintErr(error);}
        finally{if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{runId=_runId,scenario="Colony",method="Actual controls/signals with labelled fixtures; no physical OS input or human-readability claim.",passed=error is null,error,checks=_results,results=_results,retainedQaSavePaths=_savePaths},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(error is null?0:1);}
    }
    private static ContentSet DevelopmentContent(){var loaded=ContentLoader.Load(new[]{new ContentSource("labelled-colony-content",File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/colony-development-content.json")))},requireDevelopment:true);return loaded.Content??throw new InvalidOperationException(string.Join("; ",loaded.Errors));}
    private async Task PolishHistoryChecks()
    {
        var content=DevelopmentContent();
        var state=GalaxyGenerator.CreateNewGame(content,new("small",42,5,"harl")).State!;
        bool historical=true;
        foreach(bool pending in new[]{true,false})
        {
            var colony=state.Colonies[0];colony.Workforce=pending?new(0,colony.Population,0):new(colony.Population,0,0);
            var view=EmpireViewProjector.Project(content,state,state.Empires[0].Id);
            var issues=pending?ImmutableArray<GovernorIssue>.Empty:ImmutableArray.Create(new GovernorIssue("support_shortage",null));
            var entry=new GovernorReportEntry(colony.Id,new(6,0,0),new(2,3,1),null,null,issues,true,false);
            var report=new GovernorTurnReport(0,ImmutableArray.Create(entry),0);
            view=view with{Development=view.Development! with{PendingReport=pending?report:null,CompletedReports=pending?ImmutableArray<GovernorTurnReport>.Empty:ImmutableArray.Create(report)}};
            string before=SaveGame.Serialize(content,state);
            var panel=new GovernorReportPanel{CustomMinimumSize=new(740,400)};AddChild(panel);panel.ShowReports(content,view);await Frames();
            string text=Descendants<Label>(panel).Single(l=>l.Text.Contains("unchanged",StringComparison.Ordinal)).Text;
            historical&=text.Contains(pending?"0 conditions recorded in this report":"1 condition recorded in this report",StringComparison.Ordinal)&&!text.Contains("unresolved conditions",StringComparison.Ordinal);
            Check(Descendants<Label>(panel).Any(l=>l.Text.Contains("Overridden by a manual workforce order.",StringComparison.Ordinal)),"Recorded manual override detail remains available");
            Check((colony.Workforce.Support==0)==(view.Colonies[0].Economy.SupportDeficit>0),"Labelled current shortage differs from stored report condition count");
            GetTree().Root.Size=new(800,600);await Frames();await Capture(pending?"polish-history-pending":"polish-history-resolved");
            panel.Preferences=ColonyNotificationPreferences.Defaults.WithLevel("governor_actions",NotificationLevel.Off);panel.ShowReports(content,view);await Frames();
            Check(Descendants<Label>(panel).Any(l=>l.Text.Contains("Stored details remain available",StringComparison.Ordinal))&&Descendants<Label>(panel).Any(l=>l.Text.Contains("Overridden by a manual workforce order.",StringComparison.Ordinal)),"Off retains accessible historical override records");
            Check(SaveGame.Serialize(content,state)==before,"Historical report rendering and Off do not mutate campaign");
            RemoveChild(panel);panel.QueueFree();await Frames();
        }
        Check(historical,"Pending and resolved summaries identify recorded historical conditions, not current unresolved issues");
    }
    private async Task PolishTemplateDeletionChecks()
    {
        var content=DevelopmentContent();var state=GalaxyGenerator.CreateNewGame(content,new("small",42,5,"harl")).State!;
        var owner=state.Empires[0];var first=state.Colonies[0];var home=state.FindPlanet(first.PlanetId)!;
        var definition=content.GovernorTemplates["industrial"];
        var template=new GovernorTemplateSnapshot("custom_1",1,new("Referenced factory plan",definition.FoundationIds,definition.DevelopmentIds,definition.SupportTarget,definition.ProductionWeight,definition.ResearchWeight));
        owner.Development!.CustomTemplates.Add(template);owner.Development.NextTemplateNumber=2;owner.Development.NewColonyPlan=new(template,false,false);first.Development!.Template=template;
        for(int i=1;i<30;i++)
        {
            var planet=new PlanetState{Id=new(state.AllocateId()),StarId=home.StarId,Orbit=state.Planets.Where(p=>p.StarId==home.StarId).Max(p=>p.Orbit)+1,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea"};planet.Appearance=PlanetAppearance.Create(42,planet.Id);state.Planets.Add(planet);
            var colony=first.Clone();colony.Id=new(state.AllocateId());colony.PlanetId=planet.Id;state.Colonies.Add(colony);
        }
        ObservationUpdater.Refresh(state,0);Check(GameStateValidator.Validate(content,state).Count==0,"Labelled30-reference deletion fixture validates before session");
        string name=$"qa-{_runId}-template-references",path=SaveFiles.PathFor(name);SaveGame.WriteFile(path,content,state);_savePaths.Add(path);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();Press(Button(One<NewGameScreen>(main),"Start"));await Frames();
        var screen=await LoadThroughDialog(main,name);screen.NotificationPreferencesPath=Path.Combine(_output,"template-references-preferences.json");string before=Snapshot(Session(screen));
        await Click(screen,"Delete custom template");
        var dialog=Descendants<AcceptDialog>(screen).FirstOrDefault(d=>d.Name=="TemplateReferencesDialog"&&d.Visible);
        Check(dialog is not null,"Actual rejected Delete opens scrollable affected-colony/inheritance dialog");
        var view=Session(screen).View(owner.Id);var labels=Descendants<Label>(dialog!).Where(l=>l.Name.ToString().StartsWith("TemplateReference_",StringComparison.Ordinal)).ToArray();
        Check(labels.Length==30&&labels.Select(l=>l.Text).Distinct(StringComparer.Ordinal).Count()==30,"Deletion explains all30 distinct owned colony references");
        Check(labels.Select(l=>l.Text).SequenceEqual(view.Colonies.OrderBy(c=>c.Id).Select(c=>view.Research.ColonyNames[c.Id.ToString()]+" ("+c.Id+")")),"Deletion names and IDs follow stable colony order");
        Check(Descendants<Label>(dialog!).Any(l=>l.Text.Contains("New-colony inheritance",StringComparison.Ordinal)),"Deletion identifies assigned inheritance even with both switches off");
        foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})
        {
            GetTree().Root.Size=size;dialog!.PopupCenteredClamped(new(650,500));await Frames();await Reach(labels[^1]);
            Check(Visible(labels[^1])&&Visible(dialog.GetOkButton()),$"All deletion references and Close reachable at{size.X}");await Capture($"polish-template-references-{size.X}");
        }
        Check(Snapshot(Session(screen))==before,"Rejected UI deletion preserves canonical campaign and Ready");
        var missing=view.Colonies[0].Id;
        var fallback=view with{Research=view.Research with{ColonyNames=view.Research.ColonyNames.Remove(missing.ToString())}};
        ((TemplateReferencesDialog)dialog!).ShowReferences(fallback,new(view.Colonies.Select(c=>c.Id).Reverse().Append(new ColonyId(int.MaxValue)).ToImmutableArray(),true));await Frames();
        var fallbackLabels=Descendants<Label>(dialog!).Where(l=>l.Name.ToString().StartsWith("TemplateReference_",StringComparison.Ordinal)).ToArray();
        Check(fallbackLabels.Length==30&&fallbackLabels[0].Text=="Unnamed colony ("+missing+")"&&!fallbackLabels.Any(l=>l.Text.Contains(int.MaxValue.ToString(),StringComparison.Ordinal)),"Reordered diagnostic references retain own-ID fallback and omit foreign/unknown identities");
        dialog!.Hide();RemoveChild(main);main.QueueFree();await Frames();
    }
    private async Task PolishNotificationChecks()
    {
        foreach(bool vessel in new[]{false,true})
        {
            var content=DevelopmentContent();var state=GalaxyGenerator.CreateNewGame(content,new("small",42,5,"harl")).State!;
            var owner=state.Empires[0];var colony=state.Colonies[0];string target=vessel?"scout":"fabrication_hall";
            colony.ProductionReserve=10000;colony.ProjectId=target;colony.ProjectKind=vessel?"vessel":"building";colony.Development!.Origin=ProductionOrigin.Manual;
            var d=content.GovernorTemplates["industrial"];colony.Development.Template=new("industrial",1,new(d.Name,d.FoundationIds,d.DevelopmentIds,d.SupportTarget,d.ProductionWeight,d.ResearchWeight));colony.Development.WorkforceEnabled=true;
            owner.ResearchReserve=10000;owner.ResearchTargetId="cultivation_methods";
            Check(GameStateValidator.Validate(content,state).Count==0,"Labelled notification completion fixture validates before session");
            string kind=vessel?"vessel":"building",name=$"qa-{_runId}-notification-{kind}",path=SaveFiles.PathFor(name);SaveGame.WriteFile(path,content,state);_savePaths.Add(path);
            var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();Press(Button(One<NewGameScreen>(main),"Start"));await Frames();
            var screen=await LoadThroughDialog(main,name);string preferencesPath=Path.Combine(_output,$"attention-{kind}.json");screen.NotificationPreferencesPath=preferencesPath;
            async Task SetLevels(NotificationLevel level)
            {
                await Click(screen,"Colony notifications");var dialog=One<ColonyNotificationDialog>(screen);
                foreach(string category in new[]{"governor_actions","completions"}){var picker=Picker(dialog,"Notify_"+category);picker.Select((int)level);picker.EmitSignal(OptionButton.SignalName.ItemSelected,(int)level);}
                Press(dialog.GetOkButton());await Frames();
            }
            bool Badge(string id)=>Descendants<Label>(One<ColonyList>(screen)).Any(l=>l.Name==id&&l.Visible);
            async Task ReopenReport(){UiNavigation.GameMenu(screen,2);await Frames();}
            string canonical=Snapshot(Session(screen));await SetLevels(NotificationLevel.Attention);
            Check(!Badge("CompletionAttentionBadge"),"Attention does not fabricate completed work from a finishable forecast");
            Check(Snapshot(Session(screen))==canonical,"Changing notification levels preserves campaign and Ready");
            await SetLevels(NotificationLevel.Summary);await EndTurn(screen);var report=One<TurnReportDialog>(screen);
            Label[] Notices()=>Descendants<Label>(report).Where(l=>l.Text.Contains("Completed ",StringComparison.Ordinal)||l.Text.Contains("Researched ",StringComparison.Ordinal)||l.Text.Contains("Expedition completed",StringComparison.Ordinal)).ToArray();
            Check(Notices().Length==2&&Notices().All(l=>l.ThemeTypeVariation!="WarningLabel"),"Summary displays exactly two completion notices including any expedition wording");
            Check(!Badge("GovernorActionAttentionBadge")&&!Badge("CompletionAttentionBadge"),"Summary does not raise routine Attention badges");
            if(vessel)
            {
                ColonyId? chosen=null;VesselId? inspected=null;
                report.ChooseProjectRequested+=id=>chosen=id;report.VesselRequested+=id=>inspected=id;
                var choose=Button(report,"Choose next project");choose=await Reach(choose);Press(choose);await Frames();
                Check(chosen==colony.Id,"Unified vessel completion retains choose-next-project navigation");
                await ReopenReport();report=One<TurnReportDialog>(screen);
                var inspect=Button(report,"View vessel");inspect=await Reach(inspect);Press(inspect);await Frames();
                Check(Session(screen).State.Vessels.Any(v=>v.Id==inspected&&v.EmpireId==owner.Id&&v.RoleId==target&&!state.Vessels.Any(before=>before.Id==v.Id)),"Unified vessel completion retains completed-vessel navigation");
                await ReopenReport();report=One<TurnReportDialog>(screen);
            }
            report.Hide();await Frames();canonical=Snapshot(Session(screen));await SetLevels(NotificationLevel.Attention);
            Check(Badge("GovernorActionAttentionBadge")&&Badge("CompletionAttentionBadge"),"Attention raises nonblocking recorded-action and actual-completion badges");
            Check(Descendants<Label>(One<ColonyList>(screen)).Single(l=>l.Name=="CompletionAttentionBadge").Text.Contains("2 completions",StringComparison.Ordinal),"Production and research count once each, without duplicate vessel events");
            await ReopenReport();report=One<TurnReportDialog>(screen);
            Check(Notices().Length==2&&Notices().All(l=>l.ThemeTypeVariation=="WarningLabel"&&l.Text.StartsWith("Attention:",StringComparison.Ordinal)),"Attention highlights exactly two completion notices including any expedition wording");
            Check(Descendants<Label>(report).Single(l=>l.Name=="GovernorReportSummary").ThemeTypeVariation=="WarningLabel","Attention visibly highlights recorded governor actions");
            GetTree().Root.Size=new(800,600);await Frames();await Capture($"polish-attention-{kind}-800");report.Hide();await Frames();GetTree().Root.Size=new(1920,1080);await Frames();await Capture($"polish-attention-{kind}-1920");
            await SetLevels(NotificationLevel.Off);Check(!Badge("GovernorActionAttentionBadge")&&!Badge("CompletionAttentionBadge"),"Off suppresses routine Attention badges");
            await ReopenReport();report=One<TurnReportDialog>(screen);Check(Notices().Length==0,"Off suppresses all completion notices including expedition wording");
            Check(Descendants<Label>(report).Any(l=>l.Text.Contains("Stored details remain available",StringComparison.Ordinal)),"Off keeps stored governor detail available");report.Hide();await Frames();
            Check(Snapshot(Session(screen))==canonical,"Summary Attention Off and reopening reports change no campaign bytes");
            await SetLevels(NotificationLevel.Attention);string saved=await SaveThroughDialog(screen,$"attention-{kind}-history");string savedCanonical=Snapshot(Session(screen));
            await EndTurn(screen);One<TurnReportDialog>(screen).Hide();await Frames();Check(!Badge("CompletionAttentionBadge"),"Next resolution replaces stale completion badge with actual zero completions");
            screen=await LoadThroughDialog(main,saved);screen.NotificationPreferencesPath=preferencesPath;await SetLevels(NotificationLevel.Attention);
            Check(Snapshot(Session(screen))==savedCanonical&&Badge("GovernorActionAttentionBadge"),"Saved governor history and Attention survive actual UI reload");
            Check(!Badge("CompletionAttentionBadge")&&!Descendants<Button>(screen).Any(b=>b.Name=="ViewLatestTurnReport"),"Reload does not invent an unsaved economic turn report");
            RemoveChild(main);main.QueueFree();await Frames();
        }
    }
    private async Task ReplacementPreviewChecks()
    {
        var mismatches=new System.Collections.Generic.List<string>();
        foreach(bool bulk in new[]{false,true})
        {
            var content=DevelopmentContent();
            var state=GalaxyGenerator.CreateNewGame(content,new("small",42,5,"wenth")).State!;
            var owner=state.Empires[0];var colony=state.Colonies[0];
            owner.KnownTechnologyIds=content.Technologies.Keys.ToList();
            var planet=state.FindPlanet(colony.PlanetId)!;planet.PlanetTypeId="verdant_world";planet.EnvironmentId="shelf_sea";
            colony.Population=6;colony.Workforce=new(6,0,0);
            colony.CompletedBuildingIds=new(){"analysis_lab","cultivation_hub","fabrication_hall","habitat_extension"};
            colony.ProjectId="daughter_seed";colony.ProjectKind="vessel";colony.ProductionReserve=123;colony.Development!.Origin=ProductionOrigin.Manual;
            state.Empires.Add(new(){Id=new(state.AllocateId()),Controller=ControllerKind.Human,SpeciesId="harl",Development=new()});
            ObservationUpdater.Refresh(state,0);
            Check(GameStateValidator.Validate(content,state).Count==0,"Labelled replacement-preview fixture validates before session");
            string mode=bulk?"bulk":"individual",name=$"qa-{_runId}-replacement-{mode}",path=SaveFiles.PathFor(name);
            SaveGame.WriteFile(path,content,state);_savePaths.Add(path);
            var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();Press(Button(One<NewGameScreen>(main),"Start"));await Frames();
            var screen=await LoadThroughDialog(main,name);screen.NotificationPreferencesPath=Path.Combine(_output,$"replacement-{mode}-preferences.json");
            Label preview;
            if(bulk)
            {
                await Click(screen,"Manage colonies");var dialog=One<EmpireManagementDialog>(screen);
                var list=Descendants<ItemList>(dialog).Single(l=>l.Name=="ManagementColonyList");list.Select(0);list.EmitSignal(ItemList.SignalName.MultiSelected,0,true);
                Select(Picker(dialog,"BulkTemplatePicker"),"research");Toggle(dialog,"BulkReplaceActive",true);await Frames();
                preview=Descendants<Label>(dialog).Single(l=>l.Text.Contains("S/P/R",StringComparison.Ordinal));
            }
            else
            {
                Select(Picker(screen,"GovernorTemplatePicker"),"research");await Click(screen,"Apply template");
                Toggle(screen,"WorkforceGovernorToggle",true);await Frames();Toggle(screen,"ConstructionGovernorToggle",true);await Frames();Toggle(screen,"ReplaceActiveProject",true);await Frames();
                preview=Descendants<Label>(One<ColonyPanel>(screen)).Single(l=>l.Text.StartsWith("Preview Support/Production/Research",StringComparison.Ordinal));
            }
            string text=preview.Text;
            if(!text.Contains("to 2/1/3",StringComparison.Ordinal)||!text.Contains("next Ready: Discovery Center",StringComparison.Ordinal)||text.Contains("Active work retained",StringComparison.Ordinal))mismatches.Add(mode+": "+text);
            preview=await Reach(preview);await Capture($"replacement-{mode}-preview");
            if(bulk){var dialog=One<EmpireManagementDialog>(screen);await Reach(Button(dialog,"Apply to selected colonies"));Press(Button(dialog,"Apply to selected colonies"));await Frames();dialog.Hide();}
            else await Click(screen,"Apply template");
            var applied=Session(screen).State.FindColony(colony.Id)!;
            Check(applied.ProjectId is null&&applied.ProductionReserve==123,$"{mode}: replacement clears target immediately and retains reserve");
            await EndPlanningWithoutResolution(screen);
            var prepared=Session(screen).State.FindColony(colony.Id)!;
            Check(prepared.Workforce==new Workforce(2,1,3)&&prepared.ProjectId=="discovery_center"&&prepared.ProductionReserve==123,$"{mode}: actual next Ready prepares Discovery Center and2/1/3 without spending reserve");
            RemoveChild(main);main.QueueFree();await Frames();
        }
        Check(mismatches.Count==0,"Both replacement controls forecast actual next Ready: "+string.Join(" | ",mismatches));
    }
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
            GetTree().Root.Size=size;await Frames();Select(Picker(screen,"GovernorTemplatePicker"),"custom_1");foreach(string action in new[]{"Apply template","Copy template","Edit custom template","Delete custom template","Edit queue","Template presets"}){var b=Descendants<Button>(One<ColonyPanel>(screen)).Single(b=>b.Text==action);if(!b.Disabled){b=await Reach(b);Check(Visible(b),action+" focus reachable at"+size);}}
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
    private async Task<T> Reach<T>(T control) where T:Control{control=UiNavigation.Reveal(control);if(control.FocusMode!=Control.FocusModeEnum.None)control.GrabFocus();await Frames();for(Node? p=control.GetParent();p is not null;p=p.GetParent())if(p is ScrollContainer scroll)scroll.EnsureControlVisible(control);await Frames();return control;}
    private async Task Click(Node node,string text){var b=Button(node,text);b=await Reach(b);Press(b);await Frames();}
    private static bool Visible(Control c)=>c.IsVisibleInTree()&&c.GetViewport().GetVisibleRect().Encloses(c.GetGlobalRect());
    private async Task BlockedCargoControls() {
        var content=DevelopmentContent();var generated=GalaxyGenerator.CreateNewGame(content,new("small",42,5,"wenth"));Check(generated.Succeeded,"Labelled Wenth initial campaign generates normally");string name=$"qa-{_runId}-colony-wenth-initial";string path=SaveFiles.PathFor(name);SaveGame.WriteFile(path,content,generated.State!);_savePaths.Add(path);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();Press(Button(One<NewGameScreen>(main),"Start"));await Frames();var screen=await LoadThroughDialog(main,name);
        await Click(screen,"Edit queue");var queue=One<ConstructionQueueEditor>(screen);Select(Picker(queue,"QueueTargetPicker"),"vessel|daughter_seed");Press(Button(queue,"Add project"));Press(queue.GetOkButton());await Frames();Toggle(screen,"ConstructionGovernorToggle",true);await Frames();await EndTurn(screen);One<TurnReportDialog>(screen).Hide();await Frames();
        var c=Session(screen).State.Colonies[0];Check(c.ProjectId=="daughter_seed"&&c.Development!.Queue.Count==1,"Unembarkable explicit vessel remains active and queued");Check(Descendants<Label>(One<ColonyPanel>(screen)).Any(l=>l.Text.Contains("Awaiting embarkation",StringComparison.Ordinal)),"Blocked cargo explains waiting through ordinary colony UI");
        GetTree().Root.Size=new(800,600);await Frames();var label=Descendants<Label>(One<ColonyPanel>(screen)).First(l=>l.Text.Contains("Awaiting embarkation",StringComparison.Ordinal));label=await Reach(label);await Capture("colony-wenth-cargo-800");
        var legacy=SaveGame.ReadFile(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/legacy-rules4-v5-save.json")).Game!;string oldName=$"qa-{_runId}-colony-original-rules4";string oldPath=SaveFiles.PathFor(oldName);SaveGame.WriteFile(oldPath,legacy.Content,legacy.State);_savePaths.Add(oldPath);screen=await LoadThroughDialog(main,oldName);Check(!Descendants<OptionButton>(screen).Any(p=>p.Name=="GovernorTemplatePicker")&&Session(screen).State.RulesVersion==4,"Loading old campaign preserves original rules and hides unavailable controls");
        RemoveChild(main);main.QueueFree();await Frames();
    }
    private async Task ManagementChecks() {
        foreach(int count in new[]{1,6,11,30}) {
            var content=DevelopmentContent();var generated=GalaxyGenerator.CreateNewGame(content,new("small",42,5,"harl"));var state=generated.State!;var owner=state.Empires[0];owner.KnownTechnologyIds=content.Technologies.Keys.ToList();var first=state.Colonies[0];first.Workforce=new(first.Population,0,0);first.CompletedBuildingIds=new(){"analysis_lab","cultivation_hub","fabrication_hall"};var home=state.FindPlanet(first.PlanetId)!;
            for(int i=1;i<count;i++){var planet=new PlanetState{Id=new(state.AllocateId()),StarId=home.StarId,Orbit=state.Planets.Where(p=>p.StarId==home.StarId).Max(p=>p.Orbit)+1,PlanetTypeId="verdant_world",EnvironmentId="shelf_sea"};planet.Appearance=PlanetAppearance.Create(42,planet.Id);state.Planets.Add(planet);var colony=first.Clone();colony.Id=new(state.AllocateId());colony.PlanetId=planet.Id;state.Colonies.Add(colony);}
            ObservationUpdater.Refresh(state,0);Check(GameStateValidator.Validate(content,state).Count==0,$"Labelled {count}-colony fixture validates before loading");string name=$"qa-{_runId}-management-{count}";string path=SaveFiles.PathFor(name);SaveGame.WriteFile(path,content,state);_savePaths.Add(path);
            var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();Press(Button(One<NewGameScreen>(main),"Start"));await Frames();var screen=await LoadThroughDialog(main,name);screen.NotificationPreferencesPath=Path.Combine(_output,$"preferences-{count}.json");
            Check(Descendants<Button>(screen).Any(b=>b.Name=="ManageColonies"),"Actual empire-management entry exists for"+count);
            string unchanged=Snapshot(Session(screen));await Click(screen,"Manage colonies");var management=One<EmpireManagementDialog>(screen);var list=Descendants<ItemList>(management).Single(l=>l.Name=="ManagementColonyList");Check(list.ItemCount==count,$"Management lists all {count} owned colonies");Select(Picker(management,"ManagementFilter"),"manual");await Frames();Check(list.ItemCount==count,"Initial manual filter is read-only");Check(Snapshot(Session(screen))==unchanged,"Opening/filtering dashboard issues no command");Select(Picker(management,"ManagementFilter"),"all");
            if(count>1){Select(Picker(management,"BulkTemplatePicker"),"industrial");for(int i=1;i<count;i++){list.Select(i,false);list.EmitSignal(ItemList.SignalName.MultiSelected,i,true);}Press(Button(management,"Apply to selected colonies"));await Frames();Check(Session(screen).State.Colonies[0].Development!.Template is null,"Favorite colony remains manual outside bulk selection");Check(Session(screen).State.Colonies.Skip(1).All(c=>c.Development is {WorkforceEnabled:true,ConstructionEnabled:true,Template.Id:"industrial"}),"Bulk apply delegates only selected colonies");Select(Picker(management,"ManagementFilter"),"template:industrial");await Frames();Check(list.ItemCount==count-1,"Template filter shows assigned colonies");Select(Picker(management,"ManagementFilter"),"exceptions");await Frames();Check(list.ItemCount==0,"Healthy colonies produce no exception rows");Select(Picker(management,"ManagementFilter"),"all");}
            foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)}){GetTree().Root.Size=size;await Frames();management.ShowEmpire(content,Session(screen).View(owner.Id));await Frames();Check(Visible(management.GetOkButton()),$"Management Close reachable with {count} colonies at {size}");await Capture($"management-{count}-{size.X}");}
            management.Hide();await Frames();if(count>=6){Check(Descendants<Control>(One<ColonyList>(screen)).Count(c=>c.Name=="GovernorSizeSuggestion"&&c.Visible)==1,"One nonmodal size suggestion is visible");await Click(screen,"Dismiss suggestion");Check(!Descendants<Control>(One<ColonyList>(screen)).Any(c=>c.Name=="GovernorSizeSuggestion"&&c.Visible),"Size suggestion can be cleared without a command");}
            unchanged=Snapshot(Session(screen));await Click(screen,"Colony notifications");var preferences=One<ColonyNotificationDialog>(screen);var category=Picker(preferences,"Notify_governor_actions");category.Select(2);category.EmitSignal(OptionButton.SignalName.ItemSelected,2);Press(preferences.GetOkButton());await Frames();Check(File.Exists(screen.NotificationPreferencesPath)&&ColonyNotificationPreferences.Load(screen.NotificationPreferencesPath).LevelFor("governor_actions")==NotificationLevel.Off,"Explicit Apply saves only injected local preference path");Check(Snapshot(Session(screen))==unchanged,"Notification preference changes leave campaign bytes unchanged");
            if(count==6){for(int i=0;i<5;i++){var increase=Step(One<ColonyPanel>(screen),"Move a worker from support to production");increase=await Reach(increase);Press(increase);await Frames();}Check(Session(screen).View(owner.Id).Colonies[0].Economy.SupportDeficit>0,"Manual favorite can expose an unresolved support condition");await Click(screen,"Colony notifications");preferences=One<ColonyNotificationDialog>(screen);var shortage=Picker(preferences,"Notify_support_shortage");shortage.Select(2);shortage.EmitSignal(OptionButton.SignalName.ItemSelected,2);Press(preferences.GetOkButton());await Frames();await Click(screen,"Manage colonies");management=One<EmpireManagementDialog>(screen);Select(Picker(management,"ManagementFilter"),"exceptions");await Frames();Check(Descendants<ItemList>(management).Single(l=>l.Name=="ManagementColonyList").ItemCount==1,"Off notification does not hide derived dashboard exceptions");await Capture("management-off-still-shows-shortage");management.Hide();}
            if(count==30){for(int turn=0;turn<12;turn++){await EndTurn(screen);Check(Descendants<TurnReportDialog>(screen).Count(d=>d.Visible)==1,"Thirty worlds share one turn report, without individual acknowledgements");One<TurnReportDialog>(screen).Hide();await Frames();}Check(Session(screen).State.Empires[0].Development!.CompletedReports.Count==10,"Real turns retain exactly ten governor reports");await Click(screen,"Manage colonies");management=One<EmpireManagementDialog>(screen);Check(Picker(management,"GovernorHistory").ItemCount==10,"Ten-turn history is accessible despite routine Off");var expand=Descendants<CheckButton>(management).Single(b=>b.Name=="ExpandGovernorDetails");expand=await Reach(expand);expand.ButtonPressed=true;await Frames();await Capture("management-thirty-history");management.Hide();string saved=await SaveThroughDialog(screen,"management-thirty-history");string expected=Snapshot(Session(screen));screen=await LoadThroughDialog(main,saved);screen.NotificationPreferencesPath=Path.Combine(_output,$"preferences-{count}.json");Check(Snapshot(Session(screen))==expected,"Management history survives actual Save/Load");}
            RemoveChild(main);main.QueueFree();await Frames();
        }
    }
    private void PreferenceChecks() {
        string path=Path.Combine(_output,"invalid-preferences.json");File.WriteAllText(path,"{\"format\":\"OpenAntaresColonyNotifications\",\"version\":999999999999999999999999,\"levels\":{}}");
        var prefs=ColonyNotificationPreferences.Load(path);Check(prefs.Diagnostics.Length>0&&prefs.LevelFor("support_shortage")==NotificationLevel.Attention,"Malformed preference version safely restores defaults");
        foreach(string malformed in new[]{"[]","{}","{invalid","{\"format\":\"OpenAntaresColonyNotifications\",\"version\":1,\"version\":1,\"levels\":{}}"}){File.WriteAllText(path,malformed);var loaded=ColonyNotificationPreferences.Load(path);Check(loaded.Diagnostics.Length>0&&loaded.LevelFor("construction_locked")==NotificationLevel.Attention,"Malformed or repeated preference fields restore safe defaults");}
        string missing=Path.Combine(_output,"missing-preferences.json");Check(ColonyNotificationPreferences.Load(missing).Diagnostics.Length>0&&!File.Exists(missing),"Missing preferences do not create a file");
        var changed=ColonyNotificationPreferences.Defaults.WithLevel("governor_actions",NotificationLevel.Off);string valid=Path.Combine(_output,"preference-roundtrip.json");changed.Save(valid);Check(ColonyNotificationPreferences.Load(valid).LevelFor("governor_actions")==NotificationLevel.Off,"Immutable preference copy round-trips");
        File.WriteAllText(valid,"prior preference contents");bool failed=false;using(var locked=new FileStream(valid,FileMode.Open,System.IO.FileAccess.Read,FileShare.Read)){try{changed.Save(valid);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){failed=true;}}Check(failed&&File.ReadAllText(valid)=="prior preference contents"&&!File.Exists(valid+".openantares-tmp"),"Failed local preference write preserves destination and cleans its own temporary file");
    }
    private async Task ManualOverrideDigest() {
        var content=DevelopmentContent();var state=GalaxyGenerator.CreateNewGame(content,new("small",42,5,"harl")).State!;var owner=state.Empires[0];owner.KnownTechnologyIds=content.Technologies.Keys.ToList();var c=state.Colonies[0];c.CompletedBuildingIds=new(){"analysis_lab","cultivation_hub","fabrication_hall"};c.Workforce=new(c.Population,0,0);var definition=content.GovernorTemplates["industrial"];c.Development!.Template=new("industrial",1,new(definition.Name,definition.FoundationIds,definition.DevelopmentIds,definition.SupportTarget,definition.ProductionWeight,definition.ResearchWeight));c.Development.WorkforceEnabled=true;c.Development.ConstructionEnabled=true;state.Empires.Add(new EmpireState{Id=new(state.AllocateId()),Controller=ControllerKind.Human,SpeciesId="harl",Development=new()});
        var prepared=OpenAntares.Simulation.Commands.CommandProcessor.Execute(content,state,new OpenAntares.Simulation.Commands.SetReadyCommand(owner.Id,0,true));Check(prepared.Accepted&&prepared.Report is null,"Labelled two-human state prepares only first human by ordinary Ready");string name=$"qa-{_runId}-management-pending-human";string path=SaveFiles.PathFor(name);SaveGame.WriteFile(path,content,prepared.State);_savePaths.Add(path);
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();Press(Button(One<NewGameScreen>(main),"Start"));await Frames();var screen=await LoadThroughDialog(main,name);screen.NotificationPreferencesPath=Path.Combine(_output,"manual-override-preferences.json");await EndPlanningWithoutResolution(screen);await EndPlanningWithoutResolution(screen);string pendingName=await SaveThroughDialog(screen,"labelled-two-human-pending-ready");string pendingCanonical=Snapshot(Session(screen));screen=await LoadThroughDialog(main,pendingName);screen.NotificationPreferencesPath=Path.Combine(_output,"manual-override-preferences.json");Check(Snapshot(Session(screen))==pendingCanonical&&Session(screen).State.Empires[0].Ready,"Labelled two-human actual nonfinal Ready survives UI Save/Load");var increase=Step(One<ColonyPanel>(screen),"Move a worker from support to production");increase=await Reach(increase);Press(increase);await Frames();
        Check(Session(screen).State.Empires[0].Development!.PendingReport!.Entries.Single().WorkforceOverridden,"Actual manual control marks prepared workforce action overridden");await Click(screen,"Manage colonies");var management=One<EmpireManagementDialog>(screen);var expand=Descendants<CheckButton>(management).Single(b=>b.Name=="ExpandGovernorDetails");expand=await Reach(expand);expand.ButtonPressed=true;await Frames();Check(Descendants<Label>(management).Any(l=>l.Text.Contains("Overridden by a manual workforce order.",StringComparison.Ordinal)),"Stored digest does not attribute manual intervention to governor success");var explanation=Descendants<Label>(management).Single(l=>l.Text.Contains("Overridden by a manual workforce order.",StringComparison.Ordinal));explanation=await Reach(explanation);Check(Visible(explanation),"Manual override explanation is reachable by scrolling");await Capture("management-manual-override");RemoveChild(main);main.QueueFree();await Frames();
    }
    private async Task PortraitChecks() {
        // The system view now draws globes; the picture portrait lives on in the colony-management dialog.
        async Task<TextureRect> OwnedPortrait(GameScreen current) {
            var game=Session(current);var viewer=game.State.Empires.First(e=>e.Controller==ControllerKind.Human).Id;
            var view=game.View(viewer);var owned=view.Colonies[0];
            var management=One<EmpireManagementDialog>(current);if(!management.Visible)await Click(current,"Manage colonies");
            var list=Descendants<ItemList>(management).Single(l=>l.Name=="ManagementColonyList");
            int index=view.Colonies.OrderBy(c=>c.Id).ToList().FindIndex(c=>c.Id==owned.Id);
            list.DeselectAll();list.Select(index);list.EmitSignal(ItemList.SignalName.MultiSelected,index,true);await Frames();
            var portrait=Descendants<PlanetPortrait>(management).Single(p=>p.Name=="ManagementPortraitSlot");
            return Descendants<TextureRect>(portrait).Single(t=>t.Name=="PlanetImage");
        }
        var content=DevelopmentContent();var state=GalaxyGenerator.CreateNewGame(content,new("small",42,5,"harl")).State!;string name=$"qa-{_runId}-portrait-initial";string path=SaveFiles.PathFor(name);SaveGame.WriteFile(path,content,state);_savePaths.Add(path);var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();Press(Button(One<NewGameScreen>(main),"Start"));await Frames();var screen=await LoadThroughDialog(main,name);
        string before=Snapshot(Session(screen));var ownedImage=await OwnedPortrait(screen);Check(ownedImage.Texture is not null,"Actual surveyed/owned planet portrait image exists");string pixels=Pixels(ownedImage.Texture!);
        foreach(var dimensions in new[]{new Vector2I(800,600),new Vector2I(1920,1080)}){GetTree().Root.Size=dimensions;await Frames();ownedImage=await Reach(ownedImage);await Capture("portrait-owned-"+dimensions.X);Check(Pixels(ownedImage.Texture!)==pixels,"Resizing preserves observed portrait signature");}
        One<EmpireManagementDialog>(screen).Hide();await Frames();
        Check(Snapshot(Session(screen))==before,"Rendering and resizing do not mutate state, RNG or counters");string saved=await SaveThroughDialog(screen,"portrait-signature");screen=await LoadThroughDialog(main,saved);Check(Pixels((await OwnedPortrait(screen)).Texture!)==pixels,"Actual Save/Load preserves identical portrait pixels");One<EmpireManagementDialog>(screen).Hide();await Frames();Check(Snapshot(Session(screen))==before,"Portrait Save/Load preserves canonical campaign");
        var shared=(PlanetPortraitCache)typeof(GameScreen).GetField("_portraitCache",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(screen)!;await Click(screen,"Manage colonies");var management=One<EmpireManagementDialog>(screen);var list=Descendants<ItemList>(management).Single(l=>l.Name=="ManagementColonyList");Check(list.GetItemIcon(0) is not null,"Owned management thumbnail uses permitted appearance");list.Select(0);list.EmitSignal(ItemList.SignalName.MultiSelected,0,true);await Frames();Check(shared.Count<=128,"Mounted inspection and management share one bounded cache");await Capture("portrait-management");management.Hide();RemoveChild(main);main.QueueFree();await Frames();Check(shared.Count==0,"Leaving GameScreen clears its shared cache ownership");
        var styles=PlanetStyleCatalog.Load("res://art/planets/planet_styles.json");var cache=new PlanetPortraitCache();var neutral=new PlanetPortrait();AddChild(neutral);neutral.Configure(styles,cache);neutral.ShowPlanet(null);await Frames();Check(Descendants<TextureRect>(neutral).Single().Texture is null,"Unknown portrait is neutral and does not invent a descriptor");RemoveChild(neutral);neutral.QueueFree();await Frames();
        var known=SessionForStale(content,state);var first=known.View.Stars.Single(s=>s.Chart.Id==known.Remote).Planets!.Value[0];var original=cache.Get(first.Appearance!,styles.Resolve(first.PlanetTypeId,first.EnvironmentId,first.Appearance!.Variant),new(96,96));var changed=known.State.Clone();changed.Planets.Single(p=>p.Id==first.Id).Appearance=new(1,7,ulong.MaxValue);var stale=EmpireViewProjector.Project(content,changed,changed.Empires[0].Id).Stars.Single(s=>s.Chart.Id==known.Remote).Planets!.Value[0];Check(first.Appearance==stale.Appearance&&Pixels(original)==Pixels(cache.Get(stale.Appearance!,styles.Resolve(stale.PlanetTypeId,stale.EnvironmentId,stale.Appearance!.Variant),new(96,96))),"Labelled stale illustration ignores hidden live descriptor changes");
        var provenance=new System.Collections.Generic.List<object>();int pages=0;foreach(var dimensions in new[]{new Vector2I(800,600),new Vector2I(1920,1080)})foreach(string type in content.PlanetTypes.Keys)foreach(string environment in content.Environments.Keys) {
            GetTree().Root.Size=dimensions;var gallery=new VBoxContainer();gallery.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);AddChild(gallery);gallery.AddChild(new Label{Text=$"Labelled visual gallery: {content.PlanetTypes[type].Name} / {content.Environments[environment].Name}",ThemeTypeVariation="HeaderLabel",AutowrapMode=TextServer.AutowrapMode.WordSmart,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill});var grid=new GridContainer{Columns=4,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill,SizeFlagsVertical=Control.SizeFlags.ExpandFill};gallery.AddChild(grid);int galleryPortraitSize=Math.Min(360,Math.Max(1,(dimensions.X-3*grid.GetThemeConstant("h_separation"))/4));var hashes=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            for(int variant=0;variant<8;variant++){var cell=new VBoxContainer{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill,SizeFlagsVertical=Control.SizeFlags.ExpandFill};grid.AddChild(cell);var portrait=new PlanetPortrait{CustomMinimumSize=new(galleryPortraitSize,galleryPortraitSize),RenderSize=new(256,256),SizeFlagsHorizontal=Control.SizeFlags.ExpandFill,SizeFlagsVertical=Control.SizeFlags.ExpandFill};cell.AddChild(portrait);portrait.Configure(styles,cache);var descriptor=new PlanetAppearanceDescriptor(1,variant,1201488599727174231UL);var planet=new KnownPlanetView(new(1000+variant),variant+1,type,0,0,0,true,null,null){EnvironmentId=environment,Appearance=descriptor};portrait.ShowPlanet(planet);cell.AddChild(new Label{Text=$"Signature {variant+1}",HorizontalAlignment=HorizontalAlignment.Center});hashes.Add(Pixels(Descendants<TextureRect>(portrait).Single().Texture!));}
            await Frames();Check(hashes.Count==8,$"Eight rendered structures for {type}/{environment} at{dimensions.X}");var galleryImages=Descendants<TextureRect>(gallery).Where(t=>t.Name=="PlanetImage").ToArray();var signatures=Descendants<Label>(grid).Where(l=>l.Text.StartsWith("Signature ",StringComparison.Ordinal)).ToArray();Check(galleryImages.Length==8&&signatures.Length==8&&galleryImages.All(t=>GetViewport().GetVisibleRect().Encloses(t.GetGlobalRect()))&&signatures.All(l=>GetViewport().GetVisibleRect().Encloses(l.GetGlobalRect())),"All eight gallery images and signatures fit at "+dimensions.X);Check(cache.Count<=128,"Gallery cache remains bounded");string file=$"gallery-{type}-{environment}-{dimensions.X}";await Capture(file);provenance.Add(new{type,environment,variants=8,size=dimensions.X,file=file+".png",method="Labelled presentation-only combinations, including zero generation weights."});pages++;RemoveChild(gallery);gallery.QueueFree();await Frames();
        }
        Check(pages==24,"All twelve gallery pages captured at both800 and1920");var held=new TextureRect();AddChild(held);var heldDescriptor=new PlanetAppearanceDescriptor(1,0,1234);var style=styles.Resolve("verdant_world","shelf_sea",0);held.Texture=cache.Get(heldDescriptor,style,new(64,64));string heldPixels=Pixels(held.Texture);for(ulong i=0;i<180;i++)cache.Get(new(1,(int)(i%8),i+100000),styles.Resolve("modded_planet","open_ocean",(int)(i%8)),new(64,64));Check(cache.Count==128,"More than128 distinct requests retain exactly128 cache entries");Check(GodotObject.IsInstanceValid(held.Texture)&&Pixels(held.Texture)==heldPixels,"Eviction retains a still-bound texture");cache.Clear();Check(cache.Count==0&&Pixels(held.Texture)==heldPixels,"Cache Clear drops ownership without invalidating visible textures");held.Texture=null;RemoveChild(held);held.QueueFree();await Frames();
        var modded=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);for(int i=0;i<8;i++){using var image=PlanetPortraitRenderer.Render(new(1,i,12345),styles.Resolve("renamed_modded_type","shelf_sea",i),new(64,64));modded.Add(Convert.ToHexString(SHA256.HashData(image.GetData())));}Check(modded.Count==8,"Generic modded type fallback retains eight structural signatures");Check(OpenAntares.Simulation.Saves.SaveGame.Serialize(content,state)==before,"Complete gallery/cache exercise leaves source campaign bytes unchanged");File.WriteAllText(Path.Combine(_output,"portrait-gallery.json"),JsonSerializer.Serialize(new{pages,coreCombinations=96,maximumCache=128,provenance},new JsonSerializerOptions{WriteIndented=true}));
    }
    private static string Pixels(Texture2D texture){using var image=texture.GetImage();return Convert.ToHexString(SHA256.HashData(image.GetData()));}
    private static (GameState State,EmpireView View,OpenAntares.Simulation.StarId Remote) SessionForStale(ContentSet content,GameState initial){var state=initial.Clone();var owner=state.Empires[0];var ship=state.Vessels[0];var home=ship.StationaryStarId;var remote=state.Stars.First(s=>s.Id!=home).Id;ship.StationaryStarId=remote;ObservationUpdater.Refresh(state,0);ship.StationaryStarId=home;state.Turn=1;ObservationUpdater.Refresh(state,1);return(state,EmpireViewProjector.Project(content,state,owner.Id),remote);}

    private async Task CurrentStartChecks() {
        var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();var setup=One<NewGameScreen>(main);setup.Configure("small",42);Press(Button(setup,"Start"));await Frames();var screen=One<GameScreen>(main);var session=Session(screen);Check(session.State.RulesVersion==12&&session.State.Crisis!.Enabled,"Actual current Start sends rules12 Standard");Check(session.Content.Buildings.Count==7&&session.Content.Technologies.Count==11&&session.Content.GovernorTemplates.Count==4&&session.Content.Plagues.Count==1,"Actual current Start loads seven buildings, eleven technologies, four defaults and the approved plague");Check(Picker(screen,"GovernorTemplatePicker").GetSelectedMetadata().AsString()=="balanced","Balanced suggestion is selected while initial colony remains manual");Check(session.State.Colonies.All(c=>c.Development is {WorkforceEnabled:false,ConstructionEnabled:false}&&c.CompletedBuildingIds.Count==0),"Current campaign starts with no automated takeover or free buildings");var frozen=FrozenContent.Rules5();Check(frozen.Plagues.IsEmpty&&GalaxyGenerator.CreateNewGame(frozen,new("small",42,5,"harl",1,"peldra")).Succeeded,"Frozen genuine rules5 content remains separately generatable");await Capture("current-rules12-start");RemoveChild(main);main.QueueFree();await Frames();
    }
    private async Task InvalidDevelopmentContentRecovery() {
        var content=ContentFiles.LoadCore().Content!;var missing=new NewGameScreen(content with{GovernorTemplates=ImmutableSortedDictionary<string,GovernorTemplateDefinition>.Empty}){Theme=GD.Load<Theme>("res://game/theme/openantares_theme.tres")};AddChild(missing);await Frames();Check(Button(missing,"Start").Disabled,"Missing current templates disables Start");bool load=false;missing.LoadRequested+=()=>load=true;Press(Button(missing,"Load Game"));Check(load,"Missing template definitions retain Load recovery");RemoveChild(missing);missing.QueueFree();await Frames();
        var sources=System.IO.Directory.GetFiles(ProjectSettings.GlobalizePath("res://content/core"),"*.json").Select(p=>new ContentSource(Path.GetFileName(p),File.ReadAllText(p)));
        var invalid=ContentLoader.Load(sources.Concat(Enumerable.Range(0,80).Select(i=>new ContentSource($"bad-template-{i}.json","{\"governor_templates\":[{\"id\":\"invalid_"+i+"\",\"name\":\"Invalid template\",\"foundation_ids\":[\"unknown_building\"],\"development_ids\":[],\"support_target\":\"minimum\",\"production_weight\":1,\"research_weight\":1}]}"))),requireDevelopment:true);Check(!invalid.Succeeded&&invalid.Errors.Count>=80,"Malformed defaults produce80 located load errors");
        GetTree().Root.Size=new(800,600);var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();typeof(Main).GetMethod("ShowContentErrors",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(main,new object[]{invalid});await Frames();Check(Visible(Button(main,"Load Game")),"Long development errors retain reachable Load at800");await Capture("invalid-development-content-recovery-800");
        string name=$"qa-{_runId}-invalid-content-original4";string path=SaveFiles.PathFor(name);var original=SaveGame.ReadFile(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/legacy-rules4-v5-save.json")).Game!;SaveGame.WriteFile(path,original.Content,original.State);_savePaths.Add(path);Press(Button(main,"Load Game"));await Frames();var dialog=One<LoadGameDialog>(main);var list=One<ItemList>(dialog);int index=Enumerable.Range(0,list.ItemCount).Single(i=>list.GetItemText(i).StartsWith(name+"    ",StringComparison.Ordinal));list.Select(index);list.EmitSignal(ItemList.SignalName.ItemSelected,index);Press(dialog.GetOkButton());await Frames();Check(Session(One<GameScreen>(main)).State.RulesVersion==4,"Invalid current defaults still load original embedded rules4 campaign");RemoveChild(main);main.QueueFree();await Frames();
    }

    private async Task EndPlanningWithoutResolution(GameScreen screen) {
        var session=Session(screen);int turn=session.State.Turn;var button=Descendants<Button>(screen).Single(b=>b.Text=="End Turn"||b.Text=="Waiting for others (cancel)");Press(button);await Frames();Check(session.State.Turn==turn,"Labelled nonfinal Ready/withdraw does not resolve the two-human turn");
    }
    private static void SelectNumber(OptionButton picker,int id){picker=UiNavigation.Reveal(picker);int index=Enumerable.Range(0,picker.ItemCount).Single(i=>picker.GetItemMetadata(i).AsInt32()==id);picker.Select(index);picker.EmitSignal(OptionButton.SignalName.ItemSelected,index);}
    private async Task OwnColony(GameScreen screen,OpenAntares.Simulation.ColonyId colony){SelectNumber(Descendants<OptionButton>(One<ColonyList>(screen)).Single(),colony.Value);await Frames();}
    private async Task AssignGeneratedTemplate(GameScreen screen,string template){Select(Picker(screen,"GovernorTemplatePicker"),template);await Click(screen,"Apply template");Toggle(screen,"WorkforceGovernorToggle",true);await Frames();Toggle(screen,"ConstructionGovernorToggle",true);await Frames();}
    private async Task QueueGeneratedProject(GameScreen screen,string kind,string id){await Click(screen,"Edit queue");var editor=One<ConstructionQueueEditor>(screen);Select(Picker(editor,"QueueTargetPicker"),kind+"|"+id);Press(Button(editor,"Add project"));Press(editor.GetOkButton());await Frames();}
    private async Task GeneratedDevelopmentCampaigns() {
        foreach(var entry in new[]{
            (Species:"harl",Template:"industrial",Specialist:"production_complex",Research:new[]{"cultivation_methods","measurement_methods","industrial_coordination","habitat_methods"}),
            (Species:"peldra",Template:"research",Specialist:"discovery_center",Research:new[]{"cultivation_methods","measurement_methods","cooperative_science"}),
            (Species:"sulan",Template:"growth",Specialist:"habitat_network",Research:new[]{"cultivation_methods","measurement_methods","habitat_methods","adaptive_habitats"}),
            (Species:"wenth",Template:"balanced",Specialist:"production_complex",Research:new[]{"cultivation_methods","measurement_methods","industrial_coordination","habitat_methods"})}) {
            string label="generated-colony-"+entry.Species;GetTree().Root.Size=new(1920,1080);var main=GD.Load<PackedScene>("res://game/Main.tscn").Instantiate<Control>();AddChild(main);await Frames();var screen=await OpenAntares.Game.Validation.ScoutUiControlChecks.LoadGeneratedRules8(this,main,new("small",42,8,entry.Species,1,"peldra",false,false),_output,label,Check);screen.NotificationPreferencesPath=Path.Combine(_output,label+"-preferences.json");var session=Session(screen);var owner=session.State.Empires.Single(e=>e.Controller==ControllerKind.Human).Id;var opening=session.View(owner);var primaryId=opening.Colonies.Single().Id;
            var expectedStart=GalaxyGenerator.CreateNewGame(session.Content,new("small",42,8,entry.Species,1,"peldra",false,false));Check(expectedStart.Succeeded&&Snapshot(session)==SaveGame.Serialize(session.Content,expectedStart.State!)&&!session.State.Crisis!.Enabled&&!session.State.Scouting!.Enabled,label+": explicit rules8 Main Load preserves seed42 Small/plague Off/ameba Off with one Peldra rival");
            await AssignGeneratedTemplate(screen,entry.Template);await QueueGeneratedProject(screen,"building","fabrication_hall");string queued=await SaveThroughDialog(screen,label+"-queued-foundation");screen=await LoadThroughDialog(main,queued);screen.NotificationPreferencesPath=Path.Combine(_output,label+"-preferences.json");session=Session(screen);
            bool founderQueued=false,savedFounding=false,favorite=false,expandedCapacity=false,blockedSeed=false,conservedSeed=false;int turns=0;var foundations=new[]{"fabrication_hall","cultivation_hub","analysis_lab"};string founderRole=entry.Species=="wenth"?"daughter_seed":"harl_settler";
            try {
                while(turns<160) {
                    await OwnColony(screen,primaryId);session=Session(screen);var view=session.View(owner);var primary=view.Colonies.Single(c=>c.Id==primaryId);
                    if(view.Research.TargetId is null){string? research=entry.Research.FirstOrDefault(id=>!view.Research.KnownTechnologyIds.Contains(id)&&session.Content.Technologies[id].PrerequisiteTechnologyIds.All(view.Research.KnownTechnologyIds.Contains));if(research is not null){Select(Descendants<OptionButton>(One<ResearchBar>(screen)).Single(),research);await Frames();}}
                    view=session.View(owner);primary=view.Colonies.Single(c=>c.Id==primaryId);bool developed=foundations.All(primary.CompletedBuildingIds.Contains)&&primary.CompletedBuildingIds.Contains(entry.Specialist);
                    if(entry.Species=="wenth"&&!expandedCapacity&&primary.CompletedBuildingIds.Contains("habitat_extension")){expandedCapacity=true;Check(primary.Economy.Growth.Capacity==8&&primary.Population==6,label+": ordinary capacity completion changes six to eight while population stays six");}
                    if(entry.Species is "harl" or "wenth"&&developed&&!founderQueued&&(entry.Species!="wenth"||expandedCapacity)){await QueueGeneratedProject(screen,"vessel",founderRole);founderQueued=true;string shipQueued=await SaveThroughDialog(screen,label+"-queued-expedition");screen=await LoadThroughDialog(main,shipQueued);screen.NotificationPreferencesPath=Path.Combine(_output,label+"-preferences.json");session=Session(screen);await OwnColony(screen,primaryId);}
                    view=session.View(owner);
                    if(entry.Species=="harl"&&founderQueued&&view.Colonies.Length==1) {
                        var founder=view.Vessels.FirstOrDefault(v=>v.RoleId==founderRole&&v.CarriedPopulation>0&&v.Transit is null&&v.PendingMove is null&&v.PendingFounding is null);
                        if(founder is not null) {
                            var legal=founder.FoundingPreviews.Where(p=>p.RejectionCode is null).OrderByDescending(p=>view.Stars.SelectMany(s=>s.Planets??ImmutableArray<KnownPlanetView>.Empty).Single(k=>k.Id==p.Target).SupportPerWorker).ThenBy(p=>p.Target).FirstOrDefault();
                            SelectNumber(Picker(One<VesselPanel>(screen),"VesselPicker"),founder.Id.Value);await Frames();
                            if(legal is not null){var knownStar=view.Stars.Single(s=>s.Planets.HasValue&&s.Planets.Value.Any(p=>p.Id==legal.Target));One<GalaxyMap>(screen).Select(knownStar.Chart.Id);await Frames();var planet=knownStar.Planets!.Value.Single(p=>p.Id==legal.Target);Press(UiNavigation.OrbitPlanet(screen,planet.Orbit));await Frames();await Click(screen,"Found colony");if(!savedFounding){savedFounding=true;string pending=await SaveThroughDialog(screen,label+"-pending-founding");screen=await LoadThroughDialog(main,pending);screen.NotificationPreferencesPath=Path.Combine(_output,label+"-preferences.json");session=Session(screen);}}
                            else {
                                var target=view.Stars.Where(s=>s.Planets.HasValue&&s.Chart.Id!=founder.StationaryStarId).SelectMany(s=>s.Planets!.Value.Where(p=>p.OwnerId is null&&p.Colonizable&&p.SupportPerWorker>0&&p.Potential?.NativeHabitatAllowed==true).Select(p=>new{Star=s.Chart,Planet=p})).OrderBy(t=>t.Star.Id).ThenBy(t=>t.Planet.Id).FirstOrDefault();
                                if(target is not null){One<GalaxyMap>(screen).Select(target.Star.Id);await Frames();await Click(screen,"Set course");}
                            }
                        }
                    }
                    await OwnColony(screen,primaryId);view=session.View(owner);primary=view.Colonies.Single(c=>c.Id==primaryId);int beforePopulation=primary.Population,beforeCapacity=primary.Economy.Growth.Capacity,beforeSeeds=view.Vessels.Count(v=>v.RoleId=="daughter_seed");bool waitingSeed=entry.Species=="wenth"&&primary.ProjectId=="daughter_seed"&&beforePopulation<beforeCapacity;
                    if(waitingSeed){blockedSeed=true;Check(primary.Economy.Production.BlockedReasonCode=="awaiting_embarkation",label+": explicit seed waits for the current full capacity");}
                    var reference=new OpenAntares.Simulation.GameSession(session.Content,session.State.Clone());var referenceTurn=reference.Execute(new OpenAntares.Simulation.Commands.SetReadyCommand(owner,reference.State.Turn,true));Check(referenceTurn.Accepted,label+": uninterrupted reference Ready resolves");await EndTurn(screen);turns++;Check(Snapshot(session)==Snapshot(reference),label+": actual End Turn matches complete canonical reference");One<TurnReportDialog>(screen).Hide();await Frames();view=session.View(owner);
                    if(waitingSeed)Check(view.Vessels.Count(v=>v.RoleId=="daughter_seed")==beforeSeeds,label+": growth to capacity cannot retroactively embark this snapshot");
                    if(entry.Species=="wenth"&&view.Vessels.Count(v=>v.RoleId=="daughter_seed")>beforeSeeds){conservedSeed=true;Check(beforePopulation==8&&beforeCapacity==8&&view.Colonies.Single(c=>c.Id==primaryId).Population==5&&view.Vessels.Single(v=>v.RoleId=="daughter_seed").CarriedPopulation==3,label+": full eight conserves parent five plus seed three");}
                    if(entry.Species=="harl"&&view.Colonies.Length>=2&&!favorite){var child=view.Colonies.First(c=>c.Id!=primaryId);Check(child.CompletedBuildingIds.Length==0&&child.GrowthProgress==0&&child.Workforce==new Workforce(child.Population,0,0),label+": ordinary founded colony waits until next snapshot");await OwnColony(screen,primaryId);Toggle(screen,"WorkforceGovernorToggle",false);await Frames();Toggle(screen,"ConstructionGovernorToggle",false);await Frames();await OwnColony(screen,child.Id);await AssignGeneratedTemplate(screen,"industrial");favorite=true;string paused=await SaveThroughDialog(screen,label+"-manual-favorite-and-delegated-child");screen=await LoadThroughDialog(main,paused);screen.NotificationPreferencesPath=Path.Combine(_output,label+"-preferences.json");session=Session(screen);view=session.View(owner);Check(view.Colonies.Single(c=>c.Id==primaryId).Development is {WorkforceEnabled:false,ConstructionEnabled:false}&&view.Colonies.Single(c=>c.Id==child.Id).Development is {WorkforceEnabled:true,ConstructionEnabled:true},label+": paused favorite and delegated child survive UI Save/Load");}
                    primary=view.Colonies.Single(c=>c.Id==primaryId);bool complete=foundations.All(primary.CompletedBuildingIds.Contains)&&primary.CompletedBuildingIds.Contains(entry.Specialist)&&(entry.Species!="harl"||favorite&&savedFounding)&&(entry.Species!="wenth"||expandedCapacity&&blockedSeed&&conservedSeed);if(complete)break;
                }
                var final=session.View(owner);var source=final.Colonies.Single(c=>c.Id==primaryId);Check(turns<=160&&foundations.All(source.CompletedBuildingIds.Contains)&&source.CompletedBuildingIds.Contains(entry.Specialist)&&(entry.Species!="harl"||favorite&&savedFounding)&&(entry.Species!="wenth"||expandedCapacity&&blockedSeed&&conservedSeed),label+": all required generated outcomes complete within160-turn QA guard; population="+source.Population+", built="+string.Join(",",source.CompletedBuildingIds));
                await OwnColony(screen,primaryId);foreach(var dimensions in new[]{new Vector2I(800,600),new Vector2I(1920,1080)}){GetTree().Root.Size=dimensions;await Frames();await Reach(Picker(screen,"GovernorTemplatePicker"));await Capture(label+"-developed-"+dimensions.X);}
                _results.Add(new{name=label+": generated provenance",passed=true,seed=42,size="small",humanSpecies=entry.Species,rivalSpecies="peldra",template=entry.Template,specialist=entry.Specialist,resolvedTurns=turns,ownedColonies=final.Colonies.Length,foundations=3,liveOutcomeInjections=0,rerolls=0,guard=160,wenthExpandedCapacity=expandedCapacity,wenthBlockedBeforeMaturity=blockedSeed,wenthParentFiveSeedThree=conservedSeed,manualFavorite=favorite,provenance="Unmodified actual current form; decisions use own EmpireView/public definitions/shared previews through real controls."});
                string completed=await SaveThroughDialog(screen,label+"-developed");var future=new OpenAntares.Simulation.GameSession(session.Content,session.State.Clone());for(int i=0;i<3;i++)Check(future.Execute(new OpenAntares.Simulation.Commands.SetReadyCommand(owner,future.State.Turn,true)).Accepted,label+": uninterrupted future resolves");screen=await LoadThroughDialog(main,completed);session=Session(screen);for(int i=0;i<3;i++){await EndTurn(screen);One<TurnReportDialog>(screen).Hide();}Check(Snapshot(session)==Snapshot(future),label+": three future turns after UI reload equal uninterrupted canonical continuation");
            }catch{File.WriteAllText(Path.Combine(_output,label+"-failure-save.json"),Snapshot(Session(screen)));throw;}
            RemoveChild(main);main.QueueFree();await Frames();
        }
    }
}
