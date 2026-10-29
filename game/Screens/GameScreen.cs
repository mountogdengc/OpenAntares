using System.Linq;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Screens;

/// <summary>
/// The main in-game screen: a top bar with the turn and research, the galaxy map, and a side panel
/// for the selected star and, when it holds one of the viewer's colonies, that colony.
/// </summary>
public partial class GameScreen : VBoxContainer
{
    private readonly GameSession _session;
    private readonly EmpireId _viewer;
    private readonly Label _turnLabel = new();
    private readonly ResearchBar _researchBar = new();
    private readonly Label _message = new() { ThemeTypeVariation = "WarningLabel" };
    private readonly GalaxyMap _map = new();
    private readonly StarPanel _starPanel = new();
    private readonly ColonyPanel _colonyPanel = new();
    private readonly Button _endTurn = new();
    private readonly ContactsPanel _contacts = new();
    private readonly ColonyList _colonies = new();
    private readonly VesselPanel _vessels = new();
    private ColonyId? _selectedColony;
    private PlanetId? _selectedPlanet;
    private VesselId? _selectedVessel;
    private StarId? _courseDestination;
    private EmpireView View => _session.View(_viewer);
    private readonly CombatOrdersDialog _combatOrders=new();
    private readonly Label _combatBadge=new(){Name="CombatAttentionBadge",ThemeTypeVariation="WarningLabel",AutowrapMode=TextServer.AutowrapMode.WordSmart};
    public string CombatPreferencesPath{get;set;}="user://combat-notifications.json";
    private readonly FleetCommandDialog _fleetCommand=new();
    private readonly AssemblyRequestDialog _assemblyRequest=new();
    private readonly OfficerDetailDialog _officerDetails=new();
    private readonly Label _fleetBadge=new(){Name="FleetAttentionBadge",ThemeTypeVariation="WarningLabel",AutowrapMode=TextServer.AutowrapMode.WordSmart,SizeFlagsHorizontal=SizeFlags.ExpandFill};
    public string FleetPreferencesPath{get;set;}="user://fleet-notifications.json";
    private readonly ScoutOverviewDialog _scoutOverview=new();
    private readonly Label _scoutBadge=new(){Name="ScoutCriticalBadge",ThemeTypeVariation="WarningLabel",SizeFlagsHorizontal=SizeFlags.ExpandFill,AutowrapMode=TextServer.AutowrapMode.WordSmart};
    private readonly System.Collections.Generic.HashSet<string> _seenScoutMaterial=new(System.StringComparer.Ordinal);
    private bool _pendingScoutInterrupt;
    public string ScoutPreferencesPath{get;set;}="user://scout-notifications.json";
    private readonly CrisisOverviewDialog _crisisOverview=new();
    private readonly CrisisResponseDialog _crisisResponse=new();
    private readonly CrisisNotificationDialog _crisisNotifications=new();
    private OpenAntares.Game.Presentation.CrisisNotificationPreferences _crisisPreferences=OpenAntares.Game.Presentation.CrisisNotificationPreferences.Defaults;
    private readonly System.Collections.Generic.HashSet<string> _seenCrisisMaterial=new(System.StringComparer.Ordinal);
    private bool _crisisMaterialInitialized;
    private bool _pendingCrisisInterrupt;
    public string CrisisPreferencesPath{get;set;}="user://crisis-notifications.json";
    private bool _returnToCrisisOverview;
    private readonly TurnReportDialog _report = new();
    private EmpireTurnReportView? _latestReport;
    private readonly TemplateReferencesDialog _templateReferences = new();
    private readonly SaveGameDialog _saveDialog = new();
    private readonly OpenAntares.Game.Presentation.PlanetPortraitCache _portraitCache = new();
    private readonly EmpireManagementDialog _management = new();
    private readonly ColonyNotificationDialog _notificationDialog = new();
    private OpenAntares.Game.Presentation.ColonyNotificationPreferences _preferences = OpenAntares.Game.Presentation.ColonyNotificationPreferences.Defaults;
    public string NotificationPreferencesPath { get; set; } = "user://colony-notifications.json";

    /// <summary>Raised when the player wants to load a saved game. The current game is untouched unless a load succeeds.</summary>
    public event System.Action? LoadRequested;

