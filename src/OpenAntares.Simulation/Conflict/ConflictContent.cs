namespace OpenAntares.Simulation.Conflict;
public sealed record ConflictDefinition
{
    public required string AlgorithmId {get;init;}
    public required int Truce {get;init;}
    public required int OfferLifetime {get;init;}
    public required int DeclarationGrievance {get;init;}
    public required int GrievancePeaceLifetime {get;init;}
    public required int BlockadeRetainedPercent {get;init;}
    public required int PeaceHullTarget {get;init;}
    public required int WarHullTarget {get;init;}
    public required int MilitaryHullCap {get;init;}
    public required int FormationLimit {get;init;}
    public required int ActiveEncounterLimit {get;init;}
    public required int HistoryLimit {get;init;}
    public required int RoundHistoryLimit {get;init;}
    public required int ContactGate {get;init;}
    public required int ClaimGate {get;init;}
    public required int PeaceEarliest {get;init;}
    public required int PeaceDeadline {get;init;}
    public required int PeaceInactivity {get;init;}
    public required int PeaceCooldown {get;init;}
    public required int RepairHullPercent {get;init;}
    public required int HealthyCohesion {get;init;}
    public required int GroupingCommandLimit {get;init;}
    public required int FleetCommandLimit {get;init;}
}
