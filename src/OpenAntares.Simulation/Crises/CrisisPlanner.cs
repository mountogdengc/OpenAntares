using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Commands;
namespace OpenAntares.Simulation.Crises;
public static class CrisisPlanner
{
    public static CrisisPlanningResult Plan(Func<ICrisisPlanningContext> current,Func<Command,bool> submit,bool ai)
    {
        int count=0;
        try
        {
            var initial=current();var view=initial.View;
            if(!view.Enabled||!ai&&view.Authority==CrisisAuthority.Manual)return new(true,0);
            long budget=(long)view.ColonyConditions.Length+view.Vessels.Length+4;
            bool Emit(Command command){if(count>=budget||count==int.MaxValue)return false;count++;return submit(command);}
            if(view.ContainmentPreview is{Allowed:true} q&&!Emit(new SetQuarantineCommand(initial.Owner,initial.Turn,q)))return new(false,count);
            var fresh=current();view=fresh.View;
            if(view.Episode is {} episode&&(ai||view.Authority==CrisisAuthority.ContainAndResearch&&!view.ResearchPolicyPaused&&!episode.ResearchOverride))
            {
                int share=ai?50:view.AuthorityResearchShare;
                if(episode.ResearchShare!=share&&!Emit(new SetCureShareCommand(fresh.Owner,fresh.Turn,episode.Id,share)))return new(false,count);
            }
            foreach(var id in current().View.Vessels.OrderBy(v=>v.Id).Select(v=>v.Id).ToArray())
            {
                fresh=current();var vessel=fresh.View.Vessels.FirstOrDefault(v=>v.Id==id);
                if(vessel is not{CanDecontaminate:true})continue;
                var order=new VesselOrderFact(id,vessel.Station,vessel.Transit,vessel.PendingMove,vessel.PendingFounding,vessel.InspectionDueTurn);
                if(!Emit(new DecontaminateVesselCommand(fresh.Owner,fresh.Turn,id,ImmutableArray.Create(order))))return new(false,count);
            }
            return new(true,count);
        }
        catch(Exception error) when(error is OverflowException or ArgumentException or InvalidOperationException or KeyNotFoundException){return new(false,count);}
    }
}
