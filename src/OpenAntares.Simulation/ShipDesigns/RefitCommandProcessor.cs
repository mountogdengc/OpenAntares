using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Development;
using System;
using System.Linq;
namespace OpenAntares.Simulation.ShipDesigns;
public static class RefitCommandProcessor
{
    public static CommandOutcome Execute(ContentSet content,GameState state,RefitCommand command)
    {
        CommandOutcome Reject(string code)=>CommandOutcome.Reject(state,code,"Refit unavailable or changed: review the current ship, port and job before applying.");
        var owner=state.FindEmpire(command.Issuer);
        if(owner is not{Controller:ControllerKind.Human,ShipDesigns:not null}||!Rules.HasShipDesigns(state.RulesVersion))return Reject("refit_unavailable");
        if(command.ExpectedTurn!=state.Turn)return Reject(RejectionCodes.StaleTurn);
        try
        {
            var preview=RefitQueries.Preview(content,EmpireViewProjector.Project(content,state,owner.Id),command.Action);
            if(!preview.Allowed)return Reject(preview.RejectionCode??"refit_unavailable");
            if(string.IsNullOrEmpty(command.Token)||command.Token!=preview.Token)return Reject("refit_stale");
            var candidate=state.Clone();var empire=candidate.FindEmpire(owner.Id)!;var data=empire.ShipDesigns!;
            var port=candidate.FindColony(preview.Port!.Value)!;var ship=candidate.FindVessel(preview.Vessel!.Value)!;
            RefitJob? job=preview.Job is {} id?data.Refits.Single(j=>j.Id==id):null;
            string kind;
            switch(command.Action)
            {
                case RefitAction.Start:
                    if(job is not null)return CommandOutcome.NoChange(state);
                    int number=data.NextRefitId;data.NextRefitId=checked(number+1);
                    job=new(){Id=new(number),Vessel=ship.Id,Port=port.Id,SourceDesign=preview.Source!.Value,TargetDesign=preview.Target!.Value,AcceptedTurn=state.Turn,Price=preview.Price!.Value,Status=RefitStatus.Active};data.Refits.Add(job);
                    port.ProjectKind=ProductionKinds.Refit;port.ProjectId=ShipDesignTargets.Refit(job.Id).ContentId;
                    port.Development!.ConstructionEnabled=false;port.Development.ActiveQueueToken=null;port.Development.Origin=ProductionOrigin.Manual;kind="refit_accepted";break;
                case RefitAction.Cancel:
                    data.Refits.Remove(job!);port.ProjectId=null;port.ProjectKind=ProductionKinds.Building;port.Development!.ConstructionEnabled=false;port.Development.ActiveQueueToken=null;port.Development.Origin=ProductionOrigin.None;kind="refit_cancelled";break;
                case RefitAction.Resume:
                    if(job!.Status==RefitStatus.Active)return CommandOutcome.NoChange(state);
                    job.Status=RefitStatus.Active;job.Reason=null;job.Revision=checked(job.Revision+1);kind="refit_resumed";break;
                default:return Reject("refit_unavailable");
            }
            long eventId=data.NextEventId;data.NextEventId=checked(eventId+1);
            EquipmentFact Fact(DesignId id){var row=data.Designs.Single(d=>d.Id==id);return new(row.Id,row.Name,row.Recipe,ShipDesignMath.Calculate(content.ShipDesigns!,row.Recipe));}
            data.PendingEvents.Add(new(owner.Id,eventId,state.Turn,kind,kind=="refit_cancelled"?"Refit cancelled; old equipment and all banked reserve retained.":kind=="refit_resumed"?"Refit resumed with its original price and equipment commitment.":"Refit accepted; no payment or equipment change until completion.",ship.Id,port.Id,Fact(job!.SourceDesign),Fact(job.TargetDesign),0));
            while(data.PendingEvents.Count>20)data.PendingEvents.RemoveAt(0);empire.Ready=false;
            if(GameStateValidator.Validate(content,candidate).Count!=0)return Reject("refit_invalid");
            return new(candidate,null,true,null);
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or OverflowException){return Reject("refit_invalid");}
    }
}
