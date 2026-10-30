using System;
using System.Globalization;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.ShipDesigns;
public static class ShipDesignTargets
{
    public static ProductionTarget Build(DesignId id)=>new(ProductionKinds.ShipDesign,Key(id.Value));
    public static ProductionTarget Refit(RefitId id)=>new(ProductionKinds.Refit,Key(id.Value));
    private static string Key(int value)=>value>0?value.ToString(CultureInfo.InvariantCulture):throw new ArgumentOutOfRangeException(nameof(value));
    public static bool TryId(string? key,out int id)=>int.TryParse(key,NumberStyles.None,CultureInfo.InvariantCulture,out id)&&id>0&&key==id.ToString(CultureInfo.InvariantCulture);
    public static ProductionTarget Normalize(ContentSet content,GameState state,EmpireId owner,ProductionTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if(Rules.HasShipDesigns(state.RulesVersion)&&target.Kind==ProductionKinds.Vessel&&target.ContentId==content.ShipDesigns?.WarshipRoleId)return Build(new(1));
        if(target.Kind is ProductionKinds.ShipDesign or ProductionKinds.Refit)
        {
            if(!Rules.HasShipDesigns(state.RulesVersion)||!TryId(target.ContentId,out _))throw new ArgumentException("Positive canonical local design/job ID required.");
        }
        return target;
    }
    public static ShipDesign? Bound(ContentSet content,GameState state,EmpireId owner,ProductionTarget target,bool newChoice=false)
    {
        if(!Rules.HasShipDesigns(state.RulesVersion)||content.ShipDesigns is null||target.Kind!=ProductionKinds.ShipDesign||!TryId(target.ContentId,out int number))return null;
        var empire=state.FindEmpire(owner);var matches=empire?.ShipDesigns?.Designs.Where(d=>d.Id.Value==number).Take(2).ToArray();
        if(matches is not{Length:1})return null;var design=matches[0];
        try
        {
            _=ShipDesignMath.Calculate(content.ShipDesigns,design.Recipe);
            return (!newChoice||!design.Archived)&&ShipDesignStateValidator.Unlocked(content.ShipDesigns,design.Recipe,empire!)?design:null;
        }
        catch(Exception error)when(error is ArgumentException or OverflowException or System.Collections.Generic.KeyNotFoundException){return null;}
    }
    internal static bool Available(Knowledge.EmpireView view,ProductionTarget target,bool newChoice=false)=>Rules.HasShipDesigns(view.RulesVersion)&&target.Kind==ProductionKinds.ShipDesign&&TryId(target.ContentId,out int number)&&view.ShipDesigns?.Designs.Any(d=>d.Id.Value==number&&d.Unlocked&&(!newChoice||!d.Archived))==true;
    internal static RefitJob? Job(GameState state,ColonyState port)
    {
        if(!Rules.HasShipDesigns(state.RulesVersion)||port.ProjectKind!=ProductionKinds.Refit||!TryId(port.ProjectId,out int number))return null;
        var jobs=state.FindEmpire(port.EmpireId)?.ShipDesigns?.Refits.Where(j=>j.Id.Value==number&&j.Port==port.Id).Take(2).ToArray();
        return jobs is {Length:1}?jobs[0]:null;
    }
}
