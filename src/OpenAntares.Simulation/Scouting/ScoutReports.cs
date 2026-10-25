using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
namespace OpenAntares.Simulation.Scouting;
public static class ScoutReports
{
    internal static int Slot(string kind)=>kind switch{"constructed"=>0,"posture" or "move_ordered" or "move_cancelled" or "risk_ordered" or "risk_cleared" or "approach_ordered" or "retreat_planned" or "acknowledgement" or "founded"=>1,"warning"=>2,"study"=>3,"departure" or "escape" or "travelling"=>4,"arrived"=>5,"loss"=>6,"wait"=>7,_=>-1};
    private static string Signature(ScoutOrderEvent entry)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(entry.Star+":"+entry.Reason)));
    internal static bool NeedsPreparationRecord(GameState state,EmpireId owner,ScoutPlan plan)
    {
        if(plan.Outcomes.Any(e=>e.Kind!="wait"))return true;
        foreach(var v in state.Vessels.Where(v=>v.EmpireId==owner&&v.Scout is not null))
        {
            var wait=plan.Outcomes.FirstOrDefault(e=>e.Vessel==v.Id&&e.Kind=="wait");if(v.Scout!.LastWaitSignature!=(wait is null?null:Signature(wait)))return true;
        }
        return false;
    }
    public static void RecordPreparation(GameState state,EmpireId owner,ScoutPlan plan)
    {
        foreach(var v in state.Vessels.Where(v=>v.EmpireId==owner&&v.Scout is not null))
        {
            var wait=plan.Outcomes.FirstOrDefault(e=>e.Vessel==v.Id&&e.Kind=="wait");string? signature=wait is null?null:Signature(wait);
            if(wait is not null&&v.Scout!.LastWaitSignature!=signature)Record(state,wait);
            v.Scout!.LastWaitSignature=signature;
        }
        foreach(var entry in plan.Outcomes.Where(e=>e.Kind!="wait"))Record(state,entry);
    }
    internal static void RecordCommand(GameState before,GameState after,Command command)
    {
        VesselId? id=command switch{SetScoutPostureCommand c=>c.Vessel,SetMoveOrderCommand c=>c.Vessel,SetAmebaApproachOrderCommand c=>c.Vessel,SetAmebaRiskOrderCommand c=>c.Vessel,SetScoutRetreatOrderCommand c=>c.Vessel,_=>null};
        if(id is null||after.FindVessel(id.Value) is not {} v)return;
        string kind=command switch{SetScoutPostureCommand=>"posture",SetAmebaApproachOrderCommand=>"approach_ordered",SetAmebaRiskOrderCommand{Action:null}=>"risk_cleared",SetAmebaRiskOrderCommand=>"risk_ordered",_=>v.PendingMove is null?"move_cancelled":"move_ordered"};
        string reason=command switch{SetScoutPostureCommand p=>"Neutral posture: "+p.Posture+". Ameba survival remains enabled.",SetAmebaRiskOrderCommand r=>r.Action is null?"One-boundary risk intent cleared.":"One-boundary risk intent: "+r.Action+".",_=>v.PendingMove is null?"Pending route cleared.":"Ordinary route ordered; travel starts at resolution."};
        Record(after,new(v.EmpireId,after.Turn,v.Id,v.RoleId,v.CarriedPopulation,v.StationaryStarId??v.Transit!.Origin,kind,reason,v.PendingMove,EncounterNumber:v.AmebaEncounter?.Number,Revision:v.AmebaEncounter?.Revision));
    }
    private static void Record(GameState state,ScoutOrderEvent entry)
    {
        var e=state.FindEmpire(entry.Owner)!.Scouting!;
        var entries=e.PendingDigest?.Entries??ImmutableArray<ScoutOrderEvent>.Empty;
        e.PendingDigest=new(entry.Turn,Coalesce(entries.Append(entry)));
    }
    private static ImmutableArray<ScoutOrderEvent> Coalesce(IEnumerable<ScoutOrderEvent> entries)=>entries.GroupBy(e=>(e.Vessel,Slot(e.Kind))).Select(g=>g.Last()).OrderBy(e=>e.Vessel).ThenBy(e=>Slot(e.Kind)).ToImmutableArray();
    internal static void RecordMovement(GameState snapshot,GameState candidate,AmebaResolutionContext context,IEnumerable<ExpansionEvent> events)
    {
        foreach(var actual in events)
        {
            string? kind=actual.Kind switch{"arrived"=>"arrived","vessel_completed"=>"constructed","colony_founded"=>"founded",_=>null};
            if(kind is null||actual.Vessel is not {} id)continue;
            var v=candidate.FindVessel(id)??snapshot.FindVessel(id);if(v is null)throw new InvalidOperationException("Missing original vessel facts.");
            context.Events.Add(new(v.EmpireId,context.ToTurn,id,v.RoleId,v.CarriedPopulation,actual.Star??throw new InvalidOperationException("Missing actual system."),kind,kind switch{"arrived"=>"Arrived by ordinary travel.","constructed"=>"Paid vessel completed; standing orders first apply next planning period.",_=>"Expedition founded its colony."}));
        }
        foreach(var v in candidate.Vessels.Where(v=>v.Transit is not null&&!context.DepartingVessels.Contains(v.Id)))context.Events.Add(new(v.EmpireId,context.ToTurn,v.Id,v.RoleId,v.CarriedPopulation,v.Transit!.Origin,"travelling","Committed travel continues.",v.Transit.Destination));
    }
    public static void Finalize(GameState snapshot,GameState candidate,AmebaResolutionContext context)
    {
        foreach(var owner in candidate.Empires.OrderBy(e=>e.Id))
        {
            var e=owner.Scouting!;var old=e.PendingDigest;
            if(old is not null)e.CompletedDigests.Add(old);
            e.CompletedDigests=e.CompletedDigests.Where(d=>d.Turn>=Math.Max(0,candidate.Turn-10)&&d.Turn<candidate.Turn).GroupBy(d=>d.Turn).Select(g=>g.Last()).OrderBy(d=>d.Turn).TakeLast(10).ToList();
            e.PendingDigest=new(candidate.Turn,Coalesce(context.Events.Where(entry=>entry.Owner==owner.Id)));
            var references=e.CompletedDigests.Append(e.PendingDigest).SelectMany(d=>d.Entries).Select(entry=>entry.Vessel).ToHashSet();
            foreach(var id in references.Where(id=>candidate.FindVessel(id) is null).OrderBy(id=>id))
            {
                if(e.RetiredVessels.Any(r=>r.Id==id))continue;
                var v=snapshot.FindVessel(id)??throw new InvalidOperationException("Missing immutable retired vessel facts.");e.RetiredVessels.Add(new(id,owner.Id,v.RoleId,v.CarriedPopulation,v.StationaryStarId??v.Transit!.Origin,candidate.Turn));
            }
            e.RetiredVessels=e.RetiredVessels.Where(r=>references.Contains(r.Id)).OrderBy(r=>r.Id).ToList();
            e.Receipts.RemoveAll(r=>candidate.FindVessel(r.Token.Vessel)?.AmebaEncounter is not {} a||a.Number!=r.Token.EncounterNumber||a.Revision!=r.Token.Revision);
        }
        var ordered=context.Events.OrderBy(e=>e.Owner).ThenBy(e=>e.Vessel).ThenBy(e=>Slot(e.Kind)).ToArray();context.Events.Clear();context.Events.AddRange(ordered);
    }
}
