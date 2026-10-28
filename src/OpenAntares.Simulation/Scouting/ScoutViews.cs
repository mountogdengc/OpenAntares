using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Scouting;
public sealed record KnownAmebaView(int HabitatId,StarId Star,int ObservedTurn);
public sealed record ScoutRouteView(StarId Destination,string Name,int Duration,int? LastObservedTurn,bool SurvivalOverridesNeutralCaution);
public sealed record ScoutVesselView(VesselId Id,string RoleId,int Cargo,ScoutPosture? Posture,StarId? Station,TransitState? Transit,StarId? PendingMove,StarId? EffectiveDestination,AmebaRiskAction? RiskAction,AmebaStage? Stage,long? EncounterNumber,long? Revision,int Speed,int PursuitSpeed,int LateEscapePercent,int StudyPercent,int CombinedRetentionNumerator,long ResearchReward,int? DepartureDeadline,bool Held,string? Blocker,bool Acknowledged,ImmutableArray<Command> Actions,ScoutRouteView? DepartureRoute=null)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Fleets.FleetDepartureConsequence? FleetConsequence {get;init;}
}
public sealed record ScoutView(EmpireId Owner,int Turn,ImmutableArray<ScoutVesselView> Vessels,ImmutableArray<KnownAmebaView> Habitats,ImmutableArray<AmebaAttentionToken> Attention,ImmutableArray<ScoutTurnDigest> History);
public sealed record ScoutPreparationPreview(ImmutableArray<Command> Commands,ImmutableArray<AmebaAttentionToken> Attention,ImmutableArray<ScoutOrderEvent> Outcomes,ImmutableArray<string> Blockers);
