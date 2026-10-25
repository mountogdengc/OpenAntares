using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Crises;
public sealed record CrisisPreparationResult(GameState State,bool Succeeded);
public static class CrisisTurnCoordinator
{
    public static CrisisPreparationResult Prepare(ContentSet content,GameState state,EmpireId owner)=>Prepare(content,state,owner,CrisisPlanner.Plan);
    public static CrisisPreparationResult PrepareAi(ContentSet content,GameState state)=>PrepareAi(content,state,CrisisPlanner.Plan);
    internal static CrisisPreparationResult Prepare(ContentSet content,GameState state,EmpireId owner,CrisisPlanningPolicy planner)
    {
        if(!Rules.HasCrises(state.RulesVersion)||state.Crisis is not{Enabled:true})return new(state,true);
        if(state.FindEmpire(owner)?.Controller!=ControllerKind.Human)return new(state,false);
        return PrepareOwner(content,state,owner,planner,false);
    }
    internal static CrisisPreparationResult PrepareAi(ContentSet content,GameState state,CrisisPlanningPolicy planner)
    {
        if(!Rules.HasCrises(state.RulesVersion)||state.Crisis is not{Enabled:true})return new(state,true);
        var working=state;
        foreach(var owner in state.Empires.Where(e=>e.Controller==ControllerKind.Ai).OrderBy(e=>e.Id))
        {
            var result=PrepareOwner(content,working,owner.Id,planner,true);
            if(!result.Succeeded)return new(state,false);working=result.State;
        }
        return new(working,true);
    }
    private static CrisisPreparationResult PrepareOwner(ContentSet content,GameState state,EmpireId owner,CrisisPlanningPolicy planner,bool ai)
    {
        if(GameStateValidator.Validate(content,state,allowAllHumansReady:true).Count!=0)return new(state,false);
        var working=state.Clone();long accepted=0;bool failed=false,changed=false;
        long budget=(long)state.Colonies.Count(c=>c.EmpireId==owner)+state.Vessels.Count(v=>v.EmpireId==owner)+4;
        try
        {
            var result=planner(()=>CrisisPlanningQueries.Create(content,working,owner),command=>
            {
                if(failed||accepted>=budget||command.Issuer!=owner||command.ExpectedTurn!=state.Turn||command is not(SetQuarantineCommand or SetCureShareCommand or DecontaminateVesselCommand)){failed=true;return false;}
                var outcome=CommandProcessor.ExecuteCrisisAutomatic(content,working,(CrisisCommand)command);
                if(!outcome.Accepted){failed=true;return false;}
                accepted++;changed|=outcome.Changed;working=outcome.State;return true;
            },ai);
            if(failed||!result.Succeeded||accepted!=result.CommandCount||GameStateValidator.Validate(content,working,allowAllHumansReady:true).Count!=0)return new(state,false);
            return new(changed?working:state,true);
        }
        catch(Exception error) when(error is OverflowException or ArgumentException or InvalidOperationException or KeyNotFoundException){return new(state,false);}
    }
}
