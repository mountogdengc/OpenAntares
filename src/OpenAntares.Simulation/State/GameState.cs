using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Random;

namespace OpenAntares.Simulation.State;

/// <summary>
/// The authoritative, serializable state of one game. Entities refer to each other by ID only.
/// Entity lists are kept sorted by ID so iteration order is deterministic.
/// </summary>
public sealed class GameState
{
    /// <summary>The rules implementation version that produced this state.</summary>
    public int RulesVersion { get; set; } = Rules.CurrentVersion;

    /// <summary>Turn number. The initial planning period is turn 0.</summary>
    public int Turn { get; set; }

    /// <summary>The next unused entity ID. Shared by all entity kinds.</summary>
    public int NextEntityId { get; set; } = 1;

    public Pcg32 Random { get; set; } = Pcg32.FromSeed(0, 0);

    /// <summary>Absent for galaxies loaded from version-1 saves.</summary>
    public ulong? GalaxySeed { get; set; }
    public string? GalaxyShapeId { get; set; }
    public int? GalaxyWidth { get; set; }
    public int? GalaxyHeight { get; set; }

    public List<EmpireState> Empires { get; set; } = new();
    public List<StarState> Stars { get; set; } = new();
    public List<PlanetState> Planets { get; set; } = new();
    public List<ColonyState> Colonies { get; set; } = new();

    /// <summary>Reserves a fresh entity ID.</summary>
    public int AllocateId() => NextEntityId++;

    public EmpireState? FindEmpire(EmpireId id) => Empires.FirstOrDefault(e => e.Id == id);
    public StarState? FindStar(StarId id) => Stars.FirstOrDefault(s => s.Id == id);
    public PlanetState? FindPlanet(PlanetId id) => Planets.FirstOrDefault(p => p.Id == id);
    public ColonyState? FindColony(ColonyId id) => Colonies.FirstOrDefault(c => c.Id == id);

    public GameState Clone() => new()
    {
        RulesVersion = RulesVersion,
        Turn = Turn,
        NextEntityId = NextEntityId,
        Random = Random.Clone(),
        GalaxySeed = GalaxySeed,
        GalaxyShapeId = GalaxyShapeId,
        GalaxyWidth = GalaxyWidth,
        GalaxyHeight = GalaxyHeight,
        Empires = Empires.Select(e => e.Clone()).ToList(),
        Stars = Stars.Select(s => s.Clone()).ToList(),
        Planets = Planets.Select(p => p.Clone()).ToList(),
        Colonies = Colonies.Select(c => c.Clone()).ToList(),
    };
}

/// <summary>Who issues an empire's commands. AI empires use the same command path as humans.</summary>
public enum ControllerKind
{
    Human,
    Ai,
}

public sealed class EmpireState
{
    public EmpireId Id { get; set; }
    public ControllerKind Controller { get; set; }

    /// <summary>Unspent research, in hundredths.</summary>
    public long ResearchReserve { get; set; }

    public string? ResearchTargetId { get; set; }

    /// <summary>Researched technology IDs, sorted ordinally.</summary>
    public List<string> KnownTechnologyIds { get; set; } = new();

    /// <summary>Whether this empire has finished planning the current turn.</summary>
    public bool Ready { get; set; }

    public EmpireState Clone() => new()
    {
        Id = Id,
        Controller = Controller,
        ResearchReserve = ResearchReserve,
        ResearchTargetId = ResearchTargetId,
        KnownTechnologyIds = new List<string>(KnownTechnologyIds),
        Ready = Ready,
    };
}

/// <summary>A star system on the galaxy map. Coordinates are whole map units.</summary>
public sealed class StarState
{
    public StarId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public string? RegionId { get; set; }

    public StarState Clone() => new() { Id = Id, Name = Name, X = X, Y = Y, RegionId = RegionId };
}

public sealed class PlanetState
{
    public PlanetId Id { get; set; }
    public StarId StarId { get; set; }

    /// <summary>Position in the star system, counting outward from 1.</summary>
    public int Orbit { get; set; }

    /// <summary>Content ID of the planet type.</summary>
    public string PlanetTypeId { get; set; } = string.Empty;

    public PlanetState Clone() => new() { Id = Id, StarId = StarId, Orbit = Orbit, PlanetTypeId = PlanetTypeId };
}

public sealed class ColonyState
{
    public ColonyId Id { get; set; }
    public PlanetId PlanetId { get; set; }
    public EmpireId EmpireId { get; set; }

    /// <summary>Whole workforce units.</summary>
    public int Population { get; set; }

    public Workforce Workforce { get; set; }

    /// <summary>Hundredths of a workforce unit toward the next birth, 0 to 99.</summary>
    public int GrowthProgress { get; set; }

    /// <summary>Unspent production, in hundredths.</summary>
    public long ProductionReserve { get; set; }

    /// <summary>Content ID of the selected building project, if any.</summary>
    public string? ProjectId { get; set; }

    /// <summary>Completed building IDs, sorted ordinally.</summary>
    public List<string> CompletedBuildingIds { get; set; } = new();

    public ColonyState Clone() => new()
    {
        Id = Id,
        PlanetId = PlanetId,
        EmpireId = EmpireId,
        Population = Population,
        Workforce = Workforce,
        GrowthProgress = GrowthProgress,
        ProductionReserve = ProductionReserve,
        ProjectId = ProjectId,
        CompletedBuildingIds = new List<string>(CompletedBuildingIds),
    };
}
