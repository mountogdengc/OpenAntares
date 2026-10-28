using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Fleets;
internal static class OfficerRecovery
{
    internal static void Apply(ContentSet content,GameState snapshot,GameState candidate,FleetResolutionContext context)
    {
        var d=content.FleetCommand!;
        ColonyId? Destination(EmpireId owner)=>candidate.Colonies.Where(c=>c.EmpireId==owner).OrderBy(c=>c.Id).Select(c=>(ColonyId?)c.Id).FirstOrDefault();
        bool Own(EmpireId owner,ColonyId? colony)=>colony is {} id&&candidate.FindColony(id)?.EmpireId==owner;
        void Start(EmpireId owner,OfficerRecord officer,ColonyId? colony,int delay,string reason)
        {
            officer.PostedColony=null;officer.Flagship=null;officer.Request=null;officer.RecoveryColony=colony;
            officer.Status=colony is null?OfficerStatus.Missing:OfficerStatus.Recovering;
            officer.RecoveryStartedTurn=colony is null?null:candidate.Turn;officer.AvailableAtTurn=colony is null?null:checked(candidate.Turn+delay);
            context.Event(candidate,owner,officer.Status==OfficerStatus.Missing?"officer_missing":"officer_recovering",reason,officer:officer.Id,importance:FleetImportance.Attention);
        }
        // Only deadlines present at the snapshot may mature here. Newly started losses/waits cannot tick.
        foreach(var before in context.Officers.OrderBy(o=>o.Owner).ThenBy(o=>o.Officer.Id))
        {
            var officer=candidate.FindEmpire(before.Owner)!.Fleets!.Officers.Single(o=>o.Id==before.Officer.Id);
            if(officer.Status!=before.Officer.Status)continue;
            if(before.Officer.Status==OfficerStatus.Commissioning&&before.Officer.AvailableAtTurn<=candidate.Turn)
            {
                officer.Status=OfficerStatus.Posted;officer.AvailableAtTurn=null;context.Event(candidate,before.Owner,"officer_available","Commissioning completed; available at the recorded posting.",officer:officer.Id);
            }
            else if(before.Officer.Status==OfficerStatus.Recovering)
            {
                if(!Own(before.Owner,officer.RecoveryColony))
                {
                    Start(before.Owner,officer,null,d.MortalRecoveryTurns,"Recovery destination became unavailable; Missing until a later boundary with an owned colony.");continue;
                }
                if(before.Officer.AvailableAtTurn>candidate.Turn)continue;
                officer.Status=OfficerStatus.Posted;officer.PostedColony=officer.RecoveryColony;officer.RecoveryColony=null;officer.AvailableAtTurn=null;officer.Career=officer.Career with{Recoveries=checked(officer.Career.Recoveries+1)};
                context.Event(candidate,before.Owner,"officer_recovered",officer.Embodiment==CommandEmbodiment.ColonyBody?"Command link re-established at the source body; appointment available.":"Recovery completed at the selected owned colony; officer available.",officer:officer.Id);
            }
            else if(before.Officer.Status==OfficerStatus.Missing&&Destination(before.Owner) is {} colony)
                Start(before.Owner,officer,colony,d.MortalRecoveryTurns,"Owned recovery colony available; the full recovery delay starts now without another roll.");
        }
        foreach(var loss in context.Losses.OrderBy(l=>l.Appointment.Owner).ThenBy(l=>l.Appointment.Officer.Id))
        {
            var appointment=loss.Appointment;var owner=appointment.Owner;var f=candidate.FindEmpire(owner)!.Fleets!;var officer=f.Officers.Single(o=>o.Id==appointment.Officer.Id);
            if(officer.Status!=OfficerStatus.Commanding)continue; // An unavailable colony-body appointment may have retired in health normalization.
            var before=appointment.Officer;officer.PostedColony=null;officer.Flagship=null;officer.Request=null;officer.RecoveryColony=null;officer.AvailableAtTurn=null;officer.RecoveryStartedTurn=null;
            if(before.Embodiment==CommandEmbodiment.ColonyBody)
            {
                if(!Own(owner,before.SourceBody))throw new InvalidOperationException("Source-body fate must be resolved by colony-loss normalization.");
                int delay=Math.Max(1,(before.Doctrine==OfficerDoctrine.LastWatch?d.LastWatchLinkTurns:d.LinkRecoveryTurns)-(appointment.RecoveryEquipment?d.ResearchedLinkReduction:0));
                officer.RecoveryPercentAtLoss=100;Start(owner,officer,before.SourceBody,delay,$"Flagship lost; command link severed. Re-establishing the appointment requires {delay} full subsequent resolution(s); source-body record and XP retained.");continue;
            }
            if(before.Doctrine==OfficerDoctrine.LastWatch)
            {officer.RecoveryPercentAtLoss=0;Die("Last watch: officer permanently lost with the designated flagship; no recovery roll.");continue;}
            int percent=appointment.RecoveryEquipment?d.ResearchedRecoveryPercent:d.MortalRecoveryPercent;officer.RecoveryPercentAtLoss=percent;
            f.RecoveryRollCount=checked(f.RecoveryRollCount+1);int roll=f.RecoveryRandom!.NextInt(100);
            if(roll>=percent){Die($"Designated flagship destroyed; officer recovery failed ({percent}% equipment). Service record is final.");continue;}
            var destination=Destination(owner);Start(owner,officer,destination,d.MortalRecoveryTurns,destination is null?"Recovery succeeded, but no owned colony is available; Missing without a running deadline.":$"Recovery succeeded ({percent}% snapshot equipment); available after {d.MortalRecoveryTurns} full subsequent resolutions.");
            void Die(string reason){officer.Status=OfficerStatus.Dead;officer.FinalSummary=reason;context.Event(candidate,owner,"officer_dead",reason,officer:officer.Id,vessel:loss.Vessel,importance:FleetImportance.Attention);}
        }
    }
}
