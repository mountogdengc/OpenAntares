using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;

/// <summary>
/// The system inspector: a window over the galaxy map showing the star, one ring per orbit and each known planet.
/// Clicking a planet selects it; the strip below explains the selected world.
/// </summary>
public partial class StarPanel : PanelContainer
{
    public event Action<PlanetId>? PlanetSelected;
    public event Action<ColonyId>? ManageRequested;
    public event Action? CloseRequested;
    private PlanetVisualBuilder? _globes;
    private readonly Label _title = new() { Name = "SystemTitle", ThemeTypeVariation = "HeaderLabel", SizeFlagsHorizontal = SizeFlags.ExpandFill, ClipText = true };
    private readonly Label _status = new() { Name = "SystemStatus", ThemeTypeVariation = "SubtleLabel" };
    private readonly SystemOrbitView _orbits = new();
    private readonly VBoxContainer _details = new() { Name = "SystemOverview" };

    public StarPanel()
    {
        Name = "SystemView";
        ThemeTypeVariation = "SystemView";
        MouseFilter = MouseFilterEnum.Stop;
        var rows = new VBoxContainer();
        AddChild(rows);
        var header = new HBoxContainer();
        rows.AddChild(header);
        header.AddChild(_title);
        header.AddChild(_status);
        var close = new Button { Name = "CloseSystemView", Text = "Close", TooltipText = "Close the system view (Esc or right-click)." };
        close.Pressed += () => CloseRequested?.Invoke();
        header.AddChild(close);
        rows.AddChild(_orbits);
        var strip = new PanelContainer { ThemeTypeVariation = "ScreenInset" };
        rows.AddChild(strip);
        var scroll = new ScrollContainer { Name = "SystemDetailScroll", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        strip.AddChild(scroll);
        scroll.AddChild(_details);
        _details.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        Resized += FitDetails;
    }

    public void ConfigureGlobes(PlanetVisualBuilder globes) => _globes = globes;

    public override void _GuiInput(InputEvent input)
    {
        if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }) { CloseRequested?.Invoke(); AcceptEvent(); }
    }

    private void FitDetails()
    {
        // The diagram gets most of the space; the detail strip takes about a quarter and scrolls past that.
        if (_details.GetParent() is ScrollContainer scroll)
            scroll.CustomMinimumSize = new Vector2(0, Math.Max(GetThemeConstant("detail_min_height", "SystemViewMetrics"), Size.Y * 0.26f));
    }

    public void Clear()
    {
        _title.Text = string.Empty; _status.Text = string.Empty;
        _orbits.Show(1, 0, Array.Empty<SystemOrbitView.Body>());
        foreach (Node child in _details.GetChildren()) { _details.RemoveChild(child); child.QueueFree(); }
    }

    public void ShowStar(ContentSet content, StarView view, EmpireView? empire = null, PlanetId? selected = null)
    {
        foreach (Node child in _details.GetChildren()) { _details.RemoveChild(child); child.QueueFree(); }
        _title.Text = view.Chart.Name;
        void AddDetail(Node detail) => _details.AddChild(detail);
        var bodies = new List<SystemOrbitView.Body>();
        if (view.Planets is not { } planets)
        {
            _status.Text = "Not surveyed";
            _orbits.Show(1, view.Chart.Id.Value, bodies);
            AddDetail(Wrapped("No planets are known here. Send a ship to survey this system."));
            AddNotes(content, view, empire, AddDetail);
            return;
        }
        _status.Text = (view.Current ? "Current observation" : $"Last seen turn {view.LastObservedTurn}") + " · " + (planets.Length == 1 ? "1 planet" : $"{planets.Length} planets");
        var chosen = planets.FirstOrDefault(p => p.Id == selected);
        foreach (var planet in planets.OrderBy(p => p.Orbit))
        {
            var type = content.PlanetTypes[planet.PlanetTypeId];
            string owner = OwnerName(empire, planet);
            var key = new Button
            {
                Name = "OrbitPlanet_" + planet.Orbit.ToString(CultureInfo.InvariantCulture),
                ThemeTypeVariation = "OrbitPlanetKey", ToggleMode = true, FocusMode = FocusModeEnum.All,
                TooltipText = $"Orbit {planet.Orbit}: {type.Name}\n{owner}\nClick to select this world.",
            };
            key.SetPressedNoSignal(planet == chosen);
            var globe = new PlanetGlobe2D { Name = "PlanetGlobe" };
            key.AddChild(globe);
            globe.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            if (_globes is not null)
                globe.SetVisual(_globes.Build(view, planet, empire?.Colonies.FirstOrDefault(c => c.PlanetId == planet.Id)));
            var id = planet.Id;
            key.Pressed += () => PlanetSelected?.Invoke(id);
            var caption = new Label { Text = planet.Orbit.ToString("D2", CultureInfo.InvariantCulture), ThemeTypeVariation = "SystemOrbitLabel", MouseFilter = MouseFilterEnum.Ignore };
            var mark = planet.OwnerId is null ? SystemOrbitView.OwnerMark.None
                : planet.OwnerId == empire?.Viewer ? SystemOrbitView.OwnerMark.Own : SystemOrbitView.OwnerMark.Other;
            bodies.Add(new(planet.Orbit, planet.Id.Value, key, caption, mark, planet == chosen));
        }
        _orbits.Show(planets.Length == 0 ? 1 : planets.Max(p => p.Orbit), view.Chart.Id.Value, bodies);
        if (chosen is null) AddDetail(Wrapped(planets.Length == 0 ? "No planets orbit this star." : "Click a planet to see its details."));
        else ShowPlanet(content, view, empire, chosen, AddDetail);
        foreach (var other in planets.Where(p => p != chosen && p.OwnerId is { } o && o != empire?.Viewer).OrderBy(p => p.Orbit))
            AddDetail(Wrapped($"Orbit {other.Orbit} colony observed: {OwnerName(empire, other)} (turn {view.LastObservedTurn})"));
        AddNotes(content, view, empire, AddDetail);
    }

    private void ShowPlanet(ContentSet content, StarView view, EmpireView? empire, KnownPlanetView planet, Action<Node> add)
    {
        var type = content.PlanetTypes[planet.PlanetTypeId];
        add(new Label { Text = $"Orbit {planet.Orbit}: {type.Name}", ThemeTypeVariation = "ConsoleHeading" });
        if (planet.OwnerId is { } owner)
            add(Wrapped(owner == empire?.Viewer ? "Your colony." : $"Colony observed: {OwnerName(empire, planet)} (turn {view.LastObservedTurn})"));
        else add(Wrapped(planet.Colonizable ? "Unclaimed · habitable" : "Unclaimed · hostile habitat"));
        add(Wrapped($"Per worker: {Format.Points(planet.SupportPerWorker)} support, {Format.Points(planet.ProductionPerWorker)} production, {Format.Points(planet.ResearchPerWorker)} research"));
        if (planet.EnvironmentId is { } environment && content.Environments.TryGetValue(environment, out var habitat)) add(Wrapped($"Environment: {habitat.Name} — {habitat.Description}"));
        if (planet.Potential is { } potential)
        {
            add(Wrapped($"For your species, per worker: {Format.Points(potential.SupportPerWorker.Total)} support, {Format.Points(potential.ProductionPerWorker.Total)} production, {Format.Points(potential.ResearchPerWorker.Total)} research."));
            add(Wrapped(potential.NativeHabitatAllowed ? $"Native colony: base capacity {potential.NativeCapacity}." : "Native colony: unsuitable here."));
            if (potential.SealedCapacity is { } capacity) add(Wrapped(potential.SealedHabitatAllowed ? $"Sealed harbor: base capacity {capacity}." : "Sealed harbor: unavailable here."));
        }
        if (planet.OwnerId is { } mine && mine == empire?.Viewer && planet.ColonyId is { } colony)
        {
            var manage = new Button { Name = "ManageOrbitColony", Text = "Manage colony", TooltipText = "Open colony management for this world." };
            manage.Pressed += () => ManageRequested?.Invoke(colony);
            add(manage);
        }
    }

    private static void AddNotes(ContentSet content, StarView view, EmpireView? empire, Action<Node> add)
    {
        if (empire?.Scouting is { } scouting) add(Wrapped(scouting.Habitats.FirstOrDefault(h => h.Star == view.Chart.Id) is { } habitat ? $"Known stationary space ameba; observed turn {habitat.ObservedTurn}. Founding and automatic fallback exclude this habitat." : "Ameba status unknown. No additional sensor coverage."));
        if (empire?.Combat?.Pockets.FirstOrDefault(p => p.Star == view.Chart.Id) is { } pocket) add(Wrapped($"Automated sentries: {(pocket.Current ? "current observation" : "last observed turn " + pocket.ObservedTurn)}. {(pocket.Cleared ? "Cleared; access is open, but no ownership is granted." : "Founding blocked; ordinary passage and survey stay safe.")} Observed hulls: {string.Join(", ", pocket.Sentries.Select(s => $"{s.Hull}/{s.Profile.MaxHull}"))}."));
        if (view.Chart.RegionId is { } regionId && content.RegionDefinitions.TryGetValue(regionId, out var region)) add(Wrapped($"{region.Name} — {region.Description}"));
        if (empire is not null)
        {
            int ships = empire.Vessels.Count(v => v.StationaryStarId == view.Chart.Id);
            if (ships > 0) add(Wrapped(ships == 1 ? "1 of your ships is in this system." : $"{ships} of your ships are in this system."));
        }
    }

    private static string OwnerName(EmpireView? empire, KnownPlanetView planet) =>
        planet.OwnerId is not { } owner ? "Unclaimed"
        : owner == empire?.Viewer ? "Your colony"
        : empire?.Contacts.FirstOrDefault(c => c.OtherEmpireId == owner)?.CultureName ?? owner.ToString();

    private static Label Wrapped(string text) => new() { Text = text, ThemeTypeVariation = "SubtleLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart };
}
