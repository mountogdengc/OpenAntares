using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public static class ConflictAiCoordinator
{
    public static RivalPlanningOutcome Prepare(ContentSet content,GameState snapshot)
    {
        if(!Rules.HasConflict(snapshot.RulesVersion))return new(snapshot,true);
        if(GameStateValidator.Validate(content,snapshot,true).Count!=0)return new(snapshot,false);
        var working=snapshot.Clone();
        try
        {
            var frozen=snapshot.Empires.Where(e=>e.Controller==ControllerKind.Ai).OrderBy(e=>e.Id).Select(e=>EmpireViewProjector.Project(content,snapshot,e.Id)).ToArray();
            foreach(var initial in frozen)
            {
                var owner=initial.Viewer;
                EmpireView Current()
                {
                    var current=EmpireViewProjector.Project(content,working,owner);
                    return current with{ForeignVessels=initial.ForeignVessels,Diplomacy=current.Diplomacy! with{Contacts=initial.Diplomacy!.Contacts},Conflict=current.Conflict! with{Military=initial.Conflict!.Military}};
                }
                void ApplyStage(ConflictPlanningStage stage)
                {
                    foreach(var command in ConflictPlanner.Plan(content,Current(),stage))
                    {
                        Command bound=command;
                        if(command is ConflictCommand conflict){var preview=ConflictQueries.Preview(content,Current(),conflict.Action);if(!preview.Allowed)continue;bound=conflict with{Token=preview.Token};}
                        if(command is Combat.CombatActionCommand combat){var preview=Combat.CombatQueries.Preview(content,Current(),combat.Action);if(!preview.Allowed)continue;bound=combat with{PreviewToken=preview.Token};}
                        var outcome=CommandProcessor.ExecuteConflictPreparation(content,working,bound);
                        // Normal legal skips (full fleet slots, changed own authority, unsafe routes) are local.
                        if(outcome.Accepted)working=outcome.State;
                    }
                }
                RefreshVisibleTimers(working,initial);
                ApplyStage(ConflictPlanningStage.Responses);ApplyStage(ConflictPlanningStage.Declarations);ApplyStage(ConflictPlanningStage.Production);
                for(int i=0;i<content.Conflict!.GroupingCommandLimit;i++)ApplyStage(ConflictPlanningStage.Grouping);
                ApplyStage(ConflictPlanningStage.Orders);
                var diplomacy=Diplomacy.DiplomacyTurnCoordinator.PrepareOwner(content,working,owner,Current());if(!diplomacy.Succeeded)return new(snapshot,false);working=diplomacy.State;
                var expansion=RivalTurnPlanner.PrepareOwner(content,working,owner);if(!expansion.Succeeded)return new(snapshot,false);working=expansion.State;
            }
            if(GameStateValidator.Validate(content,working,true).Count!=0)return new(snapshot,false);
            Diplomacy.DiplomacyTurnCoordinator.PublishOfferDates(content,snapshot,working);
            return new(working,true);
        }
        catch(Exception e)when(e is ArgumentException or InvalidOperationException or OverflowException or System.Collections.Generic.KeyNotFoundException){return new(snapshot,false);}
    }
    internal static void RefreshVisibleTimers(GameState candidate,EmpireView view)
    {
        foreach(var pair in candidate.Conflict!.Pairs.Where(p=>p.Pair.Contains(view.Viewer)))
        {
            var own=pair.Parties.Single(p=>p.Owner==view.Viewer);var other=pair.Pair.Other(view.Viewer);
            bool shared=view.Stars.Any(s=>s.Current&&view.Colonies.Any(c=>c.StarId==s.Chart.Id)&&s.Planets is {} planets&&planets.Any(p=>p.OwnerId==other));
            own.ClaimObservedSince=shared?own.ClaimObservedSince??view.Turn:null;
        }
    }
}
