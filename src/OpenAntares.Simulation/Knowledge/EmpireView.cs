using System.Collections.Immutable;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Knowledge;

public sealed record ChartedStar(StarId Id, string Name, int X, int Y, string? RegionId);
public sealed record GalaxyMapView(ImmutableArray<ChartedStar> Stars, ulong? Seed, string? ShapeId, int? Width, int? Height, ImmutableArray<StarId> HomeMarkers)
{
    public ImmutableArray<MapVesselMarker> Units { get; init; } = ImmutableArray<MapVesselMarker>.Empty;
}
public sealed record MapVesselMarker(VesselId Id, string RoleId, bool Owned, StarId? Station, StarId? Origin, StarId? Destination, int RemainingTurns);
public sealed record KnownPlanetView(PlanetId Id, int Orbit, string PlanetTypeId, long SupportPerWorker, long ProductionPerWorker, long ResearchPerWorker, bool Colonizable, ColonyId? ColonyId, EmpireId? OwnerId);
public sealed record StarView(ChartedStar Chart, ImmutableArray<KnownPlanetView>? Planets, int? LastObservedTurn, bool Current);
public sealed record OwnedColonyView(ColonyId Id, PlanetId PlanetId, StarId StarId, EmpireId EmpireId, int Population, Workforce Workforce, int GrowthProgress, long ProductionReserve, string? ProjectId, string ProjectKind, ImmutableArray<string> CompletedBuildingIds, ColonyEconomy Economy)
{
    public int Turn { get; init; }
    public int RulesVersion { get; init; }
    public ImmutableArray<string> KnownTechnologyIds { get; init; } = ImmutableArray<string>.Empty;
}
public sealed record OwnedVesselView(VesselId Id, EmpireId EmpireId, string RoleId, StarId? StationaryStarId, TransitState? Transit, StarId? PendingMove, PlanetId? PendingFounding, int CarriedPopulation);
public sealed record OwnResearchView(EmpireId Id, long Reserve, string? TargetId, ImmutableArray<string> KnownTechnologyIds, EmpireEconomy Economy)
{
    public int Turn { get; init; }
    public bool Ready { get; init; }
    public ImmutableDictionary<string, string> ColonyNames { get; init; } = ImmutableDictionary<string, string>.Empty;
}
public sealed record ForeignVesselView(VesselId Id, EmpireId EmpireId, string RoleId, StarId StarId);
public sealed record EmpireView(int Turn, EmpireId Viewer, int RulesVersion, GalaxyMapView Galaxy, ImmutableArray<StarView> Stars, ImmutableArray<OwnedColonyView> Colonies, ImmutableArray<OwnedVesselView> Vessels, ImmutableArray<ForeignVesselView> ForeignVessels, OwnResearchView Research);

public sealed record ColonyReportView(ColonyEconomy Economy, ColonyId Id, StarId Star, string Name);
public sealed record EmpireTurnReportView(int FromTurn, int ToTurn, ImmutableArray<ColonyReportView> Colonies, EmpireEconomy Research, ImmutableArray<OpenAntares.Simulation.Turns.ExpansionEvent> Events, ImmutableDictionary<StarId, string> StarNames);
