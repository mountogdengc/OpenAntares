using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;

public readonly record struct ConflictEncounterId(long Value):IComparable<ConflictEncounterId>
{public int CompareTo(ConflictEncounterId other)=>Value.CompareTo(other.Value);}
public readonly record struct PeaceOfferId(long Value);
public enum RelationKind { Peace,War }
public enum ConflictPhase { Pending,Active,WithdrawalQueued,ResponseRequired,Completed }
public enum ConflictNoticeLevel { Summary,Attention,Emergency }
public sealed record ConflictOrders(CombatDoctrine Doctrine,ColonyId? ReturnColony,StragglerPolicy Stragglers,CombatArrivalOrder OnArrival)
{public static ConflictOrders Default=>new(CombatDoctrine.Cautious,null,StragglerPolicy.KeepTogether,CombatArrivalOrder.Repair);}
public sealed record DeclarationIntent(EmpireId Owner,int Turn);
public sealed record PeaceIntent(EmpireId Owner,int Turn,bool Proposed,bool Withdrawn=false,bool Revoked=false);
public sealed record BlockadeOrderState(FleetId Fleet,EmpireId Other,StarId Star,int AcceptedTurn,long Revision);
public sealed record ConflictAttackIntent(FleetId Fleet,VesselId Target,ConflictOrders Orders,int Turn,long Revision);
public sealed record ConflictDefensePreference(FleetId? Fleet,VesselId Anchor,ConflictOrders Orders,long Revision);
public sealed record MilitaryObservation(VesselId Vessel,EmpireId Owner,string RoleId,StarId Star,int Turn,bool Current,FleetId? Fleet,int Hull,int MaxHull,int Attack,int Armor,int Speed)
{
    public string? FrameId {get;init;}
    public string? EngineId {get;init;}
    public string? ArmorId {get;init;}
    public ImmutableArray<string> WeaponBanks {get;init;}=ImmutableArray<string>.Empty;
}
public sealed record ConflictEvent(long Id,int Turn,EmpireId Viewer,EmpireId Other,string Kind,string Reason,StarId? Star=null,FleetId? OwnFleet=null,VesselId? OwnVessel=null,ConflictEncounterId? Encounter=null);
public sealed record ConflictNotice(string Key,ConflictNoticeLevel Level,string Reason,EmpireId? Other=null,StarId? Star=null,FleetId? OwnFleet=null,ConflictEncounterId? Encounter=null,long? Revision=null);
public sealed record ConflictShotCalculation(VesselId Source,VesselId Target,int BaseAttack,int SourceHull,int SourceMaxHull,int FirePercent,int TargetArmor,int Damage);
public sealed record ConflictRoundReceipt(int Turn,long Round,bool Interception,ImmutableArray<CombatShot> Shots,ImmutableArray<CombatShipDamage> Own,ImmutableArray<CombatShipDamage> Opposing)
{[JsonRequired] public ImmutableArray<ConflictShotCalculation> Calculations {get;init;}=ImmutableArray<ConflictShotCalculation>.Empty;}
public sealed class PeaceOfferState
{
    [JsonRequired] public PeaceOfferId Id {get;set;}
    [JsonRequired] public EmpireId Proposer {get;set;}
    [JsonRequired] public int CreatedTurn {get;set;}
    [JsonRequired] public long ExpiryTurn {get;set;}
    [JsonRequired] public bool? Answer {get;set;}
}
public sealed class ConflictParty
{
    [JsonRequired] public EmpireId Owner {get;set;}
    [JsonRequired] public long PrivateRevision {get;set;}=1;
    [JsonRequired] public bool DeclarationGrievance {get;set;}
    [JsonRequired] public int GrievancePeaceBoundaries {get;set;}
    [JsonRequired] public int? LastCounterpartDeclarationTurn {get;set;}
    [JsonRequired] public int? ClaimObservedSince {get;set;}
    [JsonRequired] public int? LastVisibleActivityTurn {get;set;}
    [JsonRequired] public long NextPeaceOfferTurn {get;set;}
}
public sealed class ConflictPairState
{
    [JsonRequired] public DiplomacyPair Pair {get;set;}
    [JsonRequired] public RelationKind Relation {get;set;}
    [JsonRequired] public long Revision {get;set;}=1;
    [JsonRequired] public int? WarTurn {get;set;}
    [JsonRequired] public int? LastPeaceTurn {get;set;}
    [JsonRequired] public long? TruceEndTurn {get;set;}
    [JsonRequired] public long NextPeaceOfferId {get;set;}=1;
    [JsonRequired] public List<EmpireId> Declarers {get;set;}=new();
    [JsonRequired] public List<ConflictParty> Parties {get;set;}=new();
    [JsonRequired] public List<DeclarationIntent> Declarations {get;set;}=new();
    [JsonRequired] public List<PeaceIntent> PeaceIntents {get;set;}=new();
    [JsonRequired] public PeaceOfferState? PendingPeace {get;set;}
}
public sealed class ConflictSideState
{
    [JsonRequired] public EmpireId Owner {get;set;}
    [JsonRequired] public FleetId? Fleet {get;set;}
    [JsonRequired] public VesselId Anchor {get;set;}
    [JsonRequired] public VesselId Flagship {get;set;}
    [JsonRequired] public List<VesselId> Members {get;set;}=new();
    [JsonRequired] public List<VesselId> ProtectedCivilians {get;set;}=new();
    [JsonRequired] public List<VesselId> DestroyedMembers {get;set;}=new();
    [JsonRequired] public ConflictOrders Orders {get;set;}=ConflictOrders.Default;
    [JsonRequired] public bool WithdrawQueued {get;set;}
    [JsonRequired] public bool NeedsResponse {get;set;}
    [JsonRequired] public long ConsentRevision {get;set;}=1;
}
public sealed class ConflictEncounterState
{
    [JsonRequired] public ConflictEncounterId Id {get;set;}
    [JsonRequired] public DiplomacyPair Pair {get;set;}
    [JsonRequired] public StarId Star {get;set;}
    [JsonRequired] public ConflictSideState LowSide {get;set;}=new();
    [JsonRequired] public ConflictSideState HighSide {get;set;}=new();
    [JsonRequired] public ConflictPhase Phase {get;set;}
    [JsonRequired] public int CreatedTurn {get;set;}
    [JsonRequired] public int? CompletedTurn {get;set;}
    [JsonRequired] public long Revision {get;set;}=1;
    [JsonRequired] public long Round {get;set;}
    [JsonRequired] public int? LastResolvedTurn {get;set;}
    [JsonRequired] public List<ConflictRoundReceipt> Rounds {get;set;}=new();
}
public sealed class ConflictHistoryEntry
{
    [JsonRequired] public ConflictEncounterId Encounter {get;set;}
    [JsonRequired] public EmpireId Other {get;set;}
    [JsonRequired] public StarId Star {get;set;}
    [JsonRequired] public int StartedTurn {get;set;}
    [JsonRequired] public int EndedTurn {get;set;}
    [JsonRequired] public string Outcome {get;set;}=string.Empty;
    [JsonRequired] public List<VesselId> LostMembers {get;set;}=new();
    [JsonRequired] public List<ConflictRoundReceipt> Rounds {get;set;}=new();
}
public sealed class ConflictOwnerState
{
    [JsonRequired] public long NextEventId {get;set;}=1;
    [JsonRequired] public EmpireId Owner {get;set;}
    [JsonRequired] public long Revision {get;set;}=1;
    [JsonRequired] public List<ConflictDefensePreference> Preferences {get;set;}=new();
    [JsonRequired] public List<ConflictAttackIntent> Attacks {get;set;}=new();
    [JsonRequired] public List<BlockadeOrderState> Blockades {get;set;}=new();
    [JsonRequired] public List<OwnBlockadePressure> PublishedPressure {get;set;}=new();
    [JsonRequired] public List<MilitaryObservation> Observations {get;set;}=new();
    [JsonRequired] public List<ConflictEvent> Events {get;set;}=new();
    [JsonRequired] public List<ConflictNotice> Notices {get;set;}=new();
    [JsonRequired] public List<ConflictHistoryEntry> History {get;set;}=new();
}
public sealed class ConflictState
{
    [JsonRequired] public long NextEncounterId {get;set;}=1;
    [JsonRequired] public long NextEventId {get;set;}=1;
    [JsonRequired] public List<ConflictPairState> Pairs {get;set;}=new();
    [JsonRequired] public List<ConflictOwnerState> Owners {get;set;}=new();
    [JsonRequired] public List<ConflictEncounterState> Encounters {get;set;}=new();
    public ConflictState Clone()=>JsonSerializer.Deserialize<ConflictState>(JsonSerializer.Serialize(this))!;
}
