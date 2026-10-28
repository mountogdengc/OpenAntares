using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Fleets;
internal enum FleetResolutionPhase { Preparation, Snapshot, Economy, Health, Loss, Transport, Founding, Observation, Rewards, Recovery, Reports, Validation }
internal sealed record OfficerSnapshot(EmpireId Owner, OfficerRecord Officer, bool RecoveryEquipment);
internal sealed record FlagshipLoss(OfficerSnapshot Appointment, VesselId Vessel, StarId Location);
public sealed class FleetResolutionContext
{
    internal ImmutableArray<OfficerSnapshot> Officers { get; private init; }
    internal ImmutableDictionary<VesselId,FleetTransitProvenance> Launches { get; private init; } = ImmutableDictionary<VesselId,FleetTransitProvenance>.Empty;
    internal HashSet<VesselId> Completed { get; } = new();
    internal List<FlagshipLoss> Losses { get; } = new();
    internal List<(EmpireId Owner,RequestId Request)> Fulfilled { get; } = new();
    internal List<FleetEvent> Events { get; } = new();
    internal static FleetResolutionContext Capture(ContentSet content,GameState snapshot)
    {
        var launches=ImmutableDictionary.CreateBuilder<VesselId,FleetTransitProvenance>();
        foreach(var v in snapshot.Vessels.Where(v=>v.PendingMove is not null))
        {
            var speed=v.Fleet is {} group?snapshot.FindEmpire(v.EmpireId)!.Fleets!.Fleets.Single(f=>f.Id==group).Members.Min(id=>OfficerBonuses.EffectiveSpeed(content,snapshot,id)):OfficerBonuses.EffectiveSpeed(content,snapshot,v.Id);
            launches.Add(v.Id,new(v.StationaryStarId!.Value,v.PendingMove!.Value,FleetTravel.LaunchDuration(content,snapshot,v,v.PendingMove.Value),snapshot.Turn,speed));
        }
        return new(){Launches=launches.ToImmutable(),Officers=snapshot.Empires.OrderBy(e=>e.Id).SelectMany(e=>e.Fleets!.Officers.OrderBy(o=>o.Id).Select(o=>new OfficerSnapshot(e.Id,o.Clone(),e.KnownTechnologyIds.Contains(content.FleetCommand!.RecoveryTechnologyId)))).ToImmutableArray()};
    }
    internal void Event(GameState state,EmpireId owner,string kind,string reason,FleetId? fleet=null,RequestId? request=null,OfficerId? officer=null,VesselId? vessel=null,FleetImportance importance=FleetImportance.Summary)
    {
        var f=state.FindEmpire(owner)!.Fleets!;long id=f.NextEventId;f.NextEventId=checked(id+1);Events.Add(new(owner,id,state.Turn,kind,reason,importance,fleet,request,officer,vessel));
    }
}
