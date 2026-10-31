using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public sealed class ConflictResolutionContext
{public ImmutableArray<ConflictWarDecision> NewWars {get;internal set;}=ImmutableArray<ConflictWarDecision>.Empty; public ImmutableArray<ConflictDeclarationFailure> RejectedDeclarations {get;internal set;}=ImmutableArray<ConflictDeclarationFailure>.Empty; public ImmutableHashSet<DiplomacyPair> AcceptedPeace {get;internal set;}=ImmutableHashSet<DiplomacyPair>.Empty; internal bool RelationsPrepared {get;set;} internal bool RelationsFinalized {get;set;} public GameState Snapshot {get;private init;}=null!; public int FromTurn {get;private init;} public static ConflictResolutionContext Capture(ContentSet content,GameState snapshot)=>new(){Snapshot=snapshot.Clone(),FromTurn=snapshot.Turn};}
