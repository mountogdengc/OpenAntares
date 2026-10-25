using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Scouting;
public static class AmebaResolver
{
    public static void ResolveExisting(ContentSet c,GameState snapshot,GameState candidate,AmebaResolutionContext context)
    {
        var d=c.SpaceAmeba!;
        foreach(var id in context.EncounterInputs)
        {
            var before=snapshot.FindVessel(id)!;var v=candidate.FindVessel(id)??throw new InvalidOperationException("Encounter vessel disappeared before resolution.");var a=before.AmebaEncounter!;var owner=candidate.FindEmpire(v.EmpireId)!.Scouting!;
            if(a.RiskOrder?.Action==AmebaRiskAction.Remain){Consume("Explicitly remained at the ameba; vessel and cargo consumed.");continue;}
            if(a.RiskOrder?.Action==AmebaRiskAction.Study)
            {
                if(before.Scout is null||a.Stage!=AmebaStage.Warning||owner.SuccessfulStudies.Any(claim=>claim.HabitatId==a.HabitatId))throw new InvalidOperationException("Invalid reserved study.");
                int percent=AmebaOdds.Study(d,c.Vessels[v.RoleId].Speed);
                if(!Roll(percent)){Consume("Study failed; vessel consumed.",percent);continue;}
                v.AmebaEncounter!.Stage=AmebaStage.Exposed;v.AmebaEncounter.RiskOrder=null;v.AmebaEncounter.Revision=Next(v.AmebaEncounter.Revision);
                owner.SuccessfulStudies.Add(new(a.HabitatId,context.ToTurn,v.Id));owner.SuccessfulStudies.Sort((x,y)=>x.HabitatId.CompareTo(y.HabitatId));owner.Receipts.RemoveAll(r=>r.Token.Vessel==v.Id);
                context.ResearchAwards.TryGetValue(v.EmpireId,out long previous);context.ResearchAwards[v.EmpireId]=checked(previous+d.ResearchReward);
                Event("study","Study survived. Research is deposited after this interval's research and cure; scout is now Exposed.",percent,d.ResearchReward);continue;
            }
            if(before.PendingMove is null)throw new InvalidOperationException("Encounter has no actual response.");
            int escape=a.Stage==AmebaStage.Warning?100:AmebaOdds.LateEscape(d,c.Vessels[v.RoleId].Speed);
            if(a.Stage==AmebaStage.Exposed&&!Roll(escape)){Consume("Late escape failed; vessel consumed.",escape);continue;}
            v.AmebaEncounter=null;owner.Receipts.RemoveAll(r=>r.Token.Vessel==v.Id);context.DepartingVessels.Add(id);
            Event(a.Stage==AmebaStage.Warning?"departure":"escape",a.Stage==AmebaStage.Warning?"Departed during the safe warning opportunity.":"Survived the late escape.",escape);

            bool Roll(int percent){owner.RiskRollCount=Next(owner.RiskRollCount);return AmebaOdds.Succeeds(percent,owner.RiskRandom!.NextInt(100));}
            void Event(string kind,string reason,int? percent=null,long award=0)=>context.Events.Add(new(before.EmpireId,context.ToTurn,before.Id,before.RoleId,before.CarriedPopulation,before.StationaryStarId!.Value,kind,reason,before.PendingMove,percent,award,a.Number,a.Revision));
            void Consume(string reason,int? percent=null)
            {
                Event("loss",reason,percent);owner.Receipts.RemoveAll(r=>r.Token.Vessel==id);owner.RetiredVessels.Add(new(id,before.EmpireId,before.RoleId,before.CarriedPopulation,before.StationaryStarId!.Value,context.ToTurn));owner.RetiredVessels.Sort((x,y)=>x.Id.CompareTo(y.Id));candidate.Vessels.Remove(v);context.ConsumedVessels.Add(id);
            }
        }
    }
    public static void ObserveArrivals(ContentSet c,GameState candidate,AmebaResolutionContext context)
    {
        if(candidate.Scouting is not{Enabled:true,Habitat:{} habitat})return;
        foreach(var v in candidate.Vessels.Where(v=>v.StationaryStarId==habitat.Star&&v.AmebaEncounter is null).OrderBy(v=>v.EmpireId).ThenBy(v=>v.Id))
        {
            var e=candidate.FindEmpire(v.EmpireId)!.Scouting!;long number=e.NextEncounterNumber;e.NextEncounterNumber=Next(number);
            v.AmebaEncounter=new(){Number=number,HabitatId=habitat.Id,ArrivalTurn=context.ToTurn,Stage=AmebaStage.Warning};
            if(!e.KnownHabitats.Any(h=>h.HabitatId==habitat.Id))e.KnownHabitats.Add(new(habitat.Id,habitat.Star,context.ToTurn));
            e.KnownHabitats.Sort((x,y)=>x.HabitatId.CompareTo(y.HabitatId));
            context.Events.Add(new(v.EmpireId,context.ToTurn,v.Id,v.RoleId,v.CarriedPopulation,habitat.Star,"warning","Space ameba discovered. Departure during the next planning opportunity is certain and uses normal travel time.",Percent:100,EncounterNumber:number,Revision:1));
        }
        foreach(var owner in candidate.Empires.OrderBy(e=>e.Id))
        {
            if(!Knowledge.ObservationUpdater.IsCurrent(candidate,owner.Id,habitat.Star))continue;
            var knowledge=owner.Scouting!.KnownHabitats;int index=knowledge.FindIndex(h=>h.HabitatId==habitat.Id);
            if(index<0)throw new InvalidOperationException("Current habitat presence lacks its own discovery.");
            knowledge[index]=knowledge[index] with{ObservedTurn=context.ToTurn};
        }
    }
    public static void DepositResearch(ContentSet c,GameState candidate,AmebaResolutionContext context)
    {
        foreach(var pair in context.ResearchAwards.OrderBy(x=>x.Key)){var owner=candidate.FindEmpire(pair.Key)!;owner.ResearchReserve=checked(owner.ResearchReserve+pair.Value);}
    }
    public static void ValidateDepartures(GameState snapshot,GameState candidate,AmebaResolutionContext context)
    {
        foreach(var id in context.DepartingVessels)
        {
            var v=candidate.FindVessel(id)??throw new InvalidOperationException("Escaping vessel disappeared in transport.");
            if(v.StationaryStarId==snapshot.FindVessel(id)!.StationaryStarId)throw new InvalidOperationException("Expected departure was blocked in transport.");
        }
    }
    private static long Next(long current){long next=checked(current+1);if(next==long.MaxValue)throw new OverflowException("Encounter counter exhausted.");return next;}
}
