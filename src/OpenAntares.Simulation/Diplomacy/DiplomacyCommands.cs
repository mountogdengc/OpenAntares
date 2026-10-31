using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public sealed record DiplomacyCommand(EmpireId Issuer,int ExpectedTurn,EmpireId Other,long PublicRevision,long PrivateRevision,DiplomacyAction Action):Command(Issuer,ExpectedTurn);
public abstract record DiplomacyAction
{
    public sealed record OfferSettlement(StarId Star):DiplomacyAction;
    public sealed record OfferTrade(ColonyId Hub):DiplomacyAction;
    public sealed record Answer(OfferId Id,bool Accept,ColonyId? Hub):DiplomacyAction;
    public sealed record RevokeAnswer(OfferId Id):DiplomacyAction;
    public sealed record Withdraw(OfferId Id):DiplomacyAction;
    public sealed record Terminate(AgreementKind Kind):DiplomacyAction;
}
