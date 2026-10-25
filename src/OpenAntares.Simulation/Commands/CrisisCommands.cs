using System.Collections.Immutable;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Commands;

public abstract record CrisisCommand(EmpireId Issuer,int ExpectedTurn):Command(Issuer,ExpectedTurn);
public sealed record SetQuarantineCommand(EmpireId Issuer,int ExpectedTurn,QuarantinePreview Expected):CrisisCommand(Issuer,ExpectedTurn);
public sealed record SetCureShareCommand(EmpireId Issuer,int ExpectedTurn,long Episode,int Share):CrisisCommand(Issuer,ExpectedTurn);
public sealed record SetCrisisAuthorityCommand(EmpireId Issuer,int ExpectedTurn,CrisisAuthority Authority,int Share):CrisisCommand(Issuer,ExpectedTurn);
public sealed record SetCrisisAcknowledgementCommand(EmpireId Issuer,int ExpectedTurn,bool Require):CrisisCommand(Issuer,ExpectedTurn);
public sealed record AcknowledgeCrisisCommand(EmpireId Issuer,int ExpectedTurn,ImmutableArray<CrisisReceipt> Expected):CrisisCommand(Issuer,ExpectedTurn);
public sealed record ResumeCrisisPolicyCommand(EmpireId Issuer,int ExpectedTurn,ImmutableArray<ColonyId> Colonies,bool Research):CrisisCommand(Issuer,ExpectedTurn);
public sealed record DecontaminateVesselCommand(EmpireId Issuer,int ExpectedTurn,VesselId Vessel,ImmutableArray<VesselOrderFact> ExpectedOrders):CrisisCommand(Issuer,ExpectedTurn);
public sealed record PurgeColonyCommand(EmpireId Issuer,int ExpectedTurn,PurgePreview Expected):CrisisCommand(Issuer,ExpectedTurn);
