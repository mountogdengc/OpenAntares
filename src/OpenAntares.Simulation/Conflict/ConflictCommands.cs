using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public sealed record ConflictCommand(EmpireId Issuer,int ExpectedTurn,ConflictAction Action,string Token):Command(Issuer,ExpectedTurn);
public abstract record ConflictAction
{
    public sealed record DeclareWar(EmpireId Other):ConflictAction;
    public sealed record RevokeDeclaration(EmpireId Other):ConflictAction;
    public sealed record ProposePeace(EmpireId Other):ConflictAction;
    public sealed record AnswerPeace(EmpireId Other,PeaceOfferId Offer,bool Accept):ConflictAction;
    public sealed record WithdrawPeace(EmpireId Other,PeaceOfferId Offer):ConflictAction;
    public sealed record RevokePeaceAnswer(EmpireId Other,PeaceOfferId Offer):ConflictAction;
    public sealed record ConfigureDefense(VesselId Anchor,ConflictOrders Orders):ConflictAction;
    public sealed record Attack(FleetId Fleet,VesselId Target,ConflictOrders Orders):ConflictAction;
    public sealed record CancelAttack(FleetId Fleet):ConflictAction;
    public sealed record ChangeOrders(ConflictEncounterId Encounter,long Revision,ConflictOrders Orders):ConflictAction;
    public sealed record Withdraw(ConflictEncounterId Encounter,long Revision):ConflictAction;
    public sealed record Respond(ConflictEncounterId Encounter,long Revision):ConflictAction;
    public sealed record Blockade(FleetId Fleet,EmpireId Other):ConflictAction;
    public sealed record LiftBlockade(FleetId Fleet):ConflictAction;
}
