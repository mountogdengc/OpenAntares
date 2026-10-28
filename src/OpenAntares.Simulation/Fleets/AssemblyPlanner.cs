using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Economy;
namespace OpenAntares.Simulation.Fleets;
public static class AssemblyPlanner
{
    public static AssemblyPreview Preview(IFleetPlanningContext context,AssemblyDraft draft,RequestId? replacing=null)
    {
        var c=context.Content;var v=context.View;
        string fingerprint=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new{Draft=draft,Replacing=replacing,v.Turn,v.Viewer,v.RulesVersion,Galaxy=v.Galaxy with{Seed=null},v.Stars,v.Colonies,v.Fleets,v.Scouting,v.ForeignVessels,v.Research.KnownTechnologyIds,c.Vessels,c.Buildings,c.Species,c.FleetCommand})));
        var warnings=ImmutableArray.CreateBuilder<string>();var errors=ImmutableArray.CreateBuilder<string>();
        AssemblyPreview Failed(string reason)=>new(false,fingerprint,ImmutableArray<AssemblySlotPlan>.Empty,ImmutableArray<AssemblyColonyPreview>.Empty,ImmutableArray<FleetShipView>.Empty,warnings.ToImmutable(),ImmutableArray.Create(reason));
        try
        {
            if(!Rules.HasFleets(v.RulesVersion)||c.FleetCommand is not {} d||v.Fleets is not {} f)return Failed("Assembly requires fleet command rules.");
            if(string.IsNullOrWhiteSpace(draft.Name)||draft.Name.Length>80||draft.Name.Any(char.IsControl)||!Enum.IsDefined(draft.Priority))return Failed("Choose a printable1..80 character name and valid priority.");
            if(draft.Roles.IsDefaultOrEmpty||draft.Roles.Length>d.MemberLimit||draft.Colonies.IsDefaultOrEmpty||draft.Colonies.Length>d.ParticipatingColonyLimit||draft.Colonies.Distinct().Count()!=draft.Colonies.Length||draft.AcceptedIdleVessels.IsDefault||draft.AcceptedIdleVessels.Distinct().Count()!=draft.AcceptedIdleVessels.Length)return Failed("Choose a bounded finite quota, distinct own colonies and distinct idle ships.");
            if(!v.Galaxy.Stars.Any(s=>s.Id==draft.Rally)||v.Scouting?.Habitats.Any(h=>h.Star==draft.Rally)==true)return Failed("Rally must be charted and free of known ameba danger.");
            if(draft.Roles.Any(id=>id is null||!c.Vessels.ContainsKey(id)||!v.AvailableVesselRoleIds.Contains(id)))return Failed("A requested role is unavailable to this empire.");
            if(draft.Roles.Any(id=>c.Vessels[id].EmbarkPopulation>0)&&!draft.AuthorizeCargo)return Failed("Explicit population cargo authorization is required.");
            if(draft.Roles.Any(id=>c.Vessels[id].EmbarkPopulation>0)&&v.Colonies.Any(col=>col.StarId==draft.Rally&&col.Crisis?.Quarantined==true))return Failed("The own population rally port is quarantined.");
            var others=f.Requests.Where(r=>FleetQueries.IsActive(r.Status)&&r.Id!=replacing).ToArray();
            if(replacing is {} replacement&&!f.Requests.Any(r=>r.Id==replacement&&FleetQueries.IsActive(r.Status)))return Failed("The request to amend is unavailable or final.");
            if(others.Length>=d.RequestLimit||others.Any(r=>r.Colonies.Intersect(draft.Colonies).Any()))return Failed("A participating colony or active request slot is already reserved.");
            var colonies=draft.Colonies.OrderBy(id=>id).Select(id=>v.Colonies.SingleOrDefault(col=>col.Id==id&&col.EmpireId==v.Viewer)).ToArray();
            if(colonies.Any(col=>col?.Development is null))return Failed("Every participating colony must be owned and available.");
            if(f.Fleets.Length+others.Count(r=>r.ReservedFleetSlot)+(draft.RallyFleet is null?1:0)>d.FleetLimit)return Failed("No named rally-fleet slot is available.");
            var rally=draft.RallyFleet is {} fleet?f.Fleets.SingleOrDefault(x=>x.Id==fleet):null;
            if(draft.RallyFleet is not null&&(rally is null||others.Any(r=>r.RallyFleet==rally.Id)||rally.Members.Any(id=>f.Ships.Single(x=>x.Id==id) is not {Transit:null,PendingMove:null} ship||ship.Station!=draft.Rally)||rally.Members.Length+draft.Roles.Length-draft.AcceptedIdleVessels.Count(rally.Members.Contains)>d.MemberLimit))return Failed("The rally fleet is unavailable, departing, reserved or lacks capacity.");
            bool Forbidden(VesselId id)=>v.Scouting?.Vessels.Any(row=>row.Id==id&&row.Posture==Scouting.ScoutPosture.AvoidEncounters)==true&&Scouting.ScoutQueries.NeutralExcluded(v,draft.Rally);
            if(rally?.Members.Any(Forbidden)==true)return Failed("Rally is forbidden by a member's current posture.");
            if(draft.Coordinator is {} coordinator)
            {
                var officer=f.Officers.SingleOrDefault(o=>o.Id==coordinator);
                if(officer is null||!(officer.Status==OfficerStatus.Posted||officer.Status==OfficerStatus.Coordinating&&officer.Request==replacing)||officer.PostedColony is not {} posting||!draft.Colonies.Contains(posting))return Failed("Coordinator must be available at a participating colony.");
            }
            var idle=f.Ships.Where(ship=>ship.Transit is null&&ship.PendingMove is null&&(ship.Fleet is null||ship.Fleet==draft.RallyFleet&&ship.Request==replacing)&&(ship.Request is null||ship.Request==replacing)&&v.Vessels.Single(x=>x.Id==ship.Id).PendingFounding is null&&v.Scouting?.Vessels.Any(row=>row.Id==ship.Id&&(row.Held||row.Stage is not null))!=true&&!Forbidden(ship.Id)).OrderBy(ship=>ship.Id).ToImmutableArray();
            if(draft.AcceptedIdleVessels.Any(id=>!idle.Any(ship=>ship.Id==id)))return Failed("An explicitly accepted idle ship is stale, held, assigned or forbidden.");
            var remaining=draft.Roles.ToList();foreach(var id in draft.AcceptedIdleVessels){string role=idle.Single(ship=>ship.Id==id).RoleId;if(!remaining.Remove(role))return Failed("Accepted idle ships exceed the matching role quota.");}
            var allocated=colonies.Select(col=>col!).ToDictionary(col=>col.Id,col=>new List<AssemblyQueueItem>());
            var slots=new List<AssemblySlotPlan>();var accepted=draft.AcceptedIdleVessels.OrderBy(id=>id).ToList();
            var forecasts=new Dictionary<(ColonyId,string,string),ColonyEconomy?>();
            ColonyEconomy? Forecast(OwnedColonyView col,ProductionTarget? target)
            {
                var key=(col.Id,target?.Kind??"",target?.ContentId??"");
                if(!forecasts.TryGetValue(key,out var forecast)){forecast=context.Forecast(col.Id,col.Workforce,target);forecasts.Add(key,forecast);}return forecast;
            }
            long Travel(StarId origin,string role)
            {if(origin==draft.Rally)return 0;var start=v.Galaxy.Stars.Single(s=>s.Id==origin);var end=v.Galaxy.Stars.Single(s=>s.Id==draft.Rally);return Movement.TravelCalculator.Duration(start.X,start.Y,end.X,end.Y,c.Vessels[role].Speed);}
            List<AssemblyQueueItem> Queue(OwnedColonyView col,List<AssemblyQueueItem> additions)
            {
                var old=col.Development!.Queue.Select(q=>new AssemblyQueueItem(q.Token,q.Target,null)).ToList();
                if(draft.Priority==RequestPriority.Normal)return old.Concat(additions).ToList();
                var active=old.Where(q=>q.Token==col.Development.ActiveQueueToken).ToList();return active.Concat(additions).Concat(old.Where(q=>q.Token!=col.Development.ActiveQueueToken)).ToList();
            }
            (List<AssemblyQueueItem> Queue,long? Active,long Output) Estimate(OwnedColonyView col,List<AssemblyQueueItem> additions)
            {
                var queue=Queue(col,additions);var baseline=Forecast(col,null);long output=baseline?.NetProduction.Total??0,growth=baseline?.GrowthEarned.Total??0;int capacity=baseline?.Growth.Capacity??col.Population;
                long reserve=col.ProductionReserve,time=0,progress=col.GrowthProgress;int population=col.Population,support=col.Workforce.Support;bool stalled=false;
                long? Work(ProductionTarget target)
                {
                    if(stalled)return null;var forecast=Forecast(col,target);
                    long? price=target.Kind==ProductionKinds.Vessel&&c.Vessels.TryGetValue(target.ContentId,out var roleCost)?roleCost.Cost:target.Kind==ProductionKinds.Building&&c.Buildings.TryGetValue(target.ContentId,out var building)?building.Cost:null;
                    if(forecast is null||price is null){stalled=true;return null;}
                    long deficit=Math.Max(0,checked(price.Value-reserve));
                    if(deficit>0&&output<=0){stalled=true;return null;}
                    long interval=deficit==0?1:Math.Max(1,checked((deficit-1)/output+1));int cargo=0;
                    if(target.Kind==ProductionKinds.Vessel)
                    {
                        var role=c.Vessels[target.ContentId];cargo=role.EmbarkPopulation;
                        if(cargo>0&&(col.Crisis?.Quarantined==true||forecast.Production.BlockedRequirements.Any(r=>r.Code=="quarantine"))){stalled=true;return null;}
                        int required=role.RequiresFullColony?capacity:Math.Max(role.MinimumSourcePopulation,role.CanFoundColony?cargo+1:0);
                        int births=Math.Max(0,Math.Max(required-population,cargo-support));
                        if(required>capacity||population+births>capacity||births>0&&growth<=0){stalled=true;return null;}
                        if(births>0){long waits=Math.Max(0,checked(((long)births*100-progress+growth-1)/growth));interval=Math.Max(interval,checked(waits+1));}
                    }
                    time=checked(time+interval);reserve=checked(reserve+checked(output*interval)-price.Value);
                    long earned=checked(progress+checked(growth*interval));int added=checked((int)Math.Min(capacity-population,earned/100));population=checked(population+added-cargo);support=checked(support+added-cargo);progress=population+cargo>=capacity?0:earned%100;
                    return time;
                }
                long? active=null;
                if(col.ProjectId is {} project)active=Work(new(col.ProjectKind,project));
                for(int i=0;i<queue.Count;i++)queue[i]=queue[i] with{CompletionTurns=queue[i].Token==col.Development!.ActiveQueueToken&&col.Development.ActiveQueueToken is not null?active:Work(queue[i].Target)};
                return(queue,active,output);
            }
            for(int index=0;index<draft.Roles.Length;index++)
            {
                string role=draft.Roles[index];int ordinal=index+1;var existing=accepted.FirstOrDefault(id=>idle.Single(ship=>ship.Id==id).RoleId==role);
                if(existing.Value>0)
                {
                    accepted.Remove(existing);var ship=idle.Single(x=>x.Id==existing);slots.Add(new(ordinal,role,existing,null,null,0,c.Vessels[role].EmbarkPopulation,0,Travel(ship.Station!.Value,role)));continue;
                }
                var options=colonies.Select(col=>col!).Where(col=>col.Development!.Queue.Length+allocated[col.Id].Count<32).Select(col=>
                {
                    var additions=allocated[col.Id].Append(new AssemblyQueueItem(null,new(ProductionKinds.Vessel,role),ordinal)).ToList();var estimate=Estimate(col,additions);var item=estimate.Queue.Single(q=>q.SlotOrdinal==ordinal);
                    long? arrival=item.CompletionTurns is {} turns?checked(turns+Travel(col.StarId,role)):(long?)null;return new{Colony=col,Item=item,Arrival=arrival,Position=estimate.Queue.IndexOf(item)};
                }).OrderBy(option=>option.Arrival??long.MaxValue).ThenBy(option=>option.Colony.Id).ToArray();
                if(options.Length==0)return Failed("No participating colony has remaining finite queue capacity.");var chosen=options[0];allocated[chosen.Colony.Id].Add(new(null,new(ProductionKinds.Vessel,role),ordinal));slots.Add(new(ordinal,role,null,chosen.Colony.Id,chosen.Position,c.Vessels[role].Cost,c.Vessels[role].EmbarkPopulation,chosen.Item.CompletionTurns,chosen.Arrival));
            }
            var colonyPreviews=ImmutableArray.CreateBuilder<AssemblyColonyPreview>();
            foreach(var col in colonies.Select(col=>col!))
            {
                var estimate=Estimate(col,allocated[col.Id]);
                foreach(var item in estimate.Queue.Where(q=>q.SlotOrdinal is not null))
                {int i=slots.FindIndex(s=>s.Ordinal==item.SlotOrdinal);var slot=slots[i];slots[i]=slot with{CompletionTurns=item.CompletionTurns,ArrivalTurns=item.CompletionTurns is {} turns?checked(turns+Travel(col.StarId,slot.RoleId)):(long?)null,QueuePosition=estimate.Queue.IndexOf(item)};}
                colonyPreviews.Add(new(col.Id,col.ProjectId is {} project?new(col.ProjectKind,project):null,col.Development!.ActiveQueueToken,col.ProductionReserve,estimate.Output,allocated[col.Id].Sum(q=>c.Vessels[q.Target.ContentId].EmbarkPopulation),col.Development.Queue,estimate.Queue.ToImmutableArray()));
            }
            warnings.Add("Conditional estimates hold current output and growth fixed; future manual orders, quarantine and research may change them. Each project takes at least one resolution.");
            if(slots.Any(s=>s.ArrivalTurns is null))warnings.Add("Some work has no finite completion/arrival estimate and remains blocked behind its active project or current requirements.");
            return new(true,fingerprint,slots.OrderBy(s=>s.Ordinal).ToImmutableArray(),colonyPreviews.ToImmutable(),idle.Where(ship=>draft.Roles.Contains(ship.RoleId)).ToImmutableArray(),warnings.ToImmutable(),errors.ToImmutable());
        }
        catch(Exception error)when(error is OverflowException or ArgumentException or InvalidOperationException){return Failed("Preview cannot estimate this finite request: "+error.Message);}
    }
}
