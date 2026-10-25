using System.Collections.Immutable;
using OpenAntares.Simulation.Scouting;
namespace OpenAntares.Simulation.Commands;

public abstract record ScoutCommand(EmpireId Issuer,int ExpectedTurn):Command(Issuer,ExpectedTurn);
public sealed record SetScoutPostureCommand(EmpireId Issuer,int ExpectedTurn,VesselId Vessel,ScoutPosture Posture):ScoutCommand(Issuer,ExpectedTurn);
public sealed record SetAmebaRiskOrderCommand(EmpireId Issuer,int ExpectedTurn,VesselId Vessel,long EncounterNumber,long ExpectedRevision,AmebaRiskAction? Action):ScoutCommand(Issuer,ExpectedTurn);
public sealed record SetAmebaApproachOrderCommand(EmpireId Issuer,int ExpectedTurn,VesselId Vessel,StarId Destination,int HabitatId):ScoutCommand(Issuer,ExpectedTurn);
public sealed record AcknowledgeAmebaCommand(EmpireId Issuer,int ExpectedTurn,ImmutableArray<AmebaAttentionToken> Tokens):ScoutCommand(Issuer,ExpectedTurn);
public sealed record SetScoutRetreatOrderCommand(EmpireId Issuer,int ExpectedTurn,VesselId Vessel,StarId Destination):ScoutCommand(Issuer,ExpectedTurn);
