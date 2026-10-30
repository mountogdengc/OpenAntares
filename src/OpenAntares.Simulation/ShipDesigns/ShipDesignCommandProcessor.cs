using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Knowledge;
using System;
using System.Linq;
namespace OpenAntares.Simulation.ShipDesigns;
public static class ShipDesignCommandProcessor
{
    public static CommandOutcome Execute(ContentSet content,GameState state,ShipDesignCommand command)
    {
        CommandOutcome Reject(string code)=>CommandOutcome.Reject(state,code,"The design action is unavailable, stale, invalid, or still referenced.");
        var owner=state.FindEmpire(command.Issuer);
        if(owner is null)return Reject(RejectionCodes.UnknownEmpire);
        if(command.ExpectedTurn!=state.Turn)return Reject(RejectionCodes.StaleTurn);
        if(!Rules.HasShipDesigns(state.RulesVersion)||content.ShipDesigns is null||owner.ShipDesigns is not {} original||owner.Controller!=ControllerKind.Human||command.Action is null)return Reject("ship_design_unavailable");
        if(command.CatalogueRevision!=original.CatalogueRevision)return Reject("ship_design_stale");
        try
        {
            var candidate=state.Clone();var after=candidate.FindEmpire(owner.Id)!;var data=after.ShipDesigns!;
            if(command.Action is ShipDesignAction.Save save)
            {
                var preview=ShipDesignQueries.PreviewDraft(content,EmpireViewProjector.Project(content,state,owner.Id),save.Name,save.Recipe);
                if(!preview.Allowed)return Reject(preview.RejectionCode??"ship_design_recipe");
                string name=save.Name.Trim();
                if(data.Designs.Any(d=>string.Equals(d.Name,name,StringComparison.OrdinalIgnoreCase)))return CommandOutcome.NoChange(state);
                int number=data.NextDesignId;data.NextDesignId=checked(number+1);data.Designs.Add(new(new(number),name,save.Recipe,false));
            }
            else
            {
                DesignId id=command.Action switch{ShipDesignAction.Archive a=>a.Id,ShipDesignAction.Restore r=>r.Id,ShipDesignAction.Delete d=>d.Id,_=>default};
                int index=data.Designs.FindIndex(d=>d.Id==id);if(index<0)return Reject("ship_design_missing");var design=data.Designs[index];
                switch(command.Action)
                {
                    case ShipDesignAction.Archive:
                        if(id.Value==1)return Reject("ship_design_permanent");if(design.Archived)return CommandOutcome.NoChange(state);
                        data.Designs[index]=design with{Archived=true};break;
                    case ShipDesignAction.Restore:
                        if(!design.Archived)return CommandOutcome.NoChange(state);if(data.Designs.Count(d=>!d.Archived)>=32)return Reject("ship_design_active_limit");
                        data.Designs[index]=design with{Archived=false};break;
                    case ShipDesignAction.Delete:
                        if(id.Value==1)return Reject("ship_design_permanent");if(!design.Archived||ShipDesignCatalogue.References(state,owner.Id,id).Any)return Reject("ship_design_referenced");
                        data.Designs.RemoveAt(index);break;
                    default:return Reject("ship_design_unavailable");
                }
            }
            data.CatalogueRevision=checked(data.CatalogueRevision+1);after.Ready=false;
            if(GameStateValidator.Validate(content,candidate).Count!=0)return Reject("ship_design_invalid");
            return new(candidate,null,true,null);
        }
        catch(Exception error)when(error is OverflowException or ArgumentException or InvalidOperationException){return Reject("ship_design_invalid");}
    }
}
