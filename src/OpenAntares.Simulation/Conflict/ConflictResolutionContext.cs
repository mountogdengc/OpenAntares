using System.Collections.Generic;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public sealed record ConflictDestruction(ConflictEncounterId Encounter,VesselId Vessel,EmpireId Owner,string RoleId,StarId Star,bool Flagship);
public sealed record ConflictAppliedRound(ConflictBattleFrame Frame,ConflictExchangeResult Result,bool Interception);
internal sealed record ConflictInterruptedRefit(EmpireId Owner,ShipDesigns.RefitJob Job);
public sealed class ConflictResolutionContext
{
    public ImmutableArray<ConflictPeaceDecision> PeaceDecisions {get;internal set;}=ImmutableArray<ConflictPeaceDecision>.Empty;
    public ImmutableArray<ConflictWarDecision> NewWars {get;internal set;}=ImmutableArray<ConflictWarDecision>.Empty;
    public ImmutableArray<ConflictDeclarationFailure> RejectedDeclarations {get;internal set;}=ImmutableArray<ConflictDeclarationFailure>.Empty;
    public ImmutableHashSet<DiplomacyPair> AcceptedPeace {get;internal set;}=ImmutableHashSet<DiplomacyPair>.Empty;
    public ImmutableArray<ConflictBattleFrame> Battles {get;internal set;}=ImmutableArray<ConflictBattleFrame>.Empty;
    internal HashSet<VesselId> UsedSet {get;}=new();
    internal HashSet<ConflictEncounterId> PausedSet {get;}=new();
    public ImmutableHashSet<VesselId> UsedVessels=>UsedSet.ToImmutableHashSet();
    public ImmutableHashSet<ConflictEncounterId> PausedEncounters=>PausedSet.ToImmutableHashSet();
    internal bool RelationsPrepared {get;set;}
    internal bool RelationsFinalized {get;set;}
    internal bool MatchingResolved {get;set;}
    internal bool ExchangesResolved {get;set;}
    internal bool WithdrawalsResolved {get;set;}
    internal bool RefitsInterrupted {get;set;}
    internal bool AftermathApplied {get;set;}
    internal bool MovementApplied {get;set;}
    internal bool FoundingDeparturesApplied {get;set;}
    internal List<ConflictDeparture> DeferredDepartures {get;}=new();
    internal List<ConflictInterruptedRefit> InterruptedRefits {get;}=new();
    public ImmutableArray<ConflictDeparture> Departures {get;internal set;}=ImmutableArray<ConflictDeparture>.Empty;
    internal HashSet<VesselId> VolleyParticipants {get;}=new();
    internal List<ConflictDestruction> DestructionFacts {get;}=new();
    internal List<ConflictAppliedRound> RoundFacts {get;}=new();
    public ImmutableArray<ConflictDestruction> Destructions=>DestructionFacts.ToImmutableArray();
    public ImmutableArray<ConflictAppliedRound> Rounds=>RoundFacts.ToImmutableArray();
    public GameState Snapshot {get;private init;}=null!;
    public int FromTurn {get;private init;}
    internal Fleets.FleetResolutionContext? FleetContext {get;private init;}
    public static ConflictResolutionContext Capture(ContentSet content,GameState snapshot)=>Capture(content,snapshot,Rules.HasConflict(snapshot.RulesVersion)?Fleets.FleetResolutionContext.Capture(content,snapshot):null);
    public static ConflictResolutionContext Capture(ContentSet content,GameState snapshot,Fleets.FleetResolutionContext? fleet)=>new(){Snapshot=snapshot.Clone(),FromTurn=snapshot.Turn,FleetContext=fleet};
}
