using System;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Buffers.Binary;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static class DiplomacyTokens
{
    public static long PrivateRevision(ContentSet content,GameState state,EmpireId owner,EmpireId other)
    {
        var p=state.Diplomacy!.Pairs.Single(p=>p.Pair==DiplomacyPair.For(owner,other));var own=EmpireViewProjector.ProjectBase(content,state,owner);
        var material=new{state.Turn,Owner=owner,Other=other,Revision=p.Parties.Single(x=>x.Owner==owner).PrivateRevision,Definition=content.Diplomacy,Colonies=own.Colonies.OrderBy(c=>c.Id).ToArray(),Known=own.Research.KnownTechnologyIds,Refits=own.ShipDesigns?.Refits,Requests=own.Fleets?.Requests.Where(r=>FleetQueries.IsActive(r.Status)).OrderBy(r=>r.Id).ToArray()};
        ulong value=BinaryPrimitives.ReadUInt64LittleEndian(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(material)))&0x7FFFFFFFFFFFFFFFUL;return value==0?1:(long)value;
    }
}
