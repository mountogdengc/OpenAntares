using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Components;

/// <summary>Shows a star system: its planets, their types and per-worker rates, and any colony.</summary>
public partial class StarPanel : VBoxContainer
{
    public void ShowStar(ContentSet content, GameState state, StarId starId, EmpireId viewer)
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        StarState? star = state.FindStar(starId);
        if (star is null)
        {
            return;
        }

        AddChild(new Label { Text = star.Name, ThemeTypeVariation = "HeaderLabel" });

        if (star.RegionId is { } regionId && content.RegionDefinitions.TryGetValue(regionId, out GalaxyRegionDefinition? region))
        {
            AddChild(Wrapped($"{region.Name} — {region.Description}", "SubtleLabel"));
        }

        var planets = state.Planets.Where(p => p.StarId == starId).OrderBy(p => p.Orbit).ToList();
        AddChild(new Label
        {
            Text = planets.Count == 1 ? "1 planet" : $"{planets.Count} planets",
            ThemeTypeVariation = "SubtleLabel",
        });

        foreach (PlanetState planet in planets)
        {
            AddChild(new HSeparator());

            PlanetTypeDefinition type = content.PlanetTypes[planet.PlanetTypeId];
            AddChild(new Label { Text = $"Orbit {planet.Orbit}: {type.Name}" });
            AddChild(Wrapped(
                $"Per worker: {Format.Points(type.SupportPerWorker)} support, "
                + $"{Format.Points(type.ProductionPerWorker)} production, "
                + $"{Format.Points(type.ResearchPerWorker)} research",
                "SubtleLabel"));

            ColonyState? colony = state.Colonies.FirstOrDefault(c => c.PlanetId == planet.Id);
            if (colony is not null)
            {
                string owner = colony.EmpireId == viewer ? "Your colony" : "Colony";
                AddChild(new Label { Text = $"{owner}: population {colony.Population}" });
            }
        }
    }

    private static Label Wrapped(string text, string variation) => new()
    {
        Text = text,
        ThemeTypeVariation = variation,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };
}
