using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.ShipDesigns;
public static class ShipDesignReports
{
    internal static ShipDesignEvent Record(GameState state,EmpireId owner,string kind,string reason,VesselId vessel,ColonyId port,EquipmentFact before,EquipmentFact after,long paid)
    {
        var data=state.FindEmpire(owner)!.ShipDesigns!;long id=data.NextEventId;data.NextEventId=checked(id+1);
        var entry=new ShipDesignEvent(owner,id,state.Turn,kind,reason,vessel,port,before,after,paid);data.PendingEvents.Add(entry);while(data.PendingEvents.Count>20)data.PendingEvents.RemoveAt(0);return entry;
    }
}
