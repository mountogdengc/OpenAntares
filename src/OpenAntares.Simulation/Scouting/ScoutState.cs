using System.Collections.Generic;
using OpenAntares.Simulation.Random;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Scouting;

public enum ScoutPosture { Observe,AvoidEncounters }
public enum AmebaStage { Warning,Exposed }
public enum AmebaRiskAction { Study,Remain }

public sealed class ScoutGameState
{
    public bool Enabled { get; set; }
    public Pcg32? PlacementRandom { get; set; }
    public long SelectionCount { get; set; }
    public AmebaHabitat? Habitat { get; set; }
    public ScoutGameState Clone()=>new(){Enabled=Enabled,PlacementRandom=PlacementRandom?.Clone(),SelectionCount=SelectionCount,Habitat=Habitat};
}
public sealed class EmpireScoutState
{
    public Pcg32? RiskRandom { get; set; }
    public long RiskRollCount { get; set; }
    public long NextEncounterNumber { get; set; }=1;
    public List<KnownAmeba> KnownHabitats { get; set; }=new();
    public List<AmebaStudyClaim> SuccessfulStudies { get; set; }=new();
    public List<AmebaReceipt> Receipts { get; set; }=new();
    public ScoutTurnDigest? PendingDigest { get; set; }
    public List<ScoutTurnDigest> CompletedDigests { get; set; }=new();
    public List<RetiredScoutVessel> RetiredVessels { get; set; }=new();
    public EmpireScoutState Clone()=>new(){RiskRandom=RiskRandom?.Clone(),RiskRollCount=RiskRollCount,NextEncounterNumber=NextEncounterNumber,KnownHabitats=new(KnownHabitats),SuccessfulStudies=new(SuccessfulStudies),Receipts=new(Receipts),PendingDigest=PendingDigest,CompletedDigests=new(CompletedDigests),RetiredVessels=new(RetiredVessels)};
}
public sealed class ScoutVesselState
{
    public ScoutPosture Posture { get; set; }
    public bool WithdrawFromAmeba { get; set; }=true;
    public AmebaApproach? ApproachAcceptance { get; set; }
    public string? LastWaitSignature { get; set; }
    public ScoutVesselState Clone()=>(ScoutVesselState)MemberwiseClone();
}
public sealed class AmebaEncounterState
{
    public long Number { get; set; }
    public int HabitatId { get; set; }
    public int ArrivalTurn { get; set; }
    public AmebaStage Stage { get; set; }
    public long Revision { get; set; }=1;
    public AmebaRiskOrder? RiskOrder { get; set; }
    public AmebaEncounterState Clone()=>(AmebaEncounterState)MemberwiseClone();
}
