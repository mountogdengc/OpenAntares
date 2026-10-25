using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Scouting;
internal enum ScoutResolutionPhase { Preparation,Economy,Health,Encounter,Transport,Observation,Reward,Reports,Validation }
public sealed class AmebaResolutionContext
{
    public int ToTurn { get; private init; }
    public ImmutableArray<VesselId> EncounterInputs { get; private init; }
    public HashSet<VesselId> ConsumedVessels { get; }=new();
    public HashSet<VesselId> DepartingVessels { get; }=new();
    public List<ScoutOrderEvent> Events { get; }=new();
    public Dictionary<EmpireId,long> ResearchAwards { get; }=new();
    public static AmebaResolutionContext Capture(ContentSet c,GameState s)=>new(){ToTurn=checked(s.Turn+1),EncounterInputs=s.Vessels.Where(v=>v.AmebaEncounter is not null).OrderBy(v=>v.EmpireId).ThenBy(v=>v.Id).Select(v=>v.Id).ToImmutableArray()};
}
