using OpenAntares.Simulation.State;
using System.Linq;
using System.Collections.Immutable;
namespace OpenAntares.Simulation.ShipDesigns;
public static class ShipDesignReports
{
    internal static ImmutableArray<ShipDesignEvent> Finalize(GameState candidate)
    {
        var events=ImmutableArray.CreateBuilder<ShipDesignEvent>();
        foreach(var owner in candidate.Empires.OrderBy(e=>e.Id))
        {
            var data=owner.ShipDesigns!;var completed=data.PendingEvents.DistinctBy(e=>e.Id).OrderBy(e=>e.Id).TakeLast(20).ToArray();events.AddRange(completed);
            data.RecentEvents=data.RecentEvents.Concat(completed).DistinctBy(e=>e.Id).OrderBy(e=>e.Id).TakeLast(20).ToList();data.PendingEvents.Clear();
        }
        return events.ToImmutable();
    }
    internal static ShipDesignEvent Record(GameState state,EmpireId owner,string kind,string reason,VesselId vessel,ColonyId port,EquipmentFact before,EquipmentFact after,long paid)
    {
        var data=state.FindEmpire(owner)!.ShipDesigns!;long id=data.NextEventId;data.NextEventId=checked(id+1);
        var entry=new ShipDesignEvent(owner,id,state.Turn,kind,reason,vessel,port,before,after,paid);data.PendingEvents.Add(entry);while(data.PendingEvents.Count>20)data.PendingEvents.RemoveAt(0);return entry;
    }
}
