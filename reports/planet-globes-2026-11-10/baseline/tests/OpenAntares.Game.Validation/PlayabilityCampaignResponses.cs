using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Scouting;

namespace OpenAntares.Game.Validation;

internal sealed partial class PlayabilityCampaignControls
{
    public async Task Conflict(ConflictAction action)
    {
        await CloseWindows();
        if(action is ConflictAction.DeclareWar)
        {
            await _observer.Navigate("Review war declaration",async()=>{await Click(Screen,"OpenDiplomacy");await Click(Screen,"ConflictDeclareWar");});
            await _observer.ExecuteUi("Apply reviewed war declaration",()=>Click(Screen,"ConflictDeclarationApply"));return;
        }
        string? politics=action switch{ConflictAction.ProposePeace=>"ConflictProposePeace",ConflictAction.AnswerPeace{Accept:true}=>"ConflictAcceptPeace",ConflictAction.AnswerPeace=>"ConflictDeclinePeace",ConflictAction.WithdrawPeace=>"ConflictWithdrawPeace",ConflictAction.RevokePeaceAnswer=>"ConflictRevokePeaceAnswer",ConflictAction.RevokeDeclaration=>"ConflictRevokeDeclaration",_=>null};
        if(politics is not null)
        {
            await _observer.Navigate("Open known conflict diplomacy",()=>Click(Screen,"OpenDiplomacy"));
            await _observer.ExecuteUi("Explicit peace choice "+action.GetType().Name,()=>Click(Screen,politics));return;
        }
        var own=View.Conflict!;
        VesselId? anchor=action switch
        {
            ConflictAction.ConfigureDefense a=>a.Anchor,
            ConflictAction.Attack a=>own.Ships.First(s=>s.Fleet==a.Fleet).Id,
            ConflictAction.Blockade a=>own.Ships.First(s=>s.Fleet==a.Fleet).Id,
            ConflictAction.LiftBlockade a=>own.Ships.First(s=>s.Fleet==a.Fleet).Id,
            ConflictAction.CancelAttack a=>own.Ships.First(s=>s.Fleet==a.Fleet).Id,
            ConflictAction.ChangeOrders a=>own.Encounters.Single(e=>e.Id==a.Encounter).OwnMembers[0],
            ConflictAction.Withdraw a=>own.Encounters.Single(e=>e.Id==a.Encounter).OwnMembers[0],
            ConflictAction.Respond a=>own.Encounters.Single(e=>e.Id==a.Encounter).OwnMembers[0],_=>null
        };
        await _observer.Navigate("Review exact own conflict force",async()=>
        {
            await Click(Screen,"DecisionsConflictOrders");var dialog=All<ConflictOrdersDialog>(Screen).Single();var picker=Named<OptionButton>(dialog,"ConflictOwnForce");var selected=own.Ships.Single(s=>s.Id==anchor);
            int index=Enumerable.Range(0,picker.ItemCount).Single(i=>{var id=new VesselId(picker.GetItemMetadata(i).AsInt32());var ship=own.Ships.Single(s=>s.Id==id);return id==anchor||selected.Fleet is {} fleet&&ship.Fleet==fleet;});await Pick(picker,index);
            var orders=action switch{ConflictAction.Attack a=>a.Orders,ConflictAction.ConfigureDefense a=>a.Orders,ConflictAction.ChangeOrders a=>a.Orders,_=>null};
            if(orders is not null){await Pick(Named<OptionButton>(dialog,"ConflictDoctrine"),(int)orders.Doctrine);await Pick(Named<OptionButton>(dialog,"ConflictStragglers"),(int)orders.Stragglers);await Pick(Named<OptionButton>(dialog,"ConflictArrival"),(int)orders.OnArrival);await PickId(Named<OptionButton>(dialog,"ConflictReturnPort"),orders.ReturnColony?.Value??0);}
            if(action is ConflictAction.Attack attack)await PickId(Named<OptionButton>(dialog,"ConflictArmedTarget"),attack.Target.Value);
            string review=action switch{ConflictAction.Attack=>"ConflictReviewAttack",ConflictAction.ConfigureDefense=>"ConflictReviewDefense",ConflictAction.ChangeOrders=>"ConflictReviewChange",ConflictAction.Withdraw=>"ConflictReviewWithdraw",ConflictAction.Respond=>"ConflictReviewRespond",ConflictAction.Blockade=>"ConflictReviewBlockade",ConflictAction.LiftBlockade=>"ConflictReviewLift",ConflictAction.CancelAttack=>"ConflictCancelAttack",_=>throw new InvalidOperationException("Unsupported conflict UI action")};
            await Click(dialog,review);
        });
        await _observer.ExecuteUi("Apply reviewed conflict "+action.GetType().Name,()=>Click(Screen,"ConflictOrdersApply"));
    }

