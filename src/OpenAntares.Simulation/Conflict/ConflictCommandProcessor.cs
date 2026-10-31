using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public static class ConflictCommandProcessor
{
    public static CommandOutcome Execute(ContentSet content,GameState state,ConflictCommand command)
    {
        CommandOutcome Reject(string code="conflict_unavailable")=>CommandOutcome.Reject(state,code,"The conflict action is unavailable or stale from your current view.");
        if(!Rules.HasConflict(state.RulesVersion)||content.Conflict is null||state.Conflict is null||command.Action is null||command.ExpectedTurn!=state.Turn||state.FindEmpire(command.Issuer) is not {} issuer)return Reject();
        try
        {
            var view=EmpireViewProjector.Project(content,state,command.Issuer);var preview=ConflictQueries.Preview(content,view,command.Action);
            if(!StringComparer.Ordinal.Equals(command.Token,preview.Token))return Reject("conflict_stale");
            if(!preview.Allowed)return Reject();
            EmpireId other=command.Action switch{ConflictAction.DeclareWar a=>a.Other,ConflictAction.RevokeDeclaration a=>a.Other,_=>default};
            var original=state.Conflict.Pairs.SingleOrDefault(p=>p.Pair==DiplomacyPair.For(command.Issuer,other));if(original is null)return Reject();
            bool queued=original.Declarations.Any(i=>i.Owner==command.Issuer);
            if(command.Action is ConflictAction.DeclareWar&&queued||command.Action is ConflictAction.RevokeDeclaration&&!queued)return CommandOutcome.NoChange(state);
            var candidate=state.Clone();var pair=candidate.Conflict!.Pairs.Single(p=>p.Pair==original.Pair);
            switch(command.Action)
            {
                case ConflictAction.DeclareWar:
                    pair.Declarations.Add(new(command.Issuer,state.Turn));pair.Declarations.Sort((a,b)=>a.Owner.CompareTo(b.Owner));break;
                case ConflictAction.RevokeDeclaration:
                    pair.Declarations.RemoveAll(i=>i.Owner==command.Issuer);break;
                default:return Reject();
            }
            pair.Parties.Single(p=>p.Owner==command.Issuer).PrivateRevision=checked(pair.Parties.Single(p=>p.Owner==command.Issuer).PrivateRevision+1);
            var owner=candidate.Conflict.Owners.Single(o=>o.Owner==command.Issuer);owner.Revision=checked(owner.Revision+1);candidate.FindEmpire(command.Issuer)!.Ready=false;
            if(GameStateValidator.Validate(content,candidate,allowAllHumansReady:issuer.Controller==ControllerKind.Ai).Count!=0)return Reject("conflict_invalid");
            return new(candidate,null,true,null);
        }
        catch(Exception e)when(e is OverflowException or InvalidOperationException or ArgumentException){return Reject("conflict_invalid");}
    }
}
