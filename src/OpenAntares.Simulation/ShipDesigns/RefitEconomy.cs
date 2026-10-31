using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.ShipDesigns;
public sealed record RefitPlan(RefitId Job,EmpireId Owner,VesselId Vessel,ColonyId Port,DesignId Source,DesignId Target,long Price,bool Covered);
public static class RefitEconomy
{
    public static ColonyEconomy Calculate(ContentSet content,GameState state,ColonyState colony,ColonyEconomy result)
    {
        if(colony.ProjectKind!=ProductionKinds.Refit)return result;
        var job=ShipDesignTargets.Job(state,colony)??throw new System.ArgumentException("Refit project requires its committed job.");
        string? reason=job.Status==RefitStatus.Paused?"refit_paused":RefitEligibility.Reason(content,state,colony.EmpireId,job);
        var bank=Diplomacy.DiplomacyReservations.CalculateReserve(state,colony,result.NetProduction.Total,colony.ProjectId,job.Price,false) with{TargetKind=ProductionKinds.Refit,BlockedReasonCode=reason};
        if(reason is not null)bank=bank with{Estimate=new TurnEstimate(null)};
        return result with{Production=bank,VesselCompletionPlan=null,EmbarkOutcome=null,RefitPlan=new(job.Id,colony.EmpireId,job.Vessel,colony.Id,job.SourceDesign,job.TargetDesign,job.Price,reason is null&&bank.Ending-Diplomacy.DiplomacyReservations.Reserved(state,colony.Id)>=job.Price)};
    }
}