    public async Task Combat(CombatAction action)
    {
        await CloseWindows();
        var preview=CombatQueries.Preview(_observer.Game.Content,View,action);
        if(!preview.Allowed)throw new InvalidOperationException("Recorded combat action is unavailable: "+preview.RejectionCode);
        var expected=new CombatActionCommand(_observer.Owner,View.Turn,action,preview.Token);
        await _observer.Navigate("Review existing combat action",async()=>
        {
            if(action is CombatAction.SetRepair repair)
            {
                await Click(Screen,"OpenFleetCommand");var fleet=All<FleetCommandDialog>(Screen).Single();await Ships(fleet,repair.Vessels);
                await Click(fleet,repair.Enabled?"FleetRepair":"FleetStopRepair");await Click(Screen,"CombatPreview");
            }
            else if(action is CombatAction.RespondCombat response)
            {await Click(Screen,"OpenCombatResponses");await Click(Screen,response.Response==CombatResponse.RemainWithBlockedReturn?"CombatRemainBlocked":"CombatAcceptForce");}
            else throw new InvalidOperationException("Unsupported campaign combat action: "+action.GetType().Name);
        });
        await _observer.ExecuteUi("Apply reviewed combat "+action.GetType().Name,()=>Click(Screen,"CombatApply"),reviewedCallback:expected);
    }

    public async Task CrisisResponses()
    {
        var health=View.Crisis;if(health is not {Enabled:true})return;
        if(health.Cases.Any(c=>!c.Quarantined))
        {
            await CloseWindows();
            await _observer.Navigate("Review ordinary containment",async()=>{await Click(Screen,"OpenCrisisOverview");await Click(Screen,"CrisisSelectAll");await Click(Screen,"CrisisQuarantine");});
            await _observer.ExecuteUi("Apply reviewed quarantine",()=>Click(Screen,"CrisisConfirmQuarantine"));
        }
        health=View.Crisis;
        if(health?.Episode is {ResearchShare:not 100})
        {
            await CloseWindows();await _observer.Navigate("Review cure diversion",()=>Click(Screen,"OpenCrisisOverview"));
            await _observer.ExecuteUi("Explicit current cure share",()=>Pick(Named<OptionButton>(Screen,"CrisisResearchShare"),3));
        }
        health=View.Crisis;
        if(health is {RequireAcknowledgement:true}&&!health.PendingReceipts.IsEmpty)
        {
            await CloseWindows();await _observer.Navigate("Review exact urgent facts",async()=>{await Click(Screen,"OpenCrisisOverview");await Click(Screen,"CrisisSelectAllAttention");});
            await _observer.ExecuteUi("Explicit crisis acknowledgement",()=>Click(Screen,"CrisisAcknowledgeSelected"));
        }
    }

    public async Task ScoutResponses()
    {
        if(View.Scouting is not {} scouts)return;
        foreach(var vessel in scouts.Vessels.Where(v=>v.Stage is not null).ToArray())
        {
            await CloseWindows();
            await _observer.Navigate("Inspect current ameba response",async()=>
            {
                await Click(Screen,"OpenScoutOverview");var overview=All<ScoutOverviewDialog>(Screen).Single();var list=Named<ItemList>(overview,"ScoutVesselList");
                var rows=View.Scouting!.Vessels;int index=Enumerable.Range(0,rows.Length).Single(i=>rows[i].Id==vessel.Id);list.Select(index);list.EmitSignal(ItemList.SignalName.ItemSelected,(long)index);await _observer.Frames();
            });
            var current=View.Scouting!.Vessels.Single(v=>v.Id==vessel.Id);
            if(current.PendingMove is null)
            {
                var withdraw=Named<Button>(Screen,"ScoutWithdraw");
                if(withdraw.Disabled)throw new InvalidOperationException("Ameba audit has no legal departure; preserve the response evidence instead of manufacturing one.");
                if(current.Stage==AmebaStage.Exposed)
                {
                    await _observer.Navigate("Review explicit late departure risk",()=>Click(withdraw));
                    var confirmation=Named<ConfirmationDialog>(Screen,"ScoutConfirmation");
                    await _observer.ExecuteUi("Apply reviewed ameba departure",()=>Click(confirmation.GetOkButton()));
                }
                else await _observer.ExecuteUi("Queue ordinary safe departure",()=>Click(withdraw));
            }
        }
        if(View.Scouting is {} live&&live.Vessels.Any(v=>v.Stage is not null&&!v.Acknowledged))
        {
            await CloseWindows();await _observer.Navigate("Review exact scouting group",()=>Click(Screen,"OpenScoutOverview"));
            await _observer.ExecuteUi("Explicit scouting acknowledgement",()=>Click(Screen,"ScoutAcknowledge"));
        }
    }
}
