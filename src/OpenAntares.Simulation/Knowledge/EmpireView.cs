using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Appearance;
using OpenAntares.Simulation.Development;
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
public sealed record PlanetPotentialView(Quantity SupportPerWorker, Quantity ProductionPerWorker, Quantity ResearchPerWorker,
    int NativeCapacity, int? SealedCapacity, bool NativeHabitatAllowed, bool SealedHabitatAllowed);
public sealed record KnownPlanetView(PlanetId Id, int Orbit, string PlanetTypeId, long SupportPerWorker, long ProductionPerWorker, long ResearchPerWorker, bool Colonizable, ColonyId? ColonyId, EmpireId? OwnerId)
{
    public string? EnvironmentId { get; init; }
    public PlanetAppearanceDescriptor? Appearance { get; init; }
    public PlanetPotentialView? Potential { get; init; }
}
public sealed record StarView(ChartedStar Chart, ImmutableArray<KnownPlanetView>? Planets, int? LastObservedTurn, bool Current);
public sealed record OwnedColonyView(ColonyId Id, PlanetId PlanetId, StarId StarId, EmpireId EmpireId, int Population, Workforce Workforce, int GrowthProgress, long ProductionReserve, string? ProjectId, string ProjectKind, ImmutableArray<string> CompletedBuildingIds, ColonyEconomy Economy)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public long ReservedProduction {get;init;}
    [System.Text.Json.Serialization.JsonIgnore]
    public long SpendableProduction=>checked(ProductionReserve-ReservedProduction);
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ReserveOutcome? ProductionIfSetupReleased {get;init;}
    public CrisisCaseView? Crisis { get; init; }
    public int Turn { get; init; }
    public ColonyDevelopmentView? Development { get; init; }
    public int RulesVersion { get; init; }
    public string? SpeciesId { get; init; }
    public string? HabitatMode { get; init; }
    public string PopulationUnitLabel { get; init; } = "population";
    public ImmutableArray<string> AvailableVesselRoleIds { get; init; } = ImmutableArray<string>.Empty;
    public ImmutableArray<string> KnownTechnologyIds { get; init; } = ImmutableArray<string>.Empty;
}
public sealed record OwnedVesselView(VesselId Id, EmpireId EmpireId, string RoleId, StarId? StationaryStarId, TransitState? Transit, StarId? PendingMove, PlanetId? PendingFounding, int CarriedPopulation)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ShipDesigns.VesselLoadout? Loadout {get;init;}
    public CrisisVesselView? Crisis { get; init; }
    public ImmutableArray<Colonization.FoundingPreview> FoundingPreviews { get; init; } = ImmutableArray<Colonization.FoundingPreview>.Empty;
}
public sealed record OwnResearchView(EmpireId Id, long Reserve, string? TargetId, ImmutableArray<string> KnownTechnologyIds, EmpireEconomy Economy)
{
    public int RulesVersion { get; init; } = Rules.LegacyVersion;
    public int Turn { get; init; }
    public bool Ready { get; init; }
    public ImmutableDictionary<string, string> ColonyNames { get; init; } = ImmutableDictionary<string, string>.Empty;
}
public sealed record ForeignVesselView(VesselId Id, EmpireId EmpireId, string RoleId, StarId StarId);
public sealed record ContactView(EmpireId OtherEmpireId, string SpeciesId, string SpeciesName, string CultureName, int FirstObservedTurn);
public sealed record EmpireView(int Turn, EmpireId Viewer, int RulesVersion, GalaxyMapView Galaxy, ImmutableArray<StarView> Stars, ImmutableArray<OwnedColonyView> Colonies, ImmutableArray<OwnedVesselView> Vessels, ImmutableArray<ForeignVesselView> ForeignVessels, OwnResearchView Research)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public ImmutableArray<StarId> SettlementRestrictedSystems {get;init;}=ImmutableArray<StarId>.Empty;
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Diplomacy.DiplomacyView? Diplomacy {get;init;}
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ShipDesigns.ShipDesignEmpireView? ShipDesigns {get;init;}
    public Combat.CombatEmpireView? Combat {get;init;}
    public Fleets.FleetEmpireView? Fleets { get; init; }
    public Scouting.ScoutView? Scouting { get; init; }
    public CrisisView? Crisis { get; init; }
    public EmpireDevelopmentView? Development { get; init; }
    public ImmutableArray<ContactView> Contacts { get; init; } = ImmutableArray<ContactView>.Empty;
    public string? SpeciesId { get; init; }
    public string PopulationUnitLabel { get; init; } = "population";
    public ImmutableArray<string> AvailableVesselRoleIds { get; init; } = ImmutableArray<string>.Empty;
}

public sealed record ColonyReportView(ColonyEconomy Economy, ColonyId Id, StarId Star, string Name)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ShipDesigns.EquipmentFact? ProductionEquipment {get;init;}
}
public sealed record EmpireTurnReportView(int FromTurn, int ToTurn, ImmutableArray<ColonyReportView> Colonies, EmpireEconomy Research, ImmutableArray<OpenAntares.Simulation.Turns.ExpansionEvent> Events, ImmutableDictionary<StarId, string> StarNames)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableArray<Diplomacy.DiplomacyEvent>? DiplomacyEvents {get;init;}
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableArray<ShipDesigns.ShipDesignEvent>? ShipDesignEvents {get;init;}
    public Combat.CombatEmpireView? Combat {get;init;}
    public ImmutableArray<Combat.CombatDeparture> CombatDepartures {get;init;}=ImmutableArray<Combat.CombatDeparture>.Empty;
    public ImmutableArray<Combat.CombatEvent> CombatEvents {get;init;}=ImmutableArray<Combat.CombatEvent>.Empty;
    public ImmutableArray<Combat.CombatRoundReceipt> CombatRounds {get;init;}=ImmutableArray<Combat.CombatRoundReceipt>.Empty;
    public ImmutableArray<Fleets.FleetEvent> FleetEvents {get;init;}=ImmutableArray<Fleets.FleetEvent>.Empty;
    public ImmutableArray<Scouting.ScoutOrderEvent> ScoutEvents { get; init; }=ImmutableArray<Scouting.ScoutOrderEvent>.Empty;
    public Scouting.ScoutView? Scouting { get; init; }
    public ImmutableArray<CrisisEvent> CrisisEvents { get; init; } = ImmutableArray<CrisisEvent>.Empty;
    public ImmutableArray<OpenAntares.Simulation.Turns.EncounterEvent> EncounterEvents { get; init; } = ImmutableArray<OpenAntares.Simulation.Turns.EncounterEvent>.Empty;
}
