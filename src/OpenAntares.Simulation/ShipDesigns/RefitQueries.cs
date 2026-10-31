using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Fleets;
using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using System.Security.Cryptography;
namespace OpenAntares.Simulation.ShipDesigns;
public static class RefitQueries
{
    public static RefitPreview Preview(ContentSet content,EmpireView view,RefitAction action)
    {
        RefitPreview Reject(string code)=>new(false,code,string.Empty,null,null,null,null,null,null,null,null,null,ImmutableArray<string>.Empty);
        if(!Rules.HasShipDesigns(view.RulesVersion)||content.ShipDesigns is null||view.ShipDesigns is not {} own||view.Fleets is null||view.Combat is null||action is null)return Reject("refit_unavailable");
        try
        {
            RefitView? job=null;VesselId shipId;ColonyId portId;DesignId targetId;
            switch(action)
            {
                case RefitAction.Start start:
                    shipId=start.Vessel;portId=start.Port;targetId=start.Target;
                    job=own.Refits.SingleOrDefault(j=>j.Vessel==shipId||j.Port==portId);
                    if(job is not null&&(job.Vessel!=shipId||job.Port!=portId||job.TargetDesign!=targetId||job.Status!=RefitStatus.Active))return Reject("refit_busy");
                    break;
                case RefitAction.Cancel cancel:
                    job=own.Refits.SingleOrDefault(j=>j.Id==cancel.Job&&j.Revision==cancel.Revision);
                    if(job is null)return Reject("refit_stale");shipId=job.Vessel;portId=job.Port;targetId=job.TargetDesign;break;
                case RefitAction.Resume resume:
                    job=own.Refits.SingleOrDefault(j=>j.Id==resume.Job&&j.Revision==resume.Revision);
                    if(job is null)return Reject("refit_stale");shipId=job.Vessel;portId=job.Port;targetId=job.TargetDesign;break;
                default:return Reject("refit_unavailable");
            }
            var ship=view.Vessels.SingleOrDefault(v=>v.Id==shipId&&v.EmpireId==view.Viewer);var port=view.Colonies.SingleOrDefault(c=>c.Id==portId&&c.EmpireId==view.Viewer);
            var source=own.Designs.SingleOrDefault(d=>d.Id==ship?.Loadout?.Design);var target=own.Designs.SingleOrDefault(d=>d.Id==targetId);
            if(ship is null||port is null||source is null||target is null||ship.RoleId!=content.ShipDesigns.WarshipRoleId)return Reject("refit_unavailable");
            if(job is not null&&(job.SourceDesign!=source.Id||port.ProjectKind!=ProductionKinds.Refit||port.ProjectId!=job.Id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)))return Reject("refit_job_changed");
            if(action is not RefitAction.Cancel)
            {
                if(job is null&&target.Archived||!target.Unlocked)return Reject("refit_target_locked");
                if(source.Recipe.FrameId!=target.Recipe.FrameId)return Reject("refit_frame");
                if(SameEquipment(source.Recipe,target.Recipe))return Reject("refit_already_equipped");
                if(Eligibility(view,ship,port,job) is {} reason)return Reject(reason);
            }
            long price=job?.Price??ShipDesignMath.RefitPrice(target.Stats.BuildCost);
            long? estimate=price<=port.SpendableProduction?1:port.Economy.NetProduction.Total>0?checked((price-port.SpendableProduction-1)/port.Economy.NetProduction.Total+1):(long?)null;
            var consequences=ImmutableArray.Create("Use this colony's one production slot; bank output until a single completion payment.","Keep vessel, fleet, flagship, officer, hull and cohesion; installed equipment changes only on completion.","Construction stays paused; queue, template and workforce remain. Cancel explicitly before replacing the job or moving its ship.");
            if(action is RefitAction.Cancel)
            {
                estimate=null;consequences=ImmutableArray.Create("Cancel this job without payment or equipment changes; all banked reserve stays at the colony.","Release this job's ship and production slot. Construction remains paused; the retained queue, template and workforce are unchanged.");
            }
            string token=Token(content,view,action,job,ship,port,source,target,price);
            return new(true,null,token,job?.Id,ship.Id,port.Id,source.Id,target.Id,source.Stats,target.Stats,price,estimate,consequences);
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or OverflowException or System.Collections.Generic.KeyNotFoundException){return Reject("refit_unavailable");}
    }
    internal static bool SameEquipment(ShipRecipe a,ShipRecipe b)=>a.FrameId==b.FrameId&&a.EngineId==b.EngineId&&a.ArmorId==b.ArmorId&&a.WeaponBankIds.Order(StringComparer.Ordinal).SequenceEqual(b.WeaponBankIds.Order(StringComparer.Ordinal));
    private static string? Eligibility(EmpireView view,OwnedVesselView ship,OwnedColonyView port,RefitView? job)
    {
        var condition=view.Combat!.Ships.SingleOrDefault(v=>v.Id==ship.Id);
        if(condition is null||condition.Hull!=ship.Loadout!.Profile!.MaxHull||condition.Cohesion!=100||ship.CarriedPopulation!=0)return "refit_condition";
        if(ship.Transit is not null||ship.StationaryStarId!=port.StarId||ship.PendingMove is not null||ship.PendingFounding is not null)return "refit_location";
        if(condition.ReturnColony is not null||condition.Repairing||view.Combat.Encounters.Any(e=>e.Members.Contains(ship.Id))||view.Scouting?.Vessels.Any(v=>v.Id==ship.Id&&v.Stage is not null)==true)return "refit_activity";
        var group=view.Fleets!.Fleets.SingleOrDefault(f=>f.Members.Contains(ship.Id));
        if(group is not null&&(group.Order.Kind!=FleetOrderKind.Hold||group.Cohort is not null))return "refit_fleet_order";
        if(view.Fleets.Requests.Any(r=>FleetQueries.IsActive(r.Status)&&(r.Slots.Any(s=>s.Vessel==ship.Id&&s.Status!=SlotStatus.Missing)||group is not null&&r.RallyFleet==group.Id)))return "refit_request";
        if(job is null&&port.ProjectId is not null)return "refit_project_busy";
        if(job is null&&view.Fleets.Requests.Any(r=>r.Status==RequestStatus.Active&&r.Colonies.Contains(port.Id)&&!r.PausedContributions.Contains(port.Id)))return "refit_request_port";
        return null;
    }
    private static string Token(ContentSet content,EmpireView view,RefitAction action,RefitView? job,OwnedVesselView ship,OwnedColonyView port,DesignView source,DesignView target,long price)
    {
        var group=view.Fleets!.Fleets.SingleOrDefault(f=>f.Members.Contains(ship.Id));
        var conditions=view.Combat!.Ships.Single(c=>c.Id==ship.Id);
        var requirements=content.ShipDesigns!.Frames[target.Recipe.FrameId].Prerequisites.Concat(target.Recipe.WeaponBankIds.Prepend(target.Recipe.EngineId).Concat(target.Recipe.ArmorId is {} armor?new[]{armor}:Array.Empty<string>()).SelectMany(id=>content.ShipDesigns.Components[id].Prerequisites)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Select(id=>new{Id=id,Known=view.Research.KnownTechnologyIds.Contains(id)}).ToArray();
        var material=new
        {
            view.Turn,view.Viewer,view.RulesVersion,Action=JsonSerializer.Serialize(action,action.GetType()),Job=job,
            NewJobId=job is null?(int?)view.ShipDesigns!.NextRefitId:null,
            Ship=new{ship.Id,ship.RoleId,ship.StationaryStarId,ship.Transit,ship.PendingMove,ship.PendingFounding,ship.CarriedPopulation,Condition=conditions,Group=group},
            Port=new{port.Id,port.EmpireId,port.StarId,port.ProjectId,port.ProjectKind,ProductionReserve=port.SpendableProduction,Output=port.Economy.NetProduction.Total,ActiveQueueToken=port.Development?.ActiveQueueToken},
            Source=new{source.Id,source.Name,source.Recipe,source.Stats},Target=new{target.Id,target.Name,target.Recipe,target.Stats,target.Archived,target.Unlocked},Price=price,Requirements=requirements,
            Requests=view.Fleets.Requests.Where(r=>FleetQueries.IsActive(r.Status)&&(r.Colonies.Contains(port.Id)||r.Slots.Any(s=>s.Vessel==ship.Id)||group is not null&&r.RallyFleet==group.Id)).Select(r=>new{r.Id,r.Revision,r.Status,r.PausedContributions}).ToArray(),
            Ameba=view.Scouting?.Vessels.Where(s=>s.Id==ship.Id).Select(s=>new{s.Stage,s.EncounterNumber,s.Revision,s.Held}).ToArray()
        };
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(material)));
    }
}
