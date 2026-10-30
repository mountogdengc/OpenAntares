using OpenAntares.Simulation.State;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Immutable;
namespace OpenAntares.Simulation.ShipDesigns;
public static class ShipDesignCatalogue
{
    internal static ImmutableDictionary<DesignId,DesignReferences> Index(GameState state,EmpireId owner)
    {
        var data=state.FindEmpire(owner)?.ShipDesigns;if(data is null)return ImmutableDictionary<DesignId,DesignReferences>.Empty;
        var rows=data.Designs.ToDictionary(d=>d.Id,_=>new int[5]);
        void Add(DesignId? id,int kind){if(id is {} key&&rows.TryGetValue(key,out var counts))counts[kind]++;}
        void Target(string kind,string? key,int category){if(kind==ProductionKinds.ShipDesign&&ShipDesignTargets.TryId(key,out int id))Add(new(id),category);}
        foreach(var ship in state.Vessels.Where(v=>v.EmpireId==owner))Add(ship.Design,0);
        foreach(var colony in state.Colonies.Where(c=>c.EmpireId==owner)){Target(colony.ProjectKind,colony.ProjectId,1);if(colony.Development is {} d)foreach(var entry in d.Queue)Target(entry.Target.Kind,entry.Target.ContentId,2);}
        if(state.FindEmpire(owner)?.Fleets is {} fleets)foreach(var request in fleets.Requests)foreach(var slot in request.Slots)Add(slot.Design,3);
        foreach(var job in data.Refits){Add(job.SourceDesign,4);if(job.TargetDesign!=job.SourceDesign)Add(job.TargetDesign,4);}
        return rows.ToImmutableDictionary(r=>r.Key,r=>new DesignReferences(r.Value[0],r.Value[1],r.Value[2],r.Value[3],r.Value[4]));
    }
    public static DesignReferences References(GameState state,EmpireId owner,DesignId id)
    {
        if(state.FindEmpire(owner)?.ShipDesigns is not {} data||!data.Designs.Any(d=>d.Id==id))return new(0,0,0,0,0);
        bool Target(string kind,string? key)=>kind=="ship_design"&&key==id.Value.ToString(CultureInfo.InvariantCulture);
        var colonies=state.Colonies.Where(c=>c.EmpireId==owner).ToArray();
        return new(state.Vessels.Count(v=>v.EmpireId==owner&&v.Design==id),
            colonies.Count(c=>Target(c.ProjectKind,c.ProjectId)),
            colonies.Sum(c=>c.Development?.Queue.Count(q=>Target(q.Target.Kind,q.Target.ContentId))??0),
            state.FindEmpire(owner)!.Fleets?.Requests.Sum(r=>r.Slots.Count(s=>s.Design==id))??0,
            data.Refits.Count(j=>j.SourceDesign==id||j.TargetDesign==id));
    }
}
