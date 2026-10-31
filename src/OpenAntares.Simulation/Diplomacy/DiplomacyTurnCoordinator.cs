using System;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public static class DiplomacyTurnCoordinator
{
    public static RivalPlanningOutcome Prepare(ContentSet content,GameState snapshot)
    {
        if(!Rules.HasDiplomacy(snapshot.RulesVersion))return new(snapshot,true);var working=snapshot.Clone();
        try
        {
            var views=snapshot.Empires.Where(e=>e.Controller==ControllerKind.Ai).OrderBy(e=>e.Id).Select(e=>EmpireViewProjector.Project(content,snapshot,e.Id)).ToArray();
            foreach(var view in views)
            {
                long submitted=0;bool failed=false;
                DiplomacyPlanner.Plan(content,view,command=>
                {
                    if(failed)return;if(++submitted>3L*view.Contacts.Length||command.Issuer!=view.Viewer||command.ExpectedTurn!=snapshot.Turn){failed=true;return;}
                    // Public decisions remain frozen. Only bind subsequent intents to the AI's own preceding authorized mutations.
                    var bound=command with{PrivateRevision=DiplomacyTokens.PrivateRevision(content,working,command.Issuer,command.Other)};
                    var outcome=CommandProcessor.ExecuteRivalDiplomacy(content,working,bound);
                    if(!outcome.Accepted)
                    {
                        var key=DiplomacyPair.For(command.Issuer,command.Other);
                        bool crossed=command.Action is DiplomacyAction.OfferSettlement or DiplomacyAction.OfferTrade&&snapshot.Diplomacy!.Pairs.Single(p=>p.Pair==key).Pending is null&&working.Diplomacy!.Pairs.Single(p=>p.Pair==key).Pending is {CreatedTurn:var turn}&&turn==snapshot.Turn;
                        if(!crossed)failed=true;return;
                    }
                    working=outcome.State;
                });
                if(failed)return new(snapshot,false);
            }
            // Preparation is private. A new AI offer starts its lifetime at publication,
            // after the recipient first receives an opportunity to answer it.
            foreach(var pair in working.Diplomacy!.Pairs)
                if(pair.Pending is {} offer&&working.FindEmpire(offer.Proposer)!.Controller==ControllerKind.Ai&&snapshot.Diplomacy!.Pairs.Single(p=>p.Pair==pair.Pair).Pending?.Id!=offer.Id)
                {offer.CreatedTurn=checked(snapshot.Turn+1);offer.ExpiryTurn=checked(offer.CreatedTurn+content.Diplomacy!.OfferLifetime);}
            return new(working,true);
        }
        catch(Exception e)when(e is OverflowException or InvalidOperationException or ArgumentException){return new(snapshot,false);}
    }
}
