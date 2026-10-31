using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public sealed class ConflictResolutionContext
{public GameState Snapshot {get;private init;}=null!; public int FromTurn {get;private init;} public static ConflictResolutionContext Capture(ContentSet content,GameState snapshot)=>new(){Snapshot=snapshot.Clone(),FromTurn=snapshot.Turn};}
