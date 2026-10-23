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
    public void ShowStar(ContentSet content, StarView view)
    {
        foreach (Node child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        AddChild(new Label { Text = view.Chart.Name, ThemeTypeVariation = "HeaderLabel" });
        if (view.Chart.RegionId is { } regionId && content.RegionDefinitions.TryGetValue(regionId, out var region)) AddChild(Wrapped($"{region.Name} — {region.Description}"));
        if (view.Planets is not { } planets) { AddChild(Wrapped("Not surveyed")); return; }
        AddChild(Wrapped(view.Current ? "Current observation" : $"Last seen turn {view.LastObservedTurn}"));
        AddChild(Wrapped(planets.Length == 1 ? "1 planet" : $"{planets.Length} planets"));
        foreach (var planet in planets.OrderBy(p => p.Orbit))
        {
            AddChild(new HSeparator()); var type = content.PlanetTypes[planet.PlanetTypeId];
            AddChild(new Label { Text = $"Orbit {planet.Orbit}: {type.Name}" });
            AddChild(Wrapped($"Per worker: {Format.Points(planet.SupportPerWorker)} support, {Format.Points(planet.ProductionPerWorker)} production, {Format.Points(planet.ResearchPerWorker)} research"));
            if (planet.OwnerId is { } owner) AddChild(Wrapped($"Colony observed: {owner}"));
            var button = new Button { Text = $"Select planet {planet.Orbit}", TooltipText = "Select this exact world for colony management or founding." }; button.Pressed += () => PlanetSelected?.Invoke(planet.Id); AddChild(button);
        }
    }
    private static Label Wrapped(string text) => new() { Text = text, ThemeTypeVariation = "SubtleLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart };
}
