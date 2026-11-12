using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Screens;

/// <summary>
/// The right console shows only what is selected on the map: a system card (with its planets,
/// the chosen planet or colony, and the fleets in that system) or a fleet card. Empire-wide
/// notices live on the Decisions page that the command bar opens. Presentation only.
/// </summary>
public partial class GameScreen
{
    private readonly Label _selectionKind = new() { Name = "ConsoleSelectionKind", ThemeTypeVariation = "StencilLabel", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly Label _selectionHint = new() { Name = "ConsoleSelectionHint", Text = "ESC CLEARS", ThemeTypeVariation = "StencilLabel", Visible = false };
    private readonly VBoxContainer _systemContext = new() { Name = "SystemContext", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly VBoxContainer _systemFleets = new() { Name = "SystemFleets" };
    private readonly Button _inspectSystem = new() { Name = "InspectSelectedSystem", Text = "Inspect system" };
    private Button _conflictOrdersEntry = null!;

    private void BuildSelectionHeader(VBoxContainer console)
    {
        var header = new HBoxContainer { Name = "ConsoleSelectionHeader" };
        console.AddChild(header);
        header.AddChild(_selectionKind);
        header.AddChild(_selectionHint);
    }

    private void BuildSelectionConsole(VBoxContainer console)
    {
        console.AddChild(_systemContext);
        var header = new PanelContainer { Name = "SystemHeader", ThemeTypeVariation = "ScreenInset" };
        _systemContext.AddChild(header);
        var headerRows = new VBoxContainer();
        header.AddChild(headerRows);
        headerRows.AddChild(_selectionTitle);
        headerRows.AddChild(_selectionSummary);
        _systemContext.AddChild(_systemCard);
        var actions = new HFlowContainer { Name = "SelectionActions" };
        _systemContext.AddChild(actions);
        _manageSelected.Pressed += OpenColonies;
        _inspectSystem.Pressed += () => ShowWorkspace("System");
        foreach (var key in new[] { _manageSelected, _inspectSystem })
        {
            key.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            key.ClipText = false;
            key.ThemeTypeVariation = "CompactCommandKey";
            key.TooltipText = key.Text;
            actions.AddChild(key);
        }
        _systemContext.AddChild(_systemFleets);
        // Conflict orders are empire-wide (Decisions, Fleets). They appear here only for a system in conflict.
        _conflictOrdersEntry = new Button { Name = "OpenConflictOrders", Text = "Conflict orders", TooltipText = "Empire conflict orders / revised force response", Visible = false };
        _conflictOrdersEntry.Pressed += () => OpenConflictOrders(null);
        console.AddChild(_conflictOrdersEntry);
    }

    /// <summary>Everything empire-wide that used to sit in the console: guidance, attention notices and their entries.</summary>
    private void BuildDecisionsPage(VBoxContainer page)
    {
        _guidance.CollapseOptional = false;
        _guidance.DestinationRequested += RouteGuidance;
        page.AddChild(_guidance);
        page.AddChild(new HSeparator());
        foreach (var badge in new[] { _colonyAttention, _combatBadge, _fleetBadge, _scoutBadge, _conflictBadge }) page.AddChild(badge);
        page.AddChild(_combatResponse);
        var conflict = new Button { Name = "DecisionsConflictOrders", Text = "Conflict orders", TooltipText = "Empire conflict orders / revised force response", Visible = Rules.HasConflict(View.RulesVersion) };
        conflict.Pressed += () => OpenConflictOrders(null);
        page.AddChild(conflict);
        var colonies = new Button { Name = "OpenColonyOverview", Text = "Colony overview / alerts", TooltipText = "Colony overview / alerts", Visible = Rules.HasExpansion(View.RulesVersion) };
        colonies.Pressed += () => ShowWorkspace("Colonies");
        page.AddChild(colonies);
    }

    private static Label ConsoleText(string text, string variation = "SubtleLabel") => new()
    {
        Text = text, ThemeTypeVariation = variation, AutowrapMode = TextServer.AutowrapMode.WordSmart,
        SizeFlagsHorizontal = SizeFlags.ExpandFill
    };

    private static string Counted(int count, string one, string many) => count + " " + (count == 1 ? one : many);

    private static string PlanetOwner(EmpireView view, KnownPlanetView planet) =>
        planet.OwnerId is null ? "Unclaimed"
        : planet.OwnerId == view.Viewer ? "Your colony"
        : view.Contacts.FirstOrDefault(c => c.OtherEmpireId == planet.OwnerId)?.CultureName ?? "Observed colony";

    private Texture2D RoleIcon(string roleId) =>
        GetThemeIcon(HasThemeIcon("role_" + roleId, "Icons") ? "role_" + roleId : "escort", "Icons");

    /// <summary>Rebuilds the console for the current map selection. A null star means no system is selected.</summary>
    private void RefreshSelectionConsole(EmpireView view, StarView? star, OwnedColonyView? colony)
    {
        foreach (Node child in _systemCard.GetChildren()) { _systemCard.RemoveChild(child); child.QueueFree(); }
        foreach (Node child in _systemFleets.GetChildren()) { _systemFleets.RemoveChild(child); child.QueueFree(); }
        bool fleetCard = _fleetListStar is not null;
        _selectionKind.Text = fleetCard ? "SELECTED · FLEET" : star is not null ? "SELECTED · STAR SYSTEM" : "NOTHING SELECTED";
        _selectionHint.Visible = fleetCard || star is not null;
        _systemContext.Visible = !fleetCard;
        StarId? focus = _fleetListStar ?? star?.Chart.Id;
        _conflictOrdersEntry.Visible = Rules.HasConflict(view.RulesVersion) && focus is { } at && view.Conflict is { } conflict
            && (conflict.Encounters.Any(e => e.Star == at) || conflict.Notices.Any(n => n.Star == at));
        _manageSelected.Disabled = colony is null;
        _inspectSystem.Disabled = star is null;
        if (star is null)
        {
            _selectionTitle.Text = "NO SYSTEM SELECTED";
            _selectionSummary.Text = "Click a star for its system, or a fleet flag for its fleets.";
            return;
        }
        _selectionTitle.Text = star.Chart.Name.ToUpperInvariant();
        _selectionSummary.Text = star.Planets is null ? "Not surveyed" : star.Current ? "Current observation" : "Last seen turn " + star.LastObservedTurn;
        if (star.Planets is { } planets) ShowSystemPlanets(view, star, planets, colony);
        ShowSystemFleets(view, star.Chart.Id);
    }

    private void ShowSystemPlanets(EmpireView view, StarView star, ImmutableArray<KnownPlanetView> planets, OwnedColonyView? colony)
    {
        var content = _session.Content;
        _systemCard.AddChild(new Label { Text = "ORBITS · " + Counted(planets.Length, "PLANET", "PLANETS") + " · " + Counted(planets.Count(p => p.OwnerId is not null), "COLONY", "COLONIES"), ThemeTypeVariation = "StencilLabel" });
        var grid = new HFlowContainer { Name = "SystemPlanets" };
        _systemCard.AddChild(grid);
        foreach (var planet in planets.OrderBy(p => p.Orbit))
        {
            var key = new Button
            {
                Name = "SystemPlanet_" + planet.Id.Value, Text = planet.Orbit.ToString("D2", CultureInfo.InvariantCulture),
                ToggleMode = true, ThemeTypeVariation = "CompactCommandKey", ClipText = false, ExpandIcon = true,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TooltipText = content.PlanetTypes[planet.PlanetTypeId].Name + " · " + PlanetOwner(view, planet)
            };
            if (planet.Appearance is { } appearance && _contextPlanetStyles is { } styles)
                key.Icon = _portraitCache.Get(appearance, styles.Resolve(planet.PlanetTypeId, planet.EnvironmentId, appearance.Variant), new Vector2I(96, 96));
            key.SetPressedNoSignal(planet.Id == _selectedPlanet);
            key.Pressed += () => { _selectedPlanet = planet.Id; _requireColonyChoice = false; Callable.From(Refresh).CallDeferred(); };
            grid.AddChild(key);
            // Expanded icons contribute no minimum width; reserve the themed icon size alongside the orbit number.
            var style = key.GetThemeStylebox("normal");
            float width = key.GetThemeFont("font").GetStringSize(key.Text, fontSize: key.GetThemeFontSize("font_size")).X
                + style.GetContentMargin(Side.Left) + style.GetContentMargin(Side.Right);
            if (key.Icon is not null) width += key.GetThemeConstant("icon_max_width") + key.GetThemeConstant("h_separation");
            key.CustomMinimumSize = new Vector2(width, 0);
        }
        if (planets.FirstOrDefault(p => p.Id == _selectedPlanet) is not { } chosen) return;
        var card = new PanelContainer { Name = "PlanetCard", ThemeTypeVariation = "ScreenInset" };
        _systemCard.AddChild(card);
        var rows = new VBoxContainer();
        card.AddChild(rows);
        string type = content.PlanetTypes[chosen.PlanetTypeId].Name;
        OwnedColonyView? own = colony is not null && colony.PlanetId == chosen.Id ? colony : null;
        string heading = own is not null
            ? view.Research.ColonyNames.GetValueOrDefault(own.Id.ToString(), "Colony " + own.Id.Value) + " · colony"
            : star.Chart.Name + " " + chosen.Orbit.ToString("D2", CultureInfo.InvariantCulture) + " · " + type;
        rows.AddChild(new Label { Text = heading.ToUpperInvariant(), ThemeTypeVariation = "ConsoleHeading", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        rows.AddChild(ConsoleText((own is not null ? type + " · " : "") + PlanetOwner(view, chosen) + " · " + (chosen.Colonizable ? "Habitable" : "Hostile habitat")));
        if (own is null) return;
        var economy = own.Economy;
        string project = own.ProjectId is null ? "Saving production"
            : own.ProjectKind == "building" && content.Buildings.TryGetValue(own.ProjectId, out var building) ? building.Name
            : ShipDesignUi.Name(content, view, new ProductionTarget(own.ProjectKind, own.ProjectId));
        rows.AddChild(ConsoleText($"{Format.Points(economy.NetProduction.Total)} production / turn"));
        rows.AddChild(ConsoleText($"{Format.Points(economy.NetResearch.Total)} research / turn"));
        rows.AddChild(ConsoleText($"{own.Population} {own.PopulationUnitLabel}"));
        rows.AddChild(ConsoleText(project + "  ·  " + BreakdownText.Progress(economy.Production) + "  ·  " + BreakdownText.Estimate(economy.Production.Estimate)));
        if (economy.Production.TargetCost is > 0)
            rows.AddChild(new ProgressBar { Value = Math.Clamp(100.0 * own.ProductionReserve / economy.Production.TargetCost.Value, 0, 100), ShowPercentage = false });
    }

    private void ShowSystemFleets(EmpireView view, StarId here)
    {
        if (!Rules.HasExpansion(view.RulesVersion)) return;
        var content = _session.Content;
        _systemFleets.AddChild(new Label { Text = "FLEETS IN THIS SYSTEM", ThemeTypeVariation = "StencilLabel" });
        var ships = view.Vessels.Where(v => v.StationaryStarId == here).OrderBy(v => v.Id).ToArray();
        var grouped = new HashSet<VesselId>();
        if (view.Fleets is { } fleets)
            foreach (var fleet in fleets.Fleets.Where(f => f.Members.Any(id => ships.Any(v => v.Id == id))).OrderBy(f => f.Id))
            {
                grouped.UnionWith(fleet.Members);
                var members = ships.Where(v => fleet.Members.Contains(v.Id)).ToArray();
                AddSystemFleetRow("SystemFleetRow_" + fleet.Id.Value, FleetNaming.Fleet(fleet), FleetNaming.Status(view, members, fleet),
                    (members.FirstOrDefault(v => v.Id == fleet.Flagship) ?? members[0]).RoleId, here, fleet.Members);
            }
        // Independent ships of one kind and status share a row, so a crowded system stays readable.
        foreach (var stack in ships.Where(v => !grouped.Contains(v.Id)).GroupBy(v => FleetRoster.Key(view, v)))
        {
            var members = stack.ToArray();
            AddSystemFleetRow("SystemShipRow_" + members[0].Id.Value, FleetRoster.Title(content, view, members), FleetRoster.Status(view, members), members[0].RoleId, here, members.Select(v => v.Id).ToImmutableArray());
        }
        var foreign = view.ForeignVessels.Where(v => v.StarId == here).ToArray();
        foreach (var seen in foreign.GroupBy(v => (v.EmpireId, v.RoleId)))
            _systemFleets.AddChild(ConsoleText("Observed " + (view.Contacts.FirstOrDefault(c => c.OtherEmpireId == seen.Key.EmpireId)?.CultureName ?? "unknown empire") + ": " + content.Vessels[seen.Key.RoleId].Name + (seen.Count() > 1 ? " ×" + seen.Count() : "")));
        if (ships.Length == 0 && foreign.Length == 0) _systemFleets.AddChild(ConsoleText("No fleets here."));
    }

    private void AddSystemFleetRow(string name, string title, string status, string roleId, StarId star, ImmutableArray<VesselId> members)
    {
        var row = new MapFleetRow { Name = name, SystemId = star.Value, ThemeTypeVariation = "CommandKey", ClipText = true, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.Describe(title, status, RoleIcon(roleId), "Select to open its fleet card.");
        row.Pressed += () => OpenFleetCard(star, members);
        _systemFleets.AddChild(row);
    }

    /// <summary>Replaces the system card with the fleet card for these ships. Selection only; no order is issued.</summary>
    private void OpenFleetCard(StarId star, ImmutableArray<VesselId> members)
    {
        _hoverDestination = null;
        _fleetListStar = star;
        _map.ClearSystemSelection();
        SelectMapShips(members, false);
    }

    private string ShipDetail(EmpireView view, OwnedVesselView ship)
    {
        string text = FleetNaming.Ship(_session.Content, view, ship) + " · " + FleetNaming.Status(view, new[] { ship }, null);
        if (ship.CarriedPopulation > 0) text += $" · {ship.CarriedPopulation} {view.PopulationUnitLabel} aboard";
        if (view.Combat?.Ships.FirstOrDefault(v => v.Id == ship.Id) is { } combat) text += $" · hull {combat.Hull}/{combat.MaxHull}";
        return text;
    }

    /// <summary>The lower half of the fleet card: what is selected, and the two ways out of the card.</summary>
    private void AddFleetCardDetails(EmpireView view, StarId star, string starName, OwnedVesselView[] ships)
    {
        var chosen = ships.Where(v => _mapFleetSelection.Contains(v.Id)).ToArray();
        if (chosen.Length > 0)
        {
            var card = new PanelContainer { Name = "FleetCardDetails", ThemeTypeVariation = "ScreenInset" };
            _mapFleetList.AddChild(card);
            var rows = new VBoxContainer();
            card.AddChild(rows);
            rows.AddChild(new Label { Text = chosen.Length == 1 ? "1 SHIP SELECTED" : chosen.Length + " SHIPS SELECTED", ThemeTypeVariation = "StencilLabel" });
            if (chosen.Length <= 8)
                foreach (var ship in chosen) rows.AddChild(ConsoleText(ShipDetail(view, ship)));
            else
                foreach (var stack in chosen.GroupBy(v => FleetRoster.Key(view, v)))
                    rows.AddChild(ConsoleText(FleetRoster.Title(_session.Content, view, stack.ToArray()) + " · " + FleetRoster.Status(view, stack.ToArray())));
            if (view.Fleets is { } fleets)
                foreach (var fleet in fleets.Fleets.Where(f => f.Attention is not null && f.Members.Any(id => chosen.Any(v => v.Id == id))).OrderBy(f => f.Id))
                    rows.AddChild(ConsoleText(fleet.Name + ": " + fleet.Attention, "WarningLabel"));
        }
        else if (ships.Length > 0)
            _mapFleetList.AddChild(ConsoleText("Select a fleet or ship above for its details, then click a system to set course."));
        var actions = new HFlowContainer { Name = "FleetCardActions" };
        _mapFleetList.AddChild(actions);
        var orders = new Button { Name = "OpenVesselWorkspace", Text = "Ship orders", ThemeTypeVariation = "CompactCommandKey", ClipText = false, SizeFlagsHorizontal = SizeFlags.ExpandFill, Disabled = ships.Length == 0, TooltipText = "Review one ship's route, founding and standing orders. Opening this does not submit an order." };
        orders.Pressed += () => { if ((chosen.FirstOrDefault() ?? ships.FirstOrDefault()) is { } target) OpenShipOrders(target.Id); };
        actions.AddChild(orders);
        var system = new Button { Name = "FleetCardShowSystem", Text = "System card", ThemeTypeVariation = "CompactCommandKey", ClipText = false, SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = "Show the " + starName + " system instead of its fleets." };
        system.Pressed += () => { ClearMapFleetSelection(); _fleetListStar = null; _map.Select(star); };
        actions.AddChild(system);
    }

    private void OpenShipOrders(VesselId id)
    {
        if (View.Vessels.FirstOrDefault(v => v.Id == id) is not { } ship) { ShowMessage("That vessel is no longer available. Choose an owned vessel.", true); return; }
        ClearMapFleetSelection();
        _fleetListStar = null;
        _selectedVessel = id;
        _requireVesselChoice = false;
        _map.Select(ship.StationaryStarId ?? ship.Transit!.Destination);
        _courseDestination = null;
        ShowWorkspace("Vessels");
        Callable.From(Refresh).CallDeferred();
    }
}
