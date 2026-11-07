using System;
using System.Linq;
using Godot;
using OpenAntares.Game.Presentation;

namespace OpenAntares.Game.Screens;

public partial class GameScreen
{
    private Window OverviewWindow(string key)=>key switch
    {
        "Fleet"=>_fleetCommand,"Officers"=>_officerDetails,"Designs"=>_shipDesigns,
        "Editor"=>_shipDesignEditor,"Refit"=>_refit,"Combat"=>_combatOrders,
        "Conflict"=>_conflictOrders,"Assembly"=>_assemblyRequest,
        _=>throw new ArgumentException("Unknown existing overview",nameof(key))
    };
    private void OpenOverviewChild(string parentKey,string childKey,Action open)
    {
        var parent=OverviewWindow(parentKey);
        if(parent.Visible)
        {
            var focus=parent.GetViewport().GuiGetFocusOwner();
            var scroll=PresentationNodes<ScrollContainer>(parent).FirstOrDefault();
            _context.OverviewReturns.Push(new(parentKey,childKey,focus?.Name.ToString()??"",scroll?.ScrollVertical??0));
        }
        parent.Hide();Callable.From(open).CallDeferred();
    }
    private void WireOverviewReturns()
    {
        foreach(string key in new[]{"Fleet","Officers","Designs","Editor","Refit","Combat","Conflict","Assembly"})
        {
            var window=OverviewWindow(key);
            window.VisibilityChanged+=()=>{if(!window.Visible&&_context.OverviewReturns.Count>0)RestoreInvokingOverview();};
        }
    }
    private async void RestoreInvokingOverview()
    {
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        if(!IsInstanceValid(this)||!IsInsideTree())return;
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        if(!IsInstanceValid(this)||!IsInsideTree()||!_context.OverviewReturns.TryPeek(out var frame))return;
        var parent=OverviewWindow(frame.Parent);var child=OverviewWindow(frame.Child);
        if(child.Visible)return;
        if(!parent.Visible&&PresentationNodes<Window>(this).Any(w=>w.Visible))return;
        _context.OverviewReturns.Pop();
        if(!parent.Visible)
        {
            switch(frame.Parent)
            {
                case "Fleet":_fleetCommand.ShowEmpire(_session.Content,View);break;
                case "Designs":_shipDesigns.ShowCatalogue(_session.Content,View);break;
                default:throw new InvalidOperationException("Unsupported invoking overview");
            }
        }
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        if(!IsInstanceValid(this)||!IsInsideTree()||!parent.Visible)return;
        if(parent.FindChild(frame.FocusKey,true,false) is Control focus&&focus.IsVisibleInTree())focus.GrabFocus();
        var scroll=PresentationNodes<ScrollContainer>(parent).FirstOrDefault();if(scroll is not null)scroll.ScrollVertical=frame.ScrollVertical;
    }
}
