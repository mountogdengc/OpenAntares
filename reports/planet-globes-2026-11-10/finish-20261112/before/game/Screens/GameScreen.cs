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
/// Map-first strategy shell with a right console, persistent bottom navigation and separate
/// inspection/management workspaces. Workspace selection is presentation-only.
/// </summary>
public partial class GameScreen : VBoxContainer
{
    private readonly GameSession _session;
    private readonly EmpireId _viewer;
    private readonly ResearchBar _researchBar = new();
    private readonly Label _message = new() { Name = "CommandFeedback", ThemeTypeVariation = "WarningLabel", Visible = false };
    private readonly Label _legacyNotice = new() { Text = "Legacy colony game — original campaign rules retained.", ThemeTypeVariation = "SubtleLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly Button _alertsEntry = new() { Name = "OpenAttentionConsole", Text = "Alerts", Visible = false };
    private readonly GalaxyMap _map = new();
    private readonly StarPanel _starPanel = new();
    private readonly ColonyPanel _colonyPanel = new();
    private readonly Button _endTurn = new();
    private readonly ContactsPanel _contacts = new();
    private readonly DiplomacyDialog _diplomacy = new();
    private readonly ColonyList _colonies = new();
    private readonly VesselPanel _vessels = new();
    private readonly VBoxContainer _mainArea = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly PanelContainer _consolePanel = new() { Name = "EmpireConsole", ThemeTypeVariation = "ConsoleFrame" };
    private readonly ScrollContainer _workspaceScroll = new() { Name = "WorkspaceScroll", SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
    private readonly VBoxContainer _workspacePages = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly HBoxContainer _workspaceHeader = new();
    private readonly Label _workspaceTitle = new() { ThemeTypeVariation = "HeaderLabel", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly Label _selectionTitle = new() { ThemeTypeVariation = "ConsoleHeading", AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly Label _selectionSummary = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart, ThemeTypeVariation = "SubtleLabel" };
    private readonly Label _colonyAttention = new() { ThemeTypeVariation = "WarningLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly Button _manageSelected = new() { Name = "ManageSelectedColony", Text = "Manage colony" };
    private readonly Button _combatResponse = new() { Name = "OpenCombatResponses", Text = "Review combat response / orders" };
    private readonly Button _diplomacyEntry = new() { Name = "OpenDiplomacy", Text = "Diplomacy" };
    private readonly OptionButton _colonyChoice = new() { Name = "WorkspaceColonyPicker", FitToLongestItem = false, ClipText = true, SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly Label _noColony = new() { Text = "No owned colony at this selection. Choose one of your colonies above.", AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly System.Collections.Generic.Dictionary<string, Control> _pages = new();
    private readonly System.Collections.Generic.Dictionary<string, Button> _workspaceButtons = new();
    private string _workspace = "Galaxy";
    private ColonyId? _selectedColony;
    private PlanetId? _selectedPlanet;
    private VesselId? _selectedVessel;
    private bool _requireVesselChoice;
    private StarId? _courseDestination;
    private EmpireView View => _session.View(_viewer);
    private readonly CombatOrdersDialog _combatOrders=new();
    private readonly Label _combatBadge=new(){Name="CombatAttentionBadge",ThemeTypeVariation="WarningLabel",AutowrapMode=TextServer.AutowrapMode.WordSmart};
    public string CombatPreferencesPath{get;set;}="user://combat-notifications.json";
    private readonly FleetCommandDialog _fleetCommand=new();
    private readonly ConflictOrdersDialog _conflictOrders=new();
    private readonly Label _conflictBadge=new(){Name="ConflictAttentionBadge",ThemeTypeVariation="WarningLabel",AutowrapMode=TextServer.AutowrapMode.WordSmart};
    private readonly ShipDesignDialog _shipDesigns=new();
    private readonly ShipDesignEditor _shipDesignEditor=new();
    private readonly RefitDialog _refit=new();
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
    public event System.Action? MainMenuRequested;

    /// <param name="viewer">The empire this screen is shown to. Presentation-only; the simulation has no "current player".</param>
    public GameScreen(GameSession session, EmpireId viewer)
    {
        _context.ResetForCampaign();
        _session = session;
        _viewer = viewer;
    }

    public void ShowFirstTurnOrientation()
    {
        // Retained for existing callers; campaign startup no longer adds a banner.
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        GetTree().Root.GuiEmbedSubwindows = true;
        AddChild(_statusStrip);
        try { var styles=OpenAntares.Game.Presentation.PlanetStyleCatalog.Load("res://art/planets/planet_styles.json");_contextPlanetStyles=styles;_starPanel.ConfigurePortraits(styles,_portraitCache);_colonyPanel.ConfigurePortraits(styles,_portraitCache);_management.ConfigurePortraits(styles,_portraitCache); }
        catch (System.Exception error) when(error is System.ArgumentException or System.InvalidOperationException or System.Text.Json.JsonException or System.Collections.Generic.KeyNotFoundException) { ShowMessage("Planet illustrations unavailable; colony management remains available.",true); }

        _researchBar.CommandIssued += Execute;
        _endTurn.Pressed += EndTurn;

        AddChild(_saveDialog);
        _saveDialog.Saved += name => ShowMessage($"Saved as \"{name}\".", isWarning: false);
        AddChild(_fleetCommand);AddChild(_assemblyRequest);AddChild(_officerDetails);AddChild(_combatOrders);AddChild(_shipDesigns);AddChild(_shipDesignEditor);AddChild(_refit);
        _refit.ActionRequested+=(action,token)=>Execute(new OpenAntares.Simulation.ShipDesigns.RefitCommand(_viewer,View.Turn,action,token));
        _fleetCommand.DesignsRequested+=()=>OpenOverviewChild("Fleet","Designs",()=>_shipDesigns.ShowCatalogue(_session.Content,View));
        _shipDesigns.EditorRequested+=copy=>OpenOverviewChild("Designs","Editor",()=>_shipDesignEditor.ShowEditor(_session.Content,View,copy));
        _shipDesigns.ActionRequested+=DesignAction;
        _shipDesignEditor.Saved+=(name,recipe)=>DesignAction(new OpenAntares.Simulation.ShipDesigns.ShipDesignAction.Save(name,recipe));
        _shipDesignEditor.CancelRequested+=()=>_shipDesigns.ShowCatalogue(_session.Content,View);
        _fleetCommand.CombatRequested+=action=>OpenOverviewChild("Fleet","Combat",()=>OpenCombat(action));
        _combatResponse.Pressed += OpenCombatResponse;
        _fleetCommand.CommandIssued+=Execute;_assemblyRequest.CommandIssued+=Execute;_officerDetails.CommandIssued+=Execute;
        AddChild(_conflictOrders);_conflictOrders.CommandIssued+=Execute;
        _fleetCommand.ConflictOrdersRequested+=anchor=>OpenOverviewChild("Fleet","Conflict",()=>OpenConflictOrders(anchor));
        _fleetCommand.DisplayApplied+=()=>Callable.From(Refresh).CallDeferred();
        foreach(var dialog in new Window[]{_fleetCommand,_assemblyRequest,_officerDetails,_combatOrders,_shipDesigns,_shipDesignEditor,_refit})dialog.VisibilityChanged+=()=>Callable.From(OpenPendingCommandInterrupt).CallDeferred();
        _fleetCommand.RequestRequested+=request=>OpenOverviewChild("Fleet","Assembly",()=>_assemblyRequest.ShowEditor(_session.Content,View,request));
        _fleetCommand.OfficersRequested+=()=>OpenOverviewChild("Fleet","Officers",()=>_officerDetails.ShowOfficers(_session.Content,View));
        _fleetCommand.StarRequested+=star=>{_fleetCommand.Hide();_map.Select(star);ShowWorkspace("Galaxy");};
        _fleetCommand.InspectRequested+=id=>{_selectedVessel=id;_selectedPlanet=null;_courseDestination=null;var ship=View.Vessels.Single(v=>v.Id==id);_map.Select(ship.StationaryStarId??ship.Transit!.Destination);ShowWorkspace("Vessels");Callable.From(Refresh).CallDeferred();};
        _assemblyRequest.ColonyRequested+=SelectColony;
        AddChild(_scoutOverview);_scoutOverview.CommandIssued+=Execute;_scoutOverview.StarRequested+=star=>{_scoutOverview.Hide();_map.Select(star);ShowWorkspace("Galaxy");};_scoutOverview.DisplayApplied+=_=>Callable.From(Refresh).CallDeferred();

        AddChild(_crisisOverview);AddChild(_crisisResponse);AddChild(_crisisNotifications);
        _crisisNotifications.Applied+=preferences=>{_crisisPreferences=preferences;Callable.From(Refresh).CallDeferred();};
        _crisisNotifications.CommandRequested+=Execute;
        _colonies.CrisisNotificationsRequested+=()=>{if(View.Crisis is {} crisis)_crisisNotifications.Open(CrisisPreferencesPath,crisis);};
        _crisisResponse.VisibilityChanged+=()=>{if(!_crisisResponse.Visible&&_returnToCrisisOverview&&View.Crisis is {} c){_returnToCrisisOverview=false;Callable.From(()=>{if(View.Crisis is {} latest)_crisisOverview.ShowCrisis(_session.Content,latest);}).CallDeferred();}};
        _crisisOverview.CommandRequested+=Execute;_crisisResponse.CommandRequested+=Execute;
        _crisisOverview.QuarantineRequested+=(ids,enable,release)=>{_returnToCrisisOverview=_crisisOverview.Visible;_crisisOverview.Hide();var preview=OpenAntares.Simulation.Crises.CrisisQueries.PreviewQuarantine(_session.Content,_session.State,_viewer,ids,enable,release);Callable.From(()=>_crisisResponse.ShowQuarantine(preview)).CallDeferred();};
        _crisisOverview.PurgeRequested+=id=>{_returnToCrisisOverview=_crisisOverview.Visible;_crisisOverview.Hide();var preview=OpenAntares.Simulation.Crises.CrisisQueries.PreviewPurge(_session.Content,_session.State,_viewer,id);Callable.From(()=>_crisisResponse.ShowPurge(preview)).CallDeferred();};
        _crisisOverview.InspectionRequested+=vessel=>{_returnToCrisisOverview=_crisisOverview.Visible;_crisisOverview.Hide();var view=View.Crisis;if(view is null)return;var current=view.Vessels.FirstOrDefault(v=>v.Id==vessel.Id);if(current is null)return;Callable.From(()=>_crisisResponse.ShowInspection(view,current)).CallDeferred();};
        _crisisOverview.StarRequested+=star=>{_map.Select(star);ShowWorkspace("Galaxy");};
        _colonies.CrisisRequested+=()=>{if(View.Crisis is {} crisis)_crisisOverview.ShowCrisis(_session.Content,crisis);};
        AddChild(_management); _management.CommandIssued += Execute; _management.ColonyRequested += SelectColony;
        AddChild(_notificationDialog); _notificationDialog.Applied += preferences => { _preferences = preferences; Callable.From(Refresh).CallDeferred(); };
        _colonies.ManageRequested += () => { _management.Preferences = _preferences; _management.ShowEmpire(_session.Content,View); };
        _colonies.NotificationsRequested += () => _notificationDialog.Open(NotificationPreferencesPath);
        _colonies.LatestReportRequested += ShowLatestReport;
        AddChild(_diplomacy);_diplomacy.CommandIssued+=Execute;
        _diplomacyEntry.Pressed+=()=>ToggleCommandPage("OpenDiplomacy",OpenDiplomacyWorkspace);
        _contacts.DiplomacyRequested+=id=>_diplomacy.ShowEmpire(_session.Content,View,id);
        _report.DiplomacyRequested+=id=>{BeginReportExcursion();_diplomacy.ShowEmpire(_session.Content,View,id);};
        AddChild(_report);
        _report.VisibilityChanged+=()=>{if(!_report.Visible&&_pendingCrisisInterrupt)Callable.From(OpenPendingCommandInterrupt).CallDeferred();};
        _report.VisibilityChanged+=()=>{if(!_report.Visible&&_pendingScoutInterrupt)Callable.From(OpenPendingScoutInterrupt).CallDeferred();};
        _crisisOverview.VisibilityChanged+=()=>{if(!_crisisOverview.Visible&&_pendingScoutInterrupt)Callable.From(OpenPendingScoutInterrupt).CallDeferred();};
        _crisisResponse.VisibilityChanged+=()=>{if(!_crisisResponse.Visible&&_pendingScoutInterrupt)Callable.From(OpenPendingScoutInterrupt).CallDeferred();};
        AddChild(_templateReferences);
        _report.ChooseProjectRequested += colony => { BeginReportExcursion(); ChooseProject(colony); };
        _report.StarRequested += star => { BeginReportExcursion(); _map.Select(star); ShowWorkspace("Galaxy"); };
        _report.ColonyRequested += colony => { BeginReportExcursion(); SelectColony(colony); };
        _report.VesselRequested += vessel => { BeginReportExcursion(); _selectedVessel = vessel; _courseDestination = null; ShowWorkspace("Vessels"); Callable.From(Refresh).CallDeferred(); };
        _report.GuidanceDestinationRequested += destination => { BeginReportExcursion(); RouteGuidance(destination); };
        _report.ChooseResearchRequested += () => { BeginReportExcursion(); OpenResearch(); };
        _report.FleetCommandRequested+=()=>{BeginReportExcursion();OpenFleetCommand();};
        _report.ConflictOrdersRequested+=anchor=>{BeginReportExcursion();OpenConflictOrders(anchor);};

        var body = new HBoxContainer { Name = "StrategyBody", SizeFlagsVertical = SizeFlags.ExpandFill };
        AddChild(body);
        body.AddChild(_mainArea);
        _mainArea.SizeFlagsVertical = SizeFlags.ExpandFill;
        _map.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _map.SizeFlagsVertical = SizeFlags.ExpandFill;
        _map.StarSelected += star => { if (!_restoringNavigation) _requireColonyChoice = false; _courseDestination = star; if (_mapFleetSelection.Count == 0) _fleetListStar = null; Refresh(); };
        _map.OwnedVesselsSelected += SelectOwnedVesselsAt;
        var overlays = new HFlowContainer { Name = "MapOverlays" };
        _mainArea.AddChild(overlays);
        var overlayGroup = new ButtonGroup();
        foreach (string overlay in new[] { "Owners", "Range", "Threat", "Habitat" })
        {
            var key = new Button { Text = overlay, Name = "MapOverlay" + overlay, ToggleMode = true, ButtonGroup = overlayGroup, ButtonPressed = overlay == "Owners", ThemeTypeVariation = "CompactCommandKey", Disabled = overlay == "Range", TooltipText = overlay == "Range" ? "This campaign has no finite ship-range rule. Travel time is shown for the selected route." : "Show known " + overlay.ToLowerInvariant() + "; unknown systems remain unknown." };
            key.Pressed += () => { _map.Overlay = overlay; _map.QueueRedraw(); };
            overlays.AddChild(key);
        }
        _map.ConfigurePresentation(_session.Content);
        AddMapZoomKeys(overlays);
        _mainArea.AddChild(_map);
        _mainArea.AddChild(_workspaceHeader);
        _workspaceHeader.AddChild(_workspaceTitle);
        _mainArea.AddChild(_workspaceTabs);
        _mainArea.AddChild(_windowFrame);
        _mainArea.AddChild(_workspaceScroll);
        var workPadding = new MarginContainer { ThemeTypeVariation = "PanelPadding", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _workspaceScroll.AddChild(workPadding);
        workPadding.AddChild(_workspacePages);
        _workspaceScroll.Resized += () => KeepFocusVisibleAfterResize(_workspaceScroll);
        var colonyPage = AddWorkspace("Colony");
        colonyPage.AddChild(_colonyChoice);
        _colonyChoice.ItemSelected += index => SelectColony(new ColonyId(_colonyChoice.GetItemMetadata((int)index).AsInt32()));
        colonyPage.AddChild(_noColony);
        colonyPage.AddChild(_colonyPanel);
        var colonyTools = new Button { Name = "OpenColonyTools", Text = "All colonies / plans / notifications", Visible = Rules.HasExpansion(View.RulesVersion) };
        colonyTools.Pressed += () => ShowWorkspace("Colonies");
        colonyPage.AddChild(colonyTools);
        AddWorkspace("Colonies").AddChild(_colonies);
        var ledgerPage = AddWorkspace("Ledger");
        ledgerPage.AddChild(_ledger);
        var advancedColonies = new Button { Text = "Plans, notifications and colony alerts", Name = "OpenColonyPlans" };
        advancedColonies.Pressed += () => ShowWorkspace("Colonies");
        ledgerPage.AddChild(advancedColonies);
        _ledger.ColonySelected += SelectColony;
        _ledger.CommandIssued += Execute;
        AddWorkspace("Research").AddChild(_researchBar);
        AddWorkspace("System").AddChild(_starPanel);
        AddWorkspace("Vessels").AddChild(_vessels);
        AddWorkspace("Contacts").AddChild(_contacts);
        BuildDecisionsPage(AddWorkspace("Decisions"));
        _colonies.ColonySelected += SelectColony;
        _starPanel.PlanetSelected += planet => { _selectedPlanet = planet; _selectedColony = View.Colonies.FirstOrDefault(c => c.PlanetId == planet)?.Id; if (_selectedColony is not null) ShowWorkspace("Colony"); Callable.From(Refresh).CallDeferred(); };
        _colonyPanel.CommandIssued += Execute;
        _vessels.CommandIssued += Execute;
        _vessels.RefitRequested+=OpenRefit;
        _vessels.DiplomacyRequested+=()=>_diplomacy.ShowEmpire(_session.Content,View);
        _colonyPanel.RefitRequested+=OpenRefit;
        _colonyPanel.ShipDesignsRequested+=()=>_shipDesigns.ShowCatalogue(_session.Content,View);
        _fleetCommand.RefitRequested+=vessel=>OpenOverviewChild("Fleet","Refit",()=>OpenRefit(vessel));
        _vessels.VesselSelected += vessel => { _selectedVessel = vessel; _requireVesselChoice = false; _courseDestination = null; Callable.From(Refresh).CallDeferred(); };
        _vessels.DestinationSelected += star => { _courseDestination = star; _map.Select(star); };
        _vessels.ScoutOverviewRequested+=()=>{if(View.Scouting is {} scouting)_scoutOverview.ShowScouting(_session.Content,scouting);};
        _vessels.FleetCommandRequested+=OpenFleetCommand;
        _vessels.ConflictOrdersRequested+=()=>OpenConflictOrders(_selectedVessel);

        body.AddChild(_consolePanel);
        var consolePadding = new MarginContainer { ThemeTypeVariation = "ConsolePadding" };
        _consolePanel.AddChild(consolePadding);
        _normalConsole = consolePadding;
        _consolePanel.AddChild(_reportDock);
        // Reserve the scrollbar width so wrapped notices cannot repeatedly reflow
        // when their height is close to the console viewport height.
        var consoleScroll = new ScrollContainer { Name = "ConsoleScroll", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, VerticalScrollMode = ScrollContainer.ScrollMode.Reserve, FollowFocus = true };
        consolePadding.AddChild(consoleScroll);
        consoleScroll.Resized += () => KeepFocusVisibleAfterResize(consoleScroll);
        var console = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        consoleScroll.AddChild(console);
        console.AddChild(_legacyNotice);
        // The console shows only what is selected on the map: a system card or a fleet card.
        // Empire-wide notices and overviews live behind the command bar (Decisions, Colonies, Fleets).
        BuildSelectionHeader(console);
        WireFleetPanel(console);
        BuildSelectionConsole(console);
        _message.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        foreach (var button in console.GetChildren().OfType<Button>())
        {
            button.ClipText = true;
            if (string.IsNullOrEmpty(button.TooltipText)) button.TooltipText = button.Text;
        }

        // Keep rejection/status text visible even when narrow management pages hide the console.
        AddChild(_message);
        AddChild(_feedbackActions);
        _feedbackActions.AddChild(_reviewFeedback); _feedbackActions.AddChild(_dismissFeedback);
        _dismissFeedback.Pressed += () => ShowMessage(string.Empty, false);
        _reviewFeedback.Pressed += () => { if (_feedbackDestination is { } destination) RouteGuidance(destination); };
        var navigationPanel = new PanelContainer { ThemeTypeVariation = "CommandBar" };
        AddChild(navigationPanel);
        var navigationPadding = new MarginContainer { ThemeTypeVariation = "NavigationPadding" };
        navigationPanel.AddChild(navigationPadding);
        var navigation = new CommandBar { Name = "MainNavigation" };
        navigationPadding.AddChild(navigation);
        AddNavigation(navigation, "Galaxy", "OpenGalaxyWorkspace", OpenGalaxyPage, "Galaxy");
        AddNavigation(navigation, "Colonies", "OpenColonyWorkspace", () => ShowWorkspace("Ledger"), "Ledger");
        if (Rules.HasExpansion(View.RulesVersion)) AddNavigation(navigation, "Fleets", "OpenFleetCommand", () => { if (Rules.HasFleets(View.RulesVersion)) OpenFleetCommand(); else ShowWorkspace("Vessels"); });
        AddNavigation(navigation, "Research", "OpenResearch", OpenResearch);
        if (Rules.HasScouting(View.RulesVersion)) AddNavigation(_workspaceTabs, "Scouts", "OpenScoutOverview", () => { if (View.Scouting is { } scouting) _scoutOverview.ShowScouting(_session.Content, scouting); });
        _diplomacyEntry.Visible = Rules.HasRivals(View.RulesVersion);
        _diplomacyEntry.Text = Rules.HasDiplomacy(View.RulesVersion) ? "Diplomacy" : "Contacts";
        navigation.AddChild(_diplomacyEntry);
        if (Rules.HasRivals(View.RulesVersion)) AddNavigation(_workspaceTabs, "Contacts", "OpenContactsWorkspace", () => ShowWorkspace("Contacts"), "Contacts");
        if (Rules.HasShipDesigns(View.RulesVersion)) AddNavigation(navigation, "Designs", "OpenShipDesigns", () => _shipDesigns.ShowCatalogue(_session.Content, View));
        _alertsEntry.Pressed += () => { if (_workspace == "Decisions") { _commandPage = null; ShowWorkspace("Galaxy"); } else { ShowWorkspace("Decisions"); _commandPage = "OpenAttentionConsole"; } };
        navigation.AddChild(_alertsEntry);
        _returnReportEntry.Pressed += ReturnToReport;
        navigation.AddChild(_returnReportEntry);
        var gameMenu = new MenuButton { Text = "Game", Name = "GameMenu" };
        gameMenu.GetPopup().AddItem("Save game", 0);
        gameMenu.GetPopup().AddItem("Load game", 1);
        gameMenu.GetPopup().AddItem("Latest turn report", 2);
        gameMenu.GetPopup().AddSeparator();
        gameMenu.GetPopup().AddCheckItem("Screen effects", 3);
        gameMenu.GetPopup().AddCheckItem("Reduced effects", 4);
        gameMenu.GetPopup().AddSeparator();
        gameMenu.GetPopup().AddItem("Main menu", 5);
        ConfigureEffects(gameMenu);
        gameMenu.GetPopup().IdPressed += id => { if (id == 0) _saveDialog.Open(_session, SuggestedSaveName()); else if (id == 1) LoadRequested?.Invoke(); else if (id == 2) ShowLatestReport(); else if (id == 5) MainMenuRequested?.Invoke(); else ToggleEffects(gameMenu, id); };
        navigation.AddChild(gameMenu);
        _endTurn.Name = "EndTurn";
        _endTurn.ThemeTypeVariation = "EndTurnButton";
        navigation.AddChild(_endTurn);
        foreach (Control control in navigation.GetChildren()) control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        navigation.Configure();
        Resized += UpdateWorkspaceLayout;
        ShowWorkspace("Galaxy");

        WireOverviewReturns();
        WireReportReturns();
        WireVisualWorkspaces();
        _researchBar.PickerClosed += () => { if (_returnToReport) TryReturnFromOverview(); };
        _map.ShowGalaxy(View.Galaxy);
        if (HomeStar() is { } start)
        {
            _map.Select(start);
        }

        Refresh();
    }

    private VBoxContainer AddWorkspace(string name)
    {
        var page = new VBoxContainer { Name = name + "Workspace", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _pages.Add(name, page);
        _workspacePages.AddChild(page);
        return page;
    }

    private void AddNavigation(HFlowContainer navigation, string text, string name, System.Action action, string? workspace = null)
    {
        var button = new Button { Text = text, Name = name, ToggleMode = workspace is not null };
        button.Pressed += () => { if (navigation is CommandBar) ToggleCommandPage(name, action); else action(); };
        navigation.AddChild(button);
        if (workspace is not null) _workspaceButtons.Add(workspace, button);
    }

    private void ShowWorkspace(string workspace)
    {
        DismissReportForNavigation();
        CloseVisualWorkspace();
        RecordWorkspaceChange(workspace);
        _workspace = workspace;
        _map.Visible = workspace == "Galaxy";
        _mainArea.GetNode<Control>("MapOverlays").Visible = workspace == "Galaxy";
        _workspaceHeader.Visible = _workspaceScroll.Visible = workspace != "Galaxy";
        _windowFrame.Hide();
        _workspaceTitle.Text = workspace == "Colony" ? "Colony management" : workspace == "Ledger" ? "Colonies" : workspace;
        UpdateVisualTabs(workspace);
        foreach (var page in _pages) page.Value.Visible = page.Key == workspace;
        foreach (var button in _workspaceButtons) button.Value.SetPressedNoSignal(button.Key == workspace);
        _workspaceScroll.ScrollVertical = 0;
        UpdateWorkspaceLayout();
    }

    private void UpdateWorkspaceLayout()
    {
        bool compact = Size.X < 1100;
        _consolePanel.CustomMinimumSize = new Vector2(GetThemeConstant(compact ? "compact_console_width" : "console_width", "ConsoleMetrics"), 0);
        _consolePanel.Visible = _workspace == "Galaxy" || _report.Visible;
        ApplyTouchTargets();
    }

    private void OpenColonies()
    {
        if (_requireColonyChoice) { ShowWorkspace("Colonies"); return; }
        if (_selectedColony is { } selected) SelectColony(selected);
        else if (View.Colonies.FirstOrDefault() is { } first) SelectColony(first.Id);
        else ShowWorkspace("Colony");
    }

    private void SelectOwnedVesselsAt(StarId star)
    {
        var owned = View.Vessels.Where(v => v.StationaryStarId == star).OrderBy(v => v.Id).ToArray();
        ClearMapFleetSelection(); _hoverDestination=null; _fleetListStar=star;
        _map.ClearSystemSelection();
        Refresh();
        if (owned.Length == 0) ShowMessage("Those vessels are no longer available. Choose an owned vessel.", true);
    }

    private void OpenResearch()
    {
        _report.Hide();
        ShowWorkspace("Research");
    }

    private async void KeepFocusVisibleAfterResize(ScrollContainer scroll)
    {
        // FollowFocus owns newly acquired focus. Only repair a control that already
        // held focus when resizing began; otherwise both paths can scroll in one frame.
        if (!IsInsideTree()) return;
        Control? alreadyFocused = GetViewport().GuiGetFocusOwner();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(scroll) || !IsInsideTree()) return;
        Control? focused = GetViewport().GuiGetFocusOwner();
        if (focused is not null && object.ReferenceEquals(focused, alreadyFocused) && scroll.IsAncestorOf(focused)) scroll.EnsureControlVisible(focused);
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
        _readyRejectionVisible = false;
        CommandOutcome outcome = _session.Execute(command);
        if(command is OpenAntares.Simulation.Conflict.ConflictCommand&&_conflictOrders.Visible)_conflictOrders.ShowResult(outcome.Rejection?.Message??"",outcome.Accepted);
        ShowMessage(outcome.Rejection?.Message ?? string.Empty, isWarning: true);
        if (command is SetReadyCommand && outcome.Rejection is { } rejection) ExplainReadyRejection(rejection.Code);
        if(_combatOrders.Visible&&command is OpenAntares.Simulation.Combat.CombatActionCommand){if(outcome.Accepted)_combatOrders.Hide();else _combatOrders.ShowError(outcome.Rejection?.Message??"");}
        if(_refit.Visible&&command is OpenAntares.Simulation.ShipDesigns.RefitCommand refitCommand){if(outcome.Accepted){_refit.ShowError("");if(refitCommand.Action is OpenAntares.Simulation.ShipDesigns.RefitAction.Cancel)_refit.Hide();else _refit.ShowRefit(_session.Content,View,_refit.Vessel,false);}else _refit.ShowError(outcome.Rejection?.Message??"");}
        if(command is OpenAntares.Simulation.Diplomacy.DiplomacyCommand or OpenAntares.Simulation.Conflict.ConflictCommand)_diplomacy.ShowError(outcome.Rejection?.Message??"",outcome.Accepted);
        if(_fleetCommand.Visible)_fleetCommand.ShowError(outcome.Rejection?.Message??"");
        if(_shipDesigns.Visible)_shipDesigns.ShowError(outcome.Rejection?.Message??"");
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
        var priorFocus = GetViewport().GuiGetFocusOwner();
        string? focusKey = priorFocus is not null && IsAncestorOf(priorFocus) && !priorFocus.Name.ToString().StartsWith("@", System.StringComparison.Ordinal) ? priorFocus.Name.ToString() : null;
        ulong focusId = priorFocus?.GetInstanceId() ?? 0;
        var view = View;
        _statusStrip.ShowEmpire(view, _session.Content);
        _ledger.ShowColonies(_session.Content, view);
        ReconcileSelections(view);
        _guidance.ShowGuidance(view);
        _fleetCommand.CombatPreferencesPath=CombatPreferencesPath;_report.CombatPreferencesPath=CombatPreferencesPath;
        if(_combatOrders.Visible)_combatOrders.UpdateView(view);RefreshCombatAttention(view);
        if(_conflictOrders.Visible)_conflictOrders.UpdateView(view);
        int responses=view.Conflict?.Encounters.Count(e=>e.OwnNeedsResponse)??0;
        _conflictBadge.Text=responses>0?$"Conflict Emergency: {responses} revised force response(s) required before Ready. Review your exact force and battle orders.":view.Conflict?.Encounters.Length>0?$"Empire conflict: {view.Conflict.Encounters.Length} active battle(s). Review damage, withdrawal and return orders.":"";
        _conflictBadge.Visible = _conflictBadge.Text.Length > 0;
        _fleetCommand.PreferencesPath=FleetPreferencesPath;
        _report.FleetPreferencesPath=FleetPreferencesPath;
        if(_fleetCommand.Visible)_fleetCommand.ShowEmpire(_session.Content,view);
        if(_shipDesigns.Visible)_shipDesigns.ShowCatalogue(_session.Content,view);
        if(_shipDesignEditor.Visible)_shipDesignEditor.UpdateView(_session.Content,view);
        if(_refit.Visible)_refit.ShowRefit(_session.Content,view,_refit.Vessel,false);
        if(_assemblyRequest.Visible)_assemblyRequest.UpdateView(_session.Content,view);
        if(_officerDetails.Visible)_officerDetails.ShowOfficers(_session.Content,view);
        RefreshFleetAttention(view);
        _preferences = OpenAntares.Game.Presentation.ColonyNotificationPreferences.Load(ProjectSettings.GlobalizePath(NotificationPreferencesPath));
        _crisisPreferences=OpenAntares.Game.Presentation.CrisisNotificationPreferences.Load(ProjectSettings.GlobalizePath(CrisisPreferencesPath));
        if (_management.Visible) { _management.Preferences = _preferences; _management.ShowEmpire(_session.Content,view); }
        if(_crisisOverview.Visible&&view.Crisis is {} currentCrisis)_crisisOverview.ShowCrisis(_session.Content,currentCrisis);
        _endTurn.Text = view.Research.Ready ? "Waiting for others (cancel)" : "End Turn";
        _endTurn.Disabled=responses>0&&!view.Research.Ready;
        RefreshVisualConsole(view);
        _researchBar.ShowEmpire(_session.Content, view.Research);
        ReconcileMapFleetSelection(view);
        RefreshFleetList(view);
        _map.ShowFleetPresentation(view, _mapFleetSelection, _hoverDestination ?? _courseDestination);
        _map.ShowGalaxy(view.Galaxy);
        _map.ShowKnownAmebas(view.Scouting);
        RefreshScoutAttention(view.Scouting);
        _contacts.Show(view);
        int unanswered = view.Diplomacy?.Contacts.Count(c=>c.Pending is {OwnAnswered:false} p&&p.Recipient==view.Viewer) ?? 0;
        _diplomacyEntry.Text = unanswered > 0 ? $"Diplomacy ({unanswered})" : Rules.HasDiplomacy(view.RulesVersion) ? "Diplomacy" : "Contacts";
        if (unanswered == 0 && view.Conflict?.Notices.Length > 0) _diplomacyEntry.Text = "Diplomacy — conflict attention";
        _diplomacyEntry.TooltipText = unanswered > 0 ? $"{unanswered} unanswered offers" : "Open diplomacy";
        _combatResponse.Visible = _combatBadge.Visible;
        if(_diplomacy.Visible)_diplomacy.ShowEmpire(_session.Content,view);
        RefreshCrisisAttention(view.Crisis);
        _legacyNotice.Visible = view.RulesVersion == Rules.LegacyVersion;
        if (_map.Selected is not { } selected) { RefreshSelectionConsole(view, null, null); ApplyTouchTargets(); return; }
        var star = view.Stars.Single(s => s.Chart.Id == selected);
        if (star.Planets is { } planets && !planets.Any(p => p.Id == _selectedPlanet))
        {
            var preferred = _requireColonyChoice ? null : view.Colonies.FirstOrDefault(c => c.Id == _selectedColony && c.StarId == selected) ?? (_requireColonyChoice ? null : view.Colonies.FirstOrDefault(c => c.StarId == selected));
            _selectedPlanet = preferred?.PlanetId ?? (!_requireColonyChoice && planets.Length > 0 ? planets[0].Id : null);
        }
        if (star.Planets is null) _selectedPlanet = null;
        var colony = view.Colonies.FirstOrDefault(c => c.PlanetId == _selectedPlanet && c.StarId == selected);
        _selectedColony = colony?.Id;
        if (colony is not null) _selectedPlanet = colony.PlanetId;
        _starPanel.ShowStar(_session.Content, star, view);
        _colonies.ShowColonies(view, _selectedColony, _session.Content, _preferences, _latestReport);
        _colonyChoice.Clear();
        foreach (var owned in view.Colonies)
        {
            _colonyChoice.AddItem(view.Research.ColonyNames[owned.Id.ToString()]);
            _colonyChoice.SetItemMetadata(_colonyChoice.ItemCount - 1, owned.Id.Value);
            if (owned.Id == _selectedColony) _colonyChoice.Select(_colonyChoice.ItemCount - 1);
        }
        if (_selectedColony is null) _colonyChoice.Select(-1);
        _colonyChoice.Disabled = view.Colonies.Length == 0;
        _noColony.Visible = colony is null;
        RefreshSelectionConsole(view, star, colony);
        _colonyAttention.Text = string.Join("\n", _colonies.GetChildren().OfType<Label>().Where(l => l.ThemeTypeVariation == "WarningLabel").Select(l => l.Text));
        _colonyAttention.Visible = _colonyAttention.Text.Length > 0;
        var attention = new[] { _colonyAttention, _combatBadge, _fleetBadge, _scoutBadge, _conflictBadge }.Where(l => l.Visible && l.Text.Length > 0).Select(l => l.Text).ToArray();
        _alertsEntry.Visible = attention.Length > 0 || OpenAntares.Game.Presentation.TurnGuidance.Build(view).Count > 0;
        _alertsEntry.TooltipText = string.Join("\n", attention);
        if (colony is null) _colonyPanel.Clear(); else _colonyPanel.ShowColony(_session.Content, colony, view);
        _vessels.ShowVessels(_session.Content, view, _selectedVessel, _courseDestination, _selectedPlanet);
        ApplyTouchTargets();
        if (focusKey is not null) RestoreSemanticFocus(focusKey, focusId, _workspace);
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
    private void OpenRefit(VesselId vessel)=>_refit.ShowRefit(_session.Content,View,vessel);
    private void DesignAction(OpenAntares.Simulation.ShipDesigns.ShipDesignAction action)
    {
        var result=_session.Execute(new OpenAntares.Simulation.ShipDesigns.ShipDesignCommand(_viewer,View.Turn,View.ShipDesigns!.CatalogueRevision,action));
        ShowMessage(result.Rejection?.Message??string.Empty,isWarning:true);
        if(result.Accepted){_shipDesignEditor.Hide();_shipDesigns.ShowCatalogue(_session.Content,View);_shipDesigns.ShowError("");}
        else{if(_shipDesignEditor.Visible)_shipDesignEditor.ShowError(result.Rejection?.Message??"");if(_shipDesigns.Visible)_shipDesigns.ShowError(result.Rejection?.Message??"");}
        Callable.From(Refresh).CallDeferred();
    }
    private void OpenFleetCommand(){if(Rules.HasFleets(View.RulesVersion)){_fleetCommand.PreferencesPath=FleetPreferencesPath;_fleetCommand.ShowEmpire(_session.Content,View);}}
    private void OpenConflictOrders(VesselId? anchor){if(View.Conflict is not null)_conflictOrders.Open(_session.Content,View,anchor);}
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
        if(_report.Visible||_refit.Visible||_shipDesigns.Visible||_shipDesignEditor.Visible||_fleetCommand.Visible||_assemblyRequest.Visible||_officerDetails.Visible||_combatOrders.Visible)_pendingCrisisInterrupt=true;
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
        if(!IsInsideTree()||!_pendingScoutInterrupt||_report.Visible||_crisisOverview.Visible||_crisisResponse.Visible||_refit.Visible||_shipDesigns.Visible||_shipDesignEditor.Visible||_fleetCommand.Visible||_assemblyRequest.Visible||_officerDetails.Visible||_combatOrders.Visible)return;
        _pendingScoutInterrupt=false;if(View.Scouting is {} current)_scoutOverview.ShowScouting(_session.Content,current);
    }
    private void OpenPendingCommandInterrupt()
    {
        if(!IsInsideTree()||_report.Visible||_refit.Visible||_shipDesigns.Visible||_shipDesignEditor.Visible||_fleetCommand.Visible||_assemblyRequest.Visible||_officerDetails.Visible||_combatOrders.Visible||_crisisOverview.Visible||_crisisResponse.Visible)return;
        if(_pendingCrisisInterrupt){_pendingCrisisInterrupt=false;if(View.Crisis is {} current)_crisisOverview.ShowCrisis(_session.Content,current);}else OpenPendingScoutInterrupt();
    }

    private void SelectColony(ColonyId id)
    {
        var colony = View.Colonies.FirstOrDefault(c => c.Id == id);
        if (colony is null) { _selectedColony = null; _selectedPlanet = null; _requireColonyChoice = true; ShowMessage("That colony is no longer available. Choose an owned colony before reviewing another order.", true); Refresh(); return; }
        _requireColonyChoice = false;
        _selectedColony = id; _selectedPlanet = colony.PlanetId; _map.Select(colony.StarId);
        ShowWorkspace("Colony");
    }

    private void ShowLatestReport()
    {
        if(_latestReport is null)return;
        if (_returnToReport && _reportThroughTurn == _latestReport.ToTurn) { ReturnToReport(); return; }
        _returnToReport = false; _reportWorkspaceExcursion = false;
        _returnReportEntry.Hide();
        _reportOrigin = CaptureWorkspace();
        var reportOrigin=_reportOrigin;
        ShowWorkspace("Galaxy");
        _reportOrigin=reportOrigin;
        _reportDock.Show();
        _normalConsole?.Hide();
        _consolePanel.Show();
        _scoutOverview.Hide();
        var view=View;
        if(view.Development is {} development)view=view with{Development=development with{PendingReport=null}};
        _report.ShowReport(_session.Content,_latestReport,view,_preferences,_crisisPreferences);
    }

    private void ShowMessage(string text, bool isWarning)
    {
        if (_readyRejectionVisible && isWarning && text.Length > 0 && text != _message.Text) return;
        _readyRejectionVisible = false; _feedbackDestination = null;
        _reviewFeedback.Hide(); _feedbackActions.Visible = isWarning && text.Length > 0;
        _message.Text = text;
        _message.Visible = text.Length > 0;
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
