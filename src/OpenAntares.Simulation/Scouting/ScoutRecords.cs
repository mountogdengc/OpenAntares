using System.Collections.Immutable;
namespace OpenAntares.Simulation.Scouting;

public sealed record AmebaHabitat(int Id,string DefinitionId,StarId Star);
public sealed record KnownAmeba(int HabitatId,StarId Star,int ObservedTurn);
public sealed record AmebaStudyClaim(int HabitatId,int Turn,VesselId Vessel);
public sealed record AmebaApproach(StarId Destination,int HabitatId);
public sealed record AmebaRiskOrder(AmebaRiskAction Action,int Turn,long Revision);
public sealed record AmebaAttentionToken(VesselId Vessel,long EncounterNumber,long Revision,AmebaStage Stage,string Action,StarId? Destination,int Speed,int Cargo,int Percent,long Reward)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? FleetConsequenceFingerprint { get; init; }
}
public sealed record AmebaReceipt(AmebaAttentionToken Token,int AcknowledgedTurn);
public sealed record ScoutOrderEvent(EmpireId Owner,int Turn,VesselId Vessel,string RoleId,int Cargo,StarId Star,string Kind,string Reason,StarId? Destination=null,int? Percent=null,long ResearchAward=0,long? EncounterNumber=null,long? Revision=null);
public sealed record ScoutTurnDigest(int Turn,ImmutableArray<ScoutOrderEvent> Entries);
public sealed record RetiredScoutVessel(VesselId Id,EmpireId Owner,string RoleId,int Cargo,StarId Star,int RetiredTurn);
