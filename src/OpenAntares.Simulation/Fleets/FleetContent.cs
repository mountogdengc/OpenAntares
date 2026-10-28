using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace OpenAntares.Simulation.Fleets;

public enum OfficerSpecialty { Navigator, Surveyor, Organizer }
public enum OfficerDoctrine { RecoverCommand, LastWatch }
public enum CommandEmbodiment { Mortal, ColonyBody }

public sealed record OfficerProfileDefinition(string Id, string Name, string Pronouns, string CultureId, [property: JsonRequired] OfficerSpecialty Specialty, string Personality);
public sealed record CommandCultureDefinition(string Id, string Name, [property: JsonRequired] OfficerDoctrine DefaultDoctrine, ImmutableArray<string> ProfileIds);

public sealed record FleetCommandDefinition
{
    public required string RecoveryTechnologyId { get; init; } = string.Empty;
    public required int MemberLimit { get; init; }
    public required int FleetLimit { get; init; }
    public required int RequestLimit { get; init; }
    public required int ParticipatingColonyLimit { get; init; }
    public required int ActiveOfficerLimit { get; init; }
    public required int LifetimeCommissionLimit { get; init; }
    public required int CommissionTurns { get; init; }
    public required int XpCap { get; init; }
    public required int HighlightLimit { get; init; }
    public required int DetailTurnLimit { get; init; }
    public required int MortalRecoveryPercent { get; init; }
    public required int ResearchedRecoveryPercent { get; init; }
    public required int MortalRecoveryTurns { get; init; }
    public required int LinkRecoveryTurns { get; init; }
    public required int LastWatchLinkTurns { get; init; }
    public required int ResearchedLinkReduction { get; init; }
    public required int FirstSurveyXp { get; init; }
    public required int AssemblyHullXp { get; init; }
    public required int StudyXp { get; init; }
    public required ImmutableArray<int> RankThresholds { get; init; } = ImmutableArray<int>.Empty;
    public required ImmutableArray<int> NavigatorBonuses { get; init; } = ImmutableArray<int>.Empty;
    public required ImmutableArray<int> SurveyResearchBonuses { get; init; } = ImmutableArray<int>.Empty;
    public required ImmutableArray<int> OrganizerDiscounts { get; init; } = ImmutableArray<int>.Empty;
    public required ImmutableArray<CommandCultureDefinition> Cultures { get; init; } = ImmutableArray<CommandCultureDefinition>.Empty;
    public required ImmutableArray<OfficerProfileDefinition> Profiles { get; init; } = ImmutableArray<OfficerProfileDefinition>.Empty;
}
