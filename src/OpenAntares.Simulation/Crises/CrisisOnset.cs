using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Crises;
public static class CrisisOnset
{
    public static void Apply(ContentSet content,GameState candidate,CrisisResolutionContext context)
    {
        if(!Rules.HasCrises(candidate.RulesVersion)||candidate.Crisis is not{Enabled:true})return;
        var d=content.Plagues.Values.Single();
        if(context.ToTurn<d.FirstCheckTurn||(context.ToTurn-d.FirstCheckTurn)%d.CheckInterval!=0)return;
        foreach(var owner in candidate.Empires.OrderBy(e=>e.Id))
        {
            var e=owner.Crisis!;
            if(e.Episode is not null||e.NextEligibleTurn>context.ToTurn)continue;
            var eligible=candidate.Colonies.Where(c=>c.EmpireId==owner.Id&&c.Crisis!.Case is null&&!(c.Crisis.ImmunityFamilyId==d.Id&&context.ToTurn<c.Crisis.ImmuneUntilTurn)).OrderBy(c=>c.Id).ToArray();
            if(eligible.Length==0)continue;
            if(e.Random.NextInt(100)>=d.CheckChancePercent)continue;
            var source=eligible[e.Random.NextInt(eligible.Length)];long id=e.NextEpisodeNumber;
            if(id>=long.MaxValue-1)throw new OverflowException("Episode identifiers exhausted.");
            long cost=checked((long)((Int128)d.CureBase+(Int128)d.CurePerPopulation*source.Population));var location=CrisisQueries.Location(candidate,source);
            e.NextEpisodeNumber=checked(id+1);e.Episode=new(){Id=id,FamilyId=d.Id,Source=location,SourcePopulation=source.Population,CureCost=cost};
            source.Crisis!.PolicyPaused=false;source.Crisis.Case=new(){EpisodeId=id,Stage=CrisisStage.Warning,GraceRemaining=d.GraceTurns,AttentionRevision=1,Origin=location};
            context.Events.Add(new(new(owner.Id,id,source.Id),context.ToTurn,location,"exposure",0,"New plague warning; "+d.GraceTurns.ToString(System.Globalization.CultureInfo.InvariantCulture)+" safe warning intervals."){Material=true,CureCost=cost});
        }
    }
}
