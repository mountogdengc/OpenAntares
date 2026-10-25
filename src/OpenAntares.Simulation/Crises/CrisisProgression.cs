using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Crises;
public static class CrisisProgression
{
    public static void Apply(ContentSet content,GameState snapshot,GameState candidate,TurnEconomy economy,CrisisResolutionContext context)
    {
        if(!Rules.HasCrises(snapshot.RulesVersion)||snapshot.Crisis is not{Enabled:true})return;
        var d=content.Plagues.Values.Single();
        foreach(var colony in candidate.Colonies.OrderBy(c=>c.Id))
        {
            var health=colony.Crisis!;
            if(health.ImmunityFamilyId is not null&&health.ImmuneUntilTurn<=context.ToTurn){health.ImmunityFamilyId=null;health.ImmuneUntilTurn=0;}
        }
        foreach(var owner in candidate.Empires.OrderBy(e=>e.Id))
        {
            var e=owner.Crisis!;
            if(e.CivicLevel>0&&e.CivicExpiresAt<=context.ToTurn){e.CivicLevel=0;e.CivicExpiresAt=0;}
            if(e.Episode is not {} episode)continue;
            var cure=economy.Empire(owner.Id).Cure??throw new InvalidOperationException("Missing cure forecast.");
            if(cure.Starting!=episode.CureProgress||cure.Cost!=episode.CureCost)throw new InvalidOperationException("Cure forecast differs from snapshot episode.");
            episode.CureProgress=checked(episode.CureProgress+cure.Diverted);
            if(cure.Completes)
            {
                context.CuredEpisodes.Add((owner.Id,episode.Id));
                foreach(var colony in candidate.Colonies.Where(c=>c.EmpireId==owner.Id&&c.Crisis?.Case?.EpisodeId==episode.Id).OrderBy(c=>c.Id))
                {
                    var key=new CrisisKey(owner.Id,episode.Id,colony.Id);var location=CrisisQueries.Location(candidate,colony);
                    ClearCase(colony,d,context.ToTurn);
                    context.Events.Add(new(key,context.ToTurn,location,"case_cured",0,"Case cleared by completed cure."));
                }
                foreach(var vessel in candidate.Vessels.Where(v=>v.EmpireId==owner.Id&&v.Crisis?.CarrierEpisode==episode.Id).OrderBy(v=>v.Id))vessel.Crisis!.CarrierEpisode=null;
                CrisisReports.CloseEpisode(content,candidate,owner.Id,context,cured:true,cure.Diverted);
            }
            else if(cure.Diverted>0)
                context.Events.Add(new(new(owner.Id,episode.Id,episode.Source.Colony),context.ToTurn,episode.Source,"cure_progress",0,"New research invested in the episode cure.",cure.Diverted){CureCost=episode.CureCost,CureProgress=episode.CureProgress});
        }
        foreach(var before in snapshot.Colonies.OrderBy(c=>c.Id))
        {
            if(before.Crisis?.Case is not {} old||candidate.FindColony(before.Id) is not {} colony||colony.Crisis?.Case is not {} illness)continue;
            var key=new CrisisKey(colony.EmpireId,illness.EpisodeId,colony.Id);var location=context.Locations[colony.Id];
            if(old.Stage==CrisisStage.Warning)
            {
                illness.GraceRemaining=checked(old.GraceRemaining-1);
                if(illness.GraceRemaining==0)
                {
                    illness.Stage=CrisisStage.Active;illness.ActiveAge=0;illness.Severity=d.InitialSeverity;
                    Event("active","Plague is now Active; illness penalties begin next interval.",true);
                    if(illness.Severity>=d.MortalityThreshold)Event("critical","Severity reached the mortality threshold; inspect the next interval's loss risk.",true);
                }
                continue;
            }
            illness.ActiveAge=checked(old.ActiveAge+1);
            if(illness.ActiveAge>=d.RecoveryIntervals)
            {
                ClearCase(colony,d,context.ToTurn);Event("recovered","Natural recovery completed before this interval's possible death.",false);continue;
            }
            if(old.Severity>=d.MortalityThreshold)
            {
                int lost=Math.Min(d.MortalityUnits,colony.Population);
                colony.Population=checked(colony.Population-lost);colony.Workforce=CrisisQueries.AfterCasualties(colony.Workforce,lost);
                if(colony.Population==0)
                {
                    CrisisLoss.Remove(candidate,colony.Id,location);Event("colony_lost","Plague casualties exhausted the colony; the planet remains available for later settlement.",true,lost);continue;
                }
                Event("casualty","Plague caused population loss.",true,lost);
            }
            illness.Severity=Math.Min(d.SeverityCap,checked(old.Severity+(before.Crisis!.Quarantined?d.QuarantinedSeverityStep:d.OpenSeverityStep)));
            if(old.Severity<d.MortalityThreshold&&illness.Severity>=d.MortalityThreshold)Event("critical","Severity reached the mortality threshold; inspect the next interval's loss risk.",true);

            void Event(string kind,string reason,bool material,int lost=0)=>context.Events.Add(new(key,context.ToTurn,location,kind,0,reason,PopulationLost:lost){Material=material});
        }
    }
    private static void ClearCase(ColonyState colony,PlagueDefinition definition,int recoveryTurn)
    {
        int expiry=checked(recoveryTurn+definition.ImmunityTurns);var health=colony.Crisis!;long? episode=health.Case?.EpisodeId;
        health.Case=null;health.ImmunityFamilyId=definition.Id;health.ImmuneUntilTurn=expiry;health.PolicyPaused=false;
        if(health.ReleaseEpisode==episode&&episode is not null){health.ReleaseEpisode=null;health.Quarantined=false;}
    }
}
