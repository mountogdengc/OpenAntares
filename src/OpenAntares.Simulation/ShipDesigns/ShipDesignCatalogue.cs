using OpenAntares.Simulation.State;
using System.Linq;
using System.Globalization;
namespace OpenAntares.Simulation.ShipDesigns;
public static class ShipDesignCatalogue
{
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