    /// <param name="viewer">The empire this screen is shown to. Presentation-only; the simulation has no "current player".</param>
    public GameScreen(GameSession session, EmpireId viewer)
    {
        _session = session;
        _viewer = viewer;
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        try { var styles=OpenAntares.Game.Presentation.PlanetStyleCatalog.Load("res://art/planets/planet_styles.json");_starPanel.ConfigurePortraits(styles,_portraitCache);_colonyPanel.ConfigurePortraits(styles,_portraitCache);_management.ConfigurePortraits(styles,_portraitCache); }
        catch (System.Exception error) when(error is System.ArgumentException or System.InvalidOperationException or System.Text.Json.JsonException or System.Collections.Generic.KeyNotFoundException) { ShowMessage("Planet illustrations unavailable; colony management remains available.",true); }

        var topBar = new PanelContainer();
        AddChild(topBar);
        var topPadding = new MarginContainer { ThemeTypeVariation = "PanelPadding" };
        topBar.AddChild(topPadding);
        var topRow = new HBoxContainer();
        topPadding.AddChild(topRow);
        topRow.AddChild(_turnLabel);
        topRow.AddChild(new VSeparator());
        topRow.AddChild(_researchBar);
        _researchBar.CommandIssued += Execute;
        _message.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _message.HorizontalAlignment = HorizontalAlignment.Right;
        _message.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _message.MouseFilter = MouseFilterEnum.Stop;
        topRow.AddChild(_message);
        var saveButton = new Button { Text = "Save" };
        saveButton.Pressed += () => _saveDialog.Open(_session, SuggestedSaveName());
        topRow.AddChild(saveButton);
        var loadButton = new Button { Text = "Load" };
        loadButton.Pressed += () => LoadRequested?.Invoke();
        topRow.AddChild(loadButton);
        _endTurn.Pressed += EndTurn;
        topRow.AddChild(_endTurn);

        AddChild(_saveDialog);
        _saveDialog.Saved += name => ShowMessage($"Saved as \"{name}\".", isWarning: false);
        AddChild(_fleetCommand);AddChild(_assemblyRequest);AddChild(_officerDetails);AddChild(_combatOrders);
        _fleetCommand.CombatRequested+=action=>{_fleetCommand.Hide();Callable.From(()=>OpenCombat(action)).CallDeferred();};
        if(Rules.HasCombat(View.RulesVersion)){var row=new VBoxContainer();AddChild(row);row.AddChild(_combatBadge);var response=new Button{Name="OpenCombatResponses",Text="Review combat response / orders"};row.AddChild(response);response.Pressed+=OpenCombatResponse;}else{AddChild(_combatBadge);_combatBadge.Hide();}
        _fleetCommand.CommandIssued+=Execute;_assemblyRequest.CommandIssued+=Execute;_officerDetails.CommandIssued+=Execute;
        _fleetCommand.DisplayApplied+=()=>Callable.From(Refresh).CallDeferred();
        foreach(var dialog in new Window[]{_fleetCommand,_assemblyRequest,_officerDetails,_combatOrders})dialog.VisibilityChanged+=()=>Callable.From(OpenPendingCommandInterrupt).CallDeferred();
        _fleetCommand.RequestRequested+=request=>{_fleetCommand.Hide();Callable.From(()=>_assemblyRequest.ShowEditor(_session.Content,View,request)).CallDeferred();};
        _fleetCommand.OfficersRequested+=()=>{_fleetCommand.Hide();Callable.From(()=>_officerDetails.ShowOfficers(_session.Content,View)).CallDeferred();};
        _fleetCommand.StarRequested+=star=>{_fleetCommand.Hide();_map.Select(star);};
        _fleetCommand.InspectRequested+=id=>{_selectedVessel=id;_selectedPlanet=null;_courseDestination=null;var ship=View.Vessels.Single(v=>v.Id==id);_map.Select(ship.StationaryStarId??ship.Transit!.Destination);Callable.From(Refresh).CallDeferred();};
        _assemblyRequest.ColonyRequested+=SelectColony;
        if(Rules.HasFleets(View.RulesVersion)){var commandRow=new HBoxContainer();AddChild(commandRow);var entry=new Button{Name="OpenFleetCommand",Text="Fleet command"};entry.Pressed+=OpenFleetCommand;commandRow.AddChild(entry);commandRow.AddChild(_fleetBadge);}else{AddChild(_fleetBadge);_fleetBadge.Hide();}
        AddChild(_scoutOverview);_scoutOverview.CommandIssued+=Execute;_scoutOverview.StarRequested+=star=>{_scoutOverview.Hide();_map.Select(star);};_scoutOverview.DisplayApplied+=_=>Callable.From(Refresh).CallDeferred();
        if(Rules.HasScouting(View.RulesVersion))
        {
            var scoutRow=new HBoxContainer();AddChild(scoutRow);var scouts=new Button{Name="OpenScoutOverview",Text="Scouts / ameba"};scoutRow.AddChild(scouts);scouts.Pressed+=()=>{if(View.Scouting is {} scouting)_scoutOverview.ShowScouting(_session.Content,scouting);};scoutRow.AddChild(_scoutBadge);
        }
        else{_scoutBadge.Hide();AddChild(_scoutBadge);}

        AddChild(_crisisOverview);AddChild(_crisisResponse);AddChild(_crisisNotifications);
        _crisisNotifications.Applied+=preferences=>{_crisisPreferences=preferences;Callable.From(Refresh).CallDeferred();};
        _crisisNotifications.CommandRequested+=Execute;
        _colonies.CrisisNotificationsRequested+=()=>{if(View.Crisis is {} crisis)_crisisNotifications.Open(CrisisPreferencesPath,crisis);};
        _crisisResponse.VisibilityChanged+=()=>{if(!_crisisResponse.Visible&&_returnToCrisisOverview&&View.Crisis is {} c){_returnToCrisisOverview=false;Callable.From(()=>{if(View.Crisis is {} latest)_crisisOverview.ShowCrisis(_session.Content,latest);}).CallDeferred();}};
        _crisisOverview.CommandRequested+=Execute;_crisisResponse.CommandRequested+=Execute;
        _crisisOverview.QuarantineRequested+=(ids,enable,release)=>{_returnToCrisisOverview=_crisisOverview.Visible;_crisisOverview.Hide();var preview=OpenAntares.Simulation.Crises.CrisisQueries.PreviewQuarantine(_session.Content,_session.State,_viewer,ids,enable,release);Callable.From(()=>_crisisResponse.ShowQuarantine(preview)).CallDeferred();};
        _crisisOverview.PurgeRequested+=id=>{_returnToCrisisOverview=_crisisOverview.Visible;_crisisOverview.Hide();var preview=OpenAntares.Simulation.Crises.CrisisQueries.PreviewPurge(_session.Content,_session.State,_viewer,id);Callable.From(()=>_crisisResponse.ShowPurge(preview)).CallDeferred();};
        _crisisOverview.InspectionRequested+=vessel=>{_returnToCrisisOverview=_crisisOverview.Visible;_crisisOverview.Hide();var view=View.Crisis;if(view is null)return;var current=view.Vessels.FirstOrDefault(v=>v.Id==vessel.Id);if(current is null)return;Callable.From(()=>_crisisResponse.ShowInspection(view,current)).CallDeferred();};
        _crisisOverview.StarRequested+=star=>_map.Select(star);
        _colonies.CrisisRequested+=()=>{if(View.Crisis is {} crisis)_crisisOverview.ShowCrisis(_session.Content,crisis);};
        AddChild(_management); _management.CommandIssued += Execute; _management.ColonyRequested += SelectColony;
        AddChild(_notificationDialog); _notificationDialog.Applied += preferences => { _preferences = preferences; Callable.From(Refresh).CallDeferred(); };
        _colonies.ManageRequested += () => { _management.Preferences = _preferences; _management.ShowEmpire(_session.Content,View); };
        _colonies.NotificationsRequested += () => _notificationDialog.Open(NotificationPreferencesPath);
        _colonies.LatestReportRequested += ShowLatestReport;
        AddChild(_report);
        _report.VisibilityChanged+=()=>{if(!_report.Visible&&_pendingCrisisInterrupt)Callable.From(OpenPendingCommandInterrupt).CallDeferred();};
        _report.VisibilityChanged+=()=>{if(!_report.Visible&&_pendingScoutInterrupt)Callable.From(OpenPendingScoutInterrupt).CallDeferred();};
        _crisisOverview.VisibilityChanged+=()=>{if(!_crisisOverview.Visible&&_pendingScoutInterrupt)Callable.From(OpenPendingScoutInterrupt).CallDeferred();};
        _crisisResponse.VisibilityChanged+=()=>{if(!_crisisResponse.Visible&&_pendingScoutInterrupt)Callable.From(OpenPendingScoutInterrupt).CallDeferred();};
        AddChild(_templateReferences);
        _report.ChooseProjectRequested += ChooseProject;
        _report.StarRequested += star => _map.Select(star);
        _report.ColonyRequested += SelectColony;
        _report.VesselRequested += vessel => { _selectedVessel = vessel; _courseDestination = null; Callable.From(Refresh).CallDeferred(); };
        _report.ChooseResearchRequested += _researchBar.OpenPicker;
        _report.FleetCommandRequested+=()=>{_report.Hide();OpenFleetCommand();};

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        AddChild(body);

        _map.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _map.SizeFlagsVertical = SizeFlags.ExpandFill;
        _map.StarSelected += star => { _courseDestination = star; Refresh(); };
        body.AddChild(_map);

        var side = new PanelContainer { CustomMinimumSize = new Vector2(380, 0) };
        body.AddChild(side);
        var sidePadding = new MarginContainer { ThemeTypeVariation = "PanelPadding" };
        side.AddChild(sidePadding);
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true,
        };
        sidePadding.AddChild(scroll);
        scroll.Resized += () => KeepFocusVisibleAfterResize(scroll);
        var sideColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(sideColumn);
        sideColumn.AddChild(_contacts);
        sideColumn.AddChild(_colonies);
        _colonies.ColonySelected += SelectColony;
        sideColumn.AddChild(_starPanel);
        _starPanel.PlanetSelected += planet => { _selectedPlanet = planet; _selectedColony = View.Colonies.FirstOrDefault(c => c.PlanetId == planet)?.Id; Callable.From(Refresh).CallDeferred(); };
        sideColumn.AddChild(_colonyPanel);
        _colonyPanel.CommandIssued += Execute;
        sideColumn.AddChild(_vessels);
        _vessels.CommandIssued += Execute;
        _vessels.VesselSelected += vessel => { _selectedVessel = vessel; _courseDestination = null; Callable.From(Refresh).CallDeferred(); };
        _vessels.DestinationSelected += star => { _courseDestination = star; _map.Select(star); };
        _vessels.ScoutOverviewRequested+=()=>{if(View.Scouting is {} scouting)_scoutOverview.ShowScouting(_session.Content,scouting);};
        _vessels.FleetCommandRequested+=OpenFleetCommand;

