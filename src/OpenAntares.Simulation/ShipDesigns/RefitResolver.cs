using System.Collections.Immutable;
using System;
using System.Linq;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.ShipDesigns;
public sealed record RefitApplyResult(TurnEconomy Economy,ImmutableArray<ShipDesignEvent> Events);
public static class RefitResolver
{
    public static RefitApplyResult Apply(ContentSet content,GameState snapshot,GameState candidate,RefitResolutionContext context,TurnEconomy economy,Conflict.ConflictResolutionContext? conflict=null)
    {
        if(context.Applied||context.FromTurn!=snapshot.Turn||candidate.Turn!=snapshot.Turn)throw new InvalidOperationException("Refit phase already applied or snapshot changed.");
        context.Applied=true;var outcomes=economy.Colonies.ToBuilder();var events=ImmutableArray.CreateBuilder<ShipDesignEvent>();
        foreach(var captured in context.Jobs)
        {
            var original=captured.Job;var data=candidate.FindEmpire(captured.Owner)!.ShipDesigns!;var job=data.Refits.SingleOrDefault(j=>j.Id==original.Id);
            if(job is null)
            {
                if(conflict is null||!Conflict.ConflictAftermath.ProvesInterruption(snapshot,candidate,conflict,captured.Owner,original))throw new InvalidOperationException("Committed refit disappeared during resolution.");
                foreach(var frozenFact in new[]{captured.Before,captured.After})
                {
                    var row=data.Designs.Single(d=>d.Id==frozenFact.Id);
                    if(!ShipDesignStateValidator.SameRecipe(row.Recipe,frozenFact.Recipe)||ShipDesignMath.Calculate(content.ShipDesigns!,row.Recipe)!=frozenFact.Stats)throw new InvalidOperationException("Interrupted refit recipe or embedded stats changed.");
                }
                int interruptedIndex=Array.FindIndex(economy.Colonies.ToArray(),r=>r.Colony==original.Port);if(interruptedIndex<0)throw new InvalidOperationException("Interrupted refit economic receipt missing.");
                var interrupted=outcomes[interruptedIndex];var frozen=interrupted.RefitPlan;
                if(frozen is null||frozen.Job!=original.Id||frozen.Owner!=captured.Owner||frozen.Source!=original.SourceDesign||frozen.Target!=original.TargetDesign||frozen.Price!=original.Price||frozen.Vessel!=original.Vessel||frozen.Port!=original.Port)throw new InvalidOperationException("Interrupted refit accounting identity changed.");
                outcomes[interruptedIndex]=interrupted with{RefitPlan=frozen with{Covered=false},Production=interrupted.Production with{CompletedId=null,BlockedReasonCode="refit_interrupted",Estimate=new TurnEstimate(null)}};
                events.Add(ShipDesignReports.Record(candidate,captured.Owner,"refit_cancelled","An empire encounter interrupted this unpaid refit before its first defensive volley. Installed equipment, banked reserve and the unstarted queue are retained; construction remains paused.",original.Vessel,original.Port,captured.Before,captured.After,0));continue;
            }
            if(job.Vessel!=original.Vessel||job.Port!=original.Port||job.SourceDesign!=original.SourceDesign||job.TargetDesign!=original.TargetDesign||job.Price!=original.Price||job.Revision!=original.Revision||job.Status!=original.Status||job.AcceptedTurn!=original.AcceptedTurn)throw new InvalidOperationException("Committed refit identity changed during resolution.");
            foreach(var fact in new[]{captured.Before,captured.After})
            {
                var row=data.Designs.Single(d=>d.Id==fact.Id);
                if(!ShipDesignStateValidator.SameRecipe(row.Recipe,fact.Recipe)||ShipDesignMath.Calculate(content.ShipDesigns!,row.Recipe)!=fact.Stats)throw new InvalidOperationException("Committed recipe or embedded stats changed.");
            }
            var port=candidate.FindColony(job.Port)??throw new InvalidOperationException("Refit port disappeared.");
            if(port.EmpireId!=captured.Owner||ShipDesignTargets.Job(candidate,port)?.Id!=job.Id)throw new InvalidOperationException("Refit production slot changed.");
            int index=Array.FindIndex(economy.Colonies.ToArray(),r=>r.Colony==port.Id);if(index<0)throw new InvalidOperationException("Refit economic receipt missing.");
            var receipt=outcomes[index];var plan=receipt.RefitPlan;
            if(plan is null||plan.Job!=job.Id||plan.Owner!=captured.Owner||plan.Source!=job.SourceDesign||plan.Target!=job.TargetDesign||plan.Price!=job.Price||plan.Vessel!=job.Vessel||plan.Port!=job.Port||receipt.Production.Ending!=port.ProductionReserve||port.ProductionReserve<0)throw new InvalidOperationException("Refit provisional accounting changed.");
            var ship=candidate.FindVessel(job.Vessel);
            ShipDesignEvent Event(string kind,string reason,long paid)=>ShipDesignReports.Record(candidate,captured.Owner,kind,reason,job.Vessel,job.Port,captured.Before,captured.After,paid);
            void Clear(){data.Refits.Remove(job);port.ProjectId=null;port.ProjectKind=ProductionKinds.Building;port.Development!.ConstructionEnabled=false;port.Development.ActiveQueueToken=null;port.Development.Origin=ProductionOrigin.None;}
            if(ship is null)
            {
                Clear();events.Add(Event("refit_cancelled_missing_ship","The committed ship was lost; refit cancelled without payment. Banked reserve remains at the port.",0));
                outcomes[index]=receipt with{RefitPlan=plan with{Covered=false},Production=receipt.Production with{CompletedId=null,BlockedReasonCode="refit_ship_missing",Estimate=new TurnEstimate(null)}};continue;
            }
            if(ship.EmpireId!=captured.Owner||ship.Design!=job.SourceDesign)throw new InvalidOperationException("Refit source equipment changed during resolution.");
            if(job.Status==RefitStatus.Paused)continue;
            if(RefitEligibility.Reason(content,candidate,captured.Owner,job) is {} reason)
            {
                job.Status=RefitStatus.Paused;job.Revision=checked(job.Revision+1);job.Reason="The ship no longer meets the required condition, location or order constraints. Review before resuming.";
                events.Add(Event("refit_paused",job.Reason,0));outcomes[index]=receipt with{RefitPlan=plan with{Covered=false},Production=receipt.Production with{CompletedId=null,BlockedReasonCode=reason,Estimate=new TurnEstimate(null)}};continue;
            }
            if(!plan.Covered||Diplomacy.DiplomacyReservations.Spendable(candidate,port.Id)<job.Price)continue;
            port.ProductionReserve=checked(port.ProductionReserve-job.Price);ship.Design=job.TargetDesign;
            string key=ShipDesignTargets.Refit(job.Id).ContentId;var change=Quantity.From(receipt.Production.Change.Lines.Add(new(BreakdownKinds.CostPaid,key,null,-job.Price)));
            outcomes[index]=receipt with{Production=receipt.Production with{Ending=port.ProductionReserve,Change=change,CompletedId=key,BlockedReasonCode=null,Estimate=new TurnEstimate(1)}};
            Clear();events.Add(Event("refit_completed","Paid refit completed after a full stationary interval; equipment changes apply to subsequent snapshots.",job.Price));
        }
        return new(economy with{Colonies=outcomes.ToImmutable()},events.ToImmutable());
    }
}
