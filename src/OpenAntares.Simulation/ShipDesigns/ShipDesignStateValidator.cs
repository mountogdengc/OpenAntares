using System.Collections.Generic;
using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.ShipDesigns;
public static class ShipDesignStateValidator
{
    public static IReadOnlyList<string> Validate(ContentSet content,GameState state)
    {
        var errors=new List<string>();
        void Error(string message)=>errors.Add("ship designs: "+message);
        if(!Rules.HasShipDesigns(state.RulesVersion))
        {
            if(state.Empires.Any(e=>e.ShipDesigns is not null)||state.Vessels.Any(v=>v.Design is not null))Error("old rules cannot contain catalogue or equipped design fields");
            return errors;
        }
        if(content.ShipDesigns is not {} definition){Error("explicit content required, including Off");return errors;}
        foreach(var owner in state.Empires)
        {
            if(owner.ShipDesigns is not {} data){Error($"{owner.Id} missing catalogue");continue;}
            void OwnerError(string message)=>Error($"{owner.Id}: {message}");
            if(data.NextDesignId<=0||data.NextRefitId<=0||data.NextEventId<=0||data.CatalogueRevision<=0)OwnerError("invalid counter/revision");
            if(data.Designs.Count>256||data.Designs.Count(d=>!d.Archived)>32)OwnerError("catalogue exceeds 32 active / 256 retained bounds");
            if(data.Designs.Select(d=>d.Id).Distinct().Count()!=data.Designs.Count||!data.Designs.Select(d=>d.Id).SequenceEqual(data.Designs.Select(d=>d.Id).Order()))OwnerError("design IDs must be unique and sorted");
            if(data.Designs.Select(d=>d.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=data.Designs.Count)OwnerError("duplicate design names");
            foreach(var design in data.Designs)
            {
                if(design.Id.Value<=0||design.Id.Value>=data.NextDesignId)OwnerError("invalid design ID/next counter");
                if(string.IsNullOrWhiteSpace(design.Name)||design.Name.Length>40||design.Name!=design.Name.Trim()||design.Name.Any(char.IsControl))OwnerError("invalid trimmed design name (1..40)");
                try
                {
                    _=ShipDesignMath.Calculate(definition,design.Recipe);
                    if(!Unlocked(definition,design.Recipe,owner))OwnerError("design recipe requires unearned research");
                }
                catch(Exception ex)when(ex is ArgumentException or OverflowException or KeyNotFoundException){OwnerError("invalid recipe: "+ex.Message);}
            }
            var starter=data.Designs.FirstOrDefault(d=>d.Id.Value==1);
            if(starter is null||starter.Name!="Standard Escort"||starter.Archived||!SameRecipe(starter.Recipe,definition.StartingRecipe))OwnerError("permanent active Standard Escort required");
            if(data.Refits.Select(j=>j.Id).Distinct().Count()!=data.Refits.Count||!data.Refits.Select(j=>j.Id).SequenceEqual(data.Refits.Select(j=>j.Id).Order()))OwnerError("refit IDs must be unique and sorted");
            if(data.Refits.GroupBy(j=>j.Vessel).Any(g=>g.Count()>1)||data.Refits.GroupBy(j=>j.Port).Any(g=>g.Count()>1))OwnerError("one refit per ship and port");
            foreach(var job in data.Refits)
            {
                var source=data.Designs.FirstOrDefault(d=>d.Id==job.SourceDesign);var target=data.Designs.FirstOrDefault(d=>d.Id==job.TargetDesign);var ship=state.FindVessel(job.Vessel);
                if(job.Id.Value<=0||job.Id.Value>=data.NextRefitId||job.Revision<=0||job.Price<=0||job.AcceptedTurn<0||job.AcceptedTurn>state.Turn||!Enum.IsDefined(job.Status))OwnerError("invalid refit fields");
                if(source is null||target is null||source.Id==target.Id||source.Recipe.FrameId!=target.Recipe.FrameId||ship?.EmpireId!=owner.Id||ship.Design!=job.SourceDesign||state.FindColony(job.Port)?.EmpireId!=owner.Id)OwnerError("invalid refit equipment/ship/port references");
                if(job.Status==RefitStatus.Paused&&string.IsNullOrWhiteSpace(job.Reason))OwnerError("paused refit requires a reason");
                var port=state.FindColony(job.Port);
                if(port is null||ShipDesignTargets.Job(state,port)?.Id!=job.Id||port.Development is not{ConstructionEnabled:false,ActiveQueueToken:null,Origin:Development.ProductionOrigin.Manual})OwnerError("refit must own its manual, paused-construction production slot");
                if(owner.Controller!=ControllerKind.Human)OwnerError("refits require human planning authority");
                if(source is not null&&target is not null)
                {
                    try
                    {
                        var before=ShipDesignMath.Calculate(definition,source.Recipe);var after=ShipDesignMath.Calculate(definition,target.Recipe);
                        if(job.Price!=ShipDesignMath.RefitPrice(after.BuildCost)||RefitQueries.SameEquipment(source.Recipe,target.Recipe))OwnerError("invalid frozen refit price or identical equipment");
                        if(job.Status==RefitStatus.Active&&(port is null||ship is null||ship.RoleId!=definition.WarshipRoleId||ship.Transit is not null||ship.StationaryStarId!=state.FindPlanet(port!.PlanetId)?.StarId||ship.CarriedPopulation!=0||ship.PendingMove is not null||ship.PendingFounding is not null||ship.AmebaEncounter is not null||ship.Combat is not{Cohesion:100,Repairing:false,ReturnColony:null}||ship.Combat.Hull!=before.MaxHull||state.Combat?.Encounters.Any(e=>e.Members.Contains(ship.Id))==true))OwnerError("active refit ship condition/location/activity conflicts");
                        if(ship?.Fleet is {} fleet&&owner.Fleets?.Fleets.SingleOrDefault(f=>f.Id==fleet) is {} group&&(group.Order.Kind!=Fleets.FleetOrderKind.Hold||group.Cohort is not null))OwnerError("refit fleet must remain in Hold");
                        if(owner.Fleets?.Requests.Any(r=>Fleets.FleetQueries.IsActive(r.Status)&&(r.Slots.Any(s=>s.Vessel==job.Vessel&&s.Status!=Fleets.SlotStatus.Missing)||ship?.Fleet is {} groupId&&r.RallyFleet==groupId))==true)OwnerError("refit conflicts with active assembly/rally authority");
                    }
                    catch(Exception ex)when(ex is ArgumentException or OverflowException or KeyNotFoundException){OwnerError("invalid refit recipe: "+ex.Message);}
                }
            }
            if(data.RecentEvents.Count>20||data.PendingEvents.Count>20)OwnerError("event history exceeds 20");
            var events=data.RecentEvents.Concat(data.PendingEvents).ToArray();
            if(events.Select(e=>e.Id).Distinct().Count()!=events.Length||events.Any(e=>e.Owner!=owner.Id||e.Id<=0||e.Id>=data.NextEventId||e.Turn<0||e.Turn>state.Turn||e.Paid<0||string.IsNullOrWhiteSpace(e.Kind)||string.IsNullOrWhiteSpace(e.Reason)))OwnerError("invalid event owner/ID/turn/facts");
            foreach(var list in new[]{data.RecentEvents,data.PendingEvents})if(!list.Select(e=>e.Id).SequenceEqual(list.Select(e=>e.Id).Order()))OwnerError("event IDs must be sorted");
            foreach(var e in events)
            {
                if(e.Kind is not("refit_accepted" or "refit_cancelled" or "refit_resumed" or "refit_paused" or "refit_cancelled_missing_ship" or "refit_completed")||e.Vessel is not{Value:>0}||e.Port is not{Value:>0}||!ValidFact(content,owner,e.Before)||!ValidFact(content,owner,e.After))OwnerError("invalid historical equipment/event shape");
                if(ValidFact(content,owner,e.Before)&&ValidFact(content,owner,e.After)&&(e.Before!.Id==e.After!.Id||e.Before.Recipe.FrameId!=e.After.Recipe.FrameId||RefitQueries.SameEquipment(e.Before.Recipe,e.After.Recipe)||e.Paid!=(e.Kind=="refit_completed"?ShipDesignMath.RefitPrice(e.After.Stats.BuildCost):0)))OwnerError("invalid historical refit payment or equipment");
            }
        }
        foreach(var ship in state.Vessels)
        {
            if(ship.RoleId==definition.WarshipRoleId)
            {
                if(ship.Design is not {} id||state.FindEmpire(ship.EmpireId)?.ShipDesigns?.Designs.Any(d=>d.Id==id)!=true)Error($"{ship.Id} requires an owned equipped design");
                if(ship.CarriedPopulation!=0)Error($"{ship.Id} configured warship cannot carry population");
            }
            else if(ship.Design is not null)Error($"{ship.Id} civilian cannot have a custom design");
        }
        return errors;
    }
    internal static bool SameRecipe(ShipRecipe? a,ShipRecipe? b)=>a is not null&&b is not null&&a.FrameId==b.FrameId&&a.EngineId==b.EngineId&&a.ArmorId==b.ArmorId&&!a.WeaponBankIds.IsDefault&&!b.WeaponBankIds.IsDefault&&a.WeaponBankIds.SequenceEqual(b.WeaponBankIds);
    internal static bool ValidFact(ContentSet content,EmpireState owner,EquipmentFact? fact)
    {
        if(content.ShipDesigns is not {} definition||owner.ShipDesigns is not {} data||fact is null||fact.Id.Value<=0||fact.Id.Value>=data.NextDesignId||string.IsNullOrWhiteSpace(fact.Name)||fact.Name.Length>40||fact.Name!=fact.Name.Trim()||fact.Name.Any(char.IsControl))return false;
        try{return fact.Stats==ShipDesignMath.Calculate(definition,fact.Recipe)&&Unlocked(definition,fact.Recipe,owner);}
        catch(Exception error)when(error is ArgumentException or OverflowException or KeyNotFoundException){return false;}
    }
    internal static bool Unlocked(ShipDesignDefinition definition,ShipRecipe recipe,EmpireState owner)
    {
        var parts=recipe.WeaponBankIds.Prepend(recipe.EngineId);if(recipe.ArmorId is {} armor)parts=parts.Append(armor);
        return Rules.PrerequisitesKnown(owner,definition.Frames[recipe.FrameId].Prerequisites)&&parts.All(id=>Rules.PrerequisitesKnown(owner,definition.Components[id].Prerequisites));
    }
}
