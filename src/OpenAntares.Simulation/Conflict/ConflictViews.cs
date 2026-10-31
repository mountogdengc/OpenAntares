using System.Collections.Immutable;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public sealed record PeaceOfferView(PeaceOfferId Id,EmpireId Proposer,int CreatedTurn,long ExpiryTurn,bool? OwnAnswer);
public sealed record ConflictRelationView(EmpireId Other,RelationKind Relation,long Revision,long OwnRevision,int? WarTurn,long? TruceEndTurn,bool OwnDeclarationQueued,bool OwnPeaceProposed,PeaceOfferView? Peace,int OwnGrievance,int OtherGrievance,int GrievancePeaceBoundaries,int? OwnClaimObservedSince,int? OwnLastActivityTurn,long NextOwnPeaceOfferTurn);
public sealed record OwnConflictShipView(VesselId Id,FleetId? Fleet,StarId? Star,int Hull,int MaxHull,int Cohesion,int Attack,int Armor,int Speed,bool Military,bool NewlyCompleted,bool Repairing,bool Refitting,bool InSentry,bool InConflict,bool AssemblyBound,ConflictOrders Defense);
public sealed record OwnConflictEncounterView(ConflictEncounterId Id,EmpireId Other,StarId Star,FleetId? OwnFleet,long Revision,long Round,ConflictPhase Phase,ImmutableArray<VesselId> OwnMembers,ImmutableArray<VesselId> ProtectedCivilians,ConflictOrders OwnOrders,bool OwnWithdrawQueued,bool OwnNeedsResponse,ImmutableArray<MilitaryObservation> Opposing);
public sealed record OwnBlockadeView(FleetId Fleet,EmpireId Other,StarId Star,long EffectiveTurn,string Status);
public sealed record ConflictHistoryView(ConflictEncounterId Encounter,EmpireId Other,StarId Star,int StartedTurn,int EndedTurn,string Outcome,ImmutableArray<VesselId> LostMembers,ImmutableArray<ConflictRoundReceipt> Rounds);
public sealed record ConflictView(EmpireId Viewer,int Turn,long OwnRevision,ImmutableArray<ConflictRelationView> Relations,ImmutableArray<OwnConflictShipView> Ships,ImmutableArray<MilitaryObservation> Military,ImmutableArray<OwnConflictEncounterView> Encounters,ImmutableArray<OwnBlockadeView> Blockades,ImmutableArray<ConflictEvent> Events,ImmutableArray<ConflictNotice> Notices,ImmutableArray<ConflictHistoryView> History)
{public ImmutableArray<ConflictAttackIntent> OwnAttacks {get;init;}=ImmutableArray<ConflictAttackIntent>.Empty;}
public sealed record ConflictPreview(bool Allowed,string? RejectionCode,string Token)
{
    public ConflictVolleyView? Volley {get;init;}
    public long? EffectiveTurn {get;init;}
    public long? TruceEndTurn {get;init;}
    public ImmutableArray<string> Explanations {get;init;}=ImmutableArray<string>.Empty;
}
public sealed record ConflictVolleyView(ImmutableArray<Combat.CombatShot> Shots,ImmutableArray<Combat.CombatShipDamage> Own,ImmutableArray<Combat.CombatShipDamage> Opposing,bool Conditional,string Explanation);
