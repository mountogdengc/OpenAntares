using OpenAntares.Simulation.Commands;
namespace OpenAntares.Simulation.Fleets;
public enum RequestCancellation { KeepQueue, RemoveUnstarted }
public sealed record AssemblyCommand(EmpireId Issuer,int ExpectedTurn,AssemblyAction Action):Command(Issuer,ExpectedTurn);
public abstract record AssemblyAction
{
    public sealed record Accept(AssemblyDraft Draft,string Fingerprint):AssemblyAction;
    public abstract record Edit(RequestId Request,long Revision):AssemblyAction;
    public sealed record Pause(RequestId Request,long Revision):Edit(Request,Revision);
    public sealed record Resume(RequestId Request,long Revision,string Fingerprint):Edit(Request,Revision);
    public sealed record SetPriority(RequestId Request,long Revision,RequestPriority Priority,string? Fingerprint=null):Edit(Request,Revision);
    public sealed record SetContributionPaused(RequestId Request,long Revision,ColonyId Colony,bool Paused):Edit(Request,Revision);
    public sealed record ChangeRally(RequestId Request,long Revision,StarId Rally,FleetId? RallyFleet,string Fingerprint):Edit(Request,Revision);
    public sealed record DetachSlot(RequestId Request,long Revision,int Ordinal):Edit(Request,Revision);
    public sealed record Amend(RequestId Request,long Revision,AssemblyDraft Draft,string Fingerprint):Edit(Request,Revision);
    public sealed record Cancel(RequestId Request,long Revision,RequestCancellation Choice):Edit(Request,Revision);
}