        _map.ShowGalaxy(View.Galaxy);
        if (HomeStar() is { } start)
        {
            _map.Select(start);
        }

        Refresh();
    }

    private async void KeepFocusVisibleAfterResize(ScrollContainer scroll)
    {
        // FollowFocus runs when focus changes. A resize can hide a control that already owns focus.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(scroll) || !IsInsideTree()) return;
        Control? focused = GetViewport().GuiGetFocusOwner();
        if (focused is not null && scroll.IsAncestorOf(focused)) scroll.EnsureControlVisible(focused);
    }

    private async void ChooseProject(ColonyId colony)
    {
        if(!View.Colonies.Any(c=>c.Id==colony)){ShowMessage("That colony no longer exists; its completed work remains in the historical report.",true);return;}
        SelectColony(colony);
        // Selection rebuilds the colony panel. Let containers lay out its controls before
        // focusing the picker so the scroll container reveals it and the popup aligns with it.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if(!IsInsideTree()||!View.Colonies.Any(c=>c.Id==colony))return;
        _colonyPanel.OpenProjectPicker();
    }

    private void Execute(Command command)
    {
        CommandOutcome outcome = _session.Execute(command);
        ShowMessage(outcome.Rejection?.Message ?? string.Empty, isWarning: true);
        if(_combatOrders.Visible&&command is OpenAntares.Simulation.Combat.CombatActionCommand){if(outcome.Accepted)_combatOrders.Hide();else _combatOrders.ShowError(outcome.Rejection?.Message??"");}
        if(_fleetCommand.Visible)_fleetCommand.ShowError(outcome.Rejection?.Message??"");
        if(_assemblyRequest.Visible)_assemblyRequest.ShowError(outcome.Rejection?.Message??"");
        if(_officerDetails.Visible)_officerDetails.ShowError(outcome.Rejection?.Message??"");
        if(outcome.Changed&&command is OpenAntares.Simulation.Fleets.AssemblyCommand{Action:OpenAntares.Simulation.Fleets.AssemblyAction.Accept}&&View.Fleets is {} accepted)
        {var id=accepted.Requests.MaxBy(r=>r.Id)!.Id;Callable.From(()=>_assemblyRequest.ShowEditor(_session.Content,View,id)).CallDeferred();}
        if (outcome.Rejection?.TemplateReferences is { } references)
            _templateReferences.ShowReferences(View, references);

        // Rebuild after the triggering control's signal has finished.
        Callable.From(Refresh).CallDeferred();

        if (outcome.Report is { } report)
        {
            _latestReport=EmpireViewProjector.ProjectReport(_session.Content, _session.State, report, _viewer);
            ShowLatestReport();
        }
    }

    /// <summary>
    /// Marks the viewer's empire ready, which resolves the turn once every human empire is ready.
    /// Pressing again while waiting withdraws readiness.
    /// </summary>
    private void EndTurn()
    {
        var view = View;
        Execute(new SetReadyCommand(_viewer, view.Turn, !view.Research.Ready));
    }

    private void Refresh()
    {
        var view = View;
        _fleetCommand.CombatPreferencesPath=CombatPreferencesPath;_report.CombatPreferencesPath=CombatPreferencesPath;
        if(_combatOrders.Visible)_combatOrders.UpdateView(view);RefreshCombatAttention(view);
        _fleetCommand.PreferencesPath=FleetPreferencesPath;
        _report.FleetPreferencesPath=FleetPreferencesPath;
        if(_fleetCommand.Visible)_fleetCommand.ShowEmpire(_session.Content,view);
        if(_assemblyRequest.Visible)_assemblyRequest.UpdateView(_session.Content,view);
        if(_officerDetails.Visible)_officerDetails.ShowOfficers(_session.Content,view);
        RefreshFleetAttention(view);
        _preferences = OpenAntares.Game.Presentation.ColonyNotificationPreferences.Load(ProjectSettings.GlobalizePath(NotificationPreferencesPath));
        _crisisPreferences=OpenAntares.Game.Presentation.CrisisNotificationPreferences.Load(ProjectSettings.GlobalizePath(CrisisPreferencesPath));
        if (_management.Visible) { _management.Preferences = _preferences; _management.ShowEmpire(_session.Content,view); }
        if(_crisisOverview.Visible&&view.Crisis is {} currentCrisis)_crisisOverview.ShowCrisis(_session.Content,currentCrisis);
        _turnLabel.Text = $"Turn {view.Turn}";
        _endTurn.Text = view.Research.Ready ? "Waiting for others (cancel)" : "End Turn";
        _researchBar.ShowEmpire(_session.Content, view.Research);
        _map.ShowGalaxy(view.Galaxy);
        _map.ShowKnownAmebas(view.Scouting);
        RefreshScoutAttention(view.Scouting);
        _contacts.Show(view);
        RefreshCrisisAttention(view.Crisis);
        if (view.RulesVersion == Rules.LegacyVersion) ShowMessage("Legacy colony game - start a new game for exploration", false);
        if (_map.Selected is not { } selected) return;
        var star = view.Stars.Single(s => s.Chart.Id == selected);
        if (star.Planets is { } planets && !planets.Any(p => p.Id == _selectedPlanet))
        {
            var preferred = view.Colonies.FirstOrDefault(c => c.Id == _selectedColony && c.StarId == selected) ?? view.Colonies.FirstOrDefault(c => c.StarId == selected);
            _selectedPlanet = preferred?.PlanetId ?? (planets.Length > 0 ? planets[0].Id : null);
        }
        if (star.Planets is null) _selectedPlanet = null;
        var colony = view.Colonies.FirstOrDefault(c => c.PlanetId == _selectedPlanet && c.StarId == selected);
        _selectedColony = colony?.Id;
        if (colony is not null) _selectedPlanet = colony.PlanetId;
        _starPanel.ShowStar(_session.Content, star, view);
        _colonies.ShowColonies(view, _selectedColony, _session.Content, _preferences, _latestReport);
        if (colony is null) _colonyPanel.Clear(); else _colonyPanel.ShowColony(_session.Content, colony, view);
        if (!view.Vessels.Any(v => v.Id == _selectedVessel)) _selectedVessel = view.Vessels.Length > 0 ? view.Vessels[0].Id : null;
        _vessels.ShowVessels(_session.Content, view, _selectedVessel, _courseDestination, _selectedPlanet);
    }

    private void OpenCombat(OpenAntares.Simulation.Combat.CombatAction action)=>_combatOrders.Open(_session.Content,View,action,command=>Execute(command));
    private void OpenCombatResponse()
    {
        var combat=View.Combat;if(combat is null)return;var e=combat.Encounters.FirstOrDefault(e=>combat.Notices.Any(n=>n.Encounter==e.Id&&n.Level==OpenAntares.Simulation.Combat.CombatNoticeLevel.Emergency))??combat.Encounters.FirstOrDefault();
        if(e is null){OpenFleetCommand();return;}OpenCombat(new OpenAntares.Simulation.Combat.CombatAction.ChangeCombatOrders(e.Id,e.Revision,e.Orders,e.CivilianRiskAuthorized));
    }
    private void RefreshCombatAttention(EmpireView view)
    {
        var combat=view.Combat;_combatBadge.Visible=combat is not null&&!combat.Notices.IsEmpty;if(combat is null)return;
        int emergency=combat.Notices.Count(n=>n.Level==OpenAntares.Simulation.Combat.CombatNoticeLevel.Emergency),attention=combat.Notices.Count(n=>n.Level==OpenAntares.Simulation.Combat.CombatNoticeLevel.Attention);
        _combatBadge.Text=emergency>0?$"Combat Emergency: {emergency} encounter response(s) required before Ready. Review the exact fleet and revision.":$"Combat Attention: {attention} damage, return, repair or officer conditions. Routine display cannot hide these facts.";
    }
    private void OpenFleetCommand(){if(Rules.HasFleets(View.RulesVersion)){_fleetCommand.PreferencesPath=FleetPreferencesPath;_fleetCommand.ShowEmpire(_session.Content,View);}}
    private void RefreshFleetAttention(EmpireView view)
    {
        if(view.Fleets is not {} fleets){_fleetBadge.Hide();return;}
        int requests=fleets.Requests.Count(r=>OpenAntares.Simulation.Fleets.FleetQueries.IsActive(r.Status)&&(r.Status==OpenAntares.Simulation.Fleets.RequestStatus.SafetyPaused||r.Slots.Any(s=>s.Status==OpenAntares.Simulation.Fleets.SlotStatus.Missing)));
        int officers=fleets.Officers.Count(o=>o.Status is OpenAntares.Simulation.Fleets.OfficerStatus.Recovering or OpenAntares.Simulation.Fleets.OfficerStatus.Missing or OpenAntares.Simulation.Fleets.OfficerStatus.Dead);
        int fresh=fleets.Ships.Count(s=>s.NewlyCompleted);bool routine=OpenAntares.Game.Presentation.FleetNotificationPreferences.Load(ProjectSettings.GlobalizePath(FleetPreferencesPath)).ShowRoutine;
        int standing=fleets.Fleets.Count(f=>f.Attention is not null);
        _fleetBadge.Visible=requests+officers+standing>0||routine&&fresh>0;_fleetBadge.Text=requests+officers+standing>0?$"Attention: {standing} blocked standing orders; {requests} blocked / missing requests; {officers} recovering, missing or final loss records.":$"Summary: {fresh} newly completed independent ships.";
    }
    private void RefreshCrisisAttention(OpenAntares.Simulation.Crises.CrisisView? crisis)
    {
        if(crisis is null)return;
        bool fresh=false;
        foreach(var c in crisis.Cases.Where(c=>c.Key is not null))
            if(_seenCrisisMaterial.Add($"case:{c.Key!.Episode}:{c.Location.Colony}:{c.AttentionRevision}")&&_crisisMaterialInitialized)fresh=true;
        foreach(var entry in crisis.History.Concat(crisis.PendingDigests).SelectMany(d=>d.Entries).Where(e=>e.Disposition is "pending" or "waived"))
            if(_seenCrisisMaterial.Add($"event:{entry.Key.Episode}:{entry.Key.Colony}:{entry.Revision}:{entry.Kind}")&&_crisisMaterialInitialized)fresh=true;
        _crisisMaterialInitialized=true;
        if(!fresh||_crisisPreferences.Critical!=OpenAntares.Game.Presentation.CrisisDisplayLevel.Interrupt)return;
        if(_report.Visible||_fleetCommand.Visible||_assemblyRequest.Visible||_officerDetails.Visible||_combatOrders.Visible)_pendingCrisisInterrupt=true;
        else if(!_crisisOverview.Visible&&!_crisisResponse.Visible)
            Callable.From(()=>{if(View.Crisis is {} current)_crisisOverview.ShowCrisis(_session.Content,current);}).CallDeferred();
    }
    private void RefreshScoutAttention(OpenAntares.Simulation.Scouting.ScoutView? scouting)
    {
        if(scouting is null)return;
        _scoutOverview.PreferencesPath=ScoutPreferencesPath;
        if(_scoutOverview.Visible)_scoutOverview.ShowScouting(_session.Content,scouting);
        int losses=scouting.History.SelectMany(d=>d.Entries).Count(e=>e.Kind=="loss");int waits=scouting.Vessels.Count(v=>v.Stage is null&&v.Blocker is not null);
        _scoutBadge.Visible=scouting.Attention.Length>0||losses>0||waits>0;_scoutBadge.Text=scouting.Attention.Length>0?$"Emergency: {scouting.Attention.Length} ameba encounter(s); exact current responses required before End Turn.":losses>0?$"Critical: {losses} vessel loss(es) in retained own history.":$"Attention: {waits} scout(s) waiting for a known retreat.";
        bool fresh=false;foreach(var token in scouting.Attention)if(_seenScoutMaterial.Add($"encounter:{token.Vessel}:{token.EncounterNumber}:{token.Revision}"))fresh=true;
        foreach(var loss in scouting.History.SelectMany(d=>d.Entries).Where(e=>e.Kind=="loss"))if(_seenScoutMaterial.Add($"loss:{loss.Vessel}:{loss.Turn}"))fresh=true;
        if(!fresh||OpenAntares.Game.Presentation.ScoutNotificationPreferences.Load(ProjectSettings.GlobalizePath(ScoutPreferencesPath)).Level!=OpenAntares.Game.Presentation.ScoutDisplayLevel.Interrupt)return;
        _pendingScoutInterrupt=true;Callable.From(OpenPendingScoutInterrupt).CallDeferred();
    }
    private void OpenPendingScoutInterrupt()
    {
        // Visibility may change again between the signal and its deferred callback.
        // Keep the emergency queued until the currently exclusive report/response really closes.
        if(!IsInsideTree()||!_pendingScoutInterrupt||_report.Visible||_crisisOverview.Visible||_crisisResponse.Visible||_fleetCommand.Visible||_assemblyRequest.Visible||_officerDetails.Visible||_combatOrders.Visible)return;
        _pendingScoutInterrupt=false;if(View.Scouting is {} current)_scoutOverview.ShowScouting(_session.Content,current);
    }
    private void OpenPendingCommandInterrupt()
    {
        if(!IsInsideTree()||_report.Visible||_fleetCommand.Visible||_assemblyRequest.Visible||_officerDetails.Visible||_combatOrders.Visible||_crisisOverview.Visible||_crisisResponse.Visible)return;
        if(_pendingCrisisInterrupt){_pendingCrisisInterrupt=false;if(View.Crisis is {} current)_crisisOverview.ShowCrisis(_session.Content,current);}else OpenPendingScoutInterrupt();
    }

    private void SelectColony(ColonyId id)
    {
        var colony = View.Colonies.FirstOrDefault(c => c.Id == id); if (colony is null) return;
        _selectedColony = id; _selectedPlanet = colony.PlanetId; _map.Select(colony.StarId);
    }

    private void ShowLatestReport()
    {
        if(_latestReport is null)return;
        _scoutOverview.Hide();
        var view=View;
        if(view.Development is {} development)view=view with{Development=development with{PendingReport=null}};
        _report.ShowReport(_session.Content,_latestReport,view,_preferences,_crisisPreferences);
    }

    private void ShowMessage(string text, bool isWarning)
    {
        _message.Text = text;
        _message.TooltipText = text;
        _message.ThemeTypeVariation = isWarning ? "WarningLabel" : "SubtleLabel";
    }

    public override void _ExitTree() => _portraitCache.Clear();

    private string SuggestedSaveName()
    {
        var view = View;
        string star = HomeStar() is { } home ? view.Stars.Single(s => s.Chart.Id == home).Chart.Name : "Game";
        return $"{star} turn {view.Turn}";
    }

    /// <summary>The star of the viewer's first colony.</summary>
    private StarId? HomeStar()
    {
        return View.Colonies.FirstOrDefault()?.StarId;
    }
}
