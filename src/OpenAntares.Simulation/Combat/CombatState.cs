using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public readonly record struct PocketId(int Value) : IComparable<PocketId> { public int CompareTo(PocketId other)=>Value.CompareTo(other.Value); }
public readonly record struct SentryId(int Value) : IComparable<SentryId> { public int CompareTo(SentryId other)=>Value.CompareTo(other.Value); }
public readonly record struct EncounterId(long Value) : IComparable<EncounterId> { public int CompareTo(EncounterId other)=>Value.CompareTo(other.Value); }
public enum CombatPhase { Pending, Active, WithdrawalQueued, BlockedResponse }
public sealed record PocketObservation(PocketId Pocket, StarId Star, int Turn, bool Cleared, ImmutableArray<ObservedSentry> Sentries);
public sealed record ObservedSentry(SentryId Id, CombatProfile Profile, int Hull, int PursuitSpeed);
public sealed record CombatPendingResponse(EncounterId Encounter, long Revision, string Reason);
public sealed record CombatHistoryEntry(EncounterId Encounter, PocketId Pocket, int Turn, string Outcome, ImmutableArray<VesselId> Destroyed, long Rounds);
public sealed record CombatEvent(long Id, int Turn, string Kind, string Reason, EncounterId? Encounter = null, VesselId? Vessel = null);
public sealed class CombatGameState
{
    public bool SentriesEnabled {get;set;}
    public int NextPocketId {get;set;}=1;
    public long NextEncounterId {get;set;}=1;
    public List<PocketState> Pockets {get;set;}=new();
    public List<EncounterState> Encounters {get;set;}=new();
    public CombatGameState Clone()=>new(){SentriesEnabled=SentriesEnabled,NextPocketId=NextPocketId,NextEncounterId=NextEncounterId,Pockets=Pockets.Select(p=>p.Clone()).ToList(),Encounters=Encounters.Select(e=>e.Clone()).ToList()};
}
public sealed class CombatEmpireState
{
    public long NextEventId {get;set;}=1;
    public long LifetimeEncounters {get;set;}
    public long LifetimeShipsLost {get;set;}
    public List<PocketObservation> Observations {get;set;}=new();
    public List<CombatPendingResponse> Responses {get;set;}=new();
    public List<CombatHistoryEntry> History {get;set;}=new();
    public List<CombatEvent> PendingEvents {get;set;}=new();
    public List<CombatEvent> RecentEvents {get;set;}=new();
    public CombatEmpireState Clone(){var c=(CombatEmpireState)MemberwiseClone();c.Observations=new(Observations);c.Responses=new(Responses);c.History=new(History);c.PendingEvents=new(PendingEvents);c.RecentEvents=new(RecentEvents);return c;}
}
public sealed class ShipCombatState
{
    public int Hull {get;set;}
    public int Cohesion {get;set;}=100;
    public ColonyId? ReturnColony {get;set;}
    public CombatArrivalOrder? OnArrival {get;set;}
    public bool Repairing {get;set;}
    public ShipCombatState Clone()=>(ShipCombatState)MemberwiseClone();
}
public sealed class PocketState
{
    public PocketId Id {get;set;}
    public StarId Star {get;set;}
    public int NextSentryId {get;set;}=1;
    public List<SentryState> Sentries {get;set;}=new();
    public bool Cleared {get;set;}
    public PocketState Clone(){var c=(PocketState)MemberwiseClone();c.Sentries=Sentries.Select(s=>s.Clone()).ToList();return c;}
}
public sealed class SentryState
{
    public SentryId Id {get;set;}
    public CombatProfile Profile {get;set;}=new(1,0,0);
    public int Hull {get;set;}
    public SentryState Clone()=>(SentryState)MemberwiseClone();
}
public sealed class EncounterState
{
    public EncounterId Id {get;set;}
    public EmpireId Owner {get;set;}
    public FleetId Fleet {get;set;}
    public PocketId Pocket {get;set;}
    public long Revision {get;set;}=1;
    public List<VesselId> Members {get;set;}=new();
    public CombatOrders Orders {get;set;}=new(CombatDoctrine.Cautious,default,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair);
    public CombatPhase Phase {get;set;}
    public long Round {get;set;}
    public int? LastResolvedTurn {get;set;}
    public CombatPhase? LastResolvedPhase {get;set;}
    public long? ConsentRevision {get;set;}
    public bool CivilianRiskAuthorized {get;set;}
    public EncounterState Clone(){var c=(EncounterState)MemberwiseClone();c.Members=new(Members);return c;}
}
