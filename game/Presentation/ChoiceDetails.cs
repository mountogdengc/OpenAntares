using System;
using System.Linq;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Presentation;

/// <summary>Descriptions of existing choices; computes neither prices nor completion forecasts.</summary>
internal static class ChoiceDetails
{
    private static string Effect(BuildingEffect effect) => effect.Kind + " +" +
        (effect.Kind == EffectKinds.Capacity
            ? effect.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : Format.Points(effect.Amount));

    internal static string Research(ContentSet content, OwnResearchView view)
    {
        if (view.TargetId is not {} id) return "No target: research is saved. Choose an available technology to begin spending it.";
        var technology=content.Technologies[id];
        string needs=string.Join(", ",technology.PrerequisiteTechnologyIds.Where(p=>!view.KnownTechnologyIds.Contains(p)).Select(p=>content.Technologies[p].Name));
        var buildings=content.Buildings.Values.Where(b=>b.PrerequisiteTechnologyIds.Contains(id)).OrderBy(b=>b.Id,StringComparer.Ordinal)
            .Select(b=>b.Name+" ("+string.Join(", ",b.Effects.Select(Effect))+")");
        var equipment=content.ShipDesigns?.Components.Values.Where(c=>c.Prerequisites.Contains(id)).OrderBy(c=>c.Id,StringComparer.Ordinal).Select(c=>c.Name) ?? Enumerable.Empty<string>();
        string unlocks=string.Join("; ",buildings.Concat(equipment));
        return $"{technology.Name} · Cost {Format.Points(technology.Cost)} research. {(needs.Length==0?"Prerequisites satisfied.":"Needs "+needs+".")}\n"+
            (unlocks.Length>0?"Enables existing choices: "+unlocks+".":"Review the existing unlocked choices after completion.");
    }

    internal static string Production(ContentSet content, EmpireView view, OwnedColonyView colony, ProductionTarget target)
    {
        string name;long cost;string requirements="";
        if(target.Kind==ProductionKinds.Building)
        {
            var building=content.Buildings[target.ContentId];name=building.Name;cost=building.Cost;
            string needs=string.Join(", ",building.PrerequisiteTechnologyIds.Where(id=>!view.Research.KnownTechnologyIds.Contains(id)).Select(id=>content.Technologies[id].Name));
            requirements=(needs.Length>0?" Needs "+needs+".":" Prerequisites satisfied.")+" "+string.Join(", ",building.Effects.Select(Effect))+".";
        }
        else if(target.Kind==ProductionKinds.Vessel)
        {
            var vessel=content.Vessels[target.ContentId];name=vessel.Name;cost=vessel.Cost;
            requirements=vessel.CanFoundColony?$" Requires {vessel.MinimumSourcePopulation} {colony.PopulationUnitLabel} and {vessel.EmbarkPopulation} support workers for cargo."+(vessel.RequiresFullColony?" Requires a full colony at current capacity.":""):" One vessel; no automatic repeat order.";
        }
        else if(target.Kind==ProductionKinds.ShipDesign && int.TryParse(target.ContentId,out int id) && view.ShipDesigns?.Designs.SingleOrDefault(d=>d.Id.Value==id) is {} design)
        {name=design.Name;cost=design.Stats.BuildCost;requirements=" One exact ship with its selected equipment; no substitution.";}
        else return "This retained project has no new-choice cost preview. Review its committed work in the colony.";
        bool active=colony.ProjectKind==target.Kind&&colony.ProjectId==target.ContentId;
        string estimate=active?BreakdownText.Estimate(colony.Economy.Production.Estimate):"Completion estimate unavailable until this choice is active; preceding queued work and future conditions can change it.";
        return $"{name} · Cost {Format.Points(cost)} production.{requirements}\n{estimate}"+
            (active&&colony.ProductionIfSetupReleased is {} conditional?"\nConditional on setup reservation release: "+BreakdownText.Estimate(conditional.Estimate)+"; the partner's queued answer is private.":"");
    }
}
