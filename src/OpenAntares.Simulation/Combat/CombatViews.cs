using System.Collections.Immutable;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public enum CombatDamageBand { Operational, Damaged, Crippled }
public sealed record CombatStats(int Firepower,int Armor,int Speed);
public sealed record CombatShipFrame(VesselId VesselId,string RoleId,int Hull,int MaxHull,int Cohesion,int Attack,int Armor,int EffectiveSpeed,bool Flagship,int Cargo);
public sealed record SentryFrame(SentryId SentryId,int Hull,int MaxHull,int Attack,int Armor,int PursuitSpeed);
public sealed record CombatRoute(StarId Origin,int OriginX,int OriginY,StarId Destination,int DestinationX,int DestinationY);
public sealed record CombatFrame(EncounterId? EncounterId,int Turn,long Revision,EmpireId Owner,FleetId Fleet,ImmutableArray<CombatShipFrame> Ships,ImmutableArray<SentryFrame> Sentries,CombatOrders Orders)
{
    public CombatDefinition? Definition {get;init;}
    public CombatRoute? Route {get;init;}
}
public sealed record CombatDeparture(ImmutableArray<VesselId> Members,StarId Origin,StarId Destination,int Speed,int TotalTurns,CombatArrivalOrder OnArrival);
public readonly record struct CombatActor(bool Sentry,long Id);
public sealed record CombatShot(CombatActor Source,CombatActor Target,int Damage);
public sealed record CombatShipDamage(VesselId Id,int Before,int After,int ActualLoss);
public sealed record SentryDamage(SentryId Id,int Before,int After,int ActualLoss);
public sealed record CombatExchangeResult(ImmutableArray<CombatShipDamage> Ships,ImmutableArray<SentryDamage> Sentries,ImmutableArray<CombatShot> Shots,ImmutableArray<VesselId> DestroyedVessels,ImmutableArray<SentryId> DestroyedSentries);
public sealed record ShipCombatView(VesselId Id,int Hull,int Cohesion,ColonyId? ReturnColony,CombatArrivalOrder? OnArrival,bool Repairing);
public sealed record CombatPocketView(PocketId Id,StarId Star,int ObservedTurn,bool Cleared,bool Current,bool Unavailable,ImmutableArray<ObservedSentry> Sentries);
public sealed record CombatEncounterView(EncounterId Id,FleetId Fleet,PocketId Pocket,long Revision,ImmutableArray<VesselId> Members,CombatOrders Orders,CombatPhase Phase,long Round,long? ConsentRevision,bool CivilianRiskAuthorized,int? ContinueRiskTurn=null);
public sealed record CombatEmpireView(ImmutableArray<ShipCombatView> Ships,ImmutableArray<CombatPocketView> Pockets,ImmutableArray<CombatEncounterView> Encounters,ImmutableArray<CombatPendingResponse> Responses);
public sealed record CombatShipForecast(VesselId Id,CombatDamageBand Before,CombatDamageBand? After,int CohesionBefore,int CohesionAfter);
public sealed record CombatPreview(bool Allowed,string? RejectionCode,string Token)
{
    public CombatFrame? Frame {get;init;}
    public CombatExchangeResult? Exchange {get;init;}
    public ImmutableArray<CombatShipFrame> AffectedShips {get;init;}=ImmutableArray<CombatShipFrame>.Empty;
    public int Cargo {get;init;}
    public ImmutableArray<CombatShipForecast> Forecasts {get;init;}=ImmutableArray<CombatShipForecast>.Empty;
    public ImmutableDictionary<VesselId,ColonyId> RepairPorts {get;init;}=ImmutableDictionary<VesselId,ColonyId>.Empty;
    public ImmutableArray<CombatDeparture> Departures {get;init;}=ImmutableArray<CombatDeparture>.Empty;
    public ImmutableArray<VesselId> Exposed {get;init;}=ImmutableArray<VesselId>.Empty;
    public ImmutableArray<string> Explanations {get;init;}=ImmutableArray<string>.Empty;
}
public static class CombatRejectionCodes
{
    public const string Unavailable="combat_action_unavailable",StalePreview="combat_stale_preview",Reserved="combat_reserved",ResponseRequired="combat_response_required",PortClosed="combat_port_closed";
}
