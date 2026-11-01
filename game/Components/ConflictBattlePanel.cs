using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Conflict;
namespace OpenAntares.Game.Components;
public partial class ConflictBattlePanel:VBoxContainer
{
    public event Action<VesselId?>? OrdersRequested;
    public event Action<StarId>? StarRequested;
    public void SetView(ConflictView? own)
    {
        DevelopmentUi.Clear(this);Visible=own is not null;if(own is null)return;
        AddChild(DevelopmentUi.Text("Empire conflict — your battles and retained consequences","HeaderLabel"));
        DevelopmentUi.Button(this,"Review your conflict orders",()=>OrdersRequested?.Invoke(null),"ConflictBattleOpenOrders");
        foreach(var encounter in own.Encounters)
        {
            AddChild(DevelopmentUi.Text($"Battle {encounter.Id.Value}, round {encounter.Round}: {encounter.OwnMembers.Length} own armed ships; {encounter.ProtectedCivilians.Length} protected civilians. "+(encounter.OwnNeedsResponse?"Emergency: your revised force needs a response before Ready.":encounter.OwnWithdrawQueued?"Your withdrawal is queued.":"One automatic exchange per turn."),encounter.OwnNeedsResponse?"WarningLabel":"Label"));
            DevelopmentUi.Button(this,"Review this battle / withdrawal",()=>OrdersRequested?.Invoke(encounter.OwnMembers.FirstOrDefault()),"ConflictBattleOrders_"+encounter.Id.Value);
            DevelopmentUi.Button(this,"Locate battle system",()=>StarRequested?.Invoke(encounter.Star),"ConflictBattleStar_"+encounter.Id.Value);
        }
        foreach(var history in own.History.OrderByDescending(h=>h.EndedTurn))
        {
            AddChild(DevelopmentUi.Text($"Turn {history.EndedTurn}: {history.Outcome.Replace('_',' ')}; {history.LostMembers.Length} own ship(s) lost. Surviving damaged ships require finite repair at a legal own port."));
            foreach(var round in history.Rounds)AddChild(DevelopmentUi.Text($"Turn {round.Turn}, round {round.Round}{(round.Interception?" — interception":"")}: your hull lost {round.Own.Sum(s=>s.ActualLoss)}; observed opposing hull lost {round.Opposing.Sum(s=>s.ActualLoss)}."));
            DevelopmentUi.Button(this,"Locate aftermath / return route",()=>StarRequested?.Invoke(history.Star),"ConflictHistoryStar_"+history.Encounter.Value);
        }
        if(own.Encounters.IsEmpty&&own.History.IsEmpty)AddChild(DevelopmentUi.Text("No active battles or retained completed battles. Opening this panel never attacks or accepts a response."));
    }
}
