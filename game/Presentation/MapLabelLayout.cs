using System.Collections.Generic;
using System.Linq;
using Godot;
using OpenAntares.Simulation;

namespace OpenAntares.Game.Presentation;

public sealed record MapLabelCandidate(StarId Id, Rect2 Bounds, bool Selected, bool Home);

/// <summary>Places already measured requester-visible labels without changing chart or campaign data.</summary>
public static class MapLabelLayout
{
    public static IReadOnlyList<StarId> Place(IReadOnlyList<MapLabelCandidate> candidates, Rect2 viewport)
    {
        var selected = new List<StarId>();
        var occupied = new List<Rect2>();
        foreach (var candidate in candidates.OrderByDescending(c => c.Selected).ThenByDescending(c => c.Home).ThenBy(c => c.Id))
        {
            if (candidate.Bounds.Size.X <= 0 || candidate.Bounds.Size.Y <= 0 || !viewport.Encloses(candidate.Bounds)) continue;
            if (occupied.Any(rect => rect.Intersects(candidate.Bounds))) continue;
            selected.Add(candidate.Id);
            occupied.Add(candidate.Bounds);
        }
        return selected;
    }
}
