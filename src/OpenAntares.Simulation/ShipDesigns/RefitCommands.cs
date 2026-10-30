using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.ShipDesigns;
public sealed record RefitCommand(EmpireId Issuer,int ExpectedTurn,RefitAction Action,string Token):Command(Issuer,ExpectedTurn);
public abstract record RefitAction
{
    public sealed record Start(VesselId Vessel,ColonyId Port,DesignId Target):RefitAction;
    public sealed record Cancel(RefitId Job,long Revision):RefitAction;
    public sealed record Resume(RefitId Job,long Revision):RefitAction;
}
