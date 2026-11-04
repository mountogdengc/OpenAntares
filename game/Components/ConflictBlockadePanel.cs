using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;
public partial class ConflictBlockadePanel:VBoxContainer
{
    public static string Explain(EmpireView view,StarId? star=null)
    {
        if(view.Conflict is not {} own)return "";
        var lines=own.OwnPressure.Where(p=>star is null||p.Star==star).Select(p=>$"Blockade pressure at {view.Galaxy.Stars.Single(s=>s.Id==p.Star).Name}: retain {p.RetainedPercent}% of ordinary net production, rounded down, before trade income. Your bank, support, science and population are unchanged.").ToList();
        lines.AddRange(own.Blockades.Where(b=>star is null||b.Star==star).Select(b=>$"Your fleet {b.Fleet.Value} blockade at {view.Galaxy.Stars.Single(s=>s.Id==b.Star).Name}: {b.Status}; eligible from turn {b.EffectiveTurn}. Opposing production and reserves are unknown."));
        if(lines.Count>0)lines.Add("Estimates hold current pressure constant; future arrivals, orders, peace and force changes can change it. Review Blockade / Lift in Empire conflict orders.");
        return string.Join("\n",lines);
    }
    public void SetView(EmpireView view,StarId? star=null){DevelopmentUi.Clear(this);string text=Explain(view,star);Visible=text.Length>0;if(Visible)AddChild(DevelopmentUi.Text(text,"WarningLabel"));}
}
