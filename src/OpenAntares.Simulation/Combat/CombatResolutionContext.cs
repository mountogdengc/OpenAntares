using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public sealed record CombatDestruction(EncounterId Encounter,VesselId Vessel,EmpireId Owner,string RoleId,int Cargo,bool Flagship,StarId Star);
public sealed record CombatRoundReceipt(EncounterId Encounter,int Turn,long Round,bool Interception,CombatFrame Before,CombatExchangeResult Result,ImmutableArray<CombatShipForecast> Forecasts)
{
    public ImmutableArray<VesselId> Participants {get;init;}=ImmutableArray<VesselId>.Empty;
}
public sealed class CombatResolutionContext
{
    public int FromTurn {get;private init;}
    public ImmutableArray<EncounterId> Encounters {get;private init;}
    internal FleetResolutionContext FleetContext {get;private init;}=null!;
    internal CombatDefinition? Definition {get;private init;}
    internal bool ReportsFinalized {get;set;}
    public ImmutableArray<OwnedCombatDeparture> ReportDepartures {get;internal set;}=ImmutableArray<OwnedCombatDeparture>.Empty;
    public ImmutableArray<OwnedCombatEvent> ReportEvents {get;internal set;}=ImmutableArray<OwnedCombatEvent>.Empty;
    internal bool RepairsApplied {get;set;}
    internal GameState Snapshot {get;private init;}=null!;
    internal bool WithdrawalsResolved {get;set;}
    internal bool ExchangesResolved {get;set;}
    internal HashSet<VesselId> ConsumedSet {get;}=new();
    internal List<CombatDestruction> DestructionFacts {get;}=new();
    internal List<CombatRoundReceipt> RoundFacts {get;}=new();
    public ImmutableHashSet<VesselId> Consumed=>ConsumedSet.ToImmutableHashSet();
    public ImmutableArray<CombatDestruction> Destructions=>DestructionFacts.ToImmutableArray();
    public ImmutableArray<CombatRoundReceipt> Rounds=>RoundFacts.ToImmutableArray();
    public ImmutableDictionary<VesselId,CombatDeparture> Departures {get;internal set;}=ImmutableDictionary<VesselId,CombatDeparture>.Empty;
    public ImmutableArray<VesselId> RepairCapture {get;internal set;}=ImmutableArray<VesselId>.Empty;
    public static CombatResolutionContext Capture(ContentSet content,GameState snapshot)=>new(){Definition=content.Combat,RepairCapture=CombatRepair.Capture(content,snapshot),Snapshot=snapshot.Clone(),FromTurn=snapshot.Turn,Encounters=snapshot.Combat?.Encounters.OrderBy(e=>e.Id).Select(e=>e.Id).ToImmutableArray()??ImmutableArray<EncounterId>.Empty,FleetContext=FleetResolutionContext.Capture(content,snapshot)};
}
