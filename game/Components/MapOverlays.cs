using System.Collections.Generic;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Game.Presentation;
using OpenAntares.Simulation.Fleets;

namespace OpenAntares.Game.Components;

public partial class GalaxyMap
{
    public string Overlay { get; set; } = "Owners";
    private readonly Dictionary<StarId, EmpireId> _owners = new();
    private readonly HashSet<StarId> _threats = new(), _habitats = new();
    private VesselId? _selectedShip;
    private (StarId Origin, StarId Destination, int Turns)? _previewRoute;
    private int _maximumRouteTurns;
    private bool _routeIsPreview;
    private bool _routeBlocked;
    public void ShowFleetPresentation(EmpireView view, IReadOnlyCollection<VesselId> selected, StarId? destination)
    {
        var ships = view.Vessels.Where(v => selected.Contains(v.Id)).OrderBy(v => v.Id).ToArray();
        _routeBlocked=false;
        ShowPresentation(view, ships.FirstOrDefault()?.Id, destination);
        _maximumRouteTurns = _previewRoute?.Turns ?? 0;
        _routeIsPreview = ships.Any(v => v.Transit is null && v.PendingMove != destination);
        if (ships.Length == 0 || destination is not { } target) return;
        if(view.Fleets is null)
        {
            _routeBlocked=ships.Any(s=>s.Transit is not null)||view.Scouting?.Habitats.Any(h=>h.Star==target)==true;
            return;
        }
        var preview = FleetQueries.PreviewAction(_presentationContent!, view, MapFleetOrders.Selection(view, selected, target));
        if (preview.Allowed && preview.Routes.Length > 0)
        {
            _previewRoute = (preview.Routes[0].Origin, target, preview.Routes.Min(r => r.Turns));
            _maximumRouteTurns = preview.Routes.Max(r => r.Turns);
        }
        else
        {
            _routeBlocked=true;TooltipText=preview.Rejection??"This course is unavailable.";
            var origin=ships[0].StationaryStarId??ships[0].Transit?.Origin;
            if(origin is {} from&&from!=target&&(_previewRoute is null||ships[0].Transit is not null))_previewRoute=(from,target,0);
        }
    }
    public void ShowPresentation(EmpireView view, VesselId? selectedShip, StarId? destination)
    {
        _selectedShip = selectedShip; _previewRoute = null;
        _owners.Clear(); _threats.Clear(); _habitats.Clear();
        foreach (var star in view.Stars)
        {
            if (star.Planets is not { } planets) continue;
            var owner = planets.Where(p => p.OwnerId is not null).OrderBy(p => p.Orbit).FirstOrDefault()?.OwnerId;
            if (owner is { } empire) _owners[star.Chart.Id] = empire;
            if (planets.Any(p => p.Potential is { } potential ? potential.NativeHabitatAllowed || potential.SealedHabitatAllowed : p.Colonizable)) _habitats.Add(star.Chart.Id);
        }
        foreach (var foreign in view.ForeignVessels) _threats.Add(foreign.StarId);
        if (view.Combat is { } combat) foreach (var pocket in combat.Pockets.Where(p => !p.Cleared)) _threats.Add(pocket.Star);
        if (view.Scouting is { } scout) foreach (var habitat in scout.Habitats) _threats.Add(habitat.Star);
        if (view.Vessels.FirstOrDefault(v => v.Id == selectedShip) is { } ship)
        {
            if (ship.Transit is { } transit) _previewRoute = (transit.Origin, transit.Destination, transit.RemainingTurns);
            else if (ship.StationaryStarId is { } originId && (destination ?? ship.PendingMove) is { } destinationId && originId != destinationId)
            {
                var origin = view.Stars.First(s => s.Chart.Id == originId).Chart;
                var target = view.Stars.First(s => s.Chart.Id == destinationId).Chart;
                // The effective fleet speed is the exact same published fact used by the order workspace.
                int speed = view.Combat?.Ships.FirstOrDefault(s => s.Id == ship.Id)?.EffectiveSpeed ?? _presentationContent!.Vessels[ship.RoleId].Speed;
                if (view.Fleets?.Ships.FirstOrDefault(s => s.Id == ship.Id)?.Fleet is { } fleet && view.Combat is { } fleetCombat)
                    speed = view.Fleets.Fleets.Single(f => f.Id == fleet).Members.Min(id => fleetCombat.Ships.Single(s => s.Id == id).EffectiveSpeed);
                var preview = TravelCalculator.Preview(originId, destinationId, origin.X, origin.Y, target.X, target.Y, speed);
                _previewRoute = (originId, destinationId, preview.Turns);
            }
        }
        QueueRedraw();
    }
    private OpenAntares.Simulation.Content.ContentSet? _presentationContent;
    public void ConfigurePresentation(OpenAntares.Simulation.Content.ContentSet content) => _presentationContent = content;
    private void DrawMapDressing(MapTransform transform, Font font, int fontSize)
    {
        foreach (var star in _stars)
        {
            Vector2 at = ToScreen(star.X, star.Y, transform);
            DrawCircle(at, GetThemeConstant("glow_radius", ThemeType), GetThemeColor("star_glow", ThemeType));
            if (Overlay == "Owners" && _owners.TryGetValue(star.Id, out var owner))
            {
                Color color = GetThemeColor("owner_" + (owner.Value % 4), ThemeType);
                DrawArc(at, GetThemeConstant("ownership_radius", ThemeType), 0, Mathf.Tau, 32, color, 1, true);
            }
            if (Overlay == "Threat" && _threats.Contains(star.Id)) DrawArc(at, GetThemeConstant("ownership_radius", ThemeType), 0, Mathf.Tau, 32, GetThemeColor("threat", ThemeType), 2, true);
            if (Overlay == "Habitat" && _habitats.Contains(star.Id)) DrawArc(at, GetThemeConstant("ownership_radius", ThemeType), 0, Mathf.Tau, 32, GetThemeColor("habitat", ThemeType), 2, true);
        }
        if (_previewRoute is not { } route) return;
        var origin = _stars.FirstOrDefault(s => s.Id == route.Origin); var target = _stars.FirstOrDefault(s => s.Id == route.Destination);
        if (origin is null || target is null) return;
        Vector2 start = ToScreen(origin.X, origin.Y, transform), end = ToScreen(target.X, target.Y, transform);
        var routeColor=GetThemeColor(_routeBlocked?"route_rejected":"route",ThemeType);
        DrawDashedLine(start, end, routeColor, 2, 7, true, true);
        string eta = _routeBlocked?"Unavailable":(_routeIsPreview ? "Preview · ETA " : "ETA ") + (route.Turns == _maximumRouteTurns ? route.Turns.ToString() : route.Turns + "–" + _maximumRouteTurns) + (route.Turns == 1 && _maximumRouteTurns == 1 ? " turn" : " turns");
        Vector2 width = font.GetStringSize(eta, HorizontalAlignment.Left, -1, fontSize);
        var position = (start + end) / 2;
        position.X = Mathf.Clamp(position.X, 0, System.Math.Max(0, Size.X - width.X));
        position.Y = Mathf.Clamp(position.Y, font.GetAscent(fontSize), Size.Y);
        DrawString(font, position, eta, HorizontalAlignment.Left, -1, fontSize, routeColor);
    }
}
