using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class StarPanel : VBoxContainer
{
    public event Action<PlanetId>? PlanetSelected;
    private OpenAntares.Game.Presentation.PlanetStyleCatalog? _styles;
    private OpenAntares.Game.Presentation.PlanetPortraitCache? _cache;
    public void ConfigurePortraits(OpenAntares.Game.Presentation.PlanetStyleCatalog styles,OpenAntares.Game.Presentation.PlanetPortraitCache cache){_styles=styles;_cache=cache;}
    public void ShowStar(ContentSet content, StarView view, EmpireView? empire = null)
    {
        foreach (Node child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        var overview = new VBoxContainer { Name = "SystemOverview" };
        AddChild(overview);
        overview.AddChild(new Label { Text = view.Chart.Name, ThemeTypeVariation = "HeaderLabel" });
        void AddDetail(Node detail) => overview.AddChild(detail);
        if(empire?.Scouting is {} scouting)AddDetail(Wrapped(scouting.Habitats.FirstOrDefault(h=>h.Star==view.Chart.Id) is {} habitat?$"Known stationary space ameba; observed turn {habitat.ObservedTurn}. Founding and automatic fallback exclude this habitat.":"Ameba status unknown. No additional sensor coverage."));
        if(empire?.Combat?.Pockets.FirstOrDefault(p=>p.Star==view.Chart.Id) is {} pocket)AddDetail(Wrapped($"Automated sentries: {(pocket.Current?"current observation":"last observed turn "+pocket.ObservedTurn)}. {(pocket.Cleared?"Cleared; access is open, but no ownership is granted.":"Founding blocked; ordinary passage and survey stay safe.")} Observed hulls: {string.Join(", ",pocket.Sentries.Select(s=>$"{s.Hull}/{s.Profile.MaxHull}"))}."));
        if (view.Chart.RegionId is { } regionId && content.RegionDefinitions.TryGetValue(regionId, out var region)) AddDetail(Wrapped($"{region.Name} — {region.Description}"));
        if (view.Planets is not { } planets) { AddDetail(Wrapped("Not surveyed")); return; }
        AddDetail(Wrapped(view.Current ? "Current observation" : $"Last seen turn {view.LastObservedTurn}"));
        AddDetail(Wrapped(planets.Length == 1 ? "1 planet" : $"{planets.Length} planets"));
        foreach (var planet in planets.OrderBy(p => p.Orbit))
        {
            AddDetail(new HSeparator()); var type = content.PlanetTypes[planet.PlanetTypeId];
            AddDetail(new Label { Text = $"Orbit {planet.Orbit}: {type.Name}" });
            if (planet.Appearance is not null) { var portrait=new PlanetPortrait(); AddDetail(portrait); if(_styles is not null&&_cache is not null)portrait.Configure(_styles,_cache);portrait.ShowPlanet(planet); }
            AddDetail(Wrapped($"Per worker: {Format.Points(planet.SupportPerWorker)} support, {Format.Points(planet.ProductionPerWorker)} production, {Format.Points(planet.ResearchPerWorker)} research"));
            if (planet.EnvironmentId is { } environment && content.Environments.TryGetValue(environment, out var habitat)) AddDetail(Wrapped($"Environment: {habitat.Name} — {habitat.Description}"));
            if (planet.Potential is { } potential)
            {
                AddDetail(Wrapped($"For your species, per worker: {Format.Points(potential.SupportPerWorker.Total)} support, {Format.Points(potential.ProductionPerWorker.Total)} production, {Format.Points(potential.ResearchPerWorker.Total)} research."));
                AddDetail(Wrapped(potential.NativeHabitatAllowed ? $"Native colony: base capacity {potential.NativeCapacity}." : "Native colony: unsuitable here."));
                if (potential.SealedCapacity is { } capacity) AddDetail(Wrapped(potential.SealedHabitatAllowed ? $"Sealed harbor: base capacity {capacity}." : "Sealed harbor: unavailable here."));
            }
            if (planet.OwnerId is { } owner) AddDetail(Wrapped($"Colony observed: {empire?.Contacts.FirstOrDefault(c=>c.OtherEmpireId==owner)?.CultureName ?? owner.ToString()} (turn {view.LastObservedTurn})"));
            var button = new Button { Text = $"Select planet {planet.Orbit}", TooltipText = "Select this exact world for colony management or founding." }; button.Pressed += () => PlanetSelected?.Invoke(planet.Id); AddDetail(button);
        }
    }
    private static Label Wrapped(string text) => new() { Text = text, ThemeTypeVariation = "SubtleLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart };
}
