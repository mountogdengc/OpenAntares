using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public enum CombatResponse { AcceptRevisedForce, RemainWithBlockedReturn }
public abstract record CombatAction
{
    public sealed record AttackPocket(FleetId Fleet,PocketId Pocket,CombatOrders Orders,bool AuthorizeCivilianRisk):CombatAction;
    public sealed record ChangeCombatOrders(EncounterId Encounter,long Revision,CombatOrders Orders,bool AuthorizeCivilianRisk):CombatAction;
    public sealed record WithdrawCombat(EncounterId Encounter,long Revision):CombatAction;
    public sealed record SetRepair(ImmutableArray<VesselId> Vessels,bool Enabled):CombatAction;
    public sealed record RespondCombat(EncounterId Encounter,long Revision,CombatResponse Response):CombatAction;
}
public sealed record CombatActionCommand(EmpireId Issuer,int ExpectedTurn,CombatAction Action,string PreviewToken):Command(Issuer,ExpectedTurn);
