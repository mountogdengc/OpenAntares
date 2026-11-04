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
    public static RivalPlanningOutcome Prepare(ContentSet content,GameState snapshot)=>PrepareCore(content,snapshot,null);
    internal static RivalPlanningOutcome PrepareOwner(ContentSet content,GameState snapshot,EmpireId owner,EmpireView? frozen=null)=>PrepareCore(content,snapshot,owner,frozen,publishDates:false);
    private static RivalPlanningOutcome PrepareCore(ContentSet content,GameState snapshot,EmpireId? onlyOwner,EmpireView? frozen=null,bool publishDates=true)
    {
        if(!Rules.HasDiplomacy(snapshot.RulesVersion))return new(snapshot,true);var working=snapshot.Clone();
        try
        {
            if(frozen is not null&&(frozen.Viewer!=onlyOwner||frozen.Turn!=snapshot.Turn))throw new ArgumentException("Frozen diplomacy view has changed owner or turn.");
            var views=frozen is not null?new[]{frozen}:snapshot.Empires.Where(e=>e.Controller==ControllerKind.Ai&&(onlyOwner is null||e.Id==onlyOwner)).OrderBy(e=>e.Id).Select(e=>EmpireViewProjector.Project(content,snapshot,e.Id)).ToArray();
            foreach(var view in views)
            {
                long submitted=0;bool failed=false;
                DiplomacyPlanner.Plan(content,view,command=>
                {
                    if(failed)return;if(++submitted>3L*view.Contacts.Length||command.Issuer!=view.Viewer||command.ExpectedTurn!=snapshot.Turn){failed=true;return;}
                    // A preceding rival's newly staged offer is not an incoming published fact.
                    // Preserve the canonical first offer without answering or rebinding its public revision.
                    if(frozen is not null&&command.Action is DiplomacyAction.OfferSettlement or DiplomacyAction.OfferTrade&&view.Diplomacy!.Contacts.Single(c=>c.Identity.OtherEmpireId==command.Other).Pending is null&&working.Diplomacy!.Pairs.Single(p=>p.Pair==DiplomacyPair.For(command.Issuer,command.Other)).Pending is {CreatedTurn:var created}&&created>=snapshot.Turn)return;
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
            if(publishDates)PublishOfferDates(content,snapshot,working);
            return new(working,true);
        }
        catch(Exception e)when(e is OverflowException or InvalidOperationException or ArgumentException){return new(snapshot,false);}
    }
    internal static void PublishOfferDates(ContentSet content,GameState snapshot,GameState working)
    {
        // Only after every owner has prepared: staged future dates are not published worlds.
        foreach(var pair in working.Diplomacy!.Pairs)
            if(pair.Pending is {} offer&&working.FindEmpire(offer.Proposer)!.Controller==ControllerKind.Ai&&snapshot.Diplomacy!.Pairs.Single(p=>p.Pair==pair.Pair).Pending?.Id!=offer.Id)
            {offer.CreatedTurn=checked(snapshot.Turn+1);offer.ExpiryTurn=checked(offer.CreatedTurn+content.Diplomacy!.OfferLifetime);}
    }
}
