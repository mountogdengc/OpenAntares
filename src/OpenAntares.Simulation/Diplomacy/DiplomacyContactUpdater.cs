using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
namespace OpenAntares.Simulation.Diplomacy;
public static class DiplomacyContactUpdater
{
    public static ImmutableArray<DiplomacyEvent> Refresh(GameState snapshot,GameState candidate,ImmutableArray<EncounterEvent> observed)
    {
        var events=ImmutableArray.CreateBuilder<DiplomacyEvent>();if(!Rules.HasDiplomacy(candidate.RulesVersion))return events.ToImmutable();
        foreach(var viewer in candidate.Empires.OrderBy(e=>e.Id))
        foreach(var contact in viewer.Contacts.ToArray())
        {
            var other=candidate.FindEmpire(contact.OtherEmpireId)!;
            if(!other.Contacts.Any(c=>c.OtherEmpireId==viewer.Id)){other.Contacts.Add(new(viewer.Id,viewer.SpeciesId!,candidate.Turn));other.Contacts.Sort((a,b)=>a.OtherEmpireId.CompareTo(b.OtherEmpireId));}
            DiplomacyInitializer.EnsurePair(candidate,viewer.Id,other.Id);
        }
        foreach(var viewer in candidate.Empires.OrderBy(e=>e.Id))
        foreach(var contact in viewer.Contacts.OrderBy(c=>c.OtherEmpireId))
        {
            if(snapshot.FindEmpire(viewer.Id)!.Contacts.Any(c=>c.OtherEmpireId==contact.OtherEmpireId))continue;
            var ownObservation=observed.FirstOrDefault(e=>e.Kind=="first_contact"&&e.Viewer==viewer.Id&&e.OtherEmpireId==contact.OtherEmpireId);
            events.Add(new(viewer.Id,contact.OtherEmpireId,candidate.Turn,"first_contact",ownObservation?.StarId,null,"A mutual communications channel is established."));
        }
        return events.ToImmutable();
    }
}
