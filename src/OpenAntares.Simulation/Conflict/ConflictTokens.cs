using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public static class ConflictTokens
{
    public static string For(ContentSet content,EmpireView view,ConflictAction action)
    {
        var own=view.Conflict;
        ConflictEncounterId? encounterId=action switch{ConflictAction.ChangeOrders a=>a.Encounter,ConflictAction.Withdraw a=>a.Encounter,ConflictAction.Respond a=>a.Encounter,_=>null};
        var encounter=own?.Encounters.SingleOrDefault(e=>e.Id==encounterId);
        var target=action is ConflictAction.Attack attack?own?.Military.SingleOrDefault(m=>m.Vessel==attack.Target):null;
        VesselId? anchor=action is ConflictAction.ConfigureDefense defense?defense.Anchor:null;
        FleetId? fleet=action switch{ConflictAction.Attack a=>a.Fleet,ConflictAction.Blockade a=>a.Fleet,ConflictAction.LiftBlockade a=>a.Fleet,ConflictAction.CancelAttack a=>a.Fleet,_=>encounter?.OwnFleet??own?.Ships.SingleOrDefault(s=>s.Id==anchor)?.Fleet};
        EmpireId? other=action switch
        {
            ConflictAction.DeclareWar a=>a.Other,ConflictAction.RevokeDeclaration a=>a.Other,ConflictAction.ProposePeace a=>a.Other,
            ConflictAction.AnswerPeace a=>a.Other,ConflictAction.WithdrawPeace a=>a.Other,ConflictAction.RevokePeaceAnswer a=>a.Other,
            ConflictAction.Blockade a=>a.Other,_=>target?.Owner??encounter?.Other
        };
        var orders=action switch{ConflictAction.Attack a=>a.Orders,ConflictAction.ConfigureDefense a=>a.Orders,ConflictAction.ChangeOrders a=>a.Orders,_=>encounter?.OwnOrders};
        var port=view.Colonies.SingleOrDefault(c=>c.Id==orders?.ReturnColony);
        var ships=own?.Ships.Where(s=>fleet is {} f?s.Fleet==f:anchor is {} v&&s.Id==v).OrderBy(s=>s.Id).ToArray();
        var facts=new
        {
            view.Turn,view.Viewer,view.RulesVersion,Ready=view.Research.Ready,
            Kind=action.GetType().Name,Action=JsonSerializer.Serialize(action,action.GetType()),
            Relation=own?.Relations.SingleOrDefault(r=>r.Other==other),
            Ships=ships,Target=target,OpposingGroup=target is null?null:own?.Military.Where(m=>m.Owner==target.Owner&&m.Star==target.Star&&(target.Fleet is {} observedFleet?m.Fleet==observedFleet:m.Vessel==target.Vessel)).OrderBy(m=>m.Vessel).ToArray(),Encounter=encounter,
            Group=fleet is {} id?view.Fleets?.Fleets.SingleOrDefault(f=>f.Id==id):null,
            PendingAttack=own?.OwnAttacks.Where(a=>a.Fleet==fleet).ToArray(),
            OwnBlockade=own?.Blockades.SingleOrDefault(b=>b.Fleet==fleet),
            Port=port is null?null:new{port.Id,port.StarId,port.EmpireId,port.Crisis}
        };
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(facts)))).ToLowerInvariant();
    }
}
